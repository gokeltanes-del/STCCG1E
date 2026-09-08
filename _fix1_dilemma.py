from pathlib import Path

path = Path(r"StarTrekCCG\Game\DilemmaRules.cs")
text = path.read_text(encoding="utf-8")

old_result_beam = '''        /// <summary>#1a Alien Parasites fail (planet): beam Away Team back to ship/outpost before stop.</summary>
        public bool BeamBackTeam { get; init; }
        /// <summary>Spock #8 Crystalline Entity (space fail): kill all life aboard (Stopped/Disabled/Intruder; NOT Stasis). Apply expands beyond encounter Team.</summary>
        public bool KillAllLifeAboardExceptStasis { get; init; }
    }'''

new_result_beam = '''        /// <summary>#1a Alien Parasites fail (planet): beam Away Team back to ship/outpost before stop.</summary>
        public bool BeamBackTeam { get; init; }
        /// <summary>Alien Parasites Neg: after fail, opponent may take temporary control (AT and/or one ship+crew).</summary>
        public bool GrantOpponentControl { get; init; }
        /// <summary>Spock #8 Crystalline Entity (space fail): kill all life aboard (Stopped/Disabled/Intruder; NOT Stasis). Apply expands beyond encounter Team.</summary>
        public bool KillAllLifeAboardExceptStasis { get; init; }
    }'''

if old_result_beam not in text:
    raise SystemExit("Result BeamBackTeam block not found")
text = text.replace(old_result_beam, new_result_beam, 1)

old_parasites = '''    private static Result Parasites(Ctx ctx)
    {
        var plan = DecideAlienParasites(Sum(ctx).integ, MissionRules.IsPlanetMission(ctx.Mission));
        return new Result
        {
            Fate = plan.Fate,
            StopTeam = plan.StopTeam,
            BeamBackTeam = plan.BeamBackTeam,
            Message = plan.Message
        };
    }'''

new_parasites = '''    private static Result Parasites(Ctx ctx)
    {
        var plan = DecideAlienParasites(Sum(ctx).integ, MissionRules.IsPlanetMission(ctx.Mission));
        return new Result
        {
            Fate = plan.Fate,
            StopTeam = plan.StopTeam,
            BeamBackTeam = plan.BeamBackTeam,
            GrantOpponentControl = plan.GrantOpponentControl,
            Message = plan.Message
        };
    }'''

if old_parasites not in text:
    raise SystemExit("Parasites method not found")
text = text.replace(old_parasites, new_parasites, 1)

old_block = '''    // ---- Alien Parasites #1a (Pass/Fail + Beam-back + Stop + Replace; Hotseat-Control PARK) ----

    public readonly record struct AlienParasitesPlan(
        Fate Fate,
        bool StopTeam,
        bool BeamBackTeam,
        string Message);

    /// <summary>
    /// #1a Soll: Pass INTEGRITY&gt;32 to Overcome (discard + continue).
    /// Fail to WallFailed (dilemma stays under mission), StopTeam, planet BeamBack.
    /// No opponent control / hotseat / next-turn timer.
    /// </summary>
    public static AlienParasitesPlan DecideAlienParasites(int integritySum, bool isPlanetMission)
    {
        if (integritySum > 32)
        {
            return new AlienParasitesPlan(
                Fate.Overcome,
                StopTeam: false,
                BeamBackTeam: false,
                Message: "INTEGRITY>32 - Alien Parasites overcome.");
        }
        string msg = isPlanetMission
            ? "Alien Parasites: INTEGRITY<=32 - attempt ends; Away Team beams back; dilemma remains under mission; team stopped."
            : "Alien Parasites: INTEGRITY<=32 - attempt ends; dilemma remains under mission; crew and ship stopped.";
        return new AlienParasitesPlan(
            Fate.WallFailed,
            StopTeam: true,
            BeamBackTeam: isPlanetMission,
            Message: msg);
    }

    /// <summary>DE mini-test for Alien Parasites #1a. Returns null if OK, else failure reason.</summary>
    public static string? VerifyAlienParasites1a()
    {
        var pass = DecideAlienParasites(33, isPlanetMission: true);
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.BeamBackTeam)
            return "pass(33,planet): expected Overcome, no stop/beam";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass: seed should discard (Overcome)";

        var failEq = DecideAlienParasites(32, isPlanetMission: true);
        if (failEq.Fate != Fate.WallFailed || !failEq.StopTeam || !failEq.BeamBackTeam)
            return "fail(32,planet): expected WallFailed+Stop+BeamBack";
        if (ShouldRemoveFromSeed(failEq.Fate))
            return "fail planet: dilemma must stay under mission (WallFailed)";

        var failSpace = DecideAlienParasites(10, isPlanetMission: false);
        if (failSpace.Fate != Fate.WallFailed || !failSpace.StopTeam || failSpace.BeamBackTeam)
            return "fail(space): expected WallFailed+Stop, no BeamBack";
        if (ShouldRemoveFromSeed(failSpace.Fate))
            return "fail space: dilemma must stay under mission";

        return null;
    }'''

# The file may use literal > not &gt; in summary - check
if old_block not in text:
    # try with actual >
    old_block = old_block.replace("INTEGRITY&gt;32", "INTEGRITY>32")
    if old_block not in text:
        idx = text.find("// ---- Alien Parasites #1a")
        print("FOUND IDX", idx)
        print(repr(text[idx:idx+800]))
        raise SystemExit("Alien Parasites #1a block not found")

