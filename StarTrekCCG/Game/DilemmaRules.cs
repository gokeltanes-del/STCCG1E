
using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere-Dilemma-Effekte (45 Karten). UI liefert Team/Picks; Regeln entscheiden Outcome.
/// </summary>
public static class DilemmaRules
{
    public enum Fate
    {
        /// <summary>Filter überwunden or „otherwise“ – Dilemma weg, Attempt weiter.</summary>
        Overcome,
        /// <summary>Filter nicht geschafft – Dilemma bleibt, Attempt endet, Team stoppt.</summary>
        WallFailed,
        /// <summary>Effekt ausgelöst, Dilemma weg, Attempt endet (meist Team stoppt).</summary>
        EffectAndEnd,
        /// <summary>Dilemma bleibt am Schiff/Mission als dauerhafter Effekt.</summary>
        AttachAndEnd,
        /// <summary>Place persist on host; attempt continues (no stop).</summary>
        AttachAndContinue,
        /// <summary>Effect applied (e.g. Love Interest relocate); dilemma discarded; attempt continues (no stop).</summary>
        EffectAndContinue,
        /// <summary>Versuch endet ohne Stop (z. B. Scow) or mit Sonderfall.</summary>
        EndAttempt
    }

    public enum PersistKind
    {
        None,
        Junior,
        Scow,
        HyperAging,
        RemFatigue,
        Nitrium,
        Ktarian,
        Menthar,
        Tsiolkovsky,
        TwoDim,
        Abduction,
        Phased,
        Cytherians,
        BorgShip,
        EdoProbe,
        Conundrum,
        FrameOfMind
    }

    public sealed class Result
    {
        public Fate Fate { get; init; }
        public string Message { get; init; } = "";
        public int Score { get; init; }
        public bool StopTeam { get; init; }
        public bool EndTurn { get; init; }
        public List<Card> Kill { get; } = new();
        /// <summary>Personnel discarded without being killed (e.g. Anaphasic Organism resign).</summary>
        public List<Card> Discard { get; } = new();
        public List<Card> DiscardNonPersonnelFromHand { get; } = new();
        public Card? Relocate { get; set; }
        public PersistKind Persist { get; init; }
        public int Countdown { get; init; }
        public bool DamageShip { get; init; }
        public bool DestroyShip { get; init; }
        public bool DrawForDiscarded { get; init; }
        /// <summary>#1a Alien Parasites fail (planet): beam Away Team back to ship/outpost before stop.</summary>
        public bool BeamBackTeam { get; init; }
        /// <summary>Alien Parasites Neg: after fail, opponent may take temporary control (AT and/or one ship+crew).</summary>
        public bool GrantOpponentControl { get; init; }
        /// <summary>Spock #8 Crystalline Entity (space fail): kill all life aboard (Stopped/Disabled/Intruder; NOT Stasis). Apply expands beyond encounter Team.</summary>
        public bool KillAllLifeAboardExceptStasis { get; init; }
    }

    public sealed class Ctx
    {
        public required Card Dilemma { get; init; }
        public required Card Mission { get; init; }
        public required IReadOnlyList<Card> Team { get; init; }       // Personnel
        public required IReadOnlyList<Card> Present { get; init; }    // + Equipment
        public Card? Ship { get; init; }
        public int ShipShields { get; init; }
        public int AttemptingPlayer { get; init; }
        public IReadOnlyList<Card>? Hand { get; init; }
        public Random Rng { get; init; } = new();
        public Func<string, IReadOnlyList<Card>, Card?>? PickYou { get; init; }
        public Func<string, IReadOnlyList<Card>, Card?>? PickOpp { get; init; }
        public Func<string, bool>? Confirm { get; init; }
        /// <summary>True when the attempt is at the attempting player's outpost (Outpost Raid).</summary>
        public bool AtOwnOutpost { get; init; }
        /// <summary>True when The Traveler: Transcendence is affecting the attempting player.</summary>
        public bool TravelerAffecting { get; init; }
        public bool ThermalDeflectors { get; init; }
        /// <summary>
        /// True when the Away Team can immediately beam off the planet to an own ship or facility
        /// (no member quarantined/in stasis, and a valid destination is present).
        /// </summary>
        public bool CanBeamOffPlanet { get; init; } = true;
    }

    public static Result Resolve(Ctx ctx)
    {
        string n = (ctx.Dilemma.Name ?? "").Trim();
        return n switch
        {
            "Ancient Computer" => AncientComputer(ctx),
            "Impassable Door" => ImpassableDoor(ctx),
            "Hologram Ruse" => Wall(ctx, Sum(ctx).integ > 30 && Sum(ctx).cunn > 30, "INTEGRITY>30 and CUNNING>30"),
            "Shaka, When the Walls Fell" => Wall(ctx, Skill(ctx, "Diplomacy", 2) && Sum(ctx).cunn > 30, "2 Diplomacy and CUNNING>30"),
            "Wind Dancer" => Wall(ctx,
                Skill(ctx, "Music") || Skill(ctx, "Youth") || ctx.Team.Any(p => Eff(ctx, p).Strength > 9)
                || ctx.Team.Any(p => (p.Name ?? "").Contains("Lwaxana", StringComparison.OrdinalIgnoreCase)),
                "Music OR Youth OR STRENGTH>9 OR Lwaxana Troi"),
            "Matriarchal Society" => MatriarchalSociety(ctx),
            "Armus: Skin Of Evil" => ArmusSkinOfEvil(ctx),
            "Nausicaans" => Nausicaans(ctx),
            "Rebel Encounter" => Rebel(ctx),
            "Chalnoth" => Chalnoth(ctx),
            "Archer" => UnlessThen(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
                HighestAttr(ctx), "MEDICAL and SECURITY", "Archer kills highest attribute total.", true),
            "Anaphasic Organism" => Anaphasic(ctx),
            "El-Adrel Creature" => ElAdrel(ctx),
            "Firestorm" => Firestorm(ctx),
            "Microvirus" => Microvirus(ctx),
            "Barclay's Protomorphosis Disease" => UnlessScoreOr(ctx,
                Skill(ctx, "MEDICAL") && Skill(ctx, "SCIENCE") && Skill(ctx, "SECURITY"),
                () => { foreach (var p in ctx.Team.Where(x => !IsInorganic(x))) AddKill(_tmp, p); },
                10, "MEDICAL, SCIENCE and SECURITY", useTmp: true),
            "Nagilum" => Nagilum(ctx),
            "Crystalline Entity" => Crystalline(ctx),

            "Gravitic Mine" => GraviticMine(ctx),
            "Nanites" => Nanites(ctx),
            "Null Space" => NullSpace(ctx),
            "Microbiotic Colony" => MicrobioticColony(ctx),
            "Cosmic String Fragment" => CosmicStringFragment(ctx),

            "Birth of \"Junior\"" => BirthOfJunior(ctx),
            "Nitrium Metal Parasites" => NitriumEncounter(ctx),
            "Tsiolkovsky Infection" => Tsiolkovsky(ctx),
            "Two-Dimensional Creatures" => TwoDim(ctx),
            "Menthar Booby Trap" => Menthar(ctx),
            "Ktarian Game" => KtarianGame(ctx),
            "Radioactive Garbage Scow" => new Result
            {
                Fate = Fate.AttachAndEnd,
                Persist = PersistKind.Scow,
                StopTeam = false,
                Message = "Scow on mission: attempt ends. Mission cannot be attempted until towed away (Tractor Beam + 2 ENGINEER)."
            },
            "Hyper-Aging" => HyperAgingEncounter(ctx),
            "REM Fatigue" => RemFatigueEncounter(ctx),
            "Alien Abduction" => Abduction(ctx),
            "Phased Matter" => Phased(ctx),
            "Cytherians" => Cytherians(ctx),
            "Borg Ship" => new Result
            {
                Fate = Fate.AttachAndEnd,
                Persist = PersistKind.BorgShip,
                StopTeam = true,
                Message = "Borg Ship placed at furthest spaceline end. End of every turn: attacks ships here (WEAPONS 24), then moves one mission toward the opposite end and off the spaceline. Destroy in battle for 15 points."
            },

            "Female's Love Interest" => RelocateGender(ctx, female: true),
            "Male's Love Interest" => RelocateGender(ctx, female: false),
            "Portal Guard" => Portal(ctx),
            "Sarjenka" => Sarjenka(ctx),
            "Tarellian Plague Ship" => Tarellian(ctx),
            "Iconian Computer Weapon" => IconianComputerWeapon(ctx),
            "Alien Parasites" => Parasites(ctx),
            "Q" => Qdil(ctx),
            "Temporal Causality Loop" => Loop(ctx),

            // ---------- Alternate Universe (printed text 2026-08-26) ----------
            "Alien Labyrinth" => Wall(ctx,
                HasNamedGear(ctx, "Tricorder") || Skill(ctx, "ENGINEER", 2),
                "Tricorder OR 2 ENGINEER"),
            "Hidden Entrance" => Wall(ctx,
                ctx.Team.Any(p => (p.Name ?? "").Contains("Geordi", StringComparison.OrdinalIgnoreCase))
                || (Skill(ctx, "ENGINEER") && Sum(ctx).cunn > 32),
                "Geordi La Forge OR ENGINEER + CUNNING>32"),
            "Malfunctioning Door" => Wall(ctx,
                ctx.Team.Any(IsAndroid)
                || ctx.Team.OrderByDescending(p => Eff(ctx, p).Strength).Take(4).Sum(p => Eff(ctx, p).Strength) > 27,
                "Soong-Type android OR STRENGTH>27 from up to four"),
            "Outpost Raid" => OutpostRaidAu(ctx),
            "Zaldan" => ZaldanAu(ctx),
            "Hunter Gangs" => HunterGangsAu(ctx),
            "Punishment Zone" => KillRandomEnd(ctx,
                "Punishment Zone: one random Away Team member killed (beam-up −5 / Fed ×2 not automated)."),
            "Ferengi Attack" => FerengiAttackAu(ctx),
            "Coalescent Organism" => CoalescentAu(ctx),
            "The Gatherers" => GatherersAu(ctx),
            "Thought Fire" => ThoughtFireAu(ctx),
            "Interphasic Plasma Creatures" => InterphasicPlasmaAu(ctx),
            "Parallel Romance" => ParallelRomanceAu(ctx),
            "Quantum Singularity Lifeforms" => AttachContinue(ctx, PersistKind.None, 0,
                "If Romulan ship present: stasis here. Cure: Emergency Transporter Armbands / ENGINEER (sandbox). Attempt continues."),
            "Rascals" => AttachContinue(ctx, PersistKind.None, 0,
                "Up to 4 unique crew become kids (STR 2, Youth). Cure: 2 MEDICAL + Biology. Attempt continues."),
            "Maman Picard" => new Result
            {
                Fate = Fate.EffectAndEnd,
                StopTeam = true,
                Message = "Federation ship: relocate to spaceline end (opponent chooses — sandbox marker)."
            },
            "Conundrum" => ConundrumAu(ctx),
            "Edo Probe" => EdoProbeAu(ctx),
            "Frame of Mind" => FrameOfMindAu(ctx),
            "Empathic Echo" => EmpathicEchoAu(ctx),
            "Cardassian Trap" => CardassianTrapAu(ctx),
            "Royale Casino: Blackjack" => new Result
            {
                Fate = Fate.EffectAndEnd,
                StopTeam = false,
                Score = 0,
                Message = "Royale Casino: Blackjack — sandbox: +0 (play CUNNING blackjack later)."
            },
            "The Higher... The Fewer" => new Result
            {
                Fate = Fate.EffectAndEnd,
                StopTeam = false,
                Score = -Math.Min(ctx.Team.Count, 20),
                Message = $"The Higher… The Fewer: score −{Math.Min(ctx.Team.Count, 20)} (X = team size)."
            },
            "Worshiper" => WorshiperAu(ctx),

            _ => Fallback(ctx)
        };
    }

    private static bool HasNamedGear(Ctx ctx, string name) =>
        ctx.Present.Any(c => (c.Name ?? "").Contains(name, StringComparison.OrdinalIgnoreCase));

