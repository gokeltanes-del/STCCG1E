using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using StarTrekCCG.Models;
using StarTrekCCG.Services;

namespace StarTrekCCG;

public partial class MainWindow : Window
{
    private CardDatabase? _db;
    private List<Card> _allCards = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // ============================================================
        // HIER DEN PFAD ZU DEINEN SET-ORDNERN EINTRAGEN
        // Beispiel nach dem Ausführen von split_lackey_sets.py:
        //   C:\STCCG_Data
        // ============================================================
        string dataPath = @"C:\STCCG_Data";

        try
        {
            _db = new CardDatabase(dataPath);
            int count = _db.LoadAll();

            _allCards = _db.AllCards.OrderBy(c => c.Name).ToList();
            CardList.ItemsSource = _allCards;

            StatusText.Text = $"{count} Karten geladen aus {dataPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Fehler beim Laden";
            MessageBox.Show(
                $"Konnte die Kartendaten nicht laden:\n\n{ex.Message}\n\n" +
                "Prüfe den Pfad in MainWindow.xaml.cs (dataPath) und stelle sicher, " +
                "dass split_lackey_sets.py bereits gelaufen ist und der Ordner existiert.",
                "Datenfehler",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_db == null) return;

        string query = SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            CardList.ItemsSource = _allCards;
        }
        else
        {
            var filtered = _allCards
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || (c.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (c.Affiliation?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (c.SetFolder?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            CardList.ItemsSource = filtered;
        }
    }

    private void CardList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CardList.SelectedItem is not Card card)
            return;

        CardNameText.Text = card.Name;
        CardTypeText.Text = $"{card.Type}" + (string.IsNullOrEmpty(card.Affiliation) ? "" : $"  •  {card.Affiliation}");
        CardSetText.Text = card.SetFolder ?? "";
        CardTextBlock.Text = string.IsNullOrWhiteSpace(card.Text) ? "(kein Text)" : card.Text;
        CardLoreBlock.Text = ""; // Lore ist in Lackey oft im Text enthalten oder fehlt

        // Attribute anzeigen (noch als Roh-Strings)
        var attrs = new List<string>();
        if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrs.Add($"INT/RNG {card.IntegrityOrRange}");
        if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrs.Add($"CUN/WPN {card.CunningOrWeapons}");
        if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrs.Add($"STR/SHD {card.StrengthOrShields}");
        if (!string.IsNullOrWhiteSpace(card.Points)) attrs.Add($"PTS {card.Points}");
        if (!string.IsNullOrWhiteSpace(card.Icons)) attrs.Add($"Icons: {card.Icons}");
        AttributesText.Text = attrs.Count > 0 ? string.Join("   •   ", attrs) : "";

        // Bild laden
        string? imagePath = card.FullImagePath;
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                CardImage.Source = bitmap;
            }
            catch
            {
                CardImage.Source = null;
            }
        }
        else
        {
            CardImage.Source = null;
        }
    }
}
