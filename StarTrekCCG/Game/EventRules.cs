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
        AntiTime
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
        (c.Type ?? "").Contains("event", StringComparison.OrdinalIgnoreCase);

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
                Message = "One player draws 3 cards. Event discarded."
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
                Message = "On ship: WEAPONS +2 (cumulative)."
            },
            "Metaphasic Shields" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Metaphasic,
                Message = "On ship: SHIELDS +2 per SCIENCE classification."
            },
            "Nutational Shields" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.Nutational,
                Message = "On ship: SHIELDS +2 per ENGINEER classification."
            },
            "Plasma Fire" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.PlasmaFire,
                Message = "On ship: damage at end of controller's turn. Nullify: SECURITY."
            },
            "Warp Core Breach" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.WarpCore,
                Countdown = 1,
                Message = "On ship: destroyed at end of controller's next turn. Nullify: ENGINEER."
            },
            "Spacedock" => new PlayResult
            {
                Place = Place.OnOutpost,
                Persist = Persist.Spacedock,
                Message = "On outpost: docking fully repairs."
            },
            "Atmospheric Ionization" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Ionization,
                Message = "Planet: beam one at a time, max 3 per turn."
            },
            "Distortion Field" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.Distortion,
                Message = "Planet: flips each end of turn. Face-up = no beaming."
            },
            "Holo-Projectors" => new PlayResult
            {
                Place = Place.OnPlanet,
                Persist = Persist.HoloProjectors,
                Message = "Planet: holograms may exist (sandbox marker)."
            },
            "Espionage: Federation on Klingon" => Espionage("FED", "KLI"),
            "Espionage: Klingon on Federation" => Espionage("KLI", "FED"),
            "Espionage: Romulan on Federation" => Espionage("ROM", "FED"),
            "Espionage: Romulan on Klingon" => Espionage("ROM", "KLI"),
            "Q-Net" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.QNet,
                Message = "Between two locations: crossing requires 2 Diplomacy."
            },
            "Subspace Warp Rift" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Rift,
                Message = "Location: flying past = damage; moving again after arrival = damage."
            },
            "Tetryon Field" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Tetryon,
                Message = "Location: no flying past. After arrival, Navigation for further RANGE."
            },
            "Gaps in Normal Space" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Gaps,
                Message = "Span-4 gap: arrival kills 1 personnel (random)."
            },
            "Supernova" => new PlayResult
            {
                Place = Place.OnMission,
                Persist = Persist.Supernova,
                NeedsToxUthat = true,
                Message = "Requires Tox Uthat. Destroys ships/facilities; mission dead."
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
                Message = "Hands revealed (hotseat: both hands visible)."
            },
            "Static Warp Bubble" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.StaticWarp,
                Message = "Opponent discards 1 card at end of their turn."
            },
            "The Traveler: Transcendence" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Traveler,
                Message = "Nullifies Static Warp Bubble. Chosen player draws +1 at end of turn."
            },
            "Telepathic Alien Kidnappers" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Kidnappers,
                Message = "End of turn: name a type; random opponent hand card matching type is discarded."
            },
            "Pattern Enhancers" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.PatternEnhancers,
                Message = "Ignore beam restrictions from dilemmas/events/missions."
            },
            "Red Alert!" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.RedAlert,
                Message = "On table. Next turns: instead of your normal card play you may play up to 5 personnel and/or equipment."
            },
            "Raise the Stakes" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.RaiseStakes,
                Message = "Opponent: you win immediately OR event stays (sandbox: stays on table)."
            },
            "Genetronic Replicator" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "When personnel would die: stop 2 MEDICAL → return them to hand."
            },
            "Where No One Has Gone Before" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Spaceline ends are adjacent."
            },
            "Lore's Fingernail" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Fingernail,
                Message = "Inorganics (except holo) count as [Non] (sandbox marker)."
            },
            "Neural Servo Device" => new PlayResult
            {
                Place = Place.OnShip,
                Persist = Persist.NeuralServo,
                Message = "Non-Aligned ship without 2 SECURITY: control until end of turn (sandbox: stopped)."
            },
            "Anti-Time Anomaly" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.AntiTime,
                Countdown = 3,
                Message = "Countdown 3: then all your personnel into draw deck."
            },
            "Lore Returns" => new PlayResult
            {
                Place = Place.Table,
                Persist = Persist.Table,
                Message = "Rogue Borg commandeer (Premiere sandbox: marker, no full effect)."
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
        Message = $"Espionage: your cards may attempt [{onAff}] missions as [{asAff}]."
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

    public static bool HasSkill(IEnumerable<Card> aboard, string skill, int need = 1)
    {
        int have = 0;
        foreach (var p in aboard.Where(ModifierRules.IsPersonnelCard))
        {
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase)
                    || kv.Key.Contains(skill, StringComparison.OrdinalIgnoreCase))
                    have += kv.Value;
            }
        }
        return have >= need;
    }

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
}