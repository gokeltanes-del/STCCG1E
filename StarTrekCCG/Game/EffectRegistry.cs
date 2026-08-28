using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Maps cards → templates. Premiere catalogs are called, not duplicated.
/// Add a row here (or a new IEffect) instead of a TableWindow special path.
/// </summary>
public static class EffectRegistry
{
    private static readonly IEffect[] Templates =
    {
        new NullifyInPlayEffect(),
        new NullifyStackEffect(),
        new AttachEndOfTurnEffect(),
        new CountdownNextTurnEffect(),
        new ShipModEffect(),
        new MissionModEffect(),
        new SpacelineSpanEffect(),
        new WallDilemmaEffect(),
        new DilemmaKillEffect(),
        new DilemmaSpaceEffect(),
        new DilemmaAttachEffect(),
        new DilemmaSpecialEffect(),
        new ArtifactAcquireEffect(),
        new InstantEventEffect(),
        new InterruptPlayEffect(),
        new HiddenAgendaEffect(),
        new CorePermanentEffect()
    };

    public static IReadOnlyList<IEffect> All => Templates;

    public static IEffect? Find(Card? card)
    {
        if (card == null) return null;
        // Explicit map wins (Premiere / AU registry rows).
        var mapped = CardEffectMap.TemplateIdFor(card);
        if (mapped != null)
        {
            var byId = ById(mapped);
            if (byId != null) return byId;
        }
        foreach (var t in Templates)
            if (t.Matches(card)) return t;
        return null;
    }

    public static IEffect? ById(string templateId) =>
        Templates.FirstOrDefault(t => t.TemplateId.Equals(templateId, StringComparison.OrdinalIgnoreCase));

    public static (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        var fx = Find(action.Card);
        if (fx == null)
            return (false, "No registered template for this card (generic play still allowed by UI).");
        return fx.CanPlay(state, action);
    }

    public static ApplyResult Apply(GameState state, GameAction action)
    {
        var fx = Find(action.Card);
        if (fx == null)
            return ApplyResult.Deny("No registered template.", player: action.Player, card: action.Card);
        var gate = fx.CanPlay(state, action);
        if (!gate.ok)
            return ApplyResult.Deny(gate.reason, fx.TemplateId, action.Player, action.Card);
        return fx.Apply(state, action);
    }
}

// ---------------------------------------------------------------------------
// 1) Nullify in-play — Kevin Uxbridge (TimingRules.CanKevinTargetEvent)
// ---------------------------------------------------------------------------
internal sealed class NullifyInPlayEffect : IEffect
{
    public string TemplateId => "nullify-inplay";
    public string DisplayName => "Nullify in-play (Kevin)";

    public bool Matches(Card card) =>
        InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Card == null) return (false, "No card.");
        bool devil = InterruptRules.IsDevil(action.Card);

        if (action.Kind == GameActionKind.Respond)
        {
            if (!state.StackOpen || state.StackTop == null)
                return (false, "No open stack to respond to.");
            return TimingRules.CanRespond(action.Card, state.StackTop, action.Player);
        }

        var target = action.Target;
        if (target == null)
            return (false, devil
                ? "The Devil: choose a Treaty, Horga'hn, or Wind Dancer in play."
                : "Kevin Uxbridge: choose an Event in play.");
        return devil
            ? TimingRules.CanDevilTarget(target)
            : TimingRules.CanKevinTargetEvent(target);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var ir = InterruptRules.Resolve(action.Card!);
        string who = action.Card?.Name ?? "Nullify";
        string msg = action.Target != null
            ? $"Nullify {action.Target.Name} ({who})."
            : ir.Message;
        var result = new ApplyResult
        {
            Ok = true,
            Message = msg,
            TemplateId = TemplateId,
            Interrupt = ir
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Nullified,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = msg,
            TemplateId = TemplateId
        });
        return result;
    }

    private static bool NameIs(Card c, string n) =>
        (c.Name ?? "").Equals(n, StringComparison.OrdinalIgnoreCase);
}

