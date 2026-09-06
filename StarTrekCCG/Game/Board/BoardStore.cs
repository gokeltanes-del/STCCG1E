using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Dual-run board. Empty until Schritt 2 RebuildFrom.
/// TableWindow must not treat this as paint source yet.
/// </summary>
public sealed class BoardStore
{
    public static BoardStore Current { get; } = new();

    public Spaceline Spaceline { get; } = new();

    public Dictionary<int, CardInstance> ById { get; } = new();

    public List<int> HandP1 { get; } = new();
    public List<int> HandP2 { get; } = new();
    public List<int> TableP1 { get; } = new();
    public List<int> TableP2 { get; } = new();

    public void Clear()
    {
        ById.Clear();
        HandP1.Clear();
        HandP2.Clear();
        TableP1.Clear();
        TableP2.Clear();
        while (Spaceline.Locations.Count > 0)
            Spaceline.Remove(Spaceline.Locations[0]);
    }

    /// <summary>Typed wrapper around an already-instantiated match copy. Does not parse JSON.</summary>
    public CardInstance Wrap(Card copy)
    {
        CardInstance inst = CardKinds.Of(copy) switch
        {
            CardKind.Personnel => new PersonnelInstance(copy),
            CardKind.Ship => new ShipInstance(copy),
            CardKind.Facility => new FacilityInstance(copy),
            CardKind.Event or CardKind.Incident or CardKind.Objective => new EventInstance(copy),
            CardKind.Equipment => new EquipmentInstance(copy),
            CardKind.Mission or CardKind.TimeLocation => new MissionInstance(copy),
            _ => new OtherInstance(copy)
        };

        if (inst.InstanceId > 0)
            ById[inst.InstanceId] = inst;
        return inst;
    }

    public Location? LocationOfOccupant(int hostInstanceId)
    {
        if (hostInstanceId <= 0) return null;
        foreach (var loc in Spaceline.Locations)
        {
            foreach (var occ in loc.Occupants)
                if (occ.InstanceId == hostInstanceId) return loc;
        }
        return null;
    }

    public Occupant? FindOccupant(int hostInstanceId)
    {
        if (hostInstanceId <= 0) return null;
        foreach (var loc in Spaceline.Locations)
        {
            foreach (var occ in loc.Occupants)
                if (occ.InstanceId == hostInstanceId) return occ;
        }
        return null;
    }

    public List<Card> CrewPersonnel(int hostInstanceId)
    {
        var occ = FindOccupant(hostInstanceId);
        if (occ == null) return new List<Card>();
        return occ.Crew.Personnel.Select(p => p.Printed).ToList();
    }

    public void RemoveFromForces(int instanceId)
    {
        if (instanceId <= 0) return;
        foreach (var loc in Spaceline.Locations)
        {
            foreach (var occ in loc.Occupants)
                occ.Crew.Remove(instanceId);
            loc.AwayTeamP1.Remove(instanceId);
            loc.AwayTeamP2.Remove(instanceId);
        }
    }

    /// <summary>Beam / report: card leaves every Force, then joins the host's Crew or Away Team.</summary>
    public void ApplyHosted(Card card, Card host, int controller)
    {
        var inst = Wrap(card);
        RemoveFromForces(inst.InstanceId);

        var occ = FindOccupant(host.InstanceId);
        if (occ != null)
        {
            AddToForce(occ.Crew, inst);
            return;
        }

        var loc = Spaceline.FindByInstanceId(host.InstanceId);
        if (loc == null) return;
        var force = controller == 2 ? loc.AwayTeamP2 : loc.AwayTeamP1;
        AddToForce(force, inst);
    }

    private static void AddToForce(Force force, CardInstance inst)
    {
        if (inst is PersonnelInstance p && force.Personnel.All(x => x.InstanceId != p.InstanceId))
            force.Personnel.Add(p);
        else if (inst is EquipmentInstance e && force.Equipment.All(x => x.InstanceId != e.InstanceId))
            force.Equipment.Add(e);
    }

