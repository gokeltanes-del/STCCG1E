using System;
using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// One legal-target contract. TableWindow maps sites to visuals (halo / peek / snap / picker).
/// UI must not invent a third legality path.
///
/// Not in this cut: Beam mode, battle target pick, response stack, net/AI.
/// </summary>
public enum TargetSiteKind
{
    TableCard,
    HostFace,
    BuriedCard,
    GapSpan,
    LocationSlot,
    Zone
}

public enum TargetWhy
{
    Nullify,
    PlayOn,
    Seed,
    Report,
    Discard,
    RequiredMove
}

public readonly record struct TargetSite(
    TargetSiteKind Kind,
    Card Card,
    Card? Host,
    Card? Host2,
    TargetWhy Why,
    string Reason);

public static class TargetQuery
{
    public static bool IsNullifyDrag(Card? drag) =>
        drag != null && (InterruptRules.IsKevinNullify(drag) || InterruptRules.IsDevil(drag));

    public static bool WantsBuriedPeek(Card? drag) =>
        drag != null && (IsNullifyDrag(drag) || InterruptRules.IsHugh(drag));

    public static (bool ok, string reason) CanTarget(Card drag, Card candidate)
    {
        if (InterruptRules.IsDevil(drag))
            return TimingRules.CanDevilTarget(candidate);
        if (InterruptRules.IsKevinNullify(drag))
            return TimingRules.CanKevinTargetEvent(candidate);
        return (false, "No targeting rule for this card.");
    }

    public static bool IsLegal(Card drag, Card candidate) => CanTarget(drag, candidate).ok;

    /// <summary>
    /// Snapshot item from the table: a card in play and optional host faces.
    /// Kind is a hint (TABLE vs buried vs gap); legality still comes from CanTarget.
    /// </summary>
    public readonly record struct InPlay(
        Card Card,
        Card? Host,
        Card? Host2,
        TargetSiteKind Kind);

    public static List<TargetSite> NullifySites(Card drag, IEnumerable<InPlay> inPlay)
    {
        var list = new List<TargetSite>();
        var seen = new HashSet<Card>();
        foreach (var row in inPlay)
        {
            if (row.Card == null || !seen.Add(row.Card)) continue;
            var chk = CanTarget(drag, row.Card);
            if (!chk.ok) continue;
            list.Add(new TargetSite(row.Kind, row.Card, row.Host, row.Host2, TargetWhy.Nullify, chk.reason));
        }
        return list;
    }

    public static bool SiteBelongsToHost(TargetSite site, Card? hostCard)
    {
        if (hostCard == null) return false;
        if (ReferenceEquals(site.Card, hostCard)) return true;
        if (ReferenceEquals(site.Host, hostCard)) return true;
        if (ReferenceEquals(site.Host2, hostCard)) return true;
        return false;
    }

    public readonly record struct HostFacts(
        int Owner,
        bool Exposed,
        bool Cloaked,
        bool Occupied,
        bool EmptyOfPersonnel,
        bool HasRogueBorg,
        int SecurityCount,
        bool IsShip,
        bool IsFacility,
        bool IsOutpost,
        bool IsMission,
        bool IsPlanetMission,
        bool IsNonAlignedShip,
        bool IsBorgShip);

    public static bool IsGapPlay(Card? drag)
    {
        if (drag == null || !EventRules.IsEvent(drag)) return false;
        var p = EventRules.ResolvePlay(drag).Persist;
        return p is EventRules.Persist.Gaps or EventRules.Persist.QNet;
    }

