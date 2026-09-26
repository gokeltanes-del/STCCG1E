using System;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 7: Premiere interrupt ship-effect decide gates
/// (Asteroid Sanctuary, Distortion of S/T Continuum, Tachyon Detection Grid, Transwarp Conduit).
/// Pure decide - no WPF. View still picks hosts, AskPlayer, attaches, TurnExpiry, cloak/status.
/// </summary>
public static class InterruptShipEffectRules
{
    /// <summary>
    /// Asteroid Sanctuary apply gates. Null = legal to attach (Navigation only affects status text).
    /// </summary>
    public static string? SanctuaryDeny(bool hostIsShip, bool isYours, bool exposed, bool has2Navigation)
    {
        _ = has2Navigation; // status-only in TW; not a play deny
        if (!hostIsShip) return "Asteroid Sanctuary: play on your exposed ship.";
        if (!isYours) return "Asteroid Sanctuary: must be your ship.";
        if (!exposed) return "Asteroid Sanctuary: ship must be exposed (not cloaked).";
        return null;
    }

    /// <summary>
    /// Distortion of Space/Time Continuum apply gates (AU timing AskPlayer stays in TW).
    /// Null = legal to attach.
    /// </summary>
    public static string? DistortionDeny(bool hostIsShip, bool isYours, bool shipIsAu, bool alreadyInPlay)
    {
        if (!hostIsShip) return "Distortion of Space/Time Continuum: play on your non-AU ship.";
        if (shipIsAu) return "Distortion: target ship must be non-AU.";
        if (!isYours) return "Distortion: must be your ship.";
        if (alreadyInPlay) return "Distortion of Space/Time Continuum is unique - already in play.";
        return null;
    }

    /// <summary>
    /// Tachyon Detection Grid (Premiere 318 U):
    /// "If you control four exposed ships, plays on a cloaked ship. It de-cloaks (even if it is stopped or has cloaked this turn). It may not cloak."
    /// Exposed = undocked, uncloaked, unphased, not landed, not carried (ShipRules.IsShipExposed).
    /// Target must be cloaked (Phased is not cloaked).
    /// Null = force-decloak + lock rest of turn.
    /// </summary>
    public static string? TachyonDeny(int exposedShipsInPlay, bool targetIsCloaked)
    {
        if (exposedShipsInPlay < 4)
            return "Tachyon Detection Grid: you must control four exposed ships in play (have "
                   + exposedShipsInPlay + ").";
        if (!targetIsCloaked)
            return "Tachyon Detection Grid: play on a cloaked ship (Phased is not cloaked).";
        return null;
    }

    /// <summary>
    /// Scan (Premiere 295 C):
    /// "Plays at the start of your turn on your ship with at least two staffing icons at a [S] mission. Stop your Computer Skill and Stellar Cartography aboard to examine the bottom seed card here."
    /// </summary>
    public static string? ScanDeny(
        bool hostIsShip,
        bool isYours,
        bool isSpaceMission,
        bool has2StaffingIcons,
        bool hasSeedCards,
        bool hasComputerSkill,
        bool hasStellarCartography)
    {
        if (!hostIsShip) return "Scan: play on your ship with at least two staffing icons at a [S] mission.";
        if (!isYours) return "Scan: must be your ship.";
        if (!isSpaceMission) return "Scan: ship must be at a [S] mission.";
        if (!has2StaffingIcons) return "Scan: ship needs at least two staffing icons (printed [Cmd]/[Stf]).";
        if (!hasSeedCards) return "Scan: no seed cards under this mission.";
        if (!hasComputerSkill || !hasStellarCartography) return "Scan: need Computer Skill and Stellar Cartography aboard (unstopped).";
        return null;
    }

    /// <summary>
    /// Full Planet Scan (Premiere 117 U):
    /// "Plays at the start of your turn on your ship with at least two staffing icons at a [P] mission. Stop your Computer Skill and Geology aboard to examine the bottom seed card here."
    /// </summary>
    public static string? FullPlanetScanDeny(
        bool hostIsShip,
        bool isYours,
        bool isPlanetMission,
        bool has2StaffingIcons,
        bool hasSeedCards,
        bool hasComputerSkill,
        bool hasGeology)
    {
        if (!hostIsShip) return "Full Planet Scan: play on your ship at a planet mission.";
        if (!isYours) return "Full Planet Scan: must be your ship.";
        if (!isPlanetMission) return "Full Planet Scan: ship must be at a planet mission.";
        if (!has2StaffingIcons) return "Full Planet Scan: ship needs at least two staffing icons (printed [Cmd]/[Stf]).";
        if (!hasSeedCards) return "Full Planet Scan: no seed cards under this mission.";
        if (!hasComputerSkill || !hasGeology) return "Full Planet Scan: need Computer Skill and Geology aboard (unstopped).";
        return null;
    }

    /// <summary>
    /// Transwarp Conduit: drop host must be a ship. Null = RANGE x2 on that host.
    /// No picker — use the drop/stack target host.
    /// </summary>
    public static string? TranswarpDeny(bool hostIsShip) =>
        hostIsShip ? null : "Transwarp Conduit: play on a ship.";

