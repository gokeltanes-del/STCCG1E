using StarTrekCCG.Models;
using StarTrekCCG.Services;

namespace StarTrekCCG;

/// <summary>
/// Premiere-oriented deck construction: which card types may sit in which pile.
/// Q's Tent may hold almost anything (side deck). Side-deck-only types stay in their pile.
/// </summary>
public static class DeckPlacementRules
{
    public static bool IsAllowed(Card card, DeckSection section, out string reason)
    {
        reason = "";
        if (card == null)
        {
            reason = "No card.";
            return false;
        }

        string t = (card.Type ?? "").ToLowerInvariant();
        string text = (card.Text ?? "").ToLowerInvariant();

        bool isQ = t.Contains("q-") || t.StartsWith("q ") || t.Contains("q event")
                   || t.Contains("q interrupt") || t.Contains("q dilemma") || t.Contains("q dilemma");
        bool isTactic = t.Contains("tactic");
        bool isSite = t.Contains("site") && !t.Contains("website");
        bool isTribble = t.Contains("tribble");
        bool isMission = t.Contains("mission");
        bool isDilemma = t.Contains("dilemma");
        bool isArtifact = t.Contains("artifact");
        bool isFacility = t.Contains("facility") || t.Contains("outpost") || t.Contains("headquarters");
        bool seedsByText = text.Contains("seeds ") || text.StartsWith("seeds") || text.Contains("seed one")
                           || text.Contains("seeds during") || text.Contains("seeds or plays");

        // Side-deck-only types
        if (isTactic)
        {
            if (section is DeckSection.BattleBridge or DeckSection.QsTent) return true;
            reason = "Tactic cards belong in the Battle Bridge (or Q's Tent).";
            return false;
        }
        if (isSite)
        {
            if (section is DeckSection.SitePile or DeckSection.QsTent) return true;
            reason = "Site cards belong in the Site Pile (or Q's Tent).";
            return false;
        }
        if (isTribble)
        {
            if (section is DeckSection.Tribble or DeckSection.QsTent) return true;
            reason = "Tribble cards belong in the Tribble pile (or Q's Tent).";
            return false;
        }
        if (isQ)
        {
            if (section is DeckSection.QContinuum or DeckSection.QsTent) return true;
            if (isDilemma && section == DeckSection.Seed) return true;
            reason = "Q cards belong in the Q-Continuum (or Q's Tent).";
            return false;
        }

        // Must seed: missions, dilemmas, artifacts (earned later from the mission).
        if (isMission || isDilemma || isArtifact)
        {
            if (section is DeckSection.Seed or DeckSection.QsTent) return true;
            reason = $"{PrettyType(card)} must be seeded (or stored in Q's Tent).";
            return false;
        }

        // Outposts / HQ typically seed.
        if (isFacility)
        {
            if (section is DeckSection.Seed or DeckSection.QsTent or DeckSection.Draw) return true;
            reason = "Facilities seed or play; not this side deck.";
            return false;
        }

        if (section == DeckSection.BattleBridge)
        {
            reason = "Battle Bridge is for Tactic cards.";
            return false;
        }
        if (section == DeckSection.SitePile)
        {
            reason = "Site Pile is for Site cards.";
            return false;
        }
        if (section == DeckSection.Tribble)
        {
            reason = "Tribble pile is for Tribble cards.";
            return false;
        }
        if (section == DeckSection.QContinuum)
        {
            reason = "Q-Continuum is for Q cards.";
            return false;
        }

        // Seed: doorways / "seeds …" text / Cryosatellite payload (personnel under artifact).
        if (section == DeckSection.Seed)
        {
            if (t.Contains("doorway") || seedsByText) return true;

            // Cryosatellite (AU): up to 3 AU personnel seed under the artifact.
            // Deck presence of Cryosatellite is enforced in CanAdd (needs deck context).
            if (IsPersonnelType(t) && HasAlternateUniverseIcon(card))
                return true;

            reason = IsPersonnelType(t)
                ? "Personnel seed only under Cryosatellite (AU icon required). Add Cryosatellite to Seed first."
                : $"{PrettyType(card)} belongs in the draw deck unless the card says it seeds.";
            return false;
        }

        // Draw + Q's Tent + legacy side: allowed for playable cards
        return true;
    }

