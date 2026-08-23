using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 7.1.3 Staff a ship + 7.1.5 RANGE movement (Premiere-Kern).
/// </summary>
public static class MovementRules
{
    public readonly record struct StaffResult(bool Ok, string Reason, int CmdHave, int StfHave, int CmdNeed, int StfNeed);
    public readonly record struct MoveResult(bool Ok, string Reason, int RangeCost, int RangeLeft);

    /// <summary>Staffing-Icons aus Ship.Staff z.B. [Cmd][Stf][Stf].</summary>
    public static (int cmd, int stf) ParseStaffingRequirement(Card ship)
    {
        string s = ship.Staff ?? "";
        int cmd = Regex.Matches(s, @"\[Cmd\]", RegexOptions.IgnoreCase).Count;
        int stf = Regex.Matches(s, @"\[Stf\]", RegexOptions.IgnoreCase).Count;
        // Borg etc. vorerst ignorieren – Premiere nutzt Cmd/Stf
        return (cmd, stf);
    }

    /// <summary>
    /// Personal-Icons: [Cmd] zählt als Command; kann auch eine Staff-Anforderung erfüllen.
    /// [Stf] zählt nur als Staff.
    /// </summary>
    public static (int cmd, int stfOnly) ParsePersonnelStaffingIcons(Card personnel)
    {
        string icons = (personnel.Icons ?? "") + (personnel.Staff ?? "");
        int cmd = Regex.Matches(icons, @"\[Cmd\]", RegexOptions.IgnoreCase).Count;
        int stf = Regex.Matches(icons, @"\[Stf\]", RegexOptions.IgnoreCase).Count;
        return (cmd, stf);
    }

    /// <summary>
    /// Schiff ist staffed, wenn alle [Cmd]- und [Stf]-Anforderungen erfüllt sind.
    /// Jedes [Cmd] am Personal deckt 1 Cmd ODER (wenn Cmd schon voll) 1 Stf ab.
    /// </summary>
    public static StaffResult IsShipStaffed(Card ship, IEnumerable<Card> crewOnBoard)
        => IsShipStaffed(ship, crewOnBoard, treaties: null);

    public static StaffResult IsShipStaffed(
        Card ship,
        IEnumerable<Card> crewOnBoard,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties)
    {
        var crew = crewOnBoard?.ToList() ?? new List<Card>();
        var (cmdNeed, stfNeed) = ParseStaffingRequirement(ship);
        if (cmdNeed == 0 && stfNeed == 0)
        {
            string staff = (ship.Staff ?? "").Trim();
            if (string.IsNullOrEmpty(staff))
                return new StaffResult(true, "Keine Staffing-Icons (frei).", 0, 0, 0, 0);
            int n = crew.Count;
            if (n > 0)
                return new StaffResult(true, $"Text-Staffing „{staff}“ – Sandbox: ≥1 Crew ok.", 0, n, 0, 0);
            return new StaffResult(false, $"Staffing „{staff}“: mindestens 1 Personal nötig (vereinfacht).", 0, 0, 0, 0);
        }

        if (!HasMatchingAffiliation(ship, crew, treaties))
        {
            return new StaffResult(false,
                $"Keine matching affiliation an Bord (Schiff: {ship.Affiliation ?? "?"}" +
                (treaties is { Count: > 0 } ? ", Treaty geprüft" : "") + ").",
                0, 0, cmdNeed, stfNeed);
        }

        int cmdPool = 0, stfPool = 0;
        foreach (var p in crew)
        {
            var (c, s) = ParsePersonnelStaffingIcons(p);
            cmdPool += c;
            stfPool += s;
        }

        int cmdLeft = cmdNeed;
        int stfLeft = stfNeed;

        int useCmd = Math.Min(cmdPool, cmdLeft);
        cmdPool -= useCmd;
        cmdLeft -= useCmd;

        int cmdAsStaff = Math.Min(cmdPool, stfLeft);
        cmdPool -= cmdAsStaff;
        stfLeft -= cmdAsStaff;

        int useStf = Math.Min(stfPool, stfLeft);
        stfPool -= useStf;
        stfLeft -= useStf;

        bool ok = cmdLeft == 0 && stfLeft == 0;
        string reason = ok
            ? "Staffed."
            : $"Nicht staffed: braucht noch Cmd={cmdLeft}, Stf={stfLeft} " +
              $"(an Bord Cmd-Icons={cmdPool + useCmd + cmdAsStaff}, Stf-Icons={stfPool + useStf}).";

        return new StaffResult(ok, reason,
            useCmd + cmdAsStaff + cmdPool, useStf + stfPool, cmdNeed, stfNeed);
    }