    private static bool IsCardassian(Card p)
    {
        string blob = $"{p.Affiliation} {p.Icons} {p.Characteristics}";
        return blob.Contains("[Car]", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("Cardassian", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUniversalCard(Card c)
    {
        string u = c.Uniqueness ?? "";
        string n = c.Name ?? "";
        return u.Contains("univ", StringComparison.OrdinalIgnoreCase)
               || n.StartsWith("❖")
               || n.Contains("Universal", StringComparison.OrdinalIgnoreCase);
    }

    private static Result KillAndContinue(Ctx ctx, string msg)
    {
        var r = new Result { Fate = Fate.Overcome, StopTeam = false, Message = msg };
        AddKill(r.Kill, RandomOf(ctx, ctx.Team));
        return r;
    }

    private static Result FerengiAttackAu(Ctx ctx)
    {
        var tot = Sum(ctx);
        bool ok = tot.cunn + tot.str > 68 || Skill(ctx, "Greed");
        if (ok)
            return new Result { Fate = Fate.Overcome, Message = "Ferengi Attack: CUNNING+STRENGTH>68 or Greed." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Ferengi Attack: opponent kills one."
        };
        _tmp.Clear();
        PickKill(ctx, opp: true, "Ferengi Attack: opponent chooses a victim.");
        r.Kill.AddRange(_tmp);
        _tmp.Clear();
        return r;
    }

    private static Result CoalescentAu(Ctx ctx)
    {
        if (Skill(ctx, "Exobiology"))
            return new Result { Fate = Fate.Overcome, Message = "Coalescent Organism: Exobiology present." };
        var victim = RandomOf(ctx, ctx.Team);
        var r = Attach(ctx, PersistKind.None, 1,
            $"{victim?.Name ?? "A personnel"} marked by Coalescent Organism: dies at end of your next turn, then passes on (sandbox).");
        r.Relocate = victim;
        return r;
    }

    private static Result ConundrumAu(Ctx ctx)
    {
        if (Sum(ctx).integ > 40)
            return new Result { Fate = Fate.Overcome, Message = "Conundrum: INTEGRITY>40." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = false,
            Message = "Conundrum: this ship must chase and attack an opponent's ship on this spaceline (normal speed)."
        };
    }

    private static Result EdoProbeAu(Ctx ctx)
    {
        bool abandon = ctx.Confirm?.Invoke(
            "Edo Probe: abandon this attempt until any player completes a different mission? (NO = continue, −10 if not solved this turn)")
            ?? true;
        if (abandon)
            return Attach(ctx, PersistKind.EdoProbe, 0,
                "Edo Probe: attempt abandoned until any player solves a different mission.");
        return new Result
        {
            Fate = Fate.Overcome,
            StopTeam = false,
            Score = 0,
            Message = "Edo Probe: continue — lose 10 points if this mission is not solved this turn."
        };
    }

    private static Result FrameOfMindAu(Ctx ctx)
    {
        var victim = RandomOf(ctx, ctx.Team);
        if (victim == null)
            return new Result { Fate = Fate.Overcome, Message = "Frame of Mind: no personnel." };

        // Rulebook 7.2.2.3: Conditions first (victim affected). Victim cannot contribute to cure.
        var remainingPresent = ExcludeHeld(ctx.Present, new[] { victim });
        if (CanCure(PersistKind.FrameOfMind, remainingPresent, ctx.AttemptingPlayer))
        {
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = $"Frame of Mind on {victim.Name}: immediately cured by remaining team (3 Empathy). Discard dilemma; attempt continues."
            };
        }

        var r = AttachContinue(ctx, PersistKind.FrameOfMind, 0,
            $"Frame of Mind on {victim.Name}: Non-Aligned 3-3-3, two skills (opponent's choice). Cure: 3 Empathy. Attempt continues.");
        r.Relocate = victim;
        return r;
    }

    private static Result EmpathicEchoAu(Ctx ctx)
    {
        var empaths = ctx.Team.Where(p =>
        {
            foreach (var kv in Eff(ctx, p).Skills)
                if (kv.Key.Contains("Empathy", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }).ToList();
        if (empaths.Count == 0)
            return new Result { Fate = Fate.Overcome, Message = "Empathic Echo: no Empathy present — no target." };
        if (Skill(ctx, "SECURITY") && Skill(ctx, "MEDICAL"))
            return new Result { Fate = Fate.Overcome, Message = "Empathic Echo: SECURITY and MEDICAL present." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Empathic Echo: Empathy personnel killed."
        };
        AddKill(r.Kill, RandomOf(ctx, empaths));
        return r;
    }

    private static Result CardassianTrapAu(Ctx ctx)
    {
        if (Skill(ctx, "Empathy"))
            return new Result { Fate = Fate.Overcome, Message = "Cardassian Trap: Empathy present." };
        var pool = ctx.Team.Where(p => !IsCardassian(p) && !IsUniversalCard(p)).ToList();
        if (pool.Count == 0) pool = ctx.Team.Where(p => !IsCardassian(p)).ToList();
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Cardassian Trap: opponent captures one unique non-Cardassian (sandbox: discarded)."
        };
        AddKill(r.Kill, RandomOf(ctx, pool));
        return r;
    }

    private static Result ZaldanAu(Ctx ctx)
    {
        bool ok = Skill(ctx, "Treachery", 2)
                  || ctx.Team.Any(p => (p.Name ?? "").Contains("Wesley", StringComparison.OrdinalIgnoreCase))
                  || Skill(ctx, "Exobiology")
                  || ctx.Present.Any(c =>
                      (c.Name ?? "").Contains("disruptor", StringComparison.OrdinalIgnoreCase)
                      || ((c.Type ?? "").Contains("equipment", StringComparison.OrdinalIgnoreCase)
                          && (c.Name ?? "").Contains("Phaser", StringComparison.OrdinalIgnoreCase)));
        if (ok)
            return new Result { Fate = Fate.Overcome, Message = "Zaldan: filter met." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Zaldan kills two with Diplomacy (random)."
        };
        var dipl = ctx.Team.Where(p =>
        {
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
                if (kv.Key.Contains("Diplomacy", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }).ToList();
        var pool = dipl.Count > 0 ? dipl : ctx.Team.ToList();
        AddKill(r.Kill, RandomOf(ctx, pool));
        AddKill(r.Kill, RandomOf(ctx, pool.Where(p => !r.Kill.Contains(p))));
        return r;
    }

    private static Result InterphasicPlasmaAu(Ctx ctx)
    {
        if (Skill(ctx, "SCIENCE", 2) || Skill(ctx, "Mindmeld"))
            return new Result { Fate = Fate.Overcome, Message = "Interphasic Plasma Creatures: 2 SCIENCE or Mindmeld." };
        return Attach(ctx, PersistKind.None, 0,
            "Interphasic Plasma Creatures on table: each personnel STRENGTH −2 (sandbox).");
    }

    private static Result OutpostRaidAu(Ctx ctx)
    {
        // Printed: at your outpost — two killed (opp choice) unless STRENGTH>81; elsewhere wall STRENGTH>18.
        if (ctx.AtOwnOutpost)
        {
            if (Sum(ctx).str > 81)
                return new Result { Fate = Fate.Overcome, Message = "Outpost Raid at outpost: STRENGTH>81." };
            var r = new Result
            {
                Fate = Fate.EffectAndEnd,
                StopTeam = true,
                Message = "Outpost Raid at outpost: opponent kills two."
            };
            _tmp.Clear();
            PickKill(ctx, opp: true, "Outpost Raid: first victim.");
            PickKill(ctx, opp: true, "Outpost Raid: second victim.");
            r.Kill.AddRange(_tmp);
            _tmp.Clear();
            return r;
        }
        return Wall(ctx, Sum(ctx).str > 18, "STRENGTH>18 (not at your outpost)");
    }

    private static Result HunterGangsAu(Ctx ctx)
    {
        var kills = new List<Card>();
        var a = RandomOf(ctx, ctx.Team);
        var b = RandomOf(ctx, ctx.Team.Where(p => !ReferenceEquals(p, a)));
        foreach (var p in new[] { a, b })
        {
            if (p == null) continue;
            int cunn = Eff(ctx, p).Cunning;
            if (cunn % 2 != 0) AddKill(kills, p);
        }
        string msg = kills.Count == 0
            ? "Hunter Gangs: both escape (even CUNNING)."
            : $"Hunter Gangs: {kills.Count} killed (odd CUNNING).";
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = msg };
        r.Kill.AddRange(kills);
        return r;
    }

    private static Result GatherersAu(Ctx ctx)
    {
        if (ctx.Team.Any(p => (p.Name ?? "").Contains("Marouk", StringComparison.OrdinalIgnoreCase))
            || Sum(ctx).integ > 36)
            return new Result { Fate = Fate.Overcome, Message = "The Gatherers: Marouk or INTEGRITY>36." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "The Gatherers: discard Equipment/Artifacts present + one random hand card (UI applies equipment)."
        };
        foreach (var e in ctx.Present.Where(c =>
                     (c.Type ?? "").Contains("equipment", StringComparison.OrdinalIgnoreCase)
                     || (c.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase)))
            AddKill(r.Kill, e);
        return r;
    }

    private static Result ThoughtFireAu(Ctx ctx)
    {
        if (ctx.ThermalDeflectors)
            return new Result { Fate = Fate.Overcome, Message = "Thought Fire nullified (Thermal Deflectors)." };
        // Printed: only if Traveler is affecting you; then low CUNN+INT die unless Empathy.
        if (!ctx.TravelerAffecting)
            return new Result { Fate = Fate.Overcome, Message = "Thought Fire: Traveler not affecting you — no effect." };
        if (Skill(ctx, "Empathy"))
            return new Result { Fate = Fate.Overcome, Message = "Thought Fire: Empathy present." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Thought Fire: (CUNNING+INTEGRITY)<12 die." };
        foreach (var p in ctx.Team)
        {
            var e = Eff(ctx, p);
            if (e.Cunning + e.Integrity < 12) AddKill(r.Kill, p);
        }
        return r;
    }

    private static Result ParallelRomanceAu(Ctx ctx)
    {
        var m = RandomOf(ctx, ctx.Team.Where(IsMale));
        var f = RandomOf(ctx, ctx.Team.Where(IsFemale));
        if (m == null || f == null)
            return new Result { Fate = Fate.Overcome, Message = "Parallel Romance: no male+female — discarded." };
        return Attach(ctx, PersistKind.None, 3,
            $"Parallel Romance on {m.Name} & {f.Name}: stopped, STRENGTH −2 until countdown (sandbox).");
    }

    private static Result WorshiperAu(Ctx ctx)
    {
        // Skill counts as proxy for Greed vs Honor
        int greed = SkillCount(ctx, "Greed") + SkillCount(ctx, "Treachery");
        int honor = SkillCount(ctx, "Honor") + SkillCount(ctx, "Diplomacy");
        if (greed > honor)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = "Worshiper: Greed>Honor → +5 (sandbox)." };
        if (Skill(ctx, "Anthropology")
            || ctx.Present.Any(c => (c.Name ?? "").Contains("Edo", StringComparison.OrdinalIgnoreCase)))
            return new Result { Fate = Fate.Overcome, Message = "Worshiper: Anthropology / Edo Vessel." };
        return new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Worshiper: Away Team stopped." };
    }

    // ---- helpers ----

    private static readonly List<Card> _tmp = new();

    private static ModifierRules.EffectiveProfile Eff(Ctx ctx, Card p) =>
        ModifierRules.ResolvePersonnel(p, ctx.Present, ctx.AttemptingPlayer);

    private static (int integ, int cunn, int str) Sum(Ctx ctx)
    {
        int i = 0, c = 0, s = 0;
        foreach (var p in ctx.Team)
        {
            var e = Eff(ctx, p);
            i += e.Integrity; c += e.Cunning; s += e.Strength;
        }
        return (i, c, s);
    }

    private static int SkillCount(Ctx ctx, string name)
    {
        int have = 0;
        foreach (var p in ctx.Team)
        {
            foreach (var kv in Eff(ctx, p).Skills)
            {
                if (kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || kv.Key.Contains(name, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    have += kv.Value;
            }
        }
        return have;
    }

    private static bool Skill(Ctx c, string n) => SkillCount(c, n) >= 1;
    private static bool Skill(Ctx c, string n, int need) => SkillCount(c, n) >= need;

    public static bool IsFemale(Card p) =>
        (p.Characteristics ?? "").Contains("Female", StringComparison.OrdinalIgnoreCase);
    public static bool IsMale(Card p) =>
        (p.Characteristics ?? "").Contains("Male", StringComparison.OrdinalIgnoreCase);
    public static bool IsInorganic(Card p)
    {
        string ch = (p.Characteristics ?? "") + " " + (p.Type ?? "");
        return ch.Contains("Inorganic", StringComparison.OrdinalIgnoreCase)
               || ch.Contains("Hologram", StringComparison.OrdinalIgnoreCase)
               || ch.Contains("Android", StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsAndroid(Card p) =>
        (p.Type ?? "").Contains("android", StringComparison.OrdinalIgnoreCase)
        || (p.Characteristics ?? "").Contains("Android", StringComparison.OrdinalIgnoreCase);

    private static int TotalAttr(Ctx ctx, Card p)
    {
        var e = Eff(ctx, p);
        return e.Integrity + e.Cunning + e.Strength;
    }

    private static Result Wall(Ctx ctx, bool ok, string need) =>
        ok
            ? new Result { Fate = Fate.Overcome, Message = $"\"{ctx.Dilemma.Name}\" overcome ({need})." }
            : new Result { Fate = Fate.WallFailed, StopTeam = true, Message = $"Filter not met: {need}." };

    private static Card? RandomOf(Ctx ctx, IEnumerable<Card> set)
    {
        var list = set.ToList();
        if (list.Count == 0) return null;
        return list[ctx.Rng.Next(list.Count)];
    }

    private static void AddKill(List<Card> list, Card? c)
    {
        if (c != null && !list.Contains(c)) list.Add(c);
    }

    private static Result KillRandomEnd(Ctx ctx, string msg)
    {
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = msg };
        AddKill(r.Kill, RandomOf(ctx, ctx.Team));
        return r;
    }

    private static Card? KillRandom(Ctx ctx) => RandomOf(ctx, ctx.Team);

    private static Card? HighestAttr(Ctx ctx)
    {
        if (ctx.Team.Count == 0) return null;
        int max = ctx.Team.Max(p => TotalAttr(ctx, p));
        var tied = ctx.Team.Where(p => TotalAttr(ctx, p) == max).ToList();
        if (tied.Count == 1) return tied[0];
        // Spock Archer Soll: tie on highest attribute total = opponent chooses.
        return ctx.PickOpp?.Invoke("Archer: choose who dies (highest attribute tie)", tied)
               ?? tied[0];
    }

    private static Card? HighestFemale(Ctx ctx)
    {
        var females = ctx.Team.Where(IsFemale).ToList();
        if (females.Count == 0) return null;
        int max = females.Max(p => TotalAttr(ctx, p));
        var tied = females.Where(p => TotalAttr(ctx, p) == max).ToList();
        if (tied.Count == 1) return tied[0];
        // Same phrase as Archer ("highest total attributes") -> opponent chooses on tie.
        return ctx.PickOpp?.Invoke("Anaphasic Organism: choose which female is discarded (highest attribute tie)", tied)
               ?? tied[0];
    }

    private static Result UnlessThen(Ctx ctx, bool ok, Card? victim, string need, string failMsg, bool discardAlways)
    {
        if (ok)
            return new Result { Fate = Fate.Overcome, Message = $"Unless met ({need})." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = failMsg };
        AddKill(r.Kill, victim);
        return r;
    }

    private static Result UnlessScoreOr(Ctx ctx, bool ok, Action fail, int score, string need, bool useTmp = false)
    {
        if (ok)
            return new Result { Fate = Fate.Overcome, Score = score, Message = $"Unless met ({need}) → +{score}." };
        _tmp.Clear();
        fail();
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = $"Unless failed ({need})." };
        r.Kill.AddRange(_tmp);
        _tmp.Clear();
        return r;
    }

    private static Card? PickKill(Ctx ctx, bool opp, string prompt, bool exceptInorganic = false)
    {
        var pool = exceptInorganic ? ctx.Team.Where(p => !IsInorganic(p)).ToList() : ctx.Team.ToList();
        if (pool.Count == 0) return null;
        var pick = opp ? ctx.PickOpp?.Invoke(prompt, pool) : ctx.PickYou?.Invoke(prompt, pool);
        var v = pick ?? RandomOf(ctx, pool);
        AddKill(_tmp, v);
        return v;
    }

    private static Result Rebel(Ctx ctx)
    {
        bool str = Sum(ctx).str > 44;
        var eq = ctx.Present.Where(ModifierRules.IsEquipmentCard).ToList();
        bool smashEq = eq.Count > 0 && (ctx.Confirm?.Invoke("Rebel Encounter: destroy equipment instead of STRENGTH>44?") ?? false);
        if (str || smashEq)
        {
            var r = new Result { Fate = Fate.Overcome, Message = str ? "STRENGTH>44." : "Equipment destroyed." };
            if (smashEq && eq.Count > 0)
                r.Kill.Add(ctx.PickYou?.Invoke("Which equipment?", eq) ?? eq[0]);
            return r;
        }
        var x = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Rebel Encounter kills one at random." };
        AddKill(x.Kill, RandomOf(ctx, ctx.Team));
        return x;
    }

    // ---- El-Adrel Creature (Premiere 23 U) ----
    // Printed (PR): "Targets the two strongest members in Away Team (your choice if tie).
    // Unless they have STRENGTH>16, kills one of them (random selection). Discard dilemma."
    // Spock #10 Soll / DRG El-Adrel Creature:
    //   Two strongest AT (Tie = Dilemma-Owner picks via PickOpp).
    //   Pass: combined STR >16 -> Overcome Continue + discard (no points).
    //   Fail: 1 of the two random killed; rest of AT stopped; discard (EffectAndEnd+StopTeam).
    //   Boundary STRENGTH==16 fails. Empty AT: EffectAndEnd+Stop, no kill.

    /// <summary>Select up to two strongest Away Team members; dilemma-owner picks on STRENGTH ties.</summary>
    private static List<Card> TwoStrongestAwayTeam(Ctx ctx)
    {
        var remaining = ctx.Team.ToList();
        var selected = new List<Card>();
        while (selected.Count < 2 && remaining.Count > 0)
        {
            int max = remaining.Max(p => Eff(ctx, p).Strength);
            var tied = remaining.Where(p => Eff(ctx, p).Strength == max).ToList();
            int need = 2 - selected.Count;
            if (tied.Count <= need)
            {
                selected.AddRange(tied);
                foreach (var t in tied) remaining.Remove(t);
            }
            else
            {
                for (int i = 0; i < need; i++)
                {
                    var pick = ctx.PickOpp?.Invoke(
                                   "El-Adrel Creature: choose which tied strongest is targeted",
                                   tied)
                               ?? tied[0];
                    selected.Add(pick);
                    tied.Remove(pick);
                    remaining.Remove(pick);
                }
            }
        }
        return selected;
    }

    private static Result ElAdrel(Ctx ctx)
    {
        var two = TwoStrongestAwayTeam(ctx);
        int sum = two.Sum(p => Eff(ctx, p).Strength);
        if (sum > 16)
            return new Result
            {
                Fate = Fate.Overcome,
                Message = $"El-Adrel Creature: two strongest STRENGTH {sum} > 16."
            };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "El-Adrel Creature: one of the two strongest dies (random); Away Team stopped."
        };
        AddKill(r.Kill, RandomOf(ctx, two));
        return r;
    }

    /// <summary>DE mini-test for El-Adrel Creature. Returns null if OK, else failure reason.</summary>
    public static string? VerifyElAdrelCreature()
    {
        static Card P(string name, string str) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = "OFFICER",
            Text = "OFFICER",
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = str
        };

        static Ctx Make(
            Func<string, IReadOnlyList<Card>, Card?>? pickOpp,
            Random? rng = null,
            params Card[] team) => new()
            {
                Dilemma = new Card { Name = "El-Adrel Creature", Type = "Dilemma", MissionDilemmaType = "[P]" },
                Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
                Team = team,
                Present = team,
                AttemptingPlayer = 1,
                Rng = rng ?? new Random(1),
                PickOpp = pickOpp
            };

        var a9 = P("Alpha", "9");
        var b8 = P("Bravo", "8");
        var c7 = P("Charlie", "7");
        var d9 = P("Delta", "9");
        var e9 = P("Echo", "9");

        // Pass: 9+8=17 > 16 -> Overcome, no stop/kill, discard
        var pass = Resolve(Make(null, null, a9, b8, c7));
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.Kill.Count != 0 || pass.Score != 0)
            return $"pass 17: expected Overcome no stop/kill/score, got {pass.Fate}/stop={pass.StopTeam}/kills={pass.Kill.Count}/score={pass.Score}";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass 17: dilemma should discard";

        // Boundary fail: 8+8=16 not >16
        var eq8a = P("EqA", "8");
        var eq8b = P("EqB", "8");
        var failEq = Resolve(Make(null, new Random(42), eq8a, eq8b));
        if (failEq.Fate != Fate.EffectAndEnd || !failEq.StopTeam)
            return $"fail STR=16: expected EffectAndEnd+StopTeam, got {failEq.Fate}/stop={failEq.StopTeam}";
        if (failEq.Kill.Count != 1 || (failEq.Kill[0].Name is not ("EqA" or "EqB")))
            return $"fail STR=16: expected kill one of the two, got [{string.Join(",", failEq.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(failEq.Fate))
            return "fail STR=16: dilemma should discard";

        // Fail clear top2: 9+7 with weaker third; random kill among Alpha/Charlie only
        var failClear = Resolve(Make(null, new Random(7), a9, c7, P("Weak", "3")));
        if (failClear.Fate != Fate.EffectAndEnd || !failClear.StopTeam)
            return "fail clear: expected EffectAndEnd+StopTeam";
        if (failClear.Kill.Count != 1 || (failClear.Kill[0].Name is not ("Alpha" or "Charlie")))
            return $"fail clear: victim must be Alpha or Charlie, got [{string.Join(",", failClear.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(failClear.Fate))
            return "fail clear: dilemma should discard";

        // Tie for second slot: top unique 10 + two at 5 -> owner picks which 5 is targeted
        var top = P("Top", "10");
        var t5a = P("Tie5A", "5");
        var t5b = P("Tie5B", "5");
        Card? pickedSecond = null;
        var tieSecond = Resolve(Make((_, list) =>
        {
            pickedSecond = list.First(x => x.Name == "Tie5B");
            return pickedSecond;
        }, new Random(3), top, t5a, t5b));
        // 10+5=15 fail; kill among Top and Tie5B only
        if (tieSecond.Fate != Fate.EffectAndEnd || !tieSecond.StopTeam)
            return "tie-second: expected EffectAndEnd+StopTeam";
        if (pickedSecond?.Name != "Tie5B")
            return "tie-second: PickOpp was not used for second slot";
        if (tieSecond.Kill.Count != 1 || (tieSecond.Kill[0].Name is not ("Top" or "Tie5B")))
            return $"tie-second: victim must be Top or Tie5B, got [{string.Join(",", tieSecond.Kill.Select(k => k.Name))}]";
        if (tieSecond.Kill[0].Name == "Tie5A")
            return "tie-second: Tie5A must not be targeted";

        // Three-way tie at 6: owner picks two; 6+6=12 fail
        var x6 = P("X6", "6");
        var y6 = P("Y6", "6");
        var z6 = P("Z6", "6");
        var pickOrder = new Queue<string>(new[] { "Y6", "Z6" });
        var picks = new List<string>();
        var threeTie = Resolve(Make((_, list) =>
        {
            var name = pickOrder.Dequeue();
            picks.Add(name);
            return list.First(x => x.Name == name);
        }, new Random(11), x6, y6, z6));
        if (threeTie.Fate != Fate.EffectAndEnd || !threeTie.StopTeam)
            return "three-tie: expected EffectAndEnd+StopTeam";
        if (picks.Count != 2 || picks[0] != "Y6" || picks[1] != "Z6")
            return $"three-tie: expected PickOpp Y6 then Z6, got [{string.Join(",", picks)}]";
        if (threeTie.Kill.Count != 1 || (threeTie.Kill[0].Name is not ("Y6" or "Z6")))
            return $"three-tie: victim must be Y6 or Z6, got [{string.Join(",", threeTie.Kill.Select(k => k.Name))}]";
        if (threeTie.Kill[0].Name == "X6")
            return "three-tie: X6 must not be targeted";

        // Pass with three at 9: owner picks which two; 9+9>16 Overcome
        pickOrder = new Queue<string>(new[] { "Delta", "Echo" });
        picks.Clear();
        var passTie = Resolve(Make((_, list) =>
        {
            var name = pickOrder.Dequeue();
            picks.Add(name);
            return list.First(x => x.Name == name);
        }, null, a9, d9, e9));
        if (passTie.Fate != Fate.Overcome || passTie.StopTeam || passTie.Kill.Count != 0)
            return $"pass three-9: expected Overcome no stop/kill, got {passTie.Fate}/stop={passTie.StopTeam}";
        if (picks.Count != 2)
            return $"pass three-9: expected 2 PickOpp calls, got {picks.Count}";
        if (!ShouldRemoveFromSeed(passTie.Fate))
            return "pass three-9: dilemma should discard";

        // Empty Away Team fail: EffectAndEnd + Stop, no kill, discard
        var empty = Resolve(Make(null));
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndEnd+Stop no kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Solo STR 10 fail
        var solo = Resolve(Make(null, null, P("Solo", "10")));
        if (solo.Fate != Fate.EffectAndEnd || !solo.StopTeam)
            return "solo fail: expected EffectAndEnd+StopTeam";
        if (solo.Kill.Count != 1 || solo.Kill[0].Name != "Solo")
            return $"solo fail: expected kill Solo, got [{string.Join(",", solo.Kill.Select(k => k.Name))}]";

        // Solo STR 17 pass
        var soloPass = Resolve(Make(null, null, P("Tank", "17")));
        if (soloPass.Fate != Fate.Overcome || soloPass.StopTeam || soloPass.Kill.Count != 0)
            return $"solo pass: expected Overcome no stop/kill, got {soloPass.Fate}";

        return null;
    }


    // ---- Firestorm (Premiere 25 U) ----
    // Printed (PR): "Kills all Away Team members with INTEGRITY<5. Discard dilemma."
    // Spock #11 Soll / DRG Firestorm (TD/ETA != Conditions):
    //   Planet; NO Condition-Wall. INT after Enhancements (Eff) <5 die; Rest Continue;
    //   dilemma discard (EffectAndContinue). Boundary INT==5 survives.
    //   Thermal Deflectors in play -> nullify/discard + Continue (Overcome, no kills).
    //   PARK: ETA-Escape = Response (timing); UI thin — not wired here.

    private static Result Firestorm(Ctx ctx)
    {
        if (ctx.ThermalDeflectors)
            return new Result { Fate = Fate.Overcome, Message = "Firestorm nullified (Thermal Deflectors)." };
        var r = new Result
        {
            Fate = Fate.EffectAndContinue,
            StopTeam = false,
            Message = "Firestorm: Away Team members with INTEGRITY<5 are killed. Discard dilemma."
        };
        foreach (var p in ctx.Team.Where(p => Eff(ctx, p).Integrity < 5))
            AddKill(r.Kill, p);
        return r;
    }

    /// <summary>DE mini-test for Firestorm. Returns null if OK, else failure reason.</summary>
    public static string? VerifyFirestorm()
    {
        static Card P(string name, string integ) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = "OFFICER",
            Text = "OFFICER",
            Characteristics = "Human; Male;",
            IntegrityOrRange = integ,
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(bool thermal = false, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Firestorm", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1),
            ThermalDeflectors = thermal
        };

        var low3 = P("Low3", "3");
        var low4 = P("Low4", "4");
        var eq5 = P("Eq5", "5");
        var high7 = P("High7", "7");

        // Mixed: kill only INTEGRITY<5; survivors continue; discard
        var mixed = Resolve(Make(false, low3, low4, eq5, high7));
        if (mixed.Fate != Fate.EffectAndContinue || mixed.StopTeam)
            return $"mixed: expected EffectAndContinue no stop, got {mixed.Fate}/stop={mixed.StopTeam}";
        if (mixed.Kill.Count != 2
            || !mixed.Kill.Any(k => k.Name == "Low3")
            || !mixed.Kill.Any(k => k.Name == "Low4"))
            return $"mixed: expected kill Low3+Low4, got [{string.Join(",", mixed.Kill.Select(k => k.Name))}]";
        if (mixed.Kill.Any(k => k.Name is "Eq5" or "High7"))
            return "mixed: INTEGRITY>=5 must survive";
        if (!ShouldRemoveFromSeed(mixed.Fate))
            return "mixed: dilemma should discard";

        // Boundary INTEGRITY==5 alone: no kill, still EffectAndContinue + discard
        var boundary = Resolve(Make(false, eq5));
        if (boundary.Fate != Fate.EffectAndContinue || boundary.StopTeam || boundary.Kill.Count != 0)
            return $"boundary INT=5: expected EffectAndContinue no kill/stop, got {boundary.Fate}/kills={boundary.Kill.Count}/stop={boundary.StopTeam}";
        if (!ShouldRemoveFromSeed(boundary.Fate))
            return "boundary INT=5: dilemma should discard";

        // All low: all die; continue (no StopTeam); discard
        var allLow = Resolve(Make(false, low3, low4));
        if (allLow.Fate != Fate.EffectAndContinue || allLow.StopTeam)
            return $"allLow: expected EffectAndContinue no stop, got {allLow.Fate}/stop={allLow.StopTeam}";
        if (allLow.Kill.Count != 2)
            return $"allLow: expected 2 kills, got {allLow.Kill.Count}";
        if (!ShouldRemoveFromSeed(allLow.Fate))
            return "allLow: dilemma should discard";

        // Empty Away Team: no kill, discard + continue
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndContinue || empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndContinue no stop/kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Thermal Deflectors: nullify -> Overcome, no kills, discard
        var thermal = Resolve(Make(true, low3, eq5));
        if (thermal.Fate != Fate.Overcome || thermal.StopTeam || thermal.Kill.Count != 0)
            return $"thermal: expected Overcome no stop/kill, got {thermal.Fate}/stop={thermal.StopTeam}/kills={thermal.Kill.Count}";
        if (!ShouldRemoveFromSeed(thermal.Fate))
            return "thermal: dilemma should discard";

        return null;
    }

    // ---- Gravitic Mine (Premiere 26 U) ----
    // Printed (PR): "Unless SCIENCE and Navigation present, damages ship. Discard dilemma."
    // Spock #12 Soll / DRG Gravitic Mine:
    //   Space; Conditions SCIENCE AND Navigation.
    //   Pass -> Overcome (discard + Continue).
    //   Fail -> DamageShip (ApplyHullDamage +50 / Rotation badge) + Ship+Crew stopped
    //           (EffectAndEnd + StopTeam -> StopMissionAttemptTeam); dilemma discard.
    //   No bonus points.

    private static Result GraviticMine(Ctx ctx) =>
        SpaceUnless(ctx, Skill(ctx, "SCIENCE") && Skill(ctx, "Navigation"),
            damage: true, "SCIENCE and Navigation");

    /// <summary>DE mini-test for Gravitic Mine. Returns null if OK, else failure reason.</summary>
    public static string? VerifyGraviticMine()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Gravitic Mine", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var sci = P("Sci One", "SCIENCE", "SCIENCE");
        var nav = P("Nav One", "OFFICER", "Navigation");
        var both = P("SciNav", "SCIENCE", "SCIENCE Navigation");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Pass: SCIENCE + Navigation (two personnel) -> Overcome, Continue, no damage, discard
        var passSplit = Resolve(Make(sci, nav, civ));
        if (passSplit.Fate != Fate.Overcome || passSplit.StopTeam)
            return $"pass SCIENCE+Navigation: expected Overcome no stop, got {passSplit.Fate}/stop={passSplit.StopTeam}";
        if (passSplit.DamageShip || passSplit.DestroyShip)
            return "pass SCIENCE+Navigation: should not damage/destroy ship";
        if (passSplit.Score != 0)
            return "pass: no bonus points";
        if (!ShouldRemoveFromSeed(passSplit.Fate))
            return "pass SCIENCE+Navigation: dilemma should discard";

        // Pass: one personnel with both SCIENCE and Navigation
        var passOne = Resolve(Make(both));
        if (passOne.Fate != Fate.Overcome || passOne.StopTeam || passOne.DamageShip)
            return $"pass one SciNav: expected Overcome Continue no damage, got {passOne.Fate}/stop={passOne.StopTeam}/dmg={passOne.DamageShip}";
        if (!ShouldRemoveFromSeed(passOne.Fate))
            return "pass one SciNav: dilemma should discard";

        // Fail: SCIENCE only -> EffectAndEnd + Stop + DamageShip, discard
        var failSci = Resolve(Make(sci, civ));
        if (failSci.Fate != Fate.EffectAndEnd || !failSci.StopTeam || !failSci.DamageShip)
            return $"fail SCIENCE-only: expected EffectAndEnd+Stop+DamageShip, got {failSci.Fate}/stop={failSci.StopTeam}/dmg={failSci.DamageShip}";
        if (failSci.DestroyShip)
            return "fail SCIENCE-only: damage not destroy";
        if (!ShouldRemoveFromSeed(failSci.Fate))
            return "fail SCIENCE-only: dilemma should discard";

        // Fail: Navigation only
        var failNav = Resolve(Make(nav));
        if (failNav.Fate != Fate.EffectAndEnd || !failNav.StopTeam || !failNav.DamageShip)
            return $"fail Navigation-only: expected EffectAndEnd+Stop+DamageShip, got {failNav.Fate}/stop={failNav.StopTeam}/dmg={failNav.DamageShip}";
        if (!ShouldRemoveFromSeed(failNav.Fate))
            return "fail Navigation-only: dilemma should discard";

        // Fail: empty crew
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || !empty.DamageShip)
            return $"empty: expected EffectAndEnd+Stop+DamageShip, got {empty.Fate}/stop={empty.StopTeam}/dmg={empty.DamageShip}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Fail: civilian only
        var failCiv = Resolve(Make(civ));
        if (failCiv.Fate != Fate.EffectAndEnd || !failCiv.StopTeam || !failCiv.DamageShip)
            return $"fail civilian: expected EffectAndEnd+Stop+DamageShip, got {failCiv.Fate}/stop={failCiv.StopTeam}/dmg={failCiv.DamageShip}";
        if (!ShouldRemoveFromSeed(failCiv.Fate))
            return "fail civilian: dilemma should discard";

        return null;
    }


    // ---- Microbiotic Colony (Premiere 35 C) ----
    // Printed (PR): "Unless OFFICER, ENGINEER, and SCIENCE present, damages ship. Discard dilemma."
    // Spock #18 Soll / DRG Microbiotic Colony:
    //   Space [S]; Conditions SCIENCE AND ENGINEER AND OFFICER.
    //   Pass -> Overcome (discard + Continue).
    //   Fail -> Ship damaged (DamageShip / ApplyHullDamage +50 / Rotation badge)
    //           + Ship/Crew stopped (EffectAndEnd + StopTeam); dilemma always discarded.
    //   No bonus points.

    private static Result MicrobioticColony(Ctx ctx) =>
        SpaceUnless(ctx, Skill(ctx, "OFFICER") && Skill(ctx, "ENGINEER") && Skill(ctx, "SCIENCE"),
            damage: true, "OFFICER, ENGINEER and SCIENCE");

    /// <summary>DE mini-test for Microbiotic Colony. Returns null if OK, else failure reason.</summary>
    public static string? VerifyMicrobioticColony()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Microbiotic Colony", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var off = P("Officer One", "OFFICER", "OFFICER");
        var eng = P("Eng One", "ENGINEER", "ENGINEER");
        var sci = P("Sci One", "SCIENCE", "SCIENCE");
        var allThree = P("Multi", "OFFICER", "OFFICER ENGINEER SCIENCE");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Pass: OFFICER + ENGINEER + SCIENCE (three personnel) -> Overcome, Continue, no damage, discard
        var passSplit = Resolve(Make(off, eng, sci, civ));
        if (passSplit.Fate != Fate.Overcome || passSplit.StopTeam)
            return $"pass OFF+ENG+SCI: expected Overcome no stop, got {passSplit.Fate}/stop={passSplit.StopTeam}";
        if (passSplit.DamageShip || passSplit.DestroyShip)
            return "pass OFF+ENG+SCI: should not damage/destroy ship";
        if (passSplit.Score != 0)
            return "pass: no bonus points";
        if (!ShouldRemoveFromSeed(passSplit.Fate))
            return "pass OFF+ENG+SCI: dilemma should discard";

        // Pass: one personnel with all three skills
        var passOne = Resolve(Make(allThree));
        if (passOne.Fate != Fate.Overcome || passOne.StopTeam || passOne.DamageShip)
            return $"pass one Multi: expected Overcome Continue no damage, got {passOne.Fate}/stop={passOne.StopTeam}/dmg={passOne.DamageShip}";
        if (!ShouldRemoveFromSeed(passOne.Fate))
            return "pass one Multi: dilemma should discard";

        // Fail: missing SCIENCE (OFF+ENG only)
        var failNoSci = Resolve(Make(off, eng, civ));
        if (failNoSci.Fate != Fate.EffectAndEnd || !failNoSci.StopTeam || !failNoSci.DamageShip)
            return $"fail no SCIENCE: expected EffectAndEnd+Stop+DamageShip, got {failNoSci.Fate}/stop={failNoSci.StopTeam}/dmg={failNoSci.DamageShip}";
        if (failNoSci.DestroyShip)
            return "fail no SCIENCE: damage not destroy";
        if (!ShouldRemoveFromSeed(failNoSci.Fate))
            return "fail no SCIENCE: dilemma should discard";

        // Fail: missing ENGINEER (OFF+SCI only)
        var failNoEng = Resolve(Make(off, sci));
        if (failNoEng.Fate != Fate.EffectAndEnd || !failNoEng.StopTeam || !failNoEng.DamageShip)
            return $"fail no ENGINEER: expected EffectAndEnd+Stop+DamageShip, got {failNoEng.Fate}/stop={failNoEng.StopTeam}/dmg={failNoEng.DamageShip}";
        if (!ShouldRemoveFromSeed(failNoEng.Fate))
            return "fail no ENGINEER: dilemma should discard";

        // Fail: missing OFFICER (ENG+SCI only)
        var failNoOff = Resolve(Make(eng, sci));
        if (failNoOff.Fate != Fate.EffectAndEnd || !failNoOff.StopTeam || !failNoOff.DamageShip)
            return $"fail no OFFICER: expected EffectAndEnd+Stop+DamageShip, got {failNoOff.Fate}/stop={failNoOff.StopTeam}/dmg={failNoOff.DamageShip}";
        if (!ShouldRemoveFromSeed(failNoOff.Fate))
            return "fail no OFFICER: dilemma should discard";

        // Fail: empty crew
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || !empty.DamageShip)
            return $"empty: expected EffectAndEnd+Stop+DamageShip, got {empty.Fate}/stop={empty.StopTeam}/dmg={empty.DamageShip}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Fail: civilian only
        var failCiv = Resolve(Make(civ));
        if (failCiv.Fate != Fate.EffectAndEnd || !failCiv.StopTeam || !failCiv.DamageShip)
            return $"fail civilian: expected EffectAndEnd+Stop+DamageShip, got {failCiv.Fate}/stop={failCiv.StopTeam}/dmg={failCiv.DamageShip}";
        if (!ShouldRemoveFromSeed(failCiv.Fate))
            return "fail civilian: dilemma should discard";

