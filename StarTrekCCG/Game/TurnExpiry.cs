using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// "Until end of turn" bag. Process before the EOT draw, then the session list is cleared.
/// Attach effects here instead of new TableWindow name checks.
/// </summary>
public sealed class ExpiringEffect
{
    public string Key { get; init; } = "";
    public int Owner { get; init; }
    public Card? Card { get; init; }
    /// <summary>Discard | Flag</summary>
    public string Verb { get; init; } = "Discard";
    public string Note { get; init; } = "";
}

public static class TurnExpiry
{
    public const string VerbDiscard = "Discard";
    public const string VerbFlag = "Flag";
    public const string VerbUnmod = "Unmod";

    public static void Register(GameSession session, ExpiringEffect effect)
    {
        session.Expiring.Add(effect);
        if (!string.IsNullOrWhiteSpace(effect.Key))
            session.UntilEndOfTurn.Add(effect.Key);
    }

    public static void RegisterFlag(GameSession session, int owner, string key, string? note = null)
    {
        Register(session, new ExpiringEffect
        {
            Key = key,
            Owner = owner,
            Verb = VerbFlag,
            Note = note ?? ""
        });
    }

    public static IReadOnlyList<ExpiringEffect> Due(GameSession session, int finishingPlayer)
    {
        var list = new List<ExpiringEffect>();
        foreach (var e in session.Expiring)
        {
            if (e.Owner == 0 || e.Owner == finishingPlayer)
                list.Add(e);
        }
        return list;
    }

    public static void Consume(GameSession session, ExpiringEffect effect)
    {
        session.Expiring.Remove(effect);
        if (!string.IsNullOrWhiteSpace(effect.Key))
            session.UntilEndOfTurn.Remove(effect.Key);
    }
}