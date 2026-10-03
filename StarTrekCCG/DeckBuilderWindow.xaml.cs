using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StarTrekCCG.Models;
using StarTrekCCG.Network;
using StarTrekCCG.Services;

namespace StarTrekCCG;

public partial class DeckBuilderWindow : Window
{
    private CardDatabase? _db;
    private List<Card> _allCards = new();
    private readonly DeckService _deckService = new();
    private Deck _currentDeck = new();
    private DeckSection _activeSection = DeckSection.Draw;
    private bool _showPhysicalSets = true;
    private bool _showVirtualSets = true;
    private string? _activeSkillCat;
    private readonly HashSet<string> _selectedSkillTags = new(StringComparer.OrdinalIgnoreCase);
    private List<(string Category, List<string> Items)> _skillCategories = new();
    private readonly AccountDeckBridge? _account;
    private readonly Dictionary<string, int>? _pool;
    private bool _hosted;
    private bool _catalogReady;
    private bool _catalogGuard;
    private System.Threading.Tasks.TaskCompletionSource<string?>? _pickWait;

    public DeckBuilderWindow()
        : this(null)
    {
    }

    public DeckBuilderWindow(AccountDeckBridge? account)
    {
        _account = account;
        _pool = account == null
            ? null
            : new Dictionary<string, int>(account.Pool, StringComparer.Ordinal);
        InitializeComponent();
        Loaded += DeckBuilderWindow_Loaded;
        RefreshDeckList();
    }

    private void DeckBuilderWindow_Loaded(object sender, RoutedEventArgs e) => LoadCatalog();

    /// <summary>
    /// Moves the existing builder regions into the main window. No second window.
    /// </summary>
    public void HostIn(Panel filters, Panel cards, Panel deck, Panel detail, Panel actions, Panel zoom)
    {
        _hosted = true;
        MoveRegion(FilterRegion, filters);
        MoveRegion(CardListRegion, cards);
        MoveRegion(DeckRegion, deck);
        MoveRegion(DetailRegion, detail);
        MoveRegion(ActionRegion, actions);
        MoveRegion(CardZoomOverlay, zoom);
        LoadCatalog();
    }

    public void HandlePreviewKey(System.Windows.Input.KeyEventArgs e) => Window_PreviewKeyDown(this, e);

    public void Detach()
    {
        _hosted = false;
        _pickWait?.TrySetResult(null);
        _pickWait = null;
        foreach (var el in new FrameworkElement?[] { FilterRegion, CardListRegion, DeckRegion, DetailRegion, ActionRegion, CardZoomOverlay })
        {
            if (el?.Parent is Panel panel)
                panel.Children.Remove(el);
        }
    }

    private static void MoveRegion(FrameworkElement el, Panel host)
    {
        if (el.Parent is Panel parent)
            parent.Children.Remove(el);
        el.ClearValue(Grid.RowProperty);
        el.ClearValue(Grid.ColumnProperty);
        el.ClearValue(Grid.ColumnSpanProperty);
        el.ClearValue(Grid.RowSpanProperty);
        host.Children.Add(el);
    }

