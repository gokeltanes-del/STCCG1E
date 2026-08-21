using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium: Treaties (Events) – Affiliations „mix and cooperate“ / compatible.
/// Premiere: Fed/Kli, Fed/Rom, Rom/Kli. Weitere Sets über Namens-/Text-Parser.
/// </summary>
public static class TreatyRules
{
    /// <summary>Ein aktives Treaty: zwei normalisierte Affiliations (FED, KLI, …).</summary>
    public readonly record struct TreatyLink(string AffilA, string AffilB, string SourceName);

    public static bool IsTreatyCard(Card c)
    {
        string n = (c.Name ?? "").Trim();
        if (n.StartsWith("Treaty:", StringComparison.OrdinalIgnoreCase)) return true;
        if (n.Equals("Organian Peace Treaty", StringComparison.OrdinalIgnoreCase)) return true;
        string t = (c.Type ?? "").ToLowerInvariant();
        if (t.Contains("event") && (c.Text ?? "").Contains("treaty", StringComparison.OrdinalIgnoreCase)
            && (c.Text ?? "").Contains("compatible", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// Affiliations-Paar aus Name/Text lesen.
    /// „Treaty: Federation/Klingon“ → FED–KLI
    /// </summary>
    public static TreatyLink? ParseTreaty(Card card)
    {
        if (card == null) return null;
        string name = (card.Name ?? "").Trim();
        string text = card.Text ?? "";

        // Organian: Fed + Kli mit [OS] – Premiere-Sandbox: als Fed/Kli behandeln
        if (name.Equals("Organian Peace Treaty", StringComparison.OrdinalIgnoreCase))
            return new TreatyLink("FED", "KLI", name);

        // Name: Treaty: A/B oder Treaty: A/B/C
        var mName = Regex.Match(name,
            @"^Treaty:\s*(.+)$", RegexOptions.IgnoreCase);
        if (mName.Success)
        {
            var parts = mName.Groups[1].Value
                .Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => ReportingRules.NormalizeAffil(p.Trim()))
                .Where(p => p.Length > 0 && p is not "THE" and not "ALLIANCE" and not "ALPHA" and not "QUADRANT")
                .Distinct()
                .ToList();

            // Normalisiere Langformen
            parts = parts.Select(NormalizeTreatyToken).Where(p => p != null).Cast<string>().Distinct().ToList();

            if (parts.Count >= 2)
            {
                // Mehrere: alle Paare – für Link speichern wir erstes Paar + Extra über Liste
                // Aufrufer nutzt ParseAllLinks
                return new TreatyLink(parts[0], parts[1], name);
            }
        }

        // Text: „Your Federation and Klingon affiliations“
        var mText = Regex.Match(text,
            @"Your\s+([A-Za-z\-]+)\s+and\s+([A-Za-z\-]+)\s+affiliations",
            RegexOptions.IgnoreCase);
        if (mText.Success)
        {
            string a = NormalizeTreatyToken(ReportingRules.NormalizeAffil(mText.Groups[1].Value));
            string b = NormalizeTreatyToken(ReportingRules.NormalizeAffil(mText.Groups[2].Value));
            if (a != null && b != null)
                return new TreatyLink(a, b, name);
        }

        // Text: „Your [Fed] and [Kli] cards are compatible“
        var icons = Regex.Matches(text, @"\[([A-Za-z]+)\]");
        if (icons.Count >= 2 && text.Contains("compatible", StringComparison.OrdinalIgnoreCase))
        {
            var toks = icons.Cast<Match>()
                .Select(m => NormalizeTreatyToken(ReportingRules.NormalizeAffil(m.Groups[1].Value)))
                .Where(t => t != null)
                .Cast<string>()
                .Distinct()
                .ToList();
            if (toks.Count >= 2)
                return new TreatyLink(toks[0], toks[1], name);
        }

        return null;
    }

    /// <summary>Alle Affiliation-Paare eines Treaty-Cards (auch 3-Wege).</summary>
    public static List<TreatyLink> ParseAllLinks(Card card)
    {
        var links = new List<TreatyLink>();
        string name = (card.Name ?? "").Trim();
        var mName = Regex.Match(name, @"^Treaty:\s*(.+)$", RegexOptions.IgnoreCase);
        if (mName.Success)
        {
            var parts = mName.Groups[1].Value
                .Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => NormalizeTreatyToken(ReportingRules.NormalizeAffil(p.Trim())))
                .Where(p => p != null)
                .Cast<string>()
                .Distinct()
                .ToList();
            for (int i = 0; i < parts.Count; i++)
                for (int j = i + 1; j < parts.Count; j++)
                    links.Add(new TreatyLink(parts[i], parts[j], name));
            if (links.Count > 0) return links;
        }

        var one = ParseTreaty(card);
        if (one.HasValue) links.Add(one.Value);
        return links;
    }

    private static string? NormalizeTreatyToken(string t)
    {
        t = (t ?? "").ToUpperInvariant().Trim();
        return t switch
        {
            "FEDERATION" or "FED" => "FED",
            "KLINGON" or "KLI" => "KLI",
            "ROMULAN" or "ROM" => "ROM",
            "BAJORAN" or "BAJ" => "BAJ",
            "CARDASSIAN" or "CARD" or "CAR" => "CARD",
            "DOMINION" or "DOM" => "DOM",
            "FERENGI" or "FER" => "FER",
            "STARFLEET" or "STA" => "STA",
            "VULCAN" or "VUL" => "VUL",
            "BORG" => "BORG",
            "NON-ALIGNED" or "NONALIGNED" or "NA" or "NON" => "NA",
            "KCA" => "KCA",
            _ when t.Length >= 2 && t.Length <= 12 => t,
            _ => null
        };
    }

    public static List<TreatyLink> CollectActiveLinks(IEnumerable<Card> tablePermanents)
    {
        var links = new List<TreatyLink>();
        foreach (var c in tablePermanents ?? Array.Empty<Card>())
        {
            if (!IsTreatyCard(c) && ParseTreaty(c) == null && ParseAllLinks(c).Count == 0)
            {
                // auch Events die „compatible“ setzen ohne Treaty: im Namen
                if (!(c.Name ?? "").Contains("Treaty", StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            links.AddRange(ParseAllLinks(c));
        }
        return links;
    }

    /// <summary>
    /// Zwei Affiliation-Mengen: kompatibel wenn Schnittmenge, NA-Regel, oder Treaty-Link.
    /// </summary>
    public static bool AffiliationsCompatible(
        HashSet<string> a,
        HashSet<string> b,
        IReadOnlyList<TreatyLink>? treaties)
    {
        if (a.Count == 0 || b.Count == 0) return false;
        if (a.Overlaps(b)) return true;
        if (a.Contains("NA") && !b.Contains("BORG")) return true;
        if (b.Contains("NA") && !a.Contains("BORG")) return true;
        if (a.Contains("BORG") || b.Contains("BORG"))
            return a.Contains("BORG") && b.Contains("BORG");

        if (treaties == null || treaties.Count == 0) return false;

        foreach (var t in treaties)
        {
            bool aHasA = a.Contains(t.AffilA);
            bool aHasB = a.Contains(t.AffilB);
            bool bHasA = b.Contains(t.AffilA);
            bool bHasB = b.Contains(t.AffilB);
            // Karte A ist AffilA, Karte B ist AffilB (oder umgekehrt)
            if ((aHasA && bHasB) || (aHasB && bHasA))
                return true;
            // Multi-Affil Karte die beide Seiten des Treatys abdeckt
            if ((aHasA || aHasB) && (bHasA || bHasB) && (aHasA || aHasB) && (bHasA || bHasB))
            {
                // Wenn beide Seiten in der Vereinigung vorkommen und je eine Karte eine Seite hat
                if ((aHasA && bHasB) || (aHasB && bHasA) || (aHasA && aHasB) || (bHasA && bHasB))
                    return true;
            }
        }
        return false;
    }

    public static bool CardsCompatibleUnderTreaties(
        Card a,
        Card b,
        IReadOnlyList<TreatyLink>? treaties)
    {
        string ta = (a.Type ?? "").ToLowerInvariant();
        string tb = (b.Type ?? "").ToLowerInvariant();
        if (ta.Contains("equipment") || tb.Contains("equipment"))
            return true;

        var aa = ReportingRules.GetAffiliations(a);
        var bb = ReportingRules.GetAffiliations(b);
        return AffiliationsCompatible(aa, bb, treaties);
    }

    /// <summary>
    /// Force (mehrere Karten) untereinander kompatibel?
    /// </summary>
    public static bool ForceCompatible(
        IEnumerable<Card> force,
        IReadOnlyList<TreatyLink>? treaties)
    {
        var list = force?.Where(c => !ModifierRules.IsEquipmentCard(c)).ToList()
                   ?? new List<Card>();
        if (list.Count <= 1) return true;
        for (int i = 0; i < list.Count; i++)
            for (int j = i + 1; j < list.Count; j++)
            {
                if (!CardsCompatibleUnderTreaties(list[i], list[j], treaties)
                    && !ReportingRules.AreCompatible(list[i], list[j], treatyAllowsMix: false))
                    return false;
            }
        return true;
    }

    public static string FormatActiveTreaties(IReadOnlyList<TreatyLink> links)
    {
        if (links == null || links.Count == 0) return "Keine Treaties aktiv.";
        return string.Join("\n",
            links.Select(l => $"• {l.SourceName}: {l.AffilA} ↔ {l.AffilB}"));
    }
}