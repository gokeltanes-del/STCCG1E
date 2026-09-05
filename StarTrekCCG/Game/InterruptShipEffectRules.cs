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
    /// Tachyon Detection Grid apply gates. Null = legal to de-cloak / lock / attach.
    /// </summary>
    public static string? TachyonDeny(int exposedShipCountYouControl, bool hasCloakedOrCloakCapableTarget)
    {
        if (exposedShipCountYouControl < 4)
            return "Tachyon Detection Grid: you must control four exposed ships.";
        if (!hasCloakedOrCloakCapableTarget)
            return "Tachyon Detection Grid: no cloaked / cloak-capable ship.";
        return null;
    }

    /// <summary>
    /// Transwarp Conduit: needs an own-ship pick. Null = apply RANGE double + attach + EOT discard.
    /// </summary>
    public static string? TranswarpDeny(bool hasOwnShipTarget) =>
        hasOwnShipTarget ? null : "Transwarp Conduit: play on your ship.";
}
