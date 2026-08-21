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
        /// <summary>Nur Timing/Nullify – TimingRules.</summary>
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
        SubspaceSchism,
        TemporalRift,
        TheDevil,
        TheJuggler,
        Transwarp,
        Mindmeld,
        Wormhole,
        AlienGroupie,
        GroupieStop
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

    public static Result Resolve(Card card)
    {
        string n = (card.Name ?? "").Trim();
        return n switch
        {
            "Amanda Rogers" or "Kevin Uxbridge" or "Q2" or "Energy Vortex"
                or "The Devil" or "Hugh" or "Asteroid Sanctuary"
                => new Result
                {
                    Kind = Kind.TimingOnly,
                    Effect = Effect.TimingNullify,
                    Message = "Response/Nullify (TimingRules).",
                    DiscardAfter = !n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
                                   && !n.Equals("Q2", StringComparison.OrdinalIgnoreCase),
                    OutOfPlay = n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
                                || n.Equals("Q2", StringComparison.OrdinalIgnoreCase)
                },

            "Disruptor Overload" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.DisruptorOverload,
                Message = "Zerstört 1 Equipment (zufällig) an Crew/Away Team."
            },
            "Emergency Transporter Armbands" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.EmergencyBeam,
                Message = "Eigenes Personal an Location darf sofort beamen."
            },
            "Escape Pod" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.EscapePod,
                DiscardAfter = false,
                Message = "Nach Schiffsverlust: Crew hier parken (Sandbox-Marker)."
            },
            "Full Planet Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.PlanetScan,
                Message = "Unterste Seed-Karte einer Planet-Mission anschauen (Computer Skill + Geology stoppen)."
            },
            "Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.SpaceScan,
                Message = "Unterste Seed-Karte einer Space-Mission anschauen (Computer Skill + Stellar Cartography)."
            },
            "Life-form Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.LifeformScan,
                Message = "Personal an einer Location prüfen (vereinfacht: Team-Übersicht)."
            },
            "Long-Range Scan" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.LongRangeScan,
                Message = "Karten an Bord eines Schiffs prüfen."
            },
            "Honor Challenge" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.HonorChallenge,
                Message = "Zu Beginn Personnel Battle: Klingon+Honor tötet Treachery."
            },
            "Incoming Message: Federation" or "Incoming Message: Klingon" or "Incoming Message: Romulan"
                => new Result
                {
                    Kind = Kind.AttachShip,
                    Effect = Effect.IncomingMessage,
                    Message = "Schiff muss zur eigenen Facility derselben Affiliation (Sandbox: gestoppt bis Ankunft)."
                },
            "Jaglom Shrek: Information Broker" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.JaglomLook,
                Message = "Gegner-Hand anschauen (Hotseat)."
            },
            "Klingon Death Yell" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.DeathYell,
                Message = "Nach Tod eines Klingon: Punkte (Sandbox: +5)."
            },
            "Klingon Right of Vengeance" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.RightOfVengeance,
                Message = "Nach Tod eines Klingon: Battle initiieren (Hinweis)."
            },
            "Loss of Orbital Stability" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.LossOfOrbit,
                Message = "Schiff orbitiert Planet: kein RANGE bis Zugende; SHIELDS≤4 → Zerstörung nächster Zug."
            },
            "Near-Warp Transport" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.NearWarp,
                Message = "Bis 6 Karten von Schiff zu benachbarter Location beamen."
            },
            "Palor Toff: Alien Trader" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.PalorToff,
                Message = "Tauscht sich gegen Nicht-Personnel aus dem Discard."
            },
            "Particle Fountain" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.ParticleFountain,
                Points = 5,
                Message = "Nach Planet-Solve mit 2 ENGINEER: +5."
            },
            "Ship Seizure" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.ShipSeizure,
                Message = "Eigenes Schiff mit Tractor: leeres gegnerisches Schiff hier discarded."
            },
            "Subspace Interference" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.SubspaceInterference,
                Message = "Nullifiziert Incoming Message / Hail / Subspace Schism."
            },
            "Subspace Schism" => new Result
            {
                Kind = Kind.AttachTable,
                Effect = Effect.SubspaceSchism,
                DiscardAfter = false,
                Message = "Nächster Draw des Ziels: Karte discarded, nächste gezogen."
            },
            "Temporal Rift" => new Result
            {
                Kind = Kind.AttachTable,
                Effect = Effect.TemporalRift,
                DiscardAfter = false,
                Countdown = 3,
                Message = "Zeitort-Marker; Schiff/Dilemma hier (Sandbox)."
            },
            "The Juggler" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.TheJuggler,
                Message = "Draw-Deck eines Spielers neu mischen."
            },
            "Transwarp Conduit" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.Transwarp,
                DiscardAfter = false,
                Message = "RANGE verdoppelt bis Zugende, dann discard."
            },
            "Vulcan Mindmeld" => new Result
            {
                Kind = Kind.AttachTeam,
                Effect = Effect.Mindmeld,
                DiscardAfter = false,
                Message = "Mindmeld-Personal erhält Skills eines anderen bis Zugende."
            },
            "Wormhole" => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.Wormhole,
                Message = "Zwei Wormholes: Schiff relocatiert und stoppt."
            },
            "Alien Groupie" => new Result
            {
                Kind = Kind.AttachTeam,
                Effect = Effect.AlienGroupie,
                DiscardAfter = false,
                Countdown = 3,
                Message = "Nach Planet-Solve: 1 Male gestoppt bis Countdown."
            },
            "Auto-Destruct Sequence" => new Result
            {
                Kind = Kind.AttachShip,
                Effect = Effect.AutoDestruct,
                DiscardAfter = false,
                Countdown = 2,
                Message = "Countdown: Schiff zerstört, andere Schiffe SHIELDS&lt;8 damaged."
            },
            "Crosis" or "Rogue Borg" or "Tachyon Detection Grid"
                or "Distortion of Space/Time Continuum"
                => new Result
                {
                    Kind = Kind.Instant,
                    Effect = Effect.None,
                    Message = $"„{n}“ Premiere-Sandbox: selten / AU-nah – Marker, voller Effekt später."
                },
            _ => new Result
            {
                Kind = Kind.Instant,
                Effect = Effect.None,
                Message = $"Interrupt „{n}“ gespielt (generisch discarded)."
            }
        };
    }
}