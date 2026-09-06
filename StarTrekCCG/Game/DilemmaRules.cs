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
        public List<Card> DiscardNonPersonnelFromHand { get; } = new();
        public Card? Relocate { get; set; }
        public PersistKind Persist { get; init; }
        public int Countdown { get; init; }
        public bool DamageShip { get; init; }
        public bool DestroyShip { get; init; }
        public bool DrawForDiscarded { get; init; }
        /// <summary>#1a Alien Parasites fail (planet): beam Away Team back to ship/outpost before stop.</summary>
        public bool BeamBackTeam { get; init; }
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
    }

    public static Result Resolve(Ctx ctx)
    {
        string n = (ctx.Dilemma.Name ?? "").Trim();
        return n switch
        {
            "Ancient Computer" => AncientComputer(ctx),
            "Impassable Door" => Wall(ctx, Skill(ctx, "Computer Skill"), "Computer Skill"),
            "Hologram Ruse" => Wall(ctx, Sum(ctx).integ > 30 && Sum(ctx).cunn > 30, "INTEGRITY>30 and CUNNING>30"),
            "Shaka, When the Walls Fell" => Wall(ctx, Skill(ctx, "Diplomacy", 2) && Sum(ctx).cunn > 30, "2 Diplomacy and CUNNING>30"),
            "Wind Dancer" => Wall(ctx,
                Skill(ctx, "Music") || Skill(ctx, "Youth") || ctx.Team.Any(p => Eff(ctx, p).Strength > 9)
                || ctx.Team.Any(p => (p.Name ?? "").Contains("Lwaxana", StringComparison.OrdinalIgnoreCase)),
                "Music OR Youth OR STRENGTH>9 OR Lwaxana Troi"),
            "Matriarchal Society" => Wall(ctx, ctx.Team.Count(IsFemale) >= 2, "mind. 2 Female"),

            // Printed: "Kills one Away Team member (random selection)." — not a wall; survivors continue.
            "Armus: Skin Of Evil" => KillAndContinue(ctx, "Armus kills one random Away Team member. Discard dilemma."),
            "Nausicaans" => UnlessThen(ctx, Sum(ctx).str > 44, KillRandom(ctx),
                "STRENGTH>44", "Nausicaans kill one at random.", discardAlways: true),
            "Rebel Encounter" => Rebel(ctx),
            "Chalnoth" => UnlessScoreOr(ctx,
                Skill(ctx, "SECURITY", 3) || Sum(ctx).str > 40,
                () => PickKill(ctx, opp: true, "Chalnoth: opponent chooses a victim."),
                5, "3 SECURITY or STRENGTH>40"),
            "Archer" => UnlessThen(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
                HighestAttr(ctx), "MEDICAL and SECURITY", "Archer kills highest attribute total.", true),
            "Anaphasic Organism" => Anaphasic(ctx),
            "El-Adrel Creature" => ElAdrel(ctx),
            "Firestorm" => Firestorm(ctx), // INTEGRITY<5 die; discard; attempt continues
            "Microvirus" => UnlessScoreOr(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
                () => PickKill(ctx, opp: true, "Microvirus: opponent chooses (no inorganic).", exceptInorganic: true),
                5, "MEDICAL and SECURITY"),
            "Barclay's Protomorphosis Disease" => UnlessScoreOr(ctx,
                Skill(ctx, "MEDICAL") && Skill(ctx, "SCIENCE") && Skill(ctx, "SECURITY"),
                () => { foreach (var p in ctx.Team.Where(x => !IsInorganic(x))) AddKill(_tmp, p); },
                10, "MEDICAL, SCIENCE and SECURITY", useTmp: true),
            "Nagilum" => Nagilum(ctx),
            "Crystalline Entity" => Crystalline(ctx),

            "Gravitic Mine" => SpaceUnless(ctx, Skill(ctx, "SCIENCE") && Skill(ctx, "Navigation"),
                damage: true, "SCIENCE and Navigation"),
            "Nanites" => SpaceUnlessScore(ctx, Skill(ctx, "SCIENCE", 2) || Skill(ctx, "Diplomacy"),
                damage: true, score: 5, need: "2 SCIENCE or Diplomacy"),
            "Null Space" => SpaceUnlessScore(ctx, Skill(ctx, "Navigation", 2),
                damage: true, score: 5, need: "2 Navigation"),
            "Microbiotic Colony" => SpaceUnless(ctx,
                Skill(ctx, "OFFICER") && Skill(ctx, "ENGINEER") && Skill(ctx, "SCIENCE"),
                damage: true, "OFFICER, ENGINEER and SCIENCE"),
            "Cosmic String Fragment" => SpaceUnlessScore(ctx,
                Skill(ctx, "ENGINEER") || Skill(ctx, "Astrophysics") || Skill(ctx, "Navigation"),
                destroy: true, score: 5, need: "ENGINEER or Astrophysics or Navigation"),

            "Birth of \"Junior\"" => Attach(ctx, PersistKind.Junior, 0,
                "Junior on ship: RANGE −1 each end of turn; destroyed if RANGE&lt;1. Cure: 3 ENGINEER."),
            "Nitrium Metal Parasites" => NitriumEncounter(ctx),
            "Tsiolkovsky Infection" => Attach(ctx, PersistKind.Tsiolkovsky, 0,
                "Tsiolkovsky: personnel lose first-listed skill. Cure: 3 MEDICAL."),
            "Two-Dimensional Creatures" => Attach(ctx, PersistKind.TwoDim, 0,
                "2D Creatures: Empathy disabled, ship cannot move. Cure: ENGINEER + SCIENCE."),
            "Menthar Booby Trap" => Menthar(ctx),
            "Ktarian Game" => Attach(ctx, PersistKind.Ktarian, 0,
                "Ktarian Game: each start of turn 1 personnel disabled. Cure: CUNNING>30 or Android."),
            "Radioactive Garbage Scow" => new Result
            {
                Fate = Fate.AttachAndEnd,
                Persist = PersistKind.Scow,
                StopTeam = false,
                Message = "Scow auf der Mission: Attempt endet. Mission nicht versuchbar, bis abgeschleppt (Tractor + 2 ENGINEER)."
            },
            "Hyper-Aging" => HyperAgingEncounter(ctx),
            "REM Fatigue" => Attach(ctx, PersistKind.RemFatigue, 4,
                "REM Fatigue (quarantine, countdown 4). Cure: 3 MEDICAL or dock (score points)."),
            "Alien Abduction" => Abduction(ctx),
            "Phased Matter" => Phased(ctx),
            "Cytherians" => Attach(ctx, PersistKind.Cytherians, 0,
                "Cytherians: ship must go to far end of spaceline. There +15, dilemma removed."),
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
            "Iconian Computer Weapon" => Iconian(ctx),
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
            "Quantum Singularity Lifeforms" => Attach(ctx, PersistKind.None, 0,
                "If Romulan ship present: stasis here. Cure: Emergency Transporter Armbands / ENGINEER (sandbox)."),
            "Rascals" => Attach(ctx, PersistKind.None, 0,
                "Up to 4 unique crew become kids (STR 2, Youth). Cure: 2 MEDICAL + Biology."),
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
        var r = Attach(ctx, PersistKind.FrameOfMind, 0,
            $"Frame of Mind on {victim.Name}: Non-Aligned 3-3-3, two skills (opponent's choice). Cure: 3 Empathy.");
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
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = $"Unless fehlgeschlagen ({need})." };
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
                r.Kill.Add(ctx.PickYou?.Invoke("Welches Equipment?", eq) ?? eq[0]);
            return r;
        }
        var x = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Rebel Encounter kills one at random." };
        AddKill(x.Kill, RandomOf(ctx, ctx.Team));
        return x;
    }

    private static Result ElAdrel(Ctx ctx)
    {
        var two = ctx.Team.OrderByDescending(p => Eff(ctx, p).Strength).Take(2).ToList();
        int sum = two.Sum(p => Eff(ctx, p).Strength);
        if (sum > 16)
            return new Result { Fate = Fate.Overcome, Message = $"Two strongest STRENGTH {sum} > 16." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "El-Adrel: one of the two strongest dies." };
        AddKill(r.Kill, RandomOf(ctx, two));
        return r;
    }

    private static Result Firestorm(Ctx ctx)
    {
        if (ctx.ThermalDeflectors)
            return new Result { Fate = Fate.Overcome, Message = "Firestorm nullified (Thermal Deflectors)." };
        var r = new Result
        {
            Fate = Fate.Overcome,
            StopTeam = false,
            Message = "Firestorm: Away Team members with INTEGRITY<5 are killed. Discard dilemma."
        };
        foreach (var p in ctx.Team.Where(p => Eff(ctx, p).Integrity < 5))
            r.Kill.Add(p);
        return r;
    }

    private static Result Nagilum(Ctx ctx)
    {
        if (Skill(ctx, "Diplomacy", 3) || Sum(ctx).str > 40)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = "Nagilum overcome → +5." };
        int n = ctx.Team.Count / 2; // round down
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = $"Nagilum kills {n} (random, half)." };
        var pool = ctx.Team.ToList();
        for (int i = 0; i < n && pool.Count > 0; i++)
        {
            var v = RandomOf(ctx, pool);
            if (v == null) break;
            r.Kill.Add(v);
            pool.Remove(v);
        }
        return r;
    }

    private static Result Crystalline(Ctx ctx)
    {
        bool planet = MissionRules.IsPlanetMission(ctx.Mission);
        bool ok = planet
            ? Skill(ctx, "MEDICAL") && Skill(ctx, "SCIENCE")
            : Skill(ctx, "Music") || ctx.ShipShields > 6;
        if (ok)
            return new Result { Fate = Fate.Overcome, Score = 5, Message = "Crystalline Entity overcome → +5." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = planet ? "Away Team dies." : "Crew dies." };
        r.Kill.AddRange(ctx.Team);
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
            return new Result { Fate = Fate.Overcome, StopTeam = false,
                Message = "Nitrium cured (2 SCIENCE or 2 ENGINEER) - discarded; attempt continues." };
        return AttachContinue(ctx, PersistKind.Nitrium, 2,
            "Nitrium on ship (countdown 2). Cure: 2 SCIENCE or 2 ENGINEER. Attempt continues.");
    }

    private static Result HyperAgingEncounter(Ctx ctx)
    {
        if (CanCure(PersistKind.HyperAging, ctx.Present, ctx.AttemptingPlayer))
            return new Result { Fate = Fate.Overcome, Score = 5, StopTeam = false,
                Message = "Hyper-Aging cured (SCIENCE + 2 MEDICAL) - +5; discarded; attempt continues." };
        return AttachContinue(ctx, PersistKind.HyperAging, 3,
            "Hyper-Aging (quarantine, countdown 3). Cure: SCIENCE + 2 MEDICAL. Attempt continues.");
    }

    private static Result Menthar(Ctx ctx)
    {
        var r = Attach(ctx, PersistKind.Menthar, 0,
            "Menthar: ship cannot move. Cure: 2 ENGINEER. Without MEDICAL, 1 crew dies.");
        if (!Skill(ctx, "MEDICAL"))
            AddKill(r.Kill, RandomOf(ctx, ctx.Team));
        return r;
    }

    private static Result Abduction(Ctx ctx)
    {
        var victim = ctx.Team.OrderByDescending(p => Eff(ctx, p).Cunning).FirstOrDefault();
        var r = Attach(ctx, PersistKind.Abduction, 0,
            $"{victim?.Name ?? "?"} in Stasis (cure: 3 Leadership OR mission completed).");
        r.Relocate = victim;
        return r;
    }

    private static Result Phased(Ctx ctx)
    {
        // größere Hälfte in Stasis – wir markieren ~ceil(n/2) als Relocate (Stasis)
        var r = Attach(ctx, PersistKind.Phased, 0, "Phased Matter: larger group in stasis. Cure: ENGINEER + SCIENCE.");
        var half = (ctx.Team.Count + 1) / 2;
        foreach (var p in ctx.Team.Take(half))
            r.Kill.Add(p); // UI interpretiert Phased Kill als Stasis, nicht Tod – siehe Apply
        return r;
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
            Message = $"{v.Name} wird zum entferntesten anderen Planeten relocatiert."
        };
    }

    private static Result Portal(Ctx ctx)
    {
        bool ok = ctx.Team.Any(p => Eff(ctx, p).Cunning > 7 || Eff(ctx, p).Skills.Keys.Any(k =>
            k.Equals("Honor", StringComparison.OrdinalIgnoreCase)));
        if (ok) return new Result { Fate = Fate.Overcome, Message = "CUNNING>7 or Honor – Portal Guard passed." };
        bool beam = ctx.Confirm?.Invoke("Portal Guard: beam Away Team (YES) or kill all (NO)?") ?? true;
        if (beam)
            return new Result { Fate = Fate.EndAttempt, StopTeam = true, Message = "Away Team must beam (attempt ends)." };
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Portal Guard kills the Away Team." };
        r.Kill.AddRange(ctx.Team);
        return r;
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

    private static Result Iconian(Ctx ctx)
    {
        if (Skill(ctx, "SCIENCE"))
            return new Result { Fate = Fate.Overcome, Message = "SCIENCE present – Iconian Computer Weapon overcome." };
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

    private static Result Parasites(Ctx ctx)
    {
        var plan = DecideAlienParasites(Sum(ctx).integ, MissionRules.IsPlanetMission(ctx.Mission));
        return new Result
        {
            Fate = plan.Fate,
            StopTeam = plan.StopTeam,
            BeamBackTeam = plan.BeamBackTeam,
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
            : new Result { Fate = Fate.WallFailed, StopTeam = true, Message = "Kein Katalog-Eintrag – " + h.Reason };
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
            _ => false
        };
    }

    /// <summary>One-line host-facing effect for ship/mission detail (not full card text).</summary>
    public static string FormatHostEffectSummary(PersistKind kind, Card card, int countdown)
    {
        string effect = kind switch
        {
            PersistKind.Junior => "ENGINEER required ×3 or ship cannot move",
            PersistKind.Scow => "ship cannot move (cure: tractor + 2 ENGINEER)",
            PersistKind.HyperAging => "countdown 3; AT dies if not cured (SCIENCE + MEDICAL×2)",
            PersistKind.RemFatigue => "countdown; crew dies if not cured (MEDICAL×3)",
            PersistKind.Nitrium => "countdown 2; ship destroyed unless SCIENCE×2 or ENGINEER×2",
            PersistKind.Menthar => "ship cannot move (cure: 2 ENGINEER)",
            PersistKind.Tsiolkovsky => "attributes −3 until MEDICAL×3",
            PersistKind.TwoDim => "ship cannot move (ENGINEER + SCIENCE)",
            PersistKind.Cytherians => "must fly toward far end of spaceline",
            PersistKind.Conundrum => "must chase opponent ship",
            PersistKind.EdoProbe => "attempt this mission next or −10",
            PersistKind.FrameOfMind => "personnel is 3-3-3 until 3 Empathy",
            PersistKind.Abduction => "personnel held (cure: Leadership x3 OR mission completed)",
            PersistKind.Phased => "personnel phased (ENGINEER + SCIENCE)",
            PersistKind.Ktarian => "stopped until CUNNING>30 or Android",
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

    // ---- Alien Parasites #1a (Pass/Fail + Beam-back + Stop + Replace; Hotseat-Control PARK) ----

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
        AddKill(r.Kill, victim);
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
        if (pass.Fate != Fate.Overcome || pass.StopTeam || pass.Kill.Count != 0)
            return "pass MED+SEC: expected Overcome, no stop/kill";
        if (!ShouldRemoveFromSeed(pass.Fate))
            return "pass: dilemma should discard";

        // No female: no effect (requires Female)
        var noF = Resolve(Make(worf, data));
        if (noF.Fate != Fate.Overcome || noF.StopTeam || noF.Kill.Count != 0)
            return "no female: expected Overcome no-effect, no stop/kill";

        // Fail (no SECURITY): discard highest female among Beverly(21) vs lowF(11) -> Beverly
        var fail = Resolve(Make(bev, lowF, data));
        if (fail.Fate != Fate.EffectAndEnd || !fail.StopTeam)
            return "fail: expected EffectAndEnd + StopTeam";
        if (fail.Kill.Count != 1 || fail.Kill[0].Name != "Beverly Crusher")
            return $"fail: expected kill Beverly Crusher, got [{string.Join(",", fail.Kill.Select(k => k.Name))}]";
        if (!ShouldRemoveFromSeed(fail.Fate))
            return "fail: dilemma should discard (EffectAndEnd)";

        // Fail (no MEDICAL): SECURITY female present -> Tasha discarded
        var failSec = Resolve(Make(tasha, data));
        if (failSec.Fate != Fate.EffectAndEnd || failSec.Kill.Count != 1 || failSec.Kill[0].Name != "Tasha Yar")
            return "fail no-MEDICAL: expected Tasha Yar discarded";

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
        if (tie.Fate != Fate.EffectAndEnd || !tie.StopTeam || tie.Kill.Count != 1 || tie.Kill[0].Name != "Female B")
            return "tie: expected opp-chosen Female B discarded";
        if (picked?.Name != "Female B")
            return "tie: PickOpp was not used";

        // Sole female still discarded even if lower attrs than males
        var sole = Resolve(Make(lowF, data, worf));
        if (sole.Fate != Fate.EffectAndEnd || sole.Kill.Count != 1 || sole.Kill[0].Name != "Ensign Low")
            return "sole female: expected Ensign Low discarded";

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
}
