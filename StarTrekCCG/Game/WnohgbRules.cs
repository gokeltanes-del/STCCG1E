using System;

namespace StarTrekCCG;

/// <summary>
/// Where No One Has Gone Before (Spock 2026-09-05):
/// Controller ships: spaceline ends are adjacent while Event in play.
/// End→other end costs Destination.Span RANGE. Gaps/path otherwise normal.
/// Full staffing. Not free relocate / RANGE ignore / inter-quadrant.
/// </summary>
public static class WnohgbRules
{
    public static bool WrapAllowed(bool controllerHasEvent, int locationCount) =>
        controllerHasEvent && locationCount >= 2;

    /// <summary>Prefer wrap when it is strictly cheaper than the linear path.</summary>
    public static bool UseWrapPath(bool wrapAllowed, int directCost, int wrapCost) =>
        wrapAllowed && wrapCost < directCost;
}
