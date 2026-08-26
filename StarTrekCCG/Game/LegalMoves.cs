using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Enumerates legal actions for one side. Same function later feeds hotseat, net, and AI.
/// Standard moves + registry CanPlay + responses on the open window.
/// </summary>
public static class LegalMoves
{
    public static List<GameAction> Collect(GameState state, int player)
    {
        var list = new List<GameAction>();
        if (state.Match == GameSession.MatchPhase.Ended)
            return list;

        if (state.StackOpen)
        {
            list.Add(GameAction.Pass(state.ResponsePlayer));
            var hand = state.HandOf(state.ResponsePlayer);
            if (state.StackTop != null)
            {
                foreach (var c in TimingRules.LegalResponsesInHand(hand, state.StackTop, state.ResponsePlayer))
                    list.Add(GameAction.Respond(state.ResponsePlayer, c));
            }
            return list;
        }

        if (state.SeedPhase)
        {
            CollectSeed(state, player, list);
            return list;
        }

        if (state.Match != GameSession.MatchPhase.Play)
            return list;

        if (player != state.ActivePlayer)
            return list;

        if (state.Segment == GameSession.TurnSegment.Play)
            list.Add(GameAction.EndPhase(player));
        else
            list.Add(GameAction.EndTurn(player));

        foreach (var card in state.HandOf(player))
            AddHandPlays(state, player, card, list);

        AddActivations(state, player, list);
        AddExecuteOrders(state, player, list);
        return list;
    }

    public static IEnumerable<string> FormatLines(GameState state, int player)
    {
        var moves = Collect(state, player);
        yield return $"LegalMoves P{player} · T{state.TurnNumber} {state.Segment} · {moves.Count} action(s)";
        if (moves.Count == 0)
        {
            yield return "  (none)";
            yield break;
        }
        int i = 1;
        foreach (var a in moves)
        {
            string extra = "";
            var fx = EffectRegistry.Find(a.Card);
            if (fx != null) extra = "  {" + fx.TemplateId + "}";
            yield return $"  {i++,2}. {a.Label}{extra}";
        }
    }

    private static void CollectSeed(GameState state, int player, List<GameAction> list)
    {
        if (player != state.ActivePlayer)
            return;

        var phase = state.CurrentSeedPhase;
        if (phase == SeedSubPhase.Done)
            return;

        list.Add(new GameAction
        {
            Kind = GameActionKind.EndPhase,
            Player = player,
            Note = phase == SeedSubPhase.Facility
                ? "Finish seed phase (or keep placing)"
                : $"End {SeedRules.PhaseLabel(phase)} phase"
        });

        if (phase == SeedSubPhase.Facility)
        {
            list.Add(new GameAction
            {
                Kind = GameActionKind.EndTurn,
                Player = player,
                Note = "Finish seed → opening hand"
            });
        }

        var pile = state.SeedPileOf(player);
        var missions = state.Missions().Select(m => m.Card).ToList();
        int cryo = state.CryoPersonnelSeeded(player);

        foreach (var card in pile)
        {
            if (!SeedRules.BelongsInPhase(card, phase))
                continue;

            if (phase is SeedSubPhase.Doorway or SeedSubPhase.Mission)
            {
                var probe = GameAction.Seed(player, card, note: SeedRules.PhaseLabel(phase));
                if (EngineAuthority.Evaluate(state, probe).Ok)
                    list.Add(probe);
                continue;
            }

            if (phase == SeedSubPhase.Dilemma)
            {
                bool any = false;
                foreach (var mission in missions)
                {
                    var probe = GameAction.Seed(player, card, mission, "under mission");
                    if (!EngineAuthority.Evaluate(state, probe).Ok) continue;
                    list.Add(probe);
                    any = true;
                }
                if (!any && SeedRules.IsAuPersonnelForCryosatellite(card) && cryo >= SeedRules.CryosatellitePersonnelMax)
                {
                    list.Add(GameAction.Seed(player, card, note: "Cryo quota full"));
                }
                continue;
            }

            // Facility phase: facilities/ships need a mission; events may go TABLE.
            if (CardKinds.IsFacility(card) || CardKinds.IsShip(card) || CardKinds.IsPersonnel(card))
            {
                foreach (var mission in missions)
                {
                    var probe = GameAction.Seed(player, card, mission, "facility phase");
                    if (EngineAuthority.Evaluate(state, probe).Ok)
                        list.Add(probe);
                }
            }
            else
            {
                var probe = GameAction.Seed(player, card, note: "TABLE / facility phase");
                if (EngineAuthority.Evaluate(state, probe).Ok)
                    list.Add(probe);
            }
        }
    }

    private static void AddHandPlays(GameState state, int player, Card card, List<GameAction> list)
    {
        bool anytime = TimingRules.IsAnytimeType(card);
        bool free = PlayRules.PlaysForFree(card);
        bool playOk = anytime || free || state.NormalCardPlayAvailable;
        if (state.Segment != GameSession.TurnSegment.Play && !anytime)
            return;
        if (!playOk) return;

        if (state.HasGoddess && TimingRules.IsInterrupt(card) && !EventRules.IsGoddessException(card))
            return;

        var fx = EffectRegistry.Find(card);
        if (fx != null)
        {
            AddTemplatedPlays(state, player, card, fx, list);
            return;
        }

        // Generic (no template yet): one play action; UI still does targeting.
        list.Add(GameAction.Play(player, card));
    }