    private void LoadCatalog()
    {
        if (_catalogReady)
            return;
        string dataPath = GamePaths.DataRoot;

        try
        {
            _db = new CardDatabase(dataPath);
            int count = _db.LoadAll();

            _allCards = _db.AllCards.OrderBy(c => c.Name).ToList();
            CardList.ItemsSource = _allCards;
            ApplyCatalogDefault();
            PopulateFilters();
            _catalogReady = true;
            StatusText.Text = _pool == null
                ? $"{count} cards loaded"
                : $"{count} cards loaded. Pool {PoolTotal()}.";
            DeckTabs.SelectedIndex = 1; // Draw als Standard
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not load cards";
            BridgeDialog.Show(
                $"Could not load card data:\n\n{ex.Message}\n\n" +
                "Check dataPath in DeckBuilderWindow.xaml.cs.",
                "Data error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ----------------- Filter -----------------

    private void PopulateFilters()
    {
        var types = _allCards.Select(c => c.Type).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();
        types.Insert(0, "(All types)");
        TypeFilter.ItemsSource = types;
        TypeFilter.SelectedIndex = 0;

        var sets = _allCards.Select(c => c.SetFolder).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()
            .OrderBy(s => ExpansionCatalog.Get(s!).DisplayName).ToList();
        BuildSetFilterCheckboxes(sets!);

        AffiliationFilter.ItemsSource = AffilFamily.AllLabels();
        AffiliationFilter.SelectedIndex = 0;

        var quadrants = new List<string> { "(All quadrants)", "Alpha", "Gamma", "Delta", "Mirror" };
        QuadrantFilter.ItemsSource = quadrants;
        QuadrantFilter.SelectedIndex = 0;

        RefreshContextFilters();
    }

    private void TypeFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        RefreshContextFilters();
        ApplyFilters();
    }

    private string CurrentTypeKey()
    {
        string? t = TypeFilter.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(t) || t.StartsWith("(All"))
            return "";
        return t.ToLowerInvariant();
    }

    private void RefreshContextFilters()
    {
        string key = CurrentTypeKey();
        bool hasAffil = key.Contains("mission") || key.Contains("personnel") || key.Contains("ship")
                        || key.Contains("facility") || key.Contains("site") || string.IsNullOrEmpty(key);
        bool isMission = key.Contains("mission");

        if (AffiliationFilterPanel != null)
            AffiliationFilterPanel.Visibility = hasAffil ? Visibility.Visible : Visibility.Collapsed;
        if (AffiliationFilterLabel != null)
        {
            AffiliationFilterLabel.Text = isMission
                ? "Affiliation (mission icons)"
                : "Affiliation";
        }
        if (QuadrantFilterPanel != null)
            QuadrantFilterPanel.Visibility = (isMission || string.IsNullOrEmpty(key))
                ? Visibility.Visible : Visibility.Collapsed;

        // Skill/icon multi-filter: Personnel or All types
        bool showSkills = key.Contains("personnel") || string.IsNullOrEmpty(key);
        if (SkillFilterPanel != null)
            SkillFilterPanel.Visibility = showSkills ? Visibility.Visible : Visibility.Collapsed;
        if (showSkills)
            RebuildSkillFilters();
        else if (SkillFilterHost != null)
            SkillFilterHost.Children.Clear();

        var sorts = new List<string> { "Name (A–Z)", "Set, Name" };
        if (string.IsNullOrEmpty(key))
            sorts.Add("Type, Name");
        if (isMission || string.IsNullOrEmpty(key))
        {
            sorts.Add("Mission: points");
            sorts.Add("Mission: quadrant, region");
            sorts.Add("Mission: region, quadrant");
        }
        if (key.Contains("personnel") || string.IsNullOrEmpty(key))
        {
            sorts.Add("Personnel: integrity");
            sorts.Add("Personnel: cunning");
            sorts.Add("Personnel: strength");
            sorts.Add("Personnel: skill count");
        }
        if (key.Contains("ship") || string.IsNullOrEmpty(key))
        {
            sorts.Add("Ship: RANGE");
            sorts.Add("Ship: WEAPONS");
            sorts.Add("Ship: SHIELDS");
            sorts.Add("Ship: staffing icons");
        }
        string? keep = SortFilter.SelectedItem as string;
        SortFilter.ItemsSource = sorts;
        int idx = keep != null ? sorts.IndexOf(keep) : 0;
        SortFilter.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void BuildSetFilterCheckboxes(List<string> setKeys)
    {
        SetFilterPanel.Children.Clear();
        foreach (var key in setKeys)
        {
            var info = ExpansionCatalog.Get(key);
            var cb = new CheckBox
            {
                Content = info.FilterLabel,
                Tag = key,
                IsChecked = true,
                Foreground = Brushes.White,
                Margin = new Thickness(2, 1, 2, 1),
                FontSize = 11
            };
            cb.Checked += (_, _) => { UpdateSetFilterSummary(); ApplyFilters(); };
            cb.Unchecked += (_, _) => { UpdateSetFilterSummary(); ApplyFilters(); };
            SetFilterPanel.Children.Add(cb);
        }
        ApplySetListVisibility();
    }

    /// <summary>null = alle Sets aktiv; sonst HashSet der gewählten Keys.</summary>
    private IEnumerable<CheckBox> VisibleSetBoxes() =>
        SetFilterPanel.Children.OfType<CheckBox>().Where(b => b.Visibility == Visibility.Visible);

    private HashSet<string>? GetSelectedSetKeys()
    {
        var visible = VisibleSetBoxes().ToList();
        if (visible.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var selected = visible.Where(b => b.IsChecked == true && b.Tag is string)
            .Select(b => (string)b.Tag!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Both [P] and [V] shown and every visible box checked → no set restriction
        if (_showPhysicalSets && _showVirtualSets && selected.Count == visible.Count)
            return null;
        return selected;
    }

    private void UpdateSetFilterSummary()
    {
        var visible = VisibleSetBoxes().ToList();
        int n = visible.Count(b => b.IsChecked == true);
        string vis = (_showPhysicalSets ? "[P] " : "") + (_showVirtualSets ? "[V]" : "");
        if (visible.Count == 0) SetFilterSummary.Text = "No sets";
        else if (n == visible.Count && _showPhysicalSets && _showVirtualSets) SetFilterSummary.Text = "All sets";
        else SetFilterSummary.Text = $"{n}/{visible.Count} {vis}".Trim();
    }

    private void ApplySetListVisibility()
    {
        foreach (var cb in SetFilterPanel.Children.OfType<CheckBox>())
        {
            if (cb.Tag is not string key) continue;
            bool virt = ExpansionCatalog.Get(key).IsVirtual;
            cb.Visibility = (virt && _showVirtualSets) || (!virt && _showPhysicalSets)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        if (SetFilterPhysicalBtn != null)
            SetFilterPhysicalBtn.Opacity = _showPhysicalSets ? 1.0 : 0.4;
        if (SetFilterVirtualBtn != null)
            SetFilterVirtualBtn.Opacity = _showVirtualSets ? 1.0 : 0.4;
        UpdateSetFilterSummary();
    }

    private void SetFilterAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in VisibleSetBoxes())
            cb.IsChecked = true;
        UpdateSetFilterSummary();
        ApplyFilters();
    }

    private void SetFilterNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in VisibleSetBoxes())
            cb.IsChecked = false;
        UpdateSetFilterSummary();
        ApplyFilters();
    }

    private void SetFilterPhysical_Click(object sender, RoutedEventArgs e)
    {
        _showPhysicalSets = !_showPhysicalSets;
        if (!_showPhysicalSets && !_showVirtualSets)
            _showVirtualSets = true; // keep at least one list visible
        ApplySetListVisibility();
        ApplyFilters();
    }

    private void SetFilterVirtual_Click(object sender, RoutedEventArgs e)
    {
        _showVirtualSets = !_showVirtualSets;
        if (!_showPhysicalSets && !_showVirtualSets)
            _showPhysicalSets = true;
        ApplySetListVisibility();
        ApplyFilters();
    }

    private void FilterChanged(object sender, EventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (_db == null) return;

        string query = SearchBox.Text.Trim();
        string? selectedType = TypeFilter.SelectedItem as string;
        string? selectedAffil = AffiliationFilter.SelectedItem as string;
        string? selectedSort = SortFilter.SelectedItem as string;
        string? selectedQuadrant = QuadrantFilter.SelectedItem as string;

        IEnumerable<Card> result = _allCards;

        if (!string.IsNullOrWhiteSpace(query))
        {
            result = result.Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (c.Text?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrEmpty(selectedType) && selectedType != "(All types)")
            result = result.Where(c => string.Equals(c.Type, selectedType, StringComparison.OrdinalIgnoreCase));

        var selectedSets = GetSelectedSetKeys();
        if (selectedSets != null)
            result = result.Where(c => c.SetFolder != null && selectedSets.Contains(c.SetFolder));

        // Affiliation only when the current type uses it (Personnel/Ship/Mission/Facility).
        // Interrupts, Events, Dilemmas, Equipment, Artifacts have no affiliation — never filter them out.
        string typeKey = CurrentTypeKey();
        bool typeUsesAffiliation = string.IsNullOrEmpty(typeKey)
            || typeKey.Contains("mission") || typeKey.Contains("personnel")
            || typeKey.Contains("ship") || typeKey.Contains("facility")
            || typeKey.Contains("site");
        if (typeUsesAffiliation
            && !string.IsNullOrEmpty(selectedAffil)
            && !selectedAffil.StartsWith("(All", StringComparison.OrdinalIgnoreCase))
        {
            // Only cards that actually match the affiliation family (Affiliation / Icons / lore).
            // Do not keep empty-affiliation types here — they hide Neutral personnel in "All types".
            result = result.Where(c => AffilFamily.MatchesCard(c, selectedAffil));
        }

        if (!string.IsNullOrEmpty(selectedQuadrant) && !selectedQuadrant.StartsWith("(All"))
        {
            result = result.Where(c =>
                string.Equals(NormalizeQuadrant(c.Quadrant), selectedQuadrant, StringComparison.OrdinalIgnoreCase));
        }

        // Personnel skill / icon multi-select (AND across selected tags)
        if (AccountCardsOnly())
            result = result.Where(c => PoolQuantity(c) > 0);

        var skillTags = GetSelectedSkillTags();
        if (skillTags.Count > 0)
        {
            result = result.Where(c =>
            {
                if (!(c.Type ?? "").Contains("Personnel", StringComparison.OrdinalIgnoreCase))
                    return string.IsNullOrEmpty(CurrentTypeKey()); // keep non-personnel only when type=All
                var tags = PersonnelSkillIndex.TagsFor(c);
                return skillTags.All(t => tags.Contains(t));
            });
        }

        result = selectedSort switch
        {
            "Type, Name" or "Typ, Name" => result.OrderBy(c => c.Type ?? "").ThenBy(c => c.Name),
            "Mission: points" => result.OrderByDescending(ParsePoints).ThenBy(c => c.Name),
            "Mission: quadrant, region" or "Mission: Quadrant, Region, Name" => result
                .OrderBy(c => MissionSortKey(c.Type))
                .ThenBy(c => NormalizeQuadrant(c.Quadrant))
                .ThenBy(c => c.Region ?? "")
                .ThenBy(c => c.Name),
            "Mission: region, quadrant" or "Mission: Region, Quadrant, Name" => result
                .OrderBy(c => MissionSortKey(c.Type))
                .ThenBy(c => c.Region ?? "")
                .ThenBy(c => NormalizeQuadrant(c.Quadrant))
                .ThenBy(c => c.Name),
            "Personnel: integrity" => result.OrderByDescending(c => ParseStat(c.IntegrityOrRange)).ThenBy(c => c.Name),
            "Personnel: cunning" => result.OrderByDescending(c => ParseStat(c.CunningOrWeapons)).ThenBy(c => c.Name),
            "Personnel: strength" => result.OrderByDescending(c => ParseStat(c.StrengthOrShields)).ThenBy(c => c.Name),
            "Personnel: skill count" => result.OrderByDescending(CountSkills).ThenBy(c => c.Name),
            "Ship: RANGE" => result.OrderByDescending(c => ParseStat(c.IntegrityOrRange)).ThenBy(c => c.Name),
            "Ship: WEAPONS" => result.OrderByDescending(c => ParseStat(c.CunningOrWeapons)).ThenBy(c => c.Name),
            "Ship: SHIELDS" => result.OrderByDescending(c => ParseStat(c.StrengthOrShields)).ThenBy(c => c.Name),
            "Ship: staffing icons" => result.OrderByDescending(c => (c.Staff ?? "").Length).ThenBy(c => c.Name),
            "Set, Name" => result.OrderBy(c => c.SetFolder ?? "").ThenBy(c => c.Name),
            _ => result.OrderBy(c => c.Name)
        };

        var list = result.Select(c => new BrowserRow(c, PoolQuantity(c))).ToList();
        CardList.ItemsSource = list;
        StatusText.Text = _pool == null
            ? $"{list.Count} cards shown"
            : $"{list.Count} cards shown. Pool {PoolTotal()}.";
    }

    private static int ParseStat(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return -1;
        var m = System.Text.RegularExpressions.Regex.Match(s, @"\d+");
        return m.Success && int.TryParse(m.Value, out int n) ? n : -1;
    }

    private static int ParsePoints(Card c)
    {
        if (string.IsNullOrWhiteSpace(c.Points)) return -1;
        var m = System.Text.RegularExpressions.Regex.Match(c.Points, @"-?\d+");
        return m.Success && int.TryParse(m.Value, out int n) ? n : -1;
    }

    private static readonly string[] KnownSkills =
    {
        "OFFICER", "ENGINEER", "SCIENCE", "MEDICAL", "SECURITY", "CIVILIAN", "VIP",
        "Diplomacy", "Leadership", "Navigation", "Honor", "Treachery", "Physics",
        "Computer Skill", "Anthropology", "Archaeology", "Biology", "Geology",
        "Exobiology", "Astrometrics", "Stellar Cartography", "Transporters",
        "Empathy", "Mindmeld", "Music", "Law", "Greed", "Acquisition", "Youth",
        "Smuggling", "Resistance", "Intelligence", "Programming", "Barbering"
    };

    private static int CountSkills(Card c)
    {
        string blob = $"{c.Text} {c.Class} {c.Characteristics}";
        int n = 0;
        foreach (var sk in KnownSkills)
        {
            if (blob.Contains(sk, StringComparison.OrdinalIgnoreCase)) n++;
        }
        return n;
    }

    /// <summary>Missionen zuerst (0), alles andere danach (1) – für Quadrant/Region-Sortierung.</summary>
    private static int MissionSortKey(string? type)
    {
        string t = (type ?? "").ToLowerInvariant();
        return t.Contains("mission") ? 0 : 1;
    }

    private static string NormalizeQuadrant(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return "Alpha";
        string s = q.Trim().ToLowerInvariant();
        if (s.Contains("gamma") || s.Contains("γ")) return "Gamma";
        if (s.Contains("delta") || s.Contains("δ") || s.Contains("∆")) return "Delta";
        if (s.Contains("mirror")) return "Mirror";
        return "Alpha";
    }

    // ----------------- Kartendetail -----------------

    private void CardList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CardList.SelectedItem is BrowserRow row)
            ShowCardDetail(row.Card);
    }