    public static bool IsPlayOnDrag(Card? drag)
    {
        if (drag == null) return false;
        if (EventRules.IsEvent(drag) && EventRules.PlaysOnHost(drag)) return true;
        if (InterruptRules.IsIncomingMessage(drag)
            && !(drag.Name ?? "").Contains("Attack Authorization", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// May this card be played onto this in-play piece?
    /// Named event overrides first, then EventRules.TargetKind, then PlayOnRules.Parse.
    /// Gaps are not host-cards — use IsGapPlay + spaceline pairs in the view.
    /// </summary>
    public static (bool ok, string reason) CanPlayOn(Card drag, Card candidate, HostFacts facts, int player)
    {
        if (InterruptRules.IsIncomingMessage(drag)
            && !(drag.Name ?? "").Contains("Attack Authorization", StringComparison.OrdinalIgnoreCase))
        {
            if (!facts.IsShip)
                return (false, "Incoming Message: plays on a matching-affiliation ship.");
            string? need = InterruptRules.IncomingMessageAffiliation(drag)
                           ?? PlayOnRules.Parse(drag).Affiliation;
            if (!string.IsNullOrEmpty(need) && !AffiliationMatches(candidate, need))
                return (false, $"Incoming Message: ship is not {need}.");
            return (true, "Incoming Message on that ship.");
        }

        if (EventRules.IsEvent(drag) && EventRules.PlaysOnHost(drag))
        {
            if (IsGapPlay(drag))
                return (false, "Gap events play between two missions.");

            var play = EventRules.ResolvePlay(drag);
            var tk = EventRules.GetTargetKind(play);

            if (EventRules.IsLoreReturns(drag))
            {
                if (!facts.IsShip) return (false, "Lore Returns: target must be a ship.");
                if (facts.Owner == player) return (false, "Lore Returns: opponent's ship.");
                if (!facts.HasRogueBorg) return (false, "Lore Returns: no Rogue Borg aboard.");
                if (!facts.EmptyOfPersonnel) return (false, "Lore Returns: ship must be empty of personnel.");
                return (true, "Lore Returns on that ship.");
            }

            if (EventRules.IsNeuralServo(drag))
            {
                if (!facts.IsShip || !facts.IsNonAlignedShip)
                    return (false, "Neural Servo Device: Non-Aligned ship.");
                if (facts.SecurityCount >= 2)
                    return (false, "Neural Servo Device: ship has 2 SECURITY.");
                return (true, "Neural Servo Device on that ship.");
            }

            if (EventRules.IsPlasmaFire(drag) || EventRules.IsWarpCoreBreach(drag))
            {
                if (!facts.IsShip) return (false, "Plays on a ship.");
                if (facts.IsBorgShip) return (false, "Not a [Bor] ship.");
                return (true, "Play on that ship.");
            }

            if (tk == EventRules.TargetKind.Ship)
            {
                if (!facts.IsShip) return (false, "Plays on a ship.");
                if (facts.Owner != player && facts.Owner != 0)
                    return (false, "Plays on your ship.");
                return ExtraPlayOnSpec(drag, candidate, facts, player, "Play on that ship.");
            }

            if (tk == EventRules.TargetKind.PlanetMission)
            {
                if (!facts.IsPlanetMission) return (false, "Plays on a planet mission.");
                return (true, "Play on that planet.");
            }

            if (tk == EventRules.TargetKind.Mission)
            {
                if (!facts.IsMission) return (false, "Plays on a mission.");
                if (play.Persist == EventRules.Persist.Espionage && !string.IsNullOrEmpty(play.EspionageOn))
                {
                    var need = MissionRules.ParseAffiliationTokens(candidate.Affiliation);
                    if (!need.Contains(MissionRules.NormalizeAffiliationToken(play.EspionageOn)))
                        return (false, "Espionage: mission affiliation does not match.");
                }
                return (true, "Play on that mission.");
            }

            if (tk == EventRules.TargetKind.Outpost)
            {
                if (!facts.IsFacility) return (false, "Plays on your outpost / facility.");
                if (facts.Owner != player) return (false, "Plays on your facility.");
                return (true, "Play on that facility.");
            }
        }

        var spec = PlayOnRules.Parse(drag);
        if (spec.Host != PlayOnRules.Host.None && spec.Host != PlayOnRules.Host.Table
            && spec.Host != PlayOnRules.Host.Gap && spec.Host != PlayOnRules.Host.Event)
            return MatchPlayOnSpec(spec, candidate, facts, player);

        return (false, "No play-on target.");
    }

    public static (bool ok, string reason) CanImFacility(
        Card facility, int facilityOwner, int shipController, string? needAffil, bool sameQuadrant)
    {
        if (facilityOwner != shipController)
            return (false, "Facility must belong to the ship's controller.");
        if (!sameQuadrant)
            return (false, "Facility must be on this spaceline (same quadrant).");
        if (!string.IsNullOrEmpty(needAffil) && !AffiliationMatches(facility, needAffil))
            return (false, $"Facility is not {needAffil}.");
        return (true, "Incoming Message destination.");
    }

    private static (bool ok, string reason) ExtraPlayOnSpec(
        Card drag, Card candidate, HostFacts facts, int player, string okReason)
    {
        var spec = PlayOnRules.Parse(drag);
        if (spec.Host == PlayOnRules.Host.None) return (true, okReason);
        var m = MatchPlayOnSpec(spec, candidate, facts, player);
        return m.ok ? (true, okReason) : m;
    }

    private static (bool ok, string reason) MatchPlayOnSpec(
        PlayOnRules.Spec spec, Card candidate, HostFacts facts, int player)
    {
        bool persOk = spec.Own ? facts.Occupied && facts.Owner == player : facts.Occupied;
        bool matchAway = facts.IsPlanetMission && persOk;
        bool matchCrew = persOk && (facts.IsShip || (facts.IsFacility && !spec.ExcludeFacility));

        bool hostOk =
            (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.Ship) && facts.IsShip)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.Outpost) && facts.IsOutpost)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.Facility) && facts.IsFacility)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.Mission) && facts.IsMission)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.PlanetMission) && facts.IsPlanetMission)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.AwayTeam) && matchAway)
            || (PlayOnRules.SpecAllowsHost(spec, PlayOnRules.Host.Crew) && matchCrew);
        if (!hostOk) return (false, "Wrong host type.");
        if (spec.Allows(PlayOnRules.Host.Ship) || spec.Allows(PlayOnRules.Host.Outpost) || spec.Allows(PlayOnRules.Host.Facility))
        {
            if (spec.Own && facts.Owner != player) return (false, "Must be your card.");
            if (spec.Opponent && facts.Owner == player) return (false, "Must be opponent's card.");
        }
        if (spec.Exposed && facts.IsShip && !facts.Exposed) return (false, "Ship is not exposed.");
        if (spec.Cloaked && facts.IsShip && !facts.Cloaked) return (false, "Ship is not cloaked.");
        if (spec.Occupied && !facts.Occupied) return (false, "Must be occupied.");
        if (spec.Empty && facts.Occupied) return (false, "Must be empty.");
        if (!string.IsNullOrEmpty(spec.Affiliation) && !AffiliationMatches(candidate, spec.Affiliation))
            return (false, "Affiliation does not match.");
        return (true, "Legal play-on host.");
    }

    /// <summary>Adjacent landable columns, same quadrant. Q-Net is not a column.</summary>
    public static List<(Card left, Card right)> AdjacentLandables(IReadOnlyList<Location> line)
    {
        var pairs = new List<(Card, Card)>();
        if (line == null) return pairs;
        for (int i = 0; i < line.Count - 1; i++)
        {
            var a = line[i];
            var b = line[i + 1];
            if (a.Printed == null || b.Printed == null) continue;
            if (!string.Equals(a.Quadrant ?? "Alpha", b.Quadrant ?? "Alpha",
                    StringComparison.OrdinalIgnoreCase))
                continue;
            pairs.Add((a.Printed, b.Printed));
        }
        return pairs;
    }

    public static List<TargetSite> GapSites(IReadOnlyList<Location> line)
    {
        var list = new List<TargetSite>();
        foreach (var (left, right) in AdjacentLandables(line))
        {
            list.Add(new TargetSite(TargetSiteKind.GapSpan, left, left, right, TargetWhy.PlayOn,
                "Play between these missions."));
        }
        return list;
    }

    public static List<TargetSite> PlayOnSites(
        Card drag,
        int player,
        IEnumerable<(Card card, HostFacts facts)> pieces)
    {
        var list = new List<TargetSite>();
        var seen = new HashSet<int>();
        foreach (var (card, facts) in pieces)
        {
            if (card == null) continue;
            int id = card.InstanceId > 0 ? card.InstanceId : card.GetHashCode();
            if (!seen.Add(id)) continue;
            var chk = CanPlayOn(drag, card, facts, player);
            if (!chk.ok) continue;
            list.Add(new TargetSite(TargetSiteKind.HostFace, card, card, null, TargetWhy.PlayOn, chk.reason));
        }
        return list;
    }

    public static bool AffiliationMatches(Card card, string need)
    {
        var have = ReportingRules.GetAffiliations(card);
        if (have.Contains(need)) return true;
        if (!string.IsNullOrWhiteSpace(card.CurrentAffiliation)
            && ReportingRules.NormalizeAffil(card.CurrentAffiliation) == need)
            return true;
        return MissionRules.ParseAffiliationTokens(card.Affiliation).Contains(need);
    }
}