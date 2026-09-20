using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere Artifacts (9): Erwerb beim Encounter + spätere Nutzung.
/// </summary>
public static class ArtifactRules
{
    public enum AcquireKind
    {
        /// <summary>Sofort-Effekt, Artifact wird discarded.</summary>
        ImmediateDiscard,
        /// <summary>Sofort auf den Tisch (permanent).</summary>
        PlaceOnTable,
        /// <summary>In die Hand (später als Event/Interrupt/Equipment spielbar).</summary>
        ToHand,
        /// <summary>Als Equipment am Team / Schiff bleiben.</summary>
        UseAsEquipment
    }

    public sealed class AcquireResult
    {
        public required string Name { get; init; }
        public AcquireKind Kind { get; init; }
        public string Message { get; init; } = "";
        /// <summary>Betazoid Gift Box: Karten vom Draw Deck.</summary>
        public int DownloadFromDraw { get; init; }
        /// <summary>Printed Gift Box: ignore opponent cards that prevent downloading.</summary>
        public bool IgnoreOpponentDownloadPrevention { get; init; }
        /// <summary>Horga'hn: Extra-Normal-Play ODER Extra-Draw am Zugende.</summary>
        public bool GrantsHorgahn { get; init; }
    }

    public static bool IsArtifact(Card c) =>
        CardKinds.IsArtifact(c)
        || (c.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase);

