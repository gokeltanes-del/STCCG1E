using System;
using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 1: Q-Net / Tetryon / Rift / Gaps movement hazards.
/// Pure index + card logic - no WPF. View still applies damage/discard/status.
/// </summary>
public static class MovementHazardRules
{
    public readonly record struct HazardEvent(
        EventRules.Persist Kind,
        int HostIndex,
        int Host2Index,
        bool DestIsGapsLocation);

    /// <summary>
    /// Walk the chosen arc (direct or WNOHGB wrap). True if the edge between
    /// consecutive indices on that arc is the Q-Net gap (h1↔h2).
    /// </summary>
    public static bool QNetCrossesPath(int fromIdx, int toIdx, int h1, int h2, int count, bool useWrap)
    {
        if (fromIdx == toIdx || fromIdx < 0 || toIdx < 0 || count < 2)
            return false;
        if (fromIdx >= count || toIdx >= count) return false;
        if (h1 < 0 && h2 < 0) return false;

        int step = toIdx > fromIdx ? 1 : -1;
        if (useWrap) step = -step;

        int i = fromIdx;
        int guard = 0;
        while (i != toIdx)
        {
            int next = (i + step + count) % count;
            if (h1 >= 0 && h2 >= 0)
            {
                if ((i == h1 && next == h2) || (i == h2 && next == h1))
                    return true;
            }
            else if (h1 >= 0)
            {
                // Single-host barrier: crossing past h1 on this arc.
                if (i != h1 && next == h1) return true;
            }
            i = next;
            if (++guard > count + 1) return true;
        }
        return false;
    }

    /// <summary>Walk the chosen arc; true if index is reached before destination (includes landing on idx).</summary>
    public static bool PathVisitsIndex(int fromIdx, int toIdx, int idx, int count, bool useWrap)
    {
        if (idx < 0 || fromIdx == toIdx || count < 2) return false;
        if (fromIdx < 0 || toIdx < 0 || fromIdx >= count || toIdx >= count) return false;
        int step = toIdx > fromIdx ? 1 : -1;
        if (useWrap) step = -step;
        int i = fromIdx;
        int guard = 0;
        while (i != toIdx)
        {
            i = (i + step + count) % count;
            if (i == idx) return true;
            if (++guard > count + 1) return false;
        }
        return false;
    }

    /// <summary>
    /// Pre-move deny string, or null if movement may proceed.
    /// When wrapEnds and wrapCost &lt; directCost (WNOHGB shorter ring), only that arc is checked.
    /// </summary>
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

        int count = lineCount;
        if (count < 2)
        {
            int max = Math.Max(fromIdx, toIdx);
            foreach (var e in events)
            {
                if (e.HostIndex > max) max = e.HostIndex;
                if (e.Host2Index > max) max = e.Host2Index;
            }
            count = max + 1;
        }

        bool useWrap = WnohgbRules.UseWrapPath(wrapEnds, directCost, wrapCost);

        foreach (var e in events)
        {
            int h1 = e.HostIndex;
            int h2 = e.Host2Index;
            if (e.Kind == EventRules.Persist.QNet)
            {
                bool crosses = QNetCrossesPath(fromIdx, toIdx, h1, h2, count, useWrap);
                if (crosses && !EventRules.HasSkill(crew, "Diplomacy", 2))
                    return "Q-Net: 2 Diplomacy required aboard.";
            }
            if (e.Kind == EventRules.Persist.Tetryon && h1 >= 0)
            {
                bool passes = fromIdx != h1 && toIdx != h1
                              && PathVisitsIndex(fromIdx, toIdx, h1, count, useWrap);
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