new_block = '''    // ---- Alien Parasites (Pass/Fail + Beam-back + Stop + Neg Control min path) ----
    // Hotseat dual-window / affiliation-mix deep enforcement PARK (Pepsch min path).

    public enum AlienParasitesControlChoice
    {
        None = 0,
        AwayTeam = 1,
        OneShipAndCrew = 2,
        AwayTeamAndShip = AwayTeam | OneShipAndCrew
    }

    public readonly record struct AlienParasitesPlan(
        Fate Fate,
        bool StopTeam,
        bool BeamBackTeam,
        bool GrantOpponentControl,
        string Message);

    /// <summary>
    /// Pass INTEGRITY&gt;32 to Overcome (discard + continue).
    /// Fail to WallFailed (dilemma stays under mission), StopTeam, planet BeamBack,
    /// GrantOpponentControl (Opp chooses AT and/or one ship+crew until start of your next turn).
    /// </summary>
    public static AlienParasitesPlan DecideAlienParasites(int integritySum, bool isPlanetMission)
    {
        if (integritySum > 32)
        {
            return new AlienParasitesPlan(
                Fate.Overcome,
                StopTeam: false,
                BeamBackTeam: false,
                GrantOpponentControl: false,
                Message: "INTEGRITY>32 - Alien Parasites overcome.");
        }
        string msg = isPlanetMission
            ? "Alien Parasites: INTEGRITY<=32 - attempt ends; Away Team beams back; dilemma remains under mission; team stopped; opponent may take control."
            : "Alien Parasites: INTEGRITY<=32 - attempt ends; dilemma remains under mission; crew and ship stopped; opponent may take control.";
        return new AlienParasitesPlan(
            Fate.WallFailed,
            StopTeam: true,
            BeamBackTeam: isPlanetMission,
            GrantOpponentControl: true,
            Message: msg);
    }

    /// <summary>Restore when the controlling opponent ends their turn (= start of victim next turn).</summary>
    public static bool ShouldRestoreAlienParasitesControl(int controllerPlayer, int finishingPlayer) =>
        controllerPlayer is 1 or 2 && controllerPlayer == finishingPlayer;

    /// <summary>Parse Opp chooser label into control scope. Unknown / empty = None.</summary>
    public static AlienParasitesControlChoice ParseAlienParasitesControlChoice(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return AlienParasitesControlChoice.None;
        string s = label.Trim();
        bool at = s.Contains("Away Team", StringComparison.OrdinalIgnoreCase);
        bool ship = s.Contains("ship", StringComparison.OrdinalIgnoreCase);
        if (at && ship) return AlienParasitesControlChoice.AwayTeamAndShip;
        if (at) return AlienParasitesControlChoice.AwayTeam;
        if (ship) return AlienParasitesControlChoice.OneShipAndCrew;
        return AlienParasitesControlChoice.None;
    }

    /// <summary>DE mini-test for Alien Parasites #1a + Neg control flags. Returns null if OK.</summary>
    public static string? VerifyAlienParasites1a()
    {
        var pass = DecideAlienParasites(33, isPlanetMission: true);
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.BeamBackTeam || pass.GrantOpponentControl)
            return "pass(33,planet): expected Overcome, no stop/beam/control";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass: seed should discard (Overcome)";

        var failEq = DecideAlienParasites(32, isPlanetMission: true);
        if (failEq.Fate != Fate.WallFailed || !failEq.StopTeam || !failEq.BeamBackTeam || !failEq.GrantOpponentControl)
            return "fail(32,planet): expected WallFailed+Stop+BeamBack+Control";
        if (ShouldRemoveFromSeed(failEq.Fate))
            return "fail planet: dilemma must stay under mission (WallFailed)";

        var failSpace = DecideAlienParasites(10, isPlanetMission: false);
        if (failSpace.Fate != Fate.WallFailed || !failSpace.StopTeam || failSpace.BeamBackTeam || !failSpace.GrantOpponentControl)
            return "fail(space): expected WallFailed+Stop+Control, no BeamBack";
        if (ShouldRemoveFromSeed(failSpace.Fate))
            return "fail space: dilemma must stay under mission";

        if (!ShouldRestoreAlienParasitesControl(2, 2))
            return "restore: Opp EOT should restore";
        if (ShouldRestoreAlienParasitesControl(2, 1))
            return "restore: victim EOT must not restore";

        if (ParseAlienParasitesControlChoice("Away Team only") != AlienParasitesControlChoice.AwayTeam)
            return "parse: Away Team only";
        if (ParseAlienParasitesControlChoice("One ship + crew") != AlienParasitesControlChoice.OneShipAndCrew)
            return "parse: One ship + crew";
        if (ParseAlienParasitesControlChoice("Away Team AND one ship + crew") != AlienParasitesControlChoice.AwayTeamAndShip)
            return "parse: Both";

        return null;
    }'''

# Fix XML in new_block - source file uses raw >
new_block = new_block.replace("INTEGRITY&gt;32", "INTEGRITY>32")

if old_block not in text:
    raise SystemExit("still cannot find old block")
text = text.replace(old_block, new_block, 1)

path.write_text(text, encoding="utf-8")
print("DilemmaRules.cs Fix1 OK")
# quick verify
err = None
for needle in ["GrantOpponentControl", "ShouldRestoreAlienParasitesControl", "ParseAlienParasitesControlChoice", "AlienParasitesControlChoice"]:
    if needle not in text:
        err = needle
print("missing" if err else "all needles present", err or "")