    /// <summary>
    /// Ships, facilities, missions, span locations, TABLE cards from the store.
    /// Attached events overlaid in ToGameState. E3/E3b: RangeLeft / Stopped / Cloak / Dock / Hull from instances.
    /// </summary>
    public List<BoardPiece> ToBoardPieces(
        IReadOnlyList<TreatyRules.TreatyLink>? treatiesP1 = null,
        IReadOnlyList<TreatyRules.TreatyLink>? treatiesP2 = null,
        IReadOnlyList<int>? loreStaffedShipIds = null)
    {
        var list = new List<BoardPiece>();
        var seen = new HashSet<int>();
        var loreStaffed = loreStaffedShipIds != null && loreStaffedShipIds.Count > 0
            ? new HashSet<int>(loreStaffedShipIds)
            : null;

        for (int i = 0; i < Spaceline.Locations.Count; i++)
        {
            var loc = Spaceline.Locations[i];
            if (loc.Printed != null)
            {
                var at = CardsInForce(loc.AwayTeamP1).Concat(CardsInForce(loc.AwayTeamP2)).ToList();
                list.Add(PieceFromPrinted(
                    loc.Printed,
                    KindOfLocation(loc),
                    i,
                    hostName: null,
                    aboard: at,
                    zone: CardZone.Spaceline));
                if (loc.Printed.InstanceId > 0) seen.Add(loc.Printed.InstanceId);
            }

            foreach (var occ in loc.Occupants)
            {
                var aboard = CardsInForce(occ.Crew);
                var printed = occ.Card.Printed;
                var kind = occ.Kind == OccupantKind.Facility
                    ? BoardPieceKind.Facility
                    : BoardPieceKind.Ship;
                bool staffed = false;
                string? staffReason = null;
                if (kind == BoardPieceKind.Ship)
                {
                    // G4: one staffing truth = IsShipStaffed(+Treaties) + optional lore (Rogue+Lore).
                    // G2: Treaty/NA != Match; G3: empty-icon still needs Match crew.
                    int own = occ.Card.Owner != 0 ? occ.Card.Owner : occ.Card.Controller;
                    var treaties = own == 2 ? treatiesP2 : treatiesP1;
                    var staff = MovementRules.IsShipStaffed(printed, aboard, treaties);
                    bool lore = loreStaffed != null && loreStaffed.Contains(occ.InstanceId);
                    staffed = staff.Ok || lore;
                    staffReason = staffed
                        ? (staff.Ok ? staff.Reason : "Rogue Borg + Lore Returns")
                        : staff.Reason;
                }

                int rangeLeft = -1;
                bool cloaked = false;
                int dockedAtId = 0;
                int hullPercent = -1;
                if (occ.Card is ShipInstance shipInst)
                {
                    rangeLeft = shipInst.RangeLeft;
                    cloaked = shipInst.Cloaked;
                    dockedAtId = shipInst.DockedAtId;
                    hullPercent = shipInst.HullPercent;
                }
                list.Add(new BoardPiece
                {
                    Card = printed,
                    Kind = kind,
                    Owner = occ.Card.Owner != 0 ? occ.Card.Owner : occ.Card.Controller,
                    Controller = occ.Card.Controller != 0 ? occ.Card.Controller : occ.Card.Owner,
                    InstanceId = occ.InstanceId,
                    FaceUp = printed.FaceUp,
                    Zone = CardZone.Spaceline,
                    HostName = loc.Printed?.Name,
                    Occupied = aboard.Any(ModifierRules.IsPersonnelCard),
                    HasSecurityAboard = HasSkillName(aboard, "SECURITY"),
                    HasEngineerAboard = HasSkillName(aboard, "ENGINEER"),
                    Staffed = staffed,
                    StaffReason = staffReason,
                    SpacelineIndex = i,
                    Aboard = aboard,
                    RangeLeft = rangeLeft,
                    Stopped = occ.Card.Stopped,
                    Cloaked = cloaked,
                    DockedAtId = dockedAtId,
                    HullPercent = hullPercent
                });
                if (occ.InstanceId > 0) seen.Add(occ.InstanceId);
            }
        }

        AddTablePieces(list, TableP1, 1, seen);
        AddTablePieces(list, TableP2, 2, seen);
        return list;
    }

