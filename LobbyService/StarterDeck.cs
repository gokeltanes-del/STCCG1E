using System.Text.Json;

// The one starter copied into a new account. Read from the deck file. Card ids come from the catalog.
sealed class StarterGrant
{
    public StarterGrant(string deckName, string sourcePath, string canonicalCards, IReadOnlyList<StarterCard> pool)
    {
        DeckName = deckName;
        SourcePath = sourcePath;
        CanonicalCards = canonicalCards;
        Pool = pool;
    }

    public string DeckName { get; }
    public string SourcePath { get; }
    public string CanonicalCards { get; }
    public IReadOnlyList<StarterCard> Pool { get; }
}

readonly record struct StarterCard(string CardId, int Quantity);

static class StarterDeck
{
    public const int Latinum = 100;

    private static readonly string[] Sections =
    {
        "battle_bridge", "draw", "q_continuum", "qs_tent", "seed", "side", "site_pile", "tribble"
    };

    public static StarterGrant Load(string startDirectory)
    {
        var deckPath = FindUp(startDirectory, static dir =>
        {
            var preferred = Path.Combine(dir, "StarTrekCCG", "Decks", "Federation Premiere.stdeck");
            if (File.Exists(preferred))
                return preferred;
            var flat = Path.Combine(dir, "Decks", "Federation Premiere.stdeck");
            return File.Exists(flat) ? flat : null;
        }) ?? throw new InvalidOperationException(
            "Premiere starter deck was not found. Expected Decks/Federation Premiere.stdeck.");

        var setsRoot = FindUp(startDirectory, static dir =>
        {
            var nested = Path.Combine(dir, "StarTrekCCG", "Data", "Sets");
            if (File.Exists(Path.Combine(nested, "PR", "cards.json")))
                return nested;
            var flat = Path.Combine(dir, "Data", "Sets");
            return File.Exists(Path.Combine(flat, "PR", "cards.json")) ? flat : null;
        }) ?? throw new InvalidOperationException(
            "Card catalog was not found. Expected Data/Sets/<set>/cards.json.");

        using var deckDoc = JsonDocument.Parse(File.ReadAllText(deckPath));
        var root = deckDoc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Starter deck is not a JSON object: " + deckPath);

        var deckName = "Federation Premiere";
        if (root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
        {
            var raw = (nameEl.GetString() ?? "").Trim();
            if (raw.Length > 0)
                deckName = raw.Length > AuthStore.DeckNameMax ? raw[..AuthStore.DeckNameMax] : raw;
        }

        var indexes = new Dictionary<string, Dictionary<string, (string Set, string Release, string Name)>>(StringComparer.Ordinal);
        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var section in Sections)
            lists[section] = new List<string>();
        var pool = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var section in Sections)
        {
            if (!root.TryGetProperty(section, out var arr) || arr.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var entry in arr.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException("Starter deck entry is not an object in " + section);
                var id = ResolveId(entry, setsRoot, indexes);
                var qty = 1;
                if (entry.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetInt32(out var n) && n > 0)
                    qty = n;
                for (var i = 0; i < qty; i++)
                    lists[section].Add(id);
                pool[id] = pool.TryGetValue(id, out var have) ? have + qty : qty;
            }
        }

        if (pool.Count == 0)
            throw new InvalidOperationException("Starter deck has no cards: " + deckPath);

        var cards = pool
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new StarterCard(pair.Key, pair.Value))
            .ToList();
        var canonical = JsonSerializer.Serialize(lists);
        Console.Error.WriteLine(
            "Starting deck " + deckName + " from " + deckPath + " (" + cards.Sum(c => c.Quantity) + " copies).");
        return new StarterGrant(deckName, deckPath, canonical, cards);
    }

    private static string ResolveId(
        JsonElement entry,
        string setsRoot,
        Dictionary<string, Dictionary<string, (string Set, string Release, string Name)>> indexes)
    {
        if (entry.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String)
        {
            var existing = (idEl.GetString() ?? "").Trim();
            if (IsCardId(existing))
                return existing;
        }

        var name = entry.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? (nameEl.GetString() ?? "").Trim()
            : "";
        var set = entry.TryGetProperty("set", out var setEl) && setEl.ValueKind == JsonValueKind.String
            ? (setEl.GetString() ?? "").Trim()
            : "";
        if (name.Length == 0 || set.Length == 0)
            throw new InvalidOperationException("Starter deck entry needs a name and a set.");

        if (!indexes.TryGetValue(set, out var index))
        {
            index = LoadSet(setsRoot, set);
            indexes[set] = index;
        }

        if (!index.TryGetValue(name, out var hit))
        {
            foreach (var pair in index)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    hit = pair.Value;
                    name = pair.Key;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(hit.Name) || string.IsNullOrEmpty(hit.Release) || string.IsNullOrEmpty(hit.Set))
            throw new InvalidOperationException("Starter deck card is not in the catalog: " + set + " / " + name);

        return hit.Set + "/" + hit.Release + "/" + hit.Name;
    }

    private static Dictionary<string, (string Set, string Release, string Name)> LoadSet(string setsRoot, string setFolder)
    {
        var path = Path.Combine(setsRoot, setFolder, "cards.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("No cards.json for starter set " + setFolder);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("cards.json is not a list: " + path);

        var map = new Dictionary<string, (string Set, string Release, string Name)>(StringComparer.Ordinal);
        foreach (var card in doc.RootElement.EnumerateArray())
        {
            if (card.ValueKind != JsonValueKind.Object)
                continue;
            var name = card.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? (nameEl.GetString() ?? "").Trim()
                : "";
            if (name.Length == 0)
                continue;
            var set = card.TryGetProperty("set_folder", out var setEl) && setEl.ValueKind == JsonValueKind.String
                ? (setEl.GetString() ?? "").Trim()
                : "";
            var release = card.TryGetProperty("release_raw", out var relEl) && relEl.ValueKind == JsonValueKind.String
                ? (relEl.GetString() ?? "").Trim()
                : "";
            if (set.Length == 0)
                set = setFolder;
            if (map.ContainsKey(name))
                throw new InvalidOperationException("More than one catalog card named " + name + " in " + setFolder);
            map[name] = (set, release, name);
        }
        return map;
    }

    private static bool IsCardId(string id)
    {
        var first = id.IndexOf('/');
        if (first <= 0)
            return false;
        var second = id.IndexOf('/', first + 1);
        return second > first && second < id.Length - 1;
    }

    private static string? FindUp(string start, Func<string, string?> probe)
    {
        var dir = new DirectoryInfo(start);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var hit = probe(dir.FullName);
            if (!string.IsNullOrEmpty(hit))
                return hit;
            dir = dir.Parent;
        }
        return null;
    }
}
