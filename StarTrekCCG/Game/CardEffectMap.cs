using System;
using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere (PR) name → template id. AU rows added later.
/// Behavior = IEffect; parameters = *Rules catalogs.
/// </summary>
public static class CardEffectMap
{
    private static readonly Dictionary<string, string> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ===== Interrupts: nullify / stack =====
            ["Amanda Rogers"] = "nullify-stack",
            ["Q2"] = "nullify-stack",
            ["Kevin Uxbridge"] = "nullify-inplay",
            ["Energy Vortex"] = "nullify-stack",
            ["The Devil"] = "nullify-stack",
            ["Hugh"] = "interrupt-play",
            ["Asteroid Sanctuary"] = "interrupt-play",

            // ===== Interrupts: countdown / attach =====
            ["Crosis"] = "countdown-nextturn",
            ["Auto-Destruct Sequence"] = "countdown-nextturn",
            ["Rogue Borg"] = "countdown-nextturn",
            ["Transwarp Conduit"] = "interrupt-play",
            ["Temporal Rift"] = "interrupt-play",

            // ===== Remaining Premiere interrupts → generic interrupt-play =====
            ["Alien Groupie"] = "interrupt-play",
            ["Disruptor Overload"] = "interrupt-play",
            ["Distortion of Space/Time Continuum"] = "interrupt-play",
            ["Emergency Transporter Armbands"] = "interrupt-play",
            ["Escape Pod"] = "interrupt-play",
            ["Full Planet Scan"] = "interrupt-play",
            ["Honor Challenge"] = "interrupt-play",
            ["Incoming Message: Federation"] = "interrupt-play",
            ["Incoming Message: Klingon"] = "interrupt-play",
            ["Incoming Message: Romulan"] = "interrupt-play",
            ["Jaglom Shrek: Information Broker"] = "interrupt-play",
            ["Klingon Death Yell"] = "interrupt-play",
            ["Klingon Right of Vengeance"] = "interrupt-play",
            ["Life-form Scan"] = "interrupt-play",
            ["Long-Range Scan"] = "interrupt-play",
            ["Loss of Orbital Stability"] = "interrupt-play",
            ["Near-Warp Transport"] = "interrupt-play",
            ["Palor Toff: Alien Trader"] = "interrupt-play",
            ["Particle Fountain"] = "interrupt-play",
            ["Scan"] = "interrupt-play",
            ["Ship Seizure"] = "interrupt-play",
            ["Subspace Interference"] = "interrupt-play",
            ["Subspace Schism"] = "interrupt-play",
            ["Tachyon Detection Grid"] = "interrupt-play",
            ["The Juggler"] = "interrupt-play",
            ["Vulcan Mindmeld"] = "interrupt-play",
            ["Wormhole"] = "interrupt-play",

            // ===== Events: attach-eot =====
            ["Plasma Fire"] = "attach-eot",
            ["Warp Core Breach"] = "attach-eot",

            // ===== Events: ship-mod (on ship / outpost) =====
            ["Bynars Weapon Enhancement"] = "ship-mod",
            ["Metaphasic Shields"] = "ship-mod",
            ["Nutational Shields"] = "ship-mod",
            ["Spacedock"] = "ship-mod",
            ["Neural Servo Device"] = "ship-mod",
            ["Lore Returns"] = "ship-mod",

            // ===== Events: mission-mod / planet =====
            ["Atmospheric Ionization"] = "mission-mod",
            ["Distortion Field"] = "mission-mod",
            ["Holo-Projectors"] = "mission-mod",
            ["Espionage: Federation on Klingon"] = "mission-mod",
            ["Espionage: Klingon on Federation"] = "mission-mod",
            ["Espionage: Romulan on Federation"] = "mission-mod",
            ["Espionage: Romulan on Klingon"] = "mission-mod",
            ["Subspace Warp Rift"] = "mission-mod",
            ["Tetryon Field"] = "mission-mod",
            ["Supernova"] = "mission-mod",

            // ===== Events: spaceline span =====
            ["Gaps in Normal Space"] = "spaceline-span",
            ["Q-Net"] = "spaceline-span",

            // ===== Events: core-permanent (TABLE) =====
            ["Alien Probe"] = "core-permanent",
            ["Goddess of Empathy"] = "core-permanent",
            ["Raise the Stakes"] = "core-permanent",
            ["Static Warp Bubble"] = "core-permanent",
            ["Red Alert!"] = "core-permanent",
            ["The Traveler: Transcendence"] = "core-permanent",
            ["Telepathic Alien Kidnappers"] = "core-permanent",
            ["Lore's Fingernail"] = "core-permanent",
            ["Genetronic Replicator"] = "core-permanent",
            ["Pattern Enhancers"] = "core-permanent",
            ["Where No One Has Gone Before"] = "core-permanent",
            ["Anti-Time Anomaly"] = "core-permanent",
            ["Treaty: Federation/Klingon"] = "core-permanent",
            ["Treaty: Federation/Romulan"] = "core-permanent",
            ["Treaty: Romulan/Klingon"] = "core-permanent",

