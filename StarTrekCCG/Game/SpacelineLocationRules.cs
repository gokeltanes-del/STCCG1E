using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Centralized rules and classification pipeline for spaceline locations
/// (missions, standard time locations, and cards that play as spaceline locations
/// such as Time Travel Pod and Temporal Rift).
/// </summary>
public static class SpacelineLocationRules
{
    /// <summary>
    /// Checks if a card is or plays as a time location on the spaceline.
    /// (e.g. printed Time Location, Time Travel Pod artifact, Temporal Rift interrupt).
    /// </summary>
    public static bool IsTimeLocation(Card? c)
    {
        if (c == null) return false;
        if (CardKinds.IsTimeLocation(c)) return true;
        if (ArtifactRules.IsTimeTravelPod(c)) return true;
        if (InterruptRules.IsTemporalRift(c)) return true;
        return false;
    }

    /// <summary>
    /// Checks if a card is placed on or plays as a spaceline location (missions, time locations, etc.).
    /// </summary>
    public static bool IsSpacelineLocation(Card? c)
    {
        if (c == null) return false;
        return CardKinds.IsMission(c) || IsTimeLocation(c);
    }

    /// <summary>
    /// Checks if a non-mission card plays as a spaceline location.
    /// </summary>
    public static bool PlaysAsSpacelineLocation(Card? c)
    {
        if (c == null) return false;
        return ArtifactRules.IsTimeTravelPod(c) || InterruptRules.IsTemporalRift(c);
    }

    /// <summary>
    /// Checks if a card is a valid landable / dockable spaceline location for ships and facilities.
    /// </summary>
    public static bool IsLandableSpacelineLocation(Card? c)
    {
        if (c == null) return false;
        return CardKinds.IsMission(c)
               || EventRules.IsGapsInNormalSpace(c)
               || IsTimeLocation(c);
    }

    /// <summary>
    /// Checks if two locations are in different time continua (e.g. time location vs regular spaceline).
    /// Ships cannot fly between different time continua using ordinary warp.
    /// </summary>
    public static bool IsDifferentTimeContinuum(Card? locA, Card? locB)
    {
        return IsTimeLocation(locA) != IsTimeLocation(locB);
    }

    /// <summary>
    /// Rules verification for SpacelineLocationRules. Null = OK.
    /// </summary>
    public static string? VerifySpacelineLocationRules()
    {
        var tr = new Card { Name = "Temporal Rift", Type = "Interrupt" };
        var ttp = new Card { Name = "Time Travel Pod", Type = "Artifact" };
        var m = new Card { Name = "Farpoint Station", Type = "Mission" };
        var s = new Card { Name = "U.S.S. Enterprise", Type = "Ship" };

        if (!IsTimeLocation(tr)) return "Temporal Rift must be time location";
        if (!IsTimeLocation(ttp)) return "Time Travel Pod must be time location";
        if (IsTimeLocation(m)) return "Farpoint Station is not time location";
        if (IsTimeLocation(s)) return "Ship is not time location";

        if (!IsSpacelineLocation(tr)) return "Temporal Rift must be spaceline location";
        if (!IsSpacelineLocation(m)) return "Farpoint Station must be spaceline location";
        if (IsSpacelineLocation(s)) return "Ship is not spaceline location";

        if (!PlaysAsSpacelineLocation(tr)) return "Temporal Rift plays as spaceline location";
        if (!PlaysAsSpacelineLocation(ttp)) return "Time Travel Pod plays as spaceline location";
        if (PlaysAsSpacelineLocation(m)) return "Mission does not play as spaceline location (it is printed mission)";

        if (!IsLandableSpacelineLocation(tr)) return "Temporal Rift must be landable spaceline location";
        if (!IsLandableSpacelineLocation(m)) return "Mission must be landable spaceline location";

        if (!IsDifferentTimeContinuum(tr, m)) return "Time location and mission must be different time continua";
        if (IsDifferentTimeContinuum(tr, ttp)) return "Two time locations should both be in time continua";
        if (IsDifferentTimeContinuum(m, m)) return "Two missions should not be different time continua";

        return null;
    }
}
