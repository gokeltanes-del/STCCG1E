using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Validate + Apply entry. TableWindow / later net host / later AI all call here.
/// Does not own Borders or pixels. Catalogs (*Rules) stay the parameter source.
/// </summary>
public static class EngineAuthority
{
    public static ApplyResult Evaluate(GameState state, GameAction action)
    {
        switch (action.Kind)
        {
            case GameActionKind.Pass:
                return ApplyResult.OkResult($"P{action.Player} passes the response window.", "timing");

            case GameActionKind.EndPhase:
                if (state.SeedPhase)
                    return ApplyResult.OkResult(
                        action.Note ?? $"End {SeedRules.PhaseLabel(state.CurrentSeedPhase)} phase.",
                        "seed");
                if (state.Segment != GameSession.TurnSegment.Play)
                    return ApplyResult.Deny("Not in Play segment.", "turn", action.Player);
                return ApplyResult.OkResult("Play → Execute.", "turn");

            case GameActionKind.EndTurn:
                if (state.SeedPhase)
                {
                    if (state.CurrentSeedPhase != SeedSubPhase.Facility
                        && state.CurrentSeedPhase != SeedSubPhase.Done)
                        return ApplyResult.Deny(
                            "Finish remaining seed phases first.",
                            "seed", action.Player);
                    return ApplyResult.OkResult(
                        action.Note ?? "Finish seed.",
                        "seed");
                }
                if (state.Segment == GameSession.TurnSegment.Play)
                    return ApplyResult.Deny("End Play phase first.", "turn", action.Player);
                return ApplyResult.OkResult("End turn.", "turn");

            case GameActionKind.SeedCard:
                return EvaluateSeed(state, action);

            case GameActionKind.EncounterDilemma:
                return EvaluateEncounter(state, action);

            case GameActionKind.AttemptMission:
                return EvaluateAttempt(state, action);

            case GameActionKind.PlayCard:
            case GameActionKind.Respond:
            case GameActionKind.ActivateInPlay:
                return EvaluatePlay(state, action);

            case GameActionKind.Download:
                return EvaluateDownload(state, action);

            case GameActionKind.FlipHiddenAgenda:
                return EvaluateFlipHiddenAgenda(state, action);

            case GameActionKind.Fly:
                return EvaluateFly(state, action);

            case GameActionKind.Beam:
                return EvaluateBeam(state, action);

            default:
                return ApplyResult.OkResult(action.Label + " (generic).", "generic");
        }
    }

