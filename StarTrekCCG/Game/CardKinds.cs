using System;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium card-type taxonomy. Use this instead of Type.Contains("event")
/// so Incident / Objective / Tactic / Site / Time Location / Q-* keep working.
/// </summary>
public enum CardKind
{
    Unknown,
    Personnel,
    Ship,
    Facility,
    Equipment,
    Mission,
    TimeLocation,
    Dilemma,
    Artifact,
    Event,
    Interrupt,
    Doorway,
    Incident,
    Objective,
    Tactic,
    Site,
    Tribble,
    Trouble,
    DamageMarker,
    QEvent,
    QInterrupt,
    QDilemma,
    QArtifact,
    QMission,
    InterruptEventHybrid
}

public static class CardKinds
{
    public static CardKind Of(Card? card)
    {
        if (card == null) return CardKind.Unknown;
        string t = (card.Type ?? "").Trim();
        if (t.Length == 0) return CardKind.Unknown;

        if (t.Equals("Interrupt/Event", StringComparison.OrdinalIgnoreCase))
            return CardKind.InterruptEventHybrid;

        if (Starts(t, "Q Dilemma")) return CardKind.QDilemma;
        if (Starts(t, "Q Event")) return CardKind.QEvent;
        if (Starts(t, "Q Interrupt")) return CardKind.QInterrupt;
        if (Starts(t, "Q Artifact")) return CardKind.QArtifact;
        if (Starts(t, "Q Mission")) return CardKind.QMission;
        if (Starts(t, "Time Location")) return CardKind.TimeLocation;
        if (Starts(t, "Damage Marker")) return CardKind.DamageMarker;

        if (Contains(t, "personnel") || Contains(t, "android") || Contains(t, "animal"))
            return CardKind.Personnel;
        if (Contains(t, "ship")) return CardKind.Ship;
        if (Contains(t, "facility") || Contains(t, "outpost")
            || Contains(t, "headquarters") || Contains(t, "station"))
            return CardKind.Facility;
        if (Contains(t, "equipment")) return CardKind.Equipment;
        if (Contains(t, "mission")) return CardKind.Mission;
        if (Contains(t, "dilemma")) return CardKind.Dilemma;
        if (Contains(t, "artifact")) return CardKind.Artifact;
        if (Contains(t, "incident")) return CardKind.Incident;
        if (Contains(t, "objective")) return CardKind.Objective;
        if (Contains(t, "tactic")) return CardKind.Tactic;
        if (Contains(t, "doorway")) return CardKind.Doorway;
        if (Contains(t, "site")) return CardKind.Site;
        if (Contains(t, "tribble")) return CardKind.Tribble;
        if (Contains(t, "trouble")) return CardKind.Trouble;
        if (Contains(t, "interrupt")) return CardKind.Interrupt;
        if (Contains(t, "event")) return CardKind.Event;
        return CardKind.Unknown;
    }

    /// <summary>Compendium 6.1: costs the one normal card play (unless played for free).</summary>
    public static bool UsesNormalCardPlay(Card card)
    {
        return Of(card) switch
        {
            CardKind.Interrupt or CardKind.QInterrupt => false,
            CardKind.Doorway => false,
            // Tactics are drawn from Battle Bridge during battle, not a hand card play.
            CardKind.Tactic or CardKind.DamageMarker => false,
            _ => true
        };
    }

    /// <summary>Interrupt, Doorway, and some hybrids may play outside the Play segment.</summary>
    public static bool IsAnytimeType(Card card)
    {
        return Of(card) switch
        {
            CardKind.Interrupt or CardKind.QInterrupt or CardKind.Doorway
                or CardKind.InterruptEventHybrid => true,
            _ => false
        };
    }

    public static bool MustReportForDuty(Card card) =>
        Of(card) is CardKind.Personnel or CardKind.Ship or CardKind.Equipment;

    public static bool IsPersonnel(Card card) => Of(card) == CardKind.Personnel;
    public static bool IsShip(Card card) => Of(card) == CardKind.Ship;
    public static bool IsFacility(Card card) => Of(card) == CardKind.Facility;
    public static bool IsEquipment(Card card) => Of(card) == CardKind.Equipment;
    public static bool IsMission(Card card) => Of(card) is CardKind.Mission or CardKind.QMission;
    public static bool IsTimeLocation(Card card) => Of(card) == CardKind.TimeLocation;
    public static bool IsDilemma(Card card) => Of(card) is CardKind.Dilemma or CardKind.QDilemma;
    public static bool IsArtifact(Card card) => Of(card) is CardKind.Artifact or CardKind.QArtifact;
    public static bool IsEvent(Card card) => Of(card) is CardKind.Event or CardKind.QEvent;
    public static bool IsInterrupt(Card card) =>
        Of(card) is CardKind.Interrupt or CardKind.QInterrupt or CardKind.InterruptEventHybrid;
    public static bool IsDoorway(Card card) => Of(card) == CardKind.Doorway;
    public static bool IsIncident(Card card) => Of(card) == CardKind.Incident;
    public static bool IsObjective(Card card) => Of(card) == CardKind.Objective;
    public static bool IsTactic(Card card) => Of(card) == CardKind.Tactic;
    public static bool IsSite(Card card) => Of(card) == CardKind.Site;
    public static bool IsQCard(Card card) =>
        Of(card) is CardKind.QEvent or CardKind.QInterrupt or CardKind.QDilemma
            or CardKind.QArtifact or CardKind.QMission;

    /// <summary>Event / Incident / Objective / non-cover Doorway — live on the core / TABLE column.</summary>
    public static bool IsCorePermanent(Card card)
    {
        return Of(card) switch
        {
            CardKind.Event or CardKind.QEvent
                or CardKind.Incident or CardKind.Objective
                or CardKind.Doorway or CardKind.InterruptEventHybrid => true,
            _ => false
        };
    }

    /// <summary>May be seeded under a mission (dilemma / artifact / Q-dilemma).</summary>
    public static bool SeedsUnderMission(Card card) =>
        IsDilemma(card) || IsArtifact(card);

    public static bool IsSpacelineCard(Card card) =>
        IsMission(card) || IsTimeLocation(card);

    public static bool StacksOnHost(Card card) =>
        Of(card) is CardKind.Personnel or CardKind.Equipment or CardKind.Artifact or CardKind.Site;

    public static bool IsBeamable(Card card) =>
        Of(card) is CardKind.Personnel or CardKind.Equipment or CardKind.Artifact;

    /// <summary>Which seed sub-pile a card belongs in (Premiere seed order + later types).</summary>
    public enum SeedBucket { Doorway, Mission, Dilemma, Facility }

    public static SeedBucket SeedPile(Card card)
    {
        if (IsDoorway(card)) return SeedBucket.Doorway;
        if (IsMission(card) || IsTimeLocation(card)) return SeedBucket.Mission;
        if (SeedsUnderMission(card)) return SeedBucket.Dilemma;
        // Hidden Agenda objectives often seed; treat as facility/other seed until HA pipeline owns them.
        return SeedBucket.Facility;
    }

    public static bool AllowedInBattleBridge(Card card) =>
        Of(card) is CardKind.Tactic or CardKind.DamageMarker;

    public static bool AllowedInSitePile(Card card) => IsSite(card);

    public static bool AllowedInQContinuum(Card card) => IsQCard(card);

    public static bool AllowedInTribblePile(Card card) =>
        Of(card) is CardKind.Tribble or CardKind.Trouble;

    private static bool Starts(string t, string n) =>
        t.StartsWith(n, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string t, string n) =>
        t.Contains(n, StringComparison.OrdinalIgnoreCase);
}