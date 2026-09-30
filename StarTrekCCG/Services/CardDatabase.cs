using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StarTrekCCG.Models;

namespace StarTrekCCG.Services;

/// <summary>
/// Lädt alle cards.json aus den Set-Unterordnern und stellt die Karten bereit.
/// Erwartete Struktur:
///   DataRoot/
///     Premiere/
///       cards.json
///       Premiere_Personnel_....jpg
///     First_Contact/
///       cards.json
///       ...
/// </summary>
public class CardDatabase
{
    private readonly List<Card> _cards = new();
    private readonly Dictionary<string, Card> _byCardId = new(StringComparer.Ordinal);
    private readonly string _dataRoot;

    public IReadOnlyList<Card> AllCards => _cards.AsReadOnly();

    public CardDatabase(string dataRootPath)
    {
        _dataRoot = dataRootPath;
    }

    /// <summary>
    /// Lädt alle Sets. Gibt die Anzahl der erfolgreich geladenen Karten zurück.
    /// </summary>
    public int LoadAll()
    {
        _cards.Clear();
        _byCardId.Clear();

        if (!Directory.Exists(_dataRoot))
        {
            throw new DirectoryNotFoundException($"Data folder not found: {_dataRoot}");
        }

        // Data/PR/cards.json  or  Data/Sets/PR/cards.json
        var setDirs = Directory.EnumerateFiles(_dataRoot, "cards.json", SearchOption.AllDirectories)
            .Select(f => Path.GetDirectoryName(f)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        foreach (var setDir in setDirs)
        {
            var jsonPath = Path.Combine(setDir, "cards.json");
            if (!File.Exists(jsonPath))
                continue;

            try
            {
                var json = File.ReadAllText(jsonPath);
                var cardsInSet = JsonSerializer.Deserialize<List<Card>>(json, options);

                if (cardsInSet == null)
                    continue;

                var setName = new DirectoryInfo(setDir).Name;

                foreach (var card in cardsInSet)
                {
                    // set_folder aus dem Ordnernamen nachziehen, falls leer
                    if (string.IsNullOrWhiteSpace(card.SetFolder))
                        card.SetFolder = setName;

                    // Vollständigen Bildpfad setzen (mehrere Fallbacks – keine Ban-Liste)
                    card.FullImagePath = ResolveImagePath(setDir, card);

                    _cards.Add(card);
                }
            }
            catch (Exception ex)
            {
                // In Phase 0 nur loggen, später besser machen
                System.Diagnostics.Debug.WriteLine($"Error loading {jsonPath}: {ex.Message}");
            }
        }

        RebuildCardIdIndex();
        return _cards.Count;
    }

    private void RebuildCardIdIndex()
    {
        _byCardId.Clear();
        foreach (var card in _cards)
        {
            var id = card.CardId;
            if (string.IsNullOrEmpty(id)) continue;
            if (!_byCardId.ContainsKey(id))
                _byCardId[id] = card;
        }
    }

    /// <summary>
    /// Sucht Bild unter Image, OldImageFile (Lackey PR89 etc.) und gängigen Varianten.
    /// Keine Tournament-Blacklist – alle Karten sind im privaten Client spielbar.
    /// </summary>
    private static string? ResolveImagePath(string setDir, Card card)
    {
        var candidates = new List<string>();
        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string n = name.Trim();
            candidates.Add(Path.Combine(setDir, n));
            candidates.Add(Path.Combine(setDir, Path.GetFileName(n)));
            // Lackey oft ohne Extension
            if (!Path.HasExtension(n))
            {
                candidates.Add(Path.Combine(setDir, n + ".jpg"));
                candidates.Add(Path.Combine(setDir, n + ".png"));
                candidates.Add(Path.Combine(setDir, n + ".jpeg"));
            }
        }

        Add(card.Image);
        Add(card.OldImageFile);
        // Premiere Raise the Stakes etc.: PR89
        if (!string.IsNullOrWhiteSpace(card.OldImageFile))
        {
            string o = card.OldImageFile.Trim();
            Add(o + ".jpg");
            Add(o + ".png");
        }
        // Normalized name from card
        if (!string.IsNullOrWhiteSpace(card.Name) && !string.IsNullOrWhiteSpace(card.Type))
        {
            string safe = string.Join("_", (card.Name ?? "")
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            Add($"{card.SetFolder}_{card.Type}_{safe}.jpg".Replace(" ", "_"));
            Add($"PR_Event_{safe}.jpg".Replace(" ", "_"));
        }

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    public Card? FindByName(string name)
    {
        return _cards.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Catalog card for SetFolder/ReleaseRaw/Name. Null when the id is unknown.</summary>
    public Card? FindByCardId(string? cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId)) return null;
        return _byCardId.TryGetValue(cardId.Trim(), out var card) ? card : null;
    }

    /// <summary>
    /// cardId wins when the catalog has it. Otherwise name and set, same as before v3.
    /// A miss leaves the entry unloaded; the deck still loads.
    /// </summary>
    public Card? FindForDeckEntry(DeckEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var byId = FindByCardId(entry.CardId);
        if (byId != null) return byId;
        return _cards.FirstOrDefault(c =>
            string.Equals(c.Name, entry.Name, StringComparison.OrdinalIgnoreCase) &&
            (entry.Set == null || string.Equals(c.SetFolder, entry.Set, StringComparison.OrdinalIgnoreCase)));
    }

    public void LinkEntry(DeckEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var card = FindForDeckEntry(entry);
        entry.Card = card;
        if (card != null)
            entry.CardId = card.CardId;
    }

    public void LinkDeck(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        void Link(List<DeckEntry>? list)
        {
            if (list == null) return;
            foreach (var entry in list)
                LinkEntry(entry);
        }
        Link(deck.SeedCards);
        Link(deck.DrawCards);
        Link(deck.QsTentCards);
        Link(deck.BattleBridgeCards);
        Link(deck.QContinuumCards);
        Link(deck.SitePileCards);
        Link(deck.TribbleCards);
        Link(deck.SideCards);
    }

    public IEnumerable<Card> GetByType(string type)
    {
        return _cards.Where(c =>
            string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Card> GetBySet(string setName)
    {
        return _cards.Where(c =>
            string.Equals(c.SetFolder, setName, StringComparison.OrdinalIgnoreCase));
    }
}
