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

    /// <summary>
    /// Multi-word 1E skills must stay one token (else "Computer Skill" becomes Computer + Skill
    /// and mission/dilemma checks fail).
    /// </summary>
    public static readonly string[] KnownMultiWordSkills =
    {
        "Computer Skill", "Stellar Cartography", "Transporter Skill",
        "Engineer Skill", "Medical Skill", "Science Skill"
    };

    /// <summary>Printed classifications that also count as a skill.</summary>
    public static readonly string[] Classifications =
    {
        "OFFICER", "ENGINEER", "SCIENCE", "SECURITY", "MEDICAL", "CIVILIAN", "V.I.P.", "VIP", "ANIMAL", "ANDROID"
    };

    static readonly string[] KnownSingleSkills =
    {
        "Diplomacy", "Leadership", "Navigation", "Honor", "Treachery", "Empathy",
        "Anthropology", "Archaeology", "Astrophysics", "Biology", "Exobiology",
        "Geology", "Physics", "Youth", "Music", "Mindmeld", "Greed", "Law",
        "Resistance", "Guramba", "Cantankerousness", "Acquisition", "Anthropology"
    };

    /// <summary>
    /// Skills + classification levels from personnel data.
    /// Lackey puts classification in both <c>class</c> and the skill <c>text</c>
    /// (e.g. class=MEDICAL, text="MEDICAL MEDICAL Biology"). Parse the skill line first;
    /// only fill classification from <c>class</c> when the skill line does not already
    /// include that classification (avoids double-counting).
    /// </summary>
    public static Dictionary<string, int> ParsePersonnelSkills(Card personnel)
    {
        var skills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, int n = 1)
        {
            name = NormalizeSkill(name);
            if (string.IsNullOrEmpty(name)) return;
            skills[name] = skills.GetValueOrDefault(name) + n;
        }

        string text = (personnel.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(text))
        {
            // Longest multi-word skills first, with optional "x 2"
            string remaining = text;
            foreach (var multi in KnownMultiWordSkills.OrderByDescending(s => s.Length))
            {
                var rx = new Regex(@"\b" + Regex.Escape(multi) + @"\b(?:\s*x\s*(\d+))?", RegexOptions.IgnoreCase);
                foreach (Match m in rx.Matches(remaining))
                {
                    int n = 1;
                    if (m.Groups[1].Success && int.TryParse(m.Groups[1].Value, out int mult))
                        n = Math.Max(1, mult);
                    Add(multi, n);
                }
                remaining = rx.Replace(remaining, " ");
            }

            var tokens = Regex.Split(remaining, @"\s+");
            for (int i = 0; i < tokens.Length; i++)
            {
                string tok = tokens[i].Trim().TrimEnd(',', ';', '.');
                if (tok.Length < 2) continue;
                // "Computer Skill x2" style after multi-word already stripped: "Foo x 2"
                if (tok.Equals("x", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length
                    && int.TryParse(tokens[i + 1], out int mult) && skills.Count > 0)
                {
                    var last = skills.Keys.Last();
                    // xN is total level for that skill, not additive on top of the one token
                    skills[last] = Math.Max(skills[last], mult);
                    i++;
                    continue;
                }
                if (int.TryParse(tok, out _)) continue;
                if (tok.Contains('>') || tok.Contains('<')) continue;
                if (tok.Equals("Skill", StringComparison.OrdinalIgnoreCase)) continue;

                bool known = Classifications.Any(c => c.Equals(tok, StringComparison.OrdinalIgnoreCase))
                             || KnownSingleSkills.Any(c => c.Equals(tok, StringComparison.OrdinalIgnoreCase));
                if (!known) continue;
                Add(tok);
            }
        }

        // Classification field: only if skill line did not already list it
        string cls = (personnel.Class ?? "").Trim();
        if (cls.Length > 0)
        {
            foreach (var part in cls.Split(new[] { '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string c = part.Trim();
                if (c.Length == 0) continue;
                if (!skills.ContainsKey(c))
                    Add(c, 1);
            }
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

    /// <summary>Split printed mission text into owner side vs "Opponent's side: …".</summary>
    public static (string OwnerSide, string OpponentSide) SplitOwnerOpponentSides(string? text)
    {
        string t = (text ?? "").Trim();
        if (t.Length == 0) return ("", "");
        int idx = t.IndexOf("Opponent's side:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return (t, "");
        return (t[..idx].Trim(), t[(idx + "Opponent's side:".Length)..].Trim());
    }

    /// <summary>
    /// Mission-Requirements for the given perspective.
    /// Owner uses the printed (left) requirements; opponent uses Opponent's-side skills when present,
    /// otherwise falls back to the owner requirements (e.g. Warped Space only changes Span).
    /// </summary>
    public static List<string> ParseRequirementParts(Card mission, bool forOwner = true)
    {
        var (ownerSide, oppSide) = SplitOwnerOpponentSides(mission.Text);
        string use = forOwner ? ownerSide : oppSide;
        var parts = ParseRequirementPartsFromText(use);
        if (!forOwner && parts.Count == 0)
        {
            // Opponent's side may only change Span / gametext — keep owner skill reqs
            parts = ParseRequirementPartsFromText(ownerSide);
        }
        return parts;
    }

    public static List<string> ParseRequirementPartsFromText(string text)
    {
        var parts = new List<string>();
        text = (text ?? "").Trim();
        if (text.Length == 0) return parts;

        // Drop pure Span: N clauses (not a skill requirement)
        text = Regex.Replace(text, @"\bSpan\s*:\s*\d+\b", "", RegexOptions.IgnoreCase).Trim();
        // Drop "No gametext" / "Any crew may attempt…" fluff for skill list
        if (Regex.IsMatch(text, @"^No\s+gametext", RegexOptions.IgnoreCase))
            return parts;

        foreach (var raw in text.Split('+'))
        {
            string p = raw.Trim();
            if (p.Length == 0) continue;
            int cut = p.IndexOf('.');
            if (cut > 0 && cut < 40) p = p[..cut].Trim();
            if (p.Length > 60) continue;
            // Skip affiliation-only tokens like [fed] at start of opponent lines when mixed
            if (Regex.IsMatch(p, @"^\[.+\]$")) continue;
            parts.Add(p);
        }
        return parts;
    }

    /// <summary>
    /// Printed span, or Opponent's-side Span: N when the moving/attempting player is not the mission owner.
    /// </summary>
    public static int GetEffectiveSpan(Card mission, bool forOwner)
    {
        if (!forOwner)
        {
            var (_, opp) = SplitOwnerOpponentSides(mission.Text);
            var m = Regex.Match(opp, @"Span\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int oppSpan))
                return Math.Max(0, oppSpan);
        }
        string s = (mission.Span ?? "").Trim();
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return Math.Max(0, v);
        var m2 = Regex.Match(s, @"\d+");
        if (m2.Success && int.TryParse(m2.Value, out v))
            return v;
        return 0;
    }

    public static HashSet<string> ParseAffiliationTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return set;
        foreach (Match m in Regex.Matches(raw, @"\[([^\]]+)\]"))
        {
            string t = NormalizeAffiliationToken(m.Groups[1].Value.Trim());
            if (t.Length > 0) set.Add(t);
        }
        if (set.Count == 0)
        {
            string u = NormalizeAffiliationToken(raw.Trim());
            if (u.Length > 0) set.Add(u);
        }
        return set;
    }

    public static string NormalizeAffiliationToken(string t)
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
        if (t is "NON-ALIGNED" or "NONALIGNED" or "NA" or "NON") return "NA";
        return t;
    }

    /// <summary>
    /// Mission affiliation icons restrict which personnel may attempt.
    /// Empty / neutral mission → anyone. Otherwise at least one matching personnel affiliation required.
    /// </summary>
    public static bool TeamMatchesMissionAffiliation(Card mission, IEnumerable<Card> team)
    {
        var need = ParseAffiliationTokens(mission.Affiliation);
        if (need.Count == 0)
        {
            CheckTrace.Line($"Affiliation: mission '{mission.Name}' has no affiliation icons → any team ok");
            return true;
        }
        string needStr = string.Join("/", need);
        foreach (var p in team)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            var have = ParseAffiliationTokens(p.Affiliation);
            string haveStr = string.Join("/", have);
            bool ok = have.Overlaps(need);
            CheckTrace.Cmp("Affiliation",
                $"mission {mission.Name} [{needStr}]",
                $"{p.Name} [{haveStr}]",
                ok);
            if (ok) return true;
        }
        CheckTrace.Line($"Affiliation: no team member matches mission '{mission.Name}' need [{needStr}]");
        return false;
    }

    public static AttemptResult CanSolve(
        Card mission,
        IEnumerable<Card> team,
        int dilemmasRemaining,
        int attemptingPlayer = 0,
        int missionOwner = 0)
    {
        var teamList = team.ToList();
        if (teamList.Count == 0)
            return new AttemptResult(false, "Kein Away Team / keine Crew an der Mission.", false, 0);

        _ = dilemmasRemaining;

        // Affiliation gate (Fed mission ≠ Klingon attempt, etc.)
        if (!TeamMatchesMissionAffiliation(mission, teamList))
        {
            string need = string.Join("/", ParseAffiliationTokens(mission.Affiliation));
            return new AttemptResult(false,
                $"Affiliation mismatch: mission requires {need}; no matching personnel in team.",
                false, 0);
        }

        bool forOwner = missionOwner == 0 || attemptingPlayer == 0 || attemptingPlayer == missionOwner;

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
            CheckTrace.Line(
                $"Team '{p.Name}': "
                + string.Join(", ", ep.Skills.Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key))
                + $"  INT {ep.Integrity} CUN {ep.Cunning} STR {ep.Strength}");
            str += ep.Strength;
        }
        CheckTrace.Line(
            $"Solve '{mission.Name}' pool: "
            + string.Join(", ", pool.OrderBy(k => k.Key).Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key))
            + $"  | INT {integ} CUN {cunn} STR {str}");

        var reqs = ParseRequirementParts(mission, forOwner);
        if (reqs.Count == 0)
        {
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
                CheckTrace.Cmp("Attribute", $"{attr} {op} {need}", have.ToString(), ok);
                if (!ok) missing.Add($"{attr} {op} {need} (have {have})");
                continue;
            }

            var mSkill = Regex.Match(req, @"^([A-Za-z][A-Za-z\s\-']+?)(?:\s*x\s*(\d+))?$", RegexOptions.IgnoreCase);
            if (mSkill.Success)
            {
                string skill = mSkill.Groups[1].Value.Trim();
                // Strip leading affiliation icons from opponent-side skill lines
                skill = Regex.Replace(skill, @"^\[[^\]]+\]\s*", "").Trim();
                if (skill.Length == 0) continue;
                int need = mSkill.Groups[2].Success ? int.Parse(mSkill.Groups[2].Value) : 1;
                int have = 0;
                var matchedKeys = new List<string>();
                foreach (var kv in pool)
                {
                    bool hit = kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase)
                        || kv.Key.StartsWith(skill, StringComparison.OrdinalIgnoreCase)
                        || skill.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase);
                    CheckTrace.Cmp("Skill key", skill, kv.Key, hit);
                    if (hit)
                    {
                        have += kv.Value;
                        matchedKeys.Add($"{kv.Key}×{kv.Value}");
                    }
                }
                bool ok = have >= need;
                CheckTrace.Line(
                    $"Skill total: need '{skill}' x{need}  have {have}"
                    + (matchedKeys.Count > 0 ? $" from [{string.Join(", ", matchedKeys)}]" : " from []")
                    + $" → {(ok ? "MATCH" : "NO MATCH")}");
                if (!ok)
                    missing.Add($"{skill} x{need} (have {have})");
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