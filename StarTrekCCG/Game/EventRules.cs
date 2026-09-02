using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere Events (38). Platzierung + Persist-Kind; UI wendet Effekte an.
/// Treaties bleiben in TreatyRules.
/// </summary>
public static class EventRules
{
    public enum Place
    {
        Table,
        OnShip,
        OnPlanet,
        OnMission,
        OnOutpost,
        Instant
    }

    /// <summary>
    /// UI targeting: rules engine is source of truth; drag and menu both feed the same host.
    /// </summary>
    public enum TargetKind
    {
        None,
        Table,
        Ship,
        PlanetMission,
        Mission,
        Outpost,
        /// <summary>Two consecutive missions in the same quadrant (Gaps, Q-Net).</summary>
        GapBetweenMissions
    }

    public enum Persist
    {
        None,
        Table,
        Bynars,
        Metaphasic,
        Nutational,
        PlasmaFire,
        WarpCore,
        Ionization,
        Distortion,
        Espionage,
        Spacedock,
        QNet,
        Rift,
        Tetryon,
        Gaps,
        Supernova,
        Goddess,
        Probe,
        Traveler,
        StaticWarp,
        Kidnappers,
        PatternEnhancers,
        RedAlert,
        RaiseStakes,
        HoloProjectors,
        Fingernail,
        NeuralServo,
        AntiTime,
        LoreReturns,
        Baryon,
        YellowAlert,
        Klim,
        Thermal,
        CaptainsLog,
        LowerDecks,
        ParticleScatter,
        IntruderField,
        Wartime,
        IncomingMessage
    }

    public sealed class PlayResult
    {
        public bool Ok { get; init; } = true;
        public string Message { get; init; } = "";
        public Place Place { get; init; }
        public Persist Persist { get; init; }
        public bool DiscardAfter { get; init; }
        public int DrawCards { get; init; }
        public bool ResQ { get; init; }
        public bool Masaka { get; init; }
        public bool NeedsToxUthat { get; init; }
        public int Countdown { get; init; }
        public string? EspionageAs { get; init; }
        public string? EspionageOn { get; init; }
    }

    public static bool IsEvent(Card c) =>
        CardKinds.IsEvent(c)
        || (c.Type ?? "").Contains("event", StringComparison.OrdinalIgnoreCase);

    public static bool NameIs(Card? c, string name) =>
        c != null && (c.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase);

    public static bool IsGoddess(Card? c) => NameIs(c, "Goddess of Empathy");
    public static bool IsStaticWarpBubble(Card? c) => NameIs(c, "Static Warp Bubble");
    public static bool IsRedAlert(Card? c) => NameIs(c, "Red Alert!");
    public static bool IsPlasmaFire(Card? c) => NameIs(c, "Plasma Fire");
    public static bool IsWarpCoreBreach(Card? c) => NameIs(c, "Warp Core Breach");
    public static bool IsYellowAlert(Card? c) => NameIs(c, "Yellow Alert");
    public static bool IsThermalDeflectors(Card? c) => NameIs(c, "Thermal Deflectors");
    public static bool IsBaryonBuildup(Card? c) => NameIs(c, "Baryon Buildup");
    public static bool IsKlimDokachin(Card? c) => NameIs(c, "Klim Dokachin");
    public static bool IsCaptainsLog(Card? c) => NameIs(c, "Captain's Log");
    public static bool IsLowerDecks(Card? c) => NameIs(c, "Lower Decks");
    public static bool IsParticleScatteringField(Card? c) => NameIs(c, "Particle Scattering Field");
    public static bool IsIntruderForceField(Card? c) => NameIs(c, "Intruder Force Field");
    public static bool IsWartimeConditions(Card? c) => NameIs(c, "Wartime Conditions");
    public static bool IsKevinConvergence(Card? c) => NameIs(c, "Kevin Uxbridge: Convergence");
    public static bool IsWhereNoOneHasGoneBefore(Card? c) => NameIs(c, "Where No One Has Gone Before");
    public static bool IsPatternEnhancers(Card? c) => NameIs(c, "Pattern Enhancers");
    public static bool IsGenetronicReplicator(Card? c) => NameIs(c, "Genetronic Replicator");
    public static bool IsLoreReturns(Card? c) => NameIs(c, "Lore Returns");
    public static bool IsTravelerTranscendence(Card? c) => NameIs(c, "The Traveler: Transcendence");
    public static bool IsNeuralServo(Card? c) => NameIs(c, "Neural Servo Device");
    /// <summary>Prefer ArtifactRules for ownership; aliases keep Event table scans compiling.</summary>
    public static bool IsToxUthat(Card? c) => ArtifactRules.IsToxUthat(c);
    public static bool IsAntiTimeAnomaly(Card? c) => NameIs(c, "Anti-Time Anomaly");
    public static bool IsTemporalCausalityLoop(Card? c) => NameIs(c, "Temporal Causality Loop");
    public static bool IsHorgahn(Card? c) => ArtifactRules.IsHorgahn(c);

