using System;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Intention from UI / later network / later AI. Authority validates and applies.
/// Not a UI event — no pixel coordinates.
/// </summary>
public enum GameActionKind
{
    Pass,
    EndPhase,
    EndTurn,
    PlayCard,
    Respond,
    ChooseTarget,
    Beam,
    Fly,
    AttemptMission,
    EncounterDilemma,
    InitiateShipBattle,
    ActivateInPlay,
    Draw,
    Download,
    FlipHiddenAgenda,
    PlayTactic,
    BuildSite,
    SeedCard
}

/// <summary>
/// One legal or requested action. Targets are card/name identities, not Borders.
/// </summary>
public sealed class GameAction
{
    public GameActionKind Kind { get; init; }
    public int Player { get; init; }
    public Card? Card { get; init; }
    public Card? Target { get; init; }
    public Card? Target2 { get; init; }
    public string? TargetName { get; init; }
    public string? Note { get; init; }

    public string Id =>
        Kind + "|" + Player + "|" + (Card?.Name ?? "") + "|" + (Target?.Name ?? TargetName ?? "");

    public string Label
    {
        get
        {
            string who = $"P{Player}";
            string card = Card?.Name ?? "";
            string tgt = Target?.Name ?? TargetName ?? "";
            return Kind switch
            {
                GameActionKind.Pass => $"{who}: Pass (response)",
                GameActionKind.EndPhase when !string.IsNullOrEmpty(Note) => $"{who}: {Note}",
                GameActionKind.EndPhase => $"{who}: End Play phase → Execute",
                GameActionKind.EndTurn when !string.IsNullOrEmpty(Note) => $"{who}: {Note}",
                GameActionKind.EndTurn => $"{who}: End Execute / end turn",
                GameActionKind.SeedCard when tgt.Length > 0 => $"{who}: Seed {card} under {tgt}",
                GameActionKind.SeedCard => $"{who}: Seed {card}",
                GameActionKind.Draw => $"{who}: Draw",
                GameActionKind.PlayCard when tgt.Length > 0 => $"{who}: Play {card} on {tgt}",
                GameActionKind.PlayCard => $"{who}: Play {card}",
                GameActionKind.Respond => $"{who}: Respond {card}",
                GameActionKind.ChooseTarget => $"{who}: Choose {tgt}",
                GameActionKind.Beam => $"{who}: Beam" + (tgt.Length > 0 ? $" → {tgt}" : ""),
                GameActionKind.Fly => $"{who}: Fly" + (tgt.Length > 0 ? $" → {tgt}" : ""),
                GameActionKind.AttemptMission => $"{who}: Attempt {tgt}",
                GameActionKind.EncounterDilemma => $"{who}: Encounter {card}" + (tgt.Length > 0 ? $" at {tgt}" : ""),
                GameActionKind.InitiateShipBattle => $"{who}: Ship battle" + (tgt.Length > 0 ? $" vs {tgt}" : ""),
                GameActionKind.ActivateInPlay => $"{who}: Use {card}" + (tgt.Length > 0 ? $" ({tgt})" : ""),
                GameActionKind.Download => $"{who}: Download {card}" + (tgt.Length > 0 ? $" → {tgt}" : ""),
                GameActionKind.FlipHiddenAgenda => $"{who}: Flip {card}",
                GameActionKind.PlayTactic => $"{who}: Tactic {card}",
                GameActionKind.BuildSite => $"{who}: Build site {card}" + (tgt.Length > 0 ? $" on {tgt}" : ""),
                _ => $"{who}: {Kind}"
            };
        }
    }

    public static GameAction Pass(int player) =>
        new() { Kind = GameActionKind.Pass, Player = player };

    public static GameAction EndPhase(int player) =>
        new() { Kind = GameActionKind.EndPhase, Player = player };

    public static GameAction EndTurn(int player) =>
        new() { Kind = GameActionKind.EndTurn, Player = player };

    public static GameAction Play(int player, Card card, Card? target = null) =>
        new() { Kind = GameActionKind.PlayCard, Player = player, Card = card, Target = target };

    public static GameAction Respond(int player, Card card) =>
        new() { Kind = GameActionKind.Respond, Player = player, Card = card };

    public static GameAction Activate(int player, Card card, Card? target = null, string? note = null) =>
        new()
        {
            Kind = GameActionKind.ActivateInPlay,
            Player = player,
            Card = card,
            Target = target,
            Note = note
        };

    public static GameAction AttemptMission(int player, Card mission) =>
        new()
        {
            Kind = GameActionKind.AttemptMission,
            Player = player,
            Card = mission,
            Target = mission
        };

    public static GameAction EncounterDilemma(int player, Card dilemma, Card? mission = null) =>
        new()
        {
            Kind = GameActionKind.EncounterDilemma,
            Player = player,
            Card = dilemma,
            Target = mission
        };

    public static GameAction Download(int player, Card? card = null, Card? target = null, string? note = null) =>
        new()
        {
            Kind = GameActionKind.Download,
            Player = player,
            Card = card,
            Target = target,
            Note = note
        };

    public static GameAction FlipHiddenAgenda(int player, Card card) =>
        new()
        {
            Kind = GameActionKind.FlipHiddenAgenda,
            Player = player,
            Card = card
        };

    public static GameAction Fly(int player, Card ship, Card? destinationMission = null) =>
        new()
        {
            Kind = GameActionKind.Fly,
            Player = player,
            Card = ship,
            Target = destinationMission
        };

    public static GameAction Beam(int player, Card? fromHost = null, Card? toHost = null, string? note = null) =>
        new()
        {
            Kind = GameActionKind.Beam,
            Player = player,
            Card = fromHost,
            Target = toHost,
            Note = note
        };

    public static GameAction Seed(int player, Card card, Card? mission = null, string? note = null) =>
        new()
        {
            Kind = GameActionKind.SeedCard,
            Player = player,
            Card = card,
            Target = mission,
            Note = note
        };
}