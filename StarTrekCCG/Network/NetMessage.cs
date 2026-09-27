using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarTrekCCG.Network;

// Verb: network transport tcp json
/// <summary>
/// JSON-Envelope für Client-Server-Transport (Phase 1).
/// Keine Spiel-Logik — nur Typ + Payload + optionale Korrelation.
/// </summary>
public sealed class NetMessage
{
    public static class Types
    {
        public const string Handshake = "handshake";
        public const string Ping = "ping";
        public const string Pong = "pong";
        public const string Action = "action";
        public const string State = "state";
        public const string Error = "error";
    }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payloadJson")]
    public string? PayloadJson { get; set; }

    [JsonPropertyName("seq")]
    public long? Seq { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(NetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return JsonSerializer.Serialize(message, JsonOptions);
    }

    public static NetMessage Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var msg = JsonSerializer.Deserialize<NetMessage>(json, JsonOptions);
        if (msg is null)
            throw new InvalidOperationException("NetMessage deserialize returned null.");
        return msg;
    }

    public static NetMessage Create(string type, string? payloadJson = null, long? seq = null, string? correlationId = null)
        => new()
        {
            Type = type,
            PayloadJson = payloadJson,
            Seq = seq,
            CorrelationId = correlationId
        };
}