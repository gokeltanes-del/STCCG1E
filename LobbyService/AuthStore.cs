using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

// Accounts, server decks (cardId lists only), frozen match decks, and match history.
// History is inserted only when both seats report the same winner.
// That insert is the one latinum credit for an account match: loser 5, winner 10.
// Sandbox, one report, disagreeing reports, and a repeat report pay nothing.
// buy_pack draws a Premiere booster on the server. Not enough latinum changes nothing.
// trade_offer / trade_accept move owned cards in one transaction, or neither.
// A raiseTheStakes flag on both agreeing reports moves one server-picked card with the latinum.
sealed partial class AuthStore : IDisposable
{
    public const int NameMax = 24;
    public const int PasswordMin = 8;
    public const int PasswordMax = 72;
    public const int DeckNameMax = 80;
    public const int MaxCards = 500;
    public const int MatchLoserLatinum = 5;
    public const int MatchWinnerLatinum = MatchLoserLatinum * 2;

    private static readonly string[] Sections =
    {
        "battle_bridge", "draw", "q_continuum", "qs_tent", "seed", "side", "site_pile", "tribble"
    };

    private readonly SqliteConnection _db;
    private readonly StarterGrant _starter;
    private readonly PremiereCatalog _premiere;
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, ReportVote>> _pending = new(StringComparer.Ordinal);