    // Rule: 10.1.0.7 Undefined and Variable Attributes
    // Glossary: in orbit
    // Verb: plays-on interrupt ship
    /// <summary>
    /// Loss of Orbital Stability: plays on a ship orbiting a [P].
    /// In orbit: in space, undocked, at a planet location (Glossary: in orbit).
    /// </summary>
    public static string? LossOfOrbitalStabilityDeny(bool hostIsShip, bool isDocked, bool atPlanet)
    {
        if (!hostIsShip) return "Loss of Orbital Stability: plays on a ship.";
        if (isDocked) return "Loss of Orbital Stability: ship is docked (not in orbit).";
        if (!atPlanet) return "Loss of Orbital Stability: ship must be orbiting a planet mission.";
        return null;
    }

    // Rule: 7.1.1 · 7.1.1.0.2 Card-Activated Transport
    // Glossary: Near-Warp Transport · adjacent · exposed
    // Verb: plays to beam
    /// <summary>
    /// Near-Warp Transport: Plays to beam up to six cards (personnel and/or [Equipment])
    /// from your exposed ship with transporters to an adjacent spaceline location (if possible).
    /// Exposed = undocked, uncloaked, unphased, and not landed or carried (Glossary: exposed).
    /// </summary>
    public static string? NearWarpTransportDeny(
        bool hostIsShip,
        bool isYours,
        bool isExposed,
        bool hasTransporters,
        int beamableCardsCount,
        bool hasAdjacentLocation)
    {
        if (!hostIsShip) return "Near-Warp Transport: plays on your exposed ship with transporters.";
        if (!isYours) return "Near-Warp Transport: must be your ship.";
        if (!isExposed) return "Near-Warp Transport: ship must be exposed (undocked, uncloaked, not landed/carried).";
        if (!hasTransporters) return "Near-Warp Transport: ship must have functional transporters.";
        if (beamableCardsCount <= 0) return "Near-Warp Transport: no beamable personnel or equipment aboard.";
        if (!hasAdjacentLocation) return "Near-Warp Transport: no adjacent spaceline location in the same quadrant.";
        return null;
    }

    /// <summary>
    /// Mini-test for Near-Warp Transport gates. Null = OK.
    /// </summary>
    public static string? VerifyNearWarpTransportDecide()
    {
        if (NearWarpTransportDeny(true, true, true, true, 1, true) != null)
            return "valid ship should pass NearWarpTransportDeny";
        if (NearWarpTransportDeny(false, true, true, true, 1, true) == null)
            return "non-ship should fail NearWarpTransportDeny";
        if (NearWarpTransportDeny(true, false, true, true, 1, true) == null)
            return "opponent ship should fail NearWarpTransportDeny";
        if (NearWarpTransportDeny(true, true, false, true, 1, true) == null)
            return "unexposed ship should fail NearWarpTransportDeny";
        if (NearWarpTransportDeny(true, true, true, false, 1, true) == null)
            return "ship without transporters should fail NearWarpTransportDeny";
        if (NearWarpTransportDeny(true, true, true, true, 0, true) == null)
            return "empty ship should fail NearWarpTransportDeny";
        if (NearWarpTransportDeny(true, true, true, true, 1, false) == null)
            return "no adjacent location should fail NearWarpTransportDeny";
        return null;
    }

    /// <summary>
    /// Mini-test for Tachyon Detection Grid gates. Null = OK.
    /// </summary>
    public static string? VerifyTachyonDecide()
    {
        if (TachyonDeny(4, true) != null)
            return "4 exposed ships and cloaked target should pass TachyonDeny";
        if (TachyonDeny(5, true) != null)
            return "5 exposed ships and cloaked target should pass TachyonDeny";
        if (TachyonDeny(3, true) == null)
            return "3 exposed ships should fail TachyonDeny";
        if (TachyonDeny(4, false) == null)
            return "uncloaked target should fail TachyonDeny";
        return null;
    }

    /// <summary>
    /// Mini-test for Scan gates. Null = OK.
    /// </summary>
    public static string? VerifyScanDecide()
    {
        if (ScanDeny(true, true, true, true, true, true, true) != null)
            return "valid ship and space mission should pass ScanDeny";
        if (ScanDeny(false, true, true, true, true, true, true) == null)
            return "non-ship should fail ScanDeny";
        if (ScanDeny(true, false, true, true, true, true, true) == null)
            return "opponent ship should fail ScanDeny";
        if (ScanDeny(true, true, false, true, true, true, true) == null)
            return "planet mission should fail ScanDeny";
        if (ScanDeny(true, true, true, false, true, true, true) == null)
            return "less than 2 staffing icons should fail ScanDeny";
        if (ScanDeny(true, true, true, true, false, true, true) == null)
            return "no seed cards should fail ScanDeny";
        if (ScanDeny(true, true, true, true, true, false, true) == null)
            return "missing Computer Skill should fail ScanDeny";
        if (ScanDeny(true, true, true, true, true, true, false) == null)
            return "missing Stellar Cartography should fail ScanDeny";
        return null;
    }
}
