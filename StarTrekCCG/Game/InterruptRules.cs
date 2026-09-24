using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere Interrupts (39). Timing-Nullifier bleiben in TimingRules;
/// hier die aktiven Spiel-Effekte.
/// </summary>
public static class InterruptRules
{
    public enum Kind
    {
        /// <summary>Nur Timing/Nullify â€“ TimingRules.</summary>
        TimingOnly,
        Instant,
        AttachShip,
        AttachTeam,
        AttachTable
    }

    public enum Effect
    {
        None,
        TimingNullify,
        Sanctuary,
        AutoDestruct,
        DisruptorOverload,
        EmergencyBeam,
        EscapePod,
        PlanetScan,
        SpaceScan,
        LifeformScan,
        LongRangeScan,
        HonorChallenge,
        HughCancelBattle,
        IncomingMessage,
        JaglomLook,
        DeathYell,
        RightOfVengeance,
        LossOfOrbit,
        NearWarp,
        PalorToff,
        ParticleFountain,
        ShipSeizure,
        SubspaceInterference,
        Hail,
        SubspaceSchism,
        TemporalRift,
        TheDevil,
        TheJuggler,
        Transwarp,
        Mindmeld,
        Wormhole,
        AlienGroupie,
        GroupieStop,
        RogueBorg,
        Crosis,
        Distortion,
        Tachyon
    }

    public sealed class Result
    {
        public Kind Kind { get; init; }
        public Effect Effect { get; init; }
        public string Message { get; init; } = "";
        public bool DiscardAfter { get; init; } = true;
        public bool OutOfPlay { get; init; }
        public int Countdown { get; init; }
        public int Points { get; init; }
    }

    public static bool IsInterrupt(Card c) =>
        (c.Type ?? "").Contains("interrupt", StringComparison.OrdinalIgnoreCase);