    public AuthStore(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        _db = new SqliteConnection("Data Source=" + path);
        _db.Open();
        using var pragma = _db.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL COLLATE NOCASE UNIQUE,
                password_hash TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                latinum INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS sessions (
                token TEXT PRIMARY KEY,
                user_id INTEGER NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS decks (
                user_id INTEGER NOT NULL,
                name TEXT NOT NULL COLLATE NOCASE,
                card_ids TEXT NOT NULL,
                PRIMARY KEY (user_id, name)
            );
            CREATE TABLE IF NOT EXISTS account_cards (
                user_id INTEGER NOT NULL,
                card_id TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                PRIMARY KEY (user_id, card_id)
            );
            CREATE TABLE IF NOT EXISTS matches (
                match_id TEXT PRIMARY KEY,
                mode TEXT NOT NULL,
                host_name TEXT NOT NULL,
                guest_name TEXT NOT NULL,
                host_secret TEXT NOT NULL,
                guest_secret TEXT NOT NULL,
                host_deck_name TEXT NOT NULL,
                guest_deck_name TEXT NOT NULL,
                host_cards TEXT NOT NULL,
                guest_cards TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS match_history (
                match_id TEXT PRIMARY KEY,
                mode TEXT NOT NULL,
                host_name TEXT NOT NULL,
                guest_name TEXT NOT NULL,
                winner_seat TEXT NOT NULL,
                winner_name TEXT NOT NULL,
                written_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS match_saves (
                match_id TEXT NOT NULL,
                kind TEXT NOT NULL,
                name TEXT NOT NULL,
                host_user_id INTEGER NOT NULL,
                guest_user_id INTEGER NOT NULL,
                blob TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                PRIMARY KEY (match_id, kind, name)
            );
            CREATE TABLE IF NOT EXISTS trade_offers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                from_user INTEGER NOT NULL,
                to_user INTEGER NOT NULL,
                offer_json TEXT NOT NULL,
                request_json TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
        EnsureMatchAccountColumns();
        EnsureLatinumColumn();
        var start = Path.GetDirectoryName(path) ?? ".";
        _starter = StarterDeck.Load(start);
        _premiere = PremiereCatalog.Load(start);
    }

    public object Register(string name, string password)
    {
        if (!TryName(name, out var clean, out var error))
            return Err(error);
        if (!TryPassword(password, out error))
            return Err(error);
        lock (_gate)
        {
            using var exists = _db.CreateCommand();
            exists.CommandText = "SELECT 1 FROM users WHERE name = $name";
            exists.Parameters.AddWithValue("$name", clean);
            if (exists.ExecuteScalar() != null)
                return Err("that name is taken");
            using var tx = _db.BeginTransaction();
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
            using var insert = _db.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = "INSERT INTO users (name, password_hash, created_utc, latinum) VALUES ($name, $hash, $utc, $latinum)";
            insert.Parameters.AddWithValue("$name", clean);
            insert.Parameters.AddWithValue("$hash", hash);
            insert.Parameters.AddWithValue("$utc", Now());
            insert.Parameters.AddWithValue("$latinum", StarterDeck.Latinum);
            insert.ExecuteNonQuery();
            using var idCmd = _db.CreateCommand();
            idCmd.Transaction = tx;
            idCmd.CommandText = "SELECT id FROM users WHERE name = $name";
            idCmd.Parameters.AddWithValue("$name", clean);
            var id = Convert.ToInt32(idCmd.ExecuteScalar());
            GrantStarter(id, tx);
            var token = NewToken();
            InsertSession(id, token, tx);
            tx.Commit();
            return new { type = "auth", token, name = clean, mode = "account", latinum = StarterDeck.Latinum };
        }
    }

    public object Login(string name, string password)
    {
        if (!TryName(name, out var clean, out var error))
            return Err(error);
        if (string.IsNullOrEmpty(password))
            return Err("wrong password");
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT id, password_hash, name, latinum FROM users WHERE name = $name";
            cmd.Parameters.AddWithValue("$name", clean);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Err("wrong password");
            var id = reader.GetInt32(0);
            var hash = reader.GetString(1);
            var storedName = reader.GetString(2);
            var latinum = reader.GetInt32(3);
            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(password, hash); }
            catch { ok = false; }
            if (!ok)
                return Err("wrong password");
            var token = NewToken();
            InsertSession(id, token);
            return new { type = "auth", token, name = storedName, mode = "account", latinum };
        }
    }

    public bool TrySession(string? token, out AccountUser user)
    {
        user = new AccountUser(0, "");
        if (string.IsNullOrWhiteSpace(token))
            return false;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT u.id, u.name
                FROM sessions s
                JOIN users u ON u.id = s.user_id
                WHERE s.token = $token
                """;
            cmd.Parameters.AddWithValue("$token", token.Trim());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;
            user = new AccountUser(reader.GetInt32(0), reader.GetString(1));
            return true;
        }
    }

    public object SaveDeck(string? token, string? deckName, JsonElement cardIds)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        deckName = LobbySession.ClipName(deckName, DeckNameMax);
        if (deckName.Length == 0)
            return Err("deck name required");
        if (!TryCanonical(cardIds, out var canonical, out var count, out var error))
            return Err(error);
        lock (_gate)
        {
            if (!FitsPool(user.Id, canonical, out error))
                return Err(error);
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO decks (user_id, name, card_ids) VALUES ($user, $name, $cards)
                ON CONFLICT(user_id, name) DO UPDATE SET card_ids = excluded.card_ids
                """;
            cmd.Parameters.AddWithValue("$user", user.Id);
            cmd.Parameters.AddWithValue("$name", deckName);
            cmd.Parameters.AddWithValue("$cards", canonical);
            cmd.ExecuteNonQuery();
        }
        return new { type = "deckSaved", name = deckName, count };
    }

    public object GetPool(string? token)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        var cards = new List<object>();
        int latinum;
        lock (_gate)
        {
            latinum = ReadLatinum(user.Id);
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT card_id, quantity FROM account_cards WHERE user_id = $user ORDER BY card_id";
            cmd.Parameters.AddWithValue("$user", user.Id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                cards.Add(new { cardId = reader.GetString(0), quantity = reader.GetInt32(1) });
        }
        return new { type = "pool", latinum, cards };
    }

    public object BuyPack(string? token, string? packType)
    {
        if (!string.Equals((packType ?? "").Trim(), PremiereCatalog.PackType, StringComparison.Ordinal))
            return Err("unknown pack");
        if (!TrySession(token, out var user))
            return Err("login required");