        return null;
    }


    // ---- Microvirus (Premiere 36 C) ----
    // Printed (PR): "Unless MEDICAL and SECURITY present, kills one personnel present
    // (opponent's choice), except an inorganic. Otherwise, score points. Discard dilemma."
    // Spock #19 Soll / DRG Microvirus:
    //   Planet [P]; 5 Pts; Conditions MEDICAL AND SECURITY.
    //   Pass -> Overcome +5 Bonus-Area + Continue; dilemma discard.
    //   Fail -> Opp chooses 1 AT Kill (except inorganic) + AT stopped (EffectAndEnd+StopTeam);
    //           dilemma always discarded.
    //   DNA-related: Android / Exocomp (Inorganic) / Hologram not choosable (IsInorganic).
    //   PARK: opponent-choice UI filter thin (engine pool already excludes inorganic).

    private static Result Microvirus(Ctx ctx) =>
        UnlessScoreOr(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
            () => PickKill(ctx, opp: true, "Microvirus: opponent chooses (no inorganic).", exceptInorganic: true),
            5, "MEDICAL and SECURITY");

    /// <summary>DE mini-test for Microvirus. Returns null if OK, else failure reason.</summary>
    public static string? VerifyMicrovirus()
    {
        static Card P(string name, string cls, string text, string chars = "Human; Male;") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = chars,
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(Func<string, IReadOnlyList<Card>, Card?>? pickOpp, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Microvirus", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1),
            PickOpp = pickOpp
        };

        var med = P("Med One", "MEDICAL", "MEDICAL");
        var sec = P("Sec One", "SECURITY", "SECURITY");
        var both = P("Both", "MEDICAL", "MEDICAL SECURITY");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var android = P("Data", "OFFICER", "OFFICER", "Android; Male;");
        var holo = P("Holodoc", "MEDICAL", "MEDICAL", "Hologram; Male;");

        // Pass: MEDICAL + SECURITY (two personnel) -> Overcome +5, no stop, discard
        var passSplit = Resolve(Make(null, med, sec, civ));
        if (passSplit.Fate != Fate.Overcome || passSplit.StopTeam || passSplit.Score != 5)
            return $"pass MED+SEC: expected Overcome Score=5 no stop, got {passSplit.Fate}/{passSplit.Score}/stop={passSplit.StopTeam}";
        if (passSplit.Kill.Count != 0)
            return "pass MED+SEC: should not kill";
        if (!ShouldRemoveFromSeed(passSplit.Fate))
            return "pass MED+SEC: dilemma should discard";

        // Pass: one personnel with both skills
        var passOne = Resolve(Make(null, both));
        if (passOne.Fate != Fate.Overcome || passOne.Score != 5 || passOne.StopTeam)
            return $"pass one Both: expected Overcome +5 no stop, got {passOne.Fate}/{passOne.Score}/stop={passOne.StopTeam}";
        if (!ShouldRemoveFromSeed(passOne.Fate))
            return "pass one Both: dilemma should discard";

        // Fail: MEDICAL only (no SECURITY) -> opp chooses organic
        Card? picked = null;
        var failNoSec = Resolve(Make((_, list) => { picked = list.First(x => x.Name == "Civilian"); return picked; }, med, civ));
        if (failNoSec.Fate != Fate.EffectAndEnd || !failNoSec.StopTeam)
            return $"fail no SEC: expected EffectAndEnd+StopTeam, got {failNoSec.Fate}/stop={failNoSec.StopTeam}";
        if (failNoSec.Kill.Count != 1 || failNoSec.Kill[0].Name != "Civilian")
            return $"fail no SEC: expected kill Civilian, got [{string.Join(",", failNoSec.Kill.Select(k => k.Name))}]";
        if (picked?.Name != "Civilian")
            return "fail no SEC: PickOpp was not used";
        if (failNoSec.Score != 0)
            return "fail no SEC: should not score";
        if (!ShouldRemoveFromSeed(failNoSec.Fate))
            return "fail no SEC: dilemma should discard";

        // Fail: SECURITY only (no MEDICAL)
        picked = null;
        var failNoMed = Resolve(Make((_, list) => { picked = list.First(x => x.Name == "Sec One"); return picked; }, sec, civ));
        if (failNoMed.Fate != Fate.EffectAndEnd || !failNoMed.StopTeam)
            return $"fail no MED: expected EffectAndEnd+StopTeam, got {failNoMed.Fate}/stop={failNoMed.StopTeam}";
        if (failNoMed.Kill.Count != 1 || failNoMed.Kill[0].Name != "Sec One")
            return $"fail no MED: expected kill Sec One, got [{string.Join(",", failNoMed.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(failNoMed.Fate))
            return "fail no MED: dilemma should discard";

        // Fail: inorganic excluded from pool (android not choosable; organic killed)
        picked = null;
        var failInorg = Resolve(Make((_, list) =>
        {
            if (list.Any(x => x.Name == "Data"))
                throw new Exception("android should be excluded from Microvirus kill pool");
            picked = list.First(x => x.Name == "Civilian");
            return picked;
        }, civ, android));
        if (failInorg.Fate != Fate.EffectAndEnd || !failInorg.StopTeam)
            return $"fail inorganic-excl: expected EffectAndEnd+Stop, got {failInorg.Fate}/stop={failInorg.StopTeam}";
        if (failInorg.Kill.Count != 1 || failInorg.Kill[0].Name != "Civilian")
            return $"fail inorganic-excl: expected kill Civilian only, got [{string.Join(",", failInorg.Kill.Select(k => k.Name))}]";
        if (picked?.Name != "Civilian")
            return "fail inorganic-excl: PickOpp was not used";
        if (!ShouldRemoveFromSeed(failInorg.Fate))
            return "fail inorganic-excl: dilemma should discard";

        // Fail: only inorganic present -> EffectAndEnd+Stop, no kill, discard
        var onlyAndroid = Resolve(Make((_, list) => list[0], android));
        if (onlyAndroid.Fate != Fate.EffectAndEnd || !onlyAndroid.StopTeam || onlyAndroid.Kill.Count != 0)
            return $"only android: expected EffectAndEnd+Stop no kill, got {onlyAndroid.Fate}/stop={onlyAndroid.StopTeam}/kills={onlyAndroid.Kill.Count}";
        if (!ShouldRemoveFromSeed(onlyAndroid.Fate))
            return "only android: dilemma should discard";

        // Fail: hologram alone (inorganic) -> no kill
        var onlyHolo = Resolve(Make(null, holo));
        if (onlyHolo.Fate != Fate.EffectAndEnd || !onlyHolo.StopTeam || onlyHolo.Kill.Count != 0)
            return $"only holo: expected EffectAndEnd+Stop no kill, got {onlyHolo.Fate}/stop={onlyHolo.StopTeam}/kills={onlyHolo.Kill.Count}";
        if (!ShouldRemoveFromSeed(onlyHolo.Fate))
            return "only holo: dilemma should discard";

        // Fail: Exocomp (Inorganic) alone -> no kill
        var exo = P("Exocomp", "ENGINEER", "ENGINEER", "Exocomp; Inorganic;");
        var onlyExo = Resolve(Make(null, exo));
        if (onlyExo.Fate != Fate.EffectAndEnd || !onlyExo.StopTeam || onlyExo.Kill.Count != 0)
            return $"only exocomp: expected EffectAndEnd+Stop no kill, got {onlyExo.Fate}/stop={onlyExo.StopTeam}/kills={onlyExo.Kill.Count}";
        if (!ShouldRemoveFromSeed(onlyExo.Fate))
            return "only exocomp: dilemma should discard";

        // Empty Away Team fail
        var empty = Resolve(Make(null));
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndEnd+Stop no kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        return null;
    }

    // ---- Nagilum (Premiere 37 R) ----
    // Printed (PR): "Unless 3 Diplomacy OR STRENGTH>40 present, kills half of crew
    // (random selection, round down). Otherwise, score points. Discard dilemma."
    // Points: 5. [S] Space.
    // Spock #20 Soll / DRG Nagilum:
    //   Pass (3 Diplomacy OR STR>40) -> Overcome +5 Bonus-Area + Continue; discard.
    //   Fail -> half of Crew random kill (round down; 1->0) + Ship/Crew stopped
    //           (EffectAndEnd+StopTeam); dilemma always discarded.
    //   STRENGTH sum via Sum(ctx).str. Boundary STRENGTH==40 fails.
    //   Combo Anaphasic&Nagilum = EP (not Premiere) -- ignore.
    // Decide: DilemmaRules.Nagilum + VerifyNagilum.

    private static Result Nagilum(Ctx ctx) =>
        UnlessScoreOr(ctx,
            Skill(ctx, "Diplomacy", 3) || Sum(ctx).str > 40,
            () =>
            {
                int n = ctx.Team.Count / 2; // round down
                var pool = ctx.Team.ToList();
                for (int i = 0; i < n && pool.Count > 0; i++)
                {
                    var v = RandomOf(ctx, pool);
                    if (v == null) break;
                    AddKill(_tmp, v);
                    pool.Remove(v);
                }
            },
            5, "3 Diplomacy or STRENGTH>40");

    /// <summary>DE mini-test for Nagilum. Returns null if OK, else failure reason.</summary>
    public static string? VerifyNagilum()
    {
        static Card P(string name, string cls, string text, string str = "5") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = str
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Nagilum", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var dip1 = P("Dip One", "VIP", "Diplomacy");
        var dip2 = P("Dip Two", "VIP", "Diplomacy");
        var dip3 = P("Dip Three", "VIP", "Diplomacy");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN", "8");
        var tankA = P("Tank A", "OFFICER", "OFFICER", "10");
        var tankB = P("Tank B", "OFFICER", "OFFICER", "10");
        var tankC = P("Tank C", "OFFICER", "OFFICER", "10");
        var tankD = P("Tank D", "OFFICER", "OFFICER", "11"); // 10+10+10+11 = 41 > 40
        var weak1 = P("Weak One", "CIVILIAN", "CIVILIAN", "4");
        var weak2 = P("Weak Two", "CIVILIAN", "CIVILIAN", "4");
        var weak3 = P("Weak Three", "CIVILIAN", "CIVILIAN", "4");
        var weak4 = P("Weak Four", "CIVILIAN", "CIVILIAN", "4");
        var weak5 = P("Weak Five", "CIVILIAN", "CIVILIAN", "4");

        // Pass: 3 Diplomacy -> Overcome +5, no stop, discard
        var passDip = Resolve(Make(dip1, dip2, dip3));
        if (passDip.Fate != Fate.Overcome || passDip.StopTeam || passDip.Score != 5)
            return $"pass 3 Diplomacy: expected Overcome Score=5 no stop, got {passDip.Fate}/{passDip.Score}/stop={passDip.StopTeam}";
        if (passDip.Kill.Count != 0)
            return "pass 3 Diplomacy: should not kill";
        if (!ShouldRemoveFromSeed(passDip.Fate))
            return "pass 3 Diplomacy: dilemma should discard";

        // Pass: STRENGTH>40 without 3 Diplomacy
        var passStr = Resolve(Make(tankA, tankB, tankC, tankD));
        if (passStr.Fate != Fate.Overcome || passStr.Score != 5 || passStr.StopTeam)
            return $"pass STRENGTH>40: expected Overcome +5 no stop, got {passStr.Fate}/{passStr.Score}/stop={passStr.StopTeam}";
        if (passStr.Kill.Count != 0)
            return "pass STRENGTH>40: should not kill";
        if (!ShouldRemoveFromSeed(passStr.Fate))
            return "pass STRENGTH>40: dilemma should discard";

        // Fail boundary: STRENGTH==40 and only 2 Diplomacy -> half of 5 = 2 kills
        var eq40 = new[] { dip1, dip2, tankA, tankB, tankC }; // 5+5+10+10+10=40, 2 Diplomacy
        var failEq = Resolve(Make(eq40));
        if (failEq.Fate != Fate.EffectAndEnd || !failEq.StopTeam)
            return $"fail STR=40 + 2 Dip: expected EffectAndEnd+StopTeam, got {failEq.Fate}/stop={failEq.StopTeam}";
        if (failEq.Kill.Count != 2)
            return $"fail STR=40: expected 2 kills (half of 5), got {failEq.Kill.Count}";
        if (failEq.Kill.Distinct().Count() != 2)
            return "fail STR=40: kills must be distinct";
        if (failEq.Kill.Any(k => !eq40.Contains(k)))
            return "fail STR=40: kill not from team";
        if (failEq.Score != 0)
            return "fail STR=40: should not score";
        if (!ShouldRemoveFromSeed(failEq.Fate))
            return "fail STR=40: dilemma should discard";

        // Fail: 4 weak crew -> 2 random kills
        var four = new[] { weak1, weak2, weak3, weak4 };
        var fail4 = Resolve(Make(four));
        if (fail4.Fate != Fate.EffectAndEnd || !fail4.StopTeam)
            return $"fail 4: expected EffectAndEnd+StopTeam, got {fail4.Fate}/stop={fail4.StopTeam}";
        if (fail4.Kill.Count != 2)
            return $"fail 4: expected 2 kills, got {fail4.Kill.Count}";
        if (fail4.Kill.Distinct().Count() != 2 || fail4.Kill.Any(k => !four.Contains(k)))
            return "fail 4: kills must be 2 distinct team members";
        if (!ShouldRemoveFromSeed(fail4.Fate))
            return "fail 4: dilemma should discard";

        // Fail: 1 crew -> half round down = 0 kills, still EffectAndEnd+Stop, discard
        var solo = Resolve(Make(civ));
        if (solo.Fate != Fate.EffectAndEnd || !solo.StopTeam || solo.Kill.Count != 0)
            return $"solo: expected EffectAndEnd+Stop no kill, got {solo.Fate}/stop={solo.StopTeam}/kills={solo.Kill.Count}";
        if (!ShouldRemoveFromSeed(solo.Fate))
            return "solo: dilemma should discard";

        // Empty crew fail: EffectAndEnd+Stop, no kill, discard
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndEnd+Stop no kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Fail: 5 weak -> 2 kills (round down)
        var five = new[] { weak1, weak2, weak3, weak4, weak5 };
        var fail5 = Resolve(Make(five));
        if (fail5.Fate != Fate.EffectAndEnd || !fail5.StopTeam || fail5.Kill.Count != 2)
            return $"fail 5: expected EffectAndEnd+Stop 2 kills, got {fail5.Fate}/stop={fail5.StopTeam}/kills={fail5.Kill.Count}";
        if (!ShouldRemoveFromSeed(fail5.Fate))
            return "fail 5: dilemma should discard";

        return null;
    }

    // ---- Nanites (Premiere 38 U) ----
    // Printed (PR): "Unless 2 SCIENCE OR Diplomacy present, damages ship. Otherwise, score points. Discard dilemma."
    // Points: 5. [S] Space.
    // Card+DRG / Premiere dilemma pattern (sister: Gravitic Mine damage / CosmicString score):
    //   Pass (2 SCIENCE OR Diplomacy) -> Overcome +5 Bonus-Area + Continue; discard.
    //   Fail -> DamageShip (ApplyHullDamage +50 / Rotation badge) + Ship/Crew stopped
    //           (EffectAndEnd+StopTeam); dilemma always discarded.
    // Decide: DilemmaRules.Nanites + VerifyNanites.

    private static Result Nanites(Ctx ctx) =>
        SpaceUnlessScore(ctx, Skill(ctx, "SCIENCE", 2) || Skill(ctx, "Diplomacy"),
            damage: true, score: 5, need: "2 SCIENCE or Diplomacy");

    /// <summary>DE mini-test for Nanites. Returns null if OK, else failure reason.</summary>
    public static string? VerifyNanites()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Nanites", Type = "Dilemma", MissionDilemmaType = "[S]", Points = "5" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var sci1 = P("Sci One", "SCIENCE", "SCIENCE");
        var sci2 = P("Sci Two", "SCIENCE", "SCIENCE");
        var dip = P("Diplomat", "VIP", "Diplomacy");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var nav = P("Navigator", "OFFICER", "Navigation");

        // Pass: 2 SCIENCE -> Overcome +5, Continue, no damage, discard
        var passSci = Resolve(Make(sci1, sci2, civ));
        if (passSci.Fate != Fate.Overcome || passSci.StopTeam || passSci.Score != 5)
            return $"pass 2 SCIENCE: expected Overcome Score=5 no stop, got {passSci.Fate}/{passSci.Score}/stop={passSci.StopTeam}";
        if (passSci.DamageShip || passSci.DestroyShip)
            return "pass 2 SCIENCE: should not damage/destroy ship";
        if (!ShouldRemoveFromSeed(passSci.Fate))
            return "pass 2 SCIENCE: dilemma should discard";

        // Pass: Diplomacy alone (no SCIENCE) -> Overcome +5
        var passDip = Resolve(Make(dip, civ));
        if (passDip.Fate != Fate.Overcome || passDip.Score != 5 || passDip.StopTeam || passDip.DamageShip)
            return $"pass Diplomacy: expected Overcome +5 Continue no damage, got {passDip.Fate}/{passDip.Score}/stop={passDip.StopTeam}/dmg={passDip.DamageShip}";
        if (!ShouldRemoveFromSeed(passDip.Fate))
            return "pass Diplomacy: dilemma should discard";

        // Fail boundary: 1 SCIENCE only (no Diplomacy) -> DamageShip + Stop, no score, discard
        var failOneSci = Resolve(Make(sci1, civ));
        if (failOneSci.Fate != Fate.EffectAndEnd || !failOneSci.StopTeam || !failOneSci.DamageShip)
            return $"fail 1 SCIENCE: expected EffectAndEnd+Stop+DamageShip, got {failOneSci.Fate}/stop={failOneSci.StopTeam}/dmg={failOneSci.DamageShip}";
        if (failOneSci.DestroyShip || failOneSci.Score != 0)
            return "fail 1 SCIENCE: damage not destroy; no score";
        if (!ShouldRemoveFromSeed(failOneSci.Fate))
            return "fail 1 SCIENCE: dilemma should discard";

        // Fail: Navigation/civilian only
        var failNav = Resolve(Make(nav, civ));
        if (failNav.Fate != Fate.EffectAndEnd || !failNav.StopTeam || !failNav.DamageShip)
            return $"fail no-match: expected EffectAndEnd+Stop+DamageShip, got {failNav.Fate}/stop={failNav.StopTeam}/dmg={failNav.DamageShip}";
        if (!ShouldRemoveFromSeed(failNav.Fate))
            return "fail no-match: dilemma should discard";

        // Fail: empty crew
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || !empty.DamageShip)
            return $"empty: expected EffectAndEnd+Stop+DamageShip, got {empty.Fate}/stop={empty.StopTeam}/dmg={empty.DamageShip}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Fail: civilian only
        var failCiv = Resolve(Make(civ));
        if (failCiv.Fate != Fate.EffectAndEnd || !failCiv.StopTeam || !failCiv.DamageShip)
            return $"fail civilian: expected EffectAndEnd+Stop+DamageShip, got {failCiv.Fate}/stop={failCiv.StopTeam}/dmg={failCiv.DamageShip}";
        if (!ShouldRemoveFromSeed(failCiv.Fate))
            return "fail civilian: dilemma should discard";

        return null;
    }

    // ---- Nausicaans (Premiere 39 U) ----
    // Printed (PR): "Unless STRENGTH>44, kills one personnel (random selection). Discard dilemma."
    // [P] Planet. Icons [IPG].
    // Spock #22 Soll / DRG Nausicaans:
    //   Pass (STRENGTH>44) -> Overcome + Continue; discard.
    //   Fail -> kill 1 Away Team (random) + AT stopped (EffectAndEnd+StopTeam); dilemma always discarded.
    //   Boundary STRENGTH==44 fails.
    // PARK: nullify via Interphase Generator / Zon (artifact/personnel nullify) - not wired here.
    // Card+DRG Decide: DilemmaRules.Nausicaans + VerifyNausicaans.

    private static Result Nausicaans(Ctx ctx) =>
        UnlessThen(ctx, Sum(ctx).str > 44, KillRandom(ctx),
            "STRENGTH>44", "Nausicaans kill one at random.", discardAlways: true);

    /// <summary>DE mini-test for Nausicaans. Returns null if OK, else failure reason.</summary>
    public static string? VerifyNausicaans()
    {
        static Card P(string name, string str) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = "OFFICER",
            Text = "OFFICER",
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = str
        };

        static Ctx Make(Random? rng = null, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Nausicaans", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = rng ?? new Random(1)
        };

        var a9 = P("Alpha", "9");
        var b9 = P("Bravo", "9");
        var c9 = P("Charlie", "9");
        var d9 = P("Delta", "9");
        var e9 = P("Echo", "9"); // 9*5 = 45 > 44

        // Pass: STRENGTH 45 > 44 -> Overcome, Continue, no kill, discard
        var pass = Resolve(Make(null, a9, b9, c9, d9, e9));
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.Kill.Count != 0 || pass.Score != 0)
            return $"pass STR>44: expected Overcome no stop/kill/score, got {pass.Fate}/stop={pass.StopTeam}/kills={pass.Kill.Count}/score={pass.Score}";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass STR>44: dilemma should discard";

        // Boundary fail: STRENGTH==44 (11*4) -> not overcome
        var t11a = P("TankA", "11");
        var t11b = P("TankB", "11");
        var t11c = P("TankC", "11");
        var t11d = P("TankD", "11");
        var failEq = Resolve(Make(new Random(42), t11a, t11b, t11c, t11d));
        if (failEq.Fate != Fate.EffectAndEnd || !failEq.StopTeam)
            return $"fail STR=44: expected EffectAndEnd+StopTeam, got {failEq.Fate}/stop={failEq.StopTeam}";
        if (failEq.Kill.Count != 1)
            return $"fail STR=44: expected exactly 1 kill, got {failEq.Kill.Count}";
        if (failEq.Kill[0].Name is not ("TankA" or "TankB" or "TankC" or "TankD"))
            return $"fail STR=44: victim not from team, got {failEq.Kill[0].Name}";
        if (failEq.Score != 0)
            return "fail STR=44: should not score";
        if (!ShouldRemoveFromSeed(failEq.Fate))
            return "fail STR=44: dilemma should discard";

        // Fail clear: weak team -> 1 random kill + Stop + discard
        var w3 = P("Weak", "3");
        var w4 = P("Softer", "4");
        var failWeak = Resolve(Make(new Random(7), w3, w4));
        if (failWeak.Fate != Fate.EffectAndEnd || !failWeak.StopTeam)
            return $"fail weak: expected EffectAndEnd+StopTeam, got {failWeak.Fate}/stop={failWeak.StopTeam}";
        if (failWeak.Kill.Count != 1 || (failWeak.Kill[0].Name is not ("Weak" or "Softer")))
            return $"fail weak: expected kill Weak or Softer, got [{string.Join(",", failWeak.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(failWeak.Fate))
            return "fail weak: dilemma should discard";

        // Solo fail: only one personnel -> that one dies + Stop + discard
        var solo = Resolve(Make(new Random(3), P("Lone", "5")));
        if (solo.Fate != Fate.EffectAndEnd || !solo.StopTeam)
            return $"solo: expected EffectAndEnd+StopTeam, got {solo.Fate}/stop={solo.StopTeam}";
        if (solo.Kill.Count != 1 || solo.Kill[0].Name != "Lone")
            return $"solo: expected kill Lone, got [{string.Join(",", solo.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(solo.Fate))
            return "solo: dilemma should discard";

        // Empty Away Team: EffectAndEnd + Stop, no kill, discard
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndEnd+Stop no kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        return null;
    }

    // ---- Null Space (Premiere 41 U) ----
    // Printed (PR): "Unless 2 Navigation present, damages ship. Otherwise, score points. Discard dilemma."
    // Points: 5. [S] Space.
    // Spock #23 Soll / DRG Null Space:
    //   Pass (2 Navigation) -> Overcome +5 Bonus-Area + Continue; discard.
    //   Fail -> DamageShip (ApplyHullDamage +50 / Rotation badge) + Ship/Crew stopped
    //           (EffectAndEnd+StopTeam); dilemma always discarded.
    // Boundary: 1 Navigation fails. Decide: DilemmaRules.NullSpace + VerifyNullSpace.

    private static Result NullSpace(Ctx ctx) =>
        SpaceUnlessScore(ctx, Skill(ctx, "Navigation", 2),
            damage: true, score: 5, need: "2 Navigation");

    /// <summary>DE mini-test for Null Space. Returns null if OK, else failure reason.</summary>
    public static string? VerifyNullSpace()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Null Space", Type = "Dilemma", MissionDilemmaType = "[S]", Points = "5" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var nav1 = P("Nav One", "OFFICER", "Navigation");
        var nav2 = P("Nav Two", "OFFICER", "Navigation");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var sci = P("Scientist", "SCIENCE", "SCIENCE");

        // Pass: 2 Navigation -> Overcome +5, Continue, no damage, discard
        var passNav = Resolve(Make(nav1, nav2, civ));
        if (passNav.Fate != Fate.Overcome || passNav.StopTeam || passNav.Score != 5)
            return $"pass 2 Navigation: expected Overcome Score=5 no stop, got {passNav.Fate}/{passNav.Score}/stop={passNav.StopTeam}";
        if (passNav.DamageShip || passNav.DestroyShip)
            return "pass 2 Navigation: should not damage/destroy ship";
        if (!ShouldRemoveFromSeed(passNav.Fate))
            return "pass 2 Navigation: dilemma should discard";

        // Fail boundary: 1 Navigation only -> DamageShip + Stop, no score, discard
        var failOne = Resolve(Make(nav1, civ));
        if (failOne.Fate != Fate.EffectAndEnd || !failOne.StopTeam || !failOne.DamageShip)
            return $"fail 1 Navigation: expected EffectAndEnd+Stop+DamageShip, got {failOne.Fate}/stop={failOne.StopTeam}/dmg={failOne.DamageShip}";
        if (failOne.DestroyShip || failOne.Score != 0)
            return "fail 1 Navigation: damage not destroy; no score";
        if (!ShouldRemoveFromSeed(failOne.Fate))
            return "fail 1 Navigation: dilemma should discard";

        // Fail: SCIENCE/civilian only (no Navigation)
        var failSci = Resolve(Make(sci, civ));
        if (failSci.Fate != Fate.EffectAndEnd || !failSci.StopTeam || !failSci.DamageShip)
            return $"fail no-nav: expected EffectAndEnd+Stop+DamageShip, got {failSci.Fate}/stop={failSci.StopTeam}/dmg={failSci.DamageShip}";
        if (!ShouldRemoveFromSeed(failSci.Fate))
            return "fail no-nav: dilemma should discard";

        // Fail: empty crew
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || !empty.DamageShip)
            return $"empty: expected EffectAndEnd+Stop+DamageShip, got {empty.Fate}/stop={empty.StopTeam}/dmg={empty.DamageShip}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Fail: civilian only
        var failCiv = Resolve(Make(civ));
        if (failCiv.Fate != Fate.EffectAndEnd || !failCiv.StopTeam || !failCiv.DamageShip)
            return $"fail civilian: expected EffectAndEnd+Stop+DamageShip, got {failCiv.Fate}/stop={failCiv.StopTeam}/dmg={failCiv.DamageShip}";
        if (!ShouldRemoveFromSeed(failCiv.Fate))
            return "fail civilian: dilemma should discard";

        return null;
    }

    // ---- Cytherians (Premiere 22 R) ----
    // Printed (PR): "Attempt ends. Target location at this spaceline's far end.
    // Place on ship; it must do nothing but move towards there. Discard when reached (score points)."
    // Points: 15. [S] Space.
    // Spock #9 Soll / Glossary Cytherians + actions-required:
    //   Place on ship; Attempt ends; Crew NOT stopped (StopTeam=false).
    //   Required action: ship+crew may ONLY move toward far spaceline end (full RANGE/turn).
    //   Arrival -> discard +15. Ship destroy -> discard (no points). No instant relocate.
    //   Far-end fixed once (TW Dest). Borg play-out: no points (PARK if unclear later).
    // Card+DRG Decide: AttachAndEnd + Persist Cytherians + countdown 0 + StopTeam false;
    // Score=0 at encounter (TW awards +15 on arrival). Host ShipOrMission.
    // Apply already: Dest=ResolveFarEndMission; required-move + arrival +15.
    // PARK: full LegalMoves "only move toward far end" lock beyond existing
    // ShipHasRequiredMove gates (cloak/beam-off/initiate-battle) — no half-guess.

    private static Result Cytherians(Ctx ctx) =>
        new()
        {
            Fate = Fate.AttachAndEnd,
            Persist = PersistKind.Cytherians,
            Countdown = 0,
            StopTeam = false, // Spock #9: attempt ends, crew NOT stopped
            Message = "Cytherians: attempt ends (crew not stopped). Place on ship; must move to far end. Discard when reached (+15)."
        };

    // ---- Crystalline Entity (Premiere 21 R) ----
    // Printed (PR): "[P]: Unless MEDICAL and SCIENCE, kills Away Team.
    // [S]: Unless Music OR SHIELDS>6, kills all personnel on ship.
    // Then: Discard dilemma. Score points if overcome."
    // Spock #8 Soll / DRG / Glossary: Dual [S/P].
    // Planet pass SCIENCE+MEDICAL -> Overcome +5 Continue (discard).
    // Planet fail -> entire AT killed, EffectAndEnd+StopTeam, discard.
    // Space pass Music OR SHIELDS>6 -> Overcome +5 Continue (discard).
    // Space fail -> ALL life aboard dies (Stopped/Disabled/Intruder; NOT Stasis)
    // via KillAllLifeAboardExceptStasis (Apply beyond encounter crew); Ship stopped
    // (StopTeam); discard. Does NOT destroy the ship.
    // PARK: Lore-Double interaction (later / Lore not in this dilemma scope).

    private static Result Crystalline(Ctx ctx)
    {
        bool planet = MissionRules.IsPlanetMission(ctx.Mission);
        bool ok = planet
            ? Skill(ctx, "SCIENCE") && Skill(ctx, "MEDICAL")
            : Skill(ctx, "Music") || ctx.ShipShields > 6;
        if (ok)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = "Crystalline Entity overcome -> +5." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true, // space: ship stopped via StopMissionAttemptTeam
            KillAllLifeAboardExceptStasis = !planet,
            Message = planet
                ? "Crystalline Entity: Away Team killed (need SCIENCE and MEDICAL)."
                : "Crystalline Entity: all life aboard killed (need Music or SHIELDS>6); ship stopped."
        };
        r.Kill.AddRange(ctx.Team); // planet: entire AT; space: encounter crew seed (Apply expands)
        return r;
    }


    private static Result SpaceUnless(Ctx ctx, bool ok, bool damage, string need, bool destroy = false)
    {
        if (ok) return new Result { Fate = Fate.Overcome, Message = $"Unless met ({need})." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            DamageShip = damage,
            DestroyShip = destroy,
            Message = destroy ? $"Ship destroyed ({need} missing)." : $"Ship damaged ({need} missing)."
        };
    }

    private static Result SpaceUnlessScore(Ctx ctx, bool ok, string need, bool damage = false, bool destroy = false, int score = 0)
    {
        if (ok) return new Result { Fate = Fate.Overcome, Score = score, Message = $"Unless met ({need}) → +{score}." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            DamageShip = damage,
            DestroyShip = destroy,
            Message = destroy ? "Ship destroyed." : "Ship damaged."
        };
    }

    private static Result Attach(Ctx ctx, PersistKind kind, int cd, string msg) =>
        new() { Fate = Fate.AttachAndEnd, Persist = kind, Countdown = cd, StopTeam = true, Message = msg };
    private static Result AttachContinue(Ctx ctx, PersistKind kind, int cd, string msg) =>
        new() { Fate = Fate.AttachAndContinue, Persist = kind, Countdown = cd, StopTeam = false, Message = msg };

    private static Result NitriumEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.Nitrium, ctx.Present, ctx.AttemptingPlayer))
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = "Nitrium cured (2 SCIENCE or 2 ENGINEER) - discarded; attempt continues."
            };
        return AttachContinue(ctx, PersistKind.Nitrium, 2,
            "Nitrium on ship (countdown 2). Cure: 2 SCIENCE or 2 ENGINEER. Attempt continues.");
    }

    private static Result HyperAgingEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.HyperAging, ctx.Present, ctx.AttemptingPlayer))
            return new Result
            {
                Fate = Fate.Overcome,
                Score = 5,
                StopTeam = false,
                Message = "Hyper-Aging cured (SCIENCE + 2 MEDICAL) - +5; discarded; attempt continues."
            };
        return AttachContinue(ctx, PersistKind.HyperAging, 3,
            "Hyper-Aging (quarantine, countdown 3): no leave/beam away; joiners quarantined. Cure: SCIENCE + 2 MEDICAL. Attempt continues (not stopped).");
    }

    private static Result RemFatigueEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.RemFatigue, ctx.Present, ctx.AttemptingPlayer))
            return new Result
            {
                Fate = Fate.Overcome,
                Score = 5,
                StopTeam = false,
                Message = "REM Fatigue cured (3 MEDICAL) - +5; discarded; attempt continues."
            };
        return AttachContinue(ctx, PersistKind.RemFatigue, 4,
            "REM Fatigue (quarantine, countdown 4). Cure: 3 MEDICAL or dock (score points). Attempt continues.");
    }

    // ---- Menthar Booby Trap (Premiere 34 C) ----
    // Printed (PR): "Place on ship; it cannot move. Unless MEDICAL present, one crew member killed (random selection). Cure with 2 ENGINEER."
    // Space [S]: ALWAYS place on ship (Persist Menthar, countdown 0). No Move until Cure (2 ENGINEER).
    // MEDICAL missing -> 1 crew random kill + AttachAndEnd + StopTeam (ship/crew stopped).
    // MEDICAL present -> no kill + AttachAndContinue + StopTeam=false (still placed; attempt continues).
    // Cure/discard later: CanCure Menthar = 2 ENGINEER (after initial effect; no encounter Overcome).
    // Spock #17 Soll / DRG + Glossary Errata Menthar Booby Trap.
    // PARK: LegalMoves-level move-block beyond existing TW Menthar/TwoDim gate if thin.

    private static Result Tsiolkovsky(Ctx ctx)
    {
        if (CanCure(PersistKind.Tsiolkovsky, ctx.Present, ctx.AttemptingPlayer))
            return new Result { Fate = Fate.Overcome, StopTeam = false, Message = "Tsiolkovsky Infection cured (3 MEDICAL). Discard dilemma." };
        return AttachContinue(ctx, PersistKind.Tsiolkovsky, 0,
            "Tsiolkovsky: personnel lose first-listed skill. Cure: 3 MEDICAL. Attempt continues.");
    }

    private static Result TwoDim(Ctx ctx)
    {
        if (CanCure(PersistKind.TwoDim, ctx.Present, ctx.AttemptingPlayer))
            return new Result { Fate = Fate.Overcome, StopTeam = false, Message = "Two-Dimensional Creatures cured (ENGINEER + SCIENCE). Discard dilemma." };
        return AttachContinue(ctx, PersistKind.TwoDim, 0,
            "2D Creatures: Empathy disabled, ship cannot move. Cure: ENGINEER + SCIENCE. Attempt continues.");
    }

    private static Result Menthar(Ctx ctx)
    {
        if (Skill(ctx, "MEDICAL"))
        {
            // Rulebook 7.2.2.3: Conditions first (MEDICAL present -> no kill, continue), then check cure!
            if (CanCure(PersistKind.Menthar, ctx.Present, ctx.AttemptingPlayer))
                return new Result { Fate = Fate.Overcome, StopTeam = false, Message = "Menthar Booby Trap cured (2 ENGINEER). Discard dilemma." };
            return AttachContinue(ctx, PersistKind.Menthar, 0,
                "Menthar Booby Trap on ship: cannot move. MEDICAL present - no kill; attempt continues. Cure: 2 ENGINEER.");
        }
        var r = Attach(ctx, PersistKind.Menthar, 0,
            "Menthar Booby Trap on ship: cannot move. No MEDICAL - 1 crew killed (random); ship/crew stopped. Cure: 2 ENGINEER.");
        AddKill(r.Kill, RandomOf(ctx, ctx.Team));
        return r;
    }

    /// <summary>DE mini-test for Menthar Booby Trap. Returns null if OK, else failure reason.</summary>
    public static string? VerifyMentharBoobyTrap()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Menthar Booby Trap", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var med = P("Medic", "MEDICAL", "MEDICAL");
        var eng1 = P("Eng One", "ENGINEER", "ENGINEER");
        var eng2 = P("Eng Two", "ENGINEER", "ENGINEER");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var civ2 = P("Civilian Two", "CIVILIAN", "CIVILIAN");

        // No MEDICAL: AttachAndEnd + Stop + 1 random kill; still placed
        var noMed = Resolve(Make(civ, civ2, eng1));
        if (noMed.Fate != Fate.AttachAndEnd || !noMed.StopTeam)
            return $"no MEDICAL: expected AttachAndEnd+Stop, got {noMed.Fate}/stop={noMed.StopTeam}";
        if (noMed.Persist != PersistKind.Menthar || noMed.Countdown != 0)
            return $"no MEDICAL: expected Persist Menthar countdown 0, got {noMed.Persist}/{noMed.Countdown}";
        if (noMed.Kill.Count != 1)
            return $"no MEDICAL: expected 1 kill, got {noMed.Kill.Count}";
        if (!ShouldRemoveFromSeed(noMed.Fate))
            return "no MEDICAL: seed removed (placed on ship)";
        if (noMed.Score != 0 || noMed.DamageShip || noMed.DestroyShip)
            return "no MEDICAL: no score/damage/destroy";

        // MEDICAL present with 2 ENG: Rulebook 7.2.2.3 -> MEDICAL checked first (no kill), then 2 ENG immediately cures -> Overcome
        var withMedAndCure = Resolve(Make(med, eng1, eng2));
        if (withMedAndCure.Fate != Fate.Overcome || withMedAndCure.StopTeam)
            return $"MEDICAL+2 ENG: expected Overcome no stop, got {withMedAndCure.Fate}/stop={withMedAndCure.StopTeam}";
        if (withMedAndCure.Kill.Count != 0)
            return "MEDICAL+2 ENG: expected 0 kills";
        if (!ShouldRemoveFromSeed(withMedAndCure.Fate))
            return "MEDICAL+2 ENG: seed removed on cure";

        // MEDICAL alone (no ENG): AttachAndContinue, no kill, no stop; placed
        var medOnly = Resolve(Make(med, civ));
        if (medOnly.Fate != Fate.AttachAndContinue || medOnly.StopTeam)
            return $"MEDICAL only: expected AttachAndContinue no stop, got {medOnly.Fate}/stop={medOnly.StopTeam}";
        if (medOnly.Persist != PersistKind.Menthar || medOnly.Kill.Count != 0)
            return "MEDICAL only: expected Menthar attach, 0 kills";

        // Empty crew: no MEDICAL -> AttachAndEnd+Stop, 0 kills
        var empty = Resolve(Make());
        if (empty.Fate != Fate.AttachAndEnd || !empty.StopTeam)
            return $"empty: expected AttachAndEnd+Stop, got {empty.Fate}/stop={empty.StopTeam}";
        if (empty.Persist != PersistKind.Menthar || empty.Countdown != 0)
            return "empty: expected Menthar countdown 0";
        if (empty.Kill.Count != 0)
            return "empty: expected 0 kills";

        if (DecideAttachHost(PersistKind.Menthar) != AttachHostPreference.ShipOrMission)
            return "host: expected ShipOrMission";

        if (!CanCure(PersistKind.Menthar, new[] { eng1, eng2 }, 1))
            return "CanCure: 2 ENGINEER should cure";
        if (CanCure(PersistKind.Menthar, new[] { eng1, civ }, 1))
            return "CanCure: 1 ENGINEER should not cure";
        if (CanCure(PersistKind.Menthar, new[] { med, civ }, 1))
            return "CanCure: MEDICAL alone should not cure";

        return null;
    }

    private static Result Abduction(Ctx ctx)
    {
        var victim = ctx.Team.OrderByDescending(p => Eff(ctx, p).Cunning).FirstOrDefault();
        if (victim == null)
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = "Alien Abduction: no Away Team to abduct."
            };

        // Rulebook 7.2.2.3: Conditions first (highest Cunning target chosen and placed in stasis).
        // Victim cannot contribute skills from stasis. Check remaining present for 3 Leadership.
        var remainingPresent = ExcludeHeld(ctx.Present, new[] { victim });
        if (CanCure(PersistKind.Abduction, remainingPresent, ctx.AttemptingPlayer))
        {
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = $"Alien Abduction: {victim.Name} abducted, but immediately cured by remaining Away Team (3 Leadership). Discard dilemma; attempt continues."
            };
        }

        // Rulebook 7.2.2.3 & 7.2.2.2 / 7.2.6: Failing to cure does NOT cause mission failure or stop team!
        // Victim held in stasis on mission; remaining Away Team continues attempt.
        var r = AttachContinue(ctx, PersistKind.Abduction, 0,
            $"{victim.Name} in Stasis (cure: 3 Leadership OR mission completed).");
        r.Relocate = victim;
        return r;
    }

    /// <summary>DE mini-test for Alien Abduction. Returns null if OK, else failure reason.</summary>
    public static string? VerifyAlienAbduction()
    {
        static Card P(string name, string cls, string text, string cunn = "5") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = cunn,
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Alien Abduction", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var data = P("Data", "OFFICER", "OFFICER", "12"); // highest Cunning, no Leadership
        var picard = P("Picard", "OFFICER", "Leadership x3", "10"); // Cunning 10, Leadership x3
        var riker = P("Riker", "OFFICER", "Leadership", "8");
        var troi = P("Troi", "OFFICER", "Leadership x2", "7");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN", "4");

        // 1. Empty team
        var empty = Resolve(Make());
        if (empty.Fate != Fate.Overcome || empty.StopTeam)
            return $"empty: expected Overcome no stop, got {empty.Fate}/stop={empty.StopTeam}";

        // 2. Solo Data: no 3 Leadership -> AttachAndContinue, StopTeam=false, Relocate=Data
        var solo = Resolve(Make(data));
        if (solo.Fate != Fate.AttachAndContinue || solo.StopTeam)
            return $"solo Data: expected AttachAndContinue no stop, got {solo.Fate}/stop={solo.StopTeam}";
        if (solo.Persist != PersistKind.Abduction || solo.Relocate != data)
            return $"solo Data: expected Persist Abduction with Relocate Data, got {solo.Persist}/{solo.Relocate?.Name}";

        // 3. Data (CUNNING 12) + Riker (1 Lead) + Troi (2 Lead):
        // Data is victim (highest CUNNING). Remaining team has Riker+Troi = 3 Leadership -> immediately cured!
        var cured = Resolve(Make(data, riker, troi));
        if (cured.Fate != Fate.Overcome || cured.StopTeam)
            return $"Data + 3 Lead: expected Overcome no stop, got {cured.Fate}/stop={cured.StopTeam}";

        // 4. Picard alone has 3 Leadership and highest CUNNING (10) vs Civilian (4):
        // Picard is abducted. Civilian cannot cure -> AttachAndContinue, StopTeam=false.
        // Proves victim in stasis cannot cure their own abduction!
        var victimCannotSelfCure = Resolve(Make(picard, civ));
        if (victimCannotSelfCure.Fate != Fate.AttachAndContinue || victimCannotSelfCure.StopTeam)
            return $"Picard in stasis cannot self-cure: expected AttachAndContinue no stop, got {victimCannotSelfCure.Fate}/stop={victimCannotSelfCure.StopTeam}";
        if (victimCannotSelfCure.Relocate != picard)
            return $"Picard expected as victim, got {victimCannotSelfCure.Relocate?.Name}";

        // 5. CanCure with missionCompleted = true cures even with 0 personnel
        if (!CanCure(PersistKind.Abduction, Array.Empty<Card>(), 1, missionCompleted: true))
            return "CanCure: missionCompleted must cure Alien Abduction";

        return null;
    }

    /// <summary>DE mini-test for Two-Dimensional Creatures.</summary>
    public static string? VerifyTwoDimensionalCreatures()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Two-Dimensional Creatures", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var eng = P("Eng", "ENGINEER", "ENGINEER");
        var sci = P("Sci", "SCIENCE", "SCIENCE");
        var civ = P("Civ", "CIVILIAN", "CIVILIAN");

        // Cured at encounter: Overcome, StopTeam = false
        var cured = Resolve(Make(eng, sci));
        if (cured.Fate != Fate.Overcome || cured.StopTeam)
            return $"TwoDim cured: expected Overcome no stop, got {cured.Fate}/stop={cured.StopTeam}";

        // Not cured: AttachAndContinue, StopTeam = false (ship cannot move, but attempt continues, crew NOT stopped)
        var placed = Resolve(Make(civ));
        if (placed.Fate != Fate.AttachAndContinue || placed.StopTeam)
            return $"TwoDim placed: expected AttachAndContinue no stop, got {placed.Fate}/stop={placed.StopTeam}";
        if (placed.Persist != PersistKind.TwoDim)
            return $"TwoDim placed: expected Persist TwoDim, got {placed.Persist}";

        return null;
    }

    /// <summary>DE mini-test for Tsiolkovsky Infection.</summary>
    public static string? VerifyTsiolkovskyInfection()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Tsiolkovsky Infection", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var m1 = P("M1", "MEDICAL", "MEDICAL");
        var m2 = P("M2", "MEDICAL", "MEDICAL");
        var m3 = P("M3", "MEDICAL", "MEDICAL");
        var civ = P("Civ", "CIVILIAN", "CIVILIAN");

        // Cured at encounter with 3 MEDICAL: Overcome, StopTeam = false
        var cured = Resolve(Make(m1, m2, m3));
        if (cured.Fate != Fate.Overcome || cured.StopTeam)
            return $"Tsiolkovsky cured: expected Overcome no stop, got {cured.Fate}/stop={cured.StopTeam}";

        // Not cured: AttachAndContinue, StopTeam = false (crew infected, but attempt continues, crew NOT stopped)
        var placed = Resolve(Make(civ));
        if (placed.Fate != Fate.AttachAndContinue || placed.StopTeam)
            return $"Tsiolkovsky placed: expected AttachAndContinue no stop, got {placed.Fate}/stop={placed.StopTeam}";
        if (placed.Persist != PersistKind.Tsiolkovsky)
            return $"Tsiolkovsky placed: expected Persist Tsiolkovsky, got {placed.Persist}";

        return null;
    }

    // ---- Phased Matter (Premiere 42 C) ----
    // Printed (PR): "Divide Away Team into two groups. Place on larger group (your choice
    // if tie); they are in stasis. Cure with ENGINEER and SCIENCE."
    // Planet [P]. SCIENCE-related. Icons [IPG].
    // Spock #24 Soll / DRG + Glossary Errata Phased Matter:
    //   Owner splits AT; larger group phased (tie: owner picks which is "larger"; Solo=1+0).
    //   Smaller AT Continue (not stopped). Kill-list = stasis Held (Apply IsStasisPersist).
    //   Cure: ENGINEER + SCIENCE in another unphased AT at planet (phased do not count).
    // Decide: AttachAndContinue + Persist Phased + owner PickYou for larger group;
    // default without PickYou = first ceil(n/2) of Team. CanCurePhased excludes Held.
    // PARK: deep phasing LegalMoves edges (Sheliak etc.); Beam/leave already gated via IsCardInStasis.

    private static Result Phased(Ctx ctx)
    {
        int n = ctx.Team.Count;
        if (n == 0)
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = "No Away Team - Phased Matter has no effect."
            };

        // Larger size = ceil(n/2): Solo 1+0; even n = tie (n/2 each) - owner picks who is phased.
        int largerSize = (n + 1) / 2;
        var remaining = ctx.Team.ToList();
        var phased = new List<Card>();
        for (int i = 0; i < largerSize && remaining.Count > 0; i++)
        {
            Card pick = ctx.PickYou?.Invoke(
                               $"Phased Matter: pick for larger (phased) group ({i + 1}/{largerSize})",
                               remaining)
                           ?? remaining[0];
            if (!remaining.Contains(pick))
                pick = remaining[0];
            remaining.Remove(pick);
            phased.Add(pick);
        }

        var r = AttachContinue(ctx, PersistKind.Phased, 0,
            $"Phased Matter: {phased.Count} phased (larger group in stasis). Smaller continues (not stopped). Cure: ENGINEER + SCIENCE (unphased AT).");
        foreach (var p in phased)
            AddKill(r.Kill, p); // Apply: Phased Kill => stasis Held, not death
        return r;
    }

    /// <summary>Present for Phased Matter cure: exclude Held/phased (they do not count).</summary>
    public static List<Card> ExcludeHeld(IEnumerable<Card> present, IEnumerable<Card>? held)
    {
        var list = present?.ToList() ?? new List<Card>();
        if (held == null) return list;
        var h = held as IList<Card> ?? held.ToList();
        if (h.Count == 0) return list;
        return list.Where(c => !h.Any(x => ReferenceEquals(x, c))).ToList();
    }

    /// <summary>Phased Matter cure check: ENGINEER + SCIENCE among unphased present.</summary>
    public static bool CanCurePhased(IEnumerable<Card> presentAtPlanet, IEnumerable<Card>? phasedHeld) =>
        CanCure(PersistKind.Phased, ExcludeHeld(presentAtPlanet, phasedHeld), owner: 1);

    /// <summary>DE mini-test for Phased Matter. Returns null if OK, else failure reason.</summary>
    public static string? VerifyPhasedMatter()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(Func<string, IReadOnlyList<Card>, Card?>? pickYou = null, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Phased Matter", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1),
            PickYou = pickYou
        };

        var eng = P("Eng One", "ENGINEER", "ENGINEER");
        var sci = P("Sci One", "SCIENCE", "SCIENCE");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var off = P("Officer", "OFFICER", "OFFICER");
        var sec = P("Sec One", "SECURITY", "SECURITY");

        // Solo=1+0: single AT member phased; AttachAndContinue; StopTeam=false; seed removed (attach)
        var solo = Resolve(Make(null, civ));
        if (solo.Fate != Fate.AttachAndContinue || solo.StopTeam)
            return $"solo 1+0: expected AttachAndContinue no stop, got {solo.Fate}/stop={solo.StopTeam}";
        if (solo.Persist != PersistKind.Phased || solo.Countdown != 0)
            return $"solo 1+0: expected Persist Phased countdown 0, got {solo.Persist}/{solo.Countdown}";
        if (solo.Kill.Count != 1 || solo.Kill[0].Name != "Civilian")
            return $"solo 1+0: expected phase Civilian, got [{string.Join(",", solo.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(solo.Fate))
            return "solo 1+0: attached dilemma leaves seed stack";
        if (!IsStasisPersist(solo.Persist))
            return "solo 1+0: Phased must be stasis persist";

        // Odd count 3: larger=2 phased (default first two), smaller=1 continues
        var odd = Resolve(Make(null, eng, sci, civ));
        if (odd.Fate != Fate.AttachAndContinue || odd.StopTeam)
            return $"odd 3: expected AttachAndContinue no stop, got {odd.Fate}/stop={odd.StopTeam}";
        if (odd.Kill.Count != 2)
            return $"odd 3: expected 2 phased, got {odd.Kill.Count}";
        if (odd.Kill[0].Name != "Eng One" || odd.Kill[1].Name != "Sci One")
            return $"odd 3 default: expected Eng+Sci phased, got [{string.Join(",", odd.Kill.Select(k => k.Name))}]";

        // Even tie 4: largerSize=2; owner PickYou selects who is "larger" (phased)
        var pickOrder = new Queue<string>(new[] { "Civilian", "Sec One" });
        Card? Pick(string _, IReadOnlyList<Card> pool)
        {
            string want = pickOrder.Dequeue();
            return pool.First(c => c.Name == want);
        }
        var tie = Resolve(Make(Pick, eng, sci, civ, sec));
        if (tie.Fate != Fate.AttachAndContinue || tie.StopTeam)
            return $"tie 4: expected AttachAndContinue no stop, got {tie.Fate}/stop={tie.StopTeam}";
        if (tie.Kill.Count != 2 || tie.Kill[0].Name != "Civilian" || tie.Kill[1].Name != "Sec One")
            return $"tie 4 PickYou: expected Civilian+Sec phased, got [{string.Join(",", tie.Kill.Select(k => k.Name))}]";

        // Empty AT: no effect Overcome
        var empty = Resolve(Make());
        if (empty.Fate != Fate.Overcome || empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected Overcome no stop/kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";

        // Cure: ENG+SCI unphased OK; phased-only skills do not count; single skill fails
        if (!CanCurePhased(new[] { eng, sci, civ }, phasedHeld: Array.Empty<Card>()))
            return "CanCurePhased: ENG+SCI unphased should cure";
        if (CanCurePhased(new[] { eng, sci }, phasedHeld: new[] { eng, sci }))
            return "CanCurePhased: phased ENG+SCI must not count";
        if (CanCurePhased(new[] { eng, civ }, phasedHeld: Array.Empty<Card>()))
            return "CanCurePhased: ENGINEER alone should not cure";
        if (CanCurePhased(new[] { sci, off }, phasedHeld: Array.Empty<Card>()))
            return "CanCurePhased: SCIENCE alone should not cure";
        // Mix: ENG phased, SCI unphased -> no cure; both skills unphased beside held civ -> cure
        if (CanCurePhased(new[] { eng, sci }, phasedHeld: new[] { eng }))
            return "CanCurePhased: ENG held + SCI free should not cure (need both unphased)";
        if (!CanCurePhased(new[] { eng, sci, civ }, phasedHeld: new[] { civ }))
            return "CanCurePhased: ENG+SCI free with civ held should cure";

        return null;
    }

    private static Result RelocateGender(Ctx ctx, bool female)
    {
        var pool = ctx.Team.Where(p => female ? IsFemale(p) : IsMale(p)).ToList();
        var v = RandomOf(ctx, pool);
        if (v == null)
            return new Result { Fate = Fate.Overcome, Message = "No matching personnel – dilemma has no effect." };
        return new Result
        {
            Fate = Fate.EffectAndContinue,
            StopTeam = false,
            Relocate = v,
            Message = $"{v.Name} relocated to the furthest other planet."
        };
    }

    // ---- Portal Guard (Premiere 43 U) ----
    // Printed (PR): "Unless one Away Team member has CUNNING>7 or Honor, immediately beam entire Away Team
    // off planet surface OR kills entire Away Team."
    // Planet [P].
    // Spock #25 Soll / DRG Portal Guard:
    //   Pass (>=1 AT member CUNNING>7 OR Honor) -> Overcome discard + Continue.
    //   Fail -> WallFailed (dilemma under Mission) + StopTeam + BeamBackTeam (beam up then stopped).
    //   If any member cannot beam (quarantined/stasis) or no ship/facility present: kill entire Away Team.
    // PARK: Borg abort edges.
    // Decide: DilemmaRules.DecidePortalGuard + Portal + VerifyPortalGuard.

    public readonly record struct PortalGuardPlan(
        Fate Fate,
        bool StopTeam,
        bool BeamBackTeam,
        bool KillTeam,
        string Message);

    /// <summary>
    /// DRG Portal Guard:
    /// If Away Team meets conditions (at least one personnel with CUNNING&gt;7 OR Honor),
    /// discard dilemma; mission continues.
    /// Otherwise, mission attempt immediately ends. Entire Away Team must immediately beam off of the planet.
    /// If beaming is successful, Away Team is stopped; replace dilemma under mission to be encountered again.
    /// Otherwise, if any Away Team member cannot beam or there is no ship or facility present to beam to,
    /// Away Team is killed. Replace dilemma under mission to be encountered again.
    /// </summary>
    public static PortalGuardPlan DecidePortalGuard(bool hasCunningGt7OrHonor, bool canBeamOffPlanet = true)
    {
        if (hasCunningGt7OrHonor)
        {
            return new PortalGuardPlan(
                Fate.Overcome,
                StopTeam: false,
                BeamBackTeam: false,
                KillTeam: false,
                Message: "CUNNING>7 or Honor - Portal Guard overcome.");
        }
        if (!canBeamOffPlanet)
        {
            return new PortalGuardPlan(
                Fate.WallFailed,
                StopTeam: true,
                BeamBackTeam: false,
                KillTeam: true,
                Message: "Portal Guard: filter not met and Away Team cannot beam up (quarantined, stasis, or no ship/facility) - entire Away Team killed; dilemma remains under mission.");
        }
        return new PortalGuardPlan(
            Fate.WallFailed,
            StopTeam: true,
            BeamBackTeam: true,
            KillTeam: false,
            Message: "Portal Guard: filter not met - Away Team beams up (stopped); dilemma remains under mission.");
    }

    private static Result Portal(Ctx ctx)
    {
        bool ok = ctx.Team.Any(p =>
        {
            var e = Eff(ctx, p);
            return e.Cunning > 7
                || e.Skills.Keys.Any(k => k.Equals("Honor", StringComparison.OrdinalIgnoreCase));
        });
        var plan = DecidePortalGuard(ok, ctx.CanBeamOffPlanet);
        var r = new Result
        {
            Fate = plan.Fate,
            StopTeam = plan.StopTeam,
            BeamBackTeam = plan.BeamBackTeam,
            Message = plan.Message
        };
        if (plan.KillTeam)
        {
            r.Kill.AddRange(ctx.Team);
        }
        return r;
    }

    /// <summary>DE mini-test for Portal Guard. Returns null if OK, else failure reason.</summary>
    public static string? VerifyPortalGuard()
    {
        static Card P(string name, string cls, string text, string cunn) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = cunn,
            StrengthOrShields = "5"
        };

        static Ctx Make(bool canBeam, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Portal Guard", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            CanBeamOffPlanet = canBeam,
            Rng = new Random(1)
        };

        static Ctx MakeDefault(params Card[] team) => Make(true, team);

        var smart = P("Smart One", "OFFICER", "OFFICER", "8");          // CUNNING 8 > 7
        var honor = P("Honorable", "SECURITY", "SECURITY Honor", "5"); // Honor, low CUNNING
        var low = P("Low One", "CIVILIAN", "CIVILIAN", "7");            // CUNNING == 7 fails
        var weak = P("Weak One", "CIVILIAN", "CIVILIAN", "4");

        // Decide pass
        var dPass = DecidePortalGuard(true);
        if (dPass.Fate != Fate.Overcome || dPass.StopTeam || dPass.BeamBackTeam || dPass.KillTeam)
            return $"Decide pass: expected Overcome no stop/beam/kill, got {dPass.Fate}/stop={dPass.StopTeam}/beam={dPass.BeamBackTeam}/kill={dPass.KillTeam}";
        if (!ShouldRemoveFromSeed(dPass.Fate))
            return "Decide pass: dilemma should discard";

        // Decide fail with beam
        var dFail = DecidePortalGuard(false, canBeamOffPlanet: true);
        if (dFail.Fate != Fate.WallFailed || !dFail.StopTeam || !dFail.BeamBackTeam || dFail.KillTeam)
            return $"Decide fail: expected WallFailed+Stop+BeamBack, got {dFail.Fate}/stop={dFail.StopTeam}/beam={dFail.BeamBackTeam}/kill={dFail.KillTeam}";
        if (ShouldRemoveFromSeed(dFail.Fate))
            return "Decide fail: dilemma must stay under mission (WallFailed)";

        // Decide fail without beam (quarantine / no destination)
        var dFailKill = DecidePortalGuard(false, canBeamOffPlanet: false);
        if (dFailKill.Fate != Fate.WallFailed || !dFailKill.StopTeam || dFailKill.BeamBackTeam || !dFailKill.KillTeam)
            return $"Decide fail kill: expected WallFailed+Stop+NoBeam+Kill, got {dFailKill.Fate}/stop={dFailKill.StopTeam}/beam={dFailKill.BeamBackTeam}/kill={dFailKill.KillTeam}";
        if (ShouldRemoveFromSeed(dFailKill.Fate))
            return "Decide fail kill: dilemma must stay under mission (WallFailed)";

        // Pass: CUNNING 8 > 7
        var passCunn = Resolve(MakeDefault(smart, weak));
        if (passCunn.Fate != Fate.Overcome || passCunn.StopTeam || passCunn.BeamBackTeam)
            return $"pass CUNNING>7: expected Overcome no stop/beam, got {passCunn.Fate}/stop={passCunn.StopTeam}/beam={passCunn.BeamBackTeam}";
        if (passCunn.Kill.Count != 0)
            return "pass CUNNING>7: no kills";
        if (!ShouldRemoveFromSeed(passCunn.Fate))
            return "pass CUNNING>7: dilemma should discard";

        // Pass: Honor alone (CUNNING 5)
        var passHonor = Resolve(MakeDefault(honor, weak));
        if (passHonor.Fate != Fate.Overcome || passHonor.StopTeam || passHonor.BeamBackTeam)
            return $"pass Honor: expected Overcome no stop/beam, got {passHonor.Fate}/stop={passHonor.StopTeam}/beam={passHonor.BeamBackTeam}";
        if (!ShouldRemoveFromSeed(passHonor.Fate))
            return "pass Honor: dilemma should discard";

        // Pass even if CanBeamOffPlanet is false (condition met so beam is not needed)
        var passNoBeam = Resolve(Make(false, smart, weak));
        if (passNoBeam.Fate != Fate.Overcome || passNoBeam.StopTeam || passNoBeam.BeamBackTeam || passNoBeam.Kill.Count != 0)
            return "pass CUNNING>7 with no beam: expected Overcome without kill";

        // Boundary fail: CUNNING == 7, no Honor (can beam)
        var failEq = Resolve(MakeDefault(low));
        if (failEq.Fate != Fate.WallFailed || !failEq.StopTeam || !failEq.BeamBackTeam)
            return $"fail CUNNING=7: expected WallFailed+Stop+BeamBack, got {failEq.Fate}/stop={failEq.StopTeam}/beam={failEq.BeamBackTeam}";
        if (failEq.Kill.Count != 0)
            return "fail CUNNING=7: no kills when beam is possible";
        if (ShouldRemoveFromSeed(failEq.Fate))
            return "fail CUNNING=7: dilemma must stay (WallFailed)";

        // Fail: weak only (can beam)
        var failWeak = Resolve(MakeDefault(weak));
        if (failWeak.Fate != Fate.WallFailed || !failWeak.StopTeam || !failWeak.BeamBackTeam)
            return $"fail weak: expected WallFailed+Stop+BeamBack, got {failWeak.Fate}/stop={failWeak.StopTeam}/beam={failWeak.BeamBackTeam}";
        if (ShouldRemoveFromSeed(failWeak.Fate))
            return "fail weak: dilemma must stay (WallFailed)";

        // Fail: empty AT
        var empty = Resolve(MakeDefault());
        if (empty.Fate != Fate.WallFailed || !empty.StopTeam || !empty.BeamBackTeam)
            return $"empty: expected WallFailed+Stop+BeamBack, got {empty.Fate}/stop={empty.StopTeam}/beam={empty.BeamBackTeam}";
        if (ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma must stay (WallFailed)";

        // Fail when CanBeamOffPlanet is false (quarantined or no destination) -> ENTIRE AWAY TEAM KILLED!
        var failKillSingle = Resolve(Make(false, low));
        if (failKillSingle.Fate != Fate.WallFailed || !failKillSingle.StopTeam || failKillSingle.BeamBackTeam)
            return $"fail no-beam single: expected WallFailed+Stop+NoBeam, got {failKillSingle.Fate}/stop={failKillSingle.StopTeam}/beam={failKillSingle.BeamBackTeam}";
        if (failKillSingle.Kill.Count != 1 || !ReferenceEquals(failKillSingle.Kill[0], low))
            return $"fail no-beam single: expected low killed, got count={failKillSingle.Kill.Count}";
        if (ShouldRemoveFromSeed(failKillSingle.Fate))
            return "fail no-beam single: dilemma must stay under mission";

        var failKillMultiple = Resolve(Make(false, low, weak));
        if (failKillMultiple.Fate != Fate.WallFailed || !failKillMultiple.StopTeam || failKillMultiple.BeamBackTeam)
            return $"fail no-beam multiple: expected WallFailed+Stop+NoBeam, got {failKillMultiple.Fate}/stop={failKillMultiple.StopTeam}/beam={failKillMultiple.BeamBackTeam}";
        if (failKillMultiple.Kill.Count != 2)
            return $"fail no-beam multiple: expected 2 killed, got count={failKillMultiple.Kill.Count}";
        if (ShouldRemoveFromSeed(failKillMultiple.Fate))
            return "fail no-beam multiple: dilemma must stay under mission";

        return null;
    }

    private static Result Sarjenka(Ctx ctx)
    {
        if (ctx.Confirm?.Invoke("Sarjenka: stop all Away Teams here for +5?") ?? false)
            return new Result { Fate = Fate.Overcome, Score = 5, StopTeam = true, Message = "Sarjenka: +5, teams stopped." };
        return new Result { Fate = Fate.Overcome, Message = "Sarjenka declined – attempt continues." };
    }

    private static Result Tarellian(Ctx ctx)
    {
        var pick = ctx.PickYou?.Invoke("Tarellian: whom to beam (MEDICAL saves)?", ctx.Team.ToList())
                   ?? ctx.Team.FirstOrDefault();
        bool med = pick != null && Eff(ctx, pick).Skills.Keys.Any(k =>
            k.Contains("MEDICAL", StringComparison.OrdinalIgnoreCase));
        if (med)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = $"{pick!.Name} (MEDICAL) → +5." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "No MEDICAL aboard – crew dies." };
        r.Kill.AddRange(ctx.Team);
        return r;
    }

    // ---- Iconian Computer Weapon (Premiere 29 C) ----
    // Printed (PR): "Unless SCIENCE present, reveal hand, discarding all non-personnel
    //   cards revealed. Then, draw a card for each card discarded this way. Discard dilemma."
    // Spock #13 Soll / DRG Iconian Computer Weapon (standalone, not combo):
    //   Space; Pass SCIENCE -> Overcome (dilemma discard + Continue).
    //   Fail -> Ship+Crew stopped (EffectAndEnd + StopTeam); Hand: discard ALL non-personnel
    //           (personnel stay); draw equal number from draw deck (DrawForDiscarded);
    //           dilemma discard. Apply: TableWindow DiscardNonPersonnelFromHand + DrawOneToHand.
    //   No bonus points / no ship damage.

    private static Result IconianComputerWeapon(Ctx ctx)
    {
        if (Skill(ctx, "SCIENCE"))
            return new Result { Fate = Fate.Overcome, Message = "SCIENCE present - Iconian Computer Weapon overcome." };
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            DrawForDiscarded = true,
            Message = "Reveal hand: non-personnel discarded, draw that many."
        };
        if (ctx.Hand != null)
            r.DiscardNonPersonnelFromHand.AddRange(ctx.Hand.Where(c => !ModifierRules.IsPersonnelCard(c)));
        return r;
    }

    /// <summary>DE mini-test for Iconian Computer Weapon. Returns null if OK, else failure reason.</summary>
    public static string? VerifyIconianComputerWeapon()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Card Ev(string name) => new() { Name = name, Type = "Event", Text = "Event" };
        static Card Ir(string name) => new() { Name = name, Type = "Interrupt", Text = "Interrupt" };
        static Card Eq(string name) => new() { Name = name, Type = "Equipment", Text = "Equipment" };

        static Ctx Make(Card[]? team = null, Card[]? hand = null) => new()
        {
            Dilemma = new Card { Name = "Iconian Computer Weapon", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team ?? Array.Empty<Card>(),
            Present = team ?? Array.Empty<Card>(),
            Hand = hand,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var sci = P("Sci One", "SCIENCE", "SCIENCE");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var ev = Ev("Red Alert");
        var ir = Ir("Amanda Rogers");
        var eq = Eq("Tricorder");
        var handPers = P("Hand Pers", "OFFICER", "OFFICER");

        // Pass: SCIENCE -> Overcome Continue, no stop, no hand discard, dilemma discard
        var pass = Resolve(Make(new[] { sci, civ }, new[] { ev, handPers }));
        if (pass.Fate != Fate.Overcome || pass.StopTeam)
            return $"pass SCIENCE: expected Overcome no stop, got {pass.Fate}/stop={pass.StopTeam}";
        if (pass.DrawForDiscarded || pass.DiscardNonPersonnelFromHand.Count > 0)
            return "pass SCIENCE: should not discard hand / draw";
        if (pass.DamageShip || pass.DestroyShip || pass.Score != 0)
            return "pass: no damage/destroy/score";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass SCIENCE: dilemma should discard";

        // Fail: mixed hand -> EffectAndEnd+Stop; discard ONLY non-personnel; personnel stay (not listed)
        var failHand = new[] { ev, handPers, ir, eq };
        var fail = Resolve(Make(new[] { civ }, failHand));
        if (fail.Fate != Fate.EffectAndEnd || !fail.StopTeam)
            return $"fail no-SCIENCE: expected EffectAndEnd+Stop, got {fail.Fate}/stop={fail.StopTeam}";
        if (!fail.DrawForDiscarded)
            return "fail: DrawForDiscarded should be true";
        if (fail.DiscardNonPersonnelFromHand.Count != 3)
            return $"fail mixed hand: expected 3 non-personnel discards, got {fail.DiscardNonPersonnelFromHand.Count}";
        if (fail.DiscardNonPersonnelFromHand.Any(ModifierRules.IsPersonnelCard))
            return "fail: personnel must stay in hand (not in DiscardNonPersonnelFromHand)";
        if (!fail.DiscardNonPersonnelFromHand.Contains(ev) || !fail.DiscardNonPersonnelFromHand.Contains(ir) || !fail.DiscardNonPersonnelFromHand.Contains(eq))
            return "fail: Event/Interrupt/Equipment should all be discarded from hand";
        if (!ShouldRemoveFromSeed(fail.Fate))
            return "fail: dilemma should discard";

        // Fail: empty hand -> still stop + DrawForDiscarded, zero discards
        var emptyHand = Resolve(Make(new[] { civ }, Array.Empty<Card>()));
        if (emptyHand.Fate != Fate.EffectAndEnd || !emptyHand.StopTeam || !emptyHand.DrawForDiscarded)
            return $"empty hand: expected EffectAndEnd+Stop+DrawForDiscarded, got {emptyHand.Fate}/stop={emptyHand.StopTeam}/draw={emptyHand.DrawForDiscarded}";
        if (emptyHand.DiscardNonPersonnelFromHand.Count != 0)
            return "empty hand: no discards";
        if (!ShouldRemoveFromSeed(emptyHand.Fate))
            return "empty hand: dilemma should discard";

        // Fail: hand all personnel -> stop, zero non-personnel discards
        var allPers = Resolve(Make(new[] { civ }, new[] { handPers, sci }));
        if (allPers.Fate != Fate.EffectAndEnd || !allPers.StopTeam || !allPers.DrawForDiscarded)
            return $"all-personnel hand: expected EffectAndEnd+Stop+DrawForDiscarded, got {allPers.Fate}/stop={allPers.StopTeam}";
        if (allPers.DiscardNonPersonnelFromHand.Count != 0)
            return "all-personnel hand: personnel stay; zero discards";

        // Fail: null Hand (Decide without UI hand) -> stop flags set, empty discard list
        var nullHand = Resolve(Make(new[] { civ }, null));
        if (nullHand.Fate != Fate.EffectAndEnd || !nullHand.StopTeam || !nullHand.DrawForDiscarded)
            return $"null Hand: expected EffectAndEnd+Stop+DrawForDiscarded, got {nullHand.Fate}/stop={nullHand.StopTeam}";
        if (nullHand.DiscardNonPersonnelFromHand.Count != 0)
            return "null Hand: empty discard list";

        // Fail: empty crew / no SCIENCE
        var emptyCrew = Resolve(Make(Array.Empty<Card>(), new[] { ev }));
        if (emptyCrew.Fate != Fate.EffectAndEnd || !emptyCrew.StopTeam)
            return $"empty crew: expected EffectAndEnd+Stop, got {emptyCrew.Fate}/stop={emptyCrew.StopTeam}";
        if (emptyCrew.DiscardNonPersonnelFromHand.Count != 1 || !emptyCrew.DiscardNonPersonnelFromHand.Contains(ev))
            return "empty crew: should discard Event from hand";

        return null;
    }

    // ---- Impassable Door (Premiere 30 C) ----
    // Printed (PR): "To get past requires Computer Skill."
    // Planet [P] wall: pass Computer Skill -> Overcome (dilemma discard + Continue).
    // Fail -> WallFailed + StopTeam (dilemma stays under mission). No kills / score / damage.
    // Spock #14 Soll / DRG Impassable Door: Planet-Wall Computer Skill -> discard+Continue;
    // Fail: AT stopped; dilemma under Mission (WallFailed stays).

    private static Result ImpassableDoor(Ctx ctx) =>
        Wall(ctx, Skill(ctx, "Computer Skill"), "Computer Skill");

    /// <summary>DE mini-test for Impassable Door. Returns null if OK, else failure reason.</summary>
    public static string? VerifyImpassableDoor()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Impassable Door", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var cs1 = P("Comp One", "OFFICER", "OFFICER Computer Skill");
        var csx2 = P("Comp Double", "OFFICER", "OFFICER Computer Skill x2");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var sci = P("Sci One", "SCIENCE", "SCIENCE");

        // Pass: Computer Skill present -> Overcome Continue, dilemma discard
        var pass = Resolve(Make(cs1, civ));
        if (pass.Fate != Fate.Overcome || pass.StopTeam)
            return $"pass Computer Skill: expected Overcome no stop, got {pass.Fate}/stop={pass.StopTeam}";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass Computer Skill: dilemma should discard";
        if (pass.Kill.Count != 0 || pass.Score != 0 || pass.DamageShip || pass.DestroyShip)
            return "pass: no kill/score/damage/destroy";

        // Pass: Computer Skill x2 on one personnel
        var passX2 = Resolve(Make(csx2));
        if (passX2.Fate != Fate.Overcome || passX2.StopTeam)
            return $"pass Computer Skill x2: expected Overcome no stop, got {passX2.Fate}/stop={passX2.StopTeam}";
        if (!ShouldRemoveFromSeed(passX2.Fate))
            return "pass x2: dilemma should discard";

        // Fail: SCIENCE only (not Computer Skill) -> WallFailed + Stop; dilemma stays
        var failSci = Resolve(Make(sci, civ));
        if (failSci.Fate != Fate.WallFailed || !failSci.StopTeam)
            return $"fail SCIENCE-only: expected WallFailed+Stop, got {failSci.Fate}/stop={failSci.StopTeam}";
        if (ShouldRemoveFromSeed(failSci.Fate))
            return "fail SCIENCE-only: dilemma must stay (WallFailed)";

        // Fail: civilian / no Computer Skill
        var failCiv = Resolve(Make(civ));
        if (failCiv.Fate != Fate.WallFailed || !failCiv.StopTeam)
            return $"fail civilian: expected WallFailed+Stop, got {failCiv.Fate}/stop={failCiv.StopTeam}";
        if (ShouldRemoveFromSeed(failCiv.Fate))
            return "fail civilian: dilemma must stay";

        // Fail: empty Away Team
        var empty = Resolve(Make());
        if (empty.Fate != Fate.WallFailed || !empty.StopTeam)
            return $"empty: expected WallFailed+Stop, got {empty.Fate}/stop={empty.StopTeam}";
        if (ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma must stay";

        return null;
    }

    // ---- Ktarian Game (Premiere 31 R) ----
    // Printed (PR): "Place on ship. Now and start of each turn, one personnel aboard
    //   (random selection) is disabled. Cure with CUNNING>30 OR any android."
    // Spock #15 Soll / DRG Ktarian Game / Major Rakal:
    //   Space; place on ship; crew NOT stopped (except Disabled) -> AttachAndContinue.
    //   Cure (encounter or later): non-disabled CUNNING>30 OR android aboard -> Overcome/discard.
    //   PARK: Lefler nullify (QC); Now+SOT random Disable Apply (no Disable list on Result /
    //         no SOT tick wired) - Decide covers Attach+Continue+cure only.

    private static Result KtarianGame(Ctx ctx)
    {
        if (CanCure(PersistKind.Ktarian, ctx.Present, ctx.AttemptingPlayer))
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = "Ktarian Game cured (CUNNING>30 or Android). Discard dilemma."
            };
        return AttachContinue(ctx, PersistKind.Ktarian, 0,
            "Ktarian Game on ship: now + start of each of your turns, 1 personnel aboard disabled. Cure: CUNNING>30 or Android. Crew continues.");
    }

    /// <summary>DE mini-test for Ktarian Game. Returns null if OK, else failure reason.</summary>
    public static string? VerifyKtarianGame()
    {
        static Card P(string name, string cls, string text, string cunn = "5") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = cunn,
            StrengthOrShields = "5"
        };

        static Card Android(string name) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = "ANDROID",
            Text = "ANDROID Computer Skill",
            Characteristics = "Android; Artificial;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Ktarian Game", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var civ = P("Civilian", "CIVILIAN", "CIVILIAN", "8");
        var smart = P("Smart One", "OFFICER", "OFFICER", "12");
        var smart2 = P("Smart Two", "SCIENCE", "SCIENCE", "10");
        var smart3 = P("Smart Three", "ENGINEER", "ENGINEER", "9"); // 12+10+9=31 > 30
        var droid = Android("Data");

        var cureCunn = Resolve(Make(smart, smart2, smart3));
        if (cureCunn.Fate != Fate.Overcome || cureCunn.StopTeam)
            return $"cure CUNNING>30: expected Overcome no stop, got {cureCunn.Fate}/stop={cureCunn.StopTeam}";
        if (cureCunn.Persist != PersistKind.None)
            return "cure CUNNING>30: should not attach";
        if (!ShouldRemoveFromSeed(cureCunn.Fate))
            return "cure CUNNING>30: dilemma should discard";

        var cureDroid = Resolve(Make(civ, droid));
        if (cureDroid.Fate != Fate.Overcome || cureDroid.StopTeam)
            return $"cure Android: expected Overcome no stop, got {cureDroid.Fate}/stop={cureDroid.StopTeam}";
        if (cureDroid.Persist != PersistKind.None)
            return "cure Android: should not attach";
        if (!ShouldRemoveFromSeed(cureDroid.Fate))
            return "cure Android: dilemma should discard";

        var eq30 = new[] { P("A", "OFFICER", "OFFICER", "10"), P("B", "SCIENCE", "SCIENCE", "10"), P("C", "ENGINEER", "ENGINEER", "10") };
        var failEq = Resolve(Make(eq30));
        if (failEq.Fate != Fate.AttachAndContinue || failEq.StopTeam)
            return $"CUNNING==30: expected AttachAndContinue no stop, got {failEq.Fate}/stop={failEq.StopTeam}";
        if (failEq.Persist != PersistKind.Ktarian || failEq.Countdown != 0)
            return $"CUNNING==30: expected Persist Ktarian countdown 0, got {failEq.Persist}/{failEq.Countdown}";
        if (!ShouldRemoveFromSeed(failEq.Fate))
            return "CUNNING==30: seed removed (placed on ship)";

        var place = Resolve(Make(civ));
        if (place.Fate != Fate.AttachAndContinue || place.StopTeam)
            return $"place: expected AttachAndContinue no stop, got {place.Fate}/stop={place.StopTeam}";
        if (place.Persist != PersistKind.Ktarian || place.Countdown != 0)
            return $"place: expected Ktarian countdown 0, got {place.Persist}/{place.Countdown}";
        if (place.Kill.Count != 0 || place.Score != 0 || place.DamageShip || place.DestroyShip)
            return "place: no kill/score/damage/destroy";
        if (!ShouldRemoveFromSeed(place.Fate))
            return "place: seed removed (placed on ship)";

        var empty = Resolve(Make());
        if (empty.Fate != Fate.AttachAndContinue || empty.StopTeam)
            return $"empty: expected AttachAndContinue no stop, got {empty.Fate}/stop={empty.StopTeam}";
        if (empty.Persist != PersistKind.Ktarian || empty.Countdown != 0)
            return "empty: expected Ktarian countdown 0";

        if (DecideAttachHost(PersistKind.Ktarian) != AttachHostPreference.ShipOrMission)
            return "host: expected ShipOrMission";

        if (!CanCure(PersistKind.Ktarian, new[] { smart, smart2, smart3 }, 1))
            return "CanCure: CUNNING>30 should cure";
        if (!CanCure(PersistKind.Ktarian, new[] { droid }, 1))
            return "CanCure: Android should cure";
        if (CanCure(PersistKind.Ktarian, eq30, 1))
            return "CanCure: CUNNING==30 should not cure";

        return null;
    }


    // ---- Matriarchal Society (Premiere 33 U) ----
    // Printed (PR): "Cannot get past unless at least two female Away Team members are present."
    // Planet [P] wall: pass >=2 Female (Characteristics) -> Overcome (dilemma discard + Continue).
    // Fail -> WallFailed + StopTeam (dilemma stays under mission). No kills / score / damage.
    // Spock #16 Soll / DRG Matriarchal Society: Planet-Wall >=2 Female -> discard+Continue;
    // Fail: AT stopped; dilemma under Mission (WallFailed stays).
    // PARK: gender-related Borg immediate discard (no Borg edge wired here).

    private static Result MatriarchalSociety(Ctx ctx) =>
        Wall(ctx, ctx.Team.Count(IsFemale) >= 2, "2 Female");

    /// <summary>DE mini-test for Matriarchal Society. Returns null if OK, else failure reason.</summary>
    public static string? VerifyMatriarchalSociety()
    {
        static Card P(string name, string chars, string cls = "CIVILIAN", string text = "CIVILIAN") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = chars,
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Matriarchal Society", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var f1 = P("Female One", "Human; Female;");
        var f2 = P("Female Two", "Human; Female;");
        var f3 = P("Female Three", "Human; Female;");
        var m1 = P("Male One", "Human; Male;");
        var m2 = P("Male Two", "Human; Male;");

        // Pass: exactly 2 females -> Overcome Continue, dilemma discard
        var pass2 = Resolve(Make(f1, f2, m1));
        if (pass2.Fate != Fate.Overcome || pass2.StopTeam)
            return $"pass 2 Female: expected Overcome no stop, got {pass2.Fate}/stop={pass2.StopTeam}";
        if (!ShouldRemoveFromSeed(pass2.Fate))
            return "pass 2 Female: dilemma should discard";
        if (pass2.Kill.Count != 0 || pass2.Score != 0 || pass2.DamageShip || pass2.DestroyShip)
            return "pass 2: no kill/score/damage/destroy";

        // Pass: 3 females
        var pass3 = Resolve(Make(f1, f2, f3));
        if (pass3.Fate != Fate.Overcome || pass3.StopTeam)
            return $"pass 3 Female: expected Overcome no stop, got {pass3.Fate}/stop={pass3.StopTeam}";
        if (!ShouldRemoveFromSeed(pass3.Fate))
            return "pass 3 Female: dilemma should discard";

        // Fail: only 1 female -> WallFailed + Stop; dilemma stays
        var fail1 = Resolve(Make(f1, m1, m2));
        if (fail1.Fate != Fate.WallFailed || !fail1.StopTeam)
            return $"fail 1 Female: expected WallFailed+Stop, got {fail1.Fate}/stop={fail1.StopTeam}";
        if (ShouldRemoveFromSeed(fail1.Fate))
            return "fail 1 Female: dilemma must stay (WallFailed)";

        // Fail: males only
        var failM = Resolve(Make(m1, m2));
        if (failM.Fate != Fate.WallFailed || !failM.StopTeam)
            return $"fail males-only: expected WallFailed+Stop, got {failM.Fate}/stop={failM.StopTeam}";
        if (ShouldRemoveFromSeed(failM.Fate))
            return "fail males-only: dilemma must stay";

        // Fail: empty Away Team
        var empty = Resolve(Make());
        if (empty.Fate != Fate.WallFailed || !empty.StopTeam)
            return $"empty: expected WallFailed+Stop, got {empty.Fate}/stop={empty.StopTeam}";
        if (ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma must stay";

        return null;
    }

    private static Result Parasites(Ctx ctx)
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
    }
    private static Result Qdil(Ctx ctx)
    {
        if (Skill(ctx, "Leadership", 2) && Sum(ctx).integ > 60)
            return new Result { Fate = Fate.Overcome, Message = "2 Leadership + INTEGRITY>60." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Q: Q-Flash (sandbox: team stopped). Q-Continuum later."
        };
    }

    private static Result Loop(Ctx ctx)
    {
        if (Skill(ctx, "SCIENCE") && Sum(ctx).cunn > 35)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = "SCIENCE + CUNNING>35 → +5. Discard dilemma." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            EndTurn = true,
            // UI restores discards from this attempt and re-seeds seed cards under the mission.
            Message = "Temporal Causality Loop: cards discarded here this attempt return (seeds re-seeded); turn ends."
        };
    }

    private static Result Fallback(Ctx ctx)
    {
        var h = MissionRules.CanOvercomeDilemma(ctx.Dilemma, ctx.Present);
        return h.Ok
            ? new Result { Fate = Fate.Overcome, Message = "No catalog entry – heuristic: overcome." }
            : new Result { Fate = Fate.WallFailed, StopTeam = true, Message = "No catalog entry – " + h.Reason };
    }

    public static bool CanCure(PersistKind kind, IEnumerable<Card> present, int owner, bool missionCompleted = false)
    {
        var list = present.ToList();
        var team = list.Where(ModifierRules.IsPersonnelCard).ToList();
        var dummy = new Ctx
        {
            Dilemma = new Card { Name = "?" },
            Mission = new Card { Name = "?" },
            Team = team,
            Present = list,
            AttemptingPlayer = owner
        };
        return kind switch
        {
            PersistKind.Junior => Skill(dummy, "ENGINEER", 3),
            PersistKind.Nitrium => Skill(dummy, "SCIENCE", 2) || Skill(dummy, "ENGINEER", 2),
            PersistKind.Tsiolkovsky => Skill(dummy, "MEDICAL", 3),
            PersistKind.TwoDim => Skill(dummy, "ENGINEER") && Skill(dummy, "SCIENCE"),
            PersistKind.Menthar => Skill(dummy, "ENGINEER", 2),
            PersistKind.Ktarian => Sum(dummy).cunn > 30 || team.Any(IsAndroid),
            PersistKind.HyperAging => Skill(dummy, "MEDICAL", 2) && Skill(dummy, "SCIENCE"),
            PersistKind.RemFatigue => Skill(dummy, "MEDICAL", 3),
            PersistKind.Abduction => Skill(dummy, "Leadership", 3) || missionCompleted, // OR mission completed
            PersistKind.Phased => Skill(dummy, "ENGINEER") && Skill(dummy, "SCIENCE"),
            PersistKind.Scow => Skill(dummy, "ENGINEER", 2), // + tractor: UI prüft extra
            PersistKind.FrameOfMind => Skill(dummy, "Empathy", 3),
            _ => false
        };
    }

    /// <summary>One-line host-facing effect for ship/mission detail (not full card text).</summary>
    public static string FormatHostEffectSummary(PersistKind kind, Card card, int countdown)
    {
        string effect = kind switch
        {
            PersistKind.Junior => "RANGE −1 each your EOT; destroy if RANGE≤0 (nullify: 3 ENGINEER)",
            PersistKind.Scow => "ship cannot move (cure: tractor + 2 ENGINEER)",
            PersistKind.HyperAging => "countdown 3; AT dies if not cured (SCIENCE + MEDICAL×2)",
            PersistKind.RemFatigue => "countdown; crew dies if not cured (MEDICAL×3)",
            PersistKind.Nitrium => "countdown 2; ship destroyed unless SCIENCE×2 or ENGINEER×2",
            PersistKind.Menthar => "ship cannot move (cure: 2 ENGINEER)",
            PersistKind.Tsiolkovsky => "attributes −3 until MEDICAL×3",
            PersistKind.TwoDim => "ship cannot move (ENGINEER + SCIENCE)",
            PersistKind.Cytherians => "must move toward far end; +15 when reached",
            PersistKind.Conundrum => "must chase opponent ship",
            PersistKind.EdoProbe => "attempt this mission next or −10",
            PersistKind.FrameOfMind => "personnel is 3-3-3 until 3 Empathy",
            PersistKind.Abduction => "personnel held (cure: Leadership x3 OR mission completed)",
            PersistKind.Phased => "larger AT phased / stasis (cure: ENGINEER + SCIENCE unphased)",
            PersistKind.Ktarian => "1 personnel disabled (now + your SOT); cure: CUNNING>30 or Android",
            PersistKind.BorgShip => "Borg Ship dilemma remains",
            _ => ""
        };
        string line = "Dilemma: " + card.Name;
        if (!string.IsNullOrEmpty(effect))
            line += " — " + effect;
        if (countdown > 0)
            line += $"  ·  COUNTER {countdown}";
        return line;
    }
    // ---- Extract Slice 6: ApplyDilemmaResult decide gates (no WPF) ----

    /// <summary>Overcome / effect / attach / end-attempt remove the seed; WallFailed keeps it.</summary>
    public static bool ShouldRemoveFromSeed(Fate fate) =>
        fate is Fate.EffectAndEnd or Fate.EffectAndContinue or Fate.AttachAndEnd or Fate.AttachAndContinue or Fate.EndAttempt or Fate.Overcome;

    /// <summary>Track overcome/removed seeds for Temporal Causality Loop re-seed (not the loop card itself).</summary>
    public static bool ShouldTrackOvercomeDiscard(Fate fate, bool isTemporalCausalityLoopSeed) =>
        fate == Fate.Overcome
        || fate == Fate.EffectAndContinue
        || (fate == Fate.EffectAndEnd && !isTemporalCausalityLoopSeed);

    public enum AttachHostPreference
    {
        Mission,
        ShipOrMission,
        FurthestMission
    }

    /// <summary>Where AttachAndEnd dilemmas prefer to host.</summary>
    public static AttachHostPreference DecideAttachHost(PersistKind persist) =>
        persist switch
        {
            PersistKind.BorgShip => AttachHostPreference.FurthestMission,
            PersistKind.Scow or PersistKind.Abduction or PersistKind.Phased or PersistKind.HyperAging
                => AttachHostPreference.Mission,
            _ => AttachHostPreference.ShipOrMission
        };

    public static bool IsStasisPersist(PersistKind persist) =>
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
    }

    public static bool ShouldAwardScoreOnApply(int score, Fate fate) =>
        score > 0 && fate != Fate.Overcome;

    public static bool ShouldRestoreTemporalLoop(bool isTemporalCausalityLoop, Fate fate) =>
        isTemporalCausalityLoop && fate == Fate.EffectAndEnd;

    public static bool IsEdoContinuePenalty(string? seedName, Fate fate) =>
        (seedName ?? "").Equals("Edo Probe", StringComparison.OrdinalIgnoreCase)
        && fate == Fate.Overcome;

    public static bool IsConundrumChase(string? seedName, Fate fate) =>
        (seedName ?? "").Equals("Conundrum", StringComparison.OrdinalIgnoreCase)
        && fate == Fate.EffectAndEnd;

    // ---- Alien Parasites (Pass/Fail + Beam-back + Stop + Neg Control min path) ----
    // Hotseat dual-window / affiliation-mix deep enforcement PARK (Pepsch min path).

    [Flags]
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
    /// Pass INTEGRITY>32 to Overcome (discard + continue).
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
    }
    // ---- Anaphasic Organism (Premiere 12 C) ----
    // Printed: "Unless MEDICAL and SECURITY present, discards female with highest total attributes. Discard dilemma."
    // requires Female -> no female present: no effect, discard dilemma, attempt continues.

    private static Result Anaphasic(Ctx ctx)
    {
        if (Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"))
            return new Result { Fate = Fate.Overcome, Message = "Anaphasic Organism: MEDICAL and SECURITY present." };

        var females = ctx.Team.Where(IsFemale).ToList();
        if (females.Count == 0)
            return new Result
            {
                Fate = Fate.Overcome,
                Message = "Anaphasic Organism: no female present - dilemma has no effect."
            };

        var victim = HighestFemale(ctx);
        var r = new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = victim != null
                ? $"Anaphasic Organism: {victim.Name} discarded (highest female attributes)."
                : "Anaphasic Organism: highest female discarded."
        };
        AddKill(r.Discard, victim); // resign = discard, NOT killed (DRG)
        return r;
    }

    /// <summary>DE mini-test for Anaphasic Organism. Returns null if OK, else failure reason.</summary>
    public static string? VerifyAnaphasicOrganism()
    {
        static Card P(string name, string cls, string text, string chars, string i, string c, string s) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = chars,
            IntegrityOrRange = i,
            CunningOrWeapons = c,
            StrengthOrShields = s
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Anaphasic Organism", Type = "Dilemma" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var bev = P("Beverly Crusher", "MEDICAL", "MEDICAL MEDICAL Biology", "Human; Female;", "8", "8", "5");
        var tasha = P("Tasha Yar", "SECURITY", "SECURITY Honor Leadership", "Human; Female;", "8", "7", "8");
        var worf = P("Worf", "SECURITY", "SECURITY Honor", "Klingon; Male;", "8", "6", "10");
        var data = P("Data", "OFFICER", "OFFICER ENGINEER Computer Skill", "Android; Male;", "8", "12", "12");
        var lowF = P("Ensign Low", "CIVILIAN", "CIVILIAN", "Human; Female;", "4", "4", "3");

        // Pass: MEDICAL + SECURITY
        var pass = Resolve(Make(bev, tasha, worf));
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.Kill.Count != 0 || pass.Discard.Count != 0)
            return "pass MED+SEC: expected Overcome, no stop/kill/discard";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass: dilemma should discard";

        // No female: no effect (requires Female)
        var noF = Resolve(Make(worf, data));
        if (noF.Fate != Fate.Overcome || noF.StopTeam || noF.Kill.Count != 0 || noF.Discard.Count != 0)
            return "no female: expected Overcome no-effect, no stop/kill/discard";

        // Fail (no SECURITY): discard highest female among Beverly(21) vs lowF(11) -> Beverly (NOT killed)
        var fail = Resolve(Make(bev, lowF, data));
        if (fail.Fate != Fate.EffectAndEnd || !fail.StopTeam)
            return "fail: expected EffectAndEnd + StopTeam";
        if (fail.Kill.Count != 0)
            return $"fail: expected no kill (discard not kill), got Kill=[{string.Join(",", fail.Kill.Select(k => k.Name))}]";
        if (fail.Discard.Count != 1 || fail.Discard[0].Name != "Beverly Crusher")
            return $"fail: expected discard Beverly Crusher, got [{string.Join(",", fail.Discard.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(fail.Fate))
            return "fail: dilemma should discard (EffectAndEnd)";

        // Fail (no MEDICAL): SECURITY female present -> Tasha discarded (not killed)
        var failSec = Resolve(Make(tasha, data));
        if (failSec.Fate != Fate.EffectAndEnd || failSec.Kill.Count != 0
            || failSec.Discard.Count != 1 || failSec.Discard[0].Name != "Tasha Yar")
            return "fail no-MEDICAL: expected Tasha Yar discarded (not killed)";

        // Tie among females -> opponent chooses
        Card? picked = null;
        var tieA = P("Female A", "CIVILIAN", "CIVILIAN", "Human; Female;", "5", "5", "5");
        var tieB = P("Female B", "CIVILIAN", "CIVILIAN", "Human; Female;", "5", "5", "5");
        var tieCtx = new Ctx
        {
            Dilemma = new Card { Name = "Anaphasic Organism", Type = "Dilemma" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = new[] { tieA, tieB, data },
            Present = new[] { tieA, tieB, data },
            AttemptingPlayer = 1,
            Rng = new Random(1),
            PickOpp = (_, list) => { picked = list.First(x => x.Name == "Female B"); return picked; }
        };
        var tie = Resolve(tieCtx);
        if (tie.Fate != Fate.EffectAndEnd || !tie.StopTeam || tie.Kill.Count != 0
            || tie.Discard.Count != 1 || tie.Discard[0].Name != "Female B")
            return "tie: expected opp-chosen Female B discarded (not killed)";
        if (picked?.Name != "Female B")
            return "tie: PickOpp was not used";

        // Sole female still discarded even if lower attrs than males
        var sole = Resolve(Make(lowF, data, worf));
        if (sole.Fate != Fate.EffectAndEnd || sole.Kill.Count != 0
            || sole.Discard.Count != 1 || sole.Discard[0].Name != "Ensign Low")
            return "sole female: expected Ensign Low discarded (not killed)";

        return null;
    }

    // ---- Ancient Computer (Premiere 13 R) ----
    // Printed: "Cannot get past unless 2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER present."
    // Wall [S]: pass -> Overcome (discard + continue); fail -> WallFailed + StopTeam (stays under mission).

    private static Result AncientComputer(Ctx ctx) =>
        Wall(ctx,
            Skill(ctx, "Computer Skill", 2) || Skill(ctx, "SCIENCE", 3) || Skill(ctx, "ENGINEER", 3),
            "2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER");

    /// <summary>DE mini-test for Ancient Computer. Returns null if OK, else failure reason.</summary>
    public static string? VerifyAncientComputer()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Ancient Computer", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var cs1 = P("Comp One", "OFFICER", "OFFICER Computer Skill");
        var cs2 = P("Comp Two", "OFFICER", "OFFICER Computer Skill");
        var csx2 = P("Comp Double", "OFFICER", "OFFICER Computer Skill x2");
        var sci1 = P("Sci One", "SCIENCE", "SCIENCE");
        var sci2 = P("Sci Two", "SCIENCE", "SCIENCE");
        var sci3 = P("Sci Three", "SCIENCE", "SCIENCE");
        var eng1 = P("Eng One", "ENGINEER", "ENGINEER");
        var eng2 = P("Eng Two", "ENGINEER", "ENGINEER");
        var eng3 = P("Eng Three", "ENGINEER", "ENGINEER");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Pass: 2 Computer Skill (two personnel)
        var passCs = Resolve(Make(cs1, cs2, civ));
        if (passCs.Fate != Fate.Overcome || passCs.StopTeam)
            return "pass 2 Computer Skill: expected Overcome, no stop";
        if (!ShouldRemoveFromSeed(passCs.Fate))
            return "pass 2 CS: dilemma should discard";

        // Pass: Computer Skill x2 on one personnel
        var passCsX2 = Resolve(Make(csx2, civ));
        if (passCsX2.Fate != Fate.Overcome || passCsX2.StopTeam)
            return "pass Computer Skill x2: expected Overcome, no stop";

        // Pass: 3 SCIENCE
        var passSci = Resolve(Make(sci1, sci2, sci3));
        if (passSci.Fate != Fate.Overcome || passSci.StopTeam)
            return "pass 3 SCIENCE: expected Overcome, no stop";
        if (!ShouldRemoveFromSeed(passSci.Fate))
            return "pass 3 SCIENCE: dilemma should discard";

        // Pass: 3 ENGINEER
        var passEng = Resolve(Make(eng1, eng2, eng3));
        if (passEng.Fate != Fate.Overcome || passEng.StopTeam)
            return "pass 3 ENGINEER: expected Overcome, no stop";

        // Fail: only 1 Computer Skill
        var fail1cs = Resolve(Make(cs1, civ));
        if (fail1cs.Fate != Fate.WallFailed || !fail1cs.StopTeam)
            return "fail 1 CS: expected WallFailed + StopTeam";
        if (ShouldRemoveFromSeed(fail1cs.Fate))
            return "fail 1 CS: dilemma must stay (WallFailed)";

        // Fail: only 2 SCIENCE
        var fail2sci = Resolve(Make(sci1, sci2));
        if (fail2sci.Fate != Fate.WallFailed || !fail2sci.StopTeam)
            return "fail 2 SCIENCE: expected WallFailed + StopTeam";
        if (ShouldRemoveFromSeed(fail2sci.Fate))
            return "fail 2 SCIENCE: dilemma must stay";

        // Fail: only 2 ENGINEER
        var fail2eng = Resolve(Make(eng1, eng2));
        if (fail2eng.Fate != Fate.WallFailed || !fail2eng.StopTeam)
            return "fail 2 ENGINEER: expected WallFailed + StopTeam";

        // Fail: empty of required skills
        var failNone = Resolve(Make(civ));
        if (failNone.Fate != Fate.WallFailed || !failNone.StopTeam)
            return "fail none: expected WallFailed + StopTeam";

        return null;
    }
    // ---- Armus: Skin Of Evil (Premiere 15 R) ----
    // Printed (PR): "Kills one Away Team member (random selection)."
    // DRG / Ref: discard dilemma; NOT a wall; 1 random AT kill; survivors NOT stopped -> Continue (EffectAndContinue).

    private static Result ArmusSkinOfEvil(Ctx ctx)
    {
        var r = new Result
        {
            Fate = Fate.EffectAndContinue,
            StopTeam = false,
            Message = "Armus kills one random Away Team member. Discard dilemma."
        };
        AddKill(r.Kill, RandomOf(ctx, ctx.Team));
        return r;
    }

    /// <summary>DE mini-test for Armus: Skin Of Evil. Returns null if OK, else failure reason.</summary>
    public static string? VerifyArmusSkinOfEvil()
    {
        static Card P(string name) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = "OFFICER",
            Text = "OFFICER",
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(Random rng, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Armus: Skin Of Evil", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = rng
        };

        var a = P("Alpha");
        var b = P("Bravo");
        var c = P("Charlie");

        // Empty Away Team: no kill, still discard + continue
        var empty = Resolve(Make(new Random(1)));
        if (empty.Fate != Fate.EffectAndContinue || empty.StopTeam || empty.Kill.Count != 0)
            return "empty: expected EffectAndContinue, no stop/kill";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        // Solo: that personnel dies; attempt continues
        var solo = Resolve(Make(new Random(1), a));
        if (solo.Fate != Fate.EffectAndContinue || solo.StopTeam)
            return "solo: expected EffectAndContinue, no stop";
        if (solo.Kill.Count != 1 || solo.Kill[0].Name != "Alpha")
            return $"solo: expected kill Alpha, got [{string.Join(",", solo.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(solo.Fate))
            return "solo: dilemma should discard";

        // Three personnel, fixed seed: exactly one random kill; survivors continue
        var r1 = Resolve(Make(new Random(42), a, b, c));
        if (r1.Fate != Fate.EffectAndContinue || r1.StopTeam)
            return "team3: expected EffectAndContinue, no StopTeam";
        if (r1.Kill.Count != 1)
            return $"team3: expected exactly 1 kill, got {r1.Kill.Count}";
        var victim = r1.Kill[0].Name;
        if (victim is not ("Alpha" or "Bravo" or "Charlie"))
            return $"team3: victim '{victim}' not in Away Team";
        if (!ShouldRemoveFromSeed(r1.Fate))
            return "team3: dilemma should discard";

        // Same seed -> same victim (deterministic random selection)
        var r2 = Resolve(Make(new Random(42), a, b, c));
        if (r2.Kill.Count != 1 || r2.Kill[0].Name != victim)
            return $"rng: expected same victim '{victim}', got [{string.Join(",", r2.Kill.Select(k => k.Name))}]";

        return null;
    }

    // ---- Birth of "Junior" (Premiere 17 U) ----
    // Printed (PR): "Place on ship. End of each turn, reduces RANGE by 1; if this reduces RANGE below 1
    // (or RANGE already below 1), destroys ship. Nullify with 3 ENGINEER."
    // Spock/DRG/Glossary: encounter nullify 3 ENGINEER → Overcome+Continue; else place on ship,
    // crew NOT stopped → AttachAndContinue (RANGE −1 only on your EOTs). Cure later: 3 ENGINEER.
    // EOT destroy via EndOfTurnRestRules.JuniorDestroysShip (RANGE after countdown < 1).

    private static Result BirthOfJunior(Ctx ctx)
    {
        if (CanCure(PersistKind.Junior, ctx.Present, ctx.AttemptingPlayer))
            return new Result
            {
                Fate = Fate.Overcome,
                StopTeam = false,
                Message = "Birth of \"Junior\" nullified (3 ENGINEER). Discard dilemma."
            };
        return AttachContinue(ctx, PersistKind.Junior, 0,
            "Junior on ship: RANGE −1 each your end of turn; destroy if RANGE≤0. Nullify: 3 ENGINEER. Crew continues.");
    }

    /// <summary>DE mini-test for Birth of "Junior". Returns null if OK, else failure reason.</summary>
    public static string? VerifyBirthOfJunior()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Birth of \"Junior\"", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var eng1 = P("Eng One", "ENGINEER", "ENGINEER");
        var eng2 = P("Eng Two", "ENGINEER", "ENGINEER");
        var eng3 = P("Eng Three", "ENGINEER", "ENGINEER");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Nullify: 3 ENGINEER → Overcome, no stop, dilemma discarded
        var nullify = Resolve(Make(eng1, eng2, eng3));
        if (nullify.Fate != Fate.Overcome || nullify.StopTeam)
            return "nullify 3 ENG: expected Overcome, no StopTeam";
        if (nullify.Persist != PersistKind.None)
            return "nullify 3 ENG: should not attach";
        if (!ShouldRemoveFromSeed(nullify.Fate))
            return "nullify 3 ENG: dilemma should discard";

        // 2 ENGINEER: not enough → AttachAndContinue, countdown 0, no stop
        var two = Resolve(Make(eng1, eng2, civ));
        if (two.Fate != Fate.AttachAndContinue || two.StopTeam)
            return "2 ENG: expected AttachAndContinue, no StopTeam";
        if (two.Persist != PersistKind.Junior || two.Countdown != 0)
            return $"2 ENG: expected Persist Junior countdown 0, got {two.Persist}/{two.Countdown}";
        if (!ShouldRemoveFromSeed(two.Fate))
            return "2 ENG: seed removed (placed on ship)";

        // No ENGINEER: same attach + continue
        var none = Resolve(Make(civ));
        if (none.Fate != Fate.AttachAndContinue || none.StopTeam)
            return "0 ENG: expected AttachAndContinue, no StopTeam";
        if (none.Persist != PersistKind.Junior || none.Countdown != 0)
            return "0 ENG: expected Junior countdown 0";

        // Empty crew: still place (space dilemma on ship)
        var empty = Resolve(Make());
        if (empty.Fate != Fate.AttachAndContinue || empty.StopTeam)
            return "empty: expected AttachAndContinue, no StopTeam";
        if (empty.Persist != PersistKind.Junior || empty.Countdown != 0)
            return "empty: expected Junior countdown 0";

        // Host preference: ship (ShipOrMission)
        if (DecideAttachHost(PersistKind.Junior) != AttachHostPreference.ShipOrMission)
            return "host: expected ShipOrMission";

        // EOT destroy gate consistent with JuniorDestroysShip
        if (!EndOfTurnRestRules.JuniorDestroysShip(0))
            return "EOT: RANGE 0 should destroy";
        if (EndOfTurnRestRules.JuniorDestroysShip(1))
            return "EOT: RANGE 1 should not destroy";

        return null;
    }


    // ---- Chalnoth (Premiere 19 U) ----
    // Printed (PR): "Unless 3 SECURITY OR STRENGTH>40 present, kills one Away Team member
    // (opponent's choice). Otherwise, score points. Discard dilemma."
    // Spock #6 Soll/DRG: pass -> Overcome +5 Bonus-Area + Continue; fail -> opp PickKill, AT Stop,
    // dilemma discard (EffectAndEnd).

    private static Result Chalnoth(Ctx ctx) =>
        UnlessScoreOr(ctx,
            Skill(ctx, "SECURITY", 3) || Sum(ctx).str > 40,
            () => PickKill(ctx, opp: true, "Chalnoth: opponent chooses a victim."),
            5, "3 SECURITY or STRENGTH>40");

    /// <summary>DE mini-test for Chalnoth. Returns null if OK, else failure reason.</summary>
    public static string? VerifyChalnoth()
    {
        static Card P(string name, string cls, string text, string str = "5") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = str
        };

        static Ctx Make(Func<string, IReadOnlyList<Card>, Card?>? pickOpp, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Chalnoth", Type = "Dilemma", MissionDilemmaType = "[P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1),
            PickOpp = pickOpp
        };

        var sec1 = P("Sec One", "SECURITY", "SECURITY");
        var sec2 = P("Sec Two", "SECURITY", "SECURITY");
        var sec3 = P("Sec Three", "SECURITY", "SECURITY");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN", "8");
        var tankA = P("Tank A", "OFFICER", "OFFICER", "10");
        var tankB = P("Tank B", "OFFICER", "OFFICER", "10");
        var tankC = P("Tank C", "OFFICER", "OFFICER", "10");
        var tankD = P("Tank D", "OFFICER", "OFFICER", "11"); // 10+10+10+11 = 41 > 40

        // Pass: 3 SECURITY -> Overcome +5, no stop, discard
        var passSec = Resolve(Make(null, sec1, sec2, sec3));
        if (passSec.Fate != Fate.Overcome || passSec.StopTeam || passSec.Score != 5)
            return $"pass 3 SECURITY: expected Overcome Score=5 no stop, got {passSec.Fate}/{passSec.Score}/stop={passSec.StopTeam}";
        if (passSec.Kill.Count != 0)
            return "pass 3 SECURITY: should not kill";
        if (!ShouldRemoveFromSeed(passSec.Fate))
            return "pass 3 SECURITY: dilemma should discard";

        // Pass: STRENGTH>40 without 3 SECURITY
        var passStr = Resolve(Make(null, tankA, tankB, tankC, tankD));
        if (passStr.Fate != Fate.Overcome || passStr.Score != 5 || passStr.StopTeam)
            return $"pass STRENGTH>40: expected Overcome +5 no stop, got {passStr.Fate}/{passStr.Score}/stop={passStr.StopTeam}";
        if (!ShouldRemoveFromSeed(passStr.Fate))
            return "pass STRENGTH>40: dilemma should discard";

        // Fail boundary: STRENGTH==40 and only 2 SECURITY (5+5+10+10+10=40) -> not overcome
        var eq40 = new[] { sec1, sec2, tankA, tankB, tankC };
        Card? picked = null;
        var failEq = Resolve(Make((_, list) => { picked = list.First(x => x.Name == "Sec Two"); return picked; }, eq40));
        if (failEq.Fate != Fate.EffectAndEnd || !failEq.StopTeam)
            return $"fail STR=40 + 2 SEC: expected EffectAndEnd+StopTeam, got {failEq.Fate}/stop={failEq.StopTeam}";
        if (failEq.Kill.Count != 1 || failEq.Kill[0].Name != "Sec Two")
            return $"fail STR=40: expected kill Sec Two, got [{string.Join(",", failEq.Kill.Select(k => k.Name))}]";
        if (picked?.Name != "Sec Two")
            return "fail STR=40: PickOpp was not used";
        if (!ShouldRemoveFromSeed(failEq.Fate))
            return "fail STR=40: dilemma should discard";

        // Fail: weak team, opponent chooses victim
        picked = null;
        var failOpp = Resolve(Make((_, list) => { picked = list.First(x => x.Name == "Civilian"); return picked; }, civ, sec1));
        if (failOpp.Fate != Fate.EffectAndEnd || !failOpp.StopTeam)
            return "fail opp: expected EffectAndEnd + StopTeam";
        if (failOpp.Kill.Count != 1 || failOpp.Kill[0].Name != "Civilian")
            return $"fail opp: expected kill Civilian, got [{string.Join(",", failOpp.Kill.Select(k => k.Name))}]";
        if (picked?.Name != "Civilian")
            return "fail opp: PickOpp was not used";
        if (failOpp.Score != 0)
            return "fail opp: should not score";
        if (!ShouldRemoveFromSeed(failOpp.Fate))
            return "fail opp: dilemma should discard";

        // Empty Away Team fail: EffectAndEnd + Stop, no kill, discard
        var empty = Resolve(Make(null));
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || empty.Kill.Count != 0)
            return $"empty: expected EffectAndEnd+Stop no kill, got {empty.Fate}/stop={empty.StopTeam}/kills={empty.Kill.Count}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        return null;
    }

    // ---- Cosmic String Fragment (Premiere 20 U) ----
    // Printed (PR): "Unless ENGINEER OR Astrophysics OR Navigation present, destroys ship.
    // Otherwise, score points. Discard dilemma."
    // Spock #7 Soll/DRG: pass -> Overcome +5 Bonus-Area + Continue; fail -> DestroyShip
    // (everything aboard discarded via Apply), dilemma discard (EffectAndEnd).

    private static Result CosmicStringFragment(Ctx ctx) =>
        SpaceUnlessScore(ctx,
            Skill(ctx, "Astrophysics") || Skill(ctx, "ENGINEER") || Skill(ctx, "Navigation"),
            destroy: true, score: 5, need: "Astrophysics or ENGINEER or Navigation");

    /// <summary>DE mini-test for Cosmic String Fragment. Returns null if OK, else failure reason.</summary>
    public static string? VerifyCosmicStringFragment()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Cosmic String Fragment", Type = "Dilemma", MissionDilemmaType = "[S]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var eng = P("Eng One", "ENGINEER", "ENGINEER");
        var astro = P("Astro One", "SCIENCE", "Astrophysics");
        var nav = P("Nav One", "OFFICER", "Navigation");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Pass: ENGINEER alone -> Overcome +5, Continue (no stop), no destroy, discard
        var passEng = Resolve(Make(eng, civ));
        if (passEng.Fate != Fate.Overcome || passEng.StopTeam || passEng.Score != 5)
            return $"pass ENGINEER: expected Overcome Score=5 no stop, got {passEng.Fate}/{passEng.Score}/stop={passEng.StopTeam}";
        if (passEng.DestroyShip || passEng.DamageShip)
            return "pass ENGINEER: should not destroy/damage ship";
        if (!ShouldRemoveFromSeed(passEng.Fate))
            return "pass ENGINEER: dilemma should discard";

        // Pass: Astrophysics alone
        var passAstro = Resolve(Make(astro));
        if (passAstro.Fate != Fate.Overcome || passAstro.Score != 5 || passAstro.StopTeam || passAstro.DestroyShip)
            return $"pass Astrophysics: expected Overcome +5 Continue no destroy, got {passAstro.Fate}/{passAstro.Score}/stop={passAstro.StopTeam}/destroy={passAstro.DestroyShip}";
        if (!ShouldRemoveFromSeed(passAstro.Fate))
            return "pass Astrophysics: dilemma should discard";

        // Pass: Navigation alone
        var passNav = Resolve(Make(nav));
        if (passNav.Fate != Fate.Overcome || passNav.Score != 5 || passNav.StopTeam || passNav.DestroyShip)
            return $"pass Navigation: expected Overcome +5 Continue no destroy, got {passNav.Fate}/{passNav.Score}/stop={passNav.StopTeam}/destroy={passNav.DestroyShip}";
        if (!ShouldRemoveFromSeed(passNav.Fate))
            return "pass Navigation: dilemma should discard";

        // Fail: no matching skill -> EffectAndEnd + Stop + DestroyShip, no score, discard
        var fail = Resolve(Make(civ));
        if (fail.Fate != Fate.EffectAndEnd || !fail.StopTeam || !fail.DestroyShip)
            return $"fail: expected EffectAndEnd+Stop+DestroyShip, got {fail.Fate}/stop={fail.StopTeam}/destroy={fail.DestroyShip}";
        if (fail.Score != 0)
            return "fail: should not score";
        if (fail.DamageShip)
            return "fail: destroy not damage";
        if (!ShouldRemoveFromSeed(fail.Fate))
            return "fail: dilemma should discard";

        // Empty crew fail: still destroy ship + discard
        var empty = Resolve(Make());
        if (empty.Fate != Fate.EffectAndEnd || !empty.StopTeam || !empty.DestroyShip)
            return $"empty: expected EffectAndEnd+Stop+DestroyShip, got {empty.Fate}/stop={empty.StopTeam}/destroy={empty.DestroyShip}";
        if (!ShouldRemoveFromSeed(empty.Fate))
            return "empty: dilemma should discard";

        return null;
    }

    /// <summary>DE mini-test for Crystalline Entity. Returns null if OK, else failure reason.</summary>
    public static string? VerifyCrystallineEntity()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx MakePlanet(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Crystalline Entity", Type = "Dilemma", MissionDilemmaType = "[S/P]" },
            Mission = new Card { Name = "Test Planet", Type = "Mission", MissionDilemmaType = "[P]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        static Ctx MakeSpace(int shields, params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Crystalline Entity", Type = "Dilemma", MissionDilemmaType = "[S/P]" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            ShipShields = shields,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var med = P("Med One", "MEDICAL", "MEDICAL");
        var sci = P("Sci One", "SCIENCE", "SCIENCE");
        var music = P("Musician", "CIVILIAN", "Music");
        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");

        // Planet pass: MEDICAL + SCIENCE -> Overcome +5, no kill, discard
        var passP = Resolve(MakePlanet(med, sci, civ));
        if (passP.Fate != Fate.Overcome || passP.StopTeam || passP.Score != 5)
            return $"planet pass: expected Overcome Score=5 no stop, got {passP.Fate}/{passP.Score}/stop={passP.StopTeam}";
        if (passP.Kill.Count != 0)
            return "planet pass: should not kill";
        if (passP.DestroyShip)
            return "planet pass: must not destroy ship";
        if (!ShouldRemoveFromSeed(passP.Fate))
            return "planet pass: dilemma should discard";

        // Planet fail: MEDICAL only
        var failMed = Resolve(MakePlanet(med, civ));
        if (failMed.Fate != Fate.EffectAndEnd || !failMed.StopTeam || failMed.Kill.Count != 2)
            return $"planet fail MEDICAL-only: expected EffectAndEnd+Stop kill-all, got {failMed.Fate}/stop={failMed.StopTeam}/kills={failMed.Kill.Count}";
        if (failMed.Score != 0 || failMed.DestroyShip)
            return "planet fail MEDICAL-only: no score, no ship destroy";
        if (!ShouldRemoveFromSeed(failMed.Fate))
            return "planet fail MEDICAL-only: dilemma should discard";

        // Planet fail: SCIENCE only
        var failSci = Resolve(MakePlanet(sci));
        if (failSci.Fate != Fate.EffectAndEnd || !failSci.StopTeam || failSci.Kill.Count != 1)
            return $"planet fail SCIENCE-only: expected EffectAndEnd+Stop kill 1, got {failSci.Fate}/stop={failSci.StopTeam}/kills={failSci.Kill.Count}";
        if (!ShouldRemoveFromSeed(failSci.Fate))
            return "planet fail SCIENCE-only: dilemma should discard";

        // Space pass: Music (shields 0)
        var passMusic = Resolve(MakeSpace(0, music, civ));
        if (passMusic.Fate != Fate.Overcome || passMusic.Score != 5 || passMusic.StopTeam)
            return $"space pass Music: expected Overcome +5 no stop, got {passMusic.Fate}/{passMusic.Score}/stop={passMusic.StopTeam}";
        if (passMusic.Kill.Count != 0 || passMusic.DestroyShip)
            return "space pass Music: no kill, no destroy";
        if (!ShouldRemoveFromSeed(passMusic.Fate))
            return "space pass Music: dilemma should discard";

        // Space pass: SHIELDS>6 (boundary 7) without Music
        var passSh = Resolve(MakeSpace(7, civ));
        if (passSh.Fate != Fate.Overcome || passSh.Score != 5 || passSh.StopTeam || passSh.DestroyShip)
            return $"space pass SHIELDS=7: expected Overcome +5 no stop/destroy, got {passSh.Fate}/{passSh.Score}/stop={passSh.StopTeam}/destroy={passSh.DestroyShip}";
        if (!ShouldRemoveFromSeed(passSh.Fate))
            return "space pass SHIELDS=7: dilemma should discard";

        // Space fail: SHIELDS==6 boundary (not >6), no Music -> kill all personnel, NOT destroy ship
        var failEq6 = Resolve(MakeSpace(6, civ, med));
        if (failEq6.Fate != Fate.EffectAndEnd || !failEq6.StopTeam || failEq6.Kill.Count != 2)
            return $"space fail SHIELDS=6: expected EffectAndEnd+Stop kill-all, got {failEq6.Fate}/stop={failEq6.StopTeam}/kills={failEq6.Kill.Count}";
        if (failEq6.DestroyShip || failEq6.DamageShip)
            return "space fail SHIELDS=6: must kill life aboard, not destroy/damage ship";
        if (!failEq6.KillAllLifeAboardExceptStasis)
            return "space fail SHIELDS=6: KillAllLifeAboardExceptStasis required (Glossary beyond encounter crew)";
        if (failEq6.Score != 0)
            return "space fail SHIELDS=6: should not score";
        if (!ShouldRemoveFromSeed(failEq6.Fate))
            return "space fail SHIELDS=6: dilemma should discard";

        // Space fail: no Music, shields 0
        var failSpace = Resolve(MakeSpace(0, civ));
        if (failSpace.Fate != Fate.EffectAndEnd || !failSpace.StopTeam || failSpace.Kill.Count != 1 || failSpace.DestroyShip)
            return $"space fail: expected EffectAndEnd+Stop kill crew no destroy, got {failSpace.Fate}/stop={failSpace.StopTeam}/kills={failSpace.Kill.Count}/destroy={failSpace.DestroyShip}";
        if (!failSpace.KillAllLifeAboardExceptStasis)
            return "space fail: KillAllLifeAboardExceptStasis required";
        if (!ShouldRemoveFromSeed(failSpace.Fate))
            return "space fail: dilemma should discard";


        // Planet/pass must NOT set all-aboard flag
        if (failMed.KillAllLifeAboardExceptStasis || failSci.KillAllLifeAboardExceptStasis)
            return "planet fail: must not set KillAllLifeAboardExceptStasis";
        if (passMusic.KillAllLifeAboardExceptStasis || passSh.KillAllLifeAboardExceptStasis || passP.KillAllLifeAboardExceptStasis)
            return "pass: must not set KillAllLifeAboardExceptStasis";

        // Empty planet fail: EffectAndEnd + Stop, no kill, discard
        var emptyP = Resolve(MakePlanet());
        if (emptyP.Fate != Fate.EffectAndEnd || !emptyP.StopTeam || emptyP.Kill.Count != 0)
            return $"empty planet: expected EffectAndEnd+Stop no kill, got {emptyP.Fate}/stop={emptyP.StopTeam}/kills={emptyP.Kill.Count}";
        if (!ShouldRemoveFromSeed(emptyP.Fate))
            return "empty planet: dilemma should discard";

        return null;
    }

    /// <summary>DE mini-test for Cytherians. Returns null if OK, else failure reason.</summary>
    public static string? VerifyCytherians()
    {
        static Card P(string name, string cls, string text) => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        static Ctx Make(params Card[] team) => new()
        {
            Dilemma = new Card { Name = "Cytherians", Type = "Dilemma", MissionDilemmaType = "[S]", Points = "15" },
            Mission = new Card { Name = "Test Space", Type = "Mission", MissionDilemmaType = "[S]" },
            Team = team,
            Present = team,
            AttemptingPlayer = 1,
            Rng = new Random(1)
        };

        var civ = P("Civilian", "CIVILIAN", "CIVILIAN");
        var eng = P("Eng One", "ENGINEER", "ENGINEER");

        // Spock #9: AttachAndEnd, crew NOT stopped, Persist Cytherians, no encounter score
        var hit = Resolve(Make(civ, eng));
        if (hit.Fate != Fate.AttachAndEnd)
            return $"encounter: expected AttachAndEnd, got {hit.Fate}";
        if (hit.StopTeam)
            return "encounter: crew must NOT be stopped (Spock #9)";
        if (hit.Persist != PersistKind.Cytherians || hit.Countdown != 0)
            return $"encounter: expected Persist Cytherians countdown 0, got {hit.Persist}/{hit.Countdown}";
        if (hit.Score != 0)
            return "encounter: must not score at encounter (points on arrival)";
        if (hit.DestroyShip || hit.DamageShip || hit.Kill.Count != 0)
            return "encounter: no destroy/damage/kill";
        if (!ShouldRemoveFromSeed(hit.Fate))
            return "encounter: seed removed (placed on ship)";
        if (ShouldAwardScoreOnApply(hit.Score, hit.Fate))
            return "encounter: ShouldAwardScoreOnApply must be false";

        // Empty crew: still place + attempt ends, not stopped
        var empty = Resolve(Make());
        if (empty.Fate != Fate.AttachAndEnd || empty.StopTeam)
            return $"empty: expected AttachAndEnd StopTeam=false, got {empty.Fate}/stop={empty.StopTeam}";
        if (empty.Persist != PersistKind.Cytherians || empty.Countdown != 0 || empty.Score != 0)
            return "empty: expected Cytherians countdown 0, score 0";

        // Host preference: ship
        if (DecideAttachHost(PersistKind.Cytherians) != AttachHostPreference.ShipOrMission)
            return "host: expected ShipOrMission";

        // Far end 12.6 (fixed once in Apply via Dest): more missions that way
        int farRight = RequiredMoveRules.FarEndIndex(1, 5, _ => 1);
        if (farRight != 4)
            return $"FarEnd from=1 count=5: expected 4, got {farRight}";
        int farLeft = RequiredMoveRules.FarEndIndex(3, 5, _ => 1);
        if (farLeft != 0)
            return $"FarEnd from=3 count=5: expected 0, got {farLeft}";
        int tie = RequiredMoveRules.FarEndIndex(2, 5, _ => 1);
        if (tie != -1)
            return $"FarEnd tie from=2 count=5: expected -1, got {tie}";

        return null;
    }
}