// ---------------------------------------------------------------------------
// 2) Attach + EOT tick — Plasma Fire (EventRules catalog)
// ---------------------------------------------------------------------------
internal sealed class AttachEndOfTurnEffect : IEffect
{
    public string TemplateId => "attach-eot";
    public string DisplayName => "Attach + end-of-turn (Plasma Fire)";

    public bool Matches(Card card) =>
        (card.Name ?? "").Equals("Plasma Fire", StringComparison.OrdinalIgnoreCase);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (action.Kind == GameActionKind.ActivateInPlay)
        {
            bool warp = EventRules.NameIs(action.Card, "Warp Core Breach");
            var ship = state.Ships().FirstOrDefault(s =>
                action.Target != null
                    ? NamesEqual(s.Card, action.Target)
                    : s.Card.Name != null && (warp ? HasWarpCoreOn(state, s) : HasPlasmaOn(state, s)));
            if (ship == null)
                return (false, warp ? "No ship with Warp Core Breach." : "No ship with Plasma Fire.");
            if (warp)
            {
                if (!ship.HasEngineerAboard)
                    return (false, "Need ENGINEER aboard to nullify Warp Core Breach.");
                return (true, "Nullify Warp Core Breach.");
            }
            if (!ship.HasSecurityAboard)
                return (false, "Need SECURITY aboard to nullify Plasma Fire.");
            return (true, "Nullify Plasma Fire.");
        }

        if (!state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card!))
            return (false, "Normal card play not available.");
        if (action.Target == null)
            return (false, "Plasma Fire: play on a ship.");
        var piece = state.Ships().FirstOrDefault(s => NamesEqual(s.Card, action.Target));
        if (piece == null)
            return (false, "Plasma Fire: target is not a ship in play.");
        return (true, EventRules.ResolvePlay(action.Card!).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        if (action.Kind == GameActionKind.ActivateInPlay)
        {
            bool warp = EventRules.NameIs(action.Card, "Warp Core Breach");
            var r = ApplyResult.OkResult(
                warp ? "Warp Core Breach nullified (ENGINEER)." : "Plasma Fire nullified (SECURITY).",
                TemplateId);
            r.Events.Add(new GameEvent
            {
                Kind = GameEventKind.Detached,
                Player = action.Player,
                Card = action.Card,
                Target = action.Target,
                Message = r.Message,
                TemplateId = TemplateId
            });
            return r;
        }

        var play = EventRules.ResolvePlay(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = play.Message,
            TemplateId = TemplateId,
            EventPlay = play
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Attached,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = play.Message,
            TemplateId = TemplateId
        });
        return result;
    }

    private static bool HasPlasmaOn(GameState state, BoardPiece ship) =>
        state.Board.Any(p =>
            p.Persist == EventRules.Persist.PlasmaFire
            && string.Equals(p.HostName, ship.Card.Name, StringComparison.OrdinalIgnoreCase));

    private static bool HasWarpCoreOn(GameState state, BoardPiece ship) =>
        state.Board.Any(p =>
            p.Persist == EventRules.Persist.WarpCore
            && string.Equals(p.HostName, ship.Card.Name, StringComparison.OrdinalIgnoreCase));

    private static bool NamesEqual(Card a, Card b) =>
        (a.Name ?? "").Equals(b.Name ?? "", StringComparison.OrdinalIgnoreCase);
}

// ---------------------------------------------------------------------------
// 3) Countdown / next-turn discard — Crosis (InterruptRules + TurnScope.NextTurn)
// ---------------------------------------------------------------------------
internal sealed class CountdownNextTurnEffect : IEffect
{
    public string TemplateId => "countdown-nextturn";
    public string DisplayName => "Countdown / next-turn discard (Crosis)";

    public bool Matches(Card card) =>
        (card.Name ?? "").Equals("Crosis", StringComparison.OrdinalIgnoreCase);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (action.Target == null)
            return (false, "Crosis: play on a ship.");
        var ship = state.Ships().FirstOrDefault(s =>
            (s.Card.Name ?? "").Equals(action.Target.Name, StringComparison.OrdinalIgnoreCase));
        if (ship == null)
            return (false, "Crosis: target is not a ship in play.");
        return (true, InterruptRules.Resolve(action.Card!).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var ir = InterruptRules.Resolve(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = ir.Message,
            TemplateId = TemplateId,
            Interrupt = ir
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Attached,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = ir.Message,
            TemplateId = TemplateId
        });
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.CountdownSet,
            Player = action.Player,
            Card = action.Card,
            Message = "Countdown 1 · " + TimingRules.DescribeScope(TimingRules.TurnScope.NextTurn),
            TemplateId = TemplateId
        });
        return result;
    }
}