    /// <summary>
    /// Compendium 3.x size / copy limits after type check.
    /// Missions and Sites do not count toward the 30-card seed deck.
    /// </summary>
    public static bool CanAdd(Deck deck, Card card, DeckSection section, int addQty, out string reason)
    {
        if (!IsAllowed(card, section, out reason))
            return false;
        if (deck == null) return true;

        string t = (card.Type ?? "").ToLowerInvariant();
        bool isMission = t.Contains("mission");
        bool isSite = t.Contains("site") && !t.Contains("website");
        bool isDilemma = t.Contains("dilemma");
        bool isArtifact = t.Contains("artifact");

        var list = DeckService.GetList(deck, section);
        int copiesHere = list.Where(e => NamesMatch(e.Name, card.Name)).Sum(e => e.Quantity);

        if (section == DeckSection.Seed)
        {
            if (isMission)
            {
                int missions = CountByType(deck.SeedCards, "mission");
                if (missions + addQty > 6)
                {
                    reason = "Mission pile must be exactly 6 missions (Compendium 3.1.1).";
                    return false;
                }
                if (copiesHere > 0)
                {
                    reason = "Each mission must be a different location — this title is already in the seed.";
                    return false;
                }
            }
            else if (isDilemma || isArtifact)
            {
                if (copiesHere + addQty > 2)
                {
                    reason = "No more than 2 copies of the same card may be seeded under missions (3.1).";
                    return false;
                }
                int seed30 = SeedCountToward30(deck);
                if (seed30 + addQty > 30)
                {
                    reason = "Seed deck may contain at most 30 cards (missions/sites excluded) (3.1).";
                    return false;
                }
            }
            else if (IsPersonnelType(t))
            {
                if (!HasAlternateUniverseIcon(card))
                {
                    reason = "Only Alternate Universe ([AU]) personnel may seed under Cryosatellite.";
                    return false;
                }
                if (!SeedContainsCryosatellite(deck))
                {
                    reason = "Add Cryosatellite to the seed deck before seeding personnel under it (max 3 AU).";
                    return false;
                }
                int underCryo = CountSeedPersonnel(deck);
                if (underCryo + addQty > 3)
                {
                    reason = "Cryosatellite: at most 3 AU personnel may seed under it.";
                    return false;
                }
                // Same-title copies: still limited by global seed uniqueness patterns for non-universal;
                // allow up to 2 of same name only if deck already permits (keep soft: no extra name lock).
                int seed30 = SeedCountToward30(deck);
                if (seed30 + addQty > 30)
                {
                    reason = "Seed deck may contain at most 30 cards (missions/sites excluded) (3.1).";
                    return false;
                }
            }
            else
            {
                int seed30 = SeedCountToward30(deck);
                if (seed30 + addQty > 30)
                {
                    reason = "Seed deck may contain at most 30 cards (missions/sites excluded) (3.1).";
                    return false;
                }
            }
        }

        if (section == DeckSection.QsTent)
        {
            int distinct = list.Count;
            bool already = copiesHere > 0;
            if (already)
            {
                reason = "Q's Tent: up to 13 different cards (no duplicates).";
                return false;
            }
            if (distinct + 1 > 13)
            {
                reason = "Q's Tent: at most 13 different cards.";
                return false;
            }
        }

        if (section == DeckSection.SitePile)
        {
            int sites = list.Sum(e => e.Quantity);
            if (sites + addQty > 6)
            {
                reason = "Site pile may include at most 6 sites (3.1.2).";
                return false;
            }
        }

        return true;
    }

    public static int SeedCountToward30(Deck deck) =>
        deck.SeedCards.Where(e =>
        {
            string ty = (e.Type ?? "").ToLowerInvariant();
            bool mission = ty.Contains("mission");
            bool site = ty.Contains("site") && !ty.Contains("website");
            return !mission && !site;
        }).Sum(e => e.Quantity);

    private static int CountByType(IEnumerable<DeckEntry> list, string typeContains) =>
        list.Where(e => (e.Type ?? "").Contains(typeContains, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.Quantity);

    private static int CountSeedPersonnel(Deck deck) =>
        deck.SeedCards.Where(e => IsPersonnelType(e.Type ?? "")).Sum(e => e.Quantity);

    private static bool SeedContainsCryosatellite(Deck deck) =>
        deck.SeedCards.Any(e =>
            string.Equals(e.Name, "Cryosatellite", StringComparison.OrdinalIgnoreCase));

    private static bool IsPersonnelType(string t) =>
        t.Contains("personnel", StringComparison.OrdinalIgnoreCase);

    /// <summary>Lackey icons field typically includes [AU] for Alternate Universe cards.</summary>
    private static bool HasAlternateUniverseIcon(Card card)
    {
        string blob = $"{card.Icons} {card.Characteristics} {card.Text} {card.SetFolder}";
        if (blob.Contains("[AU]", StringComparison.OrdinalIgnoreCase)) return true;
        if (blob.Contains("Alternate Universe", StringComparison.OrdinalIgnoreCase)) return true;
        // Set folder from split_lackey_sets for AU expansion
        string set = card.SetFolder ?? "";
        if (set.Contains("Alternate", StringComparison.OrdinalIgnoreCase)
            || set.Equals("AU", StringComparison.OrdinalIgnoreCase)
            || set.Contains("Alternate_Universe", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static bool NamesMatch(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string PrettyType(Card c) =>
        string.IsNullOrWhiteSpace(c.Type) ? "This card" : c.Type;
}