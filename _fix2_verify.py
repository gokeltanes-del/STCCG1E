from pathlib import Path
path = Path(r"StarTrekCCG\Game\DilemmaRules.cs")
text = path.read_text(encoding="utf-8")
old = '''    /// <summary>DE mini-test Hyper-Aging quarantine flags. Returns null if OK.</summary>
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

new = '''    /// <summary>DE mini-test Hyper-Aging quarantine flags. Returns null if OK.</summary>
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

        var civ = new Card
        {
            Name = "Civ",
            Type = "Personnel",
            Class = "CIVILIAN",
            Text = "CIVILIAN",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };
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

if old not in text:
    raise SystemExit("verify block not found")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("verify fixed")
