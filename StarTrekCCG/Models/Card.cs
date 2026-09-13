using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarTrekCCG.Models;

/// <summary>
/// Printed card + per-copy runtime fields.
/// Copy this file over Models/Card.cs in the VS project (do not keep the old Card without InstanceId).
/// </summary>
public class Card
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("affiliation")]
    public string? Affiliation { get; set; }

    [JsonPropertyName("set_folder")]
    public string? SetFolder { get; set; }

    [JsonPropertyName("release_raw")]
    public string? ReleaseRaw { get; set; }

    [JsonPropertyName("rarity_info")]
    public string? RarityInfo { get; set; }

    [JsonPropertyName("uniqueness")]
    public string? Uniqueness { get; set; }

    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("integrity_or_range")]
    public string? IntegrityOrRange { get; set; }

    [JsonPropertyName("cunning_or_weapons")]
    public string? CunningOrWeapons { get; set; }

    [JsonPropertyName("strength_or_shields")]
    public string? StrengthOrShields { get; set; }

    [JsonPropertyName("points")]
    public string? Points { get; set; }

    [JsonPropertyName("icons")]
    public string? Icons { get; set; }

    [JsonPropertyName("staff")]
    public string? Staff { get; set; }

    [JsonPropertyName("characteristics")]
    public string? Characteristics { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("old_image_file")]
    public string? OldImageFile { get; set; }

    [JsonPropertyName("mission_dilemma_type")]
    public string? MissionDilemmaType { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("quadrant")]
    public string? Quadrant { get; set; }

    [JsonPropertyName("span")]
    public string? Span { get; set; }

    [JsonIgnore]
    public string? FullImagePath { get; set; }

    /// <summary>Per-copy id in a running game. 0 = database prototype (not in play).</summary>
    [JsonIgnore]
    public int InstanceId { get; set; }

    /// <summary>Player who owns the card (deck). 0 = shared / mission.</summary>
    [JsonIgnore]
    public int OwnerPlayer { get; set; }

    /// <summary>Player who currently controls it (commandeer / Lore Returns / assimilation).</summary>
    [JsonIgnore]
    public int Controller { get; set; }

    /// <summary>False = Hidden Agenda / seed facedown / opponent private pile.</summary>
    [JsonIgnore]
    public bool FaceUp { get; set; } = true;

    /// <summary>Frame of Mind: this copy is 3-3-3 Non-Aligned until cured.</summary>
    [JsonIgnore]
    public bool FramedOfMind { get; set; }

    [JsonIgnore]
    public List<string>? FrameSkills { get; set; }

    /// <summary>Multi-affiliation current mode (FED/ROM/…). Empty = not chosen yet.</summary>
    [JsonIgnore]
    public string? CurrentAffiliation { get; set; }

    /// <summary>Personnel is quarantined (e.g. Hyper-Aging). Cannot leave/beam away.</summary>
    [JsonIgnore]
    public bool Quarantined { get; set; }

    /// <summary>Personnel is in stasis (e.g. Phased Matter, Alien Abduction). Cannot leave or act.</summary>
    [JsonIgnore]
    public bool InStasis { get; set; }

    /// <summary>Personnel is disabled (e.g. Ktarian Game). Cannot act or use skills/attributes.</summary>
    [JsonIgnore]
    public bool Disabled { get; set; }

    /// <summary>True if leave/beam is blocked (Quarantine or Stasis or Disabled).</summary>
    [JsonIgnore]
    public bool IsLeaveBlocked => Quarantined || InStasis || Disabled;

    [JsonIgnore]
    public string Set => SetFolder ?? "Unknown";

    public override string ToString() =>
        InstanceId > 0 ? $"{Name} ({Type}) #{InstanceId}" : $"{Name} ({Type})";
}