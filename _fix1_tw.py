from pathlib import Path

path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

# 1) Add control state after _attachedDilemmas
old = '''    private readonly List<AttachedDilemma> _attachedDilemmas = new();

    /// <summary>During a mission attempt: cards discarded from this location (for Temporal Causality Loop).</summary>'''

new = '''    private readonly List<AttachedDilemma> _attachedDilemmas = new();

    /// <summary>Alien Parasites Neg: temporary control until Opp EOT / start of victim next turn.</summary>
    private sealed class AlienParasiteControlState
    {
        public int Victim { get; init; }
        public int Controller { get; init; }
        public Card Dilemma { get; init; } = null!;
        public Border? ShipBorder { get; init; }
        public int ShipOriginalOwner { get; init; }
        public List<(Card Card, int OriginalController, Border? Border)> Cards { get; } = new();
    }

    private readonly List<AlienParasiteControlState> _alienParasiteControls = new();

    /// <summary>During a mission attempt: cards discarded from this location (for Temporal Causality Loop).</summary>'''

if old not in text:
    raise SystemExit("attached dilemmas anchor not found")
text = text.replace(old, new, 1)

# 2) Hook fail path after StopMissionAttemptTeam
old_fail = '''            if (failed)
            {
                if (dilResult.BeamBackTeam)
                    BeamBackAwayTeamToShipOrOutpost(missionBorder, teamBorders);
                if (dilResult.StopTeam)
                    StopMissionAttemptTeam(missionBorder, mission, teamBorders);
                StatusText.Text = logMsg;
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                    $"Dilemma {seedCard.Name} FAILED: {logMsg}");
                if (dilResult.EndTurn)
                    FinishExecuteAndEndTurn();
                ClearCardActionUi();
                return;
            }'''

new_fail = '''            if (failed)
            {
                if (dilResult.BeamBackTeam)
                    BeamBackAwayTeamToShipOrOutpost(missionBorder, teamBorders);
                if (dilResult.StopTeam)
                    StopMissionAttemptTeam(missionBorder, mission, teamBorders);
                if (dilResult.GrantOpponentControl)
                    BeginAlienParasitesOpponentControl(seedCard, missionBorder, teamBorders, shipBorder);
                StatusText.Text = logMsg;
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                    $"Dilemma {seedCard.Name} FAILED: {logMsg}");
                if (dilResult.EndTurn)
                    FinishExecuteAndEndTurn();
                ClearCardActionUi();
                return;
            }'''

if old_fail not in text:
    raise SystemExit("fail path not found")
text = text.replace(old_fail, new_fail, 1)

# 3) Call restore in FinishExecuteAndEndTurn after ProcessUntilEndOfTurnBag
old_eot = '''        ProcessEndOfTurnEvents(finishingPlayer);
        ProcessUntilEndOfTurnBag(finishingPlayer);
        ProcessRogueBorgEndOfTurn(finishingPlayer);'''

new_eot = '''        ProcessEndOfTurnEvents(finishingPlayer);
        ProcessUntilEndOfTurnBag(finishingPlayer);
        RestoreAlienParasitesControlsIfDue(finishingPlayer);
        ProcessRogueBorgEndOfTurn(finishingPlayer);'''

if old_eot not in text:
    raise SystemExit("EOT order not found")
text = text.replace(old_eot, new_eot, 1)

# 4) Insert methods after BeamBackAwayTeamToShipOrOutpost (before StopMissionAttemptTeam)
anchor = '''    private void StopMissionAttemptTeam(Border missionBorder, Card mission, List<Border> teamBorders)
    {
        foreach (var b in teamBorders)
            MarkStopped(b);'''

