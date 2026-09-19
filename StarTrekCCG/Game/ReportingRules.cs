using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 6.3 Reporting for Duty (Premiere-Kern + Erweiterungen die Daten erlauben).
/// Gebaut-in-Reporting: usable, compatible Facility (Outpost/HQ) in native quadrant.
/// Special Reporting / Time Locations / Treaties: Hooks vorbereitet.
/// </summary>
public static class ReportingRules
{
    public readonly record struct ReportResult(bool Ok, string Reason);

    public enum Quadrant
    {
        Alpha,
        Gamma,
        Delta,
        Mirror
    }

    /// <summary>Muss die Karte reporten (nicht einfach auf den Tisch)?</summary>
    public static bool MustReportForDuty(Card card) => CardKinds.MustReportForDuty(card);

    public static bool IsFacilityHost(Card host)
    {
        string t = (host.Type ?? "").ToLowerInvariant();
        string n = (host.Name ?? "").ToLowerInvariant();
        return t.Contains("facility") || t.Contains("outpost") || t.Contains("headquarters")
               || t.Contains("station") || n.Contains("outpost") || n.Contains("headquarters");
    }

    public static bool IsShipHost(Card host)
    {
        string t = (host.Type ?? "").ToLowerInvariant();
        return t.Contains("ship");
    }

    public static bool IsMissionHost(Card host) =>
        string.Equals(host.Type, "Mission", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Native quadrant aus Icons / Quadrant-Feld.
    /// Alpha = kein GQ/DQ/MQ-Icon (Mehrheit der Premiere-Karten).
    /// </summary>
    public static Quadrant GetNativeQuadrant(Card card)
    {
        string icons = (card.Icons ?? "") + (card.Text ?? "") + (card.Quadrant ?? "");
        string u = icons.ToUpperInvariant();
        // Häufige Lackey-/CC-Markierungen
        if (u.Contains("[DQ]") || u.Contains("DELTA")) return Quadrant.Delta;
        if (u.Contains("[GQ]") || u.Contains("GAMMA")) return Quadrant.Gamma;
        if (u.Contains("[MQ]") || u.Contains("MIRROR") || u.Contains("[M]"))
        {
            // Vorsicht: [M] kann Mission sein – nur wenn klar Mirror
            if (u.Contains("[MQ]") || u.Contains("MIRROR"))
                return Quadrant.Mirror;
        }
        return Quadrant.Alpha;
    }

    /// <summary>Equipment hat keine native quadrant – immer ok.</summary>
    public static bool HasNativeQuadrant(Card card)
    {
        string t = (card.Type ?? "").ToLowerInvariant();
        return !t.Contains("equipment");
    }

    public static HashSet<string> ParseAffiliationTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return set;
        foreach (Match m in Regex.Matches(raw, @"\[([^\]]+)\]"))
        {
            string t = m.Groups[1].Value.Trim();
            if (t.Length > 0) set.Add(NormalizeAffil(t));
        }
        if (set.Count == 0)
        {
            // "Federation" / "Federation Outpost" / mehrwortig
            string u = raw.Trim();
            foreach (var word in Regex.Split(u, @"[\s/,;]+"))
            {
                if (string.IsNullOrWhiteSpace(word)) continue;
                string n = NormalizeAffil(word);
                if (n is "FED" or "KLI" or "ROM" or "BAJ" or "CARD" or "DOM"
                    or "FER" or "BORG" or "NA" or "HIR" or "KAZ" or "VID")
                    set.Add(n);
            }
            if (set.Count == 0 && u.Length > 0)
                set.Add(NormalizeAffil(u));
        }
        return set;
    }

    /// <summary>Affiliation aus Feld oder Kartenname (z.B. „Federation Outpost“).</summary>
    public static HashSet<string> GetAffiliations(Card card)
    {
        // Glossary: Lore's Fingernail — effective affiliation Non only (dual mode parked).
        if (EventRules.FingernailMakesNon(card))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "NA" };

