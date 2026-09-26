using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 7.4 Battle: Ship Battle (7.4.3) + Personnel Battle (7.4.2) + Rotation Damage (7.5.1.2).
/// Premiere-Kern: kein Battle Bridge / keine Tactics.
/// </summary>
public static class BattleRules
{
    public enum FireResult
    {
        Miss,
        Hit,
        DirectHit
    }

    public readonly record struct FireCalc(
        FireResult Result,
        int AttackTotal,
        int DefenseTotal,
        string Summary);

    public readonly record struct AttackCheck(
        bool Ok,
        string Reason);

    public readonly record struct DamageOutcome(
        int HullBefore,
        int HullAfter,
        bool Destroyed,
        bool NewlyDamaged,
        string Description);

    /// <summary>WEAPONS aus Ship/Facility-Attribut.</summary>
    public static int GetWeapons(Card card)
    {
        return ParseAttr(card.CunningOrWeapons);
    }

    /// <summary>SHIELDS aus Ship/Facility-Attribut.</summary>
    public static int GetShields(Card card)
    {
        return ParseAttr(card.StrengthOrShields);
    }

    /// <summary>Kurlan Naiskos: Attribute ×3 when all seven personnel types aboard (Class|Skill|Equipment).</summary>
    public static int KurlanMultiplier(IEnumerable<Card>? aboard)
    {
        if (aboard == null) return 1;
        var list = aboard.ToList();
        if (!list.Any(c => (c.Name ?? "").Equals("Kurlan Naiskos", StringComparison.OrdinalIgnoreCase)))
            return 1;
        return ArtifactRules.KurlanFullyStaffed(list) ? 3 : 1;
    }

    /// <summary>Apply Kurlan x3 to one ship attribute (RANGE/WEAPONS/SHIELDS).</summary>
    public static int ApplyKurlan(int attribute, IEnumerable<Card>? aboard) =>
        Math.Max(0, attribute) * KurlanMultiplier(aboard);


    // Rule: 12.11 · S.A.M.
    // Glossary: modifier order · attribute · attribute enhancements
    // AppA: Kurlan
    // Verb: triples · multiply · ApplyKurlan · KurlanMultiplier
    /// <summary>
    /// S.A.M. order for Kurlan: Set → Add/Subtract → Multiply/Divide.
    /// (printed + adds) × Kurlan — not printed×k then +adds.
    /// </summary>
    public static int AttributeAfterSam(int printed, int adds, IEnumerable<Card>? aboard) =>
        ApplyKurlan(printed + adds, aboard);

    /// <summary>
    /// Bonus over printed so ResolveFire(GetX + bonus) equals <see cref="AttributeAfterSam"/>.
    /// </summary>
    public static int AttributeBonusOverPrinted(int printed, int adds, IEnumerable<Card>? aboard) =>
        AttributeAfterSam(printed, adds, aboard) - Math.Max(0, printed);


    // Rule: 10.1.0.1 · 10.1 · 10.3.0.5 · 2.7 · 2.8
    // Glossary: personnel type · classification · skills · use (skills) · use (equipment)
    // Verb: ApplyKurlan VerifyKurlanMultiplier KurlanMultiplier HasSkill
    /// <summary>
    /// DE mini-test: Kurlan ×3 when artifact + seven types (Class|Skill|Equipment).
    /// Includes dual Class+Skill on one body and equipment skill grant.
    /// </summary>
    public static string? VerifyKurlanMultiplier()
    {
        var shipOnly = new List<Card> { new Card { Name = "Galaxy", Type = "Ship" } };
        if (KurlanMultiplier(shipOnly) != 1)
            return "no Kurlan => 1";
        var withArt = new List<Card>
        {
            new Card { Name = "Kurlan Naiskos", Type = "Artifact" },
            new Card { Name = "Galaxy", Type = "Ship" }
        };
        if (KurlanMultiplier(withArt) != 1)
            return "Kurlan without staff => 1";
        if (ApplyKurlan(9, withArt) != 9)
            return "ApplyKurlan without staff should be printed";

        var staffed = new List<Card>
        {
            new Card { Name = "Kurlan Naiskos", Type = "Artifact" },
            new Card { Name = "O1", Type = "Personnel", Class = "OFFICER" },
            new Card { Name = "E1", Type = "Personnel", Class = "ENGINEER" },
            new Card { Name = "M1", Type = "Personnel", Class = "MEDICAL" },
            new Card { Name = "S1", Type = "Personnel", Class = "SCIENCE" },
            new Card { Name = "Sec", Type = "Personnel", Class = "SECURITY" },
            new Card { Name = "V1", Type = "Personnel", Class = "V.I.P." },
            new Card { Name = "C1", Type = "Personnel", Class = "CIVILIAN" },
        };
        if (KurlanMultiplier(staffed) != 3)
            return "fully staffed Kurlan => 3";
        if (ApplyKurlan(9, staffed) != 27)
            return "RANGE 9 x3 must be 27";
        if (ApplyKurlan(8, staffed) != 24)
            return "WEAPONS 8 x3 must be 24";

        // Pepsch: one person Class OFFICER + Skill ENGINEER covers two types.
        var dual = new List<Card>
        {
            new Card { Name = "Kurlan Naiskos", Type = "Artifact" },
            new Card { Name = "Dual", Type = "Personnel", Class = "OFFICER", Text = "ENGINEER" },
            new Card { Name = "M1", Type = "Personnel", Class = "MEDICAL" },
            new Card { Name = "S1", Type = "Personnel", Class = "SCIENCE" },
            new Card { Name = "Sec", Type = "Personnel", Class = "SECURITY" },
            new Card { Name = "V1", Type = "Personnel", Class = "V.I.P." },
            new Card { Name = "C1", Type = "Personnel", Class = "CIVILIAN" },
        };
        if (KurlanMultiplier(dual) != 3)
            return "dual Class+Skill on one body must staff (OFFICER+ENGINEER)";

        // Equipment grant: Medical Kit gives MEDICAL to OFFICER (SkillEquipment catalog).
        var withKit = new List<Card>
        {
            new Card { Name = "Kurlan Naiskos", Type = "Artifact" },
            new Card { Name = "O1", Type = "Personnel", Class = "OFFICER" },
            new Card { Name = "E1", Type = "Personnel", Class = "ENGINEER" },
            new Card { Name = "Medical Kit", Type = "Equipment" },
            new Card { Name = "S1", Type = "Personnel", Class = "SCIENCE" },
            new Card { Name = "Sec", Type = "Personnel", Class = "SECURITY" },
            new Card { Name = "V1", Type = "Personnel", Class = "V.I.P." },
            new Card { Name = "C1", Type = "Personnel", Class = "CIVILIAN" },
        };
        if (KurlanMultiplier(withKit) != 3)
            return "equipment MEDICAL grant must help staff Kurlan";

        // S.A.M.: (printed + adds) × 3, not printed×3 + adds
        var samAboard = new List<Card>
        {
            new Card { Name = "Kurlan Naiskos", Type = "Artifact" },
            new Card { Name = "O1", Type = "Personnel", Class = "OFFICER" },
            new Card { Name = "E1", Type = "Personnel", Class = "ENGINEER" },
            new Card { Name = "M1", Type = "Personnel", Class = "MEDICAL" },
            new Card { Name = "S1", Type = "Personnel", Class = "SCIENCE" },
            new Card { Name = "Sec", Type = "Personnel", Class = "SECURITY" },
            new Card { Name = "V1", Type = "Personnel", Class = "V.I.P." },
            new Card { Name = "C1", Type = "Personnel", Class = "CIVILIAN" },
        };
        if (AttributeAfterSam(printed: 8, adds: 3, samAboard) != 33)
            return "S.A.M. (8+3)×3 must be 33";
        if (AttributeBonusOverPrinted(8, 3, samAboard) != 25)
            return "S.A.M. bonus over printed 8 with +3 adds must be 25";
        if (AttributeAfterSam(8, 3, samAboard) == 8 * 3 + 3)
            return "must not be printed×3 + adds";

        return null;
    }

