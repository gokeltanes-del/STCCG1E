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
    SeedCard,
    /// <summary>Attach Radioactive Garbage Scow to a ship (Tractor Beam). Not a move.</summary>
    TowScow,
    /// <summary>
    /// Start a personnel battle. Card is the attacking host (ship or mission).
    /// Target is the opposing host, or null when the Host must choose it.
    /// </summary>
    InitiatePersonnelBattle
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
                GameActionKind.TowScow => $"{who}: Tow Scow with {card}",
                GameActionKind.AttemptMission when Target2 != null => $"{who}: Attempt {tgt} ({Target2.Name})",
                GameActionKind.AttemptMission => $"{who}: Attempt {tgt}",
                GameActionKind.EncounterDilemma => $"{who}: Encounter {card}" + (tgt.Length > 0 ? $" at {tgt}" : ""),
                GameActionKind.InitiateShipBattle => $"{who}: Ship battle" + (tgt.Length > 0 ? $" vs {tgt}" : ""),
                GameActionKind.InitiatePersonnelBattle when tgt.Length > 0 => $"{who}: Personnel battle {card} vs {tgt}",
                GameActionKind.InitiatePersonnelBattle => $"{who}: Personnel battle {card}",
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

    public static GameAction Play(int player, Card card, Card? target = null, string? note = null) =>
        new() { Kind = GameActionKind.PlayCard, Player = player, Card = card, Target = target, Note = note };

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

    /// <summary>
    /// Mission attempt. Card and Target are the mission (<see cref="EngineAuthority"/>).
    /// Target2 is the Attempting-Ship for a space mission; null on a planet.
    /// </summary>
    public static GameAction AttemptMission(int player, Card mission, Card? attemptingShip = null) =>
        new()
        {
            Kind = GameActionKind.AttemptMission,
            Player = player,
            Card = mission,
            Target = mission,
            Target2 = attemptingShip
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

    public static GameAction TowScow(int player, Card ship, Card scow) =>
        new()
        {
            Kind = GameActionKind.TowScow,
            Player = player,
            Card = ship,
            Target = scow
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


    public static GameAction ShipBattle(int player, Card attacker, Card defender, string? note = null) =>
        new()
        {
            Kind = GameActionKind.InitiateShipBattle,
            Player = player,
            Card = attacker,
            Target = defender,
            Note = note
        };

    /// <summary>
    /// Personnel battle. Card is the attacking host. Target is the opposing host when already chosen.
    /// </summary>
    public static GameAction PersonnelBattle(int player, Card sourceHost, Card? targetHost = null) =>
        new()
        {
            Kind = GameActionKind.InitiatePersonnelBattle,
            Player = player,
            Card = sourceHost,
            Target = targetHost
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