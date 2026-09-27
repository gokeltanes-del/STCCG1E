using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarTrekCCG.Network;

/// <summary>
/// Lobby-room payloads (deck pick / ready / start) over NetMessage framing.
/// netztauglich: Host authoritative for StartGame when both ready.
/// </summary>
public static class NetLobbyDto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed class DeckPick
    {
        [JsonPropertyName("player")]
        public int Player { get; set; }

        [JsonPropertyName("deckName")]
        public string DeckName { get; set; } = string.Empty;
    }

    public sealed class Ready
    {
        [JsonPropertyName("player")]
        public int Player { get; set; }

        [JsonPropertyName("ready")]
        public bool IsReady { get; set; }

        [JsonPropertyName("deckName")]
        public string DeckName { get; set; } = string.Empty;

        [JsonPropertyName("deckJson")]
        public string? DeckJson { get; set; }
    }

    public sealed class Status
    {
        [JsonPropertyName("player")]
        public int Player { get; set; }

        [JsonPropertyName("deckName")]
        public string? DeckName { get; set; }

        [JsonPropertyName("ready")]
        public bool Ready { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public sealed class StartGame
    {
        [JsonPropertyName("deckP1Name")]
        public string DeckP1Name { get; set; } = string.Empty;

        [JsonPropertyName("deckP2Name")]
        public string DeckP2Name { get; set; } = string.Empty;

        [JsonPropertyName("deckP1Json")]
        public string DeckP1Json { get; set; } = string.Empty;

        [JsonPropertyName("deckP2Json")]
        public string DeckP2Json { get; set; } = string.Empty;

        /// <summary>True when both players accepted Skip seed phase; Host runs AutoCompleteSeed.</summary>
        [JsonPropertyName("skipSeedPhase")]
        public bool SkipSeedPhase { get; set; }
    }

    /// <summary>Lobby vote: propose / accept / decline Skip seed phase (before StartGame).</summary>
    public sealed class SkipSeedVote
    {
        public static class Actions
        {
            public const string Propose = "propose";
            public const string Accept = "accept";
            public const string Decline = "decline";
        }

        [JsonPropertyName("player")]
        public int Player { get; set; }

        /// <summary>propose | accept | decline</summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = Actions.Propose;
    }

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    public static T FromJson<T>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (value is null)
            throw new InvalidOperationException($"Lobby DTO deserialize returned null ({typeof(T).Name}).");
        return value;
    }
}

/// <summary>Args for NetworkLobbyWindow.GameStarting (both sides after StartGame).</summary>
public sealed class LobbyGameStartArgs : EventArgs
{
    public bool IsHost { get; init; }
    public string DeckP1Name { get; init; } = string.Empty;
    public string DeckP2Name { get; init; } = string.Empty;
    public string DeckP1Json { get; init; } = string.Empty;
    public string DeckP2Json { get; init; } = string.Empty;
    /// <summary>Both accepted Skip seed phase — Host AutoCompleteSeed then Play.</summary>
    public bool SkipSeedPhase { get; init; }
}