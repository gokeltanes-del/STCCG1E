using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Effektive Attribute/Skills: gedruckte Werte + Modifier (Equipment present, später Events).
/// Premiere-Kern: 11 Equipment-Karten als Katalog.
/// </summary>
public static class ModifierRules
{
    /// <summary>Table-wide events that change printed personnel (UI sets while events persist).</summary>
    public static class TableBuffs
    {
        public static int YellowAlertPlayer;
        public static int LowerDecksPlayer;
    }

    public enum ModifierKind
    {
        AttrBonus,
        SkillGrant,
        /// <summary>Skill present on card but disabled (e.g. Two-Dimensional Creatures → Empathy).</summary>
        SkillDisable
    }

    public readonly record struct Modifier(
        string SourceName,
        ModifierKind Kind,
        string StatOrSkill,
        int Amount,
        int Owner);

    public sealed class EffectiveProfile
    {
        public required Card Card { get; init; }
        public int Integrity { get; init; }
        public int Cunning { get; init; }
        public int Strength { get; init; }
        public int BaseIntegrity { get; init; }
        public int BaseCunning { get; init; }
        public int BaseStrength { get; init; }
        public Dictionary<string, int> Skills { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> BaseSkills { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Modifier> Applied { get; init; } = new();

        public bool HasChanges =>
            Integrity != BaseIntegrity || Cunning != BaseCunning || Strength != BaseStrength
            || Applied.Any(m => m.Kind is ModifierKind.SkillGrant or ModifierKind.SkillDisable);
    }

    public sealed class TeamSummary
    {
        public int PersonnelCount { get; init; }
        public int EquipmentCount { get; init; }
        public int Integrity { get; init; }
        public int Cunning { get; init; }
        public int Strength { get; init; }
        public int BaseIntegrity { get; init; }
        public int BaseCunning { get; init; }
        public int BaseStrength { get; init; }
        public Dictionary<string, int> Skills { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> EquipmentNames { get; init; } = new();
        public List<Modifier> Applied { get; init; } = new();
    }

    // ---- Premiere Equipment-Katalog ----

    private enum AttrTarget { Strength, Cunning }

    private sealed record AttrEquipDef(
        string Name,
        AttrTarget Target,
        int Amount,
        // mind. 1 present Personnel mit einer dieser Affiliations (normalisiert FED/KLI/…)
        HashSet<string> RequiredAnyAffil);

    private sealed record SkillEquipDef(
        string Name,
        // Classification die den Skill bekommt (OFFICER, SCIENCE, ENGINEER)
        string RequiredClass,
        string GrantedSkill);

    private static readonly List<AttrEquipDef> AttrEquipment = new()
    {
        new("Starfleet Type II Phaser", AttrTarget.Strength, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FED", "NA" }),
        new("Klingon Disruptor", AttrTarget.Strength, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KLI", "NA" }),
        new("Romulan Disruptor", AttrTarget.Strength, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ROM", "NA" }),
        new("Federation PADD", AttrTarget.Cunning, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FED" }),
        new("Klingon PADD", AttrTarget.Cunning, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KLI" }),
        new("Romulan PADD", AttrTarget.Cunning, 2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ROM" }),
    };

    private static readonly List<SkillEquipDef> SkillEquipment = new()
    {
        new("Medical Kit", "OFFICER", "MEDICAL"),
        new("Medical Tricorder", "SCIENCE", "MEDICAL"),
        new("Engineering Kit", "OFFICER", "ENGINEER"),
        new("Engineering PADD", "SCIENCE", "ENGINEER"),
        new("Tricorder", "ENGINEER", "SCIENCE"),
    };

    /// <summary>
    /// Premiere SkillEquipment catalog query: does this equipment grant the named skill?
    /// Mirrors existing SkillEquipment only (Medical Kit/Medical Tricorder -> MEDICAL;
    /// plain Tricorder grants SCIENCE, not MEDICAL). No new grant rules.
    /// </summary>
    public static bool EquipmentGrantsSkill(Card equipment, string skill)
    {
        if (equipment == null || string.IsNullOrWhiteSpace(skill) || !IsEquipmentCard(equipment))
            return false;
        string name = equipment.Name ?? "";
        foreach (var def in SkillEquipment)
        {
            if (!NamesMatch(name, def.Name)) continue;
            if (def.GrantedSkill.Equals(skill, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>Present equipment cards that grant <paramref name="skill"/> per SkillEquipment catalog.</summary>
    public static List<Card> EquipmentGrantingSkill(IEnumerable<Card>? present, string skill)
    {
        var list = new List<Card>();
        if (present == null) return list;
        foreach (var c in present)
            if (EquipmentGrantsSkill(c, skill))
                list.Add(c);
        return list;
    }

    /// <summary>
    /// SkillEquipment RequiredClass for an equipment that grants <paramref name="skill"/>
    /// (Medical Kit -> OFFICER, Medical Tricorder -> SCIENCE). Null if no match.
    /// </summary>
    public static string? EquipmentRequiredClassForSkill(Card equipment, string skill)
    {
        if (equipment == null || string.IsNullOrWhiteSpace(skill) || !IsEquipmentCard(equipment))
            return null;
        string name = equipment.Name ?? "";
        foreach (var def in SkillEquipment)
        {
            if (!NamesMatch(name, def.Name)) continue;
            if (def.GrantedSkill.Equals(skill, StringComparison.OrdinalIgnoreCase))
                return def.RequiredClass;
        }
        return null;
    }

    /// <summary>True when personnel Classification matches equipment grant RequiredClass for skill.</summary>
    public static bool PersonnelMatchesEquipmentGrant(Card personnel, Card equipment, string skill)
    {
        if (personnel == null) return false;
        string? req = EquipmentRequiredClassForSkill(equipment, skill);
        if (string.IsNullOrEmpty(req)) return false;
        return (personnel.Class ?? "").Equals(req, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsEquipmentCard(Card c) =>
        (c.Type ?? "").Contains("equipment", StringComparison.OrdinalIgnoreCase)
        || ArtifactRules.IsVaronT(c)
        || ArtifactRules.IsInterphaseGenerator(c)
        || ArtifactRules.IsDatasHead(c);

    public static bool IsPersonnelCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("personnel") || t.Contains("android") || t.Contains("animal");
    }

    public static bool IsUniversalNonHolo(Card c)
    {
        string u = $"{c.Uniqueness} {c.Icons} {c.Name}";
        bool univ = u.Contains("univ", StringComparison.OrdinalIgnoreCase)
                    || (c.Name ?? "").StartsWith("Universal", StringComparison.OrdinalIgnoreCase)
                    || (c.Icons ?? "").Contains("♦")
                    || (c.Uniqueness ?? "").Contains("♦");
        string blob = $"{c.Type} {c.Icons} {c.Characteristics} {c.Text}";
        bool holo = blob.Contains("[Holo]", StringComparison.OrdinalIgnoreCase)
                    || blob.Contains("Hologram", StringComparison.OrdinalIgnoreCase);
        return univ && !holo;
    }

    /// <summary>
    /// Effektives Profil einer Personnel-Karte bei given present-Karten (gleicher Host, inkl. Eq).
    /// Nur Modifier des <paramref name="owner"/> greifen auf „your personnel“.
    /// </summary>

    /// <summary>Remove disabled skills (exact name match, ignore case) and record SkillDisable modifiers.</summary>
    public static void ApplySkillDisables(
        Dictionary<string, int> skills,
        List<Modifier> applied,
        IEnumerable<string>? disabledSkills,
        int owner,
        string sourceName = "Two-Dimensional Creatures")
    {
        if (disabledSkills == null) return;
        foreach (var raw in disabledSkills)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string name = raw.Trim();
            var hit = skills.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (hit == null) continue;
            skills.Remove(hit);
            applied.Add(new Modifier(sourceName, ModifierKind.SkillDisable, hit, 0, owner));
        }
    }

    public const string TsiolkovskySourceName = "Tsiolkovsky Infection";

    /// <summary>
    /// Tsiolkovsky Infection: each personnel loses printed first-listed skill (not cumulative).
    /// </summary>
    public static void ApplyFirstListedSkillLoss(
        Dictionary<string, int> skills,
        List<Modifier> applied,
        Card subject,
        int owner)
    {
        if (skills == null || applied == null || subject == null) return;
        // Not cumulative: one strip per personnel from this source.
        if (applied.Any(m => m.Kind == ModifierKind.SkillDisable
                             && m.SourceName.Equals(TsiolkovskySourceName, StringComparison.OrdinalIgnoreCase)))
            return;

        string? first = null;
        if (subject.FramedOfMind && subject.FrameSkills != null && subject.FrameSkills.Count > 0)
            first = subject.FrameSkills[0];
        first ??= MissionRules.FirstListedSkill(subject);
        if (string.IsNullOrWhiteSpace(first)) return;

        var hit = skills.Keys.FirstOrDefault(k => k.Equals(first, StringComparison.OrdinalIgnoreCase));
        if (hit == null) return;
        skills.Remove(hit);
        applied.Add(new Modifier(TsiolkovskySourceName, ModifierKind.SkillDisable, hit, 0, owner));
    }

    public static EffectiveProfile ResolvePersonnel(
        Card subject,
        IEnumerable<Card> presentCards,
        int owner,
        IEnumerable<string>? disabledSkills = null,
        bool loseFirstListedSkill = false)
    {
        var present = presentCards?.ToList() ?? new List<Card>();
        var (bi, bc, bs) = MissionRules.ParseAttributes(subject);
        var baseSkills = MissionRules.ParsePersonnelSkills(subject);

        int integ = bi, cunn = bc, str = bs;
        var skills = new Dictionary<string, int>(baseSkills, StringComparer.OrdinalIgnoreCase);
        var applied = new List<Modifier>();

        if (subject.FramedOfMind)
        {
            var kept = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (subject.FrameSkills != null)
            {
                foreach (var s in subject.FrameSkills)
                    if (!string.IsNullOrWhiteSpace(s))
                        kept[s] = baseSkills.GetValueOrDefault(s, 1);
            }
            var fomApplied = new List<Modifier>
            {
                new("Frame of Mind", ModifierKind.AttrBonus, "ALL", 0, owner)
            };
            ApplySkillDisables(kept, fomApplied, disabledSkills, owner);
            if (loseFirstListedSkill)
                ApplyFirstListedSkillLoss(kept, fomApplied, subject, owner);
            return new EffectiveProfile
            {
                Card = subject,
                Integrity = 3,
                Cunning = 3,
                Strength = 3,
                BaseIntegrity = bi,
                BaseCunning = bc,
                BaseStrength = bs,
                Skills = kept,
                BaseSkills = baseSkills,
                Applied = fomApplied
            };
        }

        if (!IsPersonnelCard(subject))
        {
            return new EffectiveProfile
            {
                Card = subject,
                Integrity = integ,
                Cunning = cunn,
                Strength = str,
                BaseIntegrity = bi,
                BaseCunning = bc,
                BaseStrength = bs,
                Skills = skills,
                BaseSkills = baseSkills,
                Applied = applied
            };
        }

        var yourPersonnel = present.Where(IsPersonnelCard).ToList();
        var yourEquipment = present.Where(IsEquipmentCard).ToList();

        // Affiliation-Set aller eigenen present Personnel (für Equipment-Gates)
        var presentAffils = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in yourPersonnel)
            foreach (var a in ReportingRules.GetAffiliations(p))
                presentAffils.Add(a);

        string subjectClass = (subject.Class ?? "").Trim().ToUpperInvariant();

        foreach (var eq in yourEquipment)
        {
            string eqName = eq.Name ?? "";

            // Attribut-Boni (cumulative)
            foreach (var def in AttrEquipment)
            {
                if (!NamesMatch(eqName, def.Name)) continue;
                if (!presentAffils.Overlaps(def.RequiredAnyAffil)) continue;

                if (def.Target == AttrTarget.Strength)
                {
                    str += def.Amount;
                    applied.Add(new Modifier(eqName, ModifierKind.AttrBonus, "STRENGTH", def.Amount, owner));
                }
                else
                {
                    cunn += def.Amount;
                    applied.Add(new Modifier(eqName, ModifierKind.AttrBonus, "CUNNING", def.Amount, owner));
                }
            }

        }

        // Skill grants: one matching equipment covers all of RequiredClass present.
        // Multiple kits do NOT stack extra levels on one personnel.
        foreach (var def in SkillEquipment)
        {
            if (!subjectClass.Equals(def.RequiredClass, StringComparison.OrdinalIgnoreCase))
                continue;
            var match = yourEquipment.FirstOrDefault(eq => NamesMatch(eq.Name ?? "", def.Name));
            if (match == null) continue;
            skills[def.GrantedSkill] = skills.GetValueOrDefault(def.GrantedSkill) + 1;
            applied.Add(new Modifier(match.Name ?? def.Name, ModifierKind.SkillGrant, def.GrantedSkill, 1, owner));
        }

        if (TableBuffs.YellowAlertPlayer == owner)
        {
            cunn += 1;
            applied.Add(new Modifier("Yellow Alert", ModifierKind.AttrBonus, "CUNNING", 1, owner));
        }
        if (TableBuffs.LowerDecksPlayer == owner
            && IsUniversalNonHolo(subject))
        {
            integ += 2; cunn += 2; str += 2;
            applied.Add(new Modifier("Lower Decks", ModifierKind.AttrBonus, "ALL", 2, owner));
        }

        // Artifact-as-Equipment: Varon-T Disruptor (STRENGTH ×2, own personnel)
        foreach (var a in present.Where(ArtifactRules.IsArtifact))
        {
            if (!ArtifactRules.IsVaronT(a)) continue;
            int before = str;
            str *= 2;
            applied.Add(new Modifier(a.Name ?? "Varon-T", ModifierKind.AttrBonus, "STRENGTH",
                str - before, owner));
        }

        ApplySkillDisables(skills, applied, disabledSkills, owner);
        if (loseFirstListedSkill)
            ApplyFirstListedSkillLoss(skills, applied, subject, owner);

        return new EffectiveProfile
        {
            Card = subject,
            Integrity = integ,
            Cunning = cunn,
            Strength = str,
            BaseIntegrity = bi,
            BaseCunning = bc,
            BaseStrength = bs,
            Skills = skills,
            BaseSkills = baseSkills,
            Applied = applied
        };
    }

    /// <summary>Team-Summen inkl. Equipment-Boni (nur Personnel des Owners).</summary>
    public static TeamSummary SummarizeTeam(
        IEnumerable<Card> presentCards,
        int owner,
        IEnumerable<string>? disabledSkills = null,
        bool loseFirstListedSkill = false)
    {
        var present = presentCards?.ToList() ?? new List<Card>();
        var personnel = present.Where(IsPersonnelCard).ToList();
        var equipment = present.Where(IsEquipmentCard).ToList();

        int integ = 0, cunn = 0, str = 0;
        int bi = 0, bc = 0, bs = 0;
        var skills = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var allMods = new List<Modifier>();

        foreach (var p in personnel)
        {
            var ep = ResolvePersonnel(p, present, owner, disabledSkills, loseFirstListedSkill);
            integ += ep.Integrity;
            cunn += ep.Cunning;
            str += ep.Strength;
            bi += ep.BaseIntegrity;
            bc += ep.BaseCunning;
            bs += ep.BaseStrength;
            foreach (var kv in ep.Skills)
                skills[kv.Key] = skills.GetValueOrDefault(kv.Key) + kv.Value;
            allMods.AddRange(ep.Applied);
        }

        return new TeamSummary
        {
            PersonnelCount = personnel.Count,
            EquipmentCount = equipment.Count,
            Integrity = integ,
            Cunning = cunn,
            Strength = str,
            BaseIntegrity = bi,
            BaseCunning = bc,
            BaseStrength = bs,
            Skills = skills,
            EquipmentNames = equipment.Select(e => e.Name ?? "?").Distinct().ToList(),
            Applied = allMods
        };
    }

    public static int GetEffectiveStrength(Card personnel, IEnumerable<Card>? present, int owner)
    {
        if (present == null)
            return MissionRules.ParseAttributes(personnel).str;
        return ResolvePersonnel(personnel, present, owner).Strength;
    }

    private static bool NamesMatch(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        return a.Trim().Equals(b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Kurztext für Detail-UI.</summary>
    public static string FormatProfileLines(EffectiveProfile ep)
    {
        var lines = new List<string>();
        lines.Add(FormatAttrLine("INTEGRITY", ep.BaseIntegrity, ep.Integrity, ep.Applied, "INTEGRITY"));
        lines.Add(FormatAttrLine("CUNNING", ep.BaseCunning, ep.Cunning, ep.Applied, "CUNNING"));
        lines.Add(FormatAttrLine("STRENGTH", ep.BaseStrength, ep.Strength, ep.Applied, "STRENGTH"));

        if (ep.Skills.Count > 0)
        {
            var classKeys = new HashSet<string>(MissionRules.Classifications, StringComparer.OrdinalIgnoreCase);
            var classParts = ep.Skills.Where(kv => classKeys.Contains(kv.Key))
                .OrderBy(k => k.Key)
                .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
            var skillParts = ep.Skills.Where(kv => !classKeys.Contains(kv.Key))
                .OrderBy(k => k.Key)
                .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
            if (classParts.Any())
                lines.Add("Classification: " + string.Join(", ", classParts));
            if (skillParts.Any())
                lines.Add("Skills: " + string.Join(", ", skillParts));
        }

        var grants = ep.Applied.Where(m => m.Kind == ModifierKind.SkillGrant).ToList();
        if (grants.Count > 0)
            lines.Add("+ " + string.Join(", ",
                grants.Select(g => $"{g.StatOrSkill} ({g.SourceName})")));

        var disabled = ep.Applied.Where(m => m.Kind == ModifierKind.SkillDisable).ToList();
        if (disabled.Count > 0)
            lines.Add("Disabled: " + string.Join(", ",
                disabled.Select(d => $"{d.StatOrSkill} ({d.SourceName})")));

        return string.Join("\n", lines);
    }

    private static string FormatAttrLine(
        string label, int bas, int eff, List<Modifier> mods, string statKey)
    {
        if (eff == bas)
            return $"{label}  {bas}";
        var sources = mods
            .Where(m => m.Kind == ModifierKind.AttrBonus
                        && m.StatOrSkill.Equals(statKey, StringComparison.OrdinalIgnoreCase))
            .Select(m => $"+{m.Amount} {m.SourceName}");
        string src = string.Join(", ", sources);
        return string.IsNullOrEmpty(src)
            ? $"{label}  {bas} → {eff}"
            : $"{label}  {bas} → {eff}  ({src})";
    }

    public static string FormatTeamSummary(TeamSummary t, int owner)
    {
        var lines = new List<string>
        {
            $"P{owner} Team — {t.PersonnelCount} Pers · {t.EquipmentCount} Eq"
        };

        string Attr(string name, int bas, int eff) =>
            bas == eff ? $"{name} {eff}" : $"{name} {eff} (+{eff - bas})";

        lines.Add($"Σ {Attr("INT", t.BaseIntegrity, t.Integrity)}   " +
                  $"{Attr("CUN", t.BaseCunning, t.Cunning)}   " +
                  $"{Attr("STR", t.BaseStrength, t.Strength)}");

        if (t.Skills.Count > 0)
        {
            var sk = t.Skills.OrderBy(k => k.Key)
                .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
            lines.Add("Skills: " + string.Join("  ", sk));
        }

        if (t.EquipmentNames.Count > 0)
            lines.Add("Eq: " + string.Join(", ", t.EquipmentNames));

        return string.Join("\n", lines);
    }
}