    public static bool HasMatchingAffiliation(
        Card ship,
        IEnumerable<Card> crew,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties)
    {
        var shipAff = ReportingRules.GetAffiliations(ship);
        if (shipAff.Count == 0)
        {
            string a = (ship.Affiliation ?? "").Trim();
            if (a.Length > 0) shipAff.Add(ReportingRules.NormalizeAffil(a));
        }
        foreach (var p in crew)
        {
            var pa = ReportingRules.GetAffiliations(p);
            if (pa.Count == 0)
            {
                string a = (p.Affiliation ?? "").Trim();
                if (a.Length > 0) pa.Add(ReportingRules.NormalizeAffil(a));
            }
            if (shipAff.Overlaps(pa)) return true;
            // Treaty: Klingon auf Fed-Schiff etc.
            if (treaties != null && treaties.Count > 0
                && TreatyRules.AffiliationsCompatible(shipAff, pa, treaties))
                return true;
        }
        return shipAff.Count == 0; // unklar → nicht blocken
    }

    public static int GetShipRange(Card ship)
    {
        string r = (ship.IntegrityOrRange ?? "").Trim();
        if (int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            return Math.Max(0, v);
        // "9 / 8" oder "X" – erste Zahl
        var m = Regex.Match(r, @"\d+");
        if (m.Success && int.TryParse(m.Value, out v))
            return v;
        return 0;
    }

    public static int GetMissionSpan(Card mission, bool forOwner = true)
        => MissionRules.GetEffectiveSpan(mission, forOwner);

    /// <summary>
    /// RANGE-Kosten: Summe der Spans aller Locationen, zu denen bewegt wird
    /// (Start zählt nicht). Index entlang _spacelineOrder.
    /// </summary>
    /// <param name="forOwnerAtIndex">
    /// Optional: true when the mover is the mission owner (printed Span);
    /// false uses Opponent's-side Span when printed (e.g. Warped Space).
    /// </param>
    public static int RangeCostBetween(
        IReadOnlyList<Card> orderedMissions,
        int fromIndex,
        int toIndex,
        Func<int, bool>? forOwnerAtIndex = null)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= orderedMissions.Count || toIndex >= orderedMissions.Count)
            return int.MaxValue / 4;
        if (fromIndex == toIndex) return 0;

        int step = toIndex > fromIndex ? 1 : -1;
        int cost = 0;
        for (int i = fromIndex + step; ; i += step)
        {
            bool forOwner = forOwnerAtIndex?.Invoke(i) ?? true;
            cost += GetMissionSpan(orderedMissions[i], forOwner);
            if (i == toIndex) break;
        }
        return cost;
    }

    public static MoveResult CanMoveShip(
        Card ship,
        IEnumerable<Card> crew,
        int remainingRange,
        IReadOnlyList<Card> orderedMissions,
        int fromIndex,
        int toIndex,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null,
        Func<int, bool>? forOwnerAtIndex = null)
    {
        var staff = IsShipStaffed(ship, crew, treaties);
        if (!staff.Ok)
            return new MoveResult(false, staff.Reason, 0, remainingRange);

        if (fromIndex == toIndex)
            return new MoveResult(false, "Schiff ist bereits an dieser Mission.", 0, remainingRange);

        int cost = RangeCostBetween(orderedMissions, fromIndex, toIndex, forOwnerAtIndex);
        if (cost > remainingRange)
        {
            return new MoveResult(false,
                $"RANGE zu gering: braucht {cost}, übrig {remainingRange} (voller RANGE {GetShipRange(ship)}).",
                cost, remainingRange);
        }

        return new MoveResult(true, $"Bewegung ok, kostet {cost} RANGE.", cost, remainingRange - cost);
    }
}