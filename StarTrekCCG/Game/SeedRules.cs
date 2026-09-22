using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Seed-phase legality (missions, dilemmas, artifacts, facilities, Cryosatellite).
/// Shared by TableWindow, Quick Game, and later LegalMoves / AI — no Borders here.
/// </summary>
public enum SeedSubPhase
{
    Doorway = 0,
    Mission = 1,
    Dilemma = 2,
    Facility = 3,
    Done = 4
}

public static class SeedRules
{
    public const int CryosatellitePersonnelMax = 3;

    public static string PhaseLabel(SeedSubPhase phase) => phase switch
    {
        SeedSubPhase.Doorway => "Doorway",
        SeedSubPhase.Mission => "Mission",
        SeedSubPhase.Dilemma => "Dilemma",
        SeedSubPhase.Facility => "Facility",
        SeedSubPhase.Done => "Done",
        _ => "?"
    };

    public static SeedSubPhase FromInt(int value) =>
        Enum.IsDefined(typeof(SeedSubPhase), value) ? (SeedSubPhase)value : SeedSubPhase.Doorway;

    /// <summary>Type gate for the current seed sub-phase (no mission target yet).</summary>
    public static bool BelongsInPhase(Card card, SeedSubPhase phase)
    {
        if (card == null) return false;
        return phase switch
        {
            SeedSubPhase.Doorway => CardKinds.IsDoorway(card),
            SeedSubPhase.Mission => CardKinds.IsMission(card) || CardKinds.IsTimeLocation(card),
            SeedSubPhase.Dilemma => CardKinds.IsDilemma(card)
                                    || CardKinds.IsArtifact(card)
                                    || IsAuPersonnelForCryosatellite(card),
            SeedSubPhase.Facility => CardKinds.IsFacility(card)
                                     || CardKinds.IsShip(card)
                                     || CardKinds.IsEvent(card)
                                     || CardKinds.IsPersonnel(card)
                                     || CardKinds.IsIncident(card)
                                     || CardKinds.IsObjective(card)
                                     || CardKinds.IsDoorway(card),
            _ => false
        };
    }

    /// <summary>
    /// Full seed-play gate used by LegalMoves / EngineAuthority.
    /// <paramref name="mission"/> is required under Dilemma (and for facilities/ships).
    /// </summary>
    public static (bool ok, string reason) CanSeedNow(
        Card card, SeedSubPhase phase, Card? mission, int cryoPersonnelAlready)
    {
        if (card == null) return (false, "No card.");
        if (phase == SeedSubPhase.Done) return (false, "Seed is finished.");

        if (!BelongsInPhase(card, phase) && phase != SeedSubPhase.Facility)
            return (false, $"{card.Name} does not belong in the {PhaseLabel(phase)} seed phase.");

        return phase switch
        {
            SeedSubPhase.Doorway => (true, ""),
            SeedSubPhase.Mission => (true, ""),
            SeedSubPhase.Dilemma => CanSeedDilemmaPhase(card, mission, cryoPersonnelAlready),
            SeedSubPhase.Facility => CanSeedFacilityPhase(card, mission),
            _ => (false, "Unknown seed phase.")
        };
    }

    private static (bool ok, string reason) CanSeedDilemmaPhase(Card card, Card? mission, int cryoAlready)
    {
        if (IsAuPersonnelForCryosatellite(card))
        {
            if (CryosatellitePersonnelSlotsLeft(cryoAlready) <= 0)
                return (false, "Cryosatellite AU personnel quota (max 3) already filled.");
            if (mission == null)
                return (false, "Choose the space mission that has Cryosatellite.");
            var under = CanSeedUnderMission(card, mission);
            if (!under.ok) return under;
            return (true, "");
        }

        if (mission == null)
            return (false, $"Choose a mission to seed {card.Name} under.");
        return CanSeedUnderMission(card, mission);
    }

    private static (bool ok, string reason) CanSeedFacilityPhase(Card card, Card? mission)
    {
        bool needsMission = CardKinds.IsFacility(card) || CardKinds.IsShip(card)
                            || CardKinds.IsPersonnel(card);
        if (needsMission)
        {
            if (mission == null)
                return (false, $"Choose a mission for {card.Name}.");
            if (CardKinds.IsFacility(card))
                return CanSeedFacilityAt(card, mission);
            return (true, "");
        }

        // Events / doorways / objectives may sit on TABLE with no mission.
        return (true, "");
    }


    // ----- Location icons [P] / [S] -----