    private static int ParseAttr(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        string s = raw.Trim();
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return Math.Max(0, v);
        var m = Regex.Match(s, @"\d+");
        if (m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
            return Math.Max(0, v);
        return 0;
    }

    /// <summary>
    /// Effektiver RANGE unter Rotation Damage: max. 5, wenn HULL ≥ 50 % beschädigt.
    /// </summary>
    public static int EffectiveRange(Card ship, int hullDamagePercent)
    {
        int baseRange = MovementRules.GetShipRange(ship);
        if (hullDamagePercent >= 50 && baseRange > 5)
            return 5;
        return baseRange;
    }

    /// <summary>
    /// Darf dieses Schiff einen Ship Battle initiieren?
    /// Leader (OFFICER oder Leadership), WEAPONS &gt; 0, Matching Affiliation HARD (G1),
    /// nicht gestoppt, Affiliation-Restriktion grob. Full Cmd/Stf staffing icons not required.
    /// 
    /// <paramref name="counterAttack"/> (G7): next-turn reply at same location vs involved/still-there opponents — no Leader, no affiliation restriction. Match+WEAPONS still required. Return Fire != Counter-Attack.</summary>
    public static AttackCheck CanInitiateShipAttack(
        Card attackerShip,
        IEnumerable<Card> crewOnBoard,
        int attackerOwner,
        Card target,
        int targetOwner,
        int hullDamagePercent,
        bool isStopped,
        string? wartimeVs = null,
        bool loreStaffed = false,
        bool counterAttack = false)
    {
        if (isStopped)
            return new AttackCheck(false, "Ship is stopped and cannot attack.");

        if (hullDamagePercent >= 100)
            return new AttackCheck(false, "Ship is destroyed.");

        if (attackerOwner == targetOwner)
            return new AttackCheck(false, "May only attack opposing cards.");

        if (!IsShipOrFacility(attackerShip))
            return new AttackCheck(false, "Only ships/facilities can initiate ship battle.");

        if (!IsShipOrFacility(target))
            return new AttackCheck(false, "Target must be a ship or facility.");

        int weapons = GetWeapons(attackerShip);
        if (weapons <= 0)
            return new AttackCheck(false, $"\"{attackerShip.Name}\" has no WEAPONS.");

        var crew = crewOnBoard?.ToList() ?? new List<Card>();
        if (!counterAttack && !HasLeader(crew) && !loreStaffed)
            return new AttackCheck(false, "No leader aboard (OFFICER or Leadership required).");

        // G1 / Spock: Matching Affiliation HARD for initiate (Leader+WEAPONS alone not enough).
        // Full staffing icons (Cmd/Stf) NOT required for Open Fire. Treaty/NA != Match (G2).
        if (IsShipCard(attackerShip) && !loreStaffed
            && !MovementRules.HasMatchingAffiliation(attackerShip, crew))
        {
            return new AttackCheck(false,
                "Cannot initiate ship battle: no matching-affiliation personnel aboard (Treaty/NA does not count as Match). Leader+WEAPONS alone is not enough.");
        }
        // G7: Counter-Attack relaxes affiliation restriction only (Fed may hit back).
        if (!counterAttack)
        {
            var affCheck = CheckAffiliationAttackRestriction(attackerShip, crew, target, wartimeVs);
            if (!affCheck.Ok)
                return affCheck;
        }

        return new AttackCheck(true, counterAttack ? "Counter-Attack allowed." : "Attack allowed.");
    }

    /// <summary>
    /// G6 / Spock Soll: Return Fire on the defender (firing) ship.
    /// Matching Affiliation HARD (Treaty/NA != Match). NO Leader required.
    /// Needs WEAPONS &gt; 0. Docked / cloaked / stopped / destroyed stay in UI.
    /// Full Cmd/Stf staffing icons not required.
    /// </summary>
    public static AttackCheck CanReturnFire(
        Card firingShip,
        IEnumerable<Card> crewOnBoard,
        bool loreStaffed = false)
    {
        if (!IsShipOrFacility(firingShip))
            return new AttackCheck(false, "Only ships/facilities can return fire.");

        int weapons = GetWeapons(firingShip);
        if (weapons <= 0)
            return new AttackCheck(false, $"{firingShip.Name} has no WEAPONS to return fire.");

        var crew = crewOnBoard?.ToList() ?? new List<Card>();
        // Matching HARD for ships (Rogue/loreStaffed bypass, same as G1).
        if (IsShipCard(firingShip) && !loreStaffed
            && !MovementRules.HasMatchingAffiliation(firingShip, crew))
        {
            return new AttackCheck(false,
                "Cannot return fire: no matching-affiliation personnel aboard (Treaty/NA does not count as Match). Matching required; Leader not required for RF.");
        }

        return new AttackCheck(true, "Return Fire allowed.");
    }

    public static bool IsShipOrFacility(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("ship") || t.Contains("facility") || t.Contains("outpost")
               || t.Contains("station") || t.Contains("headquarters") || t.Contains("nor");
    }

    public static bool IsShipCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("ship");
    }

    /// <summary>Leader = Personal mit OFFICER (Classification/Skill) oder Leadership.</summary>
    public static bool HasLeader(IEnumerable<Card> crew)
    {
        foreach (var p in crew)
        {
            // Classification ist oft OFFICER / COMMANDER etc.
            string cls = (p.Class ?? "").Trim();
            if (cls.Equals("OFFICER", StringComparison.OrdinalIgnoreCase))
                return true;

            var skillMap = MissionRules.ParsePersonnelSkills(p);
            if (skillMap.ContainsKey("OFFICER") || skillMap.ContainsKey("LEADERSHIP"))
                return true;

            string blob = (p.Text ?? "") + " " + (p.Characteristics ?? "");
            if (Regex.IsMatch(blob, @"\bOFFICER\b", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(blob, @"\bLeadership\b", RegexOptions.IgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Affiliation-Attack-Restrictions (Premiere-Kern, vereinfacht):
    /// - Federation: nur gegen Borg (sonst blocken mit Hinweis)
    /// - Die meisten: nicht gegen eigene Affiliation
    /// - Klingon / Non-Aligned: unrestricted (Premiere-relevant)
    /// </summary>
    public static AttackCheck CheckAffiliationAttackRestriction(
        Card attackerShip,
        IEnumerable<Card> crew,
        Card target,
        string? wartimeVs = null)
    {
        var forceAff = ReportingRules.GetAffiliations(attackerShip);
        foreach (var p in crew)
            foreach (var a in ReportingRules.GetAffiliations(p))
                forceAff.Add(a);

        var targetAff = ReportingRules.GetAffiliations(target);

        // NormalizeAffil liefert FED / KLI / ROM / NA / BORG …
        bool hasFed = forceAff.Contains("FED");
        bool hasKli = forceAff.Contains("KLI");
        bool hasNon = forceAff.Contains("NA");
        bool hasFer = forceAff.Contains("FER");
        bool targetBorg = targetAff.Contains("BORG");
        bool sameAff = forceAff.Overlaps(targetAff) && forceAff.Count > 0;

        // Federation force may not initiate battle except vs Borg,
        // unless a card (e.g. Wartime Conditions) names the defending affiliation.
        // A Non-Aligned or Klingon card in the same force does not lift this.
        if (hasFed)
        {
            if (targetBorg)
                return new AttackCheck(true, "Affiliation ok (FED vs Borg).");
            if (!string.IsNullOrEmpty(wartimeVs)
                && targetAff.Contains(wartimeVs))
                return new AttackCheck(true, $"Affiliation ok (Wartime Conditions vs {wartimeVs}).");
            return new AttackCheck(false,
                "Federation may not initiate battle except against Borg (unless a card allows it).");
        }

        bool unrestricted = hasKli || hasFer || hasNon;

        // Standard: nicht gegen eigene Affiliation (außer unrestricted)
        if (sameAff && !unrestricted)
        {
            return new AttackCheck(false,
                "Affiliation restriction: attacking your own affiliation is not permitted.");
        }

        return new AttackCheck(true, "Affiliation ok.");
    }

    /// <summary>
    /// Open Fire / Return Fire: ATTACK (WEAPONS-Summe) vs. DEFENSE (Ziel-SHIELDS).
    /// Hit wenn ATTACK &gt; DEFENSE; Direct Hit wenn ATTACK &gt; 2× DEFENSE.
    /// </summary>
    public static FireCalc ResolveFire(
        IReadOnlyList<(Card card, int weaponsBonus)> attackers,
        Card target,
        int targetShieldsBonus = 0,
        int facilityShieldsIfDocked = 0)
    {
        int attack = 0;
        foreach (var (c, bonus) in attackers)
            attack += GetWeapons(c) + bonus;

        int defense = GetShields(target) + targetShieldsBonus;
        // 50 % Facility-SHIELDS wenn angedockt
        if (facilityShieldsIfDocked > 0)
            defense += facilityShieldsIfDocked / 2;

        FireResult result;
        if (attack > defense * 2)
            result = FireResult.DirectHit;
        else if (attack > defense)
            result = FireResult.Hit;
        else
            result = FireResult.Miss;

        string summary = result switch
        {
            FireResult.DirectHit =>
                $"DIRECT HIT – ATTACK {attack} > 2× DEFENSE {defense}",
            FireResult.Hit =>
                $"HIT – ATTACK {attack} > DEFENSE {defense}",
            _ =>
                $"MISS – ATTACK {attack} ≤ DEFENSE {defense}"
        };

        return new FireCalc(result, attack, defense, summary);
    }

    /// <summary>
    /// Rotation Damage anwenden.
    /// Hit → +50 % HULL; Direct Hit → +100 % HULL.
    /// Ab 100 % → Destroyed (am Ende der Battle).
    /// </summary>
    public static DamageOutcome ApplyRotationDamage(int currentHullDamagePercent, FireResult fire)
    {
        if (fire == FireResult.Miss)
            return new DamageOutcome(currentHullDamagePercent, currentHullDamagePercent, false, false, "No damage.");

        int add = fire == FireResult.DirectHit ? 100 : 50;
        int before = Math.Clamp(currentHullDamagePercent, 0, 100);
        int after = Math.Min(100, before + add);
        bool destroyed = after >= 100;
        bool newly = after > before;

        string desc = destroyed
            ? $"Rotation Damage: HULL {before}% → 100% – DESTROYED."
            : $"Rotation Damage: HULL {before}% → {after}% " +
              $"(Cloak offline, RANGE max 5).";

        return new DamageOutcome(before, after, destroyed, newly, desc);
    }

    /// <summary>
    /// Winner: Force mit weniger total HULL-Schaden in diesem Battle.
    /// Gleichstand → kein Winner.
    /// </summary>
    public static string DetermineWinner(
        int attackerHullTakenThisBattle,
        int defenderHullTakenThisBattle)
    {
        if (attackerHullTakenThisBattle < defenderHullTakenThisBattle)
            return "Attacker";
        if (defenderHullTakenThisBattle < attackerHullTakenThisBattle)
            return "Defender";
        return "Tie";
    }

    // ========== Personnel / Away Team Battle (7.4.2) ==========

    public enum CombatOutcome
    {
        None,
        Stun,
        MortallyWound
    }

    public readonly record struct PairingResult(
        string AttackerName,
        int AttackerStr,
        string DefenderName,
        int DefenderStr,
        CombatOutcome VsDefender,
        CombatOutcome VsAttacker,
        string Summary);

    public readonly record struct PersonnelBattleResult(
        bool Ok,
        string Reason,
        IReadOnlyList<PairingResult> Pairings,
        int AttackerLiveStrength,
        int DefenderLiveStrength,
        string Winner,               // "Attacker" | "Defender" | "Tie"
        string? MortallyWoundedName, // aus Verlierer-Force (random)
        IReadOnlyList<string> KilledNames,
        string LogSummary);

    /// <summary>Gedruckte STRENGTH (ohne Equipment).</summary>
    public static int GetStrength(Card personnel)
    {
        var (_, _, str) = MissionRules.ParseAttributes(personnel);
        if (str > 0) return str;
        return ParseAttr(personnel.StrengthOrShields);
    }

    /// <summary>Effektive STRENGTH inkl. present Equipment.</summary>
    public static int GetStrength(Card personnel, IEnumerable<Card>? present, int owner)
    {
        if (present == null) return GetStrength(personnel);
        return ModifierRules.GetEffectiveStrength(personnel, present, owner);
    }

    public static bool IsPersonnelCombatant(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("personnel") || t.Contains("android") || t.Contains("animal");
    }

    public static bool IsKlingonPersonnel(Card c)
    {
        if (!IsPersonnelCombatant(c)) return false;
        var tokens = ReportingRules.ParseAffiliationTokens(c.Affiliation);
        if (tokens.Contains("KLI")) return true;
        string blob = ((c.Affiliation ?? "") + " " + (c.Text ?? ""));
        return blob.Contains("Klingon", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Personnel Battle initiieren: Leader in angreifender Force, Gegner present, Affiliation.
    /// </summary>
    public static AttackCheck CanInitiatePersonnelAttack(
        IReadOnlyList<Card> attackerForce,
        IReadOnlyList<Card> defenderForce,
        int attackerOwner,
        int defenderOwner,
        string? wartimeVs = null,
        bool leaderRequired = true)
    {
        if (attackerOwner == defenderOwner)
            return new AttackCheck(false, "You may only attack an opposing force.");

        var atk = attackerForce?.Where(IsPersonnelCombatant).ToList() ?? new List<Card>();
        var def = defenderForce?.Where(IsPersonnelCombatant).ToList() ?? new List<Card>();

        if (atk.Count == 0)
            return new AttackCheck(false, "No personnel combatants in the attacking force.");
        if (def.Count == 0)
            return new AttackCheck(false, "No opposing personnel present.");

        if (leaderRequired && !HasLeader(atk))
            return new AttackCheck(false, "No leader in the away team / crew (OFFICER or Leadership).");

        // Affiliation: repräsentatives Personal vs. repräsentatives Ziel
        var sampleAtk = atk[0];
        var sampleDef = def[0];
        var aff = CheckAffiliationAttackRestriction(sampleAtk, atk, sampleDef, wartimeVs);
        if (!aff.Ok)
            return aff;

        return new AttackCheck(true, "Personnel battle allowed.");
    }

    /// <summary>
    /// Volle Personnel Battle (Premiere-Kern, auto-Stun/Mortal bei Vorteil).
    /// Combat-Pile: Reihenfolge zufällig; Pairings bis eine Seite leer.
    /// STRENGTH &gt; Gegner → Stun; &gt; 2× → Mortally Wound (bevorzugt).
    /// Winner: höhere Live-STRENGTH (nicht stunned/mortal).
    /// Verlierer: 1 weiterer random combatant mortally (falls noch keiner).
    /// Resolution: Mortals sterben; Überlebende gestoppt (UI).
    /// presentAtk/presentDef: alle Karten am Host inkl. Equipment (für Modifier).
    /// </summary>
    public static PersonnelBattleResult ResolvePersonnelBattle(
        IReadOnlyList<Card> attackerForce,
        IReadOnlyList<Card> defenderForce,
        Random? rng = null,
        IReadOnlyList<Card>? presentAtk = null,
        IReadOnlyList<Card>? presentDef = null,
        int atkOwner = 1,
        int defOwner = 2,
        bool klingonStrengthDoubled = false)
    {
        rng ??= new Random();

        var atk = attackerForce.Where(IsPersonnelCombatant).ToList();
        var def = defenderForce.Where(IsPersonnelCombatant).ToList();
        if (atk.Count == 0 || def.Count == 0)
            return new PersonnelBattleResult(false, "Empty force.", Array.Empty<PairingResult>(),
                0, 0, "Tie", null, Array.Empty<string>(), "Aborted.");

        var atkPresent = presentAtk ?? atk;
        var defPresent = presentDef ?? def;

        // Combat piles (shuffle)
        var atkPile = atk.OrderBy(_ => rng.Next()).ToList();
        var defPile = def.OrderBy(_ => rng.Next()).ToList();

        var stunned = new HashSet<Card>();
        var mortal = new HashSet<Card>();
        var pairings = new List<PairingResult>();

        int i = 0, j = 0;
        while (i < atkPile.Count && j < defPile.Count)
        {
            // Nächste noch kampffähige
            while (i < atkPile.Count && (stunned.Contains(atkPile[i]) || mortal.Contains(atkPile[i])))
                i++;
            while (j < defPile.Count && (stunned.Contains(defPile[j]) || mortal.Contains(defPile[j])))
                j++;
            if (i >= atkPile.Count || j >= defPile.Count) break;

            var a = atkPile[i];
            var d = defPile[j];
            int sa = GetStrength(a, atkPresent, atkOwner);
            if (klingonStrengthDoubled && IsKlingonPersonnel(a))
                sa *= 2;
            int sd = GetStrength(d, defPresent, defOwner);

            CombatOutcome vsDef = CombatOutcome.None;
            CombatOutcome vsAtk = CombatOutcome.None;

            if (sa > sd * 2)
                vsDef = CombatOutcome.MortallyWound;
            else if (sa > sd)
                vsDef = CombatOutcome.Stun;
            else if (sd > sa * 2)
                vsAtk = CombatOutcome.MortallyWound;
            else if (sd > sa)
                vsAtk = CombatOutcome.Stun;

            // Auto-Apply (Hotseat: kein manuelles „choose to stun“)
            if (vsDef == CombatOutcome.MortallyWound) mortal.Add(d);
            else if (vsDef == CombatOutcome.Stun) stunned.Add(d);

            if (vsAtk == CombatOutcome.MortallyWound) mortal.Add(a);
            else if (vsAtk == CombatOutcome.Stun) stunned.Add(a);

            string sum =
                $"{a.Name} ({sa}) vs {d.Name} ({sd}): " +
                (vsDef != CombatOutcome.None
                    ? $"{d.Name} → {vsDef}"
                    : vsAtk != CombatOutcome.None
                        ? $"{a.Name} → {vsAtk}"
                        : "Tie");

            pairings.Add(new PairingResult(a.Name, sa, d.Name, sd, vsDef, vsAtk, sum));
            i++;
            j++;
        }

        // Live STRENGTH (weder stunned noch mortal), inkl. Rest in Pile
        int LiveStr(IEnumerable<Card> force, IEnumerable<Card> present, int owner, bool isAttacker) =>
            force.Where(c => !stunned.Contains(c) && !mortal.Contains(c))
                 .Sum(c => {
                     int s = GetStrength(c, present, owner);
                     if (isAttacker && klingonStrengthDoubled && IsKlingonPersonnel(c))
                         s *= 2;
                     return s;
                 });

        int atkLive = LiveStr(atk, atkPresent, atkOwner, isAttacker: true);
        int defLive = LiveStr(def, defPresent, defOwner, isAttacker: false);

        string winner;
        if (atkLive > defLive) winner = "Attacker";
        else if (defLive > atkLive) winner = "Defender";
        else winner = "Tie";

        // Loser: one additional random combatant is mortally wounded
        string? extraMortalName = null;
        if (winner == "Attacker")
        {
            var candidates = def.Where(c => !mortal.Contains(c)).ToList();
            if (candidates.Count > 0)
            {
                var pick = candidates[rng.Next(candidates.Count)];
                mortal.Add(pick);
                extraMortalName = pick.Name;
            }
        }
        else if (winner == "Defender")
        {
            var candidates = atk.Where(c => !mortal.Contains(c)).ToList();
            if (candidates.Count > 0)
            {
                var pick = candidates[rng.Next(candidates.Count)];
                mortal.Add(pick);
                extraMortalName = pick.Name;
            }
        }

        var killed = mortal.Select(c => c.Name).Distinct().ToList();

        var log = new List<string>
        {
            $"PERSONNEL BATTLE: {atk.Count} vs {def.Count}",
        };
        if (klingonStrengthDoubled)
            log.Add("Klingon Right of Vengeance: Klingon STRENGTH doubled for this battle.");
        log.AddRange(pairings.Select(p => "  " + p.Summary));
        log.Add($"Live STRENGTH: Attacker {atkLive} · Defender {defLive} → {winner}");
        if (extraMortalName != null)
            log.Add($"Loser extra: \"{extraMortalName}\" mortally wounded.");
        if (killed.Count > 0)
            log.Add("Killed: " + string.Join(", ", killed));
        log.Add("Survivors of both forces are stopped.");

        return new PersonnelBattleResult(
            true, "OK", pairings, atkLive, defLive, winner,
            extraMortalName, killed, string.Join("\n", log));
    }

    // ========== Ship Battle Orchestration & Plan (P2) ==========

    /// <summary>
    /// G7: Counter-Attack opportunity after being attacked in ship battle.
    /// Next-turn reply at the same location vs involved/still-there opponents.
    /// </summary>
    public sealed class CounterAttackOpportunity
    {
        public int EligiblePlayer { get; set; }
        public int LocationMissionInstanceId { get; set; }
        public HashSet<int> InvolvedOpponentInstanceIds { get; } = new();
        public bool Armed { get; set; }
    }

    public static bool IsArmedCounterAttackAt(CounterAttackOpportunity? ca, int locationMissionInstanceId, int player) =>
        ca != null
        && ca.Armed
        && ca.EligiblePlayer == player
        && locationMissionInstanceId > 0
        && ca.LocationMissionInstanceId == locationMissionInstanceId;

    public static bool IsCounterAttackTarget(CounterAttackOpportunity? ca, int targetInstanceId) =>
        ca != null
        && targetInstanceId > 0
        && ca.InvolvedOpponentInstanceIds.Contains(targetInstanceId);

    public static CounterAttackOpportunity? RegisterCounterAttack(int defenderPlayer, int locationMissionInstanceId, int attackerShipInstanceId)
    {
        if (defenderPlayer is < 1 or > 2) return null;
        if (attackerShipInstanceId <= 0 || locationMissionInstanceId <= 0) return null;
        var opp = new CounterAttackOpportunity
        {
            EligiblePlayer = defenderPlayer,
            LocationMissionInstanceId = locationMissionInstanceId,
            Armed = false
        };
        opp.InvolvedOpponentInstanceIds.Add(attackerShipInstanceId);
        return opp;
    }

    public static bool UpdateCounterAttackWindow(CounterAttackOpportunity? ca, int activePlayer, out bool armedNow, out bool expired)
    {
        armedNow = false;
        expired = false;
        if (ca == null) return false;
        if (!ca.Armed)
        {
            if (activePlayer == ca.EligiblePlayer)
            {
                ca.Armed = true;
                armedNow = true;
                return true;
            }
            return false;
        }
        if (activePlayer != ca.EligiblePlayer)
        {
            expired = true;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Check whether a ship/facility may initiate battle at its location before picking a target.
    /// </summary>
    public static AttackCheck CanShipInitiateBattleAtLocation(
        Card ship,
        IEnumerable<Card> crew,
        int owner,
        bool isStopped,
        bool isDocked,
        bool isCloaked,
        bool hasRequiredMove,
        bool counterAttack = false,
        bool loreStaffed = false,
        IReadOnlyList<TreatyRules.TreatyLink>? activeTreaties = null)
    {
        if (hasRequiredMove)
            return new AttackCheck(false, "Incoming Message: ship may not initiate battle (7.10). Return fire is allowed.");
        if (isStopped)
            return new AttackCheck(false, "Stopped ship cannot attack.");
        if (isDocked)
            return new AttackCheck(false, "Docked ship cannot initiate battle — undock first.");
        if (isCloaked)
            return new AttackCheck(false, "Cloaked ship cannot initiate battle — decloak first.");

        int weapons = GetWeapons(ship);
        if (weapons <= 0)
            return new AttackCheck(false, $"{ship.Name} has no WEAPONS.");

        var crewList = crew?.ToList() ?? new List<Card>();
        if (!counterAttack && !HasLeader(crewList) && !loreStaffed)
            return new AttackCheck(false, "No leader aboard (OFFICER or Leadership required).");

        if (IsShipCard(ship) && !loreStaffed
            && !MovementRules.HasMatchingAffiliation(ship, crewList, activeTreaties))
        {
            return new AttackCheck(false,
                "Cannot initiate ship battle: no matching-affiliation personnel aboard (Treaty/NA does not count as Match). Leader+WEAPONS alone is not enough.");
        }

        return new AttackCheck(true, "Initiation ok.");
    }

    /// <summary>
    /// Target filter for ship battle: opposing, ship or facility, not destroyed, not cloaked,
    /// and (if counter-attack) involved in the prior battle.
    /// </summary>
    public static bool IsLegalShipAttackTarget(
        Card attackerShip,
        int attackerOwner,
        Card target,
        int targetOwner,
        int targetHullDamagePercent,
        bool targetIsCloaked,
        bool counterAttack = false,
        IReadOnlyCollection<int>? counterAttackInvolvedTargetIds = null)
    {
        if (ReferenceEquals(attackerShip, target)) return false;
        if (attackerShip.InstanceId > 0 && target.InstanceId > 0 && attackerShip.InstanceId == target.InstanceId) return false;
        if (!IsShipOrFacility(target)) return false;
        if (attackerOwner == targetOwner) return false;
        if (targetHullDamagePercent >= 100) return false;
        if (targetIsCloaked) return false;
        if (counterAttack && (counterAttackInvolvedTargetIds == null || !counterAttackInvolvedTargetIds.Contains(target.InstanceId)))
            return false;
        return true;
    }

    public readonly record struct ReturnFireEligibility(
        bool CanReturnFire,
        bool WouldBeDestroyed,
        FireCalc PredictedOpenFire,
        string Reason);

    public static ReturnFireEligibility DecideReturnFireEligibility(
        Card attackerShip,
        int attackerWeaponsBonus,
        Card defenderCard,
        IEnumerable<Card> defenderCrew,
        int defenderShieldsBonus,
        int defenderFacilityShieldsIfDocked,
        int defenderHullDamagePercent,
        bool defenderIsStopped,
        bool defenderIsDocked,
        bool defenderIsCloaked,
        bool defenderLoreStaffed = false)
    {
        int defWeapons = GetWeapons(defenderCard);
        var predicted = ResolveFire(
            new[] { (attackerShip, attackerWeaponsBonus) },
            defenderCard,
            targetShieldsBonus: defenderShieldsBonus,
            facilityShieldsIfDocked: defenderFacilityShieldsIfDocked);
        bool wouldDestroy = ApplyRotationDamage(defenderHullDamagePercent, predicted.Result).Destroyed;

        if (defenderIsDocked)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, $"{defenderCard.Name} is docked - cannot return fire.");
        if (defenderIsCloaked)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, $"{defenderCard.Name} is cloaked - cannot return fire.");
        if (defenderIsStopped)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, $"{defenderCard.Name} is stopped - cannot return fire.");
        if (wouldDestroy)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, $"{defenderCard.Name} would be destroyed by open fire.");
        if (defWeapons <= 0)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, $"{defenderCard.Name} has no WEAPONS to return fire.");

        var rfMatch = CanReturnFire(defenderCard, defenderCrew, loreStaffed: defenderLoreStaffed);
        if (!rfMatch.Ok)
            return new ReturnFireEligibility(false, wouldDestroy, predicted, rfMatch.Reason);

        return new ReturnFireEligibility(true, wouldDestroy, predicted, "Return Fire allowed.");
    }

    public readonly record struct ShipBattlePlan(
        FireCalc OpenFire,
        DamageOutcome DefenderDamage,
        bool ReturnFireAttempted,
        bool ReturnFireExecuted,
        string? ReturnFireDenyReason,
        FireCalc? ReturnFire,
        DamageOutcome? AttackerDamage,
        int AttackerHullTaken,
        int DefenderHullTaken,
        string Winner,
        bool DefenderDestroyed,
        bool AttackerDestroyed,
        bool AttackerSurvivesAndStops,
        bool DefenderSurvivesAndStops,
        bool BorgShipDestroyedAwardPoints,
        bool DefenderMayCounterAttack,
        IReadOnlyList<string> LogLines,
        string Summary);

    public static ShipBattlePlan ExecuteShipBattlePlan(
        Card attackerShip,
        int attackerOwner,
        int attackerHullBefore,
        int attackerWeaponsBonus,
        int attackerShieldsBonus,
        bool attackerIsBorgShip,
        int attackerKurlanMult,
        Card defenderCard,
        int defenderOwner,
        int defenderHullBefore,
        int defenderWeaponsBonus,
        int defenderShieldsBonus,
        int defenderFacilityShieldsIfDocked,
        bool defenderIsDocked,
        bool defenderIsCloaked,
        bool defenderLoreStaffed,
        IEnumerable<Card> defenderCrew,
        bool returnFireRequested,
        int defenderKurlanMult,
        int locationMissionInstanceId = 0)
    {
        var logLines = new List<string>
        {
            $"SHIP BATTLE: {attackerShip.Name} (S{attackerOwner}) → {defenderCard.Name} (S{defenderOwner})"
        };

        // 1. Open Fire
        var openFire = ResolveFire(
            new[] { (attackerShip, attackerWeaponsBonus) },
            defenderCard,
            targetShieldsBonus: defenderShieldsBonus,
            facilityShieldsIfDocked: defenderFacilityShieldsIfDocked);
        logLines.Add($"Open Fire: {openFire.Summary}" + (attackerKurlanMult > 1 ? $" (Kurlan ×{attackerKurlanMult})" : ""));

        var defDmg = ApplyRotationDamage(defenderHullBefore, openFire.Result);
        int atkHullTaken = 0;
        int defHullTaken = Math.Max(0, defDmg.HullAfter - defDmg.HullBefore);

        if (defDmg.NewlyDamaged)
            logLines.Add($"  Defender: {defDmg.Description}");

        // 2. Return Fire
        FireCalc? returnCalc = null;
        DamageOutcome? atkDmg = null;
        bool returnExecuted = false;
        string? rfDenyReason = null;

        if (returnFireRequested && !defDmg.Destroyed)
        {
            if (defenderIsDocked)
            {
                rfDenyReason = "Return Fire denied: defender is docked.";
                logLines.Add(rfDenyReason);
            }
            else if (defenderIsCloaked)
            {
                rfDenyReason = "Return Fire denied: defender is cloaked.";
                logLines.Add(rfDenyReason);
            }
            else
            {
                var rfCheck = CanReturnFire(defenderCard, defenderCrew, loreStaffed: defenderLoreStaffed);
                if (!rfCheck.Ok)
                {
                    rfDenyReason = $"Return Fire denied: {rfCheck.Reason}";
                    logLines.Add(rfDenyReason);
                }
                else
                {
                    returnExecuted = true;
                    returnCalc = ResolveFire(
                        new[] { (defenderCard, defenderWeaponsBonus) },
                        attackerShip,
                        targetShieldsBonus: attackerShieldsBonus);
                    logLines.Add($"Return Fire: {returnCalc.Value.Summary}" + (defenderKurlanMult > 1 ? $" (Kurlan ×{defenderKurlanMult})" : ""));

                    atkDmg = ApplyRotationDamage(attackerHullBefore, returnCalc.Value.Result);
                    atkHullTaken = Math.Max(0, atkDmg.Value.HullAfter - atkDmg.Value.HullBefore);
                    if (atkDmg.Value.NewlyDamaged)
                        logLines.Add($"  Attacker: {atkDmg.Value.Description}");
                }
            }
        }
        else if (returnFireRequested && defDmg.Destroyed)
        {
            rfDenyReason = "Return Fire skipped (defender already destroyed).";
            logLines.Add(rfDenyReason);
        }

        // 3. Winner
        string winner = DetermineWinner(atkHullTaken, defHullTaken);
        logLines.Add($"Winner (HULL damage): {winner}");

        // 4. Destruction & Status
        bool defDestroyed = defDmg.Destroyed || defDmg.HullAfter >= 100;
        bool atkDestroyed = (atkDmg?.Destroyed ?? false) || (atkDmg.HasValue && atkDmg.Value.HullAfter >= 100);

        if (defDestroyed)
            logLines.Add($"DESTROYED: {defenderCard.Name} (P{defenderOwner}) — Escape Pod may respond.");
        if (atkDestroyed)
            logLines.Add($"DESTROYED: {attackerShip.Name} (P{attackerOwner}) — Escape Pod may respond.");

        bool atkSurvivesAndStops = !attackerIsBorgShip && !atkDestroyed;
        bool defSurvivesAndStops = !defDestroyed;
        bool borgDestroyed = attackerIsBorgShip && atkDestroyed;
        bool defMayCounter = locationMissionInstanceId > 0 && !atkDestroyed && !attackerIsBorgShip;

        if (borgDestroyed)
            logLines.Add($"Borg Ship dilemma destroyed by return fire (+{BorgShipRules.PointsOnDestroyed}).");

        string summary = string.Join("\n", logLines);

        return new ShipBattlePlan(
            openFire,
            defDmg,
            returnFireRequested,
            returnExecuted,
            rfDenyReason,
            returnCalc,
            atkDmg,
            atkHullTaken,
            defHullTaken,
            winner,
            defDestroyed,
            atkDestroyed,
            atkSurvivesAndStops,
            defSurvivesAndStops,
            borgDestroyed,
            defMayCounter,
            logLines,
            summary);
    }

    public static bool CanEscapePodRespond(bool isShip, bool hasEscapePodInHand, bool hasCrewForEscapePod, bool resolvingDestroy) =>
        !resolvingDestroy && isShip && hasEscapePodInHand && hasCrewForEscapePod;

    public static bool CanOfferPersonnelBattle(bool hasOwnUnstoppedPersonnel, bool hasOpposingPersonnelPresent) =>
        hasOwnUnstoppedPersonnel && hasOpposingPersonnelPresent;

    public static string? VerifyBattleRulesPlan()
    {
        var attacker = new Card { Name = "Vor'Cha", Type = "Ship", CunningOrWeapons = "9", StrengthOrShields = "8", Affiliation = "Klingon" };
        var defender = new Card { Name = "Enterprise", Type = "Ship", CunningOrWeapons = "8", StrengthOrShields = "8", Affiliation = "Federation" };
        var fedCrew = new List<Card> { new Card { Name = "Riker", Type = "Personnel", Class = "OFFICER", Affiliation = "Federation" } };

        var elig = DecideReturnFireEligibility(attacker, 0, defender, fedCrew, 0, 0, 0, false, false, false);
        if (!elig.CanReturnFire)
            return "Defender Enterprise should be eligible for return fire";

        var plan = ExecuteShipBattlePlan(
            attacker, 1, 0, 0, 0, false, 1,
            defender, 2, 0, 0, 0, 0, false, false, false, fedCrew, true, 1, 100);

        if (plan.OpenFire.Result != FireResult.Hit)
            return "Vor'Cha weapons 9 vs shields 8 must be Hit";
        if (plan.DefenderDamage.HullAfter != 50)
            return "Hit must apply 50% damage";
        if (!plan.ReturnFireExecuted)
            return "Return fire must be executed";
        if (plan.ReturnFire?.Result != FireResult.Miss)
            return "Enterprise weapons 8 vs shields 8 must be Miss";

        return null;
    }
}
