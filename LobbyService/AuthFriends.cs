using Microsoft.Data.Sqlite;

sealed partial class AuthStore
{
    public bool TryUser(string name, out int id, out string storedName)
    {
        id = 0;
        storedName = "";
        if (!TryName(name, out var clean, out _))
            return false;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM users WHERE name = $name";
            cmd.Parameters.AddWithValue("$name", clean);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;
            id = reader.GetInt32(0);
            storedName = reader.GetString(1);
            return id > 0;
        }
    }

    public bool AreFriends(int left, int right)
    {
        if (!Pair(left, right, out var lo, out var hi))
            return false;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM friendships WHERE user_a = $a AND user_b = $b";
            cmd.Parameters.AddWithValue("$a", lo);
            cmd.Parameters.AddWithValue("$b", hi);
            return cmd.ExecuteScalar() != null;
        }
    }

    public void AddFriendship(int left, int right)
    {
        if (!Pair(left, right, out var lo, out var hi))
            return;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO friendships (user_a, user_b, created_utc) VALUES ($a, $b, $utc)";
            cmd.Parameters.AddWithValue("$a", lo);
            cmd.Parameters.AddWithValue("$b", hi);
            cmd.Parameters.AddWithValue("$utc", Now());
            cmd.ExecuteNonQuery();
        }
    }

    public bool RemoveFriendship(int left, int right)
    {
        if (!Pair(left, right, out var lo, out var hi))
            return false;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "DELETE FROM friendships WHERE user_a = $a AND user_b = $b";
            cmd.Parameters.AddWithValue("$a", lo);
            cmd.Parameters.AddWithValue("$b", hi);
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    public List<(int Id, string Name)> FriendRows(int userId)
    {
        var rows = new List<(int Id, string Name)>();
        if (userId <= 0)
            return rows;
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                SELECT u.id, u.name
                FROM friendships f
                JOIN users u ON u.id = CASE WHEN f.user_a = $id THEN f.user_b ELSE f.user_a END
                WHERE f.user_a = $id OR f.user_b = $id
                ORDER BY u.name COLLATE NOCASE
                """;
            cmd.Parameters.AddWithValue("$id", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                rows.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return rows;
    }

    private static bool Pair(int left, int right, out int lo, out int hi)
    {
        lo = Math.Min(left, right);
        hi = Math.Max(left, right);
        return lo > 0 && lo < hi;
    }
}