    public static AcquireResult ResolveAcquire(Card artifact)
    {
        string n = (artifact.Name ?? "").Trim();
        return n switch
        {
            "Betazoid Gift Box" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ImmediateDiscard,
                DownloadFromDraw = 3,
                IgnoreOpponentDownloadPrevention = true,
                Message = "Immediately download to hand up to three cards from your draw deck (ignore opponent cards that prevent downloading). Discard artifact."
            },
            "Horga'hn" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.PlaceOnTable,
                GrantsHorgahn = true,
                Message = "Immediately plays on table. Each turn: extra normal card play OR extra card at end of turn."
            },
            "Interphase Generator" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.UseAsEquipment,
                Message = "Use as Equipment. Where present, nullifies [IPG] dilemmas."
            },
            "Varon-T Disruptor" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.UseAsEquipment,
                Message = "Use as Equipment. Doubles the STRENGTH of each of your personnel present."
            },
            "Kurlan Naiskos" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. Plays as Event on a ship: while OFFICER, ENGINEER, MEDICAL, SCIENCE, SECURITY, V.I.P. and CIVILIAN aboard, triples ship's attributes."
            },
            "Thought Maker" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. Name a card type; take all cards of that type from opponent's draw deck, shuffle them, place on bottom of that deck."
            },
            "Tox Uthat" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. As Event on table: you may not play Supernova this turn; discard if used to play Supernova. As Interrupt: nullify Supernova (discard artifact)."
            },
            "Vulcan Stone of Gol" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. Plays as Event on any Away Team: kills all personnel present who do not have Youth or CUNNING>7. Discard artifact."
            },
            "Time Travel Pod" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.PlaceOnTable,
                Message = "Immediately play on table as a universal space time location (countdown 2 while a ship is here). Relocate an opponent's ship here now, OR once relocate your ship here. When discarded, return that ship."
            },
            "Cryosatellite" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ImmediateDiscard,
                Message = "Seed at space. When earned: also earn one additional artifact and up to 3 AU personnel seeded here; then discard Cryosatellite."
            },
            "Data's Head" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.UseAsEquipment,
                Message = "Use as Equipment. CUNNING=10 and Computer Skill. On a ship: RANGE, WEAPONS and SHIELDS +2 (not cumulative)."
            },
            "Iconian Gateway" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. Plays as Event on a planet mission: personnel present may walk to other planet missions."
            },
            "Ophidian Cane" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. As Interrupt: allow 3 through Devidian Door, OR double Devidian Foragers (four personnel), OR double Empathic Touch."
            },
            "Receptacle Stones" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. Plays as Event on crew of an opponent's ship: space dilemmas you encounter this turn also apply to that ship and crew."
            },
            "Ressikan Flute" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.PlaceOnTable,
                Message = "Immediately score X points (X = different Music personnel present, limit 5), then play on table. Points may be nullified by The Devil. Not duplicatable."
            },
            "Samuel Clemens' Pocketwatch" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Place in hand. As Interrupt: one action that must happen on your next turn (e.g. your card draw) happens now instead."
            },
            _ => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = $"Artifact {n} earned → hand."
            }
        };
    }

    public static bool NameIs(Card? c, string name) =>
        c != null && (c.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase);

    // ----- Premiere -----
    public static bool IsBetazoidGiftBox(Card? c) => NameIs(c, "Betazoid Gift Box");
    public static bool IsHorgahn(Card? c) => NameIs(c, "Horga'hn");
    public static bool IsInterphaseGenerator(Card? c) => NameIs(c, "Interphase Generator");

    /// <summary>Printed: where present, nullifies [IPG] dilemmas (icon check, not name list).</summary>
    public static bool CanNullifyIpgDilemma(Card? dilemma, System.Collections.Generic.IEnumerable<Card>? present) =>
        CardIcons.IsIpgDilemma(dilemma)
        && present != null
        && System.Linq.Enumerable.Any(present, IsInterphaseGenerator);
    public static bool IsVaronT(Card? c) => NameIs(c, "Varon-T Disruptor");
    public static bool IsKurlanNaiskos(Card? c) => NameIs(c, "Kurlan Naiskos");
    public static bool IsThoughtMaker(Card? c) => NameIs(c, "Thought Maker");
    public static bool IsToxUthat(Card? c) => NameIs(c, "Tox Uthat");
    public static bool IsVulcanStoneOfGol(Card? c) => NameIs(c, "Vulcan Stone of Gol");

    /// <summary>
    /// Printed: kills personnel without (Youth OR CUNNING>7).
    /// Dies: !Youth AND CUNNING <= 7. Survives: Youth OR CUNNING > 7.
    /// </summary>
    public static bool IsKilledByStoneOfGol(bool hasYouth, int effectiveCunning) =>
        !hasYouth && effectiveCunning <= 7;

    public static bool SurvivesStoneOfGol(bool hasYouth, int effectiveCunning) =>
        !IsKilledByStoneOfGol(hasYouth, effectiveCunning);

    /// <summary>DE mini-test Stone of Gol kill filter. Returns null if OK.</summary>
    public static string? VerifyVulcanStoneOfGol()
    {
        if (!IsKilledByStoneOfGol(hasYouth: false, effectiveCunning: 7))
            return "CUNNING=7 without Youth must die";
        if (!IsKilledByStoneOfGol(hasYouth: false, effectiveCunning: 5))
            return "CUNNING=5 without Youth must die";
        if (IsKilledByStoneOfGol(hasYouth: true, effectiveCunning: 5))
            return "Youth must survive even with low CUNNING";
        if (IsKilledByStoneOfGol(hasYouth: false, effectiveCunning: 8))
            return "CUNNING>7 without Youth must survive";
        if (IsKilledByStoneOfGol(hasYouth: true, effectiveCunning: 7))
            return "Youth + CUNNING=7 must survive";
        if (!SurvivesStoneOfGol(true, 1) || !SurvivesStoneOfGol(false, 8))
            return "SurvivesStoneOfGol mismatch";
        return null;
    }

    public static bool IsTimeTravelPod(Card? c) => NameIs(c, "Time Travel Pod");

    // ----- Alternate Universe -----
    public static bool IsCryosatellite(Card? c) => NameIs(c, "Cryosatellite");
    public static bool IsDatasHead(Card? c) => NameIs(c, "Data's Head");
    public static bool IsIconianGateway(Card? c) => NameIs(c, "Iconian Gateway");
    public static bool IsOphidianCane(Card? c) => NameIs(c, "Ophidian Cane");
    public static bool IsReceptacleStones(Card? c) => NameIs(c, "Receptacle Stones");
    public static bool IsRessikanFlute(Card? c) => NameIs(c, "Ressikan Flute");
    public static bool IsSamuelClemensPocketwatch(Card? c) => NameIs(c, "Samuel Clemens' Pocketwatch");

    /// <summary>Artifacts that grant a lasting table flag after acquire (Horga'hn, Pod, …).</summary>
    public static bool GrantsTablePermanent(Card? c) =>
        IsHorgahn(c) || IsTimeTravelPod(c) || IsCryosatellite(c) || IsRessikanFlute(c);

    /// <summary>Kurlan: alle 7 Classifications an Bord?</summary>
    public static bool KurlanFullyStaffed(IEnumerable<Card> aboard)
    {
        var need = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OFFICER", "ENGINEER", "MEDICAL", "SCIENCE", "SECURITY", "V.I.P.", "CIVILIAN"
        };
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in aboard.Where(ModifierRules.IsPersonnelCard))
        {
            string cls = (p.Class ?? "").Trim();
            if (!string.IsNullOrEmpty(cls))
                have.Add(cls);
            // auch aus Text / Skills
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (need.Contains(kv.Key))
                    have.Add(kv.Key);
            }
        }
        // VIP / CIVILIAN oft als Classification
        foreach (var p in aboard)
        {
            string t = ((p.Class ?? "") + " " + (p.Text ?? "")).ToUpperInvariant();
            if (t.Contains("V.I.P") || t.Contains("VIP")) have.Add("V.I.P.");
            if (t.Contains("CIVILIAN")) have.Add("CIVILIAN");
        }
        return need.All(n => have.Any(h =>
            h.Equals(n, StringComparison.OrdinalIgnoreCase)
            || (n == "V.I.P." && h.Contains("VIP", StringComparison.OrdinalIgnoreCase))));
    }