    public IReadOnlyList<string> ToSpacelineNames()
    {
        var names = new List<string>();
        foreach (var loc in Spaceline.Locations)
        {
            string? n = loc.Printed?.Name;
            if (!string.IsNullOrEmpty(n)) names.Add(n);
        }
        return names;
    }

    public IReadOnlyList<Card> CardsFromIds(IEnumerable<int> ids)
    {
        var list = new List<Card>();
        foreach (int id in ids)
        {
            if (ById.TryGetValue(id, out var inst))
                list.Add(inst.Printed);
        }
        return list;
    }

    /// <summary>
    /// Engine snapshot. Store board + spaceline + hands when the store has locations;
    /// otherwise the UI board (seed mid-game). Session fields always from <paramref name="seed"/>.
    /// </summary>
    public GameState ToGameState(GameStateSeed seed)
    {
        bool storeReady = Spaceline.Locations.Count > 0;
        var storePieces = storeReady
            ? ToBoardPieces(seed.TreatiesP1, seed.TreatiesP2, seed.LoreStaffedShipIds)
            : new List<BoardPiece>();
        var board = storeReady
            ? MergeStorePreferred(storePieces, seed.UiBoard)
            : seed.UiBoard.ToList();

        var hands1 = ResolveZoneCards(HandP1, seed.HandP1);
        var hands2 = ResolveZoneCards(HandP2, seed.HandP2);
        var line = storeReady && ToSpacelineNames().Count > 0
            ? ToSpacelineNames()
            : seed.Spaceline;

        return new GameState
        {
            Match = seed.Match,
            Segment = seed.Segment,
            ActivePlayer = seed.ActivePlayer,
            TurnNumber = seed.TurnNumber,
            SeedPhase = seed.SeedPhase,
            SeedSubPhase = seed.SeedSubPhase,
            SeedPileP1 = seed.SeedPileP1,
            SeedPileP2 = seed.SeedPileP2,
            CryoPersonnelSeededP1 = seed.CryoPersonnelSeededP1,
            CryoPersonnelSeededP2 = seed.CryoPersonnelSeededP2,
            NormalCardPlayAvailable = seed.NormalCardPlayAvailable,
            NormalCardPlayUsed = seed.NormalCardPlayUsed,
            StackOpen = seed.StackOpen,
            ResponsePlayer = seed.ResponsePlayer,
            StackTop = seed.StackTop,
            ScoreP1 = seed.ScoreP1,
            ScoreP2 = seed.ScoreP2,
            HandP1 = hands1,
            HandP2 = hands2,
            Board = board,
            HasGoddess = seed.HasGoddess,
            TentOpenP1 = seed.TentOpenP1,
            TentOpenP2 = seed.TentOpenP2,
            TentCountP1 = seed.TentCountP1,
            TentCountP2 = seed.TentCountP2,
            Spaceline = line,
            TreatiesP1 = seed.TreatiesP1,
            TreatiesP2 = seed.TreatiesP2,
            HasWhereNoOneHasGoneBeforeP1 = seed.HasWhereNoOneHasGoneBeforeP1,
            HasWhereNoOneHasGoneBeforeP2 = seed.HasWhereNoOneHasGoneBeforeP2,
            TentDownloadUsedP1 = seed.TentDownloadUsedP1,
            TentDownloadUsedP2 = seed.TentDownloadUsedP2,
            OncePerGameKeys = seed.OncePerGameKeys,
            StoppedInstanceIds = CollectStoppedInstanceIds(seed),
            UntilEndOfTurnKeys = seed.UntilEndOfTurnKeys
        };
    }

