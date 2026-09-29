using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarTrekCCG.Network;

/// <summary>One catalog face on a choice or reveal. Personnel battle may send several.</summary>
public sealed class NetChoiceFace
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("set")]
    public string? Set { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("instanceId")]
    public int InstanceId { get; set; }
}

/// <summary>
/// Choice / response-window payloads over NetMessage framing (Phase 4).
/// netztauglich: Host authoritative; Decide stays Engine/TimingRules; UI shows dialogs / waits / replies.
/// kind: "choice" | "responseWindow" on request; response carries selectedOption and/or passed.
/// </summary>
public sealed class NetChoiceDto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static class Kinds
    {
        public const string Choice = "choice";
        public const string ResponseWindow = "responseWindow";
        public const string ResponsePass = "responsePass";
        /// <summary>OK / Yes-No card reveal. Guest displays; Host already decided the text.</summary>
        public const string Reveal = "reveal";
        /// <summary>Watcher sees an encountered dilemma/artifact. No reply. The attempter clicks.</summary>
        public const string RevealMirror = "revealMirror";
        /// <summary>Hide the watcher encounter face.</summary>
        public const string RevealMirrorClose = "revealMirrorClose";
    }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Request: choice | responseWindow. Response may echo or use responsePass.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = Kinds.Choice;

    /// <summary>Player who must decide (1 or 2).</summary>
    [JsonPropertyName("targetPlayer")]
    public int TargetPlayer { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("options")]
    public string[]? Options { get; set; }

    [JsonPropertyName("cardName")]
    public string? CardName { get; set; }

    /// <summary>Catalog set so the viewer can show a face without reading masked seeds.</summary>
    [JsonPropertyName("cardSet")]
    public string? CardSet { get; set; }

    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("instanceId")]
    public int InstanceId { get; set; }

    /// <summary>
    /// Personnel-battle casualties. Null keeps the single CardName face.
    /// Empty means the result text has no card face. Several means those faces, not one stand-in.
    /// </summary>
    [JsonPropertyName("faces")]
    public NetChoiceFace[]? Faces { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }

    /// <summary>Legal response summary for responseWindow (names only; full list optional).</summary>
    [JsonPropertyName("legalNames")]
    public string[]? LegalNames { get; set; }

    [JsonPropertyName("legalCount")]
    public int? LegalCount { get; set; }

    /// <summary>Response: chosen option label (choice kind).</summary>
    [JsonPropertyName("selectedOption")]
    public string? SelectedOption { get; set; }

    /// <summary>Response: true when responder passed the response window.</summary>
    [JsonPropertyName("passed")]
    public bool? Passed { get; set; }

    /// <summary>Response: true when local client timed out (Host may also simulate timeout).</summary>
    [JsonPropertyName("timedOut")]
    public bool? TimedOut { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static NetChoiceDto FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<NetChoiceDto>(json, JsonOptions);
        if (dto is null)
            throw new InvalidOperationException("NetChoiceDto deserialize returned null.");
        return dto;
    }
}
