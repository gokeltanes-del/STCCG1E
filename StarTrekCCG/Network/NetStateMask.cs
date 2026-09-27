using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace StarTrekCCG.Network;

/// <summary>
/// Masks private zones in a GameSave for a viewer (Guest). Not a game rule — fog of war only.
/// Opponent hand + private decks (draw/seed/…) become FaceDown with names cleared; counts kept.
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
    /// Viewer keeps own hand clear; opponent hand is FaceDown / name empty.
    /// </summary>
    public static GameSave MaskForViewer(GameSave save, int viewerPlayer)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (viewerPlayer is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(viewerPlayer), "viewerPlayer must be 1 or 2.");

        var clone = Clone(save);
        int opponent = viewerPlayer == 1 ? 2 : 1;
        string prefix = $"p{opponent}.";

        foreach (var suffix in PrivateZoneSuffixes)
        {
            string key = prefix + suffix;
            if (!clone.Zones.TryGetValue(key, out var list) || list == null || list.Count == 0)
                continue;

            // Discard / out-of-play / table permanents are public-ish; still mask name for
            // hand + seed + draw family. All listed private suffixes get masked.
            clone.Zones[key] = list.Select(MaskCardRef).ToList();
        }

        // Face-down table cards belonging to opponent: clear printed identity for viewer.
        foreach (var snap in clone.Table)
        {
            if (snap.Owner == opponent && !snap.FaceUp)
            {
                snap.Name = string.Empty;
                snap.Set = null;
                snap.Type = null;
            }
        }

        return clone;
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
        // Dictionary comparer is not preserved by System.Text.Json — rebuild case-insensitive.
        if (clone.Zones != null)
            clone.Zones = new Dictionary<string, List<CardRef>>(clone.Zones, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}
