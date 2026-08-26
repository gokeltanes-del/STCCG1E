using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Reusable ability template. Card JSON is data; this is behavior.
/// Existing *Rules catalogs stay the source of Premiere parameters —
/// templates call them instead of copying card text.
/// </summary>
public interface IEffect
{
    /// <summary>Stable id used in LegalMoves / events (not a card name).</summary>
    string TemplateId { get; }

    /// <summary>Human label for debug lists.</summary>
    string DisplayName { get; }

    bool Matches(Card card);

    (bool ok, string reason) CanPlay(GameState state, GameAction action);

    /// <summary>
    /// Authority decision + catalog payload. Does not mutate TableWindow.
    /// UI applies the returned events / catalog result.
    /// </summary>
    ApplyResult Apply(GameState state, GameAction action);
}

public sealed class ApplyResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
    public string? TemplateId { get; init; }
    public List<GameEvent> Events { get; } = new();

    public EventRules.PlayResult? EventPlay { get; init; }
    public InterruptRules.Result? Interrupt { get; init; }
    public DilemmaRules.Result? Dilemma { get; init; }
    public ArtifactRules.AcquireResult? Artifact { get; init; }

    public static ApplyResult Deny(string message, string? template = null, int player = 0, Card? card = null)
    {
        var r = new ApplyResult { Ok = false, Message = message, TemplateId = template };
        r.Events.Add(GameEvent.Denied(player, message, card, template));
        return r;
    }

    public static ApplyResult OkResult(string message, string template)
    {
        var r = new ApplyResult { Ok = true, Message = message, TemplateId = template };
        r.Events.Add(GameEvent.Info(0, message, template));
        return r;
    }
}