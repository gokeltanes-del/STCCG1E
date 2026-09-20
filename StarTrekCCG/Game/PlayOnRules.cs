using System;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Phrase parser for Plays on / Plays as targeting (F0+F1).
/// Spec + Role are type-agnostic (Pepsch-Lock). AwayTeam != Crew.
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
        /// <summary>Personnel group on a planet surface - not ship/facility crew.</summary>
        AwayTeam,
        /// <summary>Personnel aboard a ship or facility.</summary>
        Crew,
        Event,
        Gap
    }

    public enum Ownership
    {
        Any,
        Your,
        Opponent
    }

    /// <summary>Play-role while resolving (F1). Kevin/Amanda equivalence docks in F2.</summary>
    public enum CardPlayRole
    {
        None,
        NativeEvent,
        NativeInterrupt,
        ArtifactAsEvent,
        ArtifactAsInterrupt,
        ArtifactAsEquipment,
        ImmediateTable
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
        CardPlayRole Role = CardPlayRole.None,
        string? Affiliation = null)
    {
        public bool Own => Ownership == Ownership.Your;
        public bool Opponent => Ownership == Ownership.Opponent;

        public bool Allows(Host h) =>
            h != Host.None && (h == Host || h == Host2);

        public bool IsDualPersonnelHost =>
            (Host == Host.AwayTeam && Host2 == Host.Crew)
            || (Host == Host.Crew && Host2 == Host.AwayTeam);

        public bool NeedsBoardSnap =>
            Host is Host.Ship or Host.Outpost or Host.Facility or Host.Mission
                or Host.PlanetMission or Host.AwayTeam or Host.Crew
                or Host.Event or Host.Gap
            || Host2 is Host.AwayTeam or Host.Crew;
    }

    public static bool IsShipHost(Host h) => h == Host.Ship;
    public static bool IsFacilityHost(Host h) => h is Host.Outpost or Host.Facility;
    public static bool IsPersonnelGroupHost(Host h) => h is Host.AwayTeam or Host.Crew;

    /// <summary>Central F1 entry: typed Spec + Role from printed text (any card type).</summary>
    public static Spec ResolvePlayOn(Card? card)
    {
        var spec = Parse(card);
        if (spec.Host == Host.None && spec.Role == CardPlayRole.None)
            return spec;
        if (spec.Role != CardPlayRole.None)
            return spec;
        return spec with { Role = InferNativeRole(card) };
    }

    public static CardPlayRole InferNativeRole(Card? card)
    {
        if (card == null) return CardPlayRole.None;
        string ty = (card.Type ?? "").Trim();
        if (ty.Equals("Event", StringComparison.OrdinalIgnoreCase)
            || ty.Equals("Q-Event", StringComparison.OrdinalIgnoreCase)
            || ty.Equals("Q Event", StringComparison.OrdinalIgnoreCase))
            return CardPlayRole.NativeEvent;
        if (ty.Equals("Interrupt", StringComparison.OrdinalIgnoreCase)
            || ty.Equals("Q-Interrupt", StringComparison.OrdinalIgnoreCase))
            return CardPlayRole.NativeInterrupt;
        return CardPlayRole.None;
    }

    public static Spec Parse(Card? card)
    {
        if (card == null) return default;
        string t = card.Text ?? "";
        if (t.Length == 0) return default;

        // Immediate table (Horga'hn-style) before generic plays-on.
        if (Regex.IsMatch(t, @"immediately\s+plays?\s+on\s+(?:the\s+)?table", RegexOptions.IgnoreCase)
            || Regex.IsMatch(t, @"plays?\s+immediately\s+on\s+(?:the\s+)?table", RegexOptions.IgnoreCase))
        {
            return new Spec(Host.Table, Host.None, Ownership.Any, false, false, false, false, false,
                CardPlayRole.ImmediateTable);
        }

        // Plays as [Event|Interrupt] on ... (Artifact-as-*)
        var asOn = Regex.Match(t,
            @"plays?\s+as\s+(?:an?\s+)?(?:\[(?<asType>[^\]]+)\]|(?<asType>event|interrupt))\s+on\s+(?<clause>[^.;\n]{2,100})",
            RegexOptions.IgnoreCase);
        if (asOn.Success)
        {
            string asType = asOn.Groups["asType"].Value.Trim();
            var role = asType.Contains("interrupt", StringComparison.OrdinalIgnoreCase)
                ? CardPlayRole.ArtifactAsInterrupt
                : CardPlayRole.ArtifactAsEvent;
            return BuildSpecFromClause(asOn.Groups["clause"].Value, role);
        }

        // Plays as [Event] without on (Thought Maker) — Role only, no board host.
        var asOnly = Regex.Match(t,
            @"plays?\s+as\s+(?:an?\s+)?(?:\[(?<asType>[^\]]+)\]|(?<asType>event|interrupt))\b",
            RegexOptions.IgnoreCase);
        if (asOnly.Success && !Regex.IsMatch(t, @"plays?\s+on\s+", RegexOptions.IgnoreCase))
        {
            string asType = asOnly.Groups["asType"].Value.Trim();
            var role = asType.Contains("interrupt", StringComparison.OrdinalIgnoreCase)
                ? CardPlayRole.ArtifactAsInterrupt
                : CardPlayRole.ArtifactAsEvent;
            return new Spec(Host.None, Host.None, Ownership.Any, false, false, false, false, false, role);
        }

        // Use as equipment
        if (Regex.IsMatch(t, @"use[sd]?\s+as\s+(?:\[)?equipment", RegexOptions.IgnoreCase)
            || Regex.IsMatch(t, @"used\s+as\s+equipment", RegexOptions.IgnoreCase))
        {
            return new Spec(Host.None, Host.None, Ownership.Any, false, false, false, false, false,
                CardPlayRole.ArtifactAsEquipment);
        }

        var m = Regex.Match(t,
            @"plays?\s+on\s+(?<clause>[^.;\n]{2,100})",
            RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            if (Regex.IsMatch(t, @"plays\s+between\s+two", RegexOptions.IgnoreCase))
                return new Spec(Host.Gap, Host.None, Ownership.Any, false, false, false, false, false,
                    InferNativeRole(card));
            return default;
        }

        return BuildSpecFromClause(m.Groups["clause"].Value, InferNativeRole(card));
    }

    private static Spec BuildSpecFromClause(string clause, CardPlayRole role)
    {
        string c = clause.ToLowerInvariant();

        Ownership ownership = Ownership.Any;
        if (c.Contains("opponent"))
            ownership = Ownership.Opponent;
        else if (c.Contains("your ") || c.StartsWith("your"))
            ownership = Ownership.Your;

        bool exposed = c.Contains("exposed");
        bool occupied = c.Contains("occupied");
        bool empty = Regex.IsMatch(c, @"\\bempty\\b");
        bool cloaked = c.Contains("cloaked") && !c.Contains("uncloaked");
        bool excludeFacility = Regex.IsMatch(c, @"not\\s+(?:at\\s+)?(?:a\\s+)?facility")
                               || Regex.IsMatch(c, @"except\\s+(?:at\\s+)?(?:a\\s+)?facility");

        bool hasAwayTeam = c.Contains("away team");
        bool hasCrew = Regex.IsMatch(c, @"\\bcrew\\b");

        Host host = Host.None;
        Host host2 = Host.None;

        if (c.StartsWith("table") || (c.Contains("table") && !c.Contains("turntable")))
            host = Host.Table;
        else if (hasAwayTeam && hasCrew)
        {
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

        if (host == Host.None)
            return new Spec(Host.None, Host.None, Ownership.Any, false, false, false, false, false, role);

        return new Spec(host, host2, ownership, exposed, occupied, empty, cloaked, excludeFacility,
            role, ParseAffiliationIcon(clause));
    }

    public static bool SpecAllowsHost(Spec spec, Host concrete) =>
        concrete != Host.None && spec.Allows(concrete);

    public static string? ParseAffiliationIcon(string? clause)
    {
        if (string.IsNullOrWhiteSpace(clause)) return null;
        try
        {
            // One bracket pair: [FED] / [P]. Never throw — Stone etc. have no affil icon.
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
        catch (ArgumentException)
        {
            return null;
        }
    }

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

    /// <summary>DE mini-test F0+F1. Returns null if OK.</summary>
    public static string? VerifyAwayTeamCrewSplit()
    {
        var groupie = new Card
        {
            Name = "Alien Groupie",
            Type = "Interrupt",
            Text = "Plays on an Away Team on a solved planet."
        };
        var g = Parse(groupie);
        if (g.Host != Host.AwayTeam || g.Host2 != Host.None)
            return $"Alien Groupie must be AwayTeam only, got {g.Host}/{g.Host2}";
        if (g.Role != CardPlayRole.NativeInterrupt)
            return $"Alien Groupie Role NativeInterrupt, got {g.Role}";

        var stone = new Card
        {
            Name = "Vulcan Stone of Gol",
            Type = "Artifact",
            Text = "Place in hand. Plays as [Event] on any Away Team: kills all."
        };
        var st = ResolvePlayOn(stone);
        if (st.Host != Host.AwayTeam || st.Host2 != Host.None)
            return $"Stone must parse AwayTeam, got {st.Host}/{st.Host2}";
        if (st.Role != CardPlayRole.ArtifactAsEvent)
            return $"Stone Role ArtifactAsEvent, got {st.Role}";
        if (!st.NeedsBoardSnap)
            return "Stone must NeedsBoardSnap";

        var kurlan = new Card
        {
            Name = "Kurlan Naiskos",
            Type = "Artifact",
            Text = "Place in hand. Plays as [Event] on a ship. Attributes x3 when fully staffed."
        };
        var k = ResolvePlayOn(kurlan);
        if (k.Host != Host.Ship || k.Role != CardPlayRole.ArtifactAsEvent)
            return $"Kurlan Ship+ArtifactAsEvent, got {k.Host}/{k.Role}";

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
        var s = ResolvePlayOn(shields);
        if (s.Host != Host.Ship || s.Host2 != Host.None)
            return $"Metaphasic must be Ship only, got {s.Host}/{s.Host2}";
        if (s.Ownership != Ownership.Your || s.Role != CardPlayRole.NativeEvent)
            return $"Metaphasic Your+NativeEvent, got {s.Ownership}/{s.Role}";

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
