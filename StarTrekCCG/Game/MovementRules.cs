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
        var crew = crewOnBoard?.Where(c => !c.Disabled && !c.InStasis)?.ToList() ?? new List<Card>();
        var (cmdNeed, stfNeed) = ParseStaffingRequirement(ship);
        if (cmdNeed == 0 && stfNeed == 0)
        {
            string staff = (ship.Staff ?? "").Trim();
            if (string.IsNullOrEmpty(staff))
            {
                // G3 / Spock: no staffing icons still requires >=1 matching-affiliation crew.
                // Empty crew must NOT be Ok (deny Fly with 0 crew).
                if (crew.Count == 0)
                {
                    return new StaffResult(false,
                        $"No personnel aboard {ship.Name} - even without staffing icons, matching affiliation is required.",
                        0, 0, 0, 0);
                }
                if (!HasMatchingAffiliation(ship, crew, treaties))
                {
                    string aboard = string.Join(", ",
                        crew.Select(p => $"{p.Name}[{p.Affiliation ?? "?"}]"));
                    return new StaffResult(false,
                        $"No matching affiliation aboard (Ship: {ship.Affiliation ?? "?"}). Crew: {aboard}.",
                        0, 0, 0, 0);
                }
                return new StaffResult(true,
                    "No staffing icons - matching affiliation aboard.",
                    0, crew.Count, 0, 0);
            }
            int n = crew.Count;
            if (n > 0)
            {
                if (!HasMatchingAffiliation(ship, crew, treaties))
                {
                    string aboard = string.Join(", ",
                        crew.Select(p => $"{p.Name}[{p.Affiliation ?? "?"}]"));
                    return new StaffResult(false,
                        $"No matching affiliation aboard (Ship: {ship.Affiliation ?? "?"}). Crew: {aboard}.",
                        0, 0, 0, 0);
                }
                return new StaffResult(true, $"Text staffing \"{staff}\" - Sandbox: >=1 matching crew ok.", 0, n, 0, 0);
            }
            return new StaffResult(false, $"Staffing \"{staff}\": at least 1 personnel required (simplified).", 0, 0, 0, 0);
        }

        if (crew.Count == 0)
        {
            return new StaffResult(false,
                $"No personnel aboard {ship.Name} — matching affiliation and staffing missing.",
                0, 0, cmdNeed, stfNeed);
        }

        if (!HasMatchingAffiliation(ship, crew, treaties))
        {
            string aboard = string.Join(", ",
                crew.Select(p => $"{p.Name}[{p.Affiliation ?? "?"}]"));
            return new StaffResult(false,
                $"No matching affiliation aboard (Ship: {ship.Affiliation ?? "?"}). Crew: {aboard}.",
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
            : $"Not staffed: still requires Cmd={cmdLeft}, Stf={stfLeft} " +
              $"(aboard Cmd icons={cmdPool + useCmd + cmdAsStaff}, Stf icons={stfPool + useStf}).";

        return new StaffResult(ok, reason,
            useCmd + cmdAsStaff + cmdPool, useStf + stfPool, cmdNeed, stfNeed);
    }


    /// <summary>
    /// Matching Affiliation for staffing: real shared affiliation with the ship.
    /// Treaty/NA compatibility does NOT count as Match (G2 / Spock). Those cards may still
    /// contribute staffing icons (Cmd/Stf) once a matching-affiliation personnel is aboard.
    /// </summary>
    public static bool HasMatchingAffiliation(
        Card ship,
        IEnumerable<Card> crew,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null)
    {
        _ = treaties; // API-stable; ignored for Match (Treaty != Matching Affiliation)
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
        }
        return shipAff.Count == 0; // unklar - nicht blocken
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

    /// <summary>
    /// Printed special equipment from ship Text comma-list before sentence gametext
    /// (e.g. Galaxy: "Holodeck, Tractor Beam").
    /// </summary>
    public static bool ShipHasSpecialEquipment(Card ship, string equipmentName)
    {
        if (ship == null || string.IsNullOrWhiteSpace(equipmentName))
            return false;
        string t = (ship.Text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (string.IsNullOrEmpty(t))
            return false;

        int cut = t.IndexOf('.');
        string head = (cut >= 0 ? t[..cut] : t).Trim();
        int nl = head.IndexOf('\n');
        if (nl >= 0)
            head = head[..nl].Trim();

        foreach (var raw in head.Split(','))
        {
            string part = raw.Trim();
            if (part.Equals(equipmentName, StringComparison.OrdinalIgnoreCase))
                return true;
            // Gametext glued without a period: "Tractor Beam All [Holo]..."
            if (part.StartsWith(equipmentName + " ", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Trailing token after a sentence: "... captive. Tractor Beam"
        if (cut >= 0)
        {
            string tail = t[(cut + 1)..].Trim();
            foreach (var raw in tail.Split(new[] { ',', '\n' }, StringSplitOptions.None))
            {
                string part = raw.Trim().TrimEnd('.');
                if (part.Equals(equipmentName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Turn RANGE pool: EffectiveRange (printed with hull cap) minus Baryon and Junior countdown.
    /// Junior.Countdown ticks only on ship-owner EOT; attach turn uses countdown 0 (full RANGE).
    /// </summary>
    public static int ComputeShipTurnRange(int effectiveRange, int baryonPenalty = 0, int juniorPenalty = 0) =>
        Math.Max(0, effectiveRange - Math.Max(0, baryonPenalty) - Math.Max(0, juniorPenalty));

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
        Func<int, bool>? forOwnerAtIndex = null,
        bool wrapEnds = false)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= orderedMissions.Count || toIndex >= orderedMissions.Count)
            return int.MaxValue / 4;
        if (fromIndex == toIndex) return 0;

        int Linear(int step)
        {
            int n = orderedMissions.Count;
            int cost = 0;
            int i = fromIndex;
            int guard = 0;
            do
            {
                i = (i + step + n) % n;
                bool forOwner = forOwnerAtIndex?.Invoke(i) ?? true;
                cost += GetMissionSpan(orderedMissions[i], forOwner);
                if (++guard > n + 1) break;
            } while (i != toIndex);
            return cost;
        }

        int directStep = toIndex > fromIndex ? 1 : -1;
        int direct = Linear(directStep);
        if (!wrapEnds || orderedMissions.Count < 2)
            return direct;
        int around = Linear(-directStep);
        return Math.Min(direct, around);
    }

    public static MoveResult CanMoveShip(
        Card ship,
        IEnumerable<Card> crew,
        int remainingRange,
        IReadOnlyList<Card> orderedMissions,
        int fromIndex,
        int toIndex,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null,
        Func<int, bool>? forOwnerAtIndex = null,
        bool wrapEnds = false,
        bool skipStaffing = false)
    {
        if (!skipStaffing)
        {
            var staff = IsShipStaffed(ship, crew, treaties);
            if (!staff.Ok)
                return new MoveResult(false, staff.Reason, 0, remainingRange);
        }

        if (fromIndex == toIndex)
            return new MoveResult(false, "Ship is already at this mission.", 0, remainingRange);

        int cost = RangeCostBetween(orderedMissions, fromIndex, toIndex, forOwnerAtIndex, wrapEnds);
        if (cost > remainingRange)
        {
            return new MoveResult(false,
                $"RANGE too low: needs {cost}, left {remainingRange} (full RANGE {GetShipRange(ship)}).",
                cost, remainingRange);
        }

        return new MoveResult(true, $"Movement ok, costs {cost} RANGE.", cost, remainingRange - cost);
    }

    /// <summary>RANGE along Board locations (Gaps = own Span). Q-Net = BarrierAfter.</summary>
    public static int RangeCostBetween(
        IReadOnlyList<Location> line,
        int fromIndex,
        int toIndex,
        bool wrapEnds = false)
    {
        if (line == null || fromIndex < 0 || toIndex < 0
            || fromIndex >= line.Count || toIndex >= line.Count)
            return int.MaxValue / 4;
        if (fromIndex == toIndex) return 0;

        int Linear(int step)
        {
            int n = line.Count;
            int cost = 0;
            int i = fromIndex;
            int guard = 0;
            do
            {
                i = (i + step + n) % n;
                cost += Math.Max(0, line[i].Span);
                if (++guard > n + 1) break;
            } while (i != toIndex);
            return cost;
        }

        int directStep = toIndex > fromIndex ? 1 : -1;
        int direct = Linear(directStep);
        if (!wrapEnds || line.Count < 2)
            return direct;
        return Math.Min(direct, Linear(-directStep));
    }

    public static bool PathBlocked(
        IReadOnlyList<Location> line,
        int fromIndex,
        int toIndex,
        bool wrapEnds,
        out bool usedWrap)
    {
        usedWrap = false;
        if (line == null || fromIndex < 0 || toIndex < 0
            || fromIndex >= line.Count || toIndex >= line.Count || fromIndex == toIndex)
            return false;

        bool Blocked(int step)
        {
            int n = line.Count;
            int i = fromIndex;
            int guard = 0;
            while (i != toIndex)
            {
                if (step > 0 && line[i].BarrierAfter) return true;
                if (step < 0)
                {
                    int prev = (i + n - 1) % n;
                    if (line[prev].BarrierAfter) return true;
                }
                i = (i + step + n) % n;
                if (++guard > n + 1) return true;
            }
            return false;
        }

        int directStep = toIndex > fromIndex ? 1 : -1;
        bool directBlocked = Blocked(directStep);
        if (!wrapEnds || line.Count < 2)
            return directBlocked;

        int dCost = RangeCostBetween(line, fromIndex, toIndex, wrapEnds: false);
        int wCost = RangeCostBetween(line, fromIndex, toIndex, wrapEnds: true);
        bool wrapBlocked = Blocked(-directStep);
        // Prefer shorter WNOHGB arc when clear; if that arc is Q-Net blocked, fall back to direct.
        if (wCost < dCost)
        {
            if (!wrapBlocked)
            {
                usedWrap = true;
                return false;
            }
            return directBlocked;
        }
        return directBlocked;
    }

    public static MoveResult CanMoveShip(
        Card ship,
        IEnumerable<Card> crew,
        int remainingRange,
        IReadOnlyList<Location> line,
        int fromIndex,
        int toIndex,
        IReadOnlyList<TreatyRules.TreatyLink>? treaties = null,
        bool wrapEnds = false,
        bool skipStaffing = false)
    {
        if (!skipStaffing)
        {
            var staff = IsShipStaffed(ship, crew, treaties);
            if (!staff.Ok)
                return new MoveResult(false, staff.Reason, 0, remainingRange);
        }

        if (fromIndex == toIndex)
            return new MoveResult(false, "Ship is already at this location.", 0, remainingRange);

        bool blocked = PathBlocked(line, fromIndex, toIndex, wrapEnds, out _);
        if (blocked && !EventRules.HasSkill(crew, "Diplomacy", 2))
            return new MoveResult(false, "Q-Net: 2 Diplomacy required aboard.", 0, remainingRange);

        int cost = RangeCostBetween(line, fromIndex, toIndex, wrapEnds);
        if (cost > remainingRange)
        {
            return new MoveResult(false,
                $"RANGE too low: needs {cost}, left {remainingRange} (full RANGE {GetShipRange(ship)}).",
                cost, remainingRange);
        }

        return new MoveResult(true, $"Movement ok, costs {cost} RANGE.", cost, remainingRange - cost);
    }
}
