using System;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 3: Incoming Message apply gates.
/// Pure decide logic — no WPF. View still attaches, highlights, and moves.
/// </summary>
public static class IncomingMessageRules
{
    public enum ImApplyOutcome
    {
        NeedShipHost,
        AffiliationMismatch,
        NullifyNoFacility,
        AttachAndMaybeMove,
        AlreadyAtFacility
    }

    /// <summary>
    /// Early reject before attach/UI. Null means OK to proceed (pick facility, attach, then check arrival).
    /// </summary>
    public static ImApplyOutcome? EarlyReject(bool hostIsShip, bool affiliationOk, int facilityCount)
    {
        if (!hostIsShip) return ImApplyOutcome.NeedShipHost;
        if (!affiliationOk) return ImApplyOutcome.AffiliationMismatch;
        if (facilityCount <= 0) return ImApplyOutcome.NullifyNoFacility;
        return null;
    }

    /// <summary>
    /// Full decide (includes already-at). Prefer EarlyReject + IsAlreadyAtFacility when attach must happen first.
    /// </summary>
    public static ImApplyOutcome DecideApply(
        bool hostIsShip,
        bool affiliationOk,
        int matchingFacilityCount,
        bool shipAlreadyAtFacilityMission)
    {
        var early = EarlyReject(hostIsShip, affiliationOk, matchingFacilityCount);
        if (early != null) return early.Value;
        if (shipAlreadyAtFacilityMission) return ImApplyOutcome.AlreadyAtFacility;
        return ImApplyOutcome.AttachAndMaybeMove;
    }

    /// <summary>Facility mission face: dockable's mission, or the border itself if it is already a mission.</summary>
    public static bool SameLocation(bool bothNonNull, bool referenceEqual) => bothNonNull && referenceEqual;

    /// <summary>After attach: ship already at the facility's mission location.</summary>
    public static bool IsAlreadyAtFacility(bool shipMissionNonNull, bool sameAsFacilityMission) =>
        shipMissionNonNull && sameAsFacilityMission;
}
