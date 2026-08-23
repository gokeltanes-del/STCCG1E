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
        BorgShip
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
    }

    public static Result Resolve(Ctx ctx)
    {
        string n = (ctx.Dilemma.Name ?? "").Trim();
        return n switch
        {
            "Ancient Computer" => Wall(ctx, Skill(ctx, "Computer Skill", 2) || Skill(ctx, "SCIENCE", 3) || Skill(ctx, "ENGINEER", 3),
                "2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER"),
            "Impassable Door" => Wall(ctx, Skill(ctx, "Computer Skill"), "Computer Skill"),
            "Hologram Ruse" => Wall(ctx, Sum(ctx).integ > 30 && Sum(ctx).cunn > 30, "INTEGRITY>30 and CUNNING>30"),
            "Shaka, When the Walls Fell" => Wall(ctx, Skill(ctx, "Diplomacy", 2) && Sum(ctx).cunn > 30, "2 Diplomacy and CUNNING>30"),
            "Wind Dancer" => Wall(ctx,
                Skill(ctx, "Music") || Skill(ctx, "Youth") || ctx.Team.Any(p => Eff(ctx, p).Strength > 9)
                || ctx.Team.Any(p => (p.Name ?? "").Contains("Lwaxana", StringComparison.OrdinalIgnoreCase)),
                "Music OR Youth OR STRENGTH>9 OR Lwaxana Troi"),
            "Matriarchal Society" => Wall(ctx, ctx.Team.Count(IsFemale) >= 2, "mind. 2 Female"),

            "Armus: Skin Of Evil" => KillRandomEnd(ctx, "Armus kills one random Away Team member."),
            "Nausicaans" => UnlessThen(ctx, Sum(ctx).str > 44, KillRandom(ctx),
                "STRENGTH>44", "Nausicaans kill one at random.", discardAlways: true),
            "Rebel Encounter" => Rebel(ctx),
            "Chalnoth" => UnlessScoreOr(ctx,
                Skill(ctx, "SECURITY", 3) || Sum(ctx).str > 40,
                () => PickKill(ctx, opp: true, "Chalnoth: opponent chooses a victim."),
                5, "3 SECURITY or STRENGTH>40"),
            "Archer" => UnlessThen(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
                HighestAttr(ctx), "MEDICAL and SECURITY", "Archer kills highest attribute total.", true),
            "Anaphasic Organism" => UnlessThen(ctx, Skill(ctx, "MEDICAL") && Skill(ctx, "SECURITY"),
                HighestFemale(ctx), "MEDICAL and SECURITY", "Highest female is discarded.", true),
            "El-Adrel Creature" => ElAdrel(ctx),
            "Firestorm" => Firestorm(ctx),
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
            "Nitrium Metal Parasites" => Attach(ctx, PersistKind.Nitrium, 3,
                "Nitrium on ship (countdown 3). Cure: 2 SCIENCE or 2 ENGINEER."),
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
            "Hyper-Aging" => Attach(ctx, PersistKind.HyperAging, 3,
                "Hyper-Aging (quarantine, countdown 3). Cure: 2 MEDICAL + SCIENCE."),
            "REM Fatigue" => Attach(ctx, PersistKind.RemFatigue, 3,
                "REM Fatigue (quarantine, countdown 3). Cure: 3 MEDICAL or dock."),
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

            _ => Fallback(ctx)
        };
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
        return ctx.Team.OrderByDescending(p => TotalAttr(ctx, p)).FirstOrDefault();
    }

    private static Card? HighestFemale(Ctx ctx)
    {
        return ctx.Team.Where(IsFemale).OrderByDescending(p => TotalAttr(ctx, p)).FirstOrDefault();
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
        var r = new Result { Fate = Fate.EffectAndEnd, StopTeam = true, Message = "Firestorm: INTEGRITY<5 sterben." };
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
            $"{victim?.Name ?? "?"} in Stasis (3 Leadership zum Befreien).");
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
            Fate = Fate.EffectAndEnd,
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
        if (Sum(ctx).integ > 32)
            return new Result { Fate = Fate.Overcome, Message = "INTEGRITY>32 – Alien Parasites overcome." };
        return new Result
        {
            Fate = Fate.EffectAndEnd,
            StopTeam = true,
            Message = "Alien Parasites: opponent controls the team until your next turn (sandbox: team stopped)."
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

    public static bool CanCure(PersistKind kind, IEnumerable<Card> present, int owner)
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
            PersistKind.Abduction => Skill(dummy, "Leadership", 3),
            PersistKind.Phased => Skill(dummy, "ENGINEER") && Skill(dummy, "SCIENCE"),
            PersistKind.Scow => Skill(dummy, "ENGINEER", 2), // + tractor: UI prüft extra
            _ => false
        };
    }
}
