from pathlib import Path
lm = Path(r"StarTrekCCG\Game\LegalMoves.cs")
lt = lm.read_text(encoding="utf-8")

lt = lt.replace(
'''            if (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship — UI picks destination"));''',
'''            if (!ship.QuarantineLeaveBlocked
                && (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard)))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship — UI picks destination"));''',
1)

lt = lt.replace(
'''        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility — UI picks destination"));
        }''',
'''        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (fac.QuarantineLeaveBlocked) continue;
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility — UI picks destination"));
        }''',
1)

lt = lt.replace(
'''        foreach (var m in state.Missions())
        {
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission — UI picks destination"));
        }''',
'''        foreach (var m in state.Missions())
        {
            if (m.QuarantineLeaveBlocked) continue;
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission — UI picks destination"));
        }''',
1)

if "QuarantineLeaveBlocked" not in lt:
    raise SystemExit("replacements failed")
lm.write_text(lt, encoding="utf-8")
print("LegalMoves OK", lt.count("QuarantineLeaveBlocked"))