    private static void AddTemplatedPlays(GameState state, int player, Card card, IEffect fx, List<GameAction> list)
    {
        if (fx.TemplateId == "nullify-inplay")
        {
            bool any = false;
            foreach (var ev in state.EventsInPlay())
            {
                var probe = GameAction.Play(player, card, ev.Card);
                if (fx.CanPlay(state, probe).ok)
                {
                    list.Add(probe);
                    any = true;
                }
            }
            if (!any)
                list.Add(GameAction.Play(player, card)); // still show; needs a drop target
            return;
        }

        if (fx.TemplateId is "attach-eot" or "countdown-nextturn")
        {
            foreach (var ship in state.Ships())
            {
                var probe = GameAction.Play(player, card, ship.Card);
                if (fx.CanPlay(state, probe).ok)
                    list.Add(probe);
            }
            return;
        }

        var plain = GameAction.Play(player, card);
        if (fx.CanPlay(state, plain).ok)
            list.Add(plain);
    }

    private static void AddActivations(GameState state, int player, List<GameAction> list)
    {
        foreach (var piece in state.Board)
        {
            if (piece.FaceUp) continue;
            if (piece.Owner != player && piece.Controller != player) continue;
            if (!CardIcons.HasHiddenAgenda(piece.Card)) continue;
            list.Add(new GameAction
            {
                Kind = GameActionKind.FlipHiddenAgenda,
                Player = player,
                Card = piece.Card
            });
        }

        foreach (var door in state.HandOf(player))
        {
            if (!CardKinds.IsDoorway(door)) continue;
            string n = (door.Name ?? "").ToLowerInvariant();
            if (!n.Contains("q's tent") && !n.Contains("q’s tent") && !n.Contains("qs tent"))
                continue;
            if (state.TentOpen(player) && state.TentCount(player) > 0)
            {
                list.Add(new GameAction
                {
                    Kind = GameActionKind.Download,
                    Player = player,
                    Card = door,
                    Note = "Q's Tent"
                });
            }
        }

        foreach (var piece in state.Board)
        {
            if (piece.Owner != player && piece.Controller != player) continue;
            if (!CardIcons.HasSpecialDownload(piece.Card)) continue;
            list.Add(new GameAction
            {
                Kind = GameActionKind.Download,
                Player = player,
                Card = piece.Card,
                Note = "Special Download"
            });
        }

        var plasmaFx = EffectRegistry.ById("attach-eot");
        if (plasmaFx == null) return;

        foreach (var ship in state.Ships())
        {
            bool fire = state.Board.Any(p =>
                p.Persist == EventRules.Persist.PlasmaFire
                && string.Equals(p.HostName, ship.Card.Name, System.StringComparison.OrdinalIgnoreCase));
            if (!fire) continue;

            var dummy = new Card { Name = "Plasma Fire", Type = "Event" };
            var act = GameAction.Activate(player, dummy, ship.Card, "Nullify with SECURITY");
            if (plasmaFx.CanPlay(state, act).ok)
                list.Add(act);
        }
    }

    private static void AddExecuteOrders(GameState state, int player, List<GameAction> list)
    {
        if (state.Segment != GameSession.TurnSegment.Execute)
            return;

        foreach (var m in state.Missions())
        {
            if (m.MissionSolved || m.AttemptBlocked) continue;
            var probe = GameAction.AttemptMission(player, m.Card);
            if (EngineAuthority.Evaluate(state, probe).Ok)
                list.Add(probe);
        }

        var ordered = state.OrderedMissions();

        foreach (var ship in state.Ships().Where(s =>
                     (s.Owner == player || s.Controller == player)
                     && !s.Stopped
                     && s.Staffed
                     && s.RangeLeft != 0))
        {
            bool anyDest = false;
            foreach (var dest in ordered)
            {
                if (!string.IsNullOrEmpty(ship.HostName)
                    && string.Equals(ship.HostName, dest.Name, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var path = EngineAuthority.TryEvaluateFlyPath(
                    state, player, ship.Card, ship, dest);
                if (!path.ok) continue;

                list.Add(new GameAction
                {
                    Kind = GameActionKind.Fly,
                    Player = player,
                    Card = ship.Card,
                    Target = dest,
                    Note = $"cost {path.cost}, left {path.rangeLeft}"
                });
                anyDest = true;
            }

            if (!anyDest)
            {
                int remain = ship.RangeLeft >= 0
                    ? ship.RangeLeft
                    : MovementRules.GetShipRange(ship.Card);
                list.Add(new GameAction
                {
                    Kind = GameActionKind.Fly,
                    Player = player,
                    Card = ship.Card,
                    Note = string.IsNullOrEmpty(ship.HostName)
                        ? "no spaceline anchor"
                        : $"RANGE {remain} (no reachable dest)"
                });
            }

            if (ship.Occupied)
                list.Add(GameAction.Beam(player, ship.Card, note: "UI picks destination"));
        }
    }
}