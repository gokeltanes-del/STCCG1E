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
}
