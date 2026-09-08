from pathlib import Path
path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

old = '''        if (choice.HasFlag(DilemmaRules.AlienParasitesControlChoice.AwayTeam))
        {
            foreach (var (b, pc) in awayTeam)
            {
                int orig = pc.Controller != 0 ? pc.Controller : victim;
                pc.Controller = opp;
                state.Cards.Add((pc, orig, b));
            }
        }'''

new = '''        if (choice.HasFlag(DilemmaRules.AlienParasitesControlChoice.AwayTeam))
        {
            foreach (var (b, pc) in awayTeam)
            {
                int orig = pc.Controller != 0 ? pc.Controller : victim;
                pc.Controller = opp;
                SetBorderOwner(b, opp);
                state.Cards.Add((pc, orig, b));
            }
        }'''

if old not in text:
    raise SystemExit("AT control block not found")
text = text.replace(old, new, 1)

old2 = '''                    int orig = pc.Controller != 0 ? pc.Controller : victim;
                    pc.Controller = opp;
                    state.Cards.Add((pc, orig, b));
                }
            }
            SyncDockableSideAfterOwnerChange(chosenShip);'''

new2 = '''                    int orig = pc.Controller != 0 ? pc.Controller : victim;
                    pc.Controller = opp;
                    SetBorderOwner(b, opp);
                    state.Cards.Add((pc, orig, b));
                }
            }
            SyncDockableSideAfterOwnerChange(chosenShip);'''

if old2 not in text:
    raise SystemExit("crew control block not found")
text = text.replace(old2, new2, 1)

old3 = '''            foreach (var (card, orig, border) in state.Cards)
            {
                card.Controller = orig != 0 ? orig : state.Victim;
                if (border != null && IsShipCard(card))
                    SetBorderOwner(border, state.ShipOriginalOwner != 0 ? state.ShipOriginalOwner : state.Victim);
            }'''

new3 = '''            foreach (var (card, orig, border) in state.Cards)
            {
                int back = orig != 0 ? orig : state.Victim;
                card.Controller = back;
                if (border != null)
                {
                    if (IsShipCard(card))
                        SetBorderOwner(border, state.ShipOriginalOwner != 0 ? state.ShipOriginalOwner : state.Victim);
                    else
                        SetBorderOwner(border, back);
                }
            }'''

if old3 not in text:
    raise SystemExit("restore block not found")
text = text.replace(old3, new3, 1)

path.write_text(text, encoding="utf-8")
print("owner transfer OK")
