using System;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 8: ProcessEndOfTurnEvents decide gates.
/// Pure decide - no WPF. View still damages, destroys, picks discards, reveals, restores.
/// </summary>
public static class EndOfTurnEventRules
{
    public enum PlasmaAction { SkipThermal, SkipTiming, Damage }

    public readonly record struct PlasmaPlan(
        PlasmaAction Action,
        int NextHullPercent,
        bool DestroyShip);

    /// <summary>Plasma Fire EOT: +50 HULL (cap 100); destroy at 100. Thermal suppresses.</summary>
    public static PlasmaPlan DecidePlasmaFire(
        bool thermalDeflectorsInPlay,
        bool shouldProcessOnThisTurn,
        int currentHullPercent)
    {
        if (thermalDeflectorsInPlay)
            return new(PlasmaAction.SkipThermal, currentHullPercent, false);
        if (!shouldProcessOnThisTurn)
            return new(PlasmaAction.SkipTiming, currentHullPercent, false);
        int next = Math.Min(100, currentHullPercent + 50);
        return new(PlasmaAction.Damage, next, next >= 100);
    }

    public enum WarpCoreAction { SkipTiming, TickOnly, Explode }

    public readonly record struct WarpCorePlan(WarpCoreAction Action, int CountdownAfter);

    /// <summary>
    /// Warp Core Breach: destroy at end of controller's next turn (countdown tick).
    /// ENGINEER nullify stays optional in the View.
    /// </summary>
    public static WarpCorePlan DecideWarpCore(
        bool shouldProcessOnThisTurn,
        int countdown,
        Func<int, (int CountdownAfter, bool Explode)> tick)
    {
        if (!shouldProcessOnThisTurn)
            return new(WarpCoreAction.SkipTiming, countdown);
        var (after, explode) = tick(countdown);
        return explode
            ? new(WarpCoreAction.Explode, after)
            : new(WarpCoreAction.TickOnly, after);
    }

    public enum StaticWarpAction { SkipWrongOwner, SkipTraveler, NeedHandDiscard, SkipEmptyHand }

    /// <summary>
    /// Static Warp Bubble: each opposing player's EOT before draw — discard 1 from hand
    /// unless The Traveler: Transcendence continuously nullifies.
    /// </summary>
    public static StaticWarpAction DecideStaticWarp(
        bool eventOwnerIsFinishingPlayer,
        bool travelerTranscendenceInPlay,
        bool finishingPlayerHasHandCards)
    {
        if (eventOwnerIsFinishingPlayer)
            return StaticWarpAction.SkipWrongOwner;
        if (travelerTranscendenceInPlay)
            return StaticWarpAction.SkipTraveler;
        return finishingPlayerHasHandCards
            ? StaticWarpAction.NeedHandDiscard
            : StaticWarpAction.SkipEmptyHand;
    }

    public static bool ShouldDiscardTranswarp(bool cardIsTranswarp, bool eventOwnerIsFinishingPlayer) =>
        cardIsTranswarp && eventOwnerIsFinishingPlayer;

    public static bool ShouldFlipDistortion(bool kindIsDistortion) => kindIsDistortion;

    public static bool ShouldGrantTravelerExtraDraw(bool kindIsTraveler, bool travelerPlayerIsFinishing) =>
        kindIsTraveler && travelerPlayerIsFinishing;

    public static bool ShouldRunKidnappers(bool kindIsKidnappers, bool eventOwnerIsFinishing) =>
        kindIsKidnappers && eventOwnerIsFinishing;

    public static bool ShouldRestoreNeuralServo(bool kindIsNeuralServo, bool eventOwnerIsFinishing, bool hasHost) =>
        kindIsNeuralServo && eventOwnerIsFinishing && hasHost;

    public readonly record struct AntiTimePlan(int CountdownAfter, bool Expire);

    public static AntiTimePlan DecideAntiTime(
        int countdown,
        Func<int, (int CountdownAfter, bool Done)> tick)
    {
        var (after, done) = tick(countdown);
        return new(after, done);
    }
}