    public static string FormatStateLine(GameState state)
    {
        int ships = state.Board.Count(p => p.Kind == BoardPieceKind.Ship);
        var parts = new List<string> { $"ships={ships}" };
        foreach (var ship in state.Ships())
        {
            int crew = ship.Aboard?.Count(ModifierRules.IsPersonnelCard) ?? 0;
            string rangeBit = ship.RangeLeft >= 0 ? $" range={ship.RangeLeft}" : "";
            string stopBit = ship.Stopped ? " stopped=1" : "";
            string cloakBit = ship.Cloaked ? " cloaked=1" : "";
            string dockBit = ship.DockedAtId > 0 ? $" dock={ship.DockedAtId}" : "";
            string hullBit = ship.HullPercent > 0 ? $" hull={ship.HullPercent}" : "";
            parts.Add($"{ShortName(ship.Card)}#{ship.InstanceId} aboard={crew} staffed={(ship.Staffed ? 1 : 0)}{rangeBit}{stopBit}{cloakBit}{dockBit}{hullBit} host={ship.HostName ?? "-"}");
        }
        return "state: " + string.Join(" ", parts);
    }

    public string FormatDumpCrewLine()
    {
        int ships = 0;
        var parts = new List<string>();
        foreach (var loc in Spaceline.Locations)
        {
            foreach (var occ in loc.Occupants)
            {
                if (occ.Kind != OccupantKind.Ship) continue;
                ships++;
                int crew = occ.Crew.Personnel.Count;
                parts.Add($"{occ.Card.Name}#{occ.InstanceId} crewOn={crew} host={loc.Printed?.Name ?? "-"}");
            }
        }
        return $"dump: ships={ships} " + string.Join(" ", parts);
    }

    private IReadOnlyList<Card> ResolveZoneCards(List<int> ids, IReadOnlyList<Card> fallback)
    {
        if (ids.Count == 0) return fallback;
        var resolved = CardsFromIds(ids);
        return resolved.Count == ids.Count ? resolved : fallback;
    }

    private IReadOnlyList<int> CollectStoppedInstanceIds(GameStateSeed seed)
    {
        if (ById.Count == 0)
            return seed.StoppedInstanceIds;
        var fromStore = ById.Values
            .Where(i => i.Stopped && i.InstanceId > 0)
            .Select(i => i.InstanceId)
            .Distinct()
            .ToList();
        // UI fallback when Sync has not copied stops onto fresh wraps yet.
        if (fromStore.Count == 0 && seed.StoppedInstanceIds.Count > 0)
            return seed.StoppedInstanceIds;
        return fromStore;
    }

    private static List<BoardPiece> MergeStorePreferred(
        List<BoardPiece> storePieces,
        IReadOnlyList<BoardPiece> uiBoard)
    {
        var merged = new List<BoardPiece>(storePieces.Count + uiBoard.Count);
        var byId = new Dictionary<int, int>();
        for (int i = 0; i < storePieces.Count; i++)
        {
            var p = storePieces[i];
            merged.Add(p);
            if (p.InstanceId > 0) byId[p.InstanceId] = i;
        }

        foreach (var ui in uiBoard)
        {
            if (ui.InstanceId > 0 && byId.TryGetValue(ui.InstanceId, out int idx))
            {
                merged[idx] = OverlayStatus(merged[idx], ui);
                continue;
            }
            merged.Add(ui);
        }
        return merged;
    }

