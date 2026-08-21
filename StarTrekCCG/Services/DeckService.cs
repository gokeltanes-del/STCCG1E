using StarTrekCCG.Models;
using System.IO;
using System.Text.Json;

namespace StarTrekCCG.Services;

/// <summary>
/// Abschnitte eines Decks – explizit wählen, keine Auto-Zuordnung.
/// </summary>
public enum DeckSection
{
    Seed,
    Draw,
    QsTent,
    BattleBridge,
    QContinuum,
    SitePile,
    Tribble,
    /// <summary>Legacy flaches Side (v1-Dateien)</summary>
    SideLegacy
}

/// <summary>
/// Speichert und lädt Decks als .stdeck Dateien (Format v2, liest auch v1).
/// </summary>
public class DeckService
{
    public const string FormatV1 = "STCCG1E-Deck-v1";
    public const string FormatV2 = "STCCG1E-Deck-v2";
    public const string RequiredFormat = FormatV2;
    public const string FileExtension = ".stdeck";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SectionDisplayName(DeckSection section) => section switch
    {
        DeckSection.Seed => "Seed",
        DeckSection.Draw => "Draw",
        DeckSection.QsTent => "Q's Tent",
        DeckSection.BattleBridge => "Battle Bridge",
        DeckSection.QContinuum => "Q-Continuum",
        DeckSection.SitePile => "Site Pile",
        DeckSection.Tribble => "Tribble",
        DeckSection.SideLegacy => "Side (alt)",
        _ => section.ToString()
    };

    public void Save(Deck deck, string filePath)
    {
        deck.Format = FormatV2;
        deck.Modified = DateTime.Now;
        var json = JsonSerializer.Serialize(deck, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public Deck Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Deck-Datei nicht gefunden.", filePath);

        if (!filePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Ungültige Datei. Erwartet wird eine {FileExtension}-Datei.");

        var json = File.ReadAllText(filePath);
        var deck = JsonSerializer.Deserialize<Deck>(json, JsonOptions)
                   ?? throw new InvalidDataException("Die Datei konnte nicht gelesen werden.");

        if (!string.Equals(deck.Format, FormatV1, StringComparison.Ordinal) &&
            !string.Equals(deck.Format, FormatV2, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Das ist keine gültige Star Trek CCG Deck-Datei.\n\n" +
                $"Erwartetes Format: {FormatV1} oder {FormatV2}");
        }

        // v1 → v2: flaches Side bleibt in SideCards (SideLegacy), User kann manuell verschieben
        EnsureLists(deck);
        return deck;
    }

    private static void EnsureLists(Deck deck)
    {
        deck.SeedCards ??= new();
        deck.DrawCards ??= new();
        deck.QsTentCards ??= new();
        deck.BattleBridgeCards ??= new();
        deck.QContinuumCards ??= new();
        deck.SitePileCards ??= new();
        deck.TribbleCards ??= new();
        deck.SideCards ??= new();
    }

    /// <summary>
    /// Fügt eine Karte in den angegebenen Abschnitt ein (kein Auto-Suggest).
    /// </summary>
    public void AddCard(Deck deck, Card card, DeckSection section, int quantity = 1)
    {
        var list = GetList(deck, section);

        var existing = list.FirstOrDefault(e =>
            string.Equals(e.Name, card.Name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Set, card.SetFolder, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            list.Add(new DeckEntry
            {
                Name = card.Name,
                Set = card.SetFolder,
                Type = card.Type,
                Quantity = quantity,
                Card = card
            });
        }

        SortEntries(list);
    }

    public void RemoveCard(Deck deck, DeckEntry entry, DeckSection section)
    {
        GetList(deck, section).Remove(entry);
    }

    public void DecreaseQuantity(Deck deck, DeckEntry entry, DeckSection section)
    {
        entry.Quantity--;
        if (entry.Quantity <= 0)
            GetList(deck, section).Remove(entry);
    }

    public void MoveCard(Deck deck, DeckEntry entry, DeckSection from, DeckSection to)
    {
        if (from == to) return;

        var fromList = GetList(deck, from);
        fromList.Remove(entry);

        var toList = GetList(deck, to);
        var existing = toList.FirstOrDefault(e =>
            string.Equals(e.Name, entry.Name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Set, entry.Set, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Quantity += entry.Quantity;
        }
        else
        {
            toList.Add(entry);
            SortEntries(toList);
        }
    }

    /// <summary>Zuerst Typ, dann Name (A–Z) innerhalb des Typs.</summary>
    public static void SortEntries(List<DeckEntry> list)
    {
        list.Sort((a, b) =>
        {
            int t = string.Compare(a.Type ?? "", b.Type ?? "", StringComparison.OrdinalIgnoreCase);
            if (t != 0) return t;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    public static List<DeckEntry> GetList(Deck deck, DeckSection section) => section switch
    {
        DeckSection.Seed => deck.SeedCards,
        DeckSection.Draw => deck.DrawCards,
        DeckSection.QsTent => deck.QsTentCards,
        DeckSection.BattleBridge => deck.BattleBridgeCards,
        DeckSection.QContinuum => deck.QContinuumCards,
        DeckSection.SitePile => deck.SitePileCards,
        DeckSection.Tribble => deck.TribbleCards,
        DeckSection.SideLegacy => deck.SideCards,
        _ => deck.DrawCards
    };

    public static IEnumerable<DeckSection> AllSections { get; } = new[]
    {
        DeckSection.Seed,
        DeckSection.Draw,
        DeckSection.QsTent,
        DeckSection.BattleBridge,
        DeckSection.QContinuum,
        DeckSection.SitePile,
        DeckSection.Tribble,
        DeckSection.SideLegacy
    };
}