    private void CardList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Left list: double-click only adds to the active pile (no zoom)
        if (CardList.SelectedItem is BrowserRow row)
            AddCardToSection(row.Card, _activeSection, 1);
    }

    private void CardImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2) return;
        Card? card = (CardList.SelectedItem as BrowserRow)?.Card
                     ?? (DeckList.SelectedItem as DeckEntry)?.Card;
        if (card != null)
            ShowCardZoom(card);
        e.Handled = true;
    }

    private void DeckList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Zoom only via the center card image
    }

    private void ShowCardZoom(Card card)
    {
        CardZoomCaption.Text = card.Name ?? "";
        CardZoomImage.Source = null;
        if (!string.IsNullOrEmpty(card.FullImagePath) && File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath);
                bmp.EndInit();
                CardZoomImage.Source = bmp;
            }
            catch { /* keep caption only */ }
        }
        CardZoomOverlay.Visibility = Visibility.Visible;
    }

    private void CloseCardZoom()
    {
        CardZoomOverlay.Visibility = Visibility.Collapsed;
        CardZoomImage.Source = null;
    }

    private void CloseCardZoom_Click(object sender, MouseButtonEventArgs e)
    {
        CloseCardZoom();
        e.Handled = true;
    }

    private void ShowCardDetail(Card card)
    {
        CardNameText.Text = card.Name;
        if (GametextLabel != null) GametextLabel.Visibility = Visibility.Visible;
        CardTypeText.Text = $"{card.Type}" + (string.IsNullOrEmpty(card.Affiliation) ? "" : $"  •  {card.Affiliation}");
        CardSetText.Text = ExpansionCatalog.FilterLabel(card.SetFolder);
        CardTextBlock.Text = string.IsNullOrWhiteSpace(card.Text) ? "(no text)" : card.Text;

        string type = (card.Type ?? "").ToLowerInvariant();
        var attrParts = new List<string>();

        if (type.Contains("ship"))
        {
            if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrParts.Add($"RANGE {card.IntegrityOrRange}");
            if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrParts.Add($"WEAPONS {card.CunningOrWeapons}");
            if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrParts.Add($"SHIELDS {card.StrengthOrShields}");
        }
        else if (type.Contains("personnel") || type.Contains("animal"))
        {
            if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrParts.Add($"INTEGRITY {card.IntegrityOrRange}");
            if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrParts.Add($"CUNNING {card.CunningOrWeapons}");
            if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrParts.Add($"STRENGTH {card.StrengthOrShields}");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrParts.Add($"INT/RNG {card.IntegrityOrRange}");
            if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrParts.Add($"CUN/WPN {card.CunningOrWeapons}");
            if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrParts.Add($"STR/SHD {card.StrengthOrShields}");
        }

        if (!string.IsNullOrWhiteSpace(card.Points))
            attrParts.Add($"POINTS {card.Points}");

        AttributesText.Text = attrParts.Count > 0 ? string.Join("   •   ", attrParts) : "";
        AttributesHeader.Visibility = attrParts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        ClassText.Text = !string.IsNullOrWhiteSpace(card.Class) ? $"Class: {card.Class}" : "";
        ClassText.Visibility = string.IsNullOrWhiteSpace(card.Class) ? Visibility.Collapsed : Visibility.Visible;

        IconCatalog.FillStaffing(StaffRow, card, 22);
        IconCatalog.Fill(IconRow, card, 22);
        bool staffGlyphs = StaffRow.Children.Count > 0;
        bool otherGlyphs = IconRow.Children.Count > 0;

        StaffText.Text = !string.IsNullOrWhiteSpace(card.Staff) && !staffGlyphs
            ? $"Staffing: {card.Staff}" : "";
        StaffText.Visibility = string.IsNullOrEmpty(StaffText.Text) ? Visibility.Collapsed : Visibility.Visible;

        IconsText.Text = !string.IsNullOrWhiteSpace(card.Icons) && !otherGlyphs
            ? $"Icons: {card.Icons}" : "";
        IconsText.Visibility = string.IsNullOrEmpty(IconsText.Text) ? Visibility.Collapsed : Visibility.Visible;

        CharacteristicsText.Text = !string.IsNullOrWhiteSpace(card.Characteristics) ? card.Characteristics : "";
        CharacteristicsText.Visibility = string.IsNullOrWhiteSpace(card.Characteristics) ? Visibility.Collapsed : Visibility.Visible;

        if (type.Contains("mission"))
        {
            var missionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(card.Span)) missionParts.Add($"Span: {card.Span}");
            if (!string.IsNullOrWhiteSpace(card.Quadrant)) missionParts.Add($"Quadrant: {card.Quadrant}");
            if (!string.IsNullOrWhiteSpace(card.Region)) missionParts.Add($"Region: {card.Region}");
            if (!string.IsNullOrWhiteSpace(card.MissionDilemmaType)) missionParts.Add($"Type: {card.MissionDilemmaType}");
            if (!string.IsNullOrWhiteSpace(card.Points)) missionParts.Add($"Points: {card.Points}");

            MissionInfoText.Text = missionParts.Count > 0 ? string.Join("  •  ", missionParts) : "";
            MissionInfoText.Visibility = missionParts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            MissionInfoText.Text = "";
            MissionInfoText.Visibility = Visibility.Collapsed;
        }

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
            catch { CardImage.Source = null; }
        }
        else
        {
            CardImage.Source = null;
        }
    }

    // ----------------- Deck hinzufügen (explizit) -----------------

    private void AddToSeed_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.Seed, 1);
    private void AddToDraw_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.Draw, 1);
    private void AddToQsTent_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.QsTent, 1);
    private void AddToBattleBridge_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.BattleBridge, 1);
    private void AddToQContinuum_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.QContinuum, 1);
    private void AddToSitePile_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.SitePile, 1);
    private void AddToTribble_Click(object sender, RoutedEventArgs e) => AddSelected(DeckSection.Tribble, 1);

    private void AddAllVisible_Click(object sender, RoutedEventArgs e)
    {
        if (CardList.ItemsSource is not IEnumerable<BrowserRow> visible)
            return;
        var cards = visible.Select(row => row.Card).ToList();
        if (cards.Count == 0)
        {
            StatusText.Text = "No filtered cards to add.";
            return;
        }

        string pile = DeckService.SectionDisplayName(_activeSection);
        var ask = BridgeDialog.Show(
            $"Add {cards.Count} visible card(s) to {pile}?\n(1 copy each)",
            "Add all visible",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (ask != MessageBoxResult.Yes) return;

        int added = 0, skipped = 0, overPool = 0;
        foreach (var card in cards)
        {
            if (!CanAddFromPool(card, 1, out _))
            {
                overPool++;
                continue;
            }
            if (RulesEnforced && !DeckPlacementRules.CanAdd(_currentDeck, card, _activeSection, 1, out _))
            {
                skipped++;
                continue;
            }
            _deckService.AddCard(_currentDeck, card, _activeSection, 1);
            added++;
        }
        _currentDeck.Name = DeckNameBox.Text.Trim();
        SelectTabForSection(_activeSection);
        RefreshDeckList();
        StatusText.Text = overPool == 0
            ? (skipped == 0
                ? $"+{added} cards to {pile}"
                : $"+{added} to {pile}  ({skipped} skipped, wrong pile)")
            : $"+{added} to {pile}  ({skipped} wrong pile, {overPool} over the pool)";
    }

    private void AddSelected(DeckSection section, int quantity)
    {
        if (CardList.SelectedItem is BrowserRow row)
            AddCardToSection(row.Card, section, quantity);
    }

    private bool RulesEnforced => IgnoreRulesCheck == null || IgnoreRulesCheck.IsChecked != true;

    private void AddCardToSection(Card card, DeckSection section, int quantity)
    {
        if (RulesEnforced && !DeckPlacementRules.CanAdd(_currentDeck, card, section, quantity, out string why))
        {
            BridgeDialog.Show(why, "Deck construction", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = why;
            return;
        }
        if (!CanAddFromPool(card, quantity, out var poolWhy))
        {
            BridgeDialog.Show(poolWhy, "Account pool", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = poolWhy;
            return;
        }
        _deckService.AddCard(_currentDeck, card, section, quantity);
        _currentDeck.Name = DeckNameBox.Text.Trim();
        SelectTabForSection(section);
        RefreshDeckList();
        StatusText.Text = $"+{quantity} {card.Name} → {DeckService.SectionDisplayName(section)}";
    }

    // ----------------- Menge / Entfernen / Verschieben -----------------

    private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
    {
        if (DeckList.SelectedItem is DeckEntry entry)
        {
            _deckService.DecreaseQuantity(_currentDeck, entry, _activeSection);
            RefreshDeckList();
        }
    }

    private void RemoveFromDeck_Click(object sender, RoutedEventArgs e) => RemoveSelectedFromDeck();

    private void DeckList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            RemoveSelectedFromDeck();
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (CardZoomOverlay.Visibility == Visibility.Visible)
        {
            CloseCardZoom();
            e.Handled = true;
            return;
        }
        if (Keyboard.FocusedElement is TextBox)
        {
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }
        ClearSelections();
        e.Handled = true;
    }

    private void ClearSelections()
    {
        CardList.SelectedItem = null;
        DeckList.SelectedItem = null;
        CardNameText.Text = "No card selected";
        CardTypeText.Text = "";
        CardSetText.Text = "";
        AttributesHeader.Visibility = Visibility.Collapsed;
        AttributesText.Text = "";
        ClassText.Text = "";
        StaffText.Text = "";
        IconsText.Text = "";
        IconCatalog.FillStaffing(StaffRow, null);
        IconCatalog.Fill(IconRow, null);
        CharacteristicsText.Text = "";
        MissionInfoText.Text = "";
        CardTextBlock.Text = "";
        if (GametextLabel != null) GametextLabel.Visibility = Visibility.Collapsed;
        CardImage.Source = null;
    }

    private void RemoveSelectedFromDeck()
    {
        if (DeckList.SelectedItem is not DeckEntry entry) return;
        _deckService.RemoveCard(_currentDeck, entry, _activeSection);
        RefreshDeckList();
    }

    private void MoveToSeed_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.Seed);
    private void MoveToDraw_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.Draw);
    private void MoveToQsTent_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.QsTent);
    private void MoveToBattleBridge_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.BattleBridge);
    private void MoveToQContinuum_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.QContinuum);
    private void MoveToSitePile_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.SitePile);
    private void MoveToTribble_Click(object sender, RoutedEventArgs e) => MoveSelected(DeckSection.Tribble);

    private void MoveSelected(DeckSection to)
    {
        if (DeckList.SelectedItem is not DeckEntry entry) return;
        if (_activeSection == to) return;

        var card = entry.Card;
        if (RulesEnforced && card != null &&
            !DeckPlacementRules.CanAdd(_currentDeck, card, to, entry.Quantity, out string why))
        {
            BridgeDialog.Show(why, "Deck construction", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = why;
            return;
        }
        _deckService.MoveCard(_currentDeck, entry, _activeSection, to);
        SelectTabForSection(to);
        RefreshDeckList();
    }

    private void ClearDeck_Click(object sender, RoutedEventArgs e)
    {
        var result = BridgeDialog.Show("Clear the current deck?", "Clear deck",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _currentDeck = new Deck { Name = DeckNameBox.Text.Trim() };
            RefreshDeckList();
        }
    }

    private void DeckList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeckList.SelectedItem is DeckEntry entry && entry.Card != null)
            ShowCardDetail(entry.Card);
        Dispatcher.BeginInvoke(new Action(UpdateInlineMoveButtons),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateInlineMoveButtons()
    {
        Card? card = (DeckList.SelectedItem as DeckEntry)?.Card;
        foreach (var btn in FindVisualChildren<Button>(DeckList))
        {
            if (btn.Tag is not string tag) continue;
            if (!Enum.TryParse<DeckSection>(tag, out var sec)) continue;
            bool here = sec == _activeSection;
            btn.Visibility = here ? Visibility.Collapsed : Visibility.Visible;
            string why = "Select a card";
            bool ok = card != null && (!RulesEnforced || DeckPlacementRules.CanAdd(_currentDeck, card, sec, 1, out why));
            btn.IsEnabled = ok;
            btn.Opacity = ok ? 1.0 : 0.35;
            btn.ToolTip = ok ? $"Move to {DeckService.SectionDisplayName(sec)}" : why;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < n; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }

    private void DeckTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _activeSection = TabIndexToSection(DeckTabs.SelectedIndex);
        RefreshDeckList();
    }

    private static DeckSection TabIndexToSection(int index) => index switch
    {
        0 => DeckSection.Seed,
        1 => DeckSection.Draw,
        2 => DeckSection.QsTent,
        3 => DeckSection.BattleBridge,
        4 => DeckSection.QContinuum,
        5 => DeckSection.SitePile,
        6 => DeckSection.Tribble,
        7 => DeckSection.SideLegacy,
        _ => DeckSection.Draw
    };

    private void SelectTabForSection(DeckSection section)
    {
        DeckTabs.SelectedIndex = section switch
        {
            DeckSection.Seed => 0,
            DeckSection.Draw => 1,
            DeckSection.QsTent => 2,
            DeckSection.BattleBridge => 3,
            DeckSection.QContinuum => 4,
            DeckSection.SitePile => 5,
            DeckSection.Tribble => 6,
            DeckSection.SideLegacy => 7,
            _ => 1
        };
    }

    private void RefreshDeckList()
    {
        var source = DeckService.GetList(_currentDeck, _activeSection);
        // Anzeige: Typ, dann Alphabet
        DeckService.SortEntries(source);

        DeckList.ItemsSource = null;
        DeckList.ItemsSource = source.ToList();
        Dispatcher.BeginInvoke(new Action(UpdateInlineMoveButtons),
            System.Windows.Threading.DispatcherPriority.Loaded);

        SeedTab.Header = $"Seed ({_currentDeck.SeedCount})";
        DrawTab.Header = $"Draw ({_currentDeck.DrawCount})";
        QsTentTab.Header = $"Q's Tent ({_currentDeck.QsTentCount})";
        BattleBridgeTab.Header = $"B.Bridge ({_currentDeck.BattleBridgeCount})";
        QContinuumTab.Header = $"Q-Cont. ({_currentDeck.QContinuumCount})";
        SitePileTab.Header = $"Sites ({_currentDeck.SitePileCount})";
        TribbleTab.Header = $"Tribble ({_currentDeck.TribbleCount})";

        if (_currentDeck.SideLegacyCount > 0)
        {
            SideLegacyTab.Visibility = Visibility.Visible;
            SideLegacyTab.Header = $"Side legacy ({_currentDeck.SideLegacyCount})";
        }
        else
        {
            SideLegacyTab.Visibility = Visibility.Collapsed;
        }

        DeckCountText.Text = AccountCountHead() +
            $"Seed {_currentDeck.SeedCount}  •  Draw {_currentDeck.DrawCount}  •  " +
            $"Q's Tent {_currentDeck.QsTentCount}  •  BB {_currentDeck.BattleBridgeCount}  •  " +
            $"Q-C {_currentDeck.QContinuumCount}  •  Sites {_currentDeck.SitePileCount}  •  " +
            $"Tribble {_currentDeck.TribbleCount}" +
            (_currentDeck.SideLegacyCount > 0 ? $"  •  legacy {_currentDeck.SideLegacyCount}" : "");

        DeckNameBox.Text = _currentDeck.Name;
    }

    // ----------------- Speichern / Laden -----------------

    private async void SaveDeck_Click(object sender, RoutedEventArgs e)
    {
        _currentDeck.Name = string.IsNullOrWhiteSpace(DeckNameBox.Text) ? "Untitled deck" : DeckNameBox.Text.Trim();
        if (_account != null)
        {
            await SaveAccountDeckAsync();
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save deck",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = GamePaths.DecksRoot,
            FileName = $"{_currentDeck.Name}.stdeck",
            DefaultExt = ".stdeck",
            AddExtension = true
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _deckService.Save(_currentDeck, dialog.FileName);
                BridgeDialog.Show($"Deck \"{_currentDeck.Name}\" saved (format v3).", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BridgeDialog.Show($"Could not save:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LoadDeck_Click(object sender, RoutedEventArgs e)
    {
        if (_account != null)
        {
            await LoadAccountDeckAsync();
            return;
        }
        var dialog = new OpenFileDialog
        {
            Title = "Load deck",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = GamePaths.DecksRoot
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var loaded = _deckService.Load(dialog.FileName);

                if (_db != null)
                {
                    _db.LinkDeck(loaded);
                }

                _currentDeck = loaded;
                RefreshDeckList();
                StatusText.Text = $"Loaded \"{loaded.Name}\" ({loaded.Format})";
            }
            catch (Exception ex)
            {
                BridgeDialog.Show($"Could not load:\n\n{ex.Message}", "Invalid file",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void RebuildSkillFilters()
    {
        if (SkillFilterHost == null || _allCards == null) return;
        var pool = _allCards.Where(c => (c.Type ?? "").Contains("Personnel", StringComparison.OrdinalIgnoreCase));
        _skillCategories = PersonnelSkillIndex.Collect(pool).Where(g => g.Items.Count > 0).ToList();

        if (SkillCatButtons != null)
        {
            SkillCatButtons.Children.Clear();
            foreach (var (cat, items) in _skillCategories)
            {
                var btn = new Button
                {
                    Content = $"{ShortCat(cat)} ({items.Count})",
                    Tag = cat,
                    Margin = new Thickness(0, 0, 4, 4),
                    Padding = new Thickness(8, 3, 8, 3),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48))
                };
                btn.Click += SkillCatButton_Click;
                SkillCatButtons.Children.Add(btn);
            }
        }

        if (_activeSkillCat == null || _skillCategories.All(g => g.Category != _activeSkillCat))
            _activeSkillCat = _skillCategories.FirstOrDefault().Category;
        HighlightSkillCatButtons();
        FillActiveSkillHost();
        UpdateSkillFilterSummary();
    }

    private static string ShortCat(string cat) => cat switch
    {
        "Classification" => "Class",
        "Staffing ability" => "Staff",
        "Special icons" => "Icons",
        "Regular skills" => "Skills",
        "Download" => "DL",
        "Expansion / era icons" => "Era",
        _ => cat
    };

    private void SkillCatButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string cat }) return;
        _activeSkillCat = string.Equals(_activeSkillCat, cat, StringComparison.Ordinal) ? null : cat;
        HighlightSkillCatButtons();
        FillActiveSkillHost();
    }

    private void HighlightSkillCatButtons()
    {
        if (SkillCatButtons == null) return;
        foreach (var btn in SkillCatButtons.Children.OfType<Button>())
        {
            bool on = btn.Tag is string t && string.Equals(t, _activeSkillCat, StringComparison.Ordinal);
            btn.Background = new SolidColorBrush(on
                ? Color.FromRgb(0x0E, 0x63, 0x9C)
                : Color.FromRgb(0x3A, 0x3A, 0x48));
            btn.Opacity = on ? 1.0 : 0.85;
        }
    }

    private void FillActiveSkillHost()
    {
        if (SkillFilterHost == null) return;
        SkillFilterHost.Children.Clear();
        if (_activeSkillCat == null) return;
        var group = _skillCategories.FirstOrDefault(g => g.Category == _activeSkillCat);
        if (group.Items == null) return;
        foreach (var tag in group.Items)
        {
            var cb = new CheckBox
            {
                Content = tag,
                Tag = tag,
                IsChecked = _selectedSkillTags.Contains(tag),
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(2, 1, 6, 1)
            };
            cb.Checked += SkillTag_Changed;
            cb.Unchecked += SkillTag_Changed;
            SkillFilterHost.Children.Add(cb);
        }
    }

    private void SkillTag_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: string tag } box) return;
        if (box.IsChecked == true) _selectedSkillTags.Add(tag);
        else _selectedSkillTags.Remove(tag);
        UpdateSkillFilterSummary();
        ApplyFilters();
    }

    private HashSet<string> GetSelectedSkillTags() => _selectedSkillTags;

    private void UpdateSkillFilterSummary()
    {
        if (SkillFilterSummary == null) return;
        int n = _selectedSkillTags.Count;
        SkillFilterSummary.Text = n == 0 ? "" : $"{n} selected";
    }

    private void ClearSkillFilters_Click(object sender, RoutedEventArgs e)
    {
        _selectedSkillTags.Clear();
        FillActiveSkillHost();
        UpdateSkillFilterSummary();
        ApplyFilters();
    }
    private async Task SaveAccountDeckAsync()
    {
        if (_account == null)
            return;
        try
        {
            var cards = DeckCardList.FromDeck(_currentDeck);
            var error = await _account.SaveAsync(_currentDeck.Name, cards).ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(error))
            {
                StatusText.Text = error;
                BridgeDialog.Show(error, "Deck not saved", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            StatusText.Text = "Saved " + _currentDeck.Name + " on the account. Pool " + PoolTotal() + ".";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            BridgeDialog.Show(ex.Message, "Deck not saved", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadAccountDeckAsync()
    {
        if (_account == null)
            return;
        IReadOnlyList<(string Name, int Count)> decks;
        try
        {
            decks = await _account.ListAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            return;
        }
        if (decks.Count == 0)
        {
            StatusText.Text = "No account decks.";
            return;
        }
        var picked = _hosted
            ? await PickAccountDeckHostedAsync(decks).ConfigureAwait(true)
            : PickAccountDeck(decks);
        if (picked == null)
            return;
        try
        {
            var loaded = await _account.LoadAsync(picked).ConfigureAwait(true);
            if (loaded == null)
            {
                StatusText.Text = "Deck was not read.";
                return;
            }
            if (_db != null)
                _db.LinkDeck(loaded);
            _currentDeck = loaded;
            RefreshDeckList();
            StatusText.Text = "Loaded " + loaded.Name + " from the account. Pool " + PoolTotal() + ".";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private string? PickAccountDeck(IReadOnlyList<(string Name, int Count)> decks)
    {
        var win = new Window
        {
            Title = "Account decks",
            Width = 420,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22)),
            ResizeMode = ResizeMode.NoResize
        };
        var root = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var ok = new Button
        {
            Content = "Load",
            Width = 90,
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Width = 90,
            Height = 32,
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Bottom);
        var title = new TextBlock
        {
            Text = "Choose an account deck.",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(title, Dock.Top);
        var list = new ListBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48))
        };
        list.ItemContainerStyle = new Style(typeof(ListBoxItem))
        {
            Setters =
            {
                new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22))),
                new Setter(Control.ForegroundProperty, Brushes.White)
            }
        };
        foreach (var deck in decks)
            list.Items.Add(new ListBoxItem { Content = deck.Name + " (" + deck.Count + ")", Tag = deck.Name, Foreground = Brushes.White });
        if (list.Items.Count > 0)
            list.SelectedIndex = 0;
        root.Children.Add(buttons);
        root.Children.Add(title);
        root.Children.Add(list);
        win.Content = root;
        string? picked = null;
        ok.Click += (_, _) =>
        {
            if (list.SelectedItem is ListBoxItem item && item.Tag is string name)
                picked = name;
            win.DialogResult = picked != null;
        };
        cancel.Click += (_, _) => { win.DialogResult = false; };
        win.ShowDialog();
        return picked;
    }

    private int PoolQuantity(Card card)
    {
        if (_pool == null)
            return -1;
        return _pool.TryGetValue(card.CardId, out var n) ? n : 0;
    }

    private int PoolTotal()
        => _pool == null ? 0 : _pool.Values.Sum();

    private string AccountCountHead()
    {
        if (_pool == null)
            return "";
        return "Pool " + PoolTotal() + "  |  Deck " + _currentDeck.TotalCards + "  |  ";
    }

    private bool CanAddFromPool(Card card, int quantity, out string why)
    {
        why = "";
        if (_pool == null)
            return true;
        var owned = PoolQuantity(card);
        var have = CopiesInDeck(card.CardId);
        if (have + quantity <= owned)
            return true;
        why = owned <= 0
            ? card.Name + " is not in the account pool"
            : "too many copies of " + card.Name;
        return false;
    }

    private int CopiesInDeck(string cardId)
    {
        var n = 0;
        void Take(List<DeckEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;
                var id = !string.IsNullOrWhiteSpace(entry.CardId)
                    ? entry.CardId
                    : entry.Card != null
                        ? entry.Card.CardId
                        : Card.FormatCardId(entry.Set, null, entry.Name);
                if (string.Equals(id, cardId, StringComparison.Ordinal))
                    n += entry.Quantity < 1 ? 1 : entry.Quantity;
            }
        }
        Take(_currentDeck.SeedCards);
        Take(_currentDeck.DrawCards);
        Take(_currentDeck.QsTentCards);
        Take(_currentDeck.BattleBridgeCards);
        Take(_currentDeck.QContinuumCards);
        Take(_currentDeck.SitePileCards);
        Take(_currentDeck.TribbleCards);
        Take(_currentDeck.SideCards);
        return n;
    }


    private void ApplyCatalogDefault()
    {
        if (AccountOnlyCheck == null || UnlockAllCheck == null)
            return;
        _catalogGuard = true;
        if (_pool == null)
        {
            AccountOnlyCheck.IsChecked = false;
            AccountOnlyCheck.IsEnabled = false;
            UnlockAllCheck.IsChecked = true;
        }
        else
        {
            AccountOnlyCheck.IsEnabled = true;
            AccountOnlyCheck.IsChecked = true;
            UnlockAllCheck.IsChecked = false;
        }
        _catalogGuard = false;
    }

    private bool AccountCardsOnly()
    {
        if (_pool == null)
            return false;
        if (UnlockAllCheck?.IsChecked == true)
            return false;
        return AccountOnlyCheck?.IsChecked == true;
    }

    private void CatalogFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_catalogGuard || AccountOnlyCheck == null || UnlockAllCheck == null)
            return;
        _catalogGuard = true;
        if (ReferenceEquals(sender, AccountOnlyCheck) && AccountOnlyCheck.IsChecked == true)
            UnlockAllCheck.IsChecked = false;
        else if (ReferenceEquals(sender, UnlockAllCheck) && UnlockAllCheck.IsChecked == true)
            AccountOnlyCheck.IsChecked = false;
        if (AccountOnlyCheck.IsChecked != true && UnlockAllCheck.IsChecked != true)
        {
            if (_pool != null)
                AccountOnlyCheck.IsChecked = true;
            else
                UnlockAllCheck.IsChecked = true;
        }
        _catalogGuard = false;
        if (_catalogReady)
            ApplyFilters();
    }

    private async System.Threading.Tasks.Task<string?> PickAccountDeckHostedAsync(System.Collections.Generic.IReadOnlyList<(string Name, int Count)> decks)
    {
        if (AccountPickList == null || AccountPickPanel == null)
            return null;
        AccountPickList.Items.Clear();
        foreach (var deck in decks)
            AccountPickList.Items.Add(new ListBoxItem { Content = deck.Name + " (" + deck.Count + ")", Tag = deck.Name, Foreground = Brushes.White });
        if (AccountPickList.Items.Count > 0)
            AccountPickList.SelectedIndex = 0;
        AccountPickPanel.Visibility = Visibility.Visible;
        _pickWait = new System.Threading.Tasks.TaskCompletionSource<string?>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        var picked = await _pickWait.Task.ConfigureAwait(true);
        _pickWait = null;
        AccountPickPanel.Visibility = Visibility.Collapsed;
        return picked;
    }

    private void AccountPickOk_Click(object sender, RoutedEventArgs e)
    {
        string? name = null;
        if (AccountPickList?.SelectedItem is ListBoxItem item && item.Tag is string tag)
            name = tag;
        AccountPickPanel.Visibility = Visibility.Collapsed;
        _pickWait?.TrySetResult(name);
    }

    private void AccountPickCancel_Click(object sender, RoutedEventArgs e)
    {
        AccountPickPanel.Visibility = Visibility.Collapsed;
        _pickWait?.TrySetResult(null);
    }

    private sealed class BrowserRow
    {
        private static readonly SolidColorBrush GreyName = CreateGrey();

        public BrowserRow(Card card, int poolQuantity)
        {
            Card = card;
            PoolQuantity = poolQuantity;
        }

        public Card Card { get; set; }
        public int PoolQuantity { get; set; }
        public string Name { get => Card.Name; set { } }
        public string? Type { get => Card.Type; set { } }
        public string? Affiliation { get => Card.Affiliation; set { } }
        public string? Quadrant { get => Card.Quadrant; set { } }
        public string? Region { get => Card.Region; set { } }
        public bool Unowned { get => PoolQuantity == 0; set { } }
        public string PoolLabel { get => PoolQuantity < 0 ? "" : "pool " + PoolQuantity; set { } }
        public Visibility PoolVisibility { get => PoolQuantity < 0 ? Visibility.Collapsed : Visibility.Visible; set { } }
        public Brush NameBrush { get => PoolQuantity == 0 ? GreyName : Brushes.White; set { } }

        private static SolidColorBrush CreateGrey()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            brush.Freeze();
            return brush;
        }
    }




