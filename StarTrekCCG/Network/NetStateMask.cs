using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace StarTrekCCG.Network;

/// <summary>
/// Masks private zones in a GameSave for a viewer (Guest). Not a game rule - fog of war only.
/// Opponent hand + private decks (draw/seed/.) become FaceDown with names cleared; counts kept.
/// Glossary Alien Probe: both hands stay clear (symmetric Host/Guest).
/// Occupancy / Away Team / docked ships in stacks: opponent-owned stack children masked (§6.3 / §7).
/// §12.12 Looking-at-cards exceptions are UI stubs; this mask is the network baseline.
/// </summary>
public static class NetStateMask
{
    private static readonly string[] PrivateZoneSuffixes =
    {
        "hand", "draw", "seed", "door", "mission", "dilemma", "facility",
        "qs", "bb", "qc", "site", "tribble", "side"
    };

    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Returns a deep-cloned GameSave where the non-viewer player's private zones are masked.
    /// Viewer keeps own hand clear; opponent hand is FaceDown / name empty unless Alien Probe is in play.
    /// </summary>
    public static GameSave MaskForViewer(GameSave save, int viewerPlayer)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (viewerPlayer is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(viewerPlayer), "viewerPlayer must be 1 or 2.");

        var clone = Clone(save);
        int opponent = viewerPlayer == 1 ? 2 : 1;
        string prefix = $"p{opponent}.";
        bool probeHands = AlienProbeInPlay(clone);

        foreach (var suffix in PrivateZoneSuffixes)
        {
            // Glossary Alien Probe: continuous both hands revealed — do not fog hand zones.
            if (probeHands && suffix == "hand")
                continue;

            string key = prefix + suffix;
            if (!clone.Zones.TryGetValue(key, out var list) || list == null || list.Count == 0)
                continue;

            // Discard / out-of-play / table permanents are public-ish; still mask name for
            // hand + seed + draw family. All listed private suffixes get masked.
            clone.Zones[key] = list.Select(MaskCardRef).ToList();
        }

        // Already encountered: dilemma still under the mission, or artifact not yet acquired.
        // Those faces are public. Unrevealed seeds stay nameless.
        var revealedIds = RevealedInstanceIds(clone);

        // Face-down table cards belonging to opponent: clear printed identity for viewer.
        // Stopped and RangeLeft on the row stay.
        foreach (var snap in clone.Table)
        {
            if (snap.Owner == opponent && !snap.FaceUp)
            {
                if (revealedIds.Contains(snap.InstanceId))
                    continue;
                snap.Name = string.Empty;
                snap.Set = null;
                snap.Type = null;
            }
        }

        // Seed-under-mission: opponent dilemmas/artifacts stay FaceDown + nameless for Guest fog.
        // Counts (SeedUnder ChildIds / Table rows) kept so stack depth matches Host.
        // Revealed seeds keep name/set/type so the Guest detail can show the real face.
        // Stopped and RangeLeft on the row stay. HeldIds and the reveal lists are not masked.
        if (clone.SeedUnder != null && clone.SeedUnder.Count > 0 && clone.Table != null)
        {
            var byId = clone.Table.ToDictionary(t => t.Id);
            foreach (var st in clone.SeedUnder)
            {
                foreach (int cid in st.ChildIds)
                {
                    if (!byId.TryGetValue(cid, out var snap)) continue;
                    if (snap.Owner != opponent) continue;
                    if (revealedIds.Contains(snap.InstanceId))
                        continue;
                    snap.FaceUp = false;
                    snap.Name = string.Empty;
                    snap.Set = null;
                    snap.Type = null;
                    snap.Visible = false;
                }
            }
        }

        // Occupancy / Away Team / docked ships: opponent-owned cards in host stacks are fogged.
        // Host ships/facilities/missions themselves stay named; stack children lose identity.
        // Stopped, RangeLeft, HeldIds, and reveal lists stay on the snapshot.
        MaskOpponentStackOccupancy(clone, opponent, revealedIds);

        return clone;
    }

    /// <summary>InstanceIds of seeds/artifacts already revealed under a mission.</summary>
    private static HashSet<int> RevealedInstanceIds(GameSave save)
    {
        var ids = new HashSet<int>();
        if (save.RevealedSeeds != null)
        {
            foreach (var r in save.RevealedSeeds)
                if (r != null && r.CardInstanceId > 0)
                    ids.Add(r.CardInstanceId);
        }
        if (save.RevealedArtifacts != null)
        {
            foreach (var r in save.RevealedArtifacts)
                if (r != null && r.CardInstanceId > 0)
                    ids.Add(r.CardInstanceId);
        }
        return ids;
    }

    /// <summary>Glossary Alien Probe on table / attached — both hands revealed.</summary>
    public static bool AlienProbeInPlay(GameSave save)
    {
        if (save == null) return false;
        if (save.AttachedEvents != null)
        {
            foreach (var ev in save.AttachedEvents)
            {
                if (ev == null) continue;
                if (string.Equals(ev.Kind, "Probe", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (ev.Card != null && NameIsAlienProbe(ev.Card.Name))
                    return true;
            }
        }
        if (save.Zones != null)
        {
            foreach (var key in new[] { "p1.table", "p2.table" })
            {
                if (!save.Zones.TryGetValue(key, out var list) || list == null) continue;
                if (list.Any(r => r != null && NameIsAlienProbe(r.Name)))
                    return true;
            }
        }
        if (save.Table != null)
        {
            if (save.Table.Any(t => t != null && NameIsAlienProbe(t.Name)))
                return true;
        }
        return false;
    }

    private static bool NameIsAlienProbe(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Equals("Alien Probe", StringComparison.OrdinalIgnoreCase);

    private static void MaskOpponentStackOccupancy(GameSave clone, int opponent, HashSet<int> revealedIds)
    {
        if (clone.Stacks == null || clone.Stacks.Count == 0 || clone.Table == null)
            return;

        var byId = clone.Table.ToDictionary(t => t.Id);
        foreach (var st in clone.Stacks)
        {
            foreach (int cid in st.ChildIds)
            {
                if (!byId.TryGetValue(cid, out var snap)) continue;
                if (snap.Owner != opponent) continue;
                // Already shown. Keep the face so the reveal marker still resolves.
                if (revealedIds.Contains(snap.InstanceId))
                    continue;
                // Printed identity only. Stopped and RangeLeft stay on this row.
                snap.FaceUp = false;
                snap.Name = string.Empty;
                snap.Set = null;
                snap.Type = null;
                snap.Visible = false;
            }
        }
    }

    private static CardRef MaskCardRef(CardRef r) => new()
    {
        Name = string.Empty,
        Set = null,
        Type = null,
        InstanceId = r.InstanceId,
        Owner = r.Owner,
        Controller = r.Controller,
        FaceUp = false
    };

    private static GameSave Clone(GameSave save)
    {
        var json = JsonSerializer.Serialize(save, CloneOptions);
        var clone = JsonSerializer.Deserialize<GameSave>(json, CloneOptions);
        if (clone is null)
            throw new InvalidOperationException("GameSave clone failed.");
        // Dictionary comparer is not preserved by System.Text.Json - rebuild case-insensitive.
        if (clone.Zones != null)
            clone.Zones = new Dictionary<string, List<CardRef>>(clone.Zones, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}
