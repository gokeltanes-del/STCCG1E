using System;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Phrase parser for "Plays on …" / "plays as …" targeting (F0+).
/// AwayTeam ≠ Crew (Compendium 7.0.1). Dual "crew or Away Team" → Host + Host2.
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
        /// <summary>Personnel group on a planet surface — not ship/facility crew.</summary>
        AwayTeam,
        /// <summary>Personnel aboard a ship or facility.</summary>
        Crew,
        Event,
        Gap
    }

    /// <summary>Ownership filter from printed your / opponent's / any.</summary>
    public enum Ownership
    {
        Any,
        Your,
        Opponent
    }

    public readonly record struct Spec(
        Host Host,
        Host Host2,
        Ownership Ownership,
        bool Exposed,
        bool Occupied,
        bool Empty,
        bool Cloaked,
        bool ExcludeFacility,
        string? Affiliation = null)
    {
        public bool Own => Ownership == Ownership.Your;
        public bool Opponent => Ownership == Ownership.Opponent;

        public bool Allows(Host h) =>
            h != Host.None && (h == Host || h == Host2);

        public bool IsDualPersonnelHost =>
            (Host == Host.AwayTeam && Host2 == Host.Crew)
            || (Host == Host.Crew && Host2 == Host.AwayTeam);
    }

    public static bool IsShipHost(Host h) => h == Host.Ship;
    public static bool IsFacilityHost(Host h) => h is Host.Outpost or Host.Facility;
    public static bool IsPersonnelGroupHost(Host h) => h is Host.AwayTeam or Host.Crew;

    public static Spec Parse(Card? card)
    {
        if (card == null) return default;
        string t = card.Text ?? "";
        if (t.Length == 0) return default;

        var m = Regex.Match(t,
            @"plays?\s+on\s+(?<clause>[^.;\n]{2,100})",
            RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            if (Regex.IsMatch(t, @"plays\s+between\s+two", RegexOptions.IgnoreCase))
                return new Spec(Host.Gap, Host.None, Ownership.Any, false, false, false, false, false);
            return default;
        }

        string clause = m.Groups["clause"].Value;
        string c = clause.ToLowerInvariant();

        Ownership ownership = Ownership.Any;
        if (c.Contains("opponent"))
            ownership = Ownership.Opponent;
        else if (c.Contains("your ") || c.StartsWith("your"))
            ownership = Ownership.Your;

        bool exposed = c.Contains("exposed");
        bool occupied = c.Contains("occupied");
        bool empty = Regex.IsMatch(c, @"\bempty\b");
        bool cloaked = c.Contains("cloaked") && !c.Contains("uncloaked");
        bool excludeFacility = Regex.IsMatch(c, @"not\s+(?:at\s+)?(?:a\s+)?facility")
                               || Regex.IsMatch(c, @"except\s+(?:at\s+)?(?:a\s+)?facility");

        bool hasAwayTeam = c.Contains("away team");
        bool hasCrew = Regex.IsMatch(c, @"\bcrew\b");

        Host host = Host.None;
        Host host2 = Host.None;

        if (c.StartsWith("table") || (c.Contains("table") && !c.Contains("turntable")))
            host = Host.Table;
        else if (hasAwayTeam && hasCrew)
        {
            // Printed "crew or Away Team" / "Away Team or crew" — both legal hosts.
            host = Host.Crew;
            host2 = Host.AwayTeam;
        }
        else if (hasAwayTeam)
            host = Host.AwayTeam;
        else if (hasCrew)
            host = Host.Crew;
        else if (c.Contains("planet") || c.Contains("[p]"))
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
        return new Spec(host, host2, ownership, exposed, occupied, empty, cloaked, excludeFacility,
            ParseAffiliationIcon(clause));
    }

    /// <summary>True if this Spec allows the concrete host kind (primary or dual).</summary>
    public static bool SpecAllowsHost(Spec spec, Host concrete) =>
        concrete != Host.None && spec.Allows(concrete);

    public static string? ParseAffiliationIcon(string? clause)
    {
        if (string.IsNullOrWhiteSpace(clause)) return null;
        var m = Regex.Match(clause, @"\[(?<a>[^\]]+)\]");
        if (!m.Success) return null;
        string raw = m.Groups["a"].Value.Trim();
        return raw.ToUpperInvariant() switch
        {
            "FED" or "FEDERATION" => "FED",
            "KLI" or "KLINGON" => "KLI",
            "ROM" or "ROMULAN" => "ROM",
            "BAJ" or "BAJORAN" => "BAJ",
            "CAR" or "CARD" or "CARDASSIAN" => "CARD",
            "FER" or "FERENGI" => "FER",
            "DOM" or "DOMINION" => "DOM",
            "BOR" or "BORG" => "BORG",
            "NA" or "NON" or "NON-ALIGNED" => "NA",
            "P" => null, // planet icon, not affiliation
            _ => raw.ToUpperInvariant()
        };
    }

    /// <summary>Map to interrupt drop-target enum (lossy for AwayTeam → crew buckets until F1 snap).</summary>
    public static InterruptRules.PlayTarget ToInterruptTarget(Spec spec)
    {
        if (spec.Allows(Host.Event))
            return InterruptRules.PlayTarget.Event;
        if (spec.Allows(Host.AwayTeam) || spec.Allows(Host.Crew))
            return spec.Own && !spec.Opponent
                ? InterruptRules.PlayTarget.OwnCrew
                : InterruptRules.PlayTarget.AnyCrew;
        if (spec.Allows(Host.Ship))
            return spec.Own && !spec.Opponent
                ? InterruptRules.PlayTarget.OwnShip
                : InterruptRules.PlayTarget.AnyShip;
        return InterruptRules.PlayTarget.None;
    }

    /// <summary>DE mini-test F0 AwayTeam ≠ Crew. Returns null if OK.</summary>
    public static string? VerifyAwayTeamCrewSplit()
    {
        var stone = new Card
        {
            Name = "Vulcan Stone of Gol",
            Type = "Artifact",
            Text = "Place in hand. Plays as [Event] on any Away Team: kills all."
        };
        // Stone has "plays as" not "plays on" as first phrase — Parse looks for plays on.
        // Alien Groupie style:
        var groupie = new Card
        {
            Name = "Alien Groupie",
            Type = "Interrupt",
            Text = "Plays on an Away Team on a solved planet."
        };
        var g = Parse(groupie);
        if (g.Host != Host.AwayTeam || g.Host2 != Host.None)
            return $"Alien Groupie must be AwayTeam only, got {g.Host}/{g.Host2}";

        var eta = new Card
        {
            Name = "Emergency Transporter Armbands",
            Type = "Interrupt",
            Text = "Plays on your crew or Away Team. Relocate to your ship or outpost."
        };
        var e = Parse(eta);
        if (!e.IsDualPersonnelHost)
            return $"ETA must be dual Crew+AwayTeam, got {e.Host}/{e.Host2}";
        if (e.Ownership != Ownership.Your)
            return "ETA must be Your ownership";

        var disruptor = new Card
        {
            Name = "Disruptor Overload",
            Type = "Interrupt",
            Text = "Plays on a crew or Away Team (not facility). Randomly select..."
        };
        var d = Parse(disruptor);
        if (!d.IsDualPersonnelHost)
            return $"Disruptor must be dual, got {d.Host}/{d.Host2}";
        if (!d.ExcludeFacility)
            return "Disruptor must ExcludeFacility";

        var shields = new Card
        {
            Name = "Metaphasic Shields",
            Type = "Event",
            Text = "Plays on your ship. SHIELDS +2 per SCIENCE."
        };
        var s = Parse(shields);
        if (s.Host != Host.Ship || s.Host2 != Host.None)
            return $"Metaphasic must be Ship only, got {s.Host}/{s.Host2}";
        if (s.Ownership != Ownership.Your)
            return "Metaphasic must be Your";

        var crewOnly = new Card
        {
            Name = "Fake Crew Only",
            Type = "Interrupt",
            Text = "Plays on a crew. Stop them."
        };
        var co = Parse(crewOnly);
        if (co.Host != Host.Crew || co.Host2 != Host.None)
            return $"crew-only must be Crew, got {co.Host}/{co.Host2}";

        if (SpecAllowsHost(g, Host.Crew))
            return "AwayTeam-only must not allow Crew";
        if (!SpecAllowsHost(e, Host.AwayTeam) || !SpecAllowsHost(e, Host.Crew))
            return "dual must allow both";
        return null;
    }
}