            // ===== Events: instant =====
            ["Res-Q"] = "instant-event",
            ["Masaka Transformations"] = "instant-event",
            ["Kivas Fajo: Collector"] = "instant-event",

            // ===== Dilemmas: walls =====
            ["Ancient Computer"] = "wall-dilemma",
            ["Impassable Door"] = "wall-dilemma",
            ["Wind Dancer"] = "wall-dilemma",
            ["Shaka, When the Walls Fell"] = "wall-dilemma",
            ["Matriarchal Society"] = "wall-dilemma",
            ["Hologram Ruse"] = "wall-dilemma",

            // ===== Dilemmas: kill / filter =====
            ["Armus: Skin Of Evil"] = "dilemma-kill",
            ["Nausicaans"] = "dilemma-kill",
            ["Rebel Encounter"] = "dilemma-kill",
            ["Chalnoth"] = "dilemma-kill",
            ["Archer"] = "dilemma-kill",
            ["Anaphasic Organism"] = "dilemma-kill",
            ["El-Adrel Creature"] = "dilemma-kill",
            ["Firestorm"] = "dilemma-kill",
            ["Microvirus"] = "dilemma-kill",
            ["Barclay's Protomorphosis Disease"] = "dilemma-kill",
            ["Nagilum"] = "dilemma-kill",
            ["Crystalline Entity"] = "dilemma-kill",

            // ===== Dilemmas: space filter =====
            ["Gravitic Mine"] = "dilemma-space",
            ["Nanites"] = "dilemma-space",
            ["Null Space"] = "dilemma-space",
            ["Microbiotic Colony"] = "dilemma-space",
            ["Cosmic String Fragment"] = "dilemma-space",

            // ===== Dilemmas: attach / persist =====
            ["Birth of \"Junior\""] = "dilemma-attach",
            ["Nitrium Metal Parasites"] = "dilemma-attach",
            ["Tsiolkovsky Infection"] = "dilemma-attach",
            ["Two-Dimensional Creatures"] = "dilemma-attach",
            ["Menthar Booby Trap"] = "dilemma-attach",
            ["Ktarian Game"] = "dilemma-attach",
            ["Radioactive Garbage Scow"] = "dilemma-attach",
            ["Hyper-Aging"] = "dilemma-attach",
            ["REM Fatigue"] = "dilemma-attach",
            ["Alien Abduction"] = "dilemma-attach",
            ["Phased Matter"] = "dilemma-attach",
            ["Cytherians"] = "dilemma-attach",
            ["Borg Ship"] = "dilemma-attach",

            // ===== Dilemmas: unique / relocate / Q =====
            ["Female's Love Interest"] = "dilemma-special",
            ["Male's Love Interest"] = "dilemma-special",
            ["Portal Guard"] = "dilemma-special",
            ["Sarjenka"] = "dilemma-special",
            ["Tarellian Plague Ship"] = "dilemma-special",
            ["Iconian Computer Weapon"] = "dilemma-special",
            ["Alien Parasites"] = "dilemma-special",
            ["Q"] = "dilemma-special",
            ["Temporal Causality Loop"] = "dilemma-special",

            // ===== Artifacts =====
            ["Betazoid Gift Box"] = "artifact-acquire",
            ["Horga'hn"] = "artifact-acquire",
            ["Interphase Generator"] = "artifact-acquire",
            ["Varon-T Disruptor"] = "artifact-acquire",
            ["Kurlan Naiskos"] = "artifact-acquire",
            ["Thought Maker"] = "artifact-acquire",
            ["Tox Uthat"] = "artifact-acquire",
            ["Vulcan Stone of Gol"] = "artifact-acquire",
            ["Time Travel Pod"] = "artifact-acquire",

            // =================================================================
            // Alternate Universe — same templates; new catalog rows later as needed
            // =================================================================

            // AU Events → ship-mod / core-permanent / mission-mod
            ["Baryon Buildup"] = "ship-mod",
            ["Captain's Log"] = "core-permanent",
            ["Engage Shuttle Operations"] = "core-permanent",
            ["Interrogation"] = "core-permanent",
            ["Intruder Force Field"] = "core-permanent",
            ["Klim Dokachin"] = "core-permanent",
            ["Lower Decks"] = "core-permanent",
            ["Mot's Advice"] = "ship-mod",
            ["Particle Scattering Field"] = "ship-mod",
            ["Revolving Door"] = "mission-mod",
            ["Rishon Uxbridge"] = "core-permanent",
            ["The Charybdis"] = "core-permanent",
            ["The Mask of Korgano"] = "ship-mod",
            ["Thermal Deflectors"] = "core-permanent",
            ["Wartime Conditions"] = "core-permanent",
            ["Yellow Alert"] = "core-permanent",