    private static ApplyResult EvaluateSeed(GameState state, GameAction action)
    {
        if (!state.SeedPhase)
            return ApplyResult.Deny("Not in seed phase.", "seed", action.Player, action.Card);
        if (action.Player != state.ActivePlayer)
            return ApplyResult.Deny("Only the active seeder may place now.", "seed", action.Player, action.Card);
        if (action.Card == null)
            return ApplyResult.Deny("No seed card.", "seed", action.Player);

        var gate = SeedRules.CanSeedNow(
            action.Card,
            state.CurrentSeedPhase,
            action.Target,
            state.CryoPersonnelSeeded(action.Player));
        if (!gate.ok)
            return ApplyResult.Deny(gate.reason, "seed", action.Player, action.Card);

        string where = action.Target != null ? $" under {action.Target.Name}" : "";
        string msg = $"Seed {action.Card.Name}{where} ({SeedRules.PhaseLabel(state.CurrentSeedPhase)}).";
        var result = ApplyResult.OkResult(msg, "seed");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = msg,
            TemplateId = "seed"
        });
        return result;
    }

    private static ApplyResult EvaluateDownload(GameState state, GameAction action)
    {
        if (state.SeedPhase)
            return ApplyResult.Deny("Not during seed.", "download", action.Player);
        if (action.Player != state.ActivePlayer)
            return ApplyResult.Deny(
                "Only the active player may download now.",
                "download", action.Player);

        string note = action.Note ?? "";
        bool tent = note.Contains("Tent", StringComparison.OrdinalIgnoreCase)
                    || (action.Card != null && (action.Card.Name ?? "").Contains("Tent", StringComparison.OrdinalIgnoreCase));
        bool special = note.Contains("Special", StringComparison.OrdinalIgnoreCase)
                       || (action.Card != null && CardIcons.HasSpecialDownload(action.Card));

        if (tent)
        {
            if (!state.TentOpen(action.Player))
                return ApplyResult.Deny(
                    "Q's Tent is closed (seed the doorway as a cover first).",
                    "download", action.Player, action.Card);
            if (state.TentCount(action.Player) <= 0)
                return ApplyResult.Deny("Q's Tent is empty.", "download", action.Player, action.Card);
            if (state.TentDownloadUsed(action.Player))
                return ApplyResult.Deny(
                    "Q's Tent download already used this turn.",
                    "download", action.Player, action.Card);
        }
        else if (special && action.Card != null)
        {
            if (!CardIcons.HasSpecialDownload(action.Card))
                return ApplyResult.Deny(
                    "No Special Download icon / text on that card.",
                    "download", action.Player, action.Card);
            if (state.SpecialDownloadUsed(action.Player, action.Card))
                return ApplyResult.Deny(
                    "Special Download already used on that card.",
                    "download", action.Player, action.Card);
        }

        string msg = tent
            ? "Q's Tent download authorized."
            : special
                ? $"Special Download authorized ({action.Card?.Name})."
                : "Download authorized.";
        var result = ApplyResult.OkResult(msg, tent ? "download-tent" : "download");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = msg,
            TemplateId = result.TemplateId
        });
        return result;
    }

    private static ApplyResult EvaluateFlipHiddenAgenda(GameState state, GameAction action)
    {
        if (action.Card == null)
            return ApplyResult.Deny("No card.", "hidden-agenda", action.Player);
        if (!CardIcons.HasHiddenAgenda(action.Card))
            return ApplyResult.Deny(
                "Not a Hidden Agenda card.",
                "hidden-agenda", action.Player, action.Card);

        var piece = state.Board.FirstOrDefault(p =>
            ReferenceEquals(p.Card, action.Card)
            || (p.InstanceId != 0 && p.InstanceId == action.Card.InstanceId)
            || string.Equals(p.Card.Name, action.Card.Name, StringComparison.OrdinalIgnoreCase));
        bool faceUp = piece?.FaceUp ?? action.Card.FaceUp;
        if (faceUp)
            return ApplyResult.Deny(
                "Already face-up.",
                "hidden-agenda", action.Player, action.Card);

        string msg = $"Flip {action.Card.Name} face-up authorized.";
        var result = ApplyResult.OkResult(msg, "hidden-agenda");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = action.Card,
            Message = msg,
            TemplateId = "hidden-agenda"
        });
        return result;
    }

    private static ApplyResult EvaluateAttempt(GameState state, GameAction action)
    {
        if (state.SeedPhase)
            return ApplyResult.Deny("Not during seed.", "attempt", action.Player);
        if (state.Segment != GameSession.TurnSegment.Execute)
            return ApplyResult.Deny(
                "Missions may only be attempted during the Execute segment.",
                "attempt", action.Player);
        if (action.Player != state.ActivePlayer)
            return ApplyResult.Deny(
                "Only the active player may attempt a mission.",
                "attempt", action.Player);

        var mission = action.Target ?? action.Card;
        if (mission == null || !CardKinds.IsMission(mission))
            return ApplyResult.Deny("No mission target.", "attempt", action.Player);

        var piece = state.Board.FirstOrDefault(p =>
            p.Kind == BoardPieceKind.Mission
            && (ReferenceEquals(p.Card, mission)
                || string.Equals(p.Card.Name, mission.Name, StringComparison.OrdinalIgnoreCase)));
        if (piece != null)
        {
            if (piece.MissionSolved)
                return ApplyResult.Deny("This mission is already solved.", "attempt", action.Player, mission);
            if (piece.AttemptBlocked)
                return ApplyResult.Deny(
                    piece.AttemptBlockReason ?? "This mission cannot be attempted.",
                    "attempt", action.Player, mission);
        }

        var result = ApplyResult.OkResult($"Attempt {mission.Name} authorized.", "attempt");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = mission,
            Message = result.Message,
            TemplateId = "attempt"
        });
        return result;
    }

    private static ApplyResult EvaluateFly(GameState state, GameAction action)
    {
        if (state.SeedPhase)
            return ApplyResult.Deny("Not during seed.", "fly", action.Player);
        if (state.Segment != GameSession.TurnSegment.Execute)
            return ApplyResult.Deny(
                "Ships move only in the Execute segment.",
                "fly", action.Player);
        if (action.Player != state.ActivePlayer)
            return ApplyResult.Deny("Only the active player may move ships.", "fly", action.Player);

        var ship = action.Card;
        if (ship == null || !CardKinds.IsShip(ship))
            return ApplyResult.Deny("No ship.", "fly", action.Player);

        // Prefer InstanceId over printed name so two U.S.S. Nebula copies do not share RANGE/origin.
        var piece = state.Board.FirstOrDefault(p =>
            p.Kind == BoardPieceKind.Ship
            && (ReferenceEquals(p.Card, ship)
                || (ship.InstanceId > 0 && p.InstanceId == ship.InstanceId)
                || (ship.InstanceId == 0
                    && string.Equals(p.Card.Name, ship.Name, StringComparison.OrdinalIgnoreCase))));
        if (piece != null)
        {
            if (piece.Stopped || state.IsStoppedInstance(piece.InstanceId))
                return ApplyResult.Deny("Stopped ship cannot move.", "fly", action.Player, ship);
            if (piece.Controller != action.Player && piece.Owner != action.Player)
                return ApplyResult.Deny("Only your ships may move.", "fly", action.Player, ship);
            if (piece.RangeLeft == 0)
                return ApplyResult.Deny("No RANGE remaining this turn.", "fly", action.Player, ship);
            if (!piece.Staffed)
                return ApplyResult.Deny(
                    piece.StaffReason ?? "Ship is not staffed for movement.",
                    "fly", action.Player, ship);
        }

        // Destination set → same MovementRules path as LegalMoves
        if (action.Target != null && piece != null)
        {
            var path = TryEvaluateFlyPath(state, action.Player, ship, piece, action.Target);
            if (!path.ok)
                return ApplyResult.Deny(path.reason, "fly", action.Player, ship);

            var ok = ApplyResult.OkResult(path.reason, "fly");
            ok.Events.Add(new GameEvent
            {
                Kind = GameEventKind.Info,
                Player = action.Player,
                Card = ship,
                Target = action.Target,
                Message = path.reason,
                TemplateId = "fly"
            });
            return ok;
        }

        string dest = action.Target?.Name ?? "(choose destination)";
        var result = ApplyResult.OkResult(
            $"Fly {ship.Name} → {dest} authorized.",
            "fly");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = ship,
            Target = action.Target,
            Message = result.Message,
            TemplateId = "fly"
        });
        return result;
    }

    /// <summary>
    /// Shared RANGE/spaceline check for Authority and (via Evaluate) LegalMoves.
    /// </summary>
    public static (bool ok, string reason, int cost, int rangeLeft) TryEvaluateFlyPath(
        GameState state, int player, Card ship, BoardPiece piece, Card destination)
    {
        string q = "Alpha";
        var here = BoardStore.Current.LocationOfOccupant(piece.InstanceId);
        if (!string.IsNullOrWhiteSpace(here?.Quadrant)) q = here!.Quadrant!;
        var line = BoardStore.Current.Spaceline.Locations
            .Where(l => l.Kind is LocationKind.Mission or LocationKind.Span or LocationKind.TimeLocation
                        && string.Equals(l.Quadrant ?? "Alpha", q, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int fromIdx = -1;
        if (here != null)
        {
            for (int i = 0; i < line.Count; i++)
                if (ReferenceEquals(line[i], here)) { fromIdx = i; break; }
        }
        if (fromIdx < 0 && !string.IsNullOrEmpty(piece.HostName))
        {
            for (int i = 0; i < line.Count; i++)
            {
                if (string.Equals(line[i].Printed?.Name, piece.HostName, StringComparison.OrdinalIgnoreCase))
                { fromIdx = i; break; }
            }
        }

        int toIdx = -1;
        for (int i = 0; i < line.Count; i++)
        {
            var p = line[i].Printed;
            if (p == null) continue;
            if (ReferenceEquals(p, destination)
                || (destination.InstanceId > 0 && p.InstanceId == destination.InstanceId)
                || string.Equals(p.Name, destination.Name, StringComparison.OrdinalIgnoreCase))
            { toIdx = i; break; }
        }

        string hops = FormatFlyHops(line, fromIdx, toIdx);
        DebugLog.Move(state.TurnNumber, player,
            $"fly-eval {DebugLog.Card(ship)} host={piece.HostName ?? "?"} " +
            $"from[{fromIdx}]={(fromIdx >= 0 && fromIdx < line.Count ? line[fromIdx].Label : "?")} " +
            $"to[{toIdx}]={(toIdx >= 0 && toIdx < line.Count ? line[toIdx].Label : destination.Name ?? "?")} " +
            $"line={line.Count} remain={piece.RangeLeft} {hops}");

        if (line.Count == 0)
            return (false, "No spaceline locations on the board.", 0, piece.RangeLeft);
        if (fromIdx < 0)
            return (false, "Ship is not anchored on the spaceline.", 0, piece.RangeLeft);
        if (toIdx < 0)
            return (false, "Destination is not on the spaceline.", 0, piece.RangeLeft);
        if (toIdx == fromIdx)
            return (false, "Ship is already at this location.", 0, piece.RangeLeft);

        int remain = piece.RangeLeft >= 0
            ? piece.RangeLeft
            : MovementRules.GetShipRange(ship);
        var treaties = state.TreatiesOf(player);
        bool wrap = state.HasWnohgb(player) && line.Count >= 2;
        var move = MovementRules.CanMoveShip(
            ship, piece.Aboard, remain, line, fromIdx, toIdx, treaties,
            wrapEnds: wrap, skipStaffing: piece.Staffed);

        if (!move.Ok)
            return (false, move.Reason + " · " + hops, move.RangeCost, remain);

        return (true,
            $"Fly {ship.Name} → {destination.Name}: cost {move.RangeCost}, left {move.RangeLeft}.",
            move.RangeCost, move.RangeLeft);
    }

    private static string FormatFlyHops(IReadOnlyList<Location> line, int fromIdx, int toIdx)
    {
        if (fromIdx < 0 || toIdx < 0 || fromIdx >= line.Count || toIdx >= line.Count || fromIdx == toIdx)
            return "hops=-";
        int step = toIdx > fromIdx ? 1 : -1;
        var parts = new List<string>();
        int i = fromIdx;
        int guard = 0;
        int total = 0;
        do
        {
            i += step;
            if (i < 0 || i >= line.Count) break;
            total += Math.Max(0, line[i].Span);
            parts.Add($"{line[i].Label}={line[i].Span}");
            if (++guard > line.Count + 1) break;
        } while (i != toIdx);
        return $"hops[{total}]: " + string.Join(" + ", parts);
    }

    private static ApplyResult EvaluateBeam(GameState state, GameAction action)
    {
        if (state.SeedPhase)
            return ApplyResult.Deny("Not during seed.", "beam", action.Player);
        if (state.Segment != GameSession.TurnSegment.Execute)
            return ApplyResult.Deny(
                "Beaming only in the Execute segment.",
                "beam", action.Player);
        if (action.Player != state.ActivePlayer)
            return ApplyResult.Deny("Only the active player may beam.", "beam", action.Player);

        var result = ApplyResult.OkResult(
            action.Target != null
                ? $"Beam authorized → {action.Target.Name}."
                : "Beam authorized (choose destination).",
            "beam");
        result.Events.Add(new GameEvent
        {
            Kind = GameEventKind.Info,
            Player = action.Player,
            Card = action.Card,
            Target = action.Target,
            Message = result.Message,
            TemplateId = "beam"
        });
        return result;
    }

    private static ApplyResult EvaluateEncounter(GameState state, GameAction action)
    {
        if (action.Card == null)
            return ApplyResult.Deny("No dilemma.", "dilemma", action.Player);
        if (state.SeedPhase)
            return ApplyResult.Deny("Not during seed.", "dilemma", action.Player);

        var fx = EffectRegistry.Find(action.Card);
        if (fx != null)
        {
            var gate = fx.CanPlay(state, action);
            if (!gate.ok && !IsIncompleteTargetReason(gate.reason))
                return ApplyResult.Deny(gate.reason, fx.TemplateId, action.Player, action.Card);
            return fx.Apply(state, action);
        }

        string tid = CardEffectMap.TemplateIdFor(action.Card) ?? "dilemma";
        return ApplyResult.OkResult(
            $"Encounter {action.Card.Name} (DilemmaRules.Resolve).",
            tid);
    }

    /// <summary>
    /// Event / Interrupt / mapped template path. Always attaches catalog payload when possible
    /// so the UI applies one authority result instead of re-resolving names.
    /// </summary>
    private static ApplyResult EvaluatePlay(GameState state, GameAction action)
    {
        var card = action.Card;
        if (card == null)
            return ApplyResult.Deny("No card.", "generic", action.Player);

        // Goddess: interrupts blocked except catalog exceptions (Kevin / Q2 / …).
        if (InterruptRules.IsInterrupt(card)
            && state.HasGoddess
            && !EventRules.IsGoddessException(card))
        {
            return ApplyResult.Deny(
                "Goddess of Empathy: interrupts may not be played.",
                "goddess", action.Player, card);
        }

        var fx = EffectRegistry.Find(card);
        if (fx != null)
        {
            var gate = fx.CanPlay(state, action);
            if (!gate.ok)
            {
                // UI often picks the host after authority — soft-allow missing-target denials
                // so catalog payload is still delivered; hard rules still deny.
                if (IsIncompleteTargetReason(gate.reason))
                    return CatalogPayload(state, action, fx.TemplateId, softMessage: gate.reason);
                return ApplyResult.Deny(gate.reason, fx.TemplateId, action.Player, card);
            }
            return fx.Apply(state, action);
        }

        return CatalogPayload(state, action, templateId: null, softMessage: null);
    }

    private static bool IsIncompleteTargetReason(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return false;
        return reason.Contains("choose", StringComparison.OrdinalIgnoreCase)
               || reason.Contains("target", StringComparison.OrdinalIgnoreCase)
               || reason.Contains("play on", StringComparison.OrdinalIgnoreCase)
               || reason.Contains("drop onto", StringComparison.OrdinalIgnoreCase)
               || reason.Contains("no ship", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Resolve *Rules catalogs into ApplyResult without requiring a template match.</summary>
    private static ApplyResult CatalogPayload(
        GameState state, GameAction action, string? templateId, string? softMessage)
    {
        var card = action.Card!;
        if (EventRules.IsEvent(card))
        {
            var play = EventRules.ResolvePlay(card);
            string tid = templateId
                ?? CardEffectMap.TemplateIdFor(card)
                ?? "event-catalog";
            var result = new ApplyResult
            {
                Ok = true,
                Message = softMessage != null
                    ? $"{play.Message} (host pending: {softMessage})"
                    : play.Message,
                TemplateId = tid,
                EventPlay = play
            };
            result.Events.Add(new GameEvent
            {
                Kind = GameEventKind.CardPlayed,
                Player = action.Player,
                Card = card,
                Target = action.Target,
                Message = result.Message,
                TemplateId = tid
            });
            return result;
        }

        if (InterruptRules.IsInterrupt(card) || TimingRules.IsInterrupt(card))
        {
            var ir = InterruptRules.Resolve(card);
            string tid = templateId
                ?? CardEffectMap.TemplateIdFor(card)
                ?? "interrupt-catalog";
            var result = new ApplyResult
            {
                Ok = true,
                Message = softMessage != null
                    ? $"{ir.Message} (host pending: {softMessage})"
                    : ir.Message,
                TemplateId = tid,
                Interrupt = ir
            };
            result.Events.Add(new GameEvent
            {
                Kind = GameEventKind.CardPlayed,
                Player = action.Player,
                Card = card,
                Target = action.Target,
                Message = result.Message,
                TemplateId = tid
            });
            return result;
        }

        if (ArtifactRules.IsArtifact(card))
        {
            var acq = ArtifactRules.ResolveAcquire(card);
            string tid = templateId
                ?? CardEffectMap.TemplateIdFor(card)
                ?? "artifact-acquire";
            var result = new ApplyResult
            {
                Ok = true,
                Message = acq.Message,
                TemplateId = tid,
                Artifact = acq
            };
            result.Events.Add(new GameEvent
            {
                Kind = GameEventKind.CardPlayed,
                Player = action.Player,
                Card = card,
                Message = acq.Message,
                TemplateId = tid
            });
            return result;
        }

        return ApplyResult.OkResult(
            action.Label + " (no template — UI path).",
            templateId ?? "generic");
    }

    public static string FormatResult(ApplyResult result)
    {
        var sb = new StringBuilder();
        sb.Append(result.Ok ? "OK" : "DENY");
        if (result.TemplateId != null)
            sb.Append(" {").Append(result.TemplateId).Append('}');
        sb.Append(": ").Append(result.Message);
        return sb.ToString();
    }

    public static IEnumerable<string> FormatEvents(ApplyResult result)
    {
        foreach (var e in result.Events)
            yield return "Event: " + e.Format();
    }

    /// <summary>
    /// After DilemmaRules.Resolve in the UI, stamp the mapped template id
    /// (wall / kill / space / attach / special) for LegalMoves + Action History.
    /// </summary>
    public static ApplyResult WrapDilemma(Card dilemma, DilemmaRules.Result catalog)
    {
        string templateId = CardEffectMap.TemplateIdFor(dilemma)
            ?? EffectRegistry.Find(dilemma)?.TemplateId
            ?? "dilemma";
        return DilemmaTemplate.FromCatalog(templateId, dilemma, catalog);
    }
}
