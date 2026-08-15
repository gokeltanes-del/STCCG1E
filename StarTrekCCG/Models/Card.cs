using System.Text.Json.Serialization;

namespace StarTrekCCG.Models;

/// <summary>
/// Basis-Modell für eine Star Trek CCG 1E Karte.
/// Entspricht dem Output von split_lackey_sets.py (cards.json).
/// Später werden wir Attribute sauber parsen (Integrity, Skills usw.).
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

    // Diese Felder kommen als Strings aus Lackey (z.B. "7" oder "7 / 8")
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

    // Weitere optionale Felder
    [JsonPropertyName("mission_dilemma_type")]
    public string? MissionDilemmaType { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("quadrant")]
    public string? Quadrant { get; set; }

    [JsonPropertyName("span")]
    public string? Span { get; set; }

    // Laufzeit-Felder (nicht aus JSON)
    [JsonIgnore]
    public string? FullImagePath { get; set; }

    [JsonIgnore]
    public string Set => SetFolder ?? "Unknown";

    public override string ToString() => $"{Name} ({Type})";
}