            // AU Interrupts (exact names from Alternate_Universe/cards.json)
            ["Anti-Matter Spread"] = "interrupt-play",
            ["Barclay Transporter Phobia"] = "interrupt-play",
            ["Brain Drain"] = "interrupt-play",
            ["Countermanda"] = "nullify-stack",
            ["Dead in Bed"] = "interrupt-play",
            ["Destroy Radioactive Garbage Scow"] = "interrupt-play",
            ["Devidian Foragers"] = "interrupt-play",
            ["Eyes in the Dark"] = "interrupt-play",
            ["Fire Sculptor"] = "interrupt-play",
            ["Hail"] = "interrupt-play",
            ["Howard Heirloom Candle"] = "interrupt-play",
            ["Humuhumunukunukuapua'a"] = "interrupt-play",
            ["Incoming Message: Attack Authorization"] = "interrupt-play",
            ["Isabella"] = "interrupt-play",
            ["Jamaharon"] = "interrupt-play",
            ["Kevin Uxbridge: Convergence"] = "nullify-inplay",
            ["La Forge Maneuver"] = "interrupt-play",
            ["Latinum Payoff"] = "interrupt-play",
            ["Phaser Burns"] = "interrupt-play",
            ["Rescue Captives"] = "interrupt-play",
            ["Romulan Ambush"] = "interrupt-play",
            ["Security Sacrifice"] = "interrupt-play",
            ["Seize Wesley"] = "interrupt-play",
            ["Senior Staff Meeting"] = "interrupt-play",
            ["Temporal Narcosis"] = "interrupt-play",
            ["Thine Own Self"] = "interrupt-play",
            ["Vorgon Raiders"] = "interrupt-play",
            ["Vulcan Nerve Pinch"] = "interrupt-play",
            ["Wolf"] = "interrupt-play",

            // AU Dilemmas (Fallback in DilemmaRules until AU catalog rows exist)
            ["Alien Labyrinth"] = "wall-dilemma",
            ["Cardassian Trap"] = "dilemma-special",
            ["Coalescent Organism"] = "dilemma-kill",
            ["Conundrum"] = "dilemma-special",
            ["Edo Probe"] = "dilemma-special",
            ["Empathic Echo"] = "dilemma-special",
            ["Ferengi Attack"] = "dilemma-kill",
            ["Frame of Mind"] = "dilemma-special",
            ["Hidden Entrance"] = "wall-dilemma",
            ["Hunter Gangs"] = "dilemma-kill",
            ["Interphasic Plasma Creatures"] = "dilemma-attach",
            ["Malfunctioning Door"] = "wall-dilemma",
            ["Maman Picard"] = "dilemma-special",
            ["Outpost Raid"] = "dilemma-kill",
            ["Parallel Romance"] = "dilemma-attach",
            ["Punishment Zone"] = "dilemma-kill",
            ["Quantum Singularity Lifeforms"] = "dilemma-attach",
            ["Rascals"] = "dilemma-special",
            ["Royale Casino: Blackjack"] = "dilemma-special",
            ["The Gatherers"] = "dilemma-kill",
            ["The Higher... The Fewer"] = "dilemma-special",
            ["Thought Fire"] = "dilemma-kill",
            ["Worshiper"] = "dilemma-special",
            ["Zaldan"] = "dilemma-kill",

            // AU Artifacts
            ["Cryosatellite"] = "artifact-acquire",
            ["Data's Head"] = "artifact-acquire",
            ["Iconian Gateway"] = "artifact-acquire",
            ["Ophidian Cane"] = "artifact-acquire",
            ["Receptacle Stones"] = "artifact-acquire",
            ["Ressikan Flute"] = "artifact-acquire",
            ["Samuel Clemens' Pocketwatch"] = "artifact-acquire",
        };

    public static string? TemplateIdFor(Card? card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.Name)) return null;
        return ByName.TryGetValue(card.Name.Trim(), out var id) ? id : null;
    }

    public static bool IsMapped(Card? card) => TemplateIdFor(card) != null;

    public static IReadOnlyDictionary<string, string> All => ByName;

    public static Dictionary<string, int> CountByTemplate()
    {
        var c = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ByName.Values)
        {
            c.TryGetValue(id, out int n);
            c[id] = n + 1;
        }
        return c;
    }
}