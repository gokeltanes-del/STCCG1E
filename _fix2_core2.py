from pathlib import Path

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
if old not in t: raise SystemExit("Tone missing")
t = t.replace(old, new, 1)

# append before final closing brace of class
marker = "    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>\n        $\"Held/Stasis: {dilemmaName} — {personnelNames}\";\n}\n"
if marker not in t:
    raise SystemExit("marker missing: " + repr(t[t.find("FormatHeld"):]))
extra = '''    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>
        $"Held/Stasis: {dilemmaName} — {personnelNames}";

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
        $"Quarantine: {dilemmaName} — {personnelNames}";
}
'''
t = t.replace(marker, extra, 1)
p.write_text(t, encoding="utf-8")
print("DetailStatus OK")

gs = Path(r"StarTrekCCG\Game\GameState.cs")
gt = gs.read_text(encoding="utf-8")
oldg = "    public bool AttemptBlocked { get; init; }\n    public string? AttemptBlockReason { get; init; }"
newg = oldg + "\n    /// <summary>Hyper-Aging etc.: personnel here cannot leave/beam away.</summary>\n    public bool QuarantineLeaveBlocked { get; init; }"
if oldg not in gt: raise SystemExit("GameState anchor missing")
gs.write_text(gt.replace(oldg, newg, 1), encoding="utf-8")
print("GameState OK")

lm = Path(r"StarTrekCCG\Game\LegalMoves.cs")
lt = lm.read_text(encoding="utf-8")
# use unique smaller replacements
a = '''            if (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship - UI picks destination"));'''
b = '''            if (!ship.QuarantineLeaveBlocked
                && (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard)))
                list.Add(GameAction.Beam(player, ship.Card, note: "from ship - UI picks destination"));'''
if a not in lt: raise SystemExit("ship beam missing")
lt = lt.replace(a, b, 1)

a2 = '''        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility - UI picks destination"));
        }'''
b2 = '''        foreach (var fac in state.Facilities().Where(f =>
                     f.Owner == player || f.Controller == player))
        {
            if (fac.QuarantineLeaveBlocked) continue;
            if (!fac.Occupied && !fac.Aboard.Any(ModifierRules.IsPersonnelCard))
                continue;
            list.Add(GameAction.Beam(player, fac.Card, note: "from facility - UI picks destination"));
        }'''
if a2 not in lt: raise SystemExit("fac beam missing")
lt = lt.replace(a2, b2, 1)

a3 = '''        foreach (var m in state.Missions())
        {
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission - UI picks destination"));
        }'''
b3 = '''        foreach (var m in state.Missions())
        {
            if (m.QuarantineLeaveBlocked) continue;
            bool mine = m.Aboard.Any(p =>
                ModifierRules.IsPersonnelCard(p)
                && (p.Controller == player || p.OwnerPlayer == player));
            if (!mine) continue;
            list.Add(GameAction.Beam(player, m.Card, note: "from mission - UI picks destination"));
        }'''
if a3 not in lt: raise SystemExit("mission beam missing")
lt = lt.replace(a3, b3, 1)
lm.write_text(lt, encoding="utf-8")
print("LegalMoves OK")