        if (DualAffiliationRules.IsMulti(card))
            return DualAffiliationRules.ActiveAffiliations(card);
        // Commandeer / Lore Returns / Frame: live mode overrides printed affiliation.
        if (!string.IsNullOrWhiteSpace(card.CurrentAffiliation))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NormalizeAffil(card.CurrentAffiliation)
            };
        var set = ParseAffiliationTokens(card.Affiliation);
        if (set.Count > 0) return set;
        return ParseAffiliationTokens(card.Name);
    }

    /// <summary>
    /// Live affiliation label for UI (Detail / Reveal / Badge).
    /// Uses GetAffiliations (Fingernail / CurrentAffiliation / dual mode), not printed only.
    /// </summary>
    public static string FormatLiveAffiliation(Card card)
    {
        var set = GetAffiliations(card);
        if (set.Count == 0) return "";
        return string.Join("/", set
            .Select(DualAffiliationRules.DisplayName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Type-line suffix e.g. "  ·  Non-Aligned" (empty when no affiliation).</summary>
    public static string FormatAffiliationTypeSuffix(Card card, string sep = "  ·  ")
    {
        string live = FormatLiveAffiliation(card);
        return string.IsNullOrEmpty(live) ? "" : sep + live;
    }

    /// <summary>Short bracket form e.g. [Non] / [Fed] from live GetAffiliations.</summary>
    public static string FormatLiveAffiliationBracket(Card card)
    {
        var set = GetAffiliations(card);
        if (set.Count == 0) return "";
        return string.Join("/", set.Select(BracketAffil));
    }

    public static string BracketAffil(string token) => NormalizeAffil(token) switch
    {
        "FED" => "[Fed]",
        "KLI" => "[Kli]",
        "ROM" => "[Rom]",
        "BAJ" => "[Baj]",
        "CARD" => "[Car]",
        "DOM" => "[Dom]",
        "FER" => "[Fer]",
        "BORG" => "[Bor]",
        "NA" => "[Non]",
        "HIR" => "[Hir]",
        "KAZ" => "[Kaz]",
        "VID" => "[Vid]",
        _ => "[" + token + "]"
    };

    public static string NormalizeAffil(string t)
    {
        t = t.ToUpperInvariant();
        if (t is "FEDERATION" or "FED") return "FED";
        if (t is "KLINGON" or "KLI") return "KLI";
        if (t is "ROMULAN" or "ROM") return "ROM";
        if (t is "BAJORAN" or "BAJ") return "BAJ";
        if (t is "CARDASSIAN" or "CARD" or "CAR") return "CARD";
        if (t is "DOMINION" or "DOM") return "DOM";
        if (t is "FERENGI" or "FER") return "FER";
        if (t is "BORG") return "BORG";
        if (t is "NON-ALIGNED" or "NONALIGNED" or "NA" or "NON" or "NEUTRAL" or "NEU") return "NA";
        if (t is "HIROGEN" or "HIR") return "HIR";
        if (t is "KAZON" or "KAZ") return "KAZ";
        if (t is "VIDIIAN" or "VID") return "VID";
        return t;
    }

    /// <summary>
    /// 6.3 COMPATIBLE:
    /// gleiche Affiliation; NA/Neutral mit allem außer Borg; Equipment mit allen;
    /// Treaties: mix &amp; cooperate (siehe TreatyRules).
    /// </summary>
    public static bool AreCompatible(
        Card a,
        Card b,
        bool treatyAllowsMix = false,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null)
    {
        if (treatyAllowsMix) return true;

        string ta = (a.Type ?? "").ToLowerInvariant();
        string tb = (b.Type ?? "").ToLowerInvariant();
        if (ta.Contains("equipment") || tb.Contains("equipment")
            || ta.Contains("artifact") || tb.Contains("artifact")
            || ArtifactRules.IsArtifact(a) || ArtifactRules.IsArtifact(b)
            || ModifierRules.IsEquipmentCard(a) || ModifierRules.IsEquipmentCard(b))
            return true;

        var aa = GetAffiliations(a);
        var bb = GetAffiliations(b);

        // Multi-affiliation: irgendeine gemeinsame
        if (aa.Overlaps(bb) && aa.Count > 0)
            return true;

        bool aNa = aa.Contains("NA") || aa.Count == 0 && ta.Contains("personnel");
        bool bNa = bb.Contains("NA") || bb.Count == 0 && tb.Contains("personnel");
        bool aBorg = aa.Contains("BORG");
        bool bBorg = bb.Contains("BORG");

        // Non-Aligned mix with all except Borg
        if (aNa && !bBorg) return true;
        if (bNa && !aBorg) return true;

        // Borg only with Borg
        if (aBorg || bBorg)
            return aBorg && bBorg;

        // Treaties: z.B. Fed ↔ Kli
        if (treaties != null && treaties.Count > 0
            && TreatyRules.AffiliationsCompatible(aa, bb, treaties))
            return true;

        // Leere Affiliation an Facility oft = aus Name/Typ; streng: nicht kompatibel ohne Token
        if (aa.Count == 0 || bb.Count == 0)
            return false;

        return false;
    }

    /// <summary>
    /// Usable: du kontrollierst den Host (owner match). Später: „usable by both“ (Trading Post).
    /// </summary>
    public static bool IsUsableBy(Card host, int hostOwner, int reportingPlayer, bool hostTextAllowsBoth = false)
    {
        if (hostTextAllowsBoth) return true;
        string text = (host.Text ?? "").ToLowerInvariant();
        // Heuristik: Ferengi Trading Post etc.
        if (text.Contains("usable by both") || text.Contains("both players") || text.Contains("either player"))
            return true;
        return hostOwner == reportingPlayer;
    }

    /// <summary>
    /// Built-in reporting target: Outpost / Headquarters (Facility), nicht Mission als Report-Ort.
    /// Special reporting kann Schiffe erlauben (specialReportingToShips).
    /// Away Team auf Mission ist **kein** Report – das ist Bewegen/Beaming (Execute).
    /// </summary>
    public static ReportResult CanReportTo(
        Card reporting,
        Card host,
        int hostOwner,
        int reportingPlayer,
        bool specialReporting = false,
        bool treatyAllowsMix = false,
        bool allowReportToShip = false,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null)
    {
        if (!MustReportForDuty(reporting))
            return new ReportResult(true, ""); // Events etc. brauchen kein Report

        // Mission: kein built-in Report (Away Team = Order)
        if (IsMissionHost(host))
        {
            return new ReportResult(false,
                $"\"{reporting.Name}\" does not report to a mission (Away Team = Execute/Beaming). "
                + "Report to Outpost/HQ (or ship if Special Reporting).");
        }

        // Ship als Host nur mit Special Reporting / erlaubender Karte
        if (IsShipHost(host))
        {
            if (!specialReporting && !allowReportToShip)
            {
                return new ReportResult(false,
                    $"\"{reporting.Name}\": built-in reporting only to Outpost/Headquarters – not to ships "
                    + "(unless Special Reporting).");
            }
        }
        else if (!IsFacilityHost(host))
        {
            return new ReportResult(false,
                $"\"{reporting.Name}\" must report to a facility (Outpost/HQ).");
        }

        // Usable
        if (!IsUsableBy(host, hostOwner, reportingPlayer))
        {
            return new ReportResult(false,
                $"\"{host.Name}\" is not usable by Player {reportingPlayer} (foreign facility).");
        }

        // Compatible (Equipment und Artifacts immer ok; Treaties erlauben Mix)
        string rt = (reporting.Type ?? "").ToLowerInvariant();
        if (!rt.Contains("equipment") && !rt.Contains("artifact") && !ArtifactRules.IsArtifact(reporting) && !AreCompatible(reporting, host, treatyAllowsMix, treaties))
        {
            return new ReportResult(false,
                $"\"{reporting.Name}\" ({reporting.Affiliation ?? "?"}) is not compatible with "
                + $"\"{host.Name}\" ({host.Affiliation ?? "?"}) – Treaty missing.");
        }

        // Native quadrant (Equipment ausgenommen; Special Reporting hebt auf)
        if (!specialReporting && HasNativeQuadrant(reporting) && HasNativeQuadrant(host))
        {
            var qReport = GetNativeQuadrant(reporting);
            var qHost = GetNativeQuadrant(host);
            if (qReport != qHost)
            {
                return new ReportResult(false,
                    $"Native Quadrant: \"{reporting.Name}\" ({qReport}) and \"{host.Name}\" ({qHost}) "
                    + "must match (Special Reporting overrides this).");
            }
        }

        // Site/Station special reporting: native quadrant bleibt – hier built-in Facility bereits geprüft

        return new ReportResult(true, "");
    }

    /// <summary>
    /// 6.3.1 Persona / Unique: nur innerhalb derselben Kartenfamilie
    /// (Personal↔Personal, Schiff↔Schiff, Facility↔Facility).
    /// Lore ohne Bold-Markup nicht nutzen: „matching commander“ auf Schiffen
    /// ist keine Persona (Enterprise-Lore nennt Picard → war der Bug).
    /// </summary>
    public static ReportResult CheckPersonaLimit(Card reporting, IEnumerable<Card> ownedInPlay)
    {
        if (PlayRules.GetUniqueness(reporting) == PlayRules.UniquenessKind.Universal)
            return new ReportResult(true, "");

        string key = PlayRules.PersonaKey(reporting);
        string family = PersonaFamily(reporting);

        foreach (var other in ownedInPlay)
        {
            if (ReferenceEquals(other, reporting)) continue;
            // E4: instance identity — same InstanceId is the same copy, not a second unique.
            if (reporting.InstanceId > 0 && other.InstanceId == reporting.InstanceId) continue;
            if (PersonaFamily(other) != family) continue;

            if (string.Equals(PlayRules.PersonaKey(other), key, StringComparison.OrdinalIgnoreCase))
            {
                int owner = other.OwnerPlayer != 0 ? other.OwnerPlayer : other.Controller;
                string haveBit = other.InstanceId > 0 ? $"#{other.InstanceId}" : DebugLog.Card(other);
                DebugLog.Play(0, owner,
                    $"unique deny {DebugLog.Card(reporting)} have={haveBit} owner={owner}");
                return new ReportResult(false,
                    $"Persona/Unique: \"{reporting.Name}\" – you already have \"{other.Name}\" in play.");
            }
        }

        return new ReportResult(true, "");
    }

    /// <summary>Persona-Checks nur innerhalb einer Familie.</summary>
    private static string PersonaFamily(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        if (t.Contains("personnel") || t.Contains("android") || t.Contains("animal"))
            return "personnel";
        if (t.Contains("ship"))
            return "ship";
        if (t.Contains("facility") || t.Contains("outpost") || t.Contains("headquarters") || t.Contains("station"))
            return "facility";
        if (t.Contains("equipment"))
            return "equipment";
        return t;
    }
}