        // Drawn here, from the catalog. The caller has no card names to apply.
        var drawn = _premiere.Draw();
        if (drawn.Count != PremiereCatalog.RareCount + PremiereCatalog.UncommonCount + PremiereCatalog.CommonCount)
            return Err("pack was not drawn");

        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            using var upd = _db.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE users SET latinum = latinum - $price WHERE id = $user AND latinum >= $price";
            upd.Parameters.AddWithValue("$price", PremiereCatalog.Price);
            upd.Parameters.AddWithValue("$user", user.Id);
            if (upd.ExecuteNonQuery() != 1)
            {
                tx.Rollback();
                return Err("not enough latinum");
            }

            foreach (var cardId in drawn)
                AddOwnedCard(user.Id, cardId, tx);

            var left = ReadLatinum(user.Id, tx);
            tx.Commit();
            return new { type = "pack", packType = PremiereCatalog.PackType, price = PremiereCatalog.Price, latinum = left, cardIds = drawn };
        }
    }

    public object ListDecks(string? token)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        var rows = new List<object>();
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT name, card_ids FROM decks WHERE user_id = $user ORDER BY name";
            cmd.Parameters.AddWithValue("$user", user.Id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                rows.Add(new { name = reader.GetString(0), count = CountIds(reader.GetString(1)) });
        }
        return new { type = "deckList", decks = rows };
    }

    public object GetDeck(string? token, string? deckName)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        deckName = LobbySession.ClipName(deckName, DeckNameMax);
        if (!TryReadDeck(user.Id, deckName, out var canonical, out var error))
            return Err(error);
        using var doc = JsonDocument.Parse(canonical);
        return new { type = "deckBody", name = deckName, cardIds = doc.RootElement.Clone() };
    }

    public bool TryReadDeck(int userId, string deckName, out string canonical, out string error)
    {
        canonical = "";
        error = "";
        deckName = LobbySession.ClipName(deckName, DeckNameMax);
        if (deckName.Length == 0)
        {
            error = "deck name required";
            return false;
        }
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT card_ids FROM decks WHERE user_id = $user AND name = $name";
            cmd.Parameters.AddWithValue("$user", userId);
            cmd.Parameters.AddWithValue("$name", deckName);
            var value = cmd.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(value))
            {
                error = "no such deck";
                return false;
            }
            canonical = value;
            return true;
        }
    }

    public FrozenMatch Freeze(string mode, string hostName, string guestName, string hostDeck, string hostCards, string guestDeck, string guestCards, int hostUserId, int guestUserId)
    {
        var matchId = Guid.NewGuid().ToString("N");
        var hostSecret = NewToken();
        var guestSecret = NewToken();
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO matches (
                    match_id, mode, host_name, guest_name, host_secret, guest_secret,
                    host_deck_name, guest_deck_name, host_cards, guest_cards, created_utc,
                    host_user_id, guest_user_id)
                VALUES (
                    $id, $mode, $host, $guest, $hs, $gs, $hd, $gd, $hc, $gc, $utc, $hu, $gu)
                """;
            cmd.Parameters.AddWithValue("$id", matchId);
            cmd.Parameters.AddWithValue("$mode", mode);
            cmd.Parameters.AddWithValue("$host", hostName);
            cmd.Parameters.AddWithValue("$guest", guestName);
            cmd.Parameters.AddWithValue("$hs", hostSecret);
            cmd.Parameters.AddWithValue("$gs", guestSecret);
            cmd.Parameters.AddWithValue("$hd", hostDeck);
            cmd.Parameters.AddWithValue("$gd", guestDeck);
            cmd.Parameters.AddWithValue("$hc", hostCards);
            cmd.Parameters.AddWithValue("$gc", guestCards);
            cmd.Parameters.AddWithValue("$utc", Now());
            cmd.Parameters.AddWithValue("$hu", hostUserId);
            cmd.Parameters.AddWithValue("$gu", guestUserId);
            cmd.ExecuteNonQuery();
        }
        return new FrozenMatch(matchId, hostSecret, guestSecret);
    }

    public object Report(string? matchId, string? secret, string? winnerSeat, bool raiseTheStakes)
    {
        matchId = (matchId ?? "").Trim();
        secret = (secret ?? "").Trim();
        winnerSeat = (winnerSeat ?? "").Trim().ToLowerInvariant();
        if (matchId.Length == 0 || secret.Length == 0)
            return new { type = "report", written = false, message = "match id and secret required" };
        if (winnerSeat is not ("host" or "guest"))
            return new { type = "report", written = false, message = "winner must be host or guest" };

        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT mode, host_name, guest_name, host_secret, guest_secret, host_user_id, guest_user_id,
                       host_cards, guest_cards
                FROM matches WHERE match_id = $id
                """;
            cmd.Parameters.AddWithValue("$id", matchId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return new { type = "report", written = false, message = "no such match" };
            var mode = reader.GetString(0);
            var hostName = reader.GetString(1);
            var guestName = reader.GetString(2);
            var hostSecret = reader.GetString(3);
            var guestSecret = reader.GetString(4);
            var hostUserId = reader.GetInt32(5);
            var guestUserId = reader.GetInt32(6);
            var hostCards = reader.GetString(7);
            var guestCards = reader.GetString(8);
            reader.Close();

            string seat;
            if (string.Equals(secret, hostSecret, StringComparison.Ordinal))
                seat = "host";
            else if (string.Equals(secret, guestSecret, StringComparison.Ordinal))
                seat = "guest";
            else
                return new { type = "report", written = false, message = "not your match" };

            using var already = _db.CreateCommand();
            already.CommandText = "SELECT 1 FROM match_history WHERE match_id = $id";
            already.Parameters.AddWithValue("$id", matchId);
            if (already.ExecuteScalar() != null)
            {
                DeleteAutosave(matchId);
                return new { type = "report", written = true, rewarded = false, message = "already written" };
            }

            if (!_pending.TryGetValue(matchId, out var votes))
            {
                votes = new Dictionary<string, ReportVote>(StringComparer.Ordinal);
                _pending[matchId] = votes;
            }
            // raiseTheStakes is a boolean. A card id on this message is ignored.
            votes[seat] = new ReportVote(winnerSeat, raiseTheStakes);
            if (votes.Count < 2)
                return new { type = "report", written = false, message = "waiting for the other player" };

            var hostVote = votes["host"];
            var guestVote = votes["guest"];
            _pending.Remove(matchId);
            if (!string.Equals(hostVote.Winner, guestVote.Winner, StringComparison.Ordinal))
                return new { type = "report", written = false, message = "reports disagree" };

            var agreed = hostVote.Winner;
            var stakes = hostVote.RaiseTheStakes && guestVote.RaiseTheStakes;
            var winnerName = agreed == "host" ? hostName : guestName;
            var winnerId = agreed == "host" ? hostUserId : guestUserId;
            var loserId = agreed == "host" ? guestUserId : hostUserId;
            var loserCards = agreed == "host" ? guestCards : hostCards;
            var account = string.Equals(mode, "account", StringComparison.Ordinal);
            using var tx = _db.BeginTransaction();
            using var insert = _db.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                INSERT INTO match_history (match_id, mode, host_name, guest_name, winner_seat, winner_name, written_utc)
                VALUES ($id, $mode, $host, $guest, $seat, $name, $utc)
                """;
            insert.Parameters.AddWithValue("$id", matchId);
            insert.Parameters.AddWithValue("$mode", mode);
            insert.Parameters.AddWithValue("$host", hostName);
            insert.Parameters.AddWithValue("$guest", guestName);
            insert.Parameters.AddWithValue("$seat", agreed);
            insert.Parameters.AddWithValue("$name", winnerName);
            insert.Parameters.AddWithValue("$utc", Now());
            insert.ExecuteNonQuery();
            var rewarded = false;
            if (account)
            {
                if (winnerId > 0)
                {
                    AddLatinum(_db, tx, winnerId, MatchWinnerLatinum);
                    rewarded = true;
                }
                if (loserId > 0)
                {
                    AddLatinum(_db, tx, loserId, MatchLoserLatinum);
                    rewarded = true;
                }
            }
            string? stakeCard = null;
            if (account && stakes && winnerId > 0 && loserId > 0)
                stakeCard = MoveStakeCard(loserId, winnerId, loserCards, tx);
            tx.Commit();
            DeleteAutosave(matchId);
            if (!rewarded)
                return new { type = "report", written = true, rewarded = false, message = "written" };
            var reporterId = seat == "host" ? hostUserId : guestUserId;
            var latinum = reporterId > 0 ? ReadLatinum(reporterId) : 0;
            if (stakeCard != null)
                return new { type = "report", written = true, rewarded = true, message = "written", latinum, raiseTheStakes = true, cardId = stakeCard };
            return new { type = "report", written = true, rewarded = true, message = "written", latinum, raiseTheStakes = stakes };
        }
    }

    public static bool TryCanonical(JsonElement cardIds, out string canonical, out int count, out string error)
    {
        canonical = "";
        count = 0;
        error = "";
        if (cardIds.ValueKind != JsonValueKind.Object)
        {
            error = "cardIds must be an object of lists";
            return false;
        }

        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var section in Sections)
            lists[section] = new List<string>();

        foreach (var prop in cardIds.EnumerateObject())
        {
            if (!lists.ContainsKey(prop.Name))
            {
                error = "unknown deck section " + prop.Name;
                return false;
            }
            if (prop.Value.ValueKind != JsonValueKind.Array)
            {
                error = prop.Name + " must be a list of card ids";
                return false;
            }
            foreach (var item in prop.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    error = "card id must be a string";
                    return false;
                }
                var id = (item.GetString() ?? "").Trim();
                if (!CardIdOk(id))
                {
                    error = "card id must be SetFolder/ReleaseRaw/Name";
                    return false;
                }
                lists[prop.Name].Add(id);
                count++;
                if (count > MaxCards)
                {
                    error = "deck is too large";
                    return false;
                }
            }
        }

        canonical = JsonSerializer.Serialize(lists);
        return true;
    }

    public static string Sha256Hex(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    public static int CountIds(string canonical)
    {
        try
        {
            using var doc = JsonDocument.Parse(canonical);
            var n = 0;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    n += prop.Value.GetArrayLength();
            }
            return n;
        }
        catch
        {
            return 0;
        }
    }

    public const int SaveMaxBytes = 2_000_000;
    public const int SaveNameMax = 40;

    public object PutSave(string? matchId, string? secret, string? kind, string? name, string? blob)
    {
        matchId = (matchId ?? "").Trim();
        secret = (secret ?? "").Trim();
        kind = (kind ?? "").Trim().ToLowerInvariant();
        blob = blob ?? "";
        if (matchId.Length == 0 || secret.Length == 0)
            return Err("match id and secret required");
        if (kind is not ("auto" or "manual"))
            return Err("save kind must be auto or manual");
        if (blob.Length == 0 || blob.Length > SaveMaxBytes)
            return Err("save is empty or too large");
        if (!SaveJsonOk(blob))
            return Err("save must be a json object");
        if (kind == "auto")
            name = "";
        else
        {
            name = LobbySession.ClipName(name, SaveNameMax);
            if (name.Length == 0)
                return Err("save name required");
        }

        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT mode, host_secret, host_user_id, guest_user_id
                FROM matches WHERE match_id = $id
                """;
            cmd.Parameters.AddWithValue("$id", matchId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Err("no such match");
            var mode = reader.GetString(0);
            var hostSecret = reader.GetString(1);
            var hostUser = reader.GetInt32(2);
            var guestUser = reader.GetInt32(3);
            reader.Close();
            if (!string.Equals(mode, "account", StringComparison.Ordinal) || hostUser <= 0 || guestUser <= 0)
                return Err("only an account match is saved");
            if (!string.Equals(secret, hostSecret, StringComparison.Ordinal))
                return Err("only the host stores the save");

            using var up = _db.CreateCommand();
            up.CommandText = """
                INSERT INTO match_saves (match_id, kind, name, host_user_id, guest_user_id, blob, updated_utc)
                VALUES ($id, $kind, $name, $host, $guest, $blob, $utc)
                ON CONFLICT(match_id, kind, name) DO UPDATE SET
                    blob = excluded.blob,
                    updated_utc = excluded.updated_utc
                """;
            up.Parameters.AddWithValue("$id", matchId);
            up.Parameters.AddWithValue("$kind", kind);
            up.Parameters.AddWithValue("$name", name);
            up.Parameters.AddWithValue("$host", hostUser);
            up.Parameters.AddWithValue("$guest", guestUser);
            up.Parameters.AddWithValue("$blob", blob);
            up.Parameters.AddWithValue("$utc", Now());
            up.ExecuteNonQuery();
        }
        return new { type = "saveStored", kind, name };
    }

    public object[] ListSaves(int hostUserId, int guestUserId)
    {
        var rows = new List<object>();
        if (hostUserId <= 0 || guestUserId <= 0)
            return rows.ToArray();
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT match_id, kind, name, updated_utc
                FROM match_saves
                WHERE host_user_id = $h AND guest_user_id = $g
                ORDER BY CASE kind WHEN 'auto' THEN 0 ELSE 1 END, updated_utc DESC
                """;
            cmd.Parameters.AddWithValue("$h", hostUserId);
            cmd.Parameters.AddWithValue("$g", guestUserId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new
                {
                    matchId = reader.GetString(0),
                    kind = reader.GetString(1),
                    name = reader.GetString(2),
                    savedUtc = reader.GetString(3)
                });
            }
        }
        return rows.ToArray();
    }

    public bool TryLoad(int hostUserId, int guestUserId, string? matchId, string? kind, string? name, out string blob, out string hostSecret, out string guestSecret, out string error)
    {
        blob = "";
        hostSecret = "";
        guestSecret = "";
        error = "";
        matchId = (matchId ?? "").Trim();
        kind = (kind ?? "").Trim().ToLowerInvariant();
        if (kind == "auto")
            name = "";
        else
            name = LobbySession.ClipName(name, SaveNameMax);
        if (matchId.Length == 0 || kind is not ("auto" or "manual"))
        {
            error = "save not found";
            return false;
        }
        if (hostUserId <= 0 || guestUserId <= 0)
        {
            error = "only an account match can be loaded";
            return false;
        }

        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT blob FROM match_saves
                WHERE match_id = $id AND kind = $kind AND name = $name
                  AND host_user_id = $h AND guest_user_id = $g
                """;
            cmd.Parameters.AddWithValue("$id", matchId);
            cmd.Parameters.AddWithValue("$kind", kind);
            cmd.Parameters.AddWithValue("$name", name ?? "");
            cmd.Parameters.AddWithValue("$h", hostUserId);
            cmd.Parameters.AddWithValue("$g", guestUserId);
            var found = cmd.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(found))
            {
                error = "no such save";
                return false;
            }

            using var match = _db.CreateCommand();
            match.CommandText = """
                SELECT host_secret, guest_secret, host_user_id, guest_user_id
                FROM matches WHERE match_id = $id
                """;
            match.Parameters.AddWithValue("$id", matchId);
            using var reader = match.ExecuteReader();
            if (!reader.Read()
                || reader.GetInt32(2) != hostUserId
                || reader.GetInt32(3) != guestUserId)
            {
                error = "not your match";
                return false;
            }
            hostSecret = reader.GetString(0);
            guestSecret = reader.GetString(1);
            blob = found;
            return true;
        }
    }

    public void Dispose() => _db.Dispose();

    private void DeleteAutosave(string matchId)
    {
        using var drop = _db.CreateCommand();
        drop.CommandText = "DELETE FROM match_saves WHERE match_id = $id AND kind = 'auto'";
        drop.Parameters.AddWithValue("$id", matchId);
        drop.ExecuteNonQuery();
    }

    private void GrantStarter(int userId, SqliteTransaction tx)
    {
        foreach (var card in _starter.Pool)
        {
            using var cmd = _db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO account_cards (user_id, card_id, quantity) VALUES ($user, $card, $qty)";
            cmd.Parameters.AddWithValue("$user", userId);
            cmd.Parameters.AddWithValue("$card", card.CardId);
            cmd.Parameters.AddWithValue("$qty", card.Quantity);
            cmd.ExecuteNonQuery();
        }

        using var deck = _db.CreateCommand();
        deck.Transaction = tx;
        deck.CommandText = """
            INSERT INTO decks (user_id, name, card_ids) VALUES ($user, $name, $cards)
            ON CONFLICT(user_id, name) DO UPDATE SET card_ids = excluded.card_ids
            """;
        deck.Parameters.AddWithValue("$user", userId);
        deck.Parameters.AddWithValue("$name", _starter.DeckName);
        deck.Parameters.AddWithValue("$cards", _starter.CanonicalCards);
        deck.ExecuteNonQuery();
    }

    public bool TryMatchDeck(int userId, string canonical, out string error)
    {
        error = "";
        if (userId <= 0)
        {
            error = "login required";
            return false;
        }
        lock (_gate)
        {
            using var doc = JsonDocument.Parse(canonical);
            var need = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                        continue;
                    var id = (item.GetString() ?? "").Trim();
                    if (id.Length == 0)
                        continue;
                    need[id] = need.TryGetValue(id, out var n) ? n + 1 : 1;
                }
            }

            foreach (var pair in need)
            {
                using var cmd = _db.CreateCommand();
                cmd.CommandText = "SELECT quantity FROM account_cards WHERE user_id = $user AND card_id = $card";
                cmd.Parameters.AddWithValue("$user", userId);
                cmd.Parameters.AddWithValue("$card", pair.Key);
                var owned = cmd.ExecuteScalar();
                var qty = owned == null || owned is DBNull ? 0 : Convert.ToInt32(owned);
                if (pair.Value > qty)
                {
                    var name = CardNameFromId(pair.Key);
                    error = qty <= 0
                        ? name + " is not in the account pool"
                        : "too many copies of " + name;
                    return false;
                }
            }
        }
        return true;
    }

    private static string CardNameFromId(string cardId)
    {
        var slash = cardId.LastIndexOf('/');
        if (slash < 0 || slash >= cardId.Length - 1)
            return cardId;
        var name = cardId[(slash + 1)..].Trim();
        return name.Length == 0 ? cardId : name;
    }

    private bool FitsPool(int userId, string canonical, out string error)
    {
        error = "";
        using var doc = JsonDocument.Parse(canonical);
        var need = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var item in prop.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var id = (item.GetString() ?? "").Trim();
                if (id.Length == 0)
                    continue;
                need[id] = need.TryGetValue(id, out var n) ? n + 1 : 1;
            }
        }

        foreach (var pair in need)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT quantity FROM account_cards WHERE user_id = $user AND card_id = $card";
            cmd.Parameters.AddWithValue("$user", userId);
            cmd.Parameters.AddWithValue("$card", pair.Key);
            var owned = cmd.ExecuteScalar();
            var qty = owned == null || owned is DBNull ? 0 : Convert.ToInt32(owned);
            if (pair.Value > qty)
            {
                error = "card not in account pool";
                return false;
            }
        }
        return true;
    }

    private int ReadLatinum(int userId, SqliteTransaction? tx = null)
    {
        using var cmd = _db.CreateCommand();
        if (tx != null)
            cmd.Transaction = tx;
        cmd.CommandText = "SELECT latinum FROM users WHERE id = $user";
        cmd.Parameters.AddWithValue("$user", userId);
        var value = cmd.ExecuteScalar();
        return value == null || value is DBNull ? 0 : Convert.ToInt32(value);
    }

    private void AddOwnedCard(int userId, string cardId, SqliteTransaction tx)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO account_cards (user_id, card_id, quantity) VALUES ($user, $card, 1)
            ON CONFLICT(user_id, card_id) DO UPDATE SET quantity = quantity + 1
            """;
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$card", cardId);
        cmd.ExecuteNonQuery();
    }

    private static void AddLatinum(SqliteConnection db, SqliteTransaction tx, int userId, int amount)
    {
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE users SET latinum = latinum + $amount WHERE id = $user";
        cmd.Parameters.AddWithValue("$amount", amount);
        cmd.Parameters.AddWithValue("$user", userId);
        if (cmd.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("latinum account missing");
    }

    private void EnsureLatinumColumn()
    {
        if (!HasColumn("users", "latinum"))
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "ALTER TABLE users ADD COLUMN latinum INTEGER NOT NULL DEFAULT 0;";
            cmd.ExecuteNonQuery();
        }
    }    private void EnsureMatchAccountColumns()
    {
        if (!HasColumn("matches", "host_user_id"))
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "ALTER TABLE matches ADD COLUMN host_user_id INTEGER NOT NULL DEFAULT 0;";
            cmd.ExecuteNonQuery();
        }
        if (!HasColumn("matches", "guest_user_id"))
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "ALTER TABLE matches ADD COLUMN guest_user_id INTEGER NOT NULL DEFAULT 0;";
            cmd.ExecuteNonQuery();
        }
    }

    private bool HasColumn(string table, string column)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT name FROM pragma_table_info('" + table + "')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(0), column, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static bool SaveJsonOk(string blob)
    {
        try
        {
            using var doc = JsonDocument.Parse(blob);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }


    private void InsertSession(int userId, string token, SqliteTransaction? tx = null)
    {
        using var cmd = _db.CreateCommand();
        if (tx != null)
            cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO sessions (token, user_id, created_utc) VALUES ($token, $user, $utc)";
        cmd.Parameters.AddWithValue("$token", token);
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$utc", Now());
        cmd.ExecuteNonQuery();
    }

    private static bool TryName(string? raw, out string clean, out string error)
    {
        clean = "";
        error = "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "name required";
            return false;
        }
        var trimmed = raw.Trim();
        if (trimmed.Length > NameMax)
        {
            error = "display name is max 24";
            return false;
        }
        clean = LobbySession.ClipName(trimmed, NameMax);
        if (!string.Equals(clean, trimmed, StringComparison.Ordinal) || clean.Length == 0)
        {
            error = "display name is max 24";
            return false;
        }
        return true;
    }

    private static bool TryPassword(string? password, out string error)
    {
        error = "";
        if (string.IsNullOrEmpty(password) || password.Length < PasswordMin)
        {
            error = "password must be at least 8 characters";
            return false;
        }
        if (password.Length > PasswordMax)
        {
            error = "password is too long";
            return false;
        }
        return true;
    }

    private static bool CardIdOk(string id)
    {
        if (id.Length is < 3 or > 180)
            return false;
        foreach (var ch in id)
        {
            if (char.IsControl(ch))
                return false;
        }
        var first = id.IndexOf('/');
        if (first <= 0)
            return false;
        var second = id.IndexOf('/', first + 1);
        if (second < 0 || second >= id.Length - 1)
            return false;
        return true;
    }

    private static object Err(string message) => new { type = "error", message };

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    private static string NewToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}

readonly record struct AccountUser(int Id, string Name);

readonly record struct FrozenMatch(string MatchId, string HostSecret, string GuestSecret);

readonly record struct ReportVote(string Winner, bool RaiseTheStakes);
