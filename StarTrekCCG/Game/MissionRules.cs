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
        var mode = DualAffiliationRules.ProfileFor(personnel);
        if (mode != null)
        {
            var modeSkills = new Dictionary<string, int>(mode.Skills, StringComparer.OrdinalIgnoreCase);
            string modeCls = (personnel.Class ?? "").Trim();
            if (modeCls.Length > 0 && !modeSkills.ContainsKey(modeCls))
                modeSkills[modeCls] = 1;
            if (mode.Classification != null && !modeSkills.ContainsKey(mode.Classification))
                modeSkills[mode.Classification] = 1;
            return modeSkills;
        }

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
            // Longest multi-word skills first, with optional "x2" / "x 2" / "×2"
            string remaining = text;
            foreach (var multi in KnownMultiWordSkills.OrderByDescending(s => s.Length))
            {
                var rx = new Regex(
                    @"\b" + Regex.Escape(multi) + @"\b(?:\s*[xX×]\s*(\d+))?",
                    RegexOptions.IgnoreCase);
                foreach (Match m in rx.Matches(remaining))
                {
                    int n = 1;
                    if (m.Groups[1].Success && int.TryParse(m.Groups[1].Value, out int mult))
                        n = Math.Max(1, mult);
                    Add(multi, n);
                }
                remaining = rx.Replace(remaining, " ");
            }

            // Single-word skills + classifications with the same xN pattern
            // (e.g. "Diplomacy x 2", "OFFICER", "Mindmeld") — do not rely on Keys.Last()
            var singles = Classifications.Concat(KnownSingleSkills)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(s => s.Length);
            foreach (var skill in singles)
            {
                var rx = new Regex(
                    @"\b" + Regex.Escape(skill) + @"\b(?:\s*[xX×]\s*(\d+))?",
                    RegexOptions.IgnoreCase);
                foreach (Match m in rx.Matches(remaining))
                {
                    int n = 1;
                    if (m.Groups[1].Success && int.TryParse(m.Groups[1].Value, out int mult))
                        n = Math.Max(1, mult);
                    Add(skill, n);
                }
                remaining = rx.Replace(remaining, " ");
            }

            // Leftover "x N" without skill name is ignored (skill already consumed)
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

    
    /// <summary>
    /// Printed classification parts from the classification box (not the skill box).
    /// Lackey also echoes these as leading token(s) in <c>text</c>.
    /// </summary>
    public static IReadOnlyList<string> PrintedClassificationParts(Card personnel)
    {
        var parts = new List<string>();
        if (personnel == null) return parts;
        string cls = (personnel.Class ?? "").Trim();
        if (cls.Length == 0) return parts;
        foreach (var part in cls.Split(new[] { '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string c = NormalizeSkill(part.Trim());
            if (c.Length == 0) continue;
            if (!parts.Exists(p => p.Equals(c, StringComparison.OrdinalIgnoreCase)))
                parts.Add(c);
            // VIP / V.I.P. aliases so leading-echo strip matches either form
            if (c.Equals("VIP", StringComparison.OrdinalIgnoreCase)
                && !parts.Exists(p => p.Equals("V.I.P.", StringComparison.OrdinalIgnoreCase)))
                parts.Add("V.I.P.");
            if (c.Equals("V.I.P.", StringComparison.OrdinalIgnoreCase)
                && !parts.Exists(p => p.Equals("VIP", StringComparison.OrdinalIgnoreCase)))
                parts.Add("VIP");
        }
        return parts;
    }

    /// <summary>
    /// First-listed skill from the printed skill box (Glossary / Spock):
    /// left-to-right regular/special skill — NOT the classification box.
    /// Lackey puts classification as leading token(s) in <c>text</c>; those are skipped
    /// when they match <see cref="Card.Class"/>. Next skill (incl. multi-word / xN name)
    /// is first-listed. Same-named skill after the echo (e.g. Bashir MEDICAL x2) counts.
    /// Assimilation: when Class no longer matches the leading token, that token is the
    /// first-listed skill (former classification). Used by Tsiolkovsky Infection (not cumulative).
    /// </summary>
    public static string? FirstListedSkill(Card personnel)
    {
        if (personnel == null) return null;

        string text = (personnel.Text ?? "").Trim();
        if (text.Length == 0)
            return null;

        // Strip Lackey leading classification-box echo(s) matching Card.Class (each part once).
        string remaining = text;
        var classParts = PrintedClassificationParts(personnel);
        var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool progressed = true;
        while (progressed)
        {
            progressed = false;
            foreach (var part in classParts)
            {
                if (skipped.Contains(part)) continue;
                var lead = new Regex(
                    @"^\s*" + Regex.Escape(part) + @"\b",
                    RegexOptions.IgnoreCase);
                var m = lead.Match(remaining);
                if (!m.Success) continue;
                remaining = remaining.Substring(m.Length);
                skipped.Add(part);
                progressed = true;
                break;
            }
        }
        remaining = remaining.Trim();
        if (remaining.Length == 0)
            return null;

        var candidates = KnownMultiWordSkills
            .Concat(Classifications)
            .Concat(KnownSingleSkills)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        int bestPos = int.MaxValue;
        int bestLen = -1;
        string? best = null;
        foreach (var skill in candidates)
        {
            var rx = new Regex(
                @"\b" + Regex.Escape(skill) + @"\b",
                RegexOptions.IgnoreCase);
            var match = rx.Match(remaining);
            if (!match.Success) continue;
            if (match.Index < bestPos || (match.Index == bestPos && skill.Length > bestLen))
            {
                bestPos = match.Index;
                bestLen = skill.Length;
                best = skill;
            }
        }
        return best != null ? NormalizeSkill(best) : null;
    }

public static (int integ, int cunn, int str) ParseAttributes(Card p)
    {
        int.TryParse((p.IntegrityOrRange ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i);
        int.TryParse((p.CunningOrWeapons ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int c);
        int.TryParse((p.StrengthOrShields ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int st);
        var mode = DualAffiliationRules.ProfileFor(p);
        if (mode != null)
        {
            i += mode.IntegrityDelta;
            c += mode.CunningDelta;
            st += mode.StrengthDelta;
        }
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

    public static List<string> SplitOrAlternatives(string req)
    {
        var bits = Regex.Split(req ?? "", @"\s+OR\s+", RegexOptions.IgnoreCase)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        return bits.Count > 0 ? bits : new List<string> { (req ?? "").Trim() };
    }


    /// <summary>True when text asks for printed classification (Class box), not skill.</summary>
    public static bool IsClassificationRequirement(string alt)
    {
        alt = Regex.Replace(alt ?? "", @"^\[[^\]]+\]\s*", "").Trim();
        return Regex.IsMatch(alt, @"classification", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// "ENGINEER-classification" / "ENGINEER classification" — Kit skill grants do not count.
    /// </summary>
    public static bool ClassificationRequirementMet(
        string alt,
        Dictionary<string, int> classPool,
        out string detail)
    {
        alt = Regex.Replace(alt ?? "", @"^\[[^\]]+\]\s*", "").Trim();
        detail = alt + " (not classification)";
        if (!Regex.IsMatch(alt, @"classification", RegexOptions.IgnoreCase))
            return false;

        var m = Regex.Match(alt,
            @"^([A-Za-z][A-Za-z.\s]*?)\s*-?\s*classification(?:\s*[xX×]\s*(\d+))?\.?$",
            RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            m = Regex.Match(alt,
                @"^classification\s+([A-Za-z][A-Za-z.\s]*?)(?:\s*[xX×]\s*(\d+))?\.?$",
                RegexOptions.IgnoreCase);
        }
        if (!m.Success)
        {
            detail = alt + " (classification unparsed)";
            return false;
        }
        string cls = m.Groups[1].Value.Trim();
        int needN = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
        int haveN = 0;
        foreach (var kv in classPool)
        {
            if (kv.Key.Equals(cls, StringComparison.OrdinalIgnoreCase))
                haveN += kv.Value;
        }
        detail = $"{cls}-classification x{needN} (have {haveN})";
        return haveN >= needN;
    }

    /// <summary>One alternative: "Diplomacy x5" or "CUNNING>30". Exact skill name, count >= need.</summary>
    public static bool AlternativeMet(
        string alt,
        Dictionary<string, int> pool,
        int integ, int cunn, int str,
        out string detail)
    {
        alt = Regex.Replace(alt ?? "", @"^\[[^\]]+\]\s*", "").Trim();
        var mAttr = Regex.Match(alt, @"^(INTEGRITY|CUNNING|STRENGTH)\s*(>|>=|<|<=)\s*(\d+)",
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
            detail = $"{attr} {op} {need} (have {have})";
            return ok;
        }

        var mSkill = Regex.Match(alt,
            @"^([A-Za-z][A-Za-z\s\-']+?)(?:\s*[xX×]\s*(\d+))?\.?$",
            RegexOptions.IgnoreCase);
        if (!mSkill.Success)
        {
            detail = alt + " (unparsed)";
            return false;
        }
        string skill = mSkill.Groups[1].Value.Trim();
        int needN = mSkill.Groups[2].Success ? int.Parse(mSkill.Groups[2].Value) : 1;
        int haveN = 0;
        foreach (var kv in pool)
        {
            if (kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase))
                haveN += kv.Value;
        }
        detail = $"{skill} x{needN} (have {haveN})";
        return haveN >= needN;
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
        var fromText = Regex.Match(mission.Text ?? "", @"\bspan\s+(\d+)\b", RegexOptions.IgnoreCase);
        if (fromText.Success && int.TryParse(fromText.Groups[1].Value, out v))
            return Math.Max(0, v);
        if (EventRules.NameIs(mission, "Gaps in Normal Space"))
            return 4;
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
    public static bool TeamMatchesMissionAffiliation(
        Card mission,
        IEnumerable<Card> team,
        IEnumerable<string>? extraMissionIcons = null)
    {
        var need = ParseAffiliationTokens(mission.Affiliation);
        if (extraMissionIcons != null)
        {
            foreach (var x in extraMissionIcons)
            {
                string n = NormalizeAffiliationToken(x);
                if (n.Length > 0) need.Add(n);
            }
        }
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
        int missionOwner = 0,
        IEnumerable<string>? extraMissionIcons = null,
        IEnumerable<string>? disabledSkills = null,
        bool loseFirstListedSkill = false)
    {
        var teamList = team.ToList();
        if (teamList.Count == 0)
            return new AttemptResult(false, "No Away Team / crew at the mission.", false, 0);

        _ = dilemmasRemaining;

        // Affiliation gate (Fed mission ≠ Klingon attempt, etc.)
        if (!TeamMatchesMissionAffiliation(mission, teamList, extraMissionIcons))
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
            var ep = ModifierRules.ResolvePersonnel(p, teamList, owner: attemptingPlayer, disabledSkills, loseFirstListedSkill);
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
            return new AttemptResult(true, "No skill requirements detected – present team suffices (sandbox).", true, pts);
        }

        // Printed Class counts for "X-classification" only (Kit skill grants do not).
        var classPool = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in teamList)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            string cls = (p.Class ?? "").Trim();
            if (cls.Length == 0) continue;
            classPool[cls] = classPool.GetValueOrDefault(cls) + 1;
        }

        var missing = new List<string>();
        foreach (var req in reqs)
        {
            var alts = SplitOrAlternatives(req);
            bool anyOk = false;
            var altFail = new List<string>();
            foreach (var alt in alts)
            {
                if (ClassificationRequirementMet(alt, classPool, out string classDetail))
                {
                    anyOk = true;
                    continue;
                }
                if (IsClassificationRequirement(alt))
                {
                    altFail.Add(classDetail);
                    continue;
                }
                if (AlternativeMet(alt, pool, integ, cunn, str, out string detail))
                    anyOk = true;
                else
                    altFail.Add(detail);
            }
            if (!anyOk)
                missing.Add(alts.Count > 1
                    ? "(" + string.Join(" OR ", altFail) + ")"
                    : altFail.FirstOrDefault() ?? req);
        }

        if (missing.Count > 0)
            return new AttemptResult(false, "Requirements not met: " + string.Join("; ", missing), false, 0);

        return new AttemptResult(true, "Mission requirements met.", true, ParsePoints(mission));
    }

    public static int ParsePoints(Card mission)
    {
        if (int.TryParse((mission.Points ?? "").Trim(), out int p)) return p;
        return 0;
    }


    /// <summary>
    /// Space mission attempt crew/present pool: only cards aboard the selected Attempting-Ship.
    /// Other own ships at the same spaceline location do NOT count (unless a card explicitly
    /// references location totals, e.g. total WEAPONS). Rulebook Mission Attempt; Glossary present/dilemma.
    /// </summary>
    public static List<Card> SpaceAttemptPool(
        IEnumerable<(string ShipKey, Card Card)> personnelAndEquipOnOwnShipsAtLocation,
        string attemptingShipKey)
    {
        if (string.IsNullOrEmpty(attemptingShipKey))
            return new List<Card>();
        return personnelAndEquipOnOwnShipsAtLocation
            .Where(x => string.Equals(x.ShipKey, attemptingShipKey, StringComparison.Ordinal))
            .Select(x => x.Card)
            .ToList();
    }

    /// <summary>
    /// DE mini-test: two ships same location; attempt with weak ship must not use strong ship's crew.
    /// Returns null if OK, else failure reason.
    /// </summary>
    public static string? VerifySpaceAttemptCrewScope()
    {
        static Card P(string name, string cls, string text, string aff = "Federation") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Affiliation = aff,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "6",
            CunningOrWeapons = "6",
            StrengthOrShields = "6"
        };

        var weak = P("Weak Ensign", "CIVILIAN", "CIVILIAN");
        var strongNav = P("Nav Ace", "OFFICER", "OFFICER Navigation Navigation");
        var strongEng = P("Eng Ace", "ENGINEER", "ENGINEER ENGINEER Physics");

        var atLocation = new List<(string ShipKey, Card Card)>
        {
            ("shipWeak", weak),
            ("shipStrong", strongNav),
            ("shipStrong", strongEng),
        };

        var attemptingOnly = SpaceAttemptPool(atLocation, "shipWeak");
        if (attemptingOnly.Count != 1 || !ReferenceEquals(attemptingOnly[0], weak))
            return $"pool weak ship: expected only Weak Ensign, got [{string.Join(", ", attemptingOnly.Select(c => c.Name))}]";

        var strongOnly = SpaceAttemptPool(atLocation, "shipStrong");
        if (strongOnly.Count != 2
            || !strongOnly.Contains(strongNav)
            || !strongOnly.Contains(strongEng))
            return $"pool strong ship: expected Nav+Eng, got [{string.Join(", ", strongOnly.Select(c => c.Name))}]";

        var emptyKey = SpaceAttemptPool(atLocation, "missing");
        if (emptyKey.Count != 0)
            return "pool missing ship key must be empty";

        // Solve: mission needs Navigation x2 + ENGINEER — weak ship alone fails; wrong all-ships pool would pass.
        var mission = new Card
        {
            Name = "Study Nebula",
            Type = "Mission",
            MissionDilemmaType = "[S]",
            Affiliation = "Federation",
            Text = "Navigation x2 + ENGINEER",
            Points = "30"
        };

        var wrongAllShips = atLocation.Select(x => x.Card).ToList();
        var wrongSolve = CanSolve(mission, wrongAllShips, dilemmasRemaining: 0, attemptingPlayer: 1, missionOwner: 1);
        if (!wrongSolve.Ok)
            return $"sanity: combined crew should solve ({wrongSolve.Reason})";

        var rightSolve = CanSolve(mission, attemptingOnly, dilemmasRemaining: 0, attemptingPlayer: 1, missionOwner: 1);
        if (rightSolve.Ok)
            return "weak attempting ship must NOT solve when strong ship is only present at location (not in attempt pool)";

        return null;
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
                return new AttemptResult(false, $"{attr}>{need} not reached (have {have}).", false, 0);
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
                return new AttemptResult(false, $"Skill \"{skill}\" missing in team.", false, 0);
        }

        return new AttemptResult(true, "Dilemma conditions (heuristic) met.", true, 0);
    }
}
