using System;
using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 1: Q-Net / Tetryon / Rift / Gaps movement hazards.
/// Pure index + card logic — no WPF. View still applies damage/discard/status.
/// </summary>
public static class MovementHazardRules
{
    public readonly record struct HazardEvent(
        EventRules.Persist Kind,
        int HostIndex,
        int Host2Index,
        bool DestIsGapsLocation);

    /// <summary>Pre-move deny string, or null if movement may proceed.</summary>
    public static string? CheckMovement(
        IEnumerable<HazardEvent> events,
        int fromIdx,
        int toIdx,
        IEnumerable<Card> crew,
        bool arrivedAtFromThisTurn)
    {
        int lo = Math.Min(fromIdx, toIdx);
        int hi = Math.Max(fromIdx, toIdx);
        foreach (var e in events)
        {
            int h1 = e.HostIndex;
            int h2 = e.Host2Index;
            if (e.Kind == EventRules.Persist.QNet)
            {
                bool crosses = (h1 >= 0 && h2 >= 0 && lo <= Math.Min(h1, h2) && hi >= Math.Max(h1, h2) && fromIdx != toIdx)
                               || (h1 >= 0 && fromIdx < h1 && toIdx > h1);
                if (crosses && !EventRules.HasSkill(crew, "Diplomacy", 2))
                    return "Q-Net: 2 Diplomacy required aboard.";
            }
            if (e.Kind == EventRules.Persist.Tetryon && h1 >= 0)
            {
                if (lo < h1 && hi > h1)
                    return "Tetryon Field: ships may not pass this location.";
                if (fromIdx == h1 && toIdx != h1
                    && arrivedAtFromThisTurn
                    && !EventRules.HasSkill(crew, "Navigation"))
                    return "Tetryon Field: Navigation required to use RANGE again this turn.";
            }
        }
        return null;
    }

    /// <summary>Subspace Warp Rift: fly-by or leave after arriving same turn.</summary>
    public static (bool apply, bool flyBy) RiftDamage(
        int fromIdx, int toIdx, int riftHostIdx, bool arrivedAtRiftThisTurn)
    {
        if (riftHostIdx < 0) return (false, false);
        int lo = Math.Min(fromIdx, toIdx);
        int hi = Math.Max(fromIdx, toIdx);
        bool flyBy = lo < riftHostIdx && hi > riftHostIdx;
        bool leaveAfterArrival = fromIdx == riftHostIdx && toIdx != riftHostIdx && arrivedAtRiftThisTurn;
        if (flyBy) return (true, true);
        if (leaveAfterArrival) return (true, false);
        return (false, false);
    }

    /// <summary>Gaps random kill only when landing on the Gaps span location (not Host/Host2).</summary>
    public static bool GapsKillOnArrival(bool destIsGapsLocation) => destIsGapsLocation;
}
