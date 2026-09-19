using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>Compendium 7.1.4 Dock / undock. Premiere: your facility at the same location.</summary>
public static class DockingRules
{
    public static (bool ok, string reason) CanDock(
        Card ship, Card facility, bool sameLocation, bool alreadyDocked, bool cloaked)
    {
        if (ship == null || facility == null)
            return (false, "Need a ship and a facility.");
        if (alreadyDocked)
            return (false, "Already docked.");
        if (cloaked)
            return (false, "A cloaked ship cannot dock.");
        if (!sameLocation)
            return (false, "Ship and facility must be at the same location.");
        if (!CardKinds.IsFacility(facility) && !(facility.Type ?? "").Contains("outpost", StringComparison.OrdinalIgnoreCase))
            return (false, "Must dock at a facility.");
        return (true, "May dock.");
    }

    /// <summary>REM Fatigue dock cure: facility must be an Outpost (not HQ/Station).</summary>
    public static bool IsOutpostDockCureTarget(Card? facility) =>
        DilemmaRules.IsOutpostFacility(facility);
}
