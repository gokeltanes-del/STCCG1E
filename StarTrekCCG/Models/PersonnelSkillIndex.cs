using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Extracts classification / skills / staffing / icons from personnel cards for deck-builder filters.
/// Icons are text tags for now; swap to image icons later when the sheet is sliced.
/// </summary>
public static class PersonnelSkillIndex
{
    private static readonly string[] Classifications =
    {
        "OFFICER", "ENGINEER", "SCIENCE", "MEDICAL", "SECURITY", "CIVILIAN", "V.I.P.", "VIP", "ANIMAL"
    };

    private static readonly string[] RegularSkills =
    {
        "Anthropology", "Archaeology", "Astrophysics", "Barbering", "Biology",
        "Computer Skill", "Diplomacy", "Empathy", "Exobiology", "Geology",
        "Greed", "Honor", "Law", "Leadership", "Mindmeld", "Music",
        "Navigation", "Physics", "Programming", "Smuggling", "Stellar Cartography",
        "Transporter Skill", "Treachery", "Youth", "Acquisition", "Anthropology",
        "Resistance", "Intelligence", "Guramba", "Klingon", "Tal Shiar", "Obsidian Order",
        "Section 31", "Orion Syndicate", "FCA", "Mirak"
    };

    private static readonly HashSet<string> StaffIconTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cmd", "Stf", "Staff", "Command"
    };

    private static readonly HashSet<string> ExpansionEraTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "AU", "FC", "DS9", "VOY", "TNG", "TOS", "ENT", "Prem", "1E", "2E", "HA", "Hidden Agenda",
        "SD", "Special Download", "EE", "Excelsior", "Ent-E", "Holo", "Nemesis", "Maquis", "Orb",
        "Barash", "Ketracel", "Q", "Ref", "Delta", "Gamma", "Mirror"
    };

    public static List<(string Category, List<string> Items)> Collect(IEnumerable<Card> personnel)
    {
        var classSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var staffSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var specialIcon = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var regular = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var download = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var expansion = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in personnel)
        {
            foreach (var t in TagsFor(c))
            {
                if (IsClassification(t)) classSet.Add(NormalizeClass(t));
                else if (IsStaff(t)) staffSet.Add(NormalizeIcon(t));
                else if (IsExpansion(t)) expansion.Add(NormalizeIcon(t));
                else if (IsRegularSkill(t)) regular.Add(t);
                else if (t.StartsWith("Download", StringComparison.OrdinalIgnoreCase) || t.Contains("↓") || t.Contains("download", StringComparison.OrdinalIgnoreCase))
                    download.Add(t);
                else if (t.StartsWith("[") || t.Length <= 4)
                    specialIcon.Add(NormalizeIcon(t));
            }
        }

        var list = new List<(string, List<string>)>
        {
            ("Classification", classSet.Select(NormalizeClass).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()),
            ("Staffing ability", staffSet.OrderBy(x => x).ToList()),
            ("Special icons", specialIcon.OrderBy(x => x).ToList()),
            ("Regular skills", regular.OrderBy(x => x).ToList()),
            ("Download", download.OrderBy(x => x).ToList()),
            ("Expansion / era icons", expansion.OrderBy(x => x).ToList()),
        };
        return list;
    }

    public static HashSet<string> TagsFor(Card c)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(c.Class))
            tags.Add(NormalizeClass(c.Class.Trim()));

        if (!string.IsNullOrWhiteSpace(c.Staff))
        {
            foreach (Match m in Regex.Matches(c.Staff, @"\[([^\]]+)\]|Cmd|Stf|Staff|Command"))
                tags.Add(NormalizeIcon(m.Groups[1].Success ? m.Groups[1].Value : m.Value));
        }

        if (!string.IsNullOrWhiteSpace(c.Icons))
        {
            foreach (Match m in Regex.Matches(c.Icons, @"\[([^\]]+)\]"))
                tags.Add(NormalizeIcon(m.Groups[1].Value));
        }

        string text = c.Text ?? "";
        // Classification words in text
        foreach (var cl in Classifications)
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(cl)}\b", RegexOptions.IgnoreCase))
                tags.Add(NormalizeClass(cl));
        }

        // Regular skills (multi-word first)
        foreach (var sk in RegularSkills.OrderByDescending(s => s.Length))
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(sk)}\b", RegexOptions.IgnoreCase))
                tags.Add(sk);
        }

        // Downloads
        if (Regex.IsMatch(text, @"\bdownload\b", RegexOptions.IgnoreCase))
            tags.Add("Download");

        // Icon brackets in text
        foreach (Match m in Regex.Matches(text, @"\[([^\]]+)\]"))
            tags.Add(NormalizeIcon(m.Groups[1].Value));

        return tags;
    }

    private static bool IsClassification(string t)
    {
        string n = NormalizeClass(t);
        return Classifications.Any(c => string.Equals(NormalizeClass(c), n, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRegularSkill(string t) =>
        RegularSkills.Any(s => string.Equals(s, t, StringComparison.OrdinalIgnoreCase));

    private static bool IsStaff(string t)
    {
        string n = NormalizeIcon(t);
        return StaffIconTags.Contains(n) || n is "Cmd" or "Stf";
    }

    private static bool IsExpansion(string t)
    {
        string n = NormalizeIcon(t);
        return ExpansionEraTags.Contains(n);
    }

    private static string NormalizeClass(string t)
    {
        t = t.Trim();
        if (t.Equals("VIP", StringComparison.OrdinalIgnoreCase) || t.Equals("V.I.P.", StringComparison.OrdinalIgnoreCase))
            return "V.I.P.";
        return t.ToUpperInvariant();
    }

    private static string NormalizeIcon(string t) =>
        t.Trim().Trim('[', ']');

    /// <summary>
    /// Skills + classifications mentioned in mission/dilemma gametext (Diplomacy, ENGINEER, …).
    /// Longer names matched first so "Computer Skill" is not split.
    /// </summary>
    public static HashSet<string> ExtractRequirementSkills(string? text)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return found;
        string blob = text;

        var names = RegularSkills
            .Concat(Classifications)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(n => n.Length)
            .ToList();

        foreach (var name in names)
        {
            if (name.Length < 3) continue;
            if (blob.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                found.Add(IsClassification(name) ? NormalizeClass(name) : name);
        }
        return found;
    }
}