    public static (bool planet, bool space, bool known) ParseLocationIcons(Card card)
    {
        string mdt = (card.MissionDilemmaType ?? "").Trim();
        string icons = (card.Icons ?? "").Trim();
        string blob = $"{mdt} {icons}".ToUpperInvariant();

        bool planet = false, space = false, known = false;

        if (blob.Contains("[S/P]") || blob.Contains("[P/S]") || blob.Contains("[P][S]") || blob.Contains("[S][P]"))
        {
            planet = space = known = true;
        }
        else
        {
            if (blob.Contains("[P]") || blob.Contains("[PLANET]"))
            { planet = true; known = true; }
            if (blob.Contains("[S]") || blob.Contains("[SPACE]"))
            { space = true; known = true; }
        }

        if (!known)
        {
            string bare = mdt.Trim().ToUpperInvariant();
            if (bare is "P" or "PLANET")
            { planet = true; known = true; }
            else if (bare is "S" or "SPACE")
            { space = true; known = true; }
            else if (bare is "S/P" or "P/S" or "BOTH" or "DUAL")
            { planet = space = known = true; }
        }

        if (!known)
        {
            string text = (card.Text ?? "").ToUpperInvariant();
            bool tp = text.Contains("[P]") || text.Contains("PLANET MISSION") || text.Contains("AWAY TEAM");
            bool ts = text.Contains("[S]") || text.Contains("SPACE MISSION") || text.Contains("ABOARD");
            if (tp && !ts) { planet = true; known = true; }
            else if (ts && !tp) { space = true; known = true; }
            else if (tp && ts) { planet = space = known = true; }
        }

        return (planet, space, known);
    }

    public static (bool planet, bool space) GetMissionLocation(Card mission)
    {
        var (p, s, known) = ParseLocationIcons(mission);
        if (!known) return (true, true);
        return (p, s);
    }

    public static (bool planet, bool space) GetDilemmaLocation(Card dilemma)
    {
        var (p, s, known) = ParseLocationIcons(dilemma);
        if (!known) return (true, true);
        return (p, s);
    }

    // ----- Card identity helpers -----

    public static bool IsCryosatellite(Card c) =>
        ArtifactRules.IsCryosatellite(c)
        || (c.Name ?? "").Contains("cryosatellite", StringComparison.OrdinalIgnoreCase)
        || (c.Name ?? "").Contains("cryo satellite", StringComparison.OrdinalIgnoreCase);

    /// <summary>Artifacts that may seed at space (not limited to planet-only default).</summary>
    public static bool IsArtifactSpaceAllowed(Card c)
    {
        string n = (c.Name ?? "").ToLowerInvariant();
        return n.Contains("cryosatellite")
               || n.Contains("cryo satellite")
               || n.Contains("orb negotiations")
               || n.Contains("the nexus");
    }