/// <summary>
/// One affiliation family: mission icon [FED], name Federation, duals, etc.
/// </summary>
}

public sealed class AccountDeckBridge
{
    public AccountDeckBridge(
        IReadOnlyDictionary<string, int> pool,
        Func<string, string, Task<string?>> saveAsync,
        Func<Task<IReadOnlyList<(string Name, int Count)>>> listAsync,
        Func<string, Task<Deck?>> loadAsync)
    {
        Pool = pool;
        SaveAsync = saveAsync;
        ListAsync = listAsync;
        LoadAsync = loadAsync;
    }

    public IReadOnlyDictionary<string, int> Pool { get; }
    public Func<string, string, Task<string?>> SaveAsync { get; }
    public Func<Task<IReadOnlyList<(string Name, int Count)>>> ListAsync { get; }
    public Func<string, Task<Deck?>> LoadAsync { get; }
}

internal static class AffilFamily
{
    private static readonly (string Label, string[] Tokens)[] Families =
    {
        ("Federation  [FED]", new[] { "federation", "fed", "[fed]" }),
        ("Klingon  [KLI]", new[] { "klingon", "kli", "[kli]" }),
        ("Romulan  [ROM]", new[] { "romulan", "rom", "[rom]" }),
        ("Bajoran  [BAJ]", new[] { "bajoran", "baj", "[baj]" }),
        ("Cardassian  [CAR]", new[] { "cardassian", "car", "card", "[car]", "[card]" }),
        ("Dominion  [DOM]", new[] { "dominion", "dom", "[dom]" }),
        ("Ferengi  [FER]", new[] { "ferengi", "fer", "[fer]" }),
        ("Borg  [BOR]", new[] { "borg", "bor", "[bor]" }),
        ("Non-Aligned  [NON]", new[] { "non-aligned", "nonaligned", "[non]", "non aligned" }),
        ("Starfleet  [STA]", new[] { "starfleet", "sta", "[sta]" }),
        ("Hirogen  [HIR]", new[] { "hirogen", "hir", "[hir]" }),
        ("Kazon  [KAZ]", new[] { "kazon", "kaz", "[kaz]" }),
        ("Vidiian  [VID]", new[] { "vidiian", "vid", "[vid]" }),
        ("Vulcan  [VUL]", new[] { "vulcan", "vul", "[vul]" }),
        ("Xindi  [XIN]", new[] { "xindi", "xin", "[xin]" }),
        ("Neutral  [NEU]", new[] { "neutral", "[neu]", "neu" }),
    };