// ---- Extract Slice 6: ApplyArtifactAcquire placement gates (no WPF) ----

    public enum AcquirePlacement
    {
        ImmediateDiscard,
        PlaceOnTable,
        EquipmentOnPlanetMission,
        EquipmentPreferOwnShip,
        ToHand
    }

    /// <summary>Where an earned artifact goes after ResolveAcquire.Kind.</summary>
    public static AcquirePlacement DecideAcquirePlacement(AcquireKind kind, bool isPlanetMission) =>
        kind switch
        {
            AcquireKind.ImmediateDiscard => AcquirePlacement.ImmediateDiscard,
            AcquireKind.PlaceOnTable => AcquirePlacement.PlaceOnTable,
            AcquireKind.UseAsEquipment => isPlanetMission
                ? AcquirePlacement.EquipmentOnPlanetMission
                : AcquirePlacement.EquipmentPreferOwnShip,
            _ => AcquirePlacement.ToHand
        };

    public static bool ShouldDownloadOnAcquire(AcquireResult acq) =>
        acq.Kind == AcquireKind.ImmediateDiscard && acq.DownloadFromDraw > 0;

    /// <summary>DE mini-test Gift Box acquire flags. Returns null if OK.</summary>
    public static string? VerifyBetazoidGiftBox()
    {
        var c = new Card { Name = "Betazoid Gift Box", Type = "Artifact" };
        var acq = ResolveAcquire(c);
        if (acq.Kind != AcquireKind.ImmediateDiscard)
            return "Gift Box must ImmediateDiscard";
        if (acq.DownloadFromDraw != 3)
            return "Gift Box DownloadFromDraw must be 3";
        if (!acq.IgnoreOpponentDownloadPrevention)
            return "Gift Box must IgnoreOpponentDownloadPrevention";
        if (!ShouldDownloadOnAcquire(acq))
            return "ShouldDownloadOnAcquire false for Gift Box";
        if (DecideAcquirePlacement(acq.Kind, isPlanetMission: true) != AcquirePlacement.ImmediateDiscard)
            return "Placement must ImmediateDiscard";
        return null;
    }
}