    public static bool IsAuPersonnelForCryosatellite(Card c)
    {
        if (!CardKinds.IsPersonnel(c)
            && !(c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
            return false;
        string blob = $"{c.Icons} {c.Characteristics} {c.Text} {c.SetFolder}";
        if (blob.Contains("[AU]", StringComparison.OrdinalIgnoreCase)) return true;
        if (blob.Contains("Alternate Universe", StringComparison.OrdinalIgnoreCase)) return true;
        string set = c.SetFolder ?? "";
        return set.Contains("Alternate", StringComparison.OrdinalIgnoreCase)
               || set.Equals("AU", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNeutralOrNonAlignedFacility(Card facility)
    {
        var fac = ParseAffiliationTokens(facility.Affiliation);
        string fname = (facility.Name ?? "").ToLowerInvariant();
        return fac.Contains("NA")
               || fac.Contains("NEUTRAL")
               || fname.Contains("neutral outpost");
    }

    // ----- Core legality -----

    /// <summary>May seedCard be placed under this mission (dilemma/artifact/personnel under cryo, …)?</summary>
    /// <summary>Glossary artifact: only one of each title may seed under a mission; duplicates are mis-seeds.</summary>
    public static (bool ok, string reason) CheckArtifactSeedLimits(
        Card seedCard,
        int seeder,
        IReadOnlyList<(Card card, int owner)> alreadyUnder)
    {
        if (!CardKinds.IsArtifact(seedCard) && !(seedCard.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase))
            return (true, "");
        if (ArtifactRules.SameTitleAlreadySeeded(seedCard, alreadyUnder.Select(x => x.card)))
            return (false, $"Mis-seed: {seedCard.Name} is already under this mission (duplicate title — both would be out-of-play).");
        if (ArtifactRules.PlayerAlreadySeededArtifact(seeder, seedCard, alreadyUnder))
            return (false, "Only one artifact per player per mission (Rulebook / Glossary artifact).");
        return (true, "");
    }

    public static (bool ok, string reason) CanSeedUnderMission(Card seedCard, Card mission)
    {
        var (mPlanet, mSpace) = GetMissionLocation(mission);
        string t = (seedCard.Type ?? "").ToLowerInvariant();

        if (t.Contains("artifact") || CardKinds.IsArtifact(seedCard))
        {
            if (IsCryosatellite(seedCard))
            {
                if (!mSpace)
                    return (false, "Cryosatellite seeds only under a space mission.");
                return (true, "");
            }
            if (IsArtifactSpaceAllowed(seedCard))
                return (true, "");
            if (!mPlanet)
                return (false, $"Artifact {seedCard.Name} only under planet missions (Rulebook 2.3 / Seed).");
            return (true, "");
        }

        if (t.Contains("dilemma") || CardKinds.IsDilemma(seedCard))
        {
            var (dPlanet, dSpace) = GetDilemmaLocation(seedCard);
            bool ok = (dPlanet && mPlanet) || (dSpace && mSpace);
            if (!ok)
            {
                string need = dPlanet && dSpace ? "Planet/Space"
                    : dPlanet ? "Planet [P]" : dSpace ? "Space [S]" : "unknown";
                string have = mPlanet && mSpace ? "Planet/Space"
                    : mPlanet ? "Planet [P]" : mSpace ? "Space [S]" : "unknown";
                return (false, $"Dilemma {seedCard.Name} ({need}) cannot be seeded at {mission.Name} ({have}).");
            }
            return (true, "");
        }

        // Personnel under Cryosatellite: location already constrained by the artifact; allow under mission.
        if (IsAuPersonnelForCryosatellite(seedCard))
            return (true, "");

        return (true, "");
    }

    /// <summary>Outpost/HQ/Station seed placement at a mission.</summary>
    public static (bool ok, string reason) CanSeedFacilityAt(Card facility, Card mission)
    {
        var (mPlanet, mSpace) = GetMissionLocation(mission);
        string name = (facility.Name ?? "").ToLowerInvariant();
        string text = (facility.Text ?? "").ToLowerInvariant();
        string type = (facility.Type ?? "").ToLowerInvariant();

        bool isOutpost = name.Contains("outpost") || type.Contains("outpost");
        bool isHq = name.Contains("headquarters") || text.Contains("headquarters");
        bool isStation = name.Contains("station") || type.Contains("station");

        if (isOutpost || isHq)
        {
            if (!mPlanet)
                return (false, $"{facility.Name} (Outpost/HQ) belongs at a planet mission.");
            if (!AffiliationsCompatible(facility, mission))
            {
                return (false,
                    $"{facility.Name} ({facility.Affiliation ?? "?"}) does not match mission {mission.Name} ({mission.Affiliation ?? "neutral"}).");
            }
            return (true, "");
        }

        if (isStation)
        {
            if (text.Contains("[s]") || text.Contains("space mission"))
            {
                if (!mSpace)
                    return (false, $"{facility.Name} belongs at a space mission.");
                return (true, "");
            }
            if (text.Contains("[p]") || text.Contains("planet"))
            {
                if (!mPlanet)
                    return (false, $"{facility.Name} belongs at a planet mission.");
                return (true, "");
            }
        }

        if (!mPlanet)
            return (false, $"{facility.Name} belongs at a planet mission.");
        return (true, "");
    }

    public static bool AffiliationsCompatible(Card facility, Card mission)
    {
        var fac = ParseAffiliationTokens(facility.Affiliation);
        var mis = ParseAffiliationTokens(mission.Affiliation);
        if (mis.Count == 0) return true;
        if (fac.Count == 0) return true;
        if (IsNeutralOrNonAlignedFacility(facility))
            return true;
        return fac.Overlaps(mis);
    }

    public static HashSet<string> ParseAffiliationTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return set;
        var matches = Regex.Matches(raw, @"\[([^\]]+)\]");
        foreach (Match m in matches)
        {
            string t = m.Groups[1].Value.Trim();
            if (t.Length > 0) set.Add(NormalizeAffiliationToken(t));
        }
        if (set.Count == 0)
        {
            string u = raw.Trim();
            if (u.Length > 0) set.Add(NormalizeAffiliationToken(u));
        }
        return set;
    }

    public static string NormalizeAffiliationToken(string t)
    {
        t = t.ToUpperInvariant();
        if (t is "FEDERATION" or "FED") return "FED";
        if (t is "KLINGON" or "KLI") return "KLI";
        if (t is "ROMULAN" or "ROM") return "ROM";
        if (t is "BAJORAN" or "BAJ") return "BAJ";
        if (t is "CARDASSIAN" or "CARD" or "CAR") return "CARD";
        if (t is "DOMINION" or "DOM") return "DOM";
        if (t is "FERENGI" or "FER") return "FER";
        if (t is "BORG") return "BORG";
        if (t is "NON-ALIGNED" or "NONALIGNED" or "NA" or "NON" or "NEUTRAL") return "NA";
        return t;
    }

    /// <summary>How many more AU personnel may still seed under Cryosatellite for this player.</summary>
    public static int CryosatellitePersonnelSlotsLeft(int alreadySeeded) =>
        Math.Max(0, CryosatellitePersonnelMax - alreadySeeded);
}