    /// <summary>Kevin Uxbridge / Kevin Uxbridge: Convergence â€” nullify event(s) in play.</summary>
    public static bool IsKevinNullify(Card c) =>
        (c.Name ?? "").StartsWith("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase);

    public static bool IsDevil(Card c) =>
        (c.Name ?? "").Equals("The Devil", StringComparison.OrdinalIgnoreCase);

    public static bool IsHugh(Card? c) => NameIs(c, "Hugh");

    public static bool NameIs(Card? c, string name) =>
        c != null && (c.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase);

    public static bool IsRogueBorg(Card? c) => NameIs(c, "Rogue Borg");
    public static bool IsCrosis(Card? c) => NameIs(c, "Crosis");
    public static bool IsTranswarpConduit(Card? c) => NameIs(c, "Transwarp Conduit");
    public static bool IsAsteroidSanctuary(Card? c) => NameIs(c, "Asteroid Sanctuary");
    public static bool IsDistortionContinuum(Card? c) => NameIs(c, "Distortion of Space/Time Continuum");
    public static bool IsEmergencyTransporterArmbands(Card? c) => NameIs(c, "Emergency Transporter Armbands");
    public static bool IsHonorChallenge(Card? c) => NameIs(c, "Honor Challenge");
    public static bool IsDeathYell(Card? c) => NameIs(c, "Klingon Death Yell");
    public static bool IsRightOfVengeance(Card? c) => NameIs(c, "Klingon Right of Vengeance");
    public static bool IsFullPlanetScan(Card? c) => NameIs(c, "Full Planet Scan");
    public static bool IsTachyonDetectionGrid(Card? c) => NameIs(c, "Tachyon Detection Grid");

    public static bool IsIncomingMessage(Card? c) =>
        (c?.Name ?? "").StartsWith("Incoming Message", StringComparison.OrdinalIgnoreCase);

    public static bool IsSubspaceInterference(Card? c) => NameIs(c, "Subspace Interference");
    public static bool IsHail(Card? c) => NameIs(c, "Hail");
    public static bool IsInterferenceNullifyTarget(Card? c) =>
        IsIncomingMessage(c) || IsHail(c) || IsSubspaceSchism(c);
    public static bool IsSubspaceSchism(Card? c) => NameIs(c, "Subspace Schism");
    public static bool IsAlienGroupie(Card c) =>
    NameIs(c, "Alien Groupie");

    public static bool IsAutoDestruct(Card c) =>
        NameIs(c, "Auto-Destruct Sequence");

    public static bool IsEscapePod(Card? c) => NameIs(c, "Escape Pod");
    public static bool IsWormhole(Card? c) => NameIs(c, "Wormhole");

    public static bool IsShipSeizure(Card? c) => NameIs(c, "Ship Seizure");

    /// <summary>Extract Slice 2: pair gate â€” need two Wormholes in hand to start the first.</summary>
    public static bool CanStartWormholePair(int wormholesInHand) => wormholesInHand >= 2;

    /// <summary>First copy: own exposed (not cloaked) ship.</summary>
    public static bool CanWormholeFirstOnShip(bool isShip, bool ownedByPlayer, bool exposed) =>
        isShip && ownedByPlayer && exposed;

    /// <summary>Second copy: mission or time location (printed rules).</summary>
    public static bool IsWormholeLocationCard(bool isMission, bool isTimeLocation) =>
        isMission || isTimeLocation;

    /// <summary>Affiliation pip this Incoming Message cares about (title after the colon).</summary>
    public enum HughResolveMode
    {
        CancelJustInitiatedBattle,
        BlockBorgShipPulse,
        KillRogueBorgAtLocation,
        Fail
    }

    /// <summary>Extract Slice 4: Hugh resolve priority â€” battle cancel, Borg Ship pulse block, kill Rogue Borg, else fail.</summary>
    public static HughResolveMode DecideHugh(
        bool hasJustInitiatedHughBattleOnStack,
        bool targetIsBorgShipDilemma,
        bool rogueBorgPresentAtResolvedLocation)
    {
        if (hasJustInitiatedHughBattleOnStack) return HughResolveMode.CancelJustInitiatedBattle;
        if (targetIsBorgShipDilemma) return HughResolveMode.BlockBorgShipPulse;
        if (rogueBorgPresentAtResolvedLocation) return HughResolveMode.KillRogueBorgAtLocation;
        return HughResolveMode.Fail;
    }

    public static string? IncomingMessageAffiliation(Card? c)
    {
        string n = c?.Name ?? "";
        int colon = n.IndexOf(':');
        string tail = colon >= 0 ? n[(colon + 1)..].Trim() : n;
        return PlayOnRules.ParseAffiliationIcon("[" + tail + "]")
               ?? PlayOnRules.ParseAffiliationIcon(c?.Text);
    }
    public static bool IsAmandaOrQ2(Card? c) =>
        NameIs(c, "Amanda Rogers") || NameIs(c, "Q2");

    /// <summary>Where the interrupt must be dropped (hand play).</summary>
    public enum PlayTarget
    {
        None,
        Event,
        OwnCrew,
        AnyCrew,
        OwnShip,
        AnyShip
    }

    public static PlayTarget GetPlayTarget(Card card)
    {
        string n = (card.Name ?? "").Trim();
        string t = card.Text ?? "";

        if (IsKevinNullify(card) || IsDevil(card))
            return PlayTarget.Event;

        // Hail OR-mode: play to table (like Jaglom/Juggler), then click-mark two ships.
        // Printed "Plays on any ship…" is the fly-by OR — do not force a ship drop target.
        // Fly-by uses the response window, not GetPlayTarget.
        if (IsHail(card))
            return PlayTarget.None;

        var spec = PlayOnRules.Parse(card);
        var fromText = PlayOnRules.ToInterruptTarget(spec);
        if (fromText != PlayTarget.None)
            return fromText;

        // Cards whose printed line is "Plays toâ€¦" / "Examineâ€¦" without "Plays on".
        if (n.Equals("Emergency Transporter Armbands", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Vulcan Mindmeld", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Alien Groupie", StringComparison.OrdinalIgnoreCase))
            return PlayTarget.OwnCrew;

        if (n.Equals("Disruptor Overload", StringComparison.OrdinalIgnoreCase))
            return PlayTarget.AnyCrew;

        if (n.Equals("Near-Warp Transport", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Distortion of Space/Time Continuum", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Auto-Destruct Sequence", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Full Planet Scan", StringComparison.OrdinalIgnoreCase))
            return PlayTarget.OwnShip;

        if (IsIncomingMessage(card) || IsHugh(card))
            return PlayTarget.AnyShip;

        if (n.Equals("Long-Range Scan", StringComparison.OrdinalIgnoreCase))
            return PlayTarget.AnyShip;

        if (IsWormhole(card))
            return PlayTarget.OwnShip;

        if (IsTranswarpConduit(card))
            return PlayTarget.AnyShip;

        if (IsTachyonDetectionGrid(card))
            return PlayTarget.AnyShip;

        return PlayTarget.None;
    }

    private static bool ContainsIgnore(string text, string needle) =>
        text.Contains(needle, StringComparison.OrdinalIgnoreCase);

    public static bool NeedsDropTarget(Card card) => GetPlayTarget(card) != PlayTarget.None;

    public static Result Resolve(Card card)
    {
        string n = (card.Name ?? "").Trim();
        return n switch
        {
            "Amanda Rogers" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                OutOfPlay = true,
                DiscardAfter = false,
                Message = "Nullifies an Interrupt being played (except Shield-icon). Place Amanda out-of-play."
            },
            "Kevin Uxbridge" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                OutOfPlay = true,
                DiscardAfter = false,
                Message = "Nullifies an Event in play (except Shield-icon or Treaty). Place Kevin out-of-play."
            },
            "Q2" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                OutOfPlay = true,
                DiscardAfter = false,
                Message = "Nullifies Amanda Rogers, Kevin Uxbridge, a [Q] dilemma, or a dilemma with Q in the title. Place Q2 out-of-play."
            },
            "Energy Vortex" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                Message = "While opponent's normal card play is being played: cancel it (card returns to hand). Opponent may play a different card as that play."
            },
            "The Devil" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                Message = "Nullifies Horga'hn OR Wind Dancer OR one Treaty."
            },
            "Hugh" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.HughCancelBattle,
                Message = "Cancel a battle just initiated by a Borg card or Borg Ship dilemma. OR kill all Rogue Borg at one location."
            },
            "Asteroid Sanctuary" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Sanctuary,
                DiscardAfter = false,
                Message = "Plays on your exposed ship (even at start of battle). While your 2 Navigation aboard, cancel any battle initiated against it. Discard at end of turn."
            },

            "Disruptor Overload" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.DisruptorOverload,
                Message = "Plays on a crew or Away Team (not on a facility). Destroys one of that player's non-Shield equipment present (random)."
            },
            "Emergency Transporter Armbands" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.EmergencyBeam,
                Message = "Plays on your crew or Away Team (even in battle or vs an [ETA] dilemma). Your personnel present may immediately beam away. Not while two adversaries are in combat."
            },
            "Escape Pod" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.EscapePod,
                DiscardAfter = false,
                Message = "Just after your ship here is destroyed: relocate its crew(s) atop this card. Later relocate them to your ship here, then discard."
            },
            "Full Planet Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.PlanetScan,
                Message = "Start of turn, your ship with â‰¥2 staffing icons at a planet mission: stop Computer Skill and Geology aboard to examine the bottom seed card here."
            },
            "Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.SpaceScan,
                Message = "Start of turn, your ship with â‰¥2 staffing icons at a space mission: stop Computer Skill and Stellar Cartography aboard to examine the bottom seed card here."
            },
            "Life-form Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.LifeformScan,
                Message = "Examine the cards in your opponent's hand."
            },
            "Long-Range Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.LongRangeScan,
                Message = "Examine the cards aboard a ship (except a ship with Long-Range Scan Shielding)."
            },
            "Honor Challenge" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.HonorChallenge,
                Message = "Start of a personnel battle: each of your Klingons with Honor may kill an opposing personnel present with Treachery. Cumulative."
            },
            _ when IsIncomingMessage(card) && !n.Contains("Attack Authorization", StringComparison.OrdinalIgnoreCase)
                => new Result
                {
                    Kind = Kind.AttachShip,
                    Effect = Effect.IncomingMessage,
                    DiscardAfter = false,
                    Message = "Plays on a matching-affiliation ship; its controller targets one of their matching facilities on this spaceline. Ship must do nothing but move toward it. Nullified on arrival (or if none)."
                },
            "Jaglom Shrek: Information Broker" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.JaglomLook,
                Message = "Examine opponent's draw deck, then replace unshuffled."
            },
            "Klingon Death Yell" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.DeathYell,
                Points = 5,
                Message = "Just after a Klingon with Honor dies (limit one each): score 5 points."
            },
            "Klingon Right of Vengeance" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.RightOfVengeance,
                Message = "Just after a personnel battle where a Klingon died: your Klingons present may immediately attack the same opponents, even without a leader (STRENGTH doubled this battle)."
            },
            "Loss of Orbital Stability" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.LossOfOrbit,
                Message = "Plays on a ship orbiting a planet. NO RANGE until end of turn. If SHIELDS>4, discard this. Otherwise ship destroyed at end of owner's next turn. Cumulative."
            },
            "Near-Warp Transport" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.NearWarp,
                Message = "Beam up to six personnel and/or equipment from your exposed ship with transporters to an adjacent spaceline location."
            },
            "Palor Toff: Alien Trader" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.PalorToff,
                Message = "Exchange this card for any non-Personnel card in your discard pile."
            },
            "Particle Fountain" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.ParticleFountain,
                Points = 5,
                Message = "Just after your Away Team solved a planet mission: if 2 ENGINEER in that Away Team, score 5 points."
            },
            "Ship Seizure" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.ShipSeizure,
                Message = "Plays on your ship with Tractor Beam. Discard another empty exposed ship here."
            },
            "Subspace Interference" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.SubspaceInterference,
                Message = "Nullifies Incoming Message OR Hail OR Subspace Schism."
            },
            "Subspace Schism" => new Result
            {
                Kind = Kind.AttachTable,
                Effect = Effect.SubspaceSchism,
                DiscardAfter = false,
                Message = "When a player would draw a card (limit once every turn): discard that card; they draw the next one."
            },
            "Temporal Rift" => new Result
            {
                Kind = Kind.AttachTable,
                Effect = Effect.TemporalRift,
                DiscardAfter = false,
                Countdown = 2,
                Message = "Plays on table as a universal space time location; relocate one of your exposed ships OR a dilemma here. Counts down only at the start of your turn. When nullified, return that card."
            },
            "The Juggler" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.TheJuggler,
                Message = "Choose any player to re-shuffle the cards in their draw deck."
            },
            "Transwarp Conduit" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Transwarp,
                DiscardAfter = false,
                Message = "Plays on a ship. Its full RANGE is doubled. Discard at end of turn."
            },
            "Vulcan Mindmeld" => new Result
            {
                Kind = Kind.AttachTeam,
                Effect = Effect.Mindmeld,
                DiscardAfter = false,
                Message = "Plays on your Mindmeld personnel. Gains the skills of one of your other personnel present until end of turn, then discard."
            },
            "Wormhole" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.Wormhole,
                Message = "Requires two Wormholes. Play one on your exposed ship and the other at any location (even a time location). Ship relocates there and is stopped."
            },
            "Alien Groupie" => new Result
            {
                Kind = Kind.AttachTeam,
                Effect = Effect.AlienGroupie,
                DiscardAfter = false,
                Countdown = 2,
                Message = "Plays on an Away Team that just solved a planet mission. One male present (random) is stopped until countdown 2 expires."
            },
            "Auto-Destruct Sequence" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.AutoDestruct,
                DiscardAfter = false,
                Countdown = 1,
                Message = "Plays on your ship. When countdown 1 expires, destroy the ship; then damage all other ships present with SHIELDS<8."
            },
            "Rogue Borg" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.RogueBorg,
                DiscardAfter = false,
                Message = "Plays on an occupied ship. X = number of Rogue Borg present; each has STRENGTH X "
                    + "(total XÃ—X). End of every player's turn: that Away Team battles personnel present."
            },
            "Crosis" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Crosis,
                DiscardAfter = false,
                Countdown = 1,
                Message = "Plays on a ship. Doubles STRENGTH of all Rogue Borg present. "
                    + "Discard at start of next turn."
            },
            "Tachyon Detection Grid" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Tachyon,
                DiscardAfter = false,
                Message = "If you control four ships in play (cloaked count): plays on a cloaked ship. Force de-cloak; may not cloak rest of turn. Phased is not cloaked."
            },
            "Distortion of Space/Time Continuum" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Distortion,
                DiscardAfter = false,
                Message = "Unique. Plays on your non-AU ship just after opponent plays an AU card. You may discard this to unstop that ship and crew, OR restore full RANGE, OR unstop an Away Team here."
            },

            // ---------- Alternate Universe ----------
            "Anti-Matter Spread" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Ship battle: opposing WEAPONS âˆ’1 per CUNNING<8 aboard (sandbox marker)."
            },
            "Barclay Transporter Phobia" => new Result
            {
                Kind = Kind.AttachTeam,
                Effect = Effect.None,
                DiscardAfter = false,
                Message = "One personnel refuses transport until cured (Plexing) â€” sandbox attach."
            },
            "Brain Drain" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "One personnel loses skills/CUNNING until EOT (once per turn) OR double Interphasic Plasma."
            },
            "Countermanda" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                Message = "Nullify Telepathic Alien Kidnappers OR suspend Res-Q/Palor Toff and dig discard."
            },
            "Dead in Bed" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Kill one personnel currently in stasis."
            },
            "Destroy Radioactive Garbage Scow" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Discard Scow; kill personnel here not on ship unless Thermal Deflectors (sandbox)."
            },
            "Devidian Foragers" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Two personnel from a discard pile out-of-play; add attrs to one [AU] personnel this turn."
            },
            "Eyes in the Dark" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "While facing a dilemma: if Empathy, add skills/attrs of one random personnel from an opponent ship."
            },
            "Fire Sculptor" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Move Plasma Fire / Warp Core Breach to nearest opponent ship OR melt one discard card OOP."
            },
            "Hail" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.Hail,
                Message = "Ship flying by must stop here OR two ships cannot battle each other this turn."
            },
            "Howard Heirloom Candle" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Double Anaphasic/Empathic Echo OR nullify Coalescent Organism OR prevent morph."
            },
            "Humuhumunukunukuapua'a" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "This turn at location: your Youth CUNNING/STR +4; opponent non-aligned âˆ’4."
            },
            "Incoming Message: Attack Authorization" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.IncomingMessage,
                Message = "Federation ship with Treachery must attack a ship here (ignore if V.I.P. aboard)."
            },
            "Isabella" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.None,
                DiscardAfter = false,
                Countdown = 1,
                Message = "Non-Borg ship at nebula destroyed end of your next turn unless Youth OR kill one Greed."
            },
            "Jamaharon" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "AU interrupt: Jamaharon sandbox (full text later)."
            },
            "Kevin Uxbridge: Convergence" => new Result
            {
                Kind = Kind.TimingOnly,
                Effect = Effect.TimingNullify,
                Message = "Nullify multiple events at one spaceline location (Kevin Convergence)."
            },
            "La Forge Maneuver" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Ship battle maneuver (sandbox): WEAPONS/SHIELDS swing this battle."
            },
            "Latinum Payoff" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Points = 5,
                Message = "Score points with Latinum/Ferengi condition (sandbox +5)."
            },
            "Phaser Burns" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Personnel battle: wounds / STRENGTH reduction (sandbox)."
            },
            "Rescue Captives" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Release captives to hand / outpost (sandbox)."
            },
            "Romulan Ambush" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Start of battle: Romulan ambush bonus (sandbox)."
            },
            "Security Sacrifice" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Sacrifice SECURITY to save another (sandbox)."
            },
            "Seize Wesley" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Capture/stop Wesley Crusher if present (sandbox)."
            },
            "Senior Staff Meeting" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Download / report officers meeting effect (sandbox)."
            },
            "Temporal Narcosis" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Stop personnel affected by time travel / AU (sandbox)."
            },
            "Thine Own Self" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Isolate / relocate one personnel (sandbox)."
            },
            "Vorgon Raiders" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Artifact / Tox Uthat interaction (sandbox)."
            },
            "Vulcan Nerve Pinch" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "Stop one personnel present (Vulcan)."
            },
            "Wolf" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = "AU interrupt Wolf (sandbox marker)."
            },

            _ => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = $"Interrupt \"{n}\" played (generically discarded)."
            }
        };
    }

    // ---------- Ship Seizure (player chooses; not random / first canvas child) ----------

    /// <summary>Play-on host: own ship with Tractor Beam (drop target, not a second picker).</summary>
    public static bool IsLegalShipSeizureTractor(bool isShip, bool ownedByPlayer, bool hasTractorBeam) =>
        isShip && ownedByPlayer && hasTractorBeam;

    /// <summary>
    /// Victim: another ship at same location, empty of personnel, exposed
    /// (undocked / uncloaked / not phased / not landed / not carried).
    /// Owner may be self or opponent ("another").
    /// </summary>
    public static bool IsLegalShipSeizureVictim(
        bool isShip,
        bool isAnotherShip,
        bool sameLocation,
        bool emptyOfPersonnel,
        bool exposed) =>
        isShip && isAnotherShip && sameLocation && emptyOfPersonnel && exposed;

    /// <summary>DE mini-test: Tractor gate + victim gates. Null = OK.</summary>
    public static string? VerifyShipSeizureDecide()
    {
        if (!IsLegalShipSeizureTractor(true, true, true))
            return "own tractor ship should be legal";
        if (IsLegalShipSeizureTractor(true, true, false))
            return "no Tractor Beam should fail";
        if (IsLegalShipSeizureTractor(true, false, true))
            return "opponent ship as tractor host should fail";
        if (!IsLegalShipSeizureVictim(true, true, true, true, true))
            return "empty exposed another at same loc should pass";
        if (IsLegalShipSeizureVictim(true, false, true, true, true))
            return "same ship as tractor host should fail";
        if (IsLegalShipSeizureVictim(true, true, false, true, true))
            return "different location should fail";
        if (IsLegalShipSeizureVictim(true, true, true, false, true))
            return "crewed ship should fail";
        if (IsLegalShipSeizureVictim(true, true, true, true, false))
            return "unexposed (docked/cloaked/etc) should fail";
        return null;
    }
}
