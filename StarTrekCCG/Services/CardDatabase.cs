using StarTrekCCG.Models;
using System.IO;
using System.Text.Json;

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

        if (!Directory.Exists(_dataRoot))
        {
            throw new DirectoryNotFoundException($"Datenordner nicht gefunden: {_dataRoot}");
        }

        var setDirs = Directory.GetDirectories(_dataRoot);
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

                    // Vollständigen Bildpfad setzen
                    if (!string.IsNullOrWhiteSpace(card.Image))
                    {
                        var possiblePath = Path.Combine(setDir, card.Image);
                        if (File.Exists(possiblePath))
                        {
                            card.FullImagePath = possiblePath;
                        }
                        else
                        {
                            // Fallback: nur Dateiname
                            var fileName = Path.GetFileName(card.Image);
                            var fallback = Path.Combine(setDir, fileName);
                            if (File.Exists(fallback))
                                card.FullImagePath = fallback;
                        }
                    }

                    _cards.Add(card);
                }
            }
            catch (Exception ex)
            {
                // In Phase 0 nur loggen, später besser machen
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden von {jsonPath}: {ex.Message}");
            }
        }

        return _cards.Count;
    }

    public Card? FindByName(string name)
    {
        return _cards.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
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
