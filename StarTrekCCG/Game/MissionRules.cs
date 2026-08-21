using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 7.2 Mission Attempt / Solve – Premiere-Kern (Skill-Requirements).
/// Dilemma-Encounter: nur „noch Dilemmas übrig?“-Gate; volle Encounter-Engine später.
/// </summary>
public static class MissionRules
{
    public readonly record struct AttemptResult(bool Ok, string Reason, bool Solved, int Points);

    public static bool IsPlanetMission(Card mission)
    {
        string md = (mission.MissionDilemmaType ?? "").ToUpperInvariant();
        // Lackey: [P] Planet, [S] Space, [S][P] beides
        bool p = md.Contains("[P]");
        bool s = md.Contains("[S]");
        if (p && !s) return true;
        if (s && !p) return false;
        if (p && s) return true; // Dual: Away Team ODER Crew möglich – Attempt-UI entscheidet
        return true;
    }

    public static bool IsSpaceMission(Card mission) => !IsPlanetMission(mission);

    /// <summary>Skills aus Personnel-Text (z.B. „OFFICER Diplomacy x 2 Leadership …“).</summary>
    public static Dictionary<string, int> ParsePersonnelSkills(Card personnel)
    {
        var skills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string text = (personnel.Text ?? "").Trim();
        if (string.IsNullOrEmpty(text)) return skills;

        // Classification oft erstes Wort in Caps
        var tokens = Regex.Split(text, @"\s+");
        for (int i = 0; i < tokens.Length; i++)
        {
            string tok = tokens[i].Trim().TrimEnd(',', ';', '.');
            if (tok.Length < 2) continue;
            if (tok.Equals("x", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length
                && int.TryParse(tokens[i + 1], out int mult) && skills.Count > 0)
            {
                // "Diplomacy x 2" → letztes Skill * 2
                var last = skills.Keys.Last();
                skills[last] = Math.Max(skills[last], mult);
                i++;
                continue;
            }
            // Skip pure numbers already handled
            if (int.TryParse(tok, out _)) continue;
            // Attribute-Checks nicht hier
            if (tok.Contains('>') || tok.Contains('<')) continue;

            string skill = NormalizeSkill(tok);
            if (skill.Length == 0) continue;
            skills[skill] = skills.GetValueOrDefault(skill) + 1;
        }
        return skills;
    }

    public static string NormalizeSkill(string s)
    {
        s = s.Trim();
        // Classifications als Skill-ähnlich
        return s;
    }

    public static (int integ, int cunn, int str) ParseAttributes(Card p)
    {
        int.TryParse((p.IntegrityOrRange ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i);
        int.TryParse((p.CunningOrWeapons ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int c);
        int.TryParse((p.StrengthOrShields ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int st);
        return (i, c, st);
    }

    /// <summary>
    /// Mission-Requirements aus Text: „Geology + Honor + INTEGRITY>35“
    /// </summary>
    public static List<string> ParseRequirementParts(Card mission)
    {
        string text = (mission.Text ?? "").Trim();
        // Nur erster „Satz“ bis Punkt wenn Skill-Liste
        var parts = new List<string>();
        // Split an + aber nicht innerhalb von Wörtern
        foreach (var raw in text.Split('+'))
        {
            string p = raw.Trim();
            if (p.Length == 0) continue;
            // Abschneiden ab Doppelpunkt / langen Effekten
            int cut = p.IndexOf('.');
            if (cut > 0 && cut < 40) p = p[..cut].Trim();
            // Wormhole-Sondertexte etc. skippen wenn zu lang
            if (p.Length > 60) continue;
            parts.Add(p);
        }
        return parts;
    }

    public static AttemptResult CanSolve(
        Card mission,
        IEnumerable<Card> team,
        int dilemmasRemaining)
    {
        var teamList = team.ToList();
        if (teamList.Count == 0)
            return new AttemptResult(false, "Kein Away Team / keine Crew an der Mission.", false, 0);

        // Dilemmas werden vor dem Solve separat encountered (siehe TableWindow).
        // Hier nur Skill-Check für die Mission selbst.
        _ = dilemmasRemaining;

        // Skills/Attribute poolen – inkl. Equipment-Modifier (present = team inkl. Eq)
        var pool = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int integ = 0, cunn = 0, str = 0;
        foreach (var p in teamList)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            var ep = ModifierRules.ResolvePersonnel(p, teamList, owner: 0);
            // owner 0: Affiliation-Gate prüft present Affils der Karten selbst – ok
            foreach (var kv in ep.Skills)
                pool[kv.Key] = pool.GetValueOrDefault(kv.Key) + kv.Value;
            integ += ep.Integrity;
            cunn += ep.Cunning;
            str += ep.Strength;
        }

        var reqs = ParseRequirementParts(mission);
        if (reqs.Count == 0)
        {
            // Keine parsbaren Requirements → Sandbox: Team anwesend = lösbar
            int pts = ParsePoints(mission);
            return new AttemptResult(true, "Keine Skill-Requirements erkannt – Team vor Ort genügt (Sandbox).", true, pts);
        }

        var missing = new List<string>();
        foreach (var req in reqs)
        {
            var mAttr = Regex.Match(req, @"^(INTEGRITY|CUNNING|STRENGTH)\s*(>|>=|<|<=)\s*(\d+)",
                RegexOptions.IgnoreCase);
            if (mAttr.Success)
            {
                string attr = mAttr.Groups[1].Value.ToUpperInvariant();
                string op = mAttr.Groups[2].Value;
                int need = int.Parse(mAttr.Groups[3].Value);
                int have = attr switch
                {
                    "INTEGRITY" => integ,
                    "CUNNING" => cunn,
                    "STRENGTH" => str,
                    _ => 0
                };
                bool ok = op switch
                {
                    ">" => have > need,
                    ">=" => have >= need,
                    "<" => have < need,
                    "<=" => have <= need,
                    _ => have > need
                };
                if (!ok) missing.Add($"{attr} {op} {need} (haben {have})");
                continue;
            }

            // Skill-Name, optional „x 2“
            var mSkill = Regex.Match(req, @"^([A-Za-z][A-Za-z\s\-']+?)(?:\s*x\s*(\d+))?$", RegexOptions.IgnoreCase);
            if (mSkill.Success)
            {
                string skill = mSkill.Groups[1].Value.Trim();
                int need = mSkill.Groups[2].Success ? int.Parse(mSkill.Groups[2].Value) : 1;
                int have = 0;
                foreach (var kv in pool)
                {
                    if (kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase)
                        || kv.Key.StartsWith(skill, StringComparison.OrdinalIgnoreCase)
                        || skill.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                        have += kv.Value;
                }
                if (have < need)
                    missing.Add($"{skill} x{need} (haben {have})");
            }
        }

        if (missing.Count > 0)
            return new AttemptResult(false, "Requirements nicht erfüllt: " + string.Join("; ", missing), false, 0);

        return new AttemptResult(true, "Mission requirements erfüllt.", true, ParsePoints(mission));
    }

    public static int ParsePoints(Card mission)
    {
        if (int.TryParse((mission.Points ?? "").Trim(), out int p)) return p;
        return 0;
    }

    public static bool IsDilemma(Card c) =>
        (c.Type ?? "").Contains("Dilemma", StringComparison.OrdinalIgnoreCase);

    public static bool IsArtifact(Card c) =>
        (c.Type ?? "").Contains("Artifact", StringComparison.OrdinalIgnoreCase);

    /// <summary>Planet/Space/Dual aus mission_dilemma_type der Dilemma-Karte.</summary>
    public static bool DilemmaAllowedAtMission(Card dilemma, Card mission)
    {
        string d = (dilemma.MissionDilemmaType ?? "").ToUpperInvariant();
        bool dPlanet = d.Contains("[P]");
        bool dSpace = d.Contains("[S]");
        bool mPlanet = IsPlanetMission(mission);
        if (dPlanet && dSpace) return true; // dual
        if (!dPlanet && !dSpace) return true; // unknown → allow
        if (dPlanet && mPlanet) return true;
        if (dSpace && !mPlanet) return true;
        return false;
    }

    /// <summary>
    /// Vereinfachter Overcome-Check: Skill-Requirements im Dilemma-Text
    /// (z.B. „Medical and Security present“ / „CUNNING>30“).
    /// Keine vollen Effekt-Simulationen – nur conditions zum „get past“.
    /// </summary>
    public static AttemptResult CanOvercomeDilemma(Card dilemma, IEnumerable<Card> team)
    {
        var teamList = team.ToList();
        // Reuse requirement parsing on dilemma text
        var fakeMission = dilemma; // same text field
        // Temporarily use CanSolve with 0 dilemmas by building pool check on requirement-like phrases
        var pool = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int integ = 0, cunn = 0, str = 0;
        foreach (var p in teamList)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            var ep = ModifierRules.ResolvePersonnel(p, teamList, owner: 0);
            foreach (var kv in ep.Skills)
                pool[kv.Key] = pool.GetValueOrDefault(kv.Key) + kv.Value;
            integ += ep.Integrity;
            cunn += ep.Cunning;
            str += ep.Strength;
        }

        string text = dilemma.Text ?? "";
        // Attribute thresholds in text
        foreach (Match mAttr in Regex.Matches(text, @"(INTEGRITY|CUNNING|STRENGTH)\s*(>|>=)\s*(\d+)", RegexOptions.IgnoreCase))
        {
            string attr = mAttr.Groups[1].Value.ToUpperInvariant();
            int need = int.Parse(mAttr.Groups[3].Value);
            int have = attr == "INTEGRITY" ? integ : attr == "CUNNING" ? cunn : str;
            if (have <= need && mAttr.Groups[2].Value.StartsWith(">"))
                return new AttemptResult(false, $"{attr}>{need} nicht erreicht (haben {have}).", false, 0);
        }

        // Common skill words present
        string[] common = { "Medical", "Security", "ENGINEER", "Engineer", "Science", "Officer", "OFFICER",
            "Diplomacy", "Leadership", "Navigation", "Biology", "Physics", "Anthropology", "Archaeology",
            "Honor", "Treachery", "Empathy", "Computer Skill", "Anthropology" };
        foreach (var skill in common)
        {
            if (!Regex.IsMatch(text, @"\b" + Regex.Escape(skill) + @"\b", RegexOptions.IgnoreCase))
                continue;
            // Only if appears as requirement-ish (before "to get past" or with "required"/"present")
            bool reqContext = Regex.IsMatch(text, skill + @".{0,40}(present|required|to get past|unless)", RegexOptions.IgnoreCase)
                || Regex.IsMatch(text, @"(unless|require|without).{0,40}" + skill, RegexOptions.IgnoreCase);
            if (!reqContext) continue;
            int have = pool.Where(kv => kv.Key.Contains(skill, StringComparison.OrdinalIgnoreCase)
                                        || skill.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                           .Sum(kv => kv.Value);
            if (have < 1)
                return new AttemptResult(false, $"Skill „{skill}“ fehlt im Team.", false, 0);
        }

        return new AttemptResult(true, "Dilemma conditions (Heuristik) erfüllt.", true, 0);
    }
}