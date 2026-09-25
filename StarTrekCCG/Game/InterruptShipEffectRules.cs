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
    /// Tachyon Detection Grid (Spock 2026-09-05): need >=4 Controller ships in play
    /// (cloaked count). Target must be cloaked (Phased is not cloaked).
    /// Null = force-decloak + lock rest of turn.
    /// </summary>
    public static string? TachyonDeny(int controllerShipsInPlay, bool targetIsCloaked)
    {
        if (controllerShipsInPlay < 4)
            return "Tachyon Detection Grid: you must control four ships in play (have "
                   + controllerShipsInPlay + ").";
        if (!targetIsCloaked)
            return "Tachyon Detection Grid: play on a cloaked ship (Phased is not cloaked).";
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
}