methods = r'''    /// <summary>
    /// Alien Parasites Neg min path: Opp chooses Away Team and/or one ship+crew here.
    /// Control applies now; Opp acts on their turn; restore at Opp EOT (= start of victim next turn).
    /// PARK: dual-window hotseat chooser; deep "not compatible with Opp other cards" affiliation mix.
    /// </summary>
    private void BeginAlienParasitesOpponentControl(
        Card seedCard, Border missionBorder, List<Border> teamBorders, Border? shipBorder)
    {
        int victim = _activePlayer;
        int opp = victim == 1 ? 2 : 1;

        var awayTeam = new List<(Border Border, Card Card)>();
        foreach (var b in teamBorders)
        {
            if (b.Tag is not Card pc) continue;
            if (!ModifierRules.IsPersonnelCard(pc)) continue;
            awayTeam.Add((b, pc));
        }

        var shipsHere = new List<Border>();
        foreach (var dock in GetDockablesUnderMission(missionBorder))
        {
            if (dock.Tag is not Card dc || !IsShipCard(dc)) continue;
            int own = GetBorderOwner(dock);
            if (own == 0) own = dc.OwnerPlayer;
            if (own != victim) continue;
            shipsHere.Add(dock);
        }
        // Space fail: attempting ship may not yet be under dockables scan — include it.
        if (shipBorder != null && shipBorder.Tag is Card sc && IsShipCard(sc) && !shipsHere.Contains(shipBorder))
        {
            int own = GetBorderOwner(shipBorder);
            if (own == 0) own = sc.OwnerPlayer;
            if (own == victim)
                shipsHere.Insert(0, shipBorder);
        }

        var options = new List<string>();
        if (awayTeam.Count > 0)
            options.Add("Away Team only");
        if (shipsHere.Count > 0)
            options.Add("One ship + crew");
        if (awayTeam.Count > 0 && shipsHere.Count > 0)
            options.Add("Away Team AND one ship + crew");

        if (options.Count == 0)
        {
            _session.Log.Add(_session.TurnNumber, $"P{opp}",
                "Alien Parasites: no Away Team or ship here to control.");
            return;
        }

        string pick = AskChoice(
            seedCard,
            $"P{opp}: Alien Parasites — control",
            "Opponent chooses: Away Team and/or one ship + crew here.\n"
            + "You control them until the start of their next turn.\n"
            + "Not compatible with your other cards (PARK: deep mix checks).",
            options.ToArray());

        var choice = DilemmaRules.ParseAlienParasitesControlChoice(pick);
        if (choice == DilemmaRules.AlienParasitesControlChoice.None)
        {
            _session.Log.Add(_session.TurnNumber, $"P{opp}",
                "Alien Parasites: control choice cancelled / empty.");
            return;
        }

        Border? chosenShip = null;
        if (choice.HasFlag(DilemmaRules.AlienParasitesControlChoice.OneShipAndCrew))
        {
            chosenShip = PickBorderFromList(seedCard, shipsHere, $"P{opp}: which ship + crew?");
            if (chosenShip == null && shipsHere.Count > 0)
                chosenShip = shipsHere[0];
            if (chosenShip == null)
            {
                ShowPlayError("Alien Parasites: no ship selected for control.");
                return;
            }
        }

        var state = new AlienParasiteControlState
        {
            Victim = victim,
            Controller = opp,
            Dilemma = seedCard,
            ShipBorder = chosenShip,
            ShipOriginalOwner = chosenShip != null
                ? (GetBorderOwner(chosenShip) is int o && o != 0 ? o : victim)
                : 0
        };

        if (choice.HasFlag(DilemmaRules.AlienParasitesControlChoice.AwayTeam))
        {
            foreach (var (b, pc) in awayTeam)
            {
                int orig = pc.Controller != 0 ? pc.Controller : victim;
                pc.Controller = opp;
                state.Cards.Add((pc, orig, b));
            }
        }

        if (chosenShip != null && chosenShip.Tag is Card shipCard)
        {
            int shipOrig = shipCard.Controller != 0 ? shipCard.Controller : state.ShipOriginalOwner;
            shipCard.Controller = opp;
            SetBorderOwner(chosenShip, opp);
            state.Cards.Add((shipCard, shipOrig, chosenShip));

            if (_stackOnHost.TryGetValue(chosenShip, out var crew))
            {
                foreach (var b in crew)
                {
                    if (b.Tag is not Card pc) continue;
                    if (!ModifierRules.IsPersonnelCard(pc) && !ModifierRules.IsEquipmentCard(pc))
                        continue;
                    // Avoid double-entry if already in Away Team list
                    if (state.Cards.Any(x => ReferenceEquals(x.Card, pc)))
                    {
                        pc.Controller = opp;
                        continue;
                    }
                    int orig = pc.Controller != 0 ? pc.Controller : victim;
                    pc.Controller = opp;
                    state.Cards.Add((pc, orig, b));
                }
            }
            SyncDockableSideAfterOwnerChange(chosenShip);
            UpdateHostBadge(chosenShip);
        }

        _alienParasiteControls.Add(state);

        string shipName = chosenShip?.Tag is Card sh ? (sh.Name ?? "ship") : "(none)";
        string summary =
            $"P{opp} controls ({choice}): AT×{awayTeam.Count} / ship={shipName}.\n"
            + "Acts on their turn. Restores at end of that turn (start of your next).\n"
            + "Not compatible with opponent's other cards.";
        ShowCardReveal(seedCard, "Alien Parasites — control", summary, RevealButtons.Ok, seedCard.Name);
        StatusText.Text = $"Alien Parasites: P{opp} controls selected cards until start of P{victim}'s next turn.";
        _session.Log.Add(_session.TurnNumber, $"P{opp}",
            $"Alien Parasites control granted ({choice}) ship={shipName} cards={state.Cards.Count}.");
        SyncBoardFromTable();
    }

    private void RestoreAlienParasitesControlsIfDue(int finishingPlayer)
    {
        foreach (var state in _alienParasiteControls
                     .Where(s => DilemmaRules.ShouldRestoreAlienParasitesControl(s.Controller, finishingPlayer))
                     .ToList())
        {
            foreach (var (card, orig, border) in state.Cards)
            {
                card.Controller = orig != 0 ? orig : state.Victim;
                if (border != null && IsShipCard(card))
                    SetBorderOwner(border, state.ShipOriginalOwner != 0 ? state.ShipOriginalOwner : state.Victim);
            }
            if (state.ShipBorder != null)
            {
                SyncDockableSideAfterOwnerChange(state.ShipBorder);
                UpdateHostBadge(state.ShipBorder);
            }
            _alienParasiteControls.Remove(state);
            _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}",
                $"Alien Parasites control ends — cards return to P{state.Victim}.");
            StatusText.Text = $"Alien Parasites: control returned to P{state.Victim}.";
        }
        if (_alienParasiteControls.Count != _alienParasiteControls.Count) { }
        SyncBoardFromTable();
    }

''' + anchor

# Fix the silly noop I accidentally included - remove it
methods = methods.replace('''        if (_alienParasiteControls.Count != _alienParasiteControls.Count) { }
        SyncBoardFromTable();
    }

''', '''        SyncBoardFromTable();
    }

''')

if anchor not in text:
    raise SystemExit("StopMissionAttemptTeam anchor not found")
text = text.replace(anchor, methods, 1)

path.write_text(text, encoding="utf-8")
print("TableWindow Fix1 hooks OK")
