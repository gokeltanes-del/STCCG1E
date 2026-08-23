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
    public enum ModifierKind
    {
        AttrBonus,
        SkillGrant
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
            || Applied.Any(m => m.Kind == ModifierKind.SkillGrant);
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

    public static bool IsEquipmentCard(Card c) =>
        (c.Type ?? "").Contains("equipment", StringComparison.OrdinalIgnoreCase)
        || ArtifactRules.IsVaronT(c)
        || ArtifactRules.IsInterphaseGenerator(c);

    public static bool IsPersonnelCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("personnel") || t.Contains("android") || t.Contains("animal");
    }

    /// <summary>
    /// Effektives Profil einer Personnel-Karte bei given present-Karten (gleicher Host, inkl. Eq).
    /// Nur Modifier des <paramref name="owner"/> greifen auf „your personnel“.
    /// </summary>
    public static EffectiveProfile ResolvePersonnel(
        Card subject,
        IEnumerable<Card> presentCards,
        int owner)
    {
        var present = presentCards?.ToList() ?? new List<Card>();
        var (bi, bc, bs) = MissionRules.ParseAttributes(subject);
        var baseSkills = MissionRules.ParsePersonnelSkills(subject);

        int integ = bi, cunn = bc, str = bs;
        var skills = new Dictionary<string, int>(baseSkills, StringComparer.OrdinalIgnoreCase);
        var applied = new List<Modifier>();

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

            // Skill-Grants nach Classification
            foreach (var def in SkillEquipment)
            {
                if (!NamesMatch(eqName, def.Name)) continue;
                if (!subjectClass.Equals(def.RequiredClass, StringComparison.OrdinalIgnoreCase))
                    continue;

                skills[def.GrantedSkill] = skills.GetValueOrDefault(def.GrantedSkill) + 1;
                applied.Add(new Modifier(eqName, ModifierKind.SkillGrant, def.GrantedSkill, 1, owner));
            }
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
        int owner)
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
            var ep = ResolvePersonnel(p, present, owner);
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
            $"S{owner} Team — {t.PersonnelCount} Pers · {t.EquipmentCount} Eq"
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