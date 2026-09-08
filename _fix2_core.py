from pathlib import Path

# DetailStatusRules
p = Path(r"StarTrekCCG\Game\DetailStatusRules.cs")
t = p.read_text(encoding="utf-8")
old = '''    public static DetailStatusTone ToneForDilemma(DilemmaRules.PersistKind kind, int countdown)
    {
        if (DilemmaRules.IsStasisPersist(kind))
            return DetailStatusTone.Stasis;
        if (countdown > 0)
            return DetailStatusTone.Timer;
        return DetailStatusTone.Debuff;
    }'''
new = '''    public static DetailStatusTone ToneForDilemma(DilemmaRules.PersistKind kind, int countdown)
    {
        if (DilemmaRules.IsStasisPersist(kind) || DilemmaRules.IsQuarantinePersist(kind))
            return DetailStatusTone.Stasis;
        if (countdown > 0)
            return DetailStatusTone.Timer;
        return DetailStatusTone.Debuff;
    }'''
if old not in t: raise SystemExit("ToneForDilemma missing")
t = t.replace(old, new, 1)

old2 = '''    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>
        $"Held/Stasis: {dilemmaName} - {personnelNames}";
}'''
new2 = '''    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>
        $"Held/Stasis: {dilemmaName} - {personnelNames}";

    public static string FormatQuarantineLine(string dilemmaName, string? cureHint = null)
    {
        string name = string.IsNullOrWhiteSpace(dilemmaName) ? "quarantine" : dilemmaName.Trim();
        if (string.IsNullOrWhiteSpace(cureHint))
            return $"Quarantined ({name}) — cannot leave/beam away";
        return $"Quarantined ({name} - {cureHint}) — cannot leave/beam away";
    }

    public static string QuarantineCureHint(DilemmaRules.PersistKind kind) => kind switch
    {
        DilemmaRules.PersistKind.HyperAging => "cure: SCIENCE + 2 MEDICAL",
        _ => ""
    };

    public static string FormatHeldQuarantineSectionLine(string dilemmaName, string personnelNames) =>
        $"Quarantine: {dilemmaName} - {personnelNames}";
}'''
if old2 not in t: raise SystemExit("FormatHeld missing")
p.write_text(t.replace(old2, new2, 1), encoding="utf-8")
print("DetailStatusRules OK")

# BoardPiece flag
gs = Path(r"StarTrekCCG\Game\GameState.cs")
gt = gs.read_text(encoding="utf-8")
oldg = '''    public bool AttemptBlocked { get; init; }
    public string? AttemptBlockReason { get; init; }'''
newg = '''    public bool AttemptBlocked { get; init; }
    public string? AttemptBlockReason { get; init; }
    /// <summary>Hyper-Aging etc.: personnel here cannot leave/beam away.</summary>
    public bool QuarantineLeaveBlocked { get; init; }'''
if oldg not in gt: raise SystemExit("AttemptBlocked missing")
gs.write_text(gt.replace(oldg, newg, 1), encoding="utf-8")
print("GameState OK")

# LegalMoves CollectBeam - skip mission/ship/facility beam when QuarantineLeaveBlocked
lm = Path(r"StarTrekCCG\Game\LegalMoves.cs")
lt = lm.read_text(encoding="utf-8")
oldb = '''            if (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship - UI picks destination"));
        }

        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility - UI picks destination"));
        }

        foreach (var m in state.Missions())
        {
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission - UI picks destination"));
        }
    }
}'''
newb = '''            if (!ship.QuarantineLeaveBlocked
                && (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard)))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship - UI picks destination"));
        }

        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (fac.QuarantineLeaveBlocked) continue;
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility - UI picks destination"));
        }

        foreach (var m in state.Missions())
        {
            if (m.QuarantineLeaveBlocked) continue;
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission - UI picks destination"));
        }
    }
}'''
if oldb not in lt: raise SystemExit("CollectBeam tail missing")
lm.write_text(lt.replace(oldb, newb, 1), encoding="utf-8")
print("LegalMoves OK")