// ---------------------------------------------------------------------------
// 4) Wall dilemma — Premiere walls via DilemmaRules.Resolve
// ---------------------------------------------------------------------------
internal static class DilemmaTemplate
{
    public static (bool ok, string reason) CanEncounter(GameAction action, string label)
    {
        if (action.Kind != GameActionKind.EncounterDilemma)
            return (false, $"{label} resolve during a mission attempt.");
        if (action.Card == null) return (false, "No dilemma.");
        return (true, $"Encounter {label}.");
    }

    public static ApplyResult Stamp(string templateId, Card? dilemma) =>
        ApplyResult.OkResult(
            $"{templateId} ready for {dilemma?.Name} (DilemmaRules.Resolve).",
            templateId);

    public static ApplyResult FromCatalog(string templateId, Card dilemma, DilemmaRules.Result catalog)
    {
        var r = new ApplyResult
        {
            Ok = true,
            Message = catalog.Message,
            TemplateId = templateId,
            Dilemma = catalog
        };
        r.Events.Add(new GameEvent
        {
            Kind = GameEventKind.DilemmaResolved,
            Card = dilemma,
            Message = $"{dilemma.Name}: {catalog.Fate} — {catalog.Message}",
            TemplateId = templateId
        });
        return r;
    }
}

internal sealed class WallDilemmaEffect : IEffect
{
    public string TemplateId => "wall-dilemma";
    public string DisplayName => "Wall dilemma";

    public bool Matches(Card card) => CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action) =>
        DilemmaTemplate.CanEncounter(action, "Wall dilemmas");

    public ApplyResult Apply(GameState state, GameAction action) =>
        DilemmaTemplate.Stamp(TemplateId, action.Card);

    public static ApplyResult FromCatalog(Card dilemma, DilemmaRules.Result catalog) =>
        DilemmaTemplate.FromCatalog("wall-dilemma", dilemma, catalog);
}

internal sealed class DilemmaKillEffect : IEffect
{
    public string TemplateId => "dilemma-kill";
    public string DisplayName => "Kill / filter dilemma";

    public bool Matches(Card card) => CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action) =>
        DilemmaTemplate.CanEncounter(action, "Kill dilemmas");

    public ApplyResult Apply(GameState state, GameAction action) =>
        DilemmaTemplate.Stamp(TemplateId, action.Card);
}

internal sealed class DilemmaSpaceEffect : IEffect
{
    public string TemplateId => "dilemma-space";
    public string DisplayName => "Space-filter dilemma";

    public bool Matches(Card card) => CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action) =>
        DilemmaTemplate.CanEncounter(action, "Space dilemmas");

    public ApplyResult Apply(GameState state, GameAction action) =>
        DilemmaTemplate.Stamp(TemplateId, action.Card);
}

internal sealed class DilemmaAttachEffect : IEffect
{
    public string TemplateId => "dilemma-attach";
    public string DisplayName => "Attach / persist dilemma";

    public bool Matches(Card card) => CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action) =>
        DilemmaTemplate.CanEncounter(action, "Attach dilemmas");

    public ApplyResult Apply(GameState state, GameAction action) =>
        DilemmaTemplate.Stamp(TemplateId, action.Card);
}

internal sealed class DilemmaSpecialEffect : IEffect
{
    public string TemplateId => "dilemma-special";
    public string DisplayName => "Special dilemma (relocate / Q / unique)";

    public bool Matches(Card card) => CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action) =>
        DilemmaTemplate.CanEncounter(action, "Special dilemmas");

    public ApplyResult Apply(GameState state, GameAction action) =>
        DilemmaTemplate.Stamp(TemplateId, action.Card);
}

