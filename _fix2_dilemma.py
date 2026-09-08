from pathlib import Path

# --- DilemmaRules ---
path = Path(r"StarTrekCCG\Game\DilemmaRules.cs")
text = path.read_text(encoding="utf-8")

old_ha = '''    private static Result HyperAgingEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.HyperAging, ctx.Present, ctx.AttemptingPlayer))
            return new Result { Fate = Fate.Overcome, Score = 5, StopTeam = false,
                Message = "Hyper-Aging cured (SCIENCE + 2 MEDICAL) - +5; discarded; attempt continues." };
        return AttachContinue(ctx, PersistKind.HyperAging, 3,
            "Hyper-Aging (quarantine, countdown 3). Cure: SCIENCE + 2 MEDICAL. Attempt continues.");
    }'''

new_ha = '''    private static Result HyperAgingEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.HyperAging, ctx.Present, ctx.AttemptingPlayer))
            return new Result { Fate = Fate.Overcome, Score = 5, StopTeam = false,
                Message = "Hyper-Aging cured (SCIENCE + 2 MEDICAL) - +5; discarded; attempt continues." };
        return AttachContinue(ctx, PersistKind.HyperAging, 3,
            "Hyper-Aging (quarantine, countdown 3): no leave/beam away; joiners quarantined. Cure: SCIENCE + 2 MEDICAL. Attempt continues (not stopped).");
    }'''

if old_ha not in text:
    raise SystemExit("HyperAgingEncounter not found")
text = text.replace(old_ha, new_ha, 1)

old_stasis = '''    public static bool IsStasisPersist(PersistKind persist) =>
        persist is PersistKind.Phased or PersistKind.Abduction;'''

new_stasis = '''    public static bool IsStasisPersist(PersistKind persist) =>
        persist is PersistKind.Phased or PersistKind.Abduction;

    /// <summary>Hyper-Aging quarantine: cannot leave/beam away; joiners also quarantined.</summary>
    public static bool IsQuarantinePersist(PersistKind persist) =>
        persist is PersistKind.HyperAging;

    /// <summary>Leave/Beam blocked (stasis OR quarantine).</summary>
    public static bool IsLeaveBlockedPersist(PersistKind persist) =>
        IsStasisPersist(persist) || IsQuarantinePersist(persist);

    /// <summary>DE mini-test Hyper-Aging quarantine flags. Returns null if OK.</summary>
    public static string? VerifyHyperAgingQuarantine()
    {
        if (!IsQuarantinePersist(PersistKind.HyperAging))
            return "HyperAging should be quarantine persist";
        if (IsQuarantinePersist(PersistKind.RemFatigue))
            return "RemFatigue quarantine out of scope for this fix";
        if (!IsLeaveBlockedPersist(PersistKind.HyperAging))
            return "HyperAging should leave-block";
        if (!IsLeaveBlockedPersist(PersistKind.Abduction))
            return "Abduction should still leave-block via stasis";
        if (IsStasisPersist(PersistKind.HyperAging))
            return "HyperAging must NOT be stasis (AT not stopped on place)";

        // Encounter: no cure skills -> AttachAndContinue countdown 3, not stopped
        var civ = new Card { Name = "Civ", Type = "Personnel", Classification = "CIVILIAN", Integrity = "5", Cunning = "5", Strength = "5" };
        var ctx = new Ctx
        {
            Dilemma = new Card { Name = "Hyper-Aging", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = new List<Card> { civ },
            Present = new List<Card> { civ },
            AttemptingPlayer = 1
        };
        var r = Resolve(ctx);
        if (r.Fate != Fate.AttachAndContinue || r.StopTeam)
            return $"encounter: expected AttachAndContinue no stop, got {r.Fate}/stop={r.StopTeam}";
        if (r.Persist != PersistKind.HyperAging || r.Countdown != 3)
            return $"encounter: expected HyperAging cd 3, got {r.Persist}/{r.Countdown}";
        if (!ShouldRemoveFromSeed(r.Fate))
            return "encounter: seed removed when attached";
        return null;
    }'''

if old_stasis not in text:
    raise SystemExit("IsStasisPersist not found")
text = text.replace(old_stasis, new_stasis, 1)

# Check Resolve exists as public/private
if "private static Result Resolve(" not in text and "public static Result Resolve(" not in text:
    # might be Resolve(Ctx) with different access - search
    import re
    m = re.search(r'(public|private) static Result Resolve\(Ctx', text)
    print("Resolve:", m.group(0) if m else "MISSING")
    if not m:
        # Use HyperAgingEncounter via Resolve name from Encounter
        pass

path.write_text(text, encoding="utf-8")
print("DilemmaRules Fix2 OK")
