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

        // Clone only. Host save.Log / deck names / attachments stay intact.
        MaskOpponentDeckName(clone, viewerPlayer);
        MaskFaceDownAttachments(clone, opponent);
        MaskLogSecrets(save, clone, viewerPlayer, opponent);

        return clone;
    }

    /// <summary>
    /// DeckNameP1 is player 1, DeckNameP2 is player 2. Viewer keeps their own name.
    /// </summary>
    private static void MaskOpponentDeckName(GameSave clone, int viewerPlayer)
    {
        if (viewerPlayer == 1)
            clone.DeckNameP2 = null;
        else
            clone.DeckNameP1 = null;
    }

    /// <summary>
    /// Face-down attachments lose printed identity. Face-up stays readable.
    /// Hidden Agenda lives in p1.table / p2.table (FaceUp false), not only AttachedEvents.
    /// Kind, host, and countdown stay so the guest still places the card.
    /// </summary>
    private static void MaskFaceDownAttachments(GameSave clone, int opponent)
    {
        if (clone.AttachedEvents != null)
        {
            foreach (var ev in clone.AttachedEvents)
            {
                if (ev == null || ev.FaceUp) continue;
                if (ev.Owner is 1 or 2 && ev.Owner != opponent) continue;
                if (ev.Card != null)
                {
                    ev.Card.Name = string.Empty;
                    ev.Card.Set = null;
                    ev.Card.Type = null;
                    ev.Card.FaceUp = false;
                }
                ev.EspionageAs = null;
                ev.EspionageOn = null;
            }
        }

        if (clone.Zones == null) return;
        string key = $"p{opponent}.table";
        if (!clone.Zones.TryGetValue(key, out var list) || list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var card = list[i];
            if (card == null || card.FaceUp) continue;
            list[i] = MaskCardRef(card);
        }
    }

    /// <summary>
    /// Guest copy of the log only. Secret names are redacted; the host log is not written.
    /// </summary>
    private static void MaskLogSecrets(GameSave source, GameSave clone, int viewerPlayer, int opponent)
    {
        if (clone.Log == null || clone.Log.Count == 0) return;
        var names = CollectSecretNames(source, viewerPlayer, opponent);
        if (names.Count == 0) return;
        foreach (var entry in clone.Log)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Text)) continue;
            entry.Text = RedactSecretNames(entry.Text, names);
        }
    }

    private static List<string> CollectSecretNames(GameSave source, int viewerPlayer, int opponent)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string trimmed = name.Trim();
            if (trimmed.Length < 2) return;
            found.Add(trimmed);
        }

        bool probeHands = AlienProbeInPlay(source);
        if (source.Zones != null)
        {
            foreach (var suffix in PrivateZoneSuffixes)
            {
                if (probeHands && suffix == "hand") continue;
                string key = $"p{opponent}.{suffix}";
                if (!source.Zones.TryGetValue(key, out var list) || list == null) continue;
                foreach (var card in list)
                    Add(card?.Name);
            }

            string tableKey = $"p{opponent}.table";
            if (source.Zones.TryGetValue(tableKey, out var table) && table != null)
            {
                foreach (var card in table)
                    if (card != null && !card.FaceUp)
                        Add(card.Name);
            }
        }

        var revealed = RevealedInstanceIds(source);
        var hiddenIds = HiddenTableIds(source, opponent, revealed);
        if (source.Table != null)
        {
            foreach (var snap in source.Table)
            {
                if (snap == null || snap.Owner != opponent) continue;
                if (revealed.Contains(snap.InstanceId)) continue;
                if (!snap.FaceUp || hiddenIds.Contains(snap.Id))
                    Add(snap.Name);
            }
        }

        if (source.AttachedEvents != null)
        {
            foreach (var ev in source.AttachedEvents)
            {
                if (ev == null || ev.FaceUp) continue;
                if (ev.Owner is 1 or 2 && ev.Owner != opponent) continue;
                Add(ev.Card?.Name);
            }
        }

        Add(viewerPlayer == 1 ? source.DeckNameP2 : source.DeckNameP1);
        return found.OrderByDescending(n => n.Length).ToList();
    }

    /// <summary>Unrevealed seed-under and fogged stack children (table snap ids).</summary>
    private static HashSet<int> HiddenTableIds(GameSave source, int opponent, HashSet<int> revealed)
    {
        var ids = new HashSet<int>();
        if (source.Table == null) return ids;
        var byId = source.Table.ToDictionary(t => t.Id);

        void Take(List<StackSnap>? stacks)
        {
            if (stacks == null) return;
            foreach (var st in stacks)
            {
                if (st?.ChildIds == null) continue;
                foreach (int cid in st.ChildIds)
                {
                    if (!byId.TryGetValue(cid, out var snap)) continue;
                    if (snap.Owner != opponent) continue;
                    if (revealed.Contains(snap.InstanceId)) continue;
                    ids.Add(snap.Id);
                }
            }
        }

        Take(source.SeedUnder);
        Take(source.Stacks);
        return ids;
    }

    private static string RedactSecretNames(string text, List<string> namesLongestFirst)
    {
        foreach (var name in namesLongestFirst)
        {
            int start = 0;
            while (start < text.Length)
            {
                int i = text.IndexOf(name, start, StringComparison.OrdinalIgnoreCase);
                if (i < 0) break;
                int end = i + name.Length;
                bool left = i == 0 || !IsNameChar(text[i - 1]);
                bool right = end >= text.Length || !IsNameChar(text[end]);
                if (left && right)
                {
                    text = string.Concat(text.AsSpan(0, i), "(hidden)", text.AsSpan(end));
                    start = i + "(hidden)".Length;
                }
                else
                {
                    start = i + 1;
                }
            }
        }
        return text;
    }

    private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '\'' || c == '-';

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
