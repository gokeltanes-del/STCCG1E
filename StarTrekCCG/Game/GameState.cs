using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Session + UI-only fields for <see cref="BoardStore.ToGameState"/>.
/// Locations / occupants / TABLE ids come from the store when it has them.
/// </summary>
public sealed class GameStateSeed
{
    public GameSession.MatchPhase Match { get; init; }
    public GameSession.TurnSegment Segment { get; init; }
    public int ActivePlayer { get; init; } = 1;
    public int TurnNumber { get; init; } = 1;
    public bool SeedPhase { get; init; }
    public int SeedSubPhase { get; init; }
    public IReadOnlyList<Card> SeedPileP1 { get; init; } = Array.Empty<Card>();
    public IReadOnlyList<Card> SeedPileP2 { get; init; } = Array.Empty<Card>();
    public int CryoPersonnelSeededP1 { get; init; }
    public int CryoPersonnelSeededP2 { get; init; }
    public bool NormalCardPlayAvailable { get; init; }
    public bool NormalCardPlayUsed { get; init; }
    public bool StackOpen { get; init; }
    public int ResponsePlayer { get; init; }
    public TimingRules.PendingAction? StackTop { get; init; }
    public int ScoreP1 { get; init; }
    public int ScoreP2 { get; init; }
    public IReadOnlyList<Card> HandP1 { get; init; } = Array.Empty<Card>();
    public IReadOnlyList<Card> HandP2 { get; init; } = Array.Empty<Card>();
    public IReadOnlyList<BoardPiece> UiBoard { get; init; } = Array.Empty<BoardPiece>();
    public bool HasGoddess { get; init; }
    public bool TentOpenP1 { get; init; }
    public bool TentOpenP2 { get; init; }
    public int TentCountP1 { get; init; }
    public int TentCountP2 { get; init; }
    public IReadOnlyList<string> Spaceline { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TreatyRules.TreatyLink> TreatiesP1 { get; init; } =
        Array.Empty<TreatyRules.TreatyLink>();
    public IReadOnlyList<TreatyRules.TreatyLink> TreatiesP2 { get; init; } =
        Array.Empty<TreatyRules.TreatyLink>();
    public bool HasWhereNoOneHasGoneBeforeP1 { get; init; }
    public bool HasWhereNoOneHasGoneBeforeP2 { get; init; }
    public bool TentDownloadUsedP1 { get; init; }
    public bool TentDownloadUsedP2 { get; init; }
    public IReadOnlyList<string> OncePerGameKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> StoppedInstanceIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> UntilEndOfTurnKeys { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Engine-facing snapshot. No layout pixels, no card-text cache as source of truth.
/// Board pieces prefer <see cref="BoardStore"/>; session fields come from <see cref="GameStateSeed"/>.
/// </summary>
public sealed class GameState
{
    public GameSession.MatchPhase Match { get; init; }
    public GameSession.TurnSegment Segment { get; init; }
    public int ActivePlayer { get; init; } = 1;
    public int TurnNumber { get; init; } = 1;
    public bool SeedPhase { get; init; }
    /// <summary>Matches <see cref="SeedSubPhase"/> int values while TableWindow still owns the UI enum.</summary>
    public int SeedSubPhase { get; init; }
    public IReadOnlyList<Card> SeedPileP1 { get; init; } = Array.Empty<Card>();
    public IReadOnlyList<Card> SeedPileP2 { get; init; } = Array.Empty<Card>();
    public int CryoPersonnelSeededP1 { get; init; }
    public int CryoPersonnelSeededP2 { get; init; }
    public bool NormalCardPlayAvailable { get; init; }
    public bool NormalCardPlayUsed { get; init; }

    public bool StackOpen { get; init; }
    public int ResponsePlayer { get; init; }
    public TimingRules.PendingAction? StackTop { get; init; }

    public int ScoreP1 { get; init; }
    public int ScoreP2 { get; init; }

    public IReadOnlyList<Card> HandP1 { get; init; } = Array.Empty<Card>();
    public IReadOnlyList<Card> HandP2 { get; init; } = Array.Empty<Card>();

    public IReadOnlyList<BoardPiece> Board { get; init; } = Array.Empty<BoardPiece>();

    public bool HasGoddess { get; init; }
    public bool TentOpenP1 { get; init; }
    public bool TentOpenP2 { get; init; }
    public int TentCountP1 { get; init; }
    public int TentCountP2 { get; init; }

    /// <summary>Mission names in spaceline order (left → right).</summary>
    public IReadOnlyList<string> Spaceline { get; init; } = Array.Empty<string>();

    public IReadOnlyList<TreatyRules.TreatyLink> TreatiesP1 { get; init; } =
        Array.Empty<TreatyRules.TreatyLink>();
    public IReadOnlyList<TreatyRules.TreatyLink> TreatiesP2 { get; init; } =
        Array.Empty<TreatyRules.TreatyLink>();

    /// <summary>WNOHGB on P1 TABLE. Printed: "You may move ships…" = controller only.</summary>
    public bool HasWhereNoOneHasGoneBeforeP1 { get; init; }
    /// <summary>WNOHGB on P2 TABLE.</summary>
    public bool HasWhereNoOneHasGoneBeforeP2 { get; init; }
    /// <summary>Either player has WNOHGB (legacy callers). Fly/RANGE use <see cref="HasWnohgb"/>.</summary>
    public bool HasWhereNoOneHasGoneBefore =>
        HasWhereNoOneHasGoneBeforeP1 || HasWhereNoOneHasGoneBeforeP2;

    public bool HasWnohgb(int player) =>
        player == 2 ? HasWhereNoOneHasGoneBeforeP2 : HasWhereNoOneHasGoneBeforeP1;

    public bool TentDownloadUsedP1 { get; init; }
    public bool TentDownloadUsedP2 { get; init; }

    /// <summary>Keys from GameSession.OncePerGame (includes Special Download marks).</summary>
    public IReadOnlyList<string> OncePerGameKeys { get; init; } = Array.Empty<string>();

    public IReadOnlyList<int> StoppedInstanceIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> UntilEndOfTurnKeys { get; init; } = Array.Empty<string>();

    public bool IsStoppedInstance(int instanceId) =>
        instanceId != 0 && StoppedInstanceIds.Contains(instanceId);

    public bool TentOpen(int player) => player == 2 ? TentOpenP2 : TentOpenP1;
    public int TentCount(int player) => player == 2 ? TentCountP2 : TentCountP1;
    public bool TentDownloadUsed(int player) =>
        player == 2 ? TentDownloadUsedP2 : TentDownloadUsedP1;

    public bool SpecialDownloadUsed(int player, Card source)
    {
        string key = $"P{player}|{DownloadRules.SpecialDownloadKey(source)}";
        return OncePerGameKeys.Any(k =>
            k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<TreatyRules.TreatyLink> TreatiesOf(int player) =>
        player == 2 ? TreatiesP2 : TreatiesP1;

    public IReadOnlyList<Card> HandOf(int player) =>
        player == 2 ? HandP2 : HandP1;

    public IReadOnlyList<Card> SeedPileOf(int player) =>
        player == 2 ? SeedPileP2 : SeedPileP1;

    public int CryoPersonnelSeeded(int player) =>
        player == 2 ? CryoPersonnelSeededP2 : CryoPersonnelSeededP1;

    public SeedSubPhase CurrentSeedPhase => SeedRules.FromInt(SeedSubPhase);

    /// <summary>
    /// E5: name-order fallback for LegalMoves Fly when BoardStore line empty; IM may still use names.
    /// </summary>
    public List<Card> OrderedMissions()
    {
        var list = new List<Card>();
        foreach (var name in Spaceline)
        {
            var m = Missions().FirstOrDefault(p =>
                string.Equals(p.Card.Name, name, StringComparison.OrdinalIgnoreCase));
            if (m != null) { list.Add(m.Card); continue; }
            // Gaps in Normal Space sits on the spaceline as a landable location.
            var loc = Board.FirstOrDefault(p =>
                string.Equals(p.Card.Name, name, StringComparison.OrdinalIgnoreCase));
            if (loc != null) list.Add(loc.Card);
        }
        if (list.Count == 0)
            list.AddRange(Missions().OrderBy(p => p.SpacelineIndex).Select(p => p.Card));
        return list;
    }

    public IEnumerable<BoardPiece> Ships() =>
        Board.Where(p => p.Kind == BoardPieceKind.Ship);

    public IEnumerable<BoardPiece> Facilities() =>
        Board.Where(p => p.Kind == BoardPieceKind.Facility);

    public IEnumerable<BoardPiece> Missions() =>
        Board.Where(p => p.Kind == BoardPieceKind.Mission);

    public IEnumerable<BoardPiece> EventsInPlay() =>
        Board.Where(p => p.Kind == BoardPieceKind.Event);

    public BoardPiece? PieceNamed(string name) =>
        Board.FirstOrDefault(p =>
            string.Equals(p.Card.Name, name, StringComparison.OrdinalIgnoreCase));
}

public enum BoardPieceKind
{
    Other,
    Ship,
    Mission,
    Facility,
    Event,
    InterruptToken,
    Dilemma,
    Incident,
    Objective,
    Tactic,
    Site,
    TimeLocation,
    Artifact
}

/// <summary>One in-play object the engine can target or reason about.</summary>
public sealed class BoardPiece
{
    public required Card Card { get; init; }
    public BoardPieceKind Kind { get; init; }
    public int Owner { get; init; }
    public int Controller { get; init; }
    public int InstanceId { get; init; }
    public bool FaceUp { get; init; } = true;
    public CardZone Zone { get; init; }
    public CardStatus Status { get; init; }
    public string? HostName { get; init; }
    public bool Occupied { get; init; }
    public bool HasSecurityAboard { get; init; }
    public bool HasEngineerAboard { get; init; }
    public EventRules.Persist Persist { get; init; }
    public int Countdown { get; init; }
    public TimingRules.TurnScope TurnScope { get; init; }
    public TimingRules.TurnPhasePoint PhasePoint { get; init; }

    public bool MissionSolved { get; init; }
    public bool AttemptBlocked { get; init; }
    public string? AttemptBlockReason { get; init; }
    public int RangeLeft { get; init; } = -1;
    public bool Stopped { get; init; }
    public bool Cloaked { get; init; }
    /// <summary>Facility InstanceId when docked; 0 = not docked.</summary>
    public int DockedAtId { get; init; }
    /// <summary>Hull damage 0-100. -1 = unset.</summary>
    public int HullPercent { get; init; } = -1;

    /// <summary>Ship staffing satisfied (or Rogue Borg + Lore).</summary>
    public bool Staffed { get; init; }

    public string? StaffReason { get; init; }

    /// <summary>Index on GameState.Spaceline when Kind is Mission; −1 unknown.</summary>
    public int SpacelineIndex { get; init; } = -1;

    /// <summary>Crew / personnel aboard a ship (for staffing & movement).</summary>
    public IReadOnlyList<Card> Aboard { get; init; } = Array.Empty<Card>();
}