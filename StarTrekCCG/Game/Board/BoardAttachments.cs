using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Persist-Modell für Dilemma-Attachments auf Board-Seite (P0-D1).
/// Host und Dest werden als Location/Instance-IDs geführt, nicht als WPF-Border.
/// </summary>
public sealed class BoardAttachedDilemma
{
    public required Card Card { get; init; }
    public required DilemmaRules.PersistKind Kind { get; init; }
    public int Countdown { get; set; }
    /// <summary>InstanceId des Host-Schiffs/Facility bzw. Mission/Location.</summary>
    public int? HostInstanceId { get; set; }
    public Card? Extra { get; set; }
    /// <summary>InstanceId des Ziel-Orts (z. B. Cytherians FarEnd).</summary>
    public int? DestInstanceId { get; set; }
    /// <summary>Borg Ship: Bewegungsrichtung entlang der Spaceline (+1 oder -1).</summary>
    public int Direction { get; set; } = 1;
    /// <summary>Personnel held in stasis (Abduction, Phased Matter etc.).</summary>
    public List<Card> Held { get; } = new();
    /// <summary>REM Fatigue: personnel present at encounter (kill on countdown 0).</summary>
    public List<Card> OriginalEncounter { get; } = new();
}

/// <summary>
/// Persist-Modell für Event-Attachments auf Board-Seite (P0-E1).
/// Host und Host2 werden als Instance-IDs geführt (z.B. Gaps / Q-Net Endpunkte).
/// </summary>
public sealed class BoardAttachedEvent
{
    public required Card Card { get; init; }
    public required EventRules.Persist Kind { get; init; }
    public required int Owner { get; init; }
    public int? HostInstanceId { get; set; }
    public int? Host2InstanceId { get; set; }
    public int Countdown { get; set; }
    public bool FaceUp { get; set; } = true;
    public string? EspionageAs { get; init; }
    public string? EspionageOn { get; init; }
    public int? TravelerPlayer { get; set; }
    public TimingRules.TurnScope TurnScope { get; set; } = TimingRules.TurnScope.EveryTurn;
    public TimingRules.TurnPhasePoint PhasePoint { get; set; } = TimingRules.TurnPhasePoint.EndOfTurn;
    public int? ScopePlayer { get; set; }
    public int SavedHostOwner { get; set; }
    public Card? Extra { get; set; }
}
