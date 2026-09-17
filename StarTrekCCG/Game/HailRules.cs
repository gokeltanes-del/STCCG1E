using System;

namespace StarTrekCCG;

/// <summary>
/// Hail (Alternate Universe interrupt) — pure decide helpers (Spock Soll 2026-09-17).
/// Printed: fly-by must stop at your location OR select two ships; they cannot battle this turn.
/// Interrupt does not stay attached; lasting = turn flags. SI nullifies on stack only.
/// </summary>
public static class HailRules
{
    /// <summary>
    /// Glossary fly-by: span-move that visits <paramref name="passIdx"/> from elsewhere
    /// and continues to a different destination (not reverse-back to from).
    /// </summary>
    public static bool IsFlyByPass(int fromIdx, int toIdx, int passIdx, int count, bool useWrap)
    {
        if (passIdx < 0 || fromIdx < 0 || toIdx < 0 || fromIdx == toIdx)
            return false;
        if (passIdx == fromIdx || passIdx == toIdx)
            return false;
        return MovementHazardRules.PathVisitsIndex(fromIdx, toIdx, passIdx, count, useWrap);
    }

    /// <summary>Second ship must be a different ship than the first.</summary>
    public static bool CanSelectSecondShip(bool sameAsFirst) => !sameAsFirst;

    /// <summary>Unordered pair match for battle gate.</summary>
    public static bool IsNoBattlePair(bool sameA, bool sameB, bool swappedA, bool swappedB) =>
        (sameA && sameB) || (swappedA && swappedB);
}
