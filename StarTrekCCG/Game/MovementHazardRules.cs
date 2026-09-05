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

    /// <summary>Pre-move deny string, or null if movement may proceed.
    /// When wrapEnds and the wrap path is shorter (WNOHGB), only that path is checked —
    /// ends are adjacent; the long way across the spaceline is not crossed.</summary>
    public static string? CheckMovement(
        IEnumerable<HazardEvent> events,
        int fromIdx,
        int toIdx,
        IEnumerable<Card> crew,
        bool arrivedAtFromThisTurn,
        bool wrapEnds = false,
        int lineCount = 0,
        int directCost = int.MaxValue,
        int wrapCost = int.MaxValue)
    {
        if (fromIdx < 0 || toIdx < 0 || fromIdx == toIdx)
            return null;

        bool useWrap = wrapEnds && lineCount >= 2 && wrapCost < directCost;

        foreach (var e in events)
        {
            int h1 = e.HostIndex;
            int h2 = e.Host2Index;
            if (e.Kind == EventRules.Persist.QNet)
            {
                bool crosses = useWrap
                    ? CrossesOnWrapPath(fromIdx, toIdx, h1, h2, lineCount)
                    : CrossesOnLinearPath(fromIdx, toIdx, h1, h2);
                if (crosses && !EventRules.HasSkill(crew, "Diplomacy", 2))
                    return "Q-Net: 2 Diplomacy required aboard.";
            }
            if (e.Kind == EventRules.Persist.Tetryon && h1 >= 0)
            {
                bool passes = useWrap
                    ? CrossesOnWrapPath(fromIdx, toIdx, h1, h1, lineCount)
                    : (Math.Min(fromIdx, toIdx) < h1 && Math.Max(fromIdx, toIdx) > h1);
                if (passes)
                    return "Tetryon Field: ships may not pass this location.";
                if (fromIdx == h1 && toIdx != h1
                    && arrivedAtFromThisTurn
                    && !EventRules.HasSkill(crew, "Navigation"))
                    return "Tetryon Field: Navigation required to use RANGE again this turn.";
            }
        }
        return null;
    }

    static bool CrossesOnLinearPath(int fromIdx, int toIdx, int h1, int h2)
    {
        int lo = Math.Min(fromIdx, toIdx);
        int hi = Math.Max(fromIdx, toIdx);
        if (h1 >= 0 && h2 >= 0 && lo <= Math.Min(h1, h2) && hi >= Math.Max(h1, h2) && fromIdx != toIdx)
            return true;
        if (h1 >= 0 && fromIdx < h1 && toIdx > h1)
            return true;
        return false;
    }

    /// <summary>WNOHGB: one hop between spaceline ends — crosses a barrier only if it sits on that end-edge.</summary>
    static bool CrossesOnWrapPath(int fromIdx, int toIdx, int h1, int h2, int n)
    {
        if (n < 2) return false;
        int a = Math.Min(fromIdx, toIdx);
        int b = Math.Max(fromIdx, toIdx);
        // Wrap edge is between 0 and n-1 only when flying those two ends.
        if (a != 0 || b != n - 1) return CrossesOnLinearPath(fromIdx, toIdx, h1, h2);
        // Barrier spanning the wrap edge: hosts at both ends (Q-Net between last and first).
        if (h1 >= 0 && h2 >= 0)
        {
            int lo = Math.Min(h1, h2);
            int hi = Math.Max(h1, h2);
            if (lo == 0 && hi == n - 1) return true;
        }
        return false;
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
