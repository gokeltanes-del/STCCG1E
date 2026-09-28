using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarTrekCCG.Network;

/// <summary>
/// Non-blocking play fly-in reveal (Host after successful Play / Interrupt).
/// Guest mirrors animation; does not authorize board mutation (State still authoritative).
/// </summary>
public sealed class NetPlayRevealDto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("player")]
    public int Player { get; set; }

    [JsonPropertyName("cardName")]
    public string? CardName { get; set; }

    [JsonPropertyName("cardSet")]
    public string? CardSet { get; set; }

    [JsonPropertyName("instanceId")]
    public int InstanceId { get; set; }

    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Landing point X as fraction of overlay width (Host after Apply). Null = client resolves locally.</summary>
    [JsonPropertyName("targetNormX")]
    public double? TargetNormX { get; set; }

    /// <summary>Landing point Y as fraction of overlay height (Host after Apply).</summary>
    [JsonPropertyName("targetNormY")]
    public double? TargetNormY { get; set; }

    /// <summary>
    /// Play-action target (ship/outpost/mission InstanceId) so Guest lands on the same seat
    /// as Host even when the event card itself sits on TABLE or is not yet stacked.
    /// </summary>
    [JsonPropertyName("targetInstanceId")]
    public int TargetInstanceId { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static NetPlayRevealDto FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<NetPlayRevealDto>(json, JsonOptions);
        if (dto is null)
            throw new InvalidOperationException("NetPlayRevealDto deserialize returned null.");
        return dto;
    }
}
