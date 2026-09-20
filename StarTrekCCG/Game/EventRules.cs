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
    public static bool IsGapsInNormalSpace(Card? c) => NameIs(c, "Gaps in Normal Space");
    public static bool IsQNet(Card? c) => NameIs(c, "Q-Net");
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
    public static bool IsLoresFingernail(Card? c) => NameIs(c, "Lore's Fingernail");

    /// <summary>
    /// Glossary: Lore's Fingernail — while in play, ambient for GetAffiliations.
    /// TW refreshes via RefreshTableBuffs / play / nullify.
    /// </summary>
    public static bool FingernailInPlay { get; set; }

    /// <summary>
    /// Glossary: Lore's Fingernail — inorganic (not [Holo]) become Non while in play.
    /// Classic: Soong-type + Exocomps; [Holo] excepted. Detect via IsInorganic + !IsHologram.
    /// </summary>
    public static bool FingernailMakesNon(Card? c) =>
        FingernailInPlay
        && c != null
        && ModifierRules.IsPersonnelCard(c)
        && DilemmaRules.IsInorganic(c)
        && !CardIcons.IsHologram(c);

    public static void SetFingernailInPlay(bool on) => FingernailInPlay = on;
    public static bool IsTravelerTranscendence(Card? c) => NameIs(c, "The Traveler: Transcendence");
    public static bool IsNeuralServo(Card? c) => NameIs(c, "Neural Servo Device");
    /// <summary>Prefer ArtifactRules for ownership; aliases keep Event table scans compiling.</summary>
    public static bool IsToxUthat(Card? c) => ArtifactRules.IsToxUthat(c);
    public static bool IsAntiTimeAnomaly(Card? c) => NameIs(c, "Anti-Time Anomaly");
    public static bool IsTemporalCausalityLoop(Card? c) => NameIs(c, "Temporal Causality Loop");
    public static bool IsHorgahn(Card? c) => ArtifactRules.IsHorgahn(c);
    public static bool IsAlienProbe(Card? c) => NameIs(c, "Alien Probe");
    public static bool IsAtmosphericIonization(Card? c) => NameIs(c, "Atmospheric Ionization");
    public static bool IsDistortionField(Card? c) => NameIs(c, "Distortion Field");
    public static bool IsHoloProjectors(Card? c) => NameIs(c, "Holo-Projectors");
    public static bool IsMobileHoloEmitter(Card? c) =>
        c != null && (
            NameIs(c, "Mobile Holo-Emitter")
            || (c.Name ?? "").Contains("Mobile Holo", StringComparison.OrdinalIgnoreCase)
            || ((c.Name ?? "").Contains("Holo-Emitter", StringComparison.OrdinalIgnoreCase)
                && (c.Name ?? "").Contains("Mobile", StringComparison.OrdinalIgnoreCase)));
    /// <summary>Printed Unique events (Glossary Unique).</summary>
    public static bool IsPrintedUniqueEvent(Card? c) =>
        IsAtmosphericIonization(c) || IsDistortionField(c);
    public static bool NameEquals(Card? c, string? name) =>
        c != null && !string.IsNullOrEmpty(name)
        && (c.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase);

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
            // Glossary: Atmospheric Ionization — Unique; "to/from this planet" includes
            // beams between different planet-vicinities (e.g. landed ship <-> planet facility), not only Ship<->Planet.
            "Atmospheric Ionization" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Ionization,
                Message = "Unique. Plays on a planet. Beam to/from this planet 1 at a time (incl. planet-vicinity beams); max 3 personnel this way per controller per turn. (Glossary: Atmospheric Ionization)"
            },
            // Glossary: Distortion Field — Unique; enters play FACE UP (blocks immediately);
            // EOT each turn flips (even while face-down). "to/from this planet" includes planet-vicinity
            // beams (landed ship <-> planet facility), same as Atmospheric Ionization.
            "Distortion Field" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Distortion,
                Message = "Unique. Plays on a planet face-up (blocks beaming immediately). End of each turn (even while face-down): flip. While face-up: prevents all beaming to/from this planet (incl. planet-vicinity beams). (Glossary: Distortion Field)"
            },
            // Glossary: hologram / Holo-Projectors (Spock Premiere Holo bullet-Soll).
            // Plays on [P]; [Holo] may exist there activated or deactivated.
            // Nullify -> erase only [Holo] at THIS planet that depended on THIS copy
            // (MHE / other enabler protects; other planets untouched). Not a ship Holodeck.
            "Holo-Projectors" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.HoloProjectors,
                Message = "Plays on a planet. [Holo] cards may exist here (activated or deactivated). If nullified, [Holo] that depended on these Holo-Projectors for existence are erased (other enablers protect). (Glossary: hologram / Holo-Projectors)"
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
            // Glossary: Goddess of Empathy — interrupts may not be played (except [Ref]/[Q]/Kevin Uxbridge/Q2),
            // including response/nullify window (Amanda Rogers is NOT excepted).
            "Goddess of Empathy" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Goddess,
                Message = "Plays on table. Interrupt cards (except [Ref], [Q], Kevin Uxbridge, and Q2) cannot be played — including responses/nullify (Amanda Rogers not excepted). (Glossary: Goddess of Empathy)"
            },
            // Glossary: Alien Probe — continuous both hands revealed; hand cards not nullifiable until played;
            // Battle Bridge used tactics NOT affected.
            "Alien Probe" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Probe,
                Message = "Plays on table. Both hands revealed (continuous). Hand cards not nullifiable until played; Battle Bridge tactics unaffected. (Glossary: Alien Probe)"
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
            // Glossary: Lore's Fingernail
            "Lore's Fingernail" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Fingernail,
                Message = "Glossary: Lore's Fingernail — while in play, all inorganics (except [Holo]) "
                          + "become Non-Aligned (effective affiliation Non only; dual toggle off)."
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

    /// <summary>
    /// Glossary: Goddess of Empathy — only [Ref], [Q], Kevin Uxbridge, and Q2 may still be played.
    /// Amanda Rogers is NOT an exception (blocks response/nullify window too).
    /// </summary>
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

    /// <summary>Glossary: Goddess of Empathy — true when HasGoddess blocks this interrupt play (incl. response).</summary>
    public static bool GoddessBlocksInterruptPlay(bool hasGoddess, Card? interrupt)
    {
        if (!hasGoddess || interrupt == null) return false;
        return !IsGoddessException(interrupt);
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
        // Effective skills: printed + classification-as-skill + equipment grants
        // (ModifierRules.ResolvePersonnel). Present should include equipment when callers care.
        var list = aboard?.ToList() ?? new List<Card>();
        int have = 0;
        foreach (var p in list)
        {
            if (!ModifierRules.IsPersonnelCard(p)) continue;
            var ep = ModifierRules.ResolvePersonnel(p, list, owner: 0);
            foreach (var kv in ep.Skills)
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

    /// <summary>Extract Slice 4: Lore Returns apply gates. Null = legal to commandeer.</summary>
    public static string? LoreReturnsDenyReason(
        bool hostIsShip, bool isOpponentShip, bool hasRogueBorg, bool hasAnyPersonnel)
    {
        if (!hostIsShip) return "Lore Returns: target must be a ship.";
        if (!isOpponentShip) return "Lore Returns: must be an opponent's ship.";
        if (!hasRogueBorg) return "Lore Returns: no Rogue Borg aboard.";
        if (hasAnyPersonnel) return "Lore Returns: ship must be empty of personnel.";
        return null;
    }

    /// <summary>Kevin Uxbridge: Convergence — event attached at location (host / host2 / dockable mission).</summary>
    public static bool KevinEventAtLocation(bool hostIsLoc, bool host2IsLoc, bool hostDockableMissionIsLoc) =>
        hostIsLoc || host2IsLoc || hostDockableMissionIsLoc;

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
            // Glossary: Distortion Field (NOT Interrupt Distortion of S/T Continuum)
            Persist.Distortion => "while face-up: no beaming to/from this planet (incl. planet-vicinities); EOT flip",
            Persist.ParticleScatter => "no beaming to/from this ship",
            Persist.Spacedock => "docking here fully repairs",
            Persist.CaptainsLog => "WEAPONS +3 / SHIELDS +3 if matching commander aboard",
            Persist.LoreReturns => "this ship under opponent control",
            Persist.YellowAlert => "ship on Yellow Alert",
            Persist.Thermal => "WEAPONS may not be used",
            Persist.IncomingMessage => "must do nothing but move to the targeted facility on this spaceline",
            // Glossary: Atmospheric Ionization
            Persist.Ionization => "beam to/from this planet 1 at a time; max 3 personnel/controller/turn (planet-vicinities included)",
            // Glossary: Alien Probe
            Persist.Probe => "both hands revealed (hand cards not nullifiable until played; Battle Bridge unaffected)",
            // Glossary: hologram / Holo-Projectors
            Persist.HoloProjectors => "[Holo] may exist here (act/deact); nullify erases dependents of this copy only",
            // Glossary: Lore's Fingernail
            Persist.Fingernail => "inorganics (except [Holo]) are Non-Aligned while in play; leave/nullify restores",
            _ => ""
        };

        // Name only in DetailName (white). Effect-only summary — one channel (IPG-style).
        _ = card;
        string line = effect ?? "";
        if (countdown > 0)
            line = string.IsNullOrEmpty(line)
                ? $"COUNTER {countdown}"
                : line + $"  ·  COUNTER {countdown}";
        return line;
    }

    private static string ClassShieldsLine(string classification, List<Card> aboard)
    {
        int n = CountClass(aboard, classification);
        int bonus = 2 * n;
        string noun = n == 1 ? classification : classification + "s";
        return $"SHIELDS +{bonus} ({n} {noun} aboard)";
    }

    /// <summary>
    /// Returns the effective MEDICAL skill level of a personnel card (including any equipment grants if present is provided).
    /// </summary>
    public static int GetPersonnelMedicalSkill(Card p, IEnumerable<Card>? present = null, int owner = 0)
    {
        if (!ModifierRules.IsPersonnelCard(p)) return 0;
        var skills = present != null
            ? ModifierRules.ResolvePersonnel(p, present, owner).Skills
            : MissionRules.ParsePersonnelSkills(p);
        int count = 0;
        foreach (var kv in skills)
        {
            if (SkillNameMatches(kv.Key, "MEDICAL"))
                count += kv.Value;
        }
        return count;
    }

    /// <summary>
    /// Checks if a candidate personnel is eligible to be stopped to satisfy Genetronic Replicator.
    /// The personnel targeted to die and any others also targeted to die cannot be used.
    /// Stopped or in-stasis personnel cannot be used.
    /// </summary>
    public static bool IsEligibleForGenetronicStop(
        Card candidate,
        Card victim,
        IEnumerable<Card>? alsoTargetedToDie = null,
        bool isStopped = false,
        bool isInStasis = false,
        IEnumerable<Card>? present = null,
        int owner = 0)
    {
        if (!ModifierRules.IsPersonnelCard(candidate)) return false;
        if (ReferenceEquals(candidate, victim)) return false;
        if (candidate.InstanceId > 0 && victim.InstanceId > 0 && candidate.InstanceId == victim.InstanceId)
            return false;
        if (alsoTargetedToDie != null)
        {
            foreach (var dead in alsoTargetedToDie)
            {
                if (dead == null) continue;
                if (ReferenceEquals(dead, candidate)) return false;
                if (dead.InstanceId > 0 && candidate.InstanceId > 0 && dead.InstanceId == candidate.InstanceId)
                    return false;
            }
        }
        if (isStopped || isInStasis) return false;
        return GetPersonnelMedicalSkill(candidate, present, owner) > 0;
    }

    /// <summary>
    /// Calculates the total available MEDICAL skill to be stopped for Genetronic Replicator
    /// among candidates present, excluding the victim, others also targeted to die, and stopped/disabled cards.
    /// </summary>
    public static int GetAvailableGenetronicMedical(
        IEnumerable<Card> candidates,
        Card victim,
        IEnumerable<Card>? alsoTargetedToDie = null,
        Func<Card, bool>? isStoppedOrDisabled = null,
        IEnumerable<Card>? present = null,
        int owner = 0)
    {
        int total = 0;
        foreach (var c in candidates)
        {
            bool stopped = isStoppedOrDisabled != null && isStoppedOrDisabled(c);
            if (IsEligibleForGenetronicStop(c, victim, alsoTargetedToDie, isStopped: stopped, isInStasis: false, present, owner))
            {
                total += GetPersonnelMedicalSkill(c, present, owner);
            }
        }
        return total;
    }

    /// <summary>
    /// Decides if Genetronic Replicator can save the targeted personnel.
    /// Requires at least 2 MEDICAL present from personnel who are not also targeted to die and not stopped.
    /// </summary>
    public static bool CanGenetronicSave(
        Card victim,
        IEnumerable<Card> candidates,
        IEnumerable<Card>? alsoTargetedToDie = null,
        Func<Card, bool>? isStoppedOrDisabled = null,
        IEnumerable<Card>? present = null,
        int owner = 0)
    {
        return GetAvailableGenetronicMedical(candidates, victim, alsoTargetedToDie, isStoppedOrDisabled, present, owner) >= 2;
    }

    /// <summary>
    /// Self-test verification for Genetronic Replicator rules.
    /// </summary>
    public static string? VerifyGenetronicReplicator()
    {
        var crusher = new Card
        {
            Name = "Beverly Crusher",
            Type = "Personnel",
            Class = "MEDICAL",
            Text = "MEDICAL MEDICAL Biology Exobiology",
            InstanceId = 1
        };
        var ogawa = new Card
        {
            Name = "Alyssa Ogawa",
            Type = "Personnel",
            Class = "MEDICAL",
            Text = "MEDICAL",
            InstanceId = 2
        };
        var selar = new Card
        {
            Name = "Dr. Selar",
            Type = "Personnel",
            Class = "MEDICAL",
            Text = "MEDICAL Exobiology",
            InstanceId = 3
        };
        var riker = new Card
        {
            Name = "William T. Riker",
            Type = "Personnel",
            Class = "OFFICER",
            Text = "OFFICER Leadership Navigation",
            InstanceId = 4
        };
        var toby = new Card
        {
            Name = "Dr. Toby Russell",
            Type = "Personnel",
            Class = "MEDICAL",
            Text = "MEDICAL x2 Physics",
            InstanceId = 5
        };

        // Test 1: Crusher targeted to die with only herself present -> must be false (cannot use self)
        if (CanGenetronicSave(crusher, new[] { crusher, riker }))
            return "Crusher cannot save herself when no other MEDICAL present";

        // Test 2: Crusher targeted to die with only 1 other MEDICAL (Ogawa) -> total 1 < 2 -> false
        if (CanGenetronicSave(crusher, new[] { crusher, ogawa, riker }))
            return "Cannot save Crusher with only 1 other MEDICAL present";

        // Test 3: Crusher targeted to die with 2 other MEDICAL (Ogawa + Selar) -> total 2 >= 2 -> true
        if (!CanGenetronicSave(crusher, new[] { crusher, ogawa, selar, riker }))
            return "Should be able to save Crusher with 2 other MEDICAL present";

        // Test 4: Crusher targeted to die with Toby Russell (MEDICAL x2) -> total 2 >= 2 -> true
        if (!CanGenetronicSave(crusher, new[] { crusher, toby, riker }))
            return "Should be able to save Crusher with 1 other MEDICAL x2 present";

        // Test 5: Crusher and Ogawa both targeted to die (e.g. alsoTargetedToDie contains Ogawa) -> Selar alone is 1 -> false
        if (CanGenetronicSave(crusher, new[] { crusher, ogawa, selar }, alsoTargetedToDie: new[] { ogawa }))
            return "Personnel also targeted to die must not be counted for Genetronic";

        // Test 6: One of the 2 other MEDICAL is stopped -> remaining unstopped 1 < 2 -> false
        if (CanGenetronicSave(crusher, new[] { crusher, ogawa, selar }, isStoppedOrDisabled: c => c.InstanceId == ogawa.InstanceId))
            return "Stopped MEDICAL personnel cannot be used for Genetronic";

        // Test 7: Riker targeted to die, Crusher (2 MEDICAL) present and unstopped -> true
        if (!CanGenetronicSave(riker, new[] { riker, crusher }))
            return "Should be able to save Riker when Crusher is present and not targeted to die";

        return null;
    }

    /// <summary>Glossary: Alien Probe — Standing Practice verify.</summary>
    public static string? VerifyAlienProbe()
    {
        var probe = new Card { Name = "Alien Probe", Type = "Event" };
        var r = ResolvePlay(probe);
        if (!r.Ok) return "Alien Probe: ResolvePlay failed";
        if (r.Place != Place.Table) return "Alien Probe: must play on table";
        if (r.Persist != Persist.Probe) return "Alien Probe: Persist.Probe expected";
        if (!IsAlienProbe(probe)) return "Alien Probe: IsAlienProbe helper";
        if (r.Message.IndexOf("hands revealed", StringComparison.OrdinalIgnoreCase) < 0)
            return "Alien Probe: message must cite hands revealed";
        if (r.Message.IndexOf("Battle Bridge", StringComparison.OrdinalIgnoreCase) < 0)
            return "Alien Probe: message must note Battle Bridge unaffected";
        if (r.Message.IndexOf("nullifiable", StringComparison.OrdinalIgnoreCase) < 0)
            return "Alien Probe: message must note hand cards not nullifiable until played";
        return null;
    }

    /// <summary>Glossary: Atmospheric Ionization — Standing Practice verify.</summary>
    public static string? VerifyAtmosphericIonization()
    {
        var ion = new Card { Name = "Atmospheric Ionization", Type = "Event" };
        var r = ResolvePlay(ion);
        if (!r.Ok) return "Atmospheric Ionization: ResolvePlay failed";
        if (r.Place != Place.OnPlanet) return "Atmospheric Ionization: Plays on Planet";
        if (r.Persist != Persist.Ionization) return "Atmospheric Ionization: Persist.Ionization";
        if (!IsAtmosphericIonization(ion)) return "Atmospheric Ionization: Is* helper";
        if (!IsPrintedUniqueEvent(ion)) return "Atmospheric Ionization: Unique helper";
        if (r.Message.IndexOf("Unique", StringComparison.OrdinalIgnoreCase) < 0)
            return "Atmospheric Ionization: Unique in message";
        if (r.Message.IndexOf("1 at a time", StringComparison.OrdinalIgnoreCase) < 0)
            return "Atmospheric Ionization: 1-at-a-time in message";
        if (r.Message.IndexOf("planet-vicinity", StringComparison.OrdinalIgnoreCase) < 0)
            return "Atmospheric Ionization: Glossary planet-vicinity cite missing";
        if (r.Message.IndexOf("per controller", StringComparison.OrdinalIgnoreCase) < 0)
            return "Atmospheric Ionization: per controller limit";
        return null;
    }

    /// <summary>Glossary: Distortion Field - Standing Practice verify.</summary>
    public static string? VerifyDistortionField()
    {
        var df = new Card { Name = "Distortion Field", Type = "Event" };
        var r = ResolvePlay(df);
        if (!r.Ok) return "Distortion Field: ResolvePlay failed";
        if (r.Place != Place.OnPlanet) return "Distortion Field: Plays on Planet";
        if (r.Persist != Persist.Distortion) return "Distortion Field: Persist.Distortion";
        if (!IsDistortionField(df)) return "Distortion Field: IsDistortionField helper";
        if (!IsPrintedUniqueEvent(df)) return "Distortion Field: Unique helper";
        if (r.Message.IndexOf("Unique", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: Unique in message";
        if (r.Message.IndexOf("face-up", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: enters/blocks face-up in message";
        if (r.Message.IndexOf("flip", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: EOT flip in message";
        if (r.Message.IndexOf("face-down", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: flip even while face-down in message";
        if (r.Message.IndexOf("planet-vicinity", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: Glossary planet-vicinity cite missing";
        if (r.Message.IndexOf("Glossary: Distortion Field", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: Glossary rule cite missing";
        string summary = FormatHostEffectSummary(Persist.Distortion, df, null, 0);
        if (summary.IndexOf("RANGE", StringComparison.OrdinalIgnoreCase) >= 0
            || summary.IndexOf("unstop", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Distortion Field: FormatHostEffectSummary must not confuse with Interrupt Distortion of S/T Continuum";
        if (summary.IndexOf("beaming", StringComparison.OrdinalIgnoreCase) < 0)
            return "Distortion Field: FormatHostEffectSummary must mention beaming block";
        return null;
    }

    /// <summary>Glossary: Goddess of Empathy — Standing Practice verify.</summary>
    public static string? VerifyGoddessOfEmpathy()
    {
        var goddess = new Card { Name = "Goddess of Empathy", Type = "Event" };
        var r = ResolvePlay(goddess);
        if (!r.Ok) return "Goddess: ResolvePlay failed";
        if (r.Place != Place.Table) return "Goddess: must play on table";
        if (r.Persist != Persist.Goddess) return "Goddess: Persist.Goddess expected";
        if (!IsGoddess(goddess)) return "Goddess: IsGoddess helper";
        if (r.Message.IndexOf("Glossary: Goddess of Empathy", StringComparison.OrdinalIgnoreCase) < 0)
            return "Goddess: Glossary rule cite missing";

        var amanda = new Card { Name = "Amanda Rogers", Type = "Interrupt", Icons = "[Shield]" };
        var kevin = new Card { Name = "Kevin Uxbridge", Type = "Interrupt" };
        var q2 = new Card { Name = "Q2", Type = "Interrupt" };
        var energy = new Card { Name = "Energy Vortex", Type = "Interrupt" };
        var qIcon = new Card { Name = "Q-Flash", Type = "Interrupt", Icons = "[Q]" };
        var refIcon = new Card { Name = "Referee", Type = "Interrupt", Icons = "[Ref]" };

        if (IsGoddessException(amanda)) return "Goddess: Amanda Rogers must NOT be exception";
        if (!IsGoddessException(kevin)) return "Goddess: Kevin Uxbridge must be exception";
        if (!IsGoddessException(q2)) return "Goddess: Q2 must be exception";
        if (!IsGoddessException(qIcon)) return "Goddess: [Q] must be exception";
        if (!IsGoddessException(refIcon)) return "Goddess: [Ref] must be exception";
        if (IsGoddessException(energy)) return "Goddess: Energy Vortex must NOT be exception";

        if (!GoddessBlocksInterruptPlay(true, amanda)) return "Goddess: Amanda must be blocked while in play";
        if (GoddessBlocksInterruptPlay(true, kevin)) return "Goddess: Kevin must remain legal";
        if (GoddessBlocksInterruptPlay(true, q2)) return "Goddess: Q2 must remain legal";
        if (GoddessBlocksInterruptPlay(false, amanda)) return "Goddess: no block when Goddess not in play";
        return null;
    }

    // --- Glossary: hologram / Holo-Projectors (Spock Premiere Holo bullet-Soll) ---
    // Activated: Holodeck (ship/fac) OR planet+Projectors OR MHE.
    // Deactivated: any ship/fac OR planet+Projectors OR MHE.
    // Illegal even deact: planet without Projectors/MHE.
    // Illegal voluntary relocate -> deactivate, do NOT complete relocate.
    // Erase if illegally present (planet bare); Projectors nullify dependents (MHE protects);
    // ship destroy -> discard; kill -> deactivate. Same-turn: no reactivate.

    /// <summary>Ship/facility Holodeck enables [Holo] activation aboard only — never planet surface.</summary>
    public static bool HasHolodeck(Card? shipOrFacility) =>
        shipOrFacility != null && MovementRules.ShipHasSpecialEquipment(shipOrFacility, "Holodeck");

    /// <summary>True if present cards include Mobile Holo-Emitter (or name-like).</summary>
    public static bool HasMobileHoloEmitterPresent(IEnumerable<Card>? present) =>
        present != null && present.Any(IsMobileHoloEmitter);

    /// <summary>
    /// Planet surface existence (act or deact): Holo-Projectors and/or MHE / other printed enabler.
    /// Ship Holodeck does NOT enable planet existence.
    /// </summary>
    public static bool HoloMayExistOnPlanet(bool holoProjectorsAtPlanet, bool hasMheOrOtherPrintedEnabler) =>
        holoProjectorsAtPlanet || hasMheOrOtherPrintedEnabler;

    /// <summary>
    /// Aboard ship/facility: deactivated always OK; activated needs Holodeck or MHE.
    /// </summary>
    public static bool HoloMayExistAboard(bool activated, bool holodeckAboard, bool hasMheOrOtherPrintedEnabler) =>
        !activated || holodeckAboard || hasMheOrOtherPrintedEnabler;

    /// <summary>
    /// Where [Holo] may be / stay activated: Holodeck (ship/fac), Projectors (planet), or MHE.
    /// </summary>
    public static bool HoloMayActivateHere(
        bool isPlanetSurface,
        bool projectorsAtPlanet,
        bool holodeckAboard,
        bool hasMheOrOtherPrintedEnabler) =>
        hasMheOrOtherPrintedEnabler
        || (isPlanetSurface ? projectorsAtPlanet : holodeckAboard);

    /// <summary>
    /// Combined exist gate for a destination (voluntary beam/report/move).
    /// </summary>
    public static bool HoloMayExistHere(
        bool activated,
        bool isPlanetSurface,
        bool projectorsAtPlanet,
        bool holodeckAboard,
        bool hasMheOrOtherPrintedEnabler)
    {
        if (isPlanetSurface)
            return HoloMayExistOnPlanet(projectorsAtPlanet, hasMheOrOtherPrintedEnabler);
        return HoloMayExistAboard(activated, holodeckAboard, hasMheOrOtherPrintedEnabler);
    }

    /// <summary>
    /// Voluntary beam/report/move of [Holo] — deny planet without Projectors/MHE;
    /// deny activated aboard without Holodeck/MHE.
    /// </summary>
    public static bool CanVoluntaryRelocateHolo(
        bool isHologram,
        bool activated,
        bool destIsPlanetSurface,
        bool projectorsAtDest,
        bool holodeckAtDest,
        bool mheAtDestOrMoving)
    {
        if (!isHologram) return true;
        return HoloMayExistHere(
            activated, destIsPlanetSurface, projectorsAtDest, holodeckAtDest, mheAtDestOrMoving);
    }

    /// <summary>
    /// Illegal relocate attempt while activated -> deactivate and do not complete relocate.
    /// </summary>
    public static bool IllegalRelocateShouldDeactivate(
        bool isHologram, bool activated, bool destAllowsThisState) =>
        isHologram && activated && !destAllowsThisState;

    /// <summary>
    /// Nullify erase gate: only [Holo] at THIS planet that depended on THIS Projectors copy.
    /// Protected if another Projectors remains, or MHE / other printed enabler is present.
    /// </summary>
    public static bool DependsOnThisHoloProjectorsForExistence(
        bool isHologram,
        bool atPlanetOfTheseProjectors,
        bool otherProjectorsStillAtPlanet,
        bool hasMheOrOtherPrintedEnabler)
    {
        if (!isHologram || !atPlanetOfTheseProjectors) return false;
        if (hasMheOrOtherPrintedEnabler) return false;
        if (otherProjectorsStillAtPlanet) return false;
        return true;
    }

    /// <summary>Kill path: hologram -> deactivate (not discard/erase). Glossary: hologram.</summary>
    public static void DeactivateHologram(Card? holo)
    {
        if (holo == null) return;
        holo.Disabled = true;
    }

    /// <summary>Stuck where [Holo] cannot exist (e.g. bare planet) -> erase (out of play).</summary>
    public static bool ShouldEraseWhenStuckWithoutEnabler(bool isHologram, bool mayExistHere) =>
        isHologram && !mayExistHere;

    /// <summary>Same-turn: after deactivate, may not reactivate until next turn.</summary>
    public static bool MayReactivateHologram(bool deactivatedEarlierThisTurn) =>
        !deactivatedEarlierThisTurn;

    /// <summary>Glossary: hologram / Holo-Projectors — Standing Practice verify.</summary>
    public static string? VerifyHoloProjectors()
    {
        var hp = new Card { Name = "Holo-Projectors", Type = "Event" };
        var r = ResolvePlay(hp);
        if (!r.Ok) return "Holo-Projectors: ResolvePlay failed";
        if (r.Place != Place.OnPlanet) return "Holo-Projectors: Plays on Planet";
        if (r.Persist != Persist.HoloProjectors) return "Holo-Projectors: Persist.HoloProjectors";
        if (!IsHoloProjectors(hp)) return "Holo-Projectors: IsHoloProjectors helper";
        if (r.Message.IndexOf("activated", StringComparison.OrdinalIgnoreCase) < 0
            || r.Message.IndexOf("deactivated", StringComparison.OrdinalIgnoreCase) < 0)
            return "Holo-Projectors: act/deact existence in message";
        if (r.Message.IndexOf("erased", StringComparison.OrdinalIgnoreCase) < 0)
            return "Holo-Projectors: nullify erase in message";
        if (r.Message.IndexOf("Glossary: hologram", StringComparison.OrdinalIgnoreCase) < 0)
            return "Holo-Projectors: Glossary cite missing";

        string summary = FormatHostEffectSummary(Persist.HoloProjectors, hp, null, 0);
        if (summary.IndexOf("Holo", StringComparison.OrdinalIgnoreCase) < 0)
            return "Holo-Projectors: FormatHostEffectSummary must mention Holo exist";
        if (summary.IndexOf("this copy", StringComparison.OrdinalIgnoreCase) < 0
            && summary.IndexOf("depend", StringComparison.OrdinalIgnoreCase) < 0)
            return "Holo-Projectors: FormatHostEffectSummary must note this-copy erase";

        var galaxy = new Card { Name = "Galaxy", Type = "Ship", Text = "Holodeck, Tractor Beam." };
        var noDeck = new Card { Name = "Excelsior", Type = "Ship", Text = "Tractor Beam." };
        if (!HasHolodeck(galaxy)) return "Holo-Projectors: HasHolodeck true for Galaxy";
        if (HasHolodeck(noDeck)) return "Holo-Projectors: HasHolodeck false without Holodeck";

        // Planet: bare deny; Projectors/MHE allow (act or deact)
        if (HoloMayExistOnPlanet(false, false)) return "Holo: planet without enabler must deny exist";
        if (!HoloMayExistOnPlanet(true, false)) return "Holo: Projectors enables planet exist";
        if (!HoloMayExistOnPlanet(false, true)) return "Holo: MHE enables planet exist";

        // Aboard: deact always; act needs Holodeck/MHE
        if (!HoloMayExistAboard(activated: false, holodeckAboard: false, hasMheOrOtherPrintedEnabler: false))
            return "Holo: deactivated must exist aboard any ship/fac";
        if (HoloMayExistAboard(activated: true, holodeckAboard: false, hasMheOrOtherPrintedEnabler: false))
            return "Holo: activated without Holodeck/MHE aboard must deny";
        if (!HoloMayExistAboard(activated: true, holodeckAboard: true, hasMheOrOtherPrintedEnabler: false))
            return "Holo: Holodeck enables activated aboard";
        if (!HoloMayExistAboard(activated: true, holodeckAboard: false, hasMheOrOtherPrintedEnabler: true))
            return "Holo: MHE enables activated aboard";

        if (!HoloMayActivateHere(isPlanetSurface: false, projectorsAtPlanet: false, holodeckAboard: true, hasMheOrOtherPrintedEnabler: false))
            return "Holo: Holodeck = activate aboard";
        if (HoloMayActivateHere(isPlanetSurface: true, projectorsAtPlanet: false, holodeckAboard: true, hasMheOrOtherPrintedEnabler: false))
            return "Holo: Holodeck must NOT activate on planet";
        if (!HoloMayActivateHere(isPlanetSurface: true, projectorsAtPlanet: true, holodeckAboard: false, hasMheOrOtherPrintedEnabler: false))
            return "Holo: Projectors = activate on planet";

        if (CanVoluntaryRelocateHolo(true, activated: false, destIsPlanetSurface: true, projectorsAtDest: false, holodeckAtDest: false, mheAtDestOrMoving: false))
            return "Holo: voluntary beam deact to bare planet must deny";
        if (!CanVoluntaryRelocateHolo(true, activated: false, destIsPlanetSurface: true, projectorsAtDest: true, holodeckAtDest: false, mheAtDestOrMoving: false))
            return "Holo: Projectors allow beam to planet";
        if (!CanVoluntaryRelocateHolo(true, activated: false, destIsPlanetSurface: true, projectorsAtDest: false, holodeckAtDest: false, mheAtDestOrMoving: true))
            return "Holo: MHE allow beam to planet";
        if (CanVoluntaryRelocateHolo(true, activated: true, destIsPlanetSurface: false, projectorsAtDest: false, holodeckAtDest: false, mheAtDestOrMoving: false))
            return "Holo: activated beam to no-Holodeck ship must deny";
        if (!CanVoluntaryRelocateHolo(true, activated: false, destIsPlanetSurface: false, projectorsAtDest: false, holodeckAtDest: false, mheAtDestOrMoving: false))
            return "Holo: deactivated beam to any ship must allow";

        if (!IllegalRelocateShouldDeactivate(true, activated: true, destAllowsThisState: false))
            return "Holo: illegal activated relocate should deactivate";
        if (IllegalRelocateShouldDeactivate(true, activated: false, destAllowsThisState: false))
            return "Holo: already deact illegal relocate — no extra deactivate flag required";

        if (!DependsOnThisHoloProjectorsForExistence(true, true, false, false))
            return "Holo-Projectors: dependent holo at planet must erase on nullify";
        if (DependsOnThisHoloProjectorsForExistence(true, true, false, true))
            return "Holo-Projectors: MHE must protect from erase";
        if (DependsOnThisHoloProjectorsForExistence(true, true, true, false))
            return "Holo-Projectors: other Projectors copy must protect";
        if (DependsOnThisHoloProjectorsForExistence(true, false, false, false))
            return "Holo-Projectors: other planet must not erase";
        if (DependsOnThisHoloProjectorsForExistence(false, true, false, false))
            return "Holo-Projectors: non-holo must not erase";

        var holo = new Card { Name = "Holodoc", Type = "Personnel", Icons = "[Holo]", Disabled = false };
        DeactivateHologram(holo);
        if (!holo.Disabled) return "Holo: kill path deactivates (Disabled)";
        if (!ShouldEraseWhenStuckWithoutEnabler(true, false))
            return "Holo: stuck without enabler -> erase";
        if (ShouldEraseWhenStuckWithoutEnabler(true, true))
            return "Holo: may exist -> do not erase";
        if (MayReactivateHologram(deactivatedEarlierThisTurn: true))
            return "Holo: same-turn no-reactivate";
        if (!MayReactivateHologram(deactivatedEarlierThisTurn: false))
            return "Holo: may reactivate next turn";

        var mhe = new Card { Name = "Mobile Holo-Emitter", Type = "Equipment" };
        if (!IsMobileHoloEmitter(mhe)) return "Holo-Projectors: IsMobileHoloEmitter helper";
        if (!HasMobileHoloEmitterPresent(new[] { holo, mhe }))
            return "Holo-Projectors: HasMobileHoloEmitterPresent";
        return null;
    }


    /// <summary>Glossary: Lore's Fingernail — Standing Practice verify (Premiere smoke).</summary>
    public static string? VerifyLoresFingernail()
    {
        var nail = new Card { Name = "Lore's Fingernail", Type = "Event" };
        var r = ResolvePlay(nail);
        if (!r.Ok) return "Fingernail: ResolvePlay failed";
        if (r.Place != Place.Table) return "Fingernail: must play on table";
        if (r.Persist != Persist.Fingernail) return "Fingernail: Persist.Fingernail expected";
        if (!IsLoresFingernail(nail)) return "Fingernail: IsLoresFingernail helper";
        if (r.Message.IndexOf("Glossary: Lore's Fingernail", StringComparison.OrdinalIgnoreCase) < 0)
            return "Fingernail: Glossary rule cite missing";

        string summary = FormatHostEffectSummary(Persist.Fingernail, nail, null, 0);
        if (summary.IndexOf("Non", StringComparison.OrdinalIgnoreCase) < 0
            && summary.IndexOf("inorganic", StringComparison.OrdinalIgnoreCase) < 0)
            return "Fingernail: FormatHostEffectSummary must mention Non/inorganic";

        static Card Pers(string name, string aff, string chars, string icons = "") => new()
        {
            Name = name,
            Type = "Personnel",
            Affiliation = aff,
            Characteristics = chars,
            Icons = icons,
            Class = "OFFICER",
            Text = "OFFICER",
            IntegrityOrRange = "5",
            CunningOrWeapons = "5",
            StrengthOrShields = "5"
        };

        var data = Pers("Data", "Federation", "Android; Inorganic; Male; Soong-type android;");
        var exo = Pers("Exocomp", "Federation", "Exocomp; Inorganic;");
        var ktesh = Pers("K'Tesh", "Klingon", "Hologram; Inorganic; Male;", "[Holo]");
        var jera = Pers("Jera", "Romulan", "Hologram; Female; Inorganic;", "[Holo]");
        var tomek = Pers("Tomek", "Romulan", "Hologram; Inorganic; Male;", "[Holo]");
        var einstein = Pers("Albert Einstein", "Federation", "Hologram; Inorganic; Male;", "[Holo]");
        var brahms = Pers("Dr. Leah Brahms", "Federation", "Hologram; Female; Inorganic;", "[Holo]");
        var feklhr = Pers("Fek'lhr", "Klingon", "Hologram; Inorganic; Male;", "[Holo]");
        var picard = Pers("Jean-Luc Picard", "Federation", "Human; Male;");

        if (!DilemmaRules.IsInorganic(data)) return "Fingernail: Data must be IsInorganic";
        if (!DilemmaRules.IsInorganic(exo)) return "Fingernail: Exocomp must be IsInorganic";
        if (!DilemmaRules.IsInorganic(ktesh)) return "Fingernail: K'Tesh must be IsInorganic (char)";
        if (DilemmaRules.IsInorganic(picard)) return "Fingernail: Picard must NOT be IsInorganic";
        if (!CardIcons.IsHologram(einstein)) return "Fingernail: Einstein must be IsHologram";
        if (CardIcons.IsHologram(data)) return "Fingernail: Data must NOT be IsHologram";

        SetFingernailInPlay(false);
        if (FingernailMakesNon(data)) return "Fingernail: no effect when not in play";
        var affOff = ReportingRules.GetAffiliations(data);
        if (!affOff.Contains("FED")) return "Fingernail: Data remains FED when nail not in play";
        string liveOff = ReportingRules.FormatLiveAffiliation(data);
        if (liveOff.IndexOf("Federation", StringComparison.OrdinalIgnoreCase) < 0)
            return $"Fingernail: FormatLiveAffiliation off expected Federation, got {liveOff}";


        SetFingernailInPlay(true);
        try
        {
            if (!FingernailMakesNon(data)) return "Fingernail: Data must become Non";
            if (!FingernailMakesNon(exo)) return "Fingernail: Exocomp must become Non";
            if (FingernailMakesNon(ktesh)) return "Fingernail: K'Tesh [Holo] must NOT become Non";
            if (FingernailMakesNon(jera)) return "Fingernail: Jera [Holo] must NOT become Non";
            if (FingernailMakesNon(tomek)) return "Fingernail: Tomek [Holo] must NOT become Non";
            if (FingernailMakesNon(einstein)) return "Fingernail: Einstein [Holo] must NOT become Non";
            if (FingernailMakesNon(brahms)) return "Fingernail: Brahms [Holo] must NOT become Non";
            if (FingernailMakesNon(feklhr)) return "Fingernail: Fek'lhr [Holo] must NOT become Non";
            if (FingernailMakesNon(picard)) return "Fingernail: Picard organic must NOT become Non";

            var affData = ReportingRules.GetAffiliations(data);
            if (affData.Count != 1 || !affData.Contains("NA"))
                return $"Fingernail: Data effective aff must be NA-only, got [{string.Join(",", affData)}]";
            string liveOn = ReportingRules.FormatLiveAffiliation(data);
            if (!string.Equals(liveOn, "Non-Aligned", StringComparison.OrdinalIgnoreCase))
                return $"Fingernail: FormatLiveAffiliation on expected Non-Aligned, got {liveOn}";
            if (ReportingRules.FormatLiveAffiliationBracket(data) != "[Non]")
                return $"Fingernail: Bracket expected [Non], got {ReportingRules.FormatLiveAffiliationBracket(data)}";
            if (DetailStatusRules.FormatFingernailLine().IndexOf("Lore's Fingernail", StringComparison.OrdinalIgnoreCase) < 0)
                return "Fingernail: FormatFingernailLine must name Lore's Fingernail";

            var affK = ReportingRules.GetAffiliations(ktesh);
            if (!affK.Contains("KLI"))
                return $"Fingernail: K'Tesh [Holo] keeps Klingon, got [{string.Join(",", affK)}]";

            var dual = new Card
            {
                Name = "Major Rakal",
                Type = "Personnel",
                Affiliation = "Federation/Romulan",
                Characteristics = "Android; Inorganic; Female;",
                Text = "Federation: OFFICER Diplomacy  Romulan: VIP Treachery",
                CurrentAffiliation = "FED"
            };
            if (!FingernailMakesNon(dual)) return "Fingernail: dual inorganic must be affected";
            if (DualAffiliationRules.TrySetMode(dual, "ROM"))
                return "Fingernail: dual toggle must be off while affected";
            if (DualAffiliationRules.ProfileFor(dual) != null)
                return "Fingernail: ProfileFor must not be active as printed while affected";
            var affDual = ReportingRules.GetAffiliations(dual);
            if (!affDual.Contains("NA") || affDual.Count != 1)
                return $"Fingernail: dual effective NA-only, got [{string.Join(",", affDual)}]";
        }
        finally
        {
            SetFingernailInPlay(false);
        }

        var restore = Pers("Data", "Federation", "Android; Inorganic; Male; Soong-type android;");
        restore.CurrentAffiliation = "FED";
        SetFingernailInPlay(true);
        _ = ReportingRules.GetAffiliations(restore);
        SetFingernailInPlay(false);
        var affRestored = ReportingRules.GetAffiliations(restore);
        if (!affRestored.Contains("FED"))
            return $"Fingernail: after leave restore FED, got [{string.Join(",", affRestored)}]";

        SetFingernailInPlay(true);
        try
        {
            if (ReportingRules.GetAffiliations(data).Contains("FED"))
                return "Fingernail: Data must not remain FED (battle limit lift)";
        }
        finally { SetFingernailInPlay(false); }

        return null;
    }

}
