using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using StarTrekCCG.Models;

namespace StarTrekCCG.Network;

/// <summary>
/// JSON-DTO for GameAction over the wire. No Card object references — names/sets only.
/// Host resolves Cards via lookup callback before EngineAuthority.Evaluate.
/// </summary>
public sealed class NetActionDto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("player")]
    public int Player { get; set; }

    [JsonPropertyName("cardName")]
    public string? CardName { get; set; }

    [JsonPropertyName("cardSet")]
    public string? CardSet { get; set; }

    [JsonPropertyName("targetName")]
    public string? TargetName { get; set; }

    [JsonPropertyName("target2Name")]
    public string? Target2Name { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("instanceIds")]
    public int[]? InstanceIds { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static NetActionDto FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<NetActionDto>(json, JsonOptions);
        if (dto is null)
            throw new InvalidOperationException("NetActionDto deserialize returned null.");
        return dto;
    }

    public static NetActionDto ToDto(GameAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new NetActionDto
        {
            Kind = action.Kind.ToString(),
            Player = action.Player,
            CardName = action.Card?.Name,
            CardSet = action.Card?.SetFolder,
            TargetName = action.Target?.Name ?? action.TargetName,
            Target2Name = action.Target2?.Name,
            Note = action.Note,
            InstanceIds = BuildInstanceIds(action)
        };
    }

    /// <summary>
    /// Rebuild GameAction. Card/Target resolved only when lookup is provided (Host).
    /// Pass/EndPhase/EndTurn/Draw often need no Card.
    /// </summary>
    public static GameAction FromDto(NetActionDto dto, Func<string?, string?, Card?>? lookup = null)
        => FromDto(dto, lookup, byInstance: null);

    /// <summary>
    /// Host resolve. InstanceIds (card, target, target2) win over names so two copies
    /// of the same mission title stay distinct — required for span endpoint gaps.
    /// </summary>
    public static GameAction FromDto(
        NetActionDto dto,
        Func<string?, string?, Card?>? lookup,
        Func<int, Card?>? byInstance)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!Enum.TryParse<GameActionKind>(dto.Kind, ignoreCase: true, out var kind))
            throw new InvalidOperationException($"Unknown GameActionKind '{dto.Kind}'.");

        Card? card = null;
        Card? target = null;
        Card? target2 = null;
        // InstanceIds win. Name lookup runs only when that slot is still null
        // (Guest inject id missing on Host). PlayCard must not let that name
        // fallback reuse an in-play card — Host rebinds the card from the
        // acting player's hand after FromDto.
        if (byInstance != null && dto.InstanceIds is { Length: > 0 })
        {
            if (dto.InstanceIds[0] > 0)
                card = byInstance(dto.InstanceIds[0]);
            if (dto.InstanceIds.Length > 1 && dto.InstanceIds[1] > 0)
                target = byInstance(dto.InstanceIds[1]);
            if (dto.InstanceIds.Length > 2 && dto.InstanceIds[2] > 0)
                target2 = byInstance(dto.InstanceIds[2]);
        }
        if (lookup != null)
        {
            if (card == null && !string.IsNullOrWhiteSpace(dto.CardName))
                card = lookup(dto.CardName, dto.CardSet);
            if (target == null && !string.IsNullOrWhiteSpace(dto.TargetName))
                target = lookup(dto.TargetName, null);
            if (target2 == null && !string.IsNullOrWhiteSpace(dto.Target2Name))
                target2 = lookup(dto.Target2Name, null);
        }

        return new GameAction
        {
            Kind = kind,
            Player = dto.Player,
            Card = card,
            Target = target,
            Target2 = target2,
            TargetName = dto.TargetName,
            Note = dto.Note
        };
    }

    /// <summary>
    /// Slots stay aligned: [card, target, target2]. A zero means "absent" so a later
    /// id (Attempting-Ship on AttemptMission) is not read as the card.
    /// </summary>
    private static int[]? BuildInstanceIds(GameAction action)
    {
        int cardId = action.Card?.InstanceId > 0 ? action.Card.InstanceId : 0;
        int targetId = action.Target?.InstanceId > 0 ? action.Target.InstanceId : 0;
        int target2Id = action.Target2?.InstanceId > 0 ? action.Target2.InstanceId : 0;
        if (cardId == 0 && targetId == 0 && target2Id == 0) return null;
        if (target2Id > 0)
            return new[] { cardId, targetId, target2Id };
        if (targetId > 0)
            return new[] { cardId, targetId };
        return new[] { cardId };
    }
}
