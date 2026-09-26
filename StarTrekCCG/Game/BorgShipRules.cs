using System;
using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Kompendium-Dilemma „Borg Ship“ (P1):
/// WEAPONS 24, SHIELDS 24, +15 Punkte bei Zerstörung.
/// End-of-Turn: greift alle ungetarnten Schiffe und Einrichtungen am selben Ort an,
/// zieht dann um 1 Mission weiter in Spaceline-Richtung, und verlässt das Spiel,
/// sobald es über das Spaceline-Ende hinausziehen würde.
/// </summary>
public static class BorgShipRules
{
    public const int Weapons = 24;
    public const int Shields = 24;
    public const int PointsOnDestroyed = 15;

    public static int DecideInitialDirection(int farIdx, int nearIdx) =>
        farIdx >= nearIdx ? -1 : 1;

    public static bool IsLegalTarget(bool isShip, bool isFacility, bool isCloaked)
    {
        if (isFacility) return true;
        if (isShip && !isCloaked) return true;
        return false;
    }

    public static int BorgWeaponsBonus(Card attackerCard)
    {
        int printed = BattleRules.GetWeapons(attackerCard);
        return Math.Max(0, Weapons - printed);
    }

    public static int BorgShieldsBonus(Card attackerCard)
    {
        int printed = BattleRules.GetShields(attackerCard);
        return Math.Max(0, Shields - printed);
    }

    public sealed class MovePlan
    {
        public bool LeavesPlay { get; init; }
        public int NextIndex { get; init; }
        public string AttackSummary { get; init; } = "";
        public string Message { get; init; } = "";
    }

    public static MovePlan DecideMove(
        int currentIndex,
        int direction,
        int spacelineCount,
        IReadOnlyList<string> hitLog,
        string hostMissionName,
        string? nextMissionName)
    {
        if (currentIndex < 0 && spacelineCount > 0)
            currentIndex = direction > 0 ? 0 : spacelineCount - 1;

        int nextIdx = currentIndex + direction;
        string attackPart = hitLog != null && hitLog.Count > 0
            ? ("Attacks at " + hostMissionName + ": " + string.Join(", ", hitLog) + ".")
            : ("At " + hostMissionName + ": no ships damaged.");

        if (nextIdx < 0 || nextIdx >= spacelineCount)
        {
            return new MovePlan
            {
                LeavesPlay = true,
                NextIndex = nextIdx,
                AttackSummary = attackPart,
                Message = attackPart + " Moves off the spaceline and leaves play."
            };
        }

        string destName = string.IsNullOrWhiteSpace(nextMissionName) ? "?" : nextMissionName;
        return new MovePlan
        {
            LeavesPlay = false,
            NextIndex = nextIdx,
            AttackSummary = attackPart,
            Message = attackPart + " Moves to " + destName + "."
        };
    }
}