internal sealed class ArtifactAcquireEffect : IEffect
{
    public string TemplateId => "artifact-acquire";
    public string DisplayName => "Artifact acquire";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId || ArtifactRules.IsArtifact(card);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Card == null) return (false, "No artifact.");
        // Seed under mission or later hand-play after ToHand acquire.
        if (action.Kind == GameActionKind.PlayCard
            && !state.NormalCardPlayAvailable
            && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");
        return (true, ArtifactRules.ResolveAcquire(action.Card).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var acq = ArtifactRules.ResolveAcquire(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = acq.Message,
            TemplateId = TemplateId,
            Artifact = acq
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.CardPlayed,
            Player = action.Player,
            Card = action.Card,
            Message = acq.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

// ---------------------------------------------------------------------------
// 5) Instant event — Kivas Fajo: Collector (EventRules)
// ---------------------------------------------------------------------------
// ---------------------------------------------------------------------------
// ship-mod — Bynars / Metaphasic / Nutational / Spacedock / Neural Servo / Lore Returns
// ---------------------------------------------------------------------------
internal sealed class ShipModEffect : IEffect
{
    public string TemplateId => "ship-mod";
    public string DisplayName => "Ship / outpost modifier event";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (action.Card == null) return (false, "No card.");
        if (!state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");

        var play = EventRules.ResolvePlay(action.Card);
        if (play.Place is EventRules.Place.OnShip or EventRules.Place.OnOutpost)
        {
            if (action.Target == null)
                return (false, $"{action.Card.Name}: choose a ship or outpost.");
        }
        return (true, play.Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var play = EventRules.ResolvePlay(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = play.Message,
            TemplateId = TemplateId,
            EventPlay = play
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Attached,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = play.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

// ---------------------------------------------------------------------------
// mission-mod — Espionage / Ionization / Distortion / Rift / Tetryon / Supernova
// ---------------------------------------------------------------------------
internal sealed class MissionModEffect : IEffect
{
    public string TemplateId => "mission-mod";
    public string DisplayName => "Mission / planet modifier event";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (action.Card == null) return (false, "No card.");
        if (!state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");

        var play = EventRules.ResolvePlay(action.Card);
        if (play.Place is EventRules.Place.OnMission or EventRules.Place.OnPlanet)
        {
            if (action.Target == null)
                return (false, $"{action.Card.Name}: choose a mission.");
        }
        return (true, play.Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var play = EventRules.ResolvePlay(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = play.Message,
            TemplateId = TemplateId,
            EventPlay = play
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Attached,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = play.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

// ---------------------------------------------------------------------------
// spaceline-span — Gaps / Q-Net
// ---------------------------------------------------------------------------
internal sealed class SpacelineSpanEffect : IEffect
{
    public string TemplateId => "spaceline-span";
    public string DisplayName => "Spaceline span (Gaps / Q-Net)";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId;

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (action.Card == null) return (false, "No card.");
        if (!state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");
        // Gap targeting is two missions — UI still supplies hosts; engine only checks play window.
        return (true, EventRules.ResolvePlay(action.Card).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var play = EventRules.ResolvePlay(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = play.Message,
            TemplateId = TemplateId,
            EventPlay = play
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Attached,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = play.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

// ---------------------------------------------------------------------------
// interrupt-play — generic Premiere interrupt (InterruptRules.Resolve payload)
// ---------------------------------------------------------------------------
internal sealed class InterruptPlayEffect : IEffect
{
    public string TemplateId => "interrupt-play";
    public string DisplayName => "Interrupt (catalog)";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId
        || (CardKinds.IsInterrupt(card) && CardEffectMap.TemplateIdFor(card) == null);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Card == null) return (false, "No card.");
        if (state.SeedPhase) return (false, "Not during seed.");
        if (state.HasGoddess && !EventRules.IsGoddessException(action.Card))
            return (false, "Goddess of Empathy: interrupts blocked.");
        if (action.Kind == GameActionKind.Respond)
        {
            if (!state.StackOpen || state.StackTop == null)
                return (false, "No open stack.");
            return TimingRules.CanRespond(action.Card, state.StackTop, action.Player);
        }
        // Most Premiere interrupts are "play as response or at any time" — UI/TimingRules gate the rest.
        return (true, InterruptRules.Resolve(action.Card).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var ir = InterruptRules.Resolve(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = ir.Message,
            TemplateId = TemplateId,
            Interrupt = ir
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.CardPlayed,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = ir.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

internal sealed class InstantEventEffect : IEffect
{
    public string TemplateId => "instant-event";
    public string DisplayName => "Instant event";

    public bool Matches(Card card) =>
        CardEffectMap.TemplateIdFor(card) == TemplateId
        || (card.Name ?? "").Equals("Kivas Fajo: Collector", StringComparison.OrdinalIgnoreCase);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (state.SeedPhase) return (false, "Not during seed.");
        if (state.StackOpen && action.Kind != GameActionKind.Respond && action.Kind != GameActionKind.PlayCard)
            return (false, "Stack is open.");
        if (!state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card!))
            return (false, "Normal card play not available.");
        return (true, EventRules.ResolvePlay(action.Card!).Message);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var play = EventRules.ResolvePlay(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = play.Message,
            TemplateId = TemplateId,
            EventPlay = play
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.InstantResolved,
            Player = action.Player,
            Card = action.Card,
            Message = play.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

internal sealed class NullifyStackEffect : IEffect
{
    public string TemplateId => "nullify-stack";
    public string DisplayName => "Nullify stack (Amanda / Q2)";

    public bool Matches(Card card)
    {
        string n = (card.Name ?? "").Trim();
        return n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Q2", StringComparison.OrdinalIgnoreCase);
    }

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Card == null) return (false, "No card.");
        if (!state.StackOpen || state.StackTop == null)
            return (false, "No open stack to respond to.");
        return TimingRules.CanRespond(action.Card, state.StackTop, action.Player);
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        var ir = InterruptRules.Resolve(action.Card!);
        var result = new ApplyResult
        {
            Ok = true,
            Message = ir.Message,
            TemplateId = TemplateId,
            Interrupt = ir
        };
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Nullified,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target ?? state.StackTop?.Card,
            Message = ir.Message,
            TemplateId = TemplateId
        });
        return result;
    }
}

internal sealed class HiddenAgendaEffect : IEffect
{
    public string TemplateId => "hidden-agenda";
    public string DisplayName => "Hidden Agenda";

    public bool Matches(Card card) => CardIcons.HasHiddenAgenda(card);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Kind == GameActionKind.FlipHiddenAgenda)
            return (action.Card != null, "Flip Hidden Agenda face-up.");
        if (state.SeedPhase)
            return (true, "May seed face-down.");
        if (action.Card != null && !state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");
        return (true, "Play face-down on core.");
    }

    public ApplyResult Apply(GameState state, GameAction action)
    {
        string msg = action.Kind == GameActionKind.FlipHiddenAgenda
            ? $"Flip {action.Card?.Name} face-up."
            : $"Play {action.Card?.Name} face-down (Hidden Agenda).";
        return ApplyResult.OkResult(msg, TemplateId);
    }
}

internal sealed class CorePermanentEffect : IEffect
{
    public string TemplateId => "core-permanent";
    public string DisplayName => "Core permanent (Incident / Objective)";

    public bool Matches(Card card) =>
        CardKinds.IsIncident(card) || CardKinds.IsObjective(card);

    public (bool ok, string reason) CanPlay(GameState state, GameAction action)
    {
        if (action.Card != null && CardIcons.HasHiddenAgenda(action.Card))
            return EffectRegistry.ById("hidden-agenda")?.CanPlay(state, action)
                   ?? (true, "Hidden Agenda.");
        if (state.SeedPhase)
            return (true, "May seed.");
        if (action.Card != null && !state.NormalCardPlayAvailable && !PlayRules.PlaysForFree(action.Card))
            return (false, "Normal card play not available.");
        return (true, "Play on core / TABLE.");
    }

    public ApplyResult Apply(GameState state, GameAction action) =>
        ApplyResult.OkResult(
            $"Play {action.Card?.Name} on core ({CardKinds.Of(action.Card)}).",
            TemplateId);
}