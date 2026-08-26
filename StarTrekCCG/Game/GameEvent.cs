using System;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// What the authority produced after Validate+Apply. UI / log / later net clients consume these.
/// </summary>
public enum GameEventKind
{
    Info,
    Denied,
    CardPlayed,
    CardMoved,
    Nullified,
    Attached,
    Detached,
    CountdownSet,
    CountdownTicked,
    InstantResolved,
    DilemmaResolved,
    SegmentChanged,
    TurnChanged,
    ScoreChanged
}

public sealed class GameEvent
{
    public GameEventKind Kind { get; init; }
    public int Player { get; init; }
    public Card? Card { get; init; }
    public Card? Target { get; init; }
    public string Message { get; init; } = "";
    public string? TemplateId { get; init; }

    public string Format()
    {
        string t = TemplateId != null ? $"[{TemplateId}] " : "";
        return t + Message;
    }

    public static GameEvent Info(int player, string message, string? template = null) =>
        new() { Kind = GameEventKind.Info, Player = player, Message = message, TemplateId = template };

    public static GameEvent Denied(int player, string message, Card? card = null, string? template = null) =>
        new()
        {
            Kind = GameEventKind.Denied,
            Player = player,
            Card = card,
            Message = message,
            TemplateId = template
        };
}