using System.Text.Json.Serialization;

namespace StarTrekCCG.Models;

/// <summary>
/// Komplettes Deck: Seed, Draw und benannte Side Decks (1E).
/// Format v2 – v1-Dateien mit flachem "side" werden beim Laden migriert.
/// </summary>
public class Deck
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "STCCG1E-Deck-v2";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Deck";

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.Now;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.Now;

    /// <summary>Seed Deck (Missions, Dilemmas, Doorways, Facilities, …)</summary>
    [JsonPropertyName("seed")]
    public List<DeckEntry> SeedCards { get; set; } = new();

    /// <summary>Draw Deck</summary>
    [JsonPropertyName("draw")]
    public List<DeckEntry> DrawCards { get; set; } = new();

    /// <summary>Q's Tent Side Deck (bis 13 verschiedene Karten, keine Duplikate – Prüfung später)</summary>
    [JsonPropertyName("qs_tent")]
    public List<DeckEntry> QsTentCards { get; set; } = new();

    /// <summary>Battle Bridge Side Deck (Tactic-Karten)</summary>
    [JsonPropertyName("battle_bridge")]
    public List<DeckEntry> BattleBridgeCards { get; set; } = new();

    /// <summary>Q-Continuum Side Deck</summary>
    [JsonPropertyName("q_continuum")]
    public List<DeckEntry> QContinuumCards { get; set; } = new();

    /// <summary>Site Pile (bis 6 Site-Karten)</summary>
    [JsonPropertyName("site_pile")]
    public List<DeckEntry> SitePileCards { get; set; } = new();

    /// <summary>Tribble / Trouble Side Deck</summary>
    [JsonPropertyName("tribble")]
    public List<DeckEntry> TribbleCards { get; set; } = new();

    /// <summary>
    /// Legacy flaches Side-Deck (v1). Beim Laden von v1 gefüllt; neue Decks nutzen die benannten Listen.
    /// </summary>
    [JsonPropertyName("side")]
    public List<DeckEntry> SideCards { get; set; } = new();

    [JsonIgnore] public int SeedCount => SeedCards.Sum(c => c.Quantity);
    [JsonIgnore] public int DrawCount => DrawCards.Sum(c => c.Quantity);
    [JsonIgnore] public int QsTentCount => QsTentCards.Sum(c => c.Quantity);
    [JsonIgnore] public int BattleBridgeCount => BattleBridgeCards.Sum(c => c.Quantity);
    [JsonIgnore] public int QContinuumCount => QContinuumCards.Sum(c => c.Quantity);
    [JsonIgnore] public int SitePileCount => SitePileCards.Sum(c => c.Quantity);
    [JsonIgnore] public int TribbleCount => TribbleCards.Sum(c => c.Quantity);
    [JsonIgnore] public int SideLegacyCount => SideCards.Sum(c => c.Quantity);

    [JsonIgnore]
    public int SideTotalCount =>
        QsTentCount + BattleBridgeCount + QContinuumCount + SitePileCount + TribbleCount + SideLegacyCount;

    [JsonIgnore]
    public int TotalCards => SeedCount + DrawCount + SideTotalCount;

    public override string ToString() =>
        $"{Name} (Seed {SeedCount} / Draw {DrawCount} / Side {SideTotalCount})";
}