    public static List<string> AllLabels()
    {
        var list = new List<string> { "(All affiliations)" };
        list.AddRange(Families.Select(f => f.Label));
        return list;
    }

    public static bool MatchesCard(Card c, string selectedLabel)
    {
        // Combine affiliation text, icon codes, and characteristics (e.g. Neutral pets)
        string blob = $"{c.Affiliation} {c.Icons} {c.Characteristics}";
        return Matches(blob, selectedLabel);
    }

    public static bool Matches(string? affiliation, string selectedLabel)
    {
        if (string.IsNullOrWhiteSpace(affiliation)) return false;
        if (string.IsNullOrWhiteSpace(selectedLabel)) return true;

        string normSelected = NormKey(selectedLabel);
        string spaced = " " + affiliation.ToLowerInvariant()
            .Replace("[", " ").Replace("]", " ")
            .Replace("/", " ").Replace("-", " ")
            .Replace(",", " ").Replace(";", " ") + " ";
        while (spaced.Contains("  ")) spaced = spaced.Replace("  ", " ");
        string compact = spaced.Replace(" ", "");

        foreach (var (label, tokens) in Families)
        {
            if (NormKey(label) != normSelected)
                continue;

            // Exact / contains family name (Neutral, Federation, …)
            string bare = label.Split('[')[0].Trim().ToLowerInvariant();
            if (bare.Length > 2)
            {
                if (spaced.Contains(" " + bare + " ") || compact.Contains(bare.Replace(" ", "").Replace("-", "")))
                    return true;
            }

            foreach (var tok in tokens)
            {
                string t = tok.ToLowerInvariant().Replace("[", "").Replace("]", "").Replace("-", " ").Trim();
                if (t.Length == 0) continue;
                string tCompact = t.Replace(" ", "");
                // Short codes: whole word only ("neu" must not match inside other words incorrectly;
                // "neutral" is handled via bare name above)
                if (tCompact.Length <= 3)
                {
                    if (spaced.Contains(" " + tCompact + " "))
                        return true;
                }
                else if (spaced.Contains(" " + t + " ") || compact.Contains(tCompact))
                    return true;
            }
            return false;
        }

        // Fallback: selected label text without icon
        string fallback = selectedLabel.Split('[')[0].Trim();
        return fallback.Length > 0 && affiliation.Contains(fallback, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormKey(string s)
    {
        string x = s.ToLowerInvariant();
        while (x.Contains("  ")) x = x.Replace("  ", " ");
        return x.Trim();
    }
}
