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
'''

# Need full Parasites method - read around it
idx = text.find('private static Result Parasites(Ctx ctx)')
print("Parasites at", idx)
print(repr(text[idx:idx+450]))
