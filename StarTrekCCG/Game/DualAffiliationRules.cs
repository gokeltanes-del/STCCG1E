using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 6.3.3 Multi-affiliation cards.
/// One mode at a time. Switch between actions. Mode-dependent skills (Rakal / DeSeve).
/// Dual-personnel cards (Sisters of Duras) are a different rule and not handled here.
/// </summary>
public static class DualAffiliationRules
{
    public sealed class ModeProfile
    {
        public required string Affiliation { get; init; }
        public Dictionary<string, int> Skills { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public int IntegrityDelta { get; init; }
        public int CunningDelta { get; init; }
        public int StrengthDelta { get; init; }
        public string? Classification { get; init; }
    }

    private static readonly string[] AffilWords =
    {
        "Federation", "Romulan", "Klingon", "Bajoran", "Cardassian",
        "Ferengi", "Dominion", "Hirogen", "Kazon", "Vidiian",
        "Starfleet", "Vulcan", "Non-Aligned", "Neutral", "Borg"
    };

    public static bool IsMulti(Card? c)
    {
        if (c == null) return false;
        return PrintedModes(c).Count >= 2;
    }

    public static List<string> PrintedModes(Card card)
    {
        var set = ReportingRules.ParseAffiliationTokens(card.Affiliation);
        if (set.Count == 0)
            set = ReportingRules.ParseAffiliationTokens(card.Name);
        return set.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string? CurrentMode(Card card)
    {
        if (!string.IsNullOrWhiteSpace(card.CurrentAffiliation))
            return ReportingRules.NormalizeAffil(card.CurrentAffiliation);
        return null;
    }

    /// <summary>In-play: current mode only. Not yet chosen / in hand: all printed icons.</summary>
    public static HashSet<string> ActiveAffiliations(Card card)
    {
        var mode = CurrentMode(card);
        if (mode != null)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { mode };
        return new HashSet<string>(PrintedModes(card), StringComparer.OrdinalIgnoreCase);
    }

    public static bool HasModeDependentText(Card card)
    {
        string t = card.Text ?? "";
        return AffilWords.Any(w =>
            t.Contains(w + ":", StringComparison.OrdinalIgnoreCase));
    }

    public static ModeProfile? ProfileFor(Card card, string? mode = null)
    {
        // Glossary: Lore's Fingernail — dual ProfileFor not active as printed while Non.
        if (EventRules.FingernailMakesNon(card)) return null;
        mode ??= CurrentMode(card);
        if (mode == null) return null;
        var all = ParseModeProfiles(card);
        return all.FirstOrDefault(p =>
            string.Equals(p.Affiliation, mode, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ModeProfile> ParseModeProfiles(Card card)
    {
        var list = new List<ModeProfile>();
        string text = card.Text ?? "";
        if (string.IsNullOrWhiteSpace(text) || !HasModeDependentText(card))
            return list;

        var matches = new List<(int Index, string Word, string Token)>();
        foreach (var word in AffilWords)
        {
            var rx = new Regex(@"\b" + Regex.Escape(word) + @"\s*:", RegexOptions.IgnoreCase);
            foreach (Match m in rx.Matches(text))
                matches.Add((m.Index, word, ReportingRules.NormalizeAffil(word)));
        }
        matches = matches.OrderBy(x => x.Index).ToList();
        for (int i = 0; i < matches.Count; i++)
        {
            int start = matches[i].Index + matches[i].Word.Length + 1;
            int end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            if (start < 0) start = 0;
            if (end < start) end = start;
            string slice = text[start..end].Trim().Trim(',', ';', '.');
            list.Add(ParseSlice(matches[i].Token, slice));
        }
        return list;
    }

    private static ModeProfile ParseSlice(string token, string slice)
    {
        int di = 0, dc = 0, ds = 0;
        string work = slice;
        work = ApplyDelta(work, "INTEGRITY", ref di);
        work = ApplyDelta(work, "CUNNING", ref dc);
        work = ApplyDelta(work, "STRENGTH", ref ds);

        var dummy = new Card { Name = token, Type = "Personnel", Text = work, Class = "" };
        var skills = MissionRules.ParsePersonnelSkills(dummy);

        string? cls = null;
        foreach (var key in skills.Keys.ToList())
        {
            if (MissionRules.Classifications.Contains(key))
            {
                cls ??= key;
            }
        }

        return new ModeProfile
        {
            Affiliation = token,
            Skills = skills,
            IntegrityDelta = di,
            CunningDelta = dc,
            StrengthDelta = ds,
            Classification = cls
        };
    }

    private static string ApplyDelta(string text, string stat, ref int delta)
    {
        var rx = new Regex(
            @"\b" + Regex.Escape(stat) + @"\s*([+-])\s*(\d+)",
            RegexOptions.IgnoreCase);
        foreach (Match m in rx.Matches(text))
        {
            int n = int.Parse(m.Groups[2].Value);
            delta += m.Groups[1].Value == "-" ? -n : n;
        }
        return rx.Replace(text, " ");
    }

    public static string DisplayName(string token) => token.ToUpperInvariant() switch
    {
        "FED" => "Federation",
        "ROM" => "Romulan",
        "KLI" => "Klingon",
        "BAJ" => "Bajoran",
        "CARD" => "Cardassian",
        "FER" => "Ferengi",
        "DOM" => "Dominion",
        "NA" => "Non-Aligned",
        "BORG" => "Borg",
        _ => token
    };

    public static bool TrySetMode(Card card, string mode)
    {
        // Glossary: Lore's Fingernail — dual toggle off while inorganic becomes Non.
        if (EventRules.FingernailMakesNon(card)) return false;
        string n = ReportingRules.NormalizeAffil(mode);
        if (!PrintedModes(card).Contains(n)) return false;
        card.CurrentAffiliation = n;
        return true;
    }
}