    /// <summary>E3/E3b: RangeLeft / Stopped / Cloak / Dock / Hull prefer store; solved / persist still UI. Crew / host stay store. G4: Staffed is store-only (no ui||store drift).</summary>
    private static BoardPiece OverlayStatus(BoardPiece store, BoardPiece ui) => new()
    {
        Card = store.Card,
        Kind = store.Kind,
        Owner = store.Owner,
        Controller = store.Controller,
        InstanceId = store.InstanceId,
        FaceUp = store.FaceUp,
        Zone = store.Zone != CardZone.Unknown ? store.Zone : ui.Zone,
        Status = ui.Status,
        HostName = store.HostName ?? ui.HostName,
        Occupied = store.Occupied || ui.Occupied,
        HasSecurityAboard = store.HasSecurityAboard || ui.HasSecurityAboard,
        HasEngineerAboard = store.HasEngineerAboard || ui.HasEngineerAboard,
        Persist = ui.Persist != default ? ui.Persist : store.Persist,
        Countdown = ui.Countdown != 0 ? ui.Countdown : store.Countdown,
        TurnScope = ui.TurnScope,
        PhasePoint = ui.PhasePoint,
        MissionSolved = ui.MissionSolved,
        AttemptBlocked = ui.AttemptBlocked,
        AttemptBlockReason = ui.AttemptBlockReason,
        RangeLeft = store.RangeLeft >= 0 ? store.RangeLeft : ui.RangeLeft,
        Stopped = store.Stopped || ui.Stopped,
        Cloaked = store.Cloaked || ui.Cloaked,
        DockedAtId = store.DockedAtId > 0 ? store.DockedAtId : ui.DockedAtId,
        HullPercent = store.HullPercent >= 0 ? store.HullPercent : ui.HullPercent,
        Staffed = store.Staffed,
        StaffReason = store.StaffReason ?? ui.StaffReason,
        SpacelineIndex = store.SpacelineIndex >= 0 ? store.SpacelineIndex : ui.SpacelineIndex,
        Aboard = store.Aboard.Count > 0 ? store.Aboard : ui.Aboard
    };

    private void AddTablePieces(List<BoardPiece> list, List<int> ids, int owner, HashSet<int> seen)
    {
        foreach (int id in ids)
        {
            if (id <= 0 || !seen.Add(id)) continue;
            if (!ById.TryGetValue(id, out var inst)) continue;
            list.Add(PieceFromPrinted(
                inst.Printed,
                MapKind(inst.Printed),
                spacelineIndex: -1,
                hostName: null,
                aboard: Array.Empty<Card>(),
                zone: CardZone.TableCore,
                owner: owner));
        }
    }

    private static BoardPiece PieceFromPrinted(
        Card c,
        BoardPieceKind kind,
        int spacelineIndex,
        string? hostName,
        IReadOnlyList<Card> aboard,
        CardZone zone,
        int owner = 0)
    {
        int own = owner != 0 ? owner : (c.OwnerPlayer != 0 ? c.OwnerPlayer : c.Controller);
        int ctrl = c.Controller != 0 ? c.Controller : own;
        return new BoardPiece
        {
            Card = c,
            Kind = kind,
            Owner = own,
            Controller = ctrl,
            InstanceId = c.InstanceId,
            FaceUp = c.FaceUp,
            Zone = zone,
            HostName = hostName,
            Occupied = aboard.Any(ModifierRules.IsPersonnelCard),
            HasSecurityAboard = HasSkillName(aboard, "SECURITY"),
            HasEngineerAboard = HasSkillName(aboard, "ENGINEER"),
            SpacelineIndex = spacelineIndex,
            Aboard = aboard
        };
    }

    private static List<Card> CardsInForce(Force force)
    {
        var list = new List<Card>(force.Personnel.Count + force.Equipment.Count);
        foreach (var p in force.Personnel) list.Add(p.Printed);
        foreach (var e in force.Equipment) list.Add(e.Printed);
        return list;
    }

    private static BoardPieceKind KindOfLocation(Location loc) => loc.Kind switch
    {
        LocationKind.TimeLocation => BoardPieceKind.TimeLocation,
        LocationKind.Span => MapKind(loc.Printed),
        _ => BoardPieceKind.Mission
    };

