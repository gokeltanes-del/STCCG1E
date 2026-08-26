using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarTrekCCG;

/// <summary>
/// Table snapshot (positions + zone lists + session). Mechanics are not compiled into the file,
/// so later code changes still load as long as card names/sets resolve.
/// Format: STCCG1E-Save-v1 (schema 2 adds instance/controller/stack/TurnScope).
/// Copy this whole file over the VS project GameSave.cs — TableWindow needs these types.
/// </summary>
public sealed class GameSave
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "STCCG1E-Save-v1";

    /// <summary>1 = original. 2 = instance/controller/stack/TurnScope fields present.</summary>
    [JsonPropertyName("schema")]
    public int Schema { get; set; } = 2;

    [JsonPropertyName("savedUtc")]
    public DateTime SavedUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("note")]
    public string Note { get; set; } = "Positions + piles + session. Card abilities re-applied from current rules on load.";

    [JsonPropertyName("deckNameP1")]
    public string? DeckNameP1 { get; set; }

    [JsonPropertyName("deckNameP2")]
    public string? DeckNameP2 { get; set; }

    [JsonPropertyName("session")]
    public SessionSnap Session { get; set; } = new();

    [JsonPropertyName("zones")]
    public Dictionary<string, List<CardRef>> Zones { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("table")]
    public List<TableCardSnap> Table { get; set; } = new();

    [JsonPropertyName("stacks")]
    public List<StackSnap> Stacks { get; set; } = new();

    [JsonPropertyName("seedUnder")]
    public List<StackSnap> SeedUnder { get; set; } = new();

    [JsonPropertyName("spaceline")]
    public List<int> Spaceline { get; set; } = new();

    [JsonPropertyName("attachedEvents")]
    public List<AttachedEventSnap> AttachedEvents { get; set; } = new();

    [JsonPropertyName("attachedDilemmas")]
    public List<AttachedDilemmaSnap> AttachedDilemmas { get; set; } = new();

    [JsonPropertyName("log")]
    public List<LogSnap> Log { get; set; } = new();

    [JsonPropertyName("stack")]
    public StackWindowSnap? Stack { get; set; }

    [JsonPropertyName("oncePerGame")]
    public List<string> OncePerGame { get; set; } = new();
}

public sealed class SessionSnap
{
    public string Match { get; set; } = "Setup";
    public string Segment { get; set; } = "Play";
    public int ActivePlayer { get; set; } = 1;
    public int TurnNumber { get; set; } = 1;
    public bool HasDrawn { get; set; }
    public bool SuppressDraw { get; set; }
    public bool NormalPlayUsed { get; set; }
    public bool NormalPlayForfeit { get; set; }
    public bool SeedPhaseActive { get; set; }
    public string SeedSubPhase { get; set; } = "Done";
    public int ScoreP1 { get; set; }
    public int ScoreP2 { get; set; }
    public bool HorgahnP1 { get; set; }
    public bool HorgahnP2 { get; set; }
    public bool HorgahnExtraUsed { get; set; }
    public int IonizationBeamsThisTurn { get; set; }
    public int RedAlertPlaysLeft { get; set; }
    public int PointsToWin { get; set; } = 100;
    public int? Winner { get; set; }
    public int NextInstanceId { get; set; }
}

public sealed class CardRef
{
    public string Name { get; set; } = "";
    public string? Set { get; set; }
    public string? Type { get; set; }
    public int InstanceId { get; set; }
    public int Owner { get; set; }
    public int Controller { get; set; }
    public bool FaceUp { get; set; } = true;
}

public sealed class TableCardSnap
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Set { get; set; }
    public string? Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Z { get; set; }
    public int Owner { get; set; }
    public bool Visible { get; set; } = true;
    public int Hull { get; set; }
    public bool Stopped { get; set; }
    public int? RangeLeft { get; set; }
    public int RepairTurns { get; set; }
    public int? SolvedBy { get; set; }
    public int InstanceId { get; set; }
    public int Controller { get; set; }
    public bool FaceUp { get; set; } = true;
}

public sealed class StackSnap
{
    public int HostId { get; set; }
    public List<int> ChildIds { get; set; } = new();
}

public sealed class AttachedEventSnap
{
    public CardRef Card { get; set; } = new();
    public string Kind { get; set; } = "";
    public int Owner { get; set; }
    public int? HostId { get; set; }
    public int? Host2Id { get; set; }
    public int Countdown { get; set; }
    public bool FaceUp { get; set; } = true;
    public string? EspionageAs { get; set; }
    public string? EspionageOn { get; set; }
    public string? TurnScope { get; set; }
    public string? PhasePoint { get; set; }
    public int? ScopePlayer { get; set; }
}

public sealed class AttachedDilemmaSnap
{
    public CardRef Card { get; set; } = new();
    public string Kind { get; set; } = "";
    public int HostId { get; set; }
    public int Countdown { get; set; }
}

public sealed class LogSnap
{
    public DateTime Utc { get; set; }
    public int Turn { get; set; }
    public string Actor { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class StackWindowSnap
{
    public bool Open { get; set; }
    public int ResponsePlayer { get; set; }
    public int ConsecutivePasses { get; set; }
    public List<StackItemSnap> Items { get; set; } = new();
}

public sealed class StackItemSnap
{
    public string Kind { get; set; } = "";
    public int Controller { get; set; }
    public string? Card { get; set; }
    public string? Target { get; set; }
    public string Summary { get; set; } = "";
    public bool IsResponse { get; set; }
    public bool Cancelled { get; set; }
}