using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

sealed partial class AuthStore
{
    public object OfferTrade(string? token, string? toName, JsonElement root)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        if (!TryName(toName, out var clean, out var error))
            return Err(error);
        if (!TryTradeLines(root, "offerCards", out var offer, out error))
            return Err(error);
        if (!TryTradeLines(root, "requestCards", out var request, out error))
            return Err(error);
        if (offer.Count == 0 && request.Count == 0)
            return Err("name a card");

        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            if (!TryUserByName(clean, tx, out var toId, out var storedName))
            {
                tx.Rollback();
                return Err("no such account");
            }
            if (toId == user.Id)
            {
                tx.Rollback();
                return Err("cannot trade with yourself");
            }
            if (InLiveMatch(user.Id, tx) || InLiveMatch(toId, tx))
            {
                tx.Rollback();
                return Err("account is currently in a match");
            }
            if (!TradeFits(user.Id, offer, request, tx, out error)
                || !TradeFits(toId, request, offer, tx, out error))
            {
                tx.Rollback();
                return Err(error);
            }

            using var pending = _db.CreateCommand();
            pending.Transaction = tx;
            pending.CommandText = "SELECT COUNT(*) FROM trade_offers WHERE from_user = $user";
            pending.Parameters.AddWithValue("$user", user.Id);
            if (Convert.ToInt32(pending.ExecuteScalar()) >= 10)
            {
                tx.Rollback();
                return Err("too many open offers");
            }

            using var insert = _db.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                INSERT INTO trade_offers (from_user, to_user, offer_json, request_json, created_utc)
                VALUES ($from, $to, $offer, $request, $utc)
                """;
            insert.Parameters.AddWithValue("$from", user.Id);
            insert.Parameters.AddWithValue("$to", toId);
            insert.Parameters.AddWithValue("$offer", SerializeLines(offer));
            insert.Parameters.AddWithValue("$request", SerializeLines(request));
            insert.Parameters.AddWithValue("$utc", Now());
            insert.ExecuteNonQuery();
            using var idCmd = _db.CreateCommand();
            idCmd.Transaction = tx;
            idCmd.CommandText = "SELECT last_insert_rowid()";
            var offerId = Convert.ToInt64(idCmd.ExecuteScalar());
            tx.Commit();
            return new { type = "tradeOffered", offerId, to = storedName };
        }
    }

    public object ListTrades(string? token)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        var rows = new List<object>();
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT o.id, fu.name, tu.name, o.from_user, o.to_user, o.offer_json, o.request_json
                FROM trade_offers o
                JOIN users fu ON fu.id = o.from_user
                JOIN users tu ON tu.id = o.to_user
                WHERE o.from_user = $user OR o.to_user = $user
                ORDER BY o.id
                """;
            cmd.Parameters.AddWithValue("$user", user.Id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var toId = reader.GetInt32(4);
                rows.Add(new
                {
                    offerId = reader.GetInt64(0),
                    fromName = reader.GetString(1),
                    toName = reader.GetString(2),
                    incoming = toId == user.Id,
                    offerCards = reader.GetString(5),
                    requestCards = reader.GetString(6)
                });
            }
        }
        return new { type = "tradeList", offers = rows };
    }

    public object AcceptTrade(string? token, string? offerIdText)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        if (!long.TryParse((offerIdText ?? "").Trim(), out var offerId) || offerId <= 0)
            return Err("offer id required");

        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            if (!TryReadOffer(offerId, tx, out var fromId, out var toId, out var offer, out var request, out var error))
            {
                tx.Rollback();
                return Err(error);
            }
            if (toId != user.Id)
            {
                tx.Rollback();
                return Err("not your trade");
            }
            if (InLiveMatch(fromId, tx) || InLiveMatch(toId, tx))
            {
                tx.Rollback();
                return Err("account is currently in a match");
            }
            if (!TradeFits(fromId, offer, request, tx, out error)
                || !TradeFits(toId, request, offer, tx, out error))
            {
                tx.Rollback();
                return Err(error);
            }
            if (!MoveLines(fromId, toId, offer, tx) || !MoveLines(toId, fromId, request, tx))
            {
                tx.Rollback();
                return Err("card not in account pool");
            }
            using var drop = _db.CreateCommand();
            drop.Transaction = tx;
            drop.CommandText = "DELETE FROM trade_offers WHERE id = $id";
            drop.Parameters.AddWithValue("$id", offerId);
            drop.ExecuteNonQuery();
            tx.Commit();
            return new { type = "tradeDone", offerId };
        }
    }

    public object DeclineTrade(string? token, string? offerIdText)
    {
        if (!TrySession(token, out var user))
            return Err("login required");
        if (!long.TryParse((offerIdText ?? "").Trim(), out var offerId) || offerId <= 0)
            return Err("offer id required");
        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            if (!TryReadOffer(offerId, tx, out var fromId, out var toId, out _, out _, out var error))
            {
                tx.Rollback();
                return Err(error);
            }
            if (user.Id != fromId && user.Id != toId)
            {
                tx.Rollback();
                return Err("not your trade");
            }
            using var drop = _db.CreateCommand();
            drop.Transaction = tx;
            drop.CommandText = "DELETE FROM trade_offers WHERE id = $id";
            drop.Parameters.AddWithValue("$id", offerId);
            drop.ExecuteNonQuery();
            tx.Commit();
            return new { type = "tradeDeclined", offerId };
        }
    }

    // One copy, chosen here. The report does not name a card.
    // A saved deck that would then list more copies than the pool loses the extra copy.
    private string? MoveStakeCard(int loserId, int winnerId, string loserCards, SqliteTransaction tx)
    {
        var copies = FlattenCopies(loserCards);
        if (copies.Count == 0)
            return null;
        var cardId = copies[RandomNumberGenerator.GetInt32(copies.Count)];
        if (!TakeOwned(loserId, cardId, 1, tx))
            return null;
        GiveOwned(winnerId, cardId, 1, tx);
        TrimDecksToPool(loserId, cardId, tx);
        return cardId;
    }

    private bool TryReadOffer(
        long offerId,
        SqliteTransaction tx,
        out int fromId,
        out int toId,
        out Dictionary<string, int> offer,
        out Dictionary<string, int> request,
        out string error)
    {
        fromId = 0;
        toId = 0;
        offer = new Dictionary<string, int>(StringComparer.Ordinal);
        request = new Dictionary<string, int>(StringComparer.Ordinal);
        error = "";
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT from_user, to_user, offer_json, request_json
            FROM trade_offers WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$id", offerId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            error = "no such offer";
            return false;
        }
        fromId = reader.GetInt32(0);
        toId = reader.GetInt32(1);
        var offerJson = reader.GetString(2);
        var requestJson = reader.GetString(3);
        reader.Close();
        if (!ParseStoredLines(offerJson, out offer) || !ParseStoredLines(requestJson, out request))
        {
            error = "no such offer";
            return false;
        }
        return true;
    }

    private bool TryUserByName(string name, SqliteTransaction tx, out int id, out string storedName)
    {
        id = 0;
        storedName = "";
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT id, name FROM users WHERE name = $name";
        cmd.Parameters.AddWithValue("$name", name);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return false;
        id = reader.GetInt32(0);
        storedName = reader.GetString(1);
        return true;
    }

    private bool InLiveMatch(int userId, SqliteTransaction tx)
    {
        if (userId <= 0)
            return false;
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT 1 FROM matches m
            WHERE (m.host_user_id = $user OR m.guest_user_id = $user)
              AND NOT EXISTS (SELECT 1 FROM match_history h WHERE h.match_id = m.match_id)
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$user", userId);
        return cmd.ExecuteScalar() != null;
    }

    private bool TradeFits(
        int userId,
        Dictionary<string, int> give,
        Dictionary<string, int> get,
        SqliteTransaction tx,
        out string error)
    {
        error = "";
        var pool = LoadPool(userId, tx);
        foreach (var pair in give)
        {
            pool.TryGetValue(pair.Key, out var qty);
            if (pair.Value > qty)
            {
                error = "card not in account pool";
                return false;
            }
            pool[pair.Key] = qty - pair.Value;
        }
        foreach (var pair in get)
        {
            pool.TryGetValue(pair.Key, out var qty);
            pool[pair.Key] = qty + pair.Value;
        }
        return DecksFitPool(userId, pool, tx, out error);
    }

    private bool DecksFitPool(int userId, Dictionary<string, int> pool, SqliteTransaction tx, out string error)
    {
        error = "";
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT name, card_ids FROM decks WHERE user_id = $user";
        cmd.Parameters.AddWithValue("$user", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var counts = CountCanonical(reader.GetString(1));
            foreach (var pair in counts)
            {
                pool.TryGetValue(pair.Key, out var qty);
                if (pair.Value > qty)
                {
                    error = "trade would break a saved deck";
                    return false;
                }
            }
            _ = name;
        }
        return true;
    }

    private Dictionary<string, int> LoadPool(int userId, SqliteTransaction tx)
    {
        var pool = new Dictionary<string, int>(StringComparer.Ordinal);
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT card_id, quantity FROM account_cards WHERE user_id = $user";
        cmd.Parameters.AddWithValue("$user", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            pool[reader.GetString(0)] = reader.GetInt32(1);
        return pool;
    }

    private static Dictionary<string, int> CountCanonical(string canonical)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(canonical);
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
                    counts[id] = counts.TryGetValue(id, out var n) ? n + 1 : 1;
                }
            }
        }
        catch
        {
            // A deck that cannot be read is left for the caller to reject via an empty fit.
        }
        return counts;
    }

    private bool MoveLines(int fromId, int toId, Dictionary<string, int> lines, SqliteTransaction tx)
    {
        foreach (var pair in lines)
        {
            if (!TakeOwned(fromId, pair.Key, pair.Value, tx))
                return false;
            GiveOwned(toId, pair.Key, pair.Value, tx);
        }
        return true;
    }

    private bool TakeOwned(int userId, string cardId, int qty, SqliteTransaction tx)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE account_cards
            SET quantity = quantity - $qty
            WHERE user_id = $user AND card_id = $card AND quantity >= $qty
            """;
        cmd.Parameters.AddWithValue("$qty", qty);
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$card", cardId);
        if (cmd.ExecuteNonQuery() != 1)
            return false;
        using var drop = _db.CreateCommand();
        drop.Transaction = tx;
        drop.CommandText = """
            DELETE FROM account_cards
            WHERE user_id = $user AND card_id = $card AND quantity <= 0
            """;
        drop.Parameters.AddWithValue("$user", userId);
        drop.Parameters.AddWithValue("$card", cardId);
        drop.ExecuteNonQuery();
        return true;
    }

    private void GiveOwned(int userId, string cardId, int qty, SqliteTransaction tx)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO account_cards (user_id, card_id, quantity) VALUES ($user, $card, $qty)
            ON CONFLICT(user_id, card_id) DO UPDATE SET quantity = quantity + excluded.quantity
            """;
        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$card", cardId);
        cmd.Parameters.AddWithValue("$qty", qty);
        cmd.ExecuteNonQuery();
    }

    private void TrimDecksToPool(int userId, string cardId, SqliteTransaction tx)
    {
        var owned = 0;
        using (var qty = _db.CreateCommand())
        {
            qty.Transaction = tx;
            qty.CommandText = "SELECT quantity FROM account_cards WHERE user_id = $user AND card_id = $card";
            qty.Parameters.AddWithValue("$user", userId);
            qty.Parameters.AddWithValue("$card", cardId);
            var value = qty.ExecuteScalar();
            if (value != null && value is not DBNull)
                owned = Convert.ToInt32(value);
        }

        var rows = new List<(string Name, string Cards)>();
        using (var cmd = _db.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT name, card_ids FROM decks WHERE user_id = $user";
            cmd.Parameters.AddWithValue("$user", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                rows.Add((reader.GetString(0), reader.GetString(1)));
        }

        foreach (var row in rows)
        {
            if (!TrimCanonical(row.Cards, cardId, owned, out var updated))
                continue;
            using var upd = _db.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE decks SET card_ids = $cards WHERE user_id = $user AND name = $name";
            upd.Parameters.AddWithValue("$cards", updated);
            upd.Parameters.AddWithValue("$user", userId);
            upd.Parameters.AddWithValue("$name", row.Name);
            upd.ExecuteNonQuery();
        }
    }

    private static bool TrimCanonical(string canonical, string cardId, int owned, out string updated)
    {
        updated = canonical;
        Dictionary<string, List<string>> lists;
        try
        {
            using var doc = JsonDocument.Parse(canonical);
            lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var section in Sections)
                lists[section] = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!lists.ContainsKey(prop.Name) || prop.Value.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                        continue;
                    var id = (item.GetString() ?? "").Trim();
                    if (id.Length > 0)
                        lists[prop.Name].Add(id);
                }
            }
        }
        catch
        {
            return false;
        }

        var have = 0;
        foreach (var list in lists.Values)
        {
            foreach (var id in list)
            {
                if (string.Equals(id, cardId, StringComparison.Ordinal))
                    have++;
            }
        }
        if (have <= owned)
            return false;

        var extra = have - owned;
        foreach (var section in Sections)
        {
            var list = lists[section];
            for (var i = list.Count - 1; i >= 0 && extra > 0; i--)
            {
                if (!string.Equals(list[i], cardId, StringComparison.Ordinal))
                    continue;
                list.RemoveAt(i);
                extra--;
            }
        }
        updated = JsonSerializer.Serialize(lists);
        return true;
    }

    private static List<string> FlattenCopies(string canonical)
    {
        var list = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(canonical);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                        continue;
                    var id = (item.GetString() ?? "").Trim();
                    if (id.Length > 0)
                        list.Add(id);
                }
            }
        }
        catch
        {
            return list;
        }
        return list;
    }

    private static bool TryTradeLines(JsonElement root, string name, out Dictionary<string, int> lines, out string error)
    {
        lines = new Dictionary<string, int>(StringComparer.Ordinal);
        error = "";
        if (!root.TryGetProperty(name, out var el)
            || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;
        if (el.ValueKind != JsonValueKind.Array)
        {
            error = name + " must be a list";
            return false;
        }
        var total = 0;
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                error = name + " entries need cardId and quantity";
                return false;
            }
            var id = item.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? (idEl.GetString() ?? "").Trim()
                : "";
            if (!CardIdOk(id))
            {
                error = "card id must be SetFolder/ReleaseRaw/Name";
                return false;
            }
            var qty = 1;
            if (item.TryGetProperty("quantity", out var qtyEl))
            {
                if (!qtyEl.TryGetInt32(out qty) || qty < 1 || qty > 99)
                {
                    error = "quantity must be 1 to 99";
                    return false;
                }
            }
            var next = lines.TryGetValue(id, out var have) ? have + qty : qty;
            if (next > 99)
            {
                error = "quantity must be 1 to 99";
                return false;
            }
            lines[id] = next;
            total += qty;
            if (total > MaxCards)
            {
                error = "trade is too large";
                return false;
            }
        }
        return true;
    }

    private static string SerializeLines(Dictionary<string, int> lines)
    {
        var keys = new List<string>(lines.Keys);
        keys.Sort(StringComparer.Ordinal);
        var rows = new List<object>(keys.Count);
        foreach (var key in keys)
            rows.Add(new { cardId = key, quantity = lines[key] });
        return JsonSerializer.Serialize(rows);
    }


    // Both offers move in one transaction, or neither does. No latinum.
    public bool TryCommitAccountTrade(
        int fromId,
        int toId,
        IReadOnlyList<string> fromGives,
        IReadOnlyList<string> toGives,
        out string error)
    {
        error = "";
        if (fromId <= 0 || toId <= 0 || fromId == toId)
        {
            error = "no such account";
            return false;
        }
        if (!TryCountCopies(fromGives, out var give, out error) || !TryCountCopies(toGives, out var get, out error))
            return false;
        if (give.Count == 0 && get.Count == 0)
        {
            error = "name a card";
            return false;
        }

        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            if (!TradeFits(fromId, give, get, tx, out error) || !TradeFits(toId, get, give, tx, out error))
            {
                tx.Rollback();
                return false;
            }
            if (!MoveLines(fromId, toId, give, tx) || !MoveLines(toId, fromId, get, tx))
            {
                tx.Rollback();
                error = "card not in account pool";
                return false;
            }
            tx.Commit();
            return true;
        }
    }

    private static bool TryCountCopies(IReadOnlyList<string>? ids, out Dictionary<string, int> lines, out string error)
    {
        lines = new Dictionary<string, int>(StringComparer.Ordinal);
        error = "";
        if (ids == null)
            return true;
        if (ids.Count > 4)
        {
            error = "trade is too large";
            return false;
        }
        foreach (var raw in ids)
        {
            var id = (raw ?? "").Trim();
            if (id.Length == 0)
                continue;
            if (!CardIdOk(id))
            {
                error = "card id must be SetFolder/ReleaseRaw/Name";
                return false;
            }
            lines[id] = lines.TryGetValue(id, out var have) ? have + 1 : 1;
        }
        return true;
    }

    private static bool ParseStoredLines(string json, out Dictionary<string, int> lines)
    {
        lines = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return false;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return false;
                var id = item.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                    ? (idEl.GetString() ?? "").Trim()
                    : "";
                if (!CardIdOk(id) || !item.TryGetProperty("quantity", out var qtyEl) || !qtyEl.TryGetInt32(out var qty) || qty < 1)
                    return false;
                lines[id] = qty;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