    private static BoardPieceKind MapKind(Card? c) => CardKinds.Of(c) switch
    {
        CardKind.Ship => BoardPieceKind.Ship,
        CardKind.Mission or CardKind.QMission => BoardPieceKind.Mission,
        CardKind.TimeLocation => BoardPieceKind.TimeLocation,
        CardKind.Facility => BoardPieceKind.Facility,
        CardKind.Event or CardKind.QEvent => BoardPieceKind.Event,
        CardKind.Incident => BoardPieceKind.Incident,
        CardKind.Objective => BoardPieceKind.Objective,
        CardKind.Dilemma or CardKind.QDilemma => BoardPieceKind.Dilemma,
        CardKind.Artifact or CardKind.QArtifact => BoardPieceKind.Artifact,
        CardKind.Tactic or CardKind.DamageMarker => BoardPieceKind.Tactic,
        CardKind.Site => BoardPieceKind.Site,
        CardKind.Interrupt or CardKind.QInterrupt => BoardPieceKind.InterruptToken,
        _ => BoardPieceKind.Other
    };

    private static bool HasSkillName(IEnumerable<Card> aboard, string skill)
    {
        foreach (var p in aboard)
        {
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase)
                    || kv.Key.Contains(skill, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static string ShortName(Card c)
    {
        string n = c.Name ?? "?";
        int space = n.LastIndexOf(' ');
        return space > 0 && n.Length - space < 12 ? n[(space + 1)..] : n;
    }


    public enum InPlaySide
    {
        /// <summary>Who currently controls the card (Lore commandeer).</summary>
        Controller,
        /// <summary>Printed owner / seed owner. Default for unique/persona (Glossary: restrict stays with owner).</summary>
        Owner
    }

    /// <summary>
    /// E4: In-play instances from spaceline (missions, occupants, forces) + TABLE.
    /// Hands excluded. Prefer <see cref="InPlaySide.Owner"/> for unique/persona.
    /// Optional <paramref name="nameOrPersona"/> filters by <see cref="PlayRules.PersonaKey"/>.
    /// </summary>
    public IEnumerable<CardInstance> InPlayInstances(
        int player = 0,
        InPlaySide side = InPlaySide.Owner,
        string? nameOrPersona = null)
    {
        string? key = string.IsNullOrWhiteSpace(nameOrPersona)
            ? null
            : nameOrPersona.Trim().ToLowerInvariant();

        foreach (var inst in EnumerateInPlayInstances())
        {
            if (player > 0)
            {
                int who = side == InPlaySide.Controller
                    ? EffectiveController(inst)
                    : EffectiveOwner(inst);
                if (who != player) continue;
            }

            if (key != null
                && !string.Equals(PlayRules.PersonaKey(inst.Printed), key, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return inst;
        }
    }

    /// <summary>E4: Printed cards in play (see <see cref="InPlayInstances"/>).</summary>
    public IEnumerable<Card> InPlay(
        int player = 0,
        InPlaySide side = InPlaySide.Owner,
        string? nameOrPersona = null) =>
        InPlayInstances(player, side, nameOrPersona).Select(i => i.Printed);

    /// <summary>
    /// E4: First in-play instance with the same persona for <paramref name="player"/>,
    /// excluding <paramref name="excludeInstanceId"/> (the card about to enter play).
    /// </summary>
    public CardInstance? FindConflictingUnique(
        int player,
        string personaKey,
        int excludeInstanceId = 0,
        InPlaySide side = InPlaySide.Owner)
    {
        foreach (var inst in InPlayInstances(player, side, personaKey))
        {
            if (excludeInstanceId > 0 && inst.InstanceId == excludeInstanceId)
                continue;
            return inst;
        }
        return null;
    }

    /// <summary>True when spaceline or TABLE has content (store usable for InPlay).</summary>
    public bool HasInPlaySurface =>
        Spaceline.Locations.Count > 0 || TableP1.Count > 0 || TableP2.Count > 0;

    private IEnumerable<CardInstance> EnumerateInPlayInstances()
    {
        var seen = new HashSet<int>();

        foreach (var loc in Spaceline.Locations)
        {
            if (loc.Printed != null)
            {
                var missionInst = ResolveInstance(loc.Printed);
                if (missionInst != null && TryAddSeen(seen, missionInst.InstanceId))
                    yield return missionInst;
            }

            foreach (var occ in loc.Occupants)
            {
                if (TryAddSeen(seen, occ.InstanceId))
                    yield return occ.Card;

                foreach (var p in occ.Crew.Personnel)
                {
                    if (TryAddSeen(seen, p.InstanceId))
                        yield return p;
                }
                foreach (var e in occ.Crew.Equipment)
                {
                    if (TryAddSeen(seen, e.InstanceId))
                        yield return e;
                }
            }

            foreach (var p in loc.AwayTeamP1.Personnel.Concat(loc.AwayTeamP2.Personnel))
            {
                if (TryAddSeen(seen, p.InstanceId))
                    yield return p;
            }
            foreach (var e in loc.AwayTeamP1.Equipment.Concat(loc.AwayTeamP2.Equipment))
            {
                if (TryAddSeen(seen, e.InstanceId))
                    yield return e;
            }
        }

        foreach (int id in TableP1.Concat(TableP2))
        {
            if (id <= 0 || !seen.Add(id)) continue;
            if (ById.TryGetValue(id, out var inst))
                yield return inst;
        }
    }

    private CardInstance? ResolveInstance(Card printed)
    {
        if (printed.InstanceId > 0 && ById.TryGetValue(printed.InstanceId, out var inst))
            return inst;
        // Mission columns may only hold Printed until Wrap runs.
        return printed.InstanceId > 0 ? Wrap(printed) : null;
    }

    private static bool TryAddSeen(HashSet<int> seen, int instanceId)
    {
        if (instanceId <= 0) return true;
        return seen.Add(instanceId);
    }

    private static int EffectiveController(CardInstance inst)
    {
        int c = inst.Controller;
        return c != 0 ? c : inst.Owner;
    }

    private static int EffectiveOwner(CardInstance inst)
    {
        int o = inst.Owner;
        return o != 0 ? o : inst.Controller;
    }

    public IEnumerable<string> DumpLines()
    {
        yield return "Board dump";
        yield return $"instances={ById.Count} locations={Spaceline.Locations.Count}";
        yield return $"hand P1={HandP1.Count} P2={HandP2.Count}  table P1={TableP1.Count} P2={TableP2.Count}";

        if (Spaceline.Locations.Count == 0)
        {
            yield return "(spaceline empty)";
            yield break;
        }

        for (int i = 0; i < Spaceline.Locations.Count; i++)
        {
            var loc = Spaceline.Locations[i];
            string barrier = loc.BarrierAfter ? " |Q-Net|" : "";
            yield return $"  [{i}] {loc.Kind} {loc.Label} span={loc.Span} q={loc.Quadrant ?? "-"} occ={loc.Occupants.Count}{barrier}";
            foreach (var occ in loc.Occupants)
            {
                yield return $"      {occ}";
                foreach (var line in occ.Crew.DumpLines())
                    yield return "        " + line;
            }
            foreach (var line in loc.AwayTeamP1.DumpLines())
                if (loc.AwayTeamP1.Personnel.Count + loc.AwayTeamP1.Equipment.Count > 0)
                    yield return "      " + line;
            foreach (var line in loc.AwayTeamP2.DumpLines())
                if (loc.AwayTeamP2.Personnel.Count + loc.AwayTeamP2.Equipment.Count > 0)
                    yield return "      " + line;
        }
    }
}