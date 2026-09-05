using System;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 9: EOT-rest / SOT decide gates
/// (repairs, Rogue Borg invade, Edo penalty, SOT discard). Pure decide — no WPF.
/// Borg Ship EOT battle path and Persist branches stay in the View.
/// </summary>
public static class EndOfTurnRestRules
{
    public enum RepairAction { SkipHull, ResetProgress, Progress, FullyRepair }

    /// <summary>
    /// Outpost repair: 2 consecutive turns at own repair facility → full repair.
    /// Leaving the facility resets progress.
    /// </summary>
    public static RepairAction DecideRepair(
        int hullPercent,
        bool atOwnRepairFacility,
        int turnsAlreadyAtFacility)
    {
        if (hullPercent <= 0 || hullPercent >= 100)
            return RepairAction.SkipHull;
        if (!atOwnRepairFacility)
            return turnsAlreadyAtFacility > 0 ? RepairAction.ResetProgress : RepairAction.SkipHull;
        int next = turnsAlreadyAtFacility + 1;
        return next >= 2 ? RepairAction.FullyRepair : RepairAction.Progress;
    }

    public static int NextRepairTurnCount(RepairAction action, int turnsAlreadyAtFacility) =>
        action switch
        {
            RepairAction.FullyRepair => 0,
            RepairAction.Progress => turnsAlreadyAtFacility + 1,
            RepairAction.ResetProgress => 0,
            _ => turnsAlreadyAtFacility
        };

    public enum RogueInvadeAction { SkipPruned, SkipIntruderField, SkipNoPersonnel, Battle }

    /// <summary>
    /// Rogue Borg EOT: invade if ≥1 Rogue Borg STRENGTH and personnel aboard.
    /// Intruder Force Field blocks when Rogue count &lt; 3.
    /// </summary>
    public static RogueInvadeAction DecideRogueInvade(
        int rogueCount,
        int rogueStrengthTotal,
        bool intruderForceFieldActiveForShipOwner,
        bool nonRoguePersonnelPresent)
    {
        if (rogueCount <= 0 || rogueStrengthTotal <= 0)
            return RogueInvadeAction.SkipPruned;
        if (rogueCount < 3 && intruderForceFieldActiveForShipOwner)
            return RogueInvadeAction.SkipIntruderField;
        if (!nonRoguePersonnelPresent)
            return RogueInvadeAction.SkipNoPersonnel;
        return RogueInvadeAction.Battle;
    }

    /// <summary>Edo Probe continue: −10 if that mission was not solved this turn.</summary>
    public static bool ShouldApplyEdoContinuePenalty(bool missionSolvedThisTurn) =>
        !missionSolvedThisTurn;

    /// <summary>Start-of-turn timed discard (Crosis / NextTurn scope) after countdown expires.</summary>
    public static bool ShouldDiscardExpiredStartOfTurnEvent(
        bool countdownExpired,
        bool isCrosis,
        bool turnScopeIsNextTurn) =>
        countdownExpired && (isCrosis || turnScopeIsNextTurn);

    public enum DilemmaCurePoints { None, Five }

    /// <summary>Hyper-Aging / Remodulation Fatigue cure awards +5.</summary>
    public static DilemmaCurePoints CureAward(bool isHyperAgingOrRemFatigue) =>
        isHyperAgingOrRemFatigue ? DilemmaCurePoints.Five : DilemmaCurePoints.None;

    /// <summary>Junior Officer: destroy when EffectiveRange − countdown &lt; 1.</summary>
    public static bool JuniorDestroysShip(int effectiveRangeAfterCountdown) =>
        effectiveRangeAfterCountdown < 1;

    /// <summary>Nitrium / HyperAging / RemFatigue countdown hit 0 this EOT.</summary>
    public static bool CountdownExpired(int countdownAfterTick) => countdownAfterTick <= 0;
}
