using System;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Phrase parser for "Plays on …" / "Nullifies …" on Events and Interrupts.
/// Catalog overrides remain for unique cards; this covers the repeating templates
/// across Premiere and later sets (467 "plays on table", 56 ship, 20 mission, …).
/// </summary>
public static class PlayOnRules
{
    public enum Host
    {
        None,
        Table,
        Ship,
        Outpost,
        Facility,
        Mission,
        PlanetMission,
        Crew,
        Event,
        Gap
    }

    public readonly record struct Spec(
        Host Host,
        bool Own,
        bool Opponent,
        bool Exposed,
        bool Occupied,
        bool Empty,
        bool Cloaked);

    public static bool IsShipHost(Host h) => h == Host.Ship;
    public static bool IsFacilityHost(Host h) => h is Host.Outpost or Host.Facility;

    public static Spec Parse(Card? card)
    {
        if (card == null) return default;
        string t = card.Text ?? "";
        if (t.Length == 0) return default;

        // First "plays on …" clause wins (printed targeting line).
        var m = Regex.Match(t,
            @"plays?\s+on\s+(?<clause>[^.;\n]{2,80})",
            RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            if (Regex.IsMatch(t, @"plays\s+between\s+two", RegexOptions.IgnoreCase))
                return new Spec(Host.Gap, false, false, false, false, false, false);
            return default;
        }

        string clause = m.Groups["clause"].Value;
        string c = clause.ToLowerInvariant();

        bool own = c.Contains("your ") || c.StartsWith("your");
        bool opp = c.Contains("opponent");
        bool exposed = c.Contains("exposed");
        bool occupied = c.Contains("occupied");
        bool empty = Regex.IsMatch(c, @"\bempty\b");
        bool cloaked = c.Contains("cloaked") && !c.Contains("uncloaked");

        Host host = Host.None;
        if (c.StartsWith("table") || c.Contains("table"))
            host = Host.Table;
        else if (c.Contains("away team") || c.Contains("crew"))
            host = Host.Crew;
        else if (c.Contains("planet"))
            host = Host.PlanetMission;
        else if (c.Contains("mission") || c.Contains("homeworld") || c.Contains("spaceline location"))
            host = Host.Mission;
        else if (c.Contains("outpost"))
            host = Host.Outpost;
        else if (c.Contains("facility") || c.Contains("headquarters") || c.Contains("nor")
                 || c.Contains("station"))
            host = Host.Facility;
        else if (c.Contains("ship"))
            host = Host.Ship;
        else if (c.Contains("event"))
            host = Host.Event;

        if (host == Host.None) return default;
        return new Spec(host, own, opp, exposed, occupied, empty, cloaked);
    }

    /// <summary>Map to the interrupt drop-target enum used by TableWindow.</summary>
    public static InterruptRules.PlayTarget ToInterruptTarget(Spec spec)
    {
        return spec.Host switch
        {
            Host.Event => InterruptRules.PlayTarget.Event,
            Host.Crew when spec.Own && !spec.Opponent => InterruptRules.PlayTarget.OwnCrew,
            Host.Crew => InterruptRules.PlayTarget.AnyCrew,
            Host.Ship when spec.Own && !spec.Opponent => InterruptRules.PlayTarget.OwnShip,
            Host.Ship => InterruptRules.PlayTarget.AnyShip,
            // Outpost/facility/mission are not ships — caller must use Spec, not this flatten.
            _ => InterruptRules.PlayTarget.None
        };
    }
}