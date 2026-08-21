using System.Text.Json.Serialization;
using StarTrekCCG.Models;

namespace StarTrekCCG.Models;

/// <summary>
/// Ein Eintrag in einem Deck: eine Karte + wie oft sie enthalten ist.
/// </summary>
public class DeckEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("set")]
    public string? Set { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    // Wird zur Laufzeit gesetzt, nicht in der JSON-Datei gespeichert
    [JsonIgnore]
    public Card? Card { get; set; }

    public override string ToString() => $"{Quantity}x {Name}";
}
