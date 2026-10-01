using System.Security.Cryptography;
using System.Text.Json;

// Premiere booster. Rarity is the last token of rarity_info in Data/Sets/PR/cards.json.
// A missing rarity stops the service. Card names never come from the client.
sealed class PremiereCatalog
{
    public const int Price = 50;
    public const string PackType = "premiere_booster";
    public const int RareCount = 1;
    public const int UncommonCount = 3;
    public const int CommonCount = 11;

    private readonly string[] _rares;
    private readonly string[] _uncommons;
    private readonly string[] _commons;

    private PremiereCatalog(string[] rares, string[] uncommons, string[] commons)
    {
        _rares = rares;
        _uncommons = uncommons;
        _commons = commons;
    }

    public int RarePool => _rares.Length;
    public int UncommonPool => _uncommons.Length;
    public int CommonPool => _commons.Length;

    public static PremiereCatalog Load(string startDirectory)
    {
        var setsRoot = FindUp(startDirectory, static dir =>
        {
            var nested = Path.Combine(dir, "StarTrekCCG", "Data", "Sets");
            if (File.Exists(Path.Combine(nested, "PR", "cards.json")))
                return nested;
            var flat = Path.Combine(dir, "Data", "Sets");
            return File.Exists(Path.Combine(flat, "PR", "cards.json")) ? flat : null;
        }) ?? throw new InvalidOperationException(
            "Card catalog was not found. Expected Data/Sets/PR/cards.json.");

        var path = Path.Combine(setsRoot, "PR", "cards.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("cards.json is not a list: " + path);

        var rares = new List<string>();
        var uncommons = new List<string>();
        var commons = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in doc.RootElement.EnumerateArray())
        {
            if (card.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Premiere cards.json has an entry that is not an object: " + path);

            var name = ReadText(card, "name");
            if (name.Length == 0)
                throw new InvalidOperationException("Premiere card has no name in " + path);

            if (!card.TryGetProperty("rarity_info", out var rarEl) || rarEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("Premiere card has no rarity_info: " + name);
            var rarity = (rarEl.GetString() ?? "").Trim();
            if (rarity.Length == 0)
                throw new InvalidOperationException("Premiere card has no rarity_info: " + name);

            var set = ReadText(card, "set_folder");
            var release = ReadText(card, "release_raw");
            if (set.Length == 0 || release.Length == 0)
                throw new InvalidOperationException("Premiere card has no set_folder or release_raw: " + name);

            var letter = LastToken(rarity);
            List<string> bucket = letter switch
            {
                "R" => rares,
                "U" => uncommons,
                "C" => commons,
                _ => throw new InvalidOperationException(
                    "Premiere card rarity is not R, U, or C: " + name + " (" + rarity + ")")
            };

            var id = set + "/" + release + "/" + name;
            if (!seen.Add(id))
                throw new InvalidOperationException("More than one Premiere card id " + id);
            bucket.Add(id);
        }

        if (rares.Count < RareCount || uncommons.Count < UncommonCount || commons.Count < CommonCount)
            throw new InvalidOperationException(
                "Premiere booster cannot be filled from " + path
                + " (" + rares.Count + " rare, " + uncommons.Count + " uncommon, " + commons.Count + " common).");

        Console.Error.WriteLine(
            "Premiere booster from " + path
            + " (" + rares.Count + " rare, " + uncommons.Count + " uncommon, " + commons.Count + " common). Price " + Price + ".");
        return new PremiereCatalog(rares.ToArray(), uncommons.ToArray(), commons.ToArray());
    }

    public IReadOnlyList<string> Draw()
    {
        var ids = new List<string>(RareCount + UncommonCount + CommonCount);
        ids.AddRange(Take(_rares, RareCount));
        ids.AddRange(Take(_uncommons, UncommonCount));
        ids.AddRange(Take(_commons, CommonCount));
        return ids;
    }

    private static string[] Take(string[] pool, int count)
    {
        if (pool.Length < count)
            throw new InvalidOperationException("Premiere rarity pool is smaller than a booster.");
        var copy = (string[])pool.Clone();
        for (var i = copy.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        var picked = new string[count];
        Array.Copy(copy, picked, count);
        return picked;
    }

    private static string ReadText(JsonElement card, string property)
    {
        if (!card.TryGetProperty(property, out var el) || el.ValueKind != JsonValueKind.String)
            return "";
        return (el.GetString() ?? "").Trim();
    }

    private static string LastToken(string rarity)
    {
        var parts = rarity.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "" : parts[^1];
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
