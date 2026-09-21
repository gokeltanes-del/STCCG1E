using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Enumerates legal actions for one side. Same function later feeds hotseat, net, and AI.
/// Always call <see cref="CollectBoth"/> on a window change: the opponent may have
/// responses (or later instants) while it is not their turn.
/// </summary>
public static class LegalMoves
{
    /// <summary>
    /// Actions <paramref name="player"/> may take in this exact window.
    /// Opponent gets responses when they are ResponsePlayer; otherwise an empty
    /// off-turn list (later: "plays at any time" instants go here).
    /// </summary>
    public static List<GameAction> Collect(GameState state, int player)
    {
        var list = new List<GameAction>();
        if (state.Match == GameSession.MatchPhase.Ended)
            return list;

        if (state.StackOpen)
        {
            if (player != state.ResponsePlayer)
                return list;
            list.Add(GameAction.Pass(player));
            var hand = state.HandOf(player);
            if (state.StackTop != null)
            {
                // Glossary: Goddess of Empathy — interrupts blocked in response/nullify window too (Amanda not excepted).
                foreach (var c in TimingRules.LegalResponsesInHand(hand, state.StackTop, player))
                {
                    if (state.HasGoddess && TimingRules.IsInterrupt(c) && !EventRules.IsGoddessException(c))
                        continue;
                    list.Add(GameAction.Respond(player, c));
                }
            }
            return list;
        }

        CollectOffTurn(state, player, list);

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

    /// <summary>Both seats — what net / AI must ask every window.</summary>
    public static (List<GameAction> P1, List<GameAction> P2) CollectBoth(GameState state) =>
        (Collect(state, 1), Collect(state, 2));

    public static IEnumerable<string> FormatLines(GameState state, int player)
    {
        var (p1, p2) = CollectBoth(state);
        yield return $"LegalMoves T{state.TurnNumber} {state.Segment}"
                     + (state.StackOpen ? $" · stack → P{state.ResponsePlayer}" : $" · active P{state.ActivePlayer}");
        foreach (var line in FormatSide(1, p1, state.ActivePlayer, state.StackOpen ? state.ResponsePlayer : 0))
            yield return line;
        foreach (var line in FormatSide(2, p2, state.ActivePlayer, state.StackOpen ? state.ResponsePlayer : 0))
            yield return line;
        _ = player;
    }

    private static IEnumerable<string> FormatSide(int player, List<GameAction> moves, int active, int responder)
    {
        string tag = responder == player ? "respond"
                   : active == player ? "turn"
                   : "waiting";
        yield return $"  P{player} [{tag}] · {moves.Count} action(s)";
        if (moves.Count == 0)
        {
            yield return "    (none)";
            yield break;
        }
        int i = 1;
        foreach (var a in moves)
        {
            string extra = "";
            var fx = EffectRegistry.Find(a.Card);
            if (fx != null) extra = "  {" + fx.TemplateId + "}";
            yield return $"    {i++,2}. {a.Label}{extra}";
        }
    }

    /// <summary>
    /// Cards that do not need the turn (stack already handled above).
    /// Hook for future "plays at any time" / just-after interrupts.
    /// </summary>
    private static void CollectOffTurn(GameState state, int player, List<GameAction> list)
    {
        if (state.SeedPhase) return;
        if (state.Match != GameSession.MatchPhase.Play) return;
        if (player == state.ActivePlayer) return;
        foreach (var card in state.HandOf(player))
        {
            if (!TimingRules.IsAnytimeType(card)) continue;
            AddHandPlays(state, player, card, list);
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
        // Verb: StartOfTurnWindow — before anytime/normal-play gates (no stack entry when closed).
        var sot = TimingRules.CanPlayStartOfTurnCard(state, card, player);
        if (!sot.ok) return;

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

        var attachEot = EffectRegistry.ById("attach-eot");
        if (attachEot == null) return;

        foreach (var ship in state.Ships())
        {
            bool fire = state.Board.Any(p =>
                p.Persist == EventRules.Persist.PlasmaFire
                && string.Equals(p.HostName, ship.Card.Name, System.StringComparison.OrdinalIgnoreCase));
            if (fire)
            {
                var dummy = new Card { Name = "Plasma Fire", Type = "Event" };
                var act = GameAction.Activate(player, dummy, ship.Card, "Nullify with SECURITY");
                if (attachEot.CanPlay(state, act).ok)
                    list.Add(act);
            }

            bool breach = state.Board.Any(p =>
                p.Persist == EventRules.Persist.WarpCore
                && string.Equals(p.HostName, ship.Card.Name, System.StringComparison.OrdinalIgnoreCase));
            if (breach)
            {
                var dummy = new Card { Name = "Warp Core Breach", Type = "Event" };
                var act = GameAction.Activate(player, dummy, ship.Card, "Nullify with ENGINEER");
                if (attachEot.CanPlay(state, act).ok)
                    list.Add(act);
            }
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

        // E5: Fly destinations from BoardStore Locations (same line as CanMoveShip / TryEvaluateFlyPath).
        // OrderedMissions() only when the store line is empty.
        foreach (var ship in state.Ships().Where(s =>
                     (s.Owner == player || s.Controller == player)
                     && !s.Stopped
                     && s.Staffed
                     && s.RangeLeft != 0))
        {
            var line = EngineAuthority.FlyLineForPiece(ship);
            List<Card> destinations;
            if (line.Count > 0)
            {
                destinations = new List<Card>();
                foreach (var loc in line)
                {
                    if (loc.Printed == null) continue;
                    if (!destinations.Contains(loc.Printed))
                        destinations.Add(loc.Printed);
                }
            }
            else
            {
                destinations = state.OrderedMissions();
            }

            bool anyDest = false;
            foreach (var dest in destinations)
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

            if (!ship.Cloaked
                && !ship.QuarantineLeaveBlocked
                && (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard)))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship — UI picks destination"));
        }

        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (fac.QuarantineLeaveBlocked) continue;
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility — UI picks destination"));
        }

        foreach (var m in state.Missions())
        {
            if (m.QuarantineLeaveBlocked) continue;
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission — UI picks destination"));
        }
    }
}