    public static TargetKind GetTargetKind(PlayResult r)
    {
        if (r.Persist is Persist.Gaps or Persist.QNet)
            return TargetKind.GapBetweenMissions;
        return r.Place switch
        {
            Place.Instant => TargetKind.None,
            Place.Table => TargetKind.Table,
            Place.OnShip => TargetKind.Ship,
            Place.OnPlanet => TargetKind.PlanetMission,
            Place.OnMission => TargetKind.Mission,
            Place.OnOutpost => TargetKind.Outpost,
            _ => TargetKind.None
        };
    }

    public static bool NeedsTableHost(TargetKind k) =>
        k is TargetKind.Ship or TargetKind.PlanetMission or TargetKind.Mission
            or TargetKind.Outpost or TargetKind.GapBetweenMissions;

    /// <summary>Event that remains in the TABLE column (not on a ship/mission/gap).</summary>
    public static bool StaysOnTable(Card ev)
    {
        if (!IsEvent(ev)) return false;
        return ResolvePlay(ev).Place == Place.Table;
    }

    /// <summary>Event that attaches to a ship, planet, mission, outpost or spaceline gap.</summary>
    public static bool PlaysOnHost(Card ev)
    {
        if (!IsEvent(ev)) return false;
        return NeedsTableHost(GetTargetKind(ResolvePlay(ev)));
    }

