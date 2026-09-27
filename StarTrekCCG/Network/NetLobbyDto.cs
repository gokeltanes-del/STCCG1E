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
}