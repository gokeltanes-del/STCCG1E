using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

// Accounts, server decks (cardId lists only), frozen match decks, and match history.
// History is inserted only when both seats report the same winner.
sealed class AuthStore : IDisposable
{
    public const int NameMax = 24;
    public const int PasswordMin = 8;
    public const int PasswordMax = 72;
    public const int DeckNameMax = 80;
    public const int MaxCards = 500;

    private static readonly string[] Sections =
    {
        "battle_bridge", "draw", "q_continuum", "qs_tent", "seed", "side", "site_pile", "tribble"
    };

    private readonly SqliteConnection _db;
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, string>> _pending = new(StringComparer.Ordinal);

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
                created_utc TEXT NOT NULL
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
            """;
        cmd.ExecuteNonQuery();
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
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
            using var insert = _db.CreateCommand();
            insert.CommandText = "INSERT INTO users (name, password_hash, created_utc) VALUES ($name, $hash, $utc)";
            insert.Parameters.AddWithValue("$name", clean);
            insert.Parameters.AddWithValue("$hash", hash);
            insert.Parameters.AddWithValue("$utc", Now());
            insert.ExecuteNonQuery();
            using var idCmd = _db.CreateCommand();
            idCmd.CommandText = "SELECT id FROM users WHERE name = $name";
            idCmd.Parameters.AddWithValue("$name", clean);
            var id = Convert.ToInt32(idCmd.ExecuteScalar());
            var token = NewToken();
            InsertSession(id, token);
            return new { type = "auth", token, name = clean, mode = "account" };
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
            cmd.CommandText = "SELECT id, password_hash, name FROM users WHERE name = $name";
            cmd.Parameters.AddWithValue("$name", clean);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Err("wrong password");
            var id = reader.GetInt32(0);
            var hash = reader.GetString(1);
            var storedName = reader.GetString(2);
            bool ok;
            try { ok = BCrypt.Net.BCrypt.Verify(password, hash); }
            catch { ok = false; }
            if (!ok)
                return Err("wrong password");
            var token = NewToken();
            InsertSession(id, token);
            return new { type = "auth", token, name = storedName, mode = "account" };
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

    public FrozenMatch Freeze(string mode, string hostName, string guestName, string hostDeck, string hostCards, string guestDeck, string guestCards)
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
                    host_deck_name, guest_deck_name, host_cards, guest_cards, created_utc)
                VALUES (
                    $id, $mode, $host, $guest, $hs, $gs, $hd, $gd, $hc, $gc, $utc)
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
            cmd.ExecuteNonQuery();
        }
        return new FrozenMatch(matchId, hostSecret, guestSecret);
    }

    public object Report(string? matchId, string? secret, string? winnerSeat)
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
                SELECT mode, host_name, guest_name, host_secret, guest_secret
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
                return new { type = "report", written = true, message = "already written" };

            if (!_pending.TryGetValue(matchId, out var votes))
            {
                votes = new Dictionary<string, string>(StringComparer.Ordinal);
                _pending[matchId] = votes;
            }
            votes[seat] = winnerSeat;
            if (votes.Count < 2)
                return new { type = "report", written = false, message = "waiting for the other player" };

            var hostVote = votes["host"];
            var guestVote = votes["guest"];
            _pending.Remove(matchId);
            if (!string.Equals(hostVote, guestVote, StringComparison.Ordinal))
                return new { type = "report", written = false, message = "reports disagree" };

            var winnerName = hostVote == "host" ? hostName : guestName;
            using var insert = _db.CreateCommand();
            insert.CommandText = """
                INSERT INTO match_history (match_id, mode, host_name, guest_name, winner_seat, winner_name, written_utc)
                VALUES ($id, $mode, $host, $guest, $seat, $name, $utc)
                """;
            insert.Parameters.AddWithValue("$id", matchId);
            insert.Parameters.AddWithValue("$mode", mode);
            insert.Parameters.AddWithValue("$host", hostName);
            insert.Parameters.AddWithValue("$guest", guestName);
            insert.Parameters.AddWithValue("$seat", hostVote);
            insert.Parameters.AddWithValue("$name", winnerName);
            insert.Parameters.AddWithValue("$utc", Now());
            insert.ExecuteNonQuery();
            return new { type = "report", written = true, message = "written" };
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

    public void Dispose() => _db.Dispose();

    private void InsertSession(int userId, string token)
    {
        using var cmd = _db.CreateCommand();
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