    public static PlayResult ResolvePlay(Card ev)
    {
        string n = (ev.Name ?? "").Trim();
        return n switch
        {
            "Kivas Fajo: Collector" => new PlayResult
            {
                Place = Place.Instant,
                DiscardAfter = true,
                DrawCards = 3,
                Message = "Choose a player to draw three cards. Discard event."
            },
            "Res-Q" => new PlayResult
            {
                Place = Place.Instant,
                DiscardAfter = true,
                ResQ = true,
                Message = "One card from discard to hand. Event discarded."
            },
            "Masaka Transformations" => new PlayResult
            {
                Place = Place.Instant,
                DiscardAfter = true,
                Masaka = true,
                Message = "Hand under draw deck; redraw same number. Event discarded."
            },
            "Bynars Weapon Enhancement" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Bynars,
                Message = "Plays on ship. WEAPONS +2 (cumulative)."
            },
            "Metaphasic Shields" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Metaphasic,
                Message = "Plays on your ship. SHIELDS +2 for each of your SCIENCE-classification personnel present."
            },
            "Nutational Shields" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Nutational,
                Message = "Plays on your ship. SHIELDS +2 for each of your ENGINEER-classification personnel present."
            },
            "Plasma Fire" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.PlasmaFire,
                Message = "Plays on a non-[Bor] ship. End of each of its controller's turns: ship damaged. May be nullified by SECURITY."
            },
            "Warp Core Breach" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.WarpCore,
                Countdown = 1,
                Message = "Plays on a non-[Bor] ship. End of its controller's next turn: ship destroyed. May be nullified by ENGINEER."
            },
            "Spacedock" => new PlayResult
            {
                Place = Place.OnOutpost,
                Persist = Persist.Spacedock,
                Message = "Plays on your outpost. Any of your ships that docks here is fully repaired."
            },
            "Atmospheric Ionization" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Ionization,
                Message = "Plays on a planet (unique). Beam to/from here one at a time; max 3 personnel this way per controller per turn."
            },
            "Distortion Field" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Distortion,
                Message = "Plays on a planet (unique). End of each turn (even face-down): flip. Face-up: no beaming to/from here."
            },
            "Holo-Projectors" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.HoloProjectors,
                Message = "Plays on a planet. Holo cards may exist here. If nullified, holos that depended on this are erased."
            },
            "Espionage: Federation on Klingon" => Espionage("FED", "KLI"),
            "Espionage: Klingon on Federation" => Espionage("KLI", "FED"),
            "Espionage: Romulan on Federation" => Espionage("ROM", "FED"),
            "Espionage: Romulan on Klingon" => Espionage("ROM", "KLI"),
            "Q-Net" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.QNet,
                Message = "Plays between two adjacent spaceline locations. No ship may pass unless 2 Diplomacy aboard."
            },
            "Subspace Warp Rift" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Rift,
                Message = "Ships that fly by here are damaged. Ships that move here are damaged if they move again the same turn (unless relocated)."
            },
            "Tetryon Field" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Tetryon,
                Message = "Ships may not fly by here. Ships that move here need Navigation aboard to use RANGE again this turn."
            },
            "Gaps in Normal Space" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Gaps,
                Message = "Insert as a span-4 space location. When a ship moves here, randomly kill one personnel aboard. If nullified, cards here relocate to an adjacent location."
            },
            "Supernova" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Supernova,
                NeedsToxUthat = true,
                Message = "Requires Tox Uthat. Destroys all ships and facilities here. Mission becomes unattemptable space, loses gametext / points / affiliation icons."
            },
            "Goddess of Empathy" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Goddess,
                Message = "Interrupts (except Ref/Q/Kevin/Q2) may not be played."
            },
            "Alien Probe" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Probe,
                Message = "Plays on table. Players play with their hands revealed."
            },
            "Static Warp Bubble" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.StaticWarp,
                Message = "At the end of each of their turns, opponent must discard a card (their choice)."
            },
            "The Traveler: Transcendence" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Traveler,
                Message = "While in play, nullifies each Static Warp Bubble (they stay on table but have no effect). Chosen player draws +1 at end of turn."
            },
            "Telepathic Alien Kidnappers" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Kidnappers,
                Message = "End of each turn: name a card type, then randomly select a card from opponent's hand; discard it if that type."
            },
            "Pattern Enhancers" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.PatternEnhancers,
                Message = "Ignore dilemma/event/mission effects that prevent beaming or that target your just-beamed personnel or equipment."
            },
            "Red Alert!" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.RedAlert,
                Message = "In place of your normal card play, report up to 5 personnel and/or equipment. "
                          + "Playing this event spends this turn's card play (5-play from next turn). "
                          + "When nullified, any player may immediately download Yellow Alert."
            },
            "Raise the Stakes" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.RaiseStakes,
                Message = "Opponent chooses: you win the game immediately, OR this stays on table "
                          + "(winner may keep one random card from opponent's deck). Cumulative."
            },
            "Genetronic Replicator" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "When a personnel is targeted to die, stop 2 MEDICAL present (not also targeted) to return that personnel to hand instead."
            },
            "Where No One Has Gone Before" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "You may move ships between opposite ends of a spaceline as if those locations were adjacent."
            },
            "Lore's Fingernail" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Fingernail,
                Message = "All inorganics (except holograms) become Non-Aligned."
            },
            "Neural Servo Device" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.NeuralServo,
                Message = "Plays on a Non-Aligned ship without 2 SECURITY. Until end of turn you control ship and crew; they are not compatible with your other cards."
            },
            "Anti-Time Anomaly" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.AntiTime,
                Countdown = 3,
                Message = "Countdown 3. Start of each turn: opponent may flip one ship at any Devron System. "
                          + "When expired, each player shuffles all personnel they own (even uniqueness-only) into their draw deck."
            },
            "Lore Returns" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.LoreReturns,
                Message = "Plays on opponent's empty ship with Rogue Borg aboard. You gain control of those Rogue Borg; "
                    + "they commandeer the ship (Non-Aligned). While Rogue Borg aboard, ship is staffed and may battle / beam."
            },

            // ---------- Alternate Universe ----------
            "Baryon Buildup" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Baryon,
                Message = "Plays on ship. RANGE −2 (cumulative). Nullified at start of your turn if ship is empty and docked at your facility."
            },
            "Captain's Log" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.CaptainsLog,
                Message = "Your ships with matching commander aboard: SHIELDS and WEAPONS +3 (Captain's Order)."
            },
            "Engage Shuttle Operations" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Shuttle takeoff/land/load with Tractor + ENGINEER (sandbox marker)."
            },
            "Interrogation" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "On captive: once per turn 'How many lights?' → points / release (sandbox)."
            },
            "Intruder Force Field" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.IntruderField,
                Message = "Reverses Telepathic Alien Kidnappers affecting you. Rogue Borg need 3+ to invade your ships."
            },
            "Klim Dokachin" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Klim,
                Message = "Opponent loses their regular card draw if they played a unique personnel this turn."
            },
            "Lower Decks" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.LowerDecks,
                Message = "Your non-holographic universal personnel are attributes all +2 (Captain's Order)."
            },
            "Mot's Advice" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Table,
                Message = "On one personnel: gains Barbering while in play (sandbox on host)."
            },
            "Particle Scattering Field" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.ParticleScatter,
                Message = "On your ship with a Particle Scattering Device: no planet beaming here. You may discard this at any time."
            },
            "Revolving Door" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Table,
                Message = "Closes a non-Shield Doorway / Iconian Gateway while face-up; may flip by discard (or nullifies itself)."
            },
            "Rishon Uxbridge" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Plays atop an Event: that event is immune to Kevin Uxbridge."
            },
            "The Charybdis" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Artifacts at completed missions cannot be acquired until Archaeology present."
            },
            "The Mask of Korgano" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Table,
                Message = "On your personnel: toggles [AU] icon while in play."
            },
            "Thermal Deflectors" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Thermal,
                Message = "While in play, nullifies Firestorm, Thought Fire, Plasma Fire, Fire Sculptor, and Phaser Burns."
            },
            "Wartime Conditions" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Wartime,
                Message = "Play only if a Federation ship was attacked. Federation may battle that attacking affiliation at will."
            },
            "Yellow Alert" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.YellowAlert,
                Message = "Cancels and prevents Red Alert! Your personnel each CUNNING +1 (not cumulative). Captain's Order."
            },

            _ when TreatyRules.IsTreatyCard(ev) => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Treaty on table (TreatyRules)."
            },
            _ => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = $"Event \"{n}\" played on table."
            }
        };
    }

    private static PlayResult Espionage(string asAff, string onAff) => new()
    {
        Place = Place.OnMission,
        Persist = Persist.Espionage,
        EspionageAs = asAff,
        EspionageOn = onAff,
        Message = $"Plays on a [{onAff}] mission. Your cards may attempt it as if [{asAff}]. Discard when that mission is solved."
    };

    public static bool IsGoddessException(Card interrupt)
    {
        string n = (interrupt.Name ?? "").Trim();
        if (n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase)) return true;
        if (n.Equals("Q2", StringComparison.OrdinalIgnoreCase)) return true;
        string icons = (interrupt.Icons ?? "") + (interrupt.Type ?? "");
        if (icons.Contains("[Q]", StringComparison.OrdinalIgnoreCase)) return true;
        if (icons.Contains("[Ref]", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static int CountClass(IEnumerable<Card> aboard, string classification)
    {
        int n = 0;
        foreach (var p in aboard.Where(ModifierRules.IsPersonnelCard))
        {
            string cls = (p.Class ?? "").Trim();
            if (cls.Equals(classification, StringComparison.OrdinalIgnoreCase))
                n++;
        }
        return n;
    }

    /// <summary>
    /// Printed "May be nullified by SKILL" on a ship-hosted Premiere event.
    /// SECURITY = Plasma Fire, ENGINEER = Warp Core Breach.
    /// </summary>
    public static string? SkillNullifier(Persist kind) => kind switch
    {
        Persist.PlasmaFire => "SECURITY",
        Persist.WarpCore => "ENGINEER",
        _ => null
    };

    public static bool SkillNameMatches(string have, string need)
    {
        if (string.IsNullOrWhiteSpace(have) || string.IsNullOrWhiteSpace(need)) return false;
        return have.Equals(need, StringComparison.OrdinalIgnoreCase);
    }

    public static int CountSkill(IEnumerable<Card> aboard, string skill)
    {
        int have = 0;
        foreach (var p in aboard)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (SkillNameMatches(kv.Key, skill))
                    have += kv.Value;
            }
        }
        return have;
    }

    public static bool HasSkill(IEnumerable<Card> aboard, string skill, int need = 1) =>
        CountSkill(aboard, skill) >= need;

    public static int WeaponsBonusFromEvents(IEnumerable<(Persist kind, Card ev)> attached)
    {
        int b = 0;
        foreach (var (kind, _) in attached)
            if (kind == Persist.Bynars) b += 2;
        return b;
    }

    public static int ShieldsBonusFromEvents(
        IEnumerable<(Persist kind, Card ev)> attached,
        IEnumerable<Card> aboard)
    {
        int b = 0;
        var list = aboard.ToList();
        foreach (var (kind, _) in attached)
        {
            if (kind == Persist.Metaphasic)
                b += 2 * CountClass(list, "SCIENCE");
            if (kind == Persist.Nutational)
                b += 2 * CountClass(list, "ENGINEER");
        }
        return b;
    }

    /// <summary>
    /// One-line host-facing effect for the ship/facility detail pane.
    /// Shows the live bonus when it depends on who is aboard — not the Persist enum name.
    /// </summary>
    public static string FormatHostEffectSummary(
        Persist kind, Card card, IEnumerable<Card>? aboard, int countdown)
    {
        var list = aboard?.ToList() ?? new List<Card>();
        string effect = kind switch
        {
            Persist.Nutational => ClassShieldsLine("ENGINEER", list),
            Persist.Metaphasic => ClassShieldsLine("SCIENCE", list),
            Persist.Bynars => "WEAPONS +2",
            Persist.PlasmaFire => "damages ship each of controller's EOT (may be nullified by SECURITY)",
            Persist.WarpCore => "destroys ship at end of controller's next turn (may be nullified by ENGINEER)",
            Persist.Baryon => "RANGE −2",
            Persist.NeuralServo => "control of this ship until EOT",
            Persist.Distortion => "RANGE may be used to unstop",
            Persist.ParticleScatter => "no beaming to/from this ship",
            Persist.Spacedock => "docking here fully repairs",
            Persist.CaptainsLog => "WEAPONS +3 / SHIELDS +3 if matching commander aboard",
            Persist.LoreReturns => "this ship under opponent control",
            Persist.YellowAlert => "ship on Yellow Alert",
            Persist.Thermal => "WEAPONS may not be used",
            Persist.IncomingMessage => "must do nothing but move to the targeted facility on this spaceline",
            _ => ""
        };

        string line = "Event: " + card.Name;
        if (!string.IsNullOrEmpty(effect))
            line += " — " + effect;
        if (countdown > 0)
            line += $"  ·  COUNTER {countdown}";
        return line;
    }

    private static string ClassShieldsLine(string classification, List<Card> aboard)
    {
        int n = CountClass(aboard, classification);
        int bonus = 2 * n;
        string noun = n == 1 ? classification : classification + "s";
        return $"SHIELDS +{bonus} ({n} {noun} aboard)";
    }
}