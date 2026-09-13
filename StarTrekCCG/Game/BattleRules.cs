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

    /// <summary>Kurlan Naiskos: Attribute ×3 wenn alle Classifications an Bord.</summary>
    public static int KurlanMultiplier(IEnumerable<Card>? aboard)
    {
        if (aboard == null) return 1;
        var list = aboard.ToList();
        if (!list.Any(c => (c.Name ?? "").Equals("Kurlan Naiskos", StringComparison.OrdinalIgnoreCase)))
            return 1;
        return ArtifactRules.KurlanFullyStaffed(list) ? 3 : 1;
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

    /// <summary>
    /// Personnel Battle initiieren: Leader in angreifender Force, Gegner present, Affiliation.
    /// </summary>
    public static AttackCheck CanInitiatePersonnelAttack(
        IReadOnlyList<Card> attackerForce,
        IReadOnlyList<Card> defenderForce,
        int attackerOwner,
        int defenderOwner,
        string? wartimeVs = null)
    {
        if (attackerOwner == defenderOwner)
            return new AttackCheck(false, "You may only attack an opposing force.");

        var atk = attackerForce?.Where(IsPersonnelCombatant).ToList() ?? new List<Card>();
        var def = defenderForce?.Where(IsPersonnelCombatant).ToList() ?? new List<Card>();

        if (atk.Count == 0)
            return new AttackCheck(false, "No personnel combatants in the attacking force.");
        if (def.Count == 0)
            return new AttackCheck(false, "No opposing personnel present.");

        if (!HasLeader(atk))
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
        int defOwner = 2)
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
        int LiveStr(IEnumerable<Card> force, IEnumerable<Card> present, int owner) =>
            force.Where(c => !stunned.Contains(c) && !mortal.Contains(c))
                 .Sum(c => GetStrength(c, present, owner));

        int atkLive = LiveStr(atk, atkPresent, atkOwner);
        int defLive = LiveStr(def, defPresent, defOwner);

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
}
