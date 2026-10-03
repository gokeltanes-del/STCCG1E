using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using StarTrekCCG.Models;
using StarTrekCCG.Services;

namespace StarTrekCCG;

/// <summary>
/// Account trade on the main-bridge viewscreen. Bottom band is this account.
/// Top band is only the other player's four offered faces. No second binder.
/// The cell above ACCEPT is left empty on purpose.
/// </summary>
public sealed class AccountTradeScreen
{
    public const int PageSize = 9;

    private readonly Grid _root;
    private readonly Grid _setHost;
    private readonly Grid _binderHost;
    private readonly Border _remoteFrame;
    private readonly Border _localFrame;
    private readonly UniformGrid _remoteGrid;
    private readonly UniformGrid _localGrid;
    private readonly Button _accept;
    private readonly Dictionary<string, BitmapImage> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Card> _byId = new(StringComparer.Ordinal);
    private readonly string[] _mine = { "", "", "", "" };
    private readonly string[] _theirs = { "", "", "", "" };

    private List<SetBand> _sets = new();
    private Dictionary<string, int> _owned = new(StringComparer.Ordinal);
    private string? _openKey;
    private int _setPage;
    private int _spread;
    private bool _mineLocked;
    private bool _theirsLocked;
    private Point _press;
    private bool _armed;
    private bool _duplicatesOnly;
    private int _snapSlot = -1;
    private Grid? _zoomLayer;
    private Image? _zoomImage;
    private bool _zoomOpen;

    public event Action<string[]>? SlotsChanged;
    public event Action<string[]>? AcceptPressed;

    public AccountTradeScreen()
    {
        var style = Application.Current?.TryFindResource("SkinStyle_SciFiButton") as Style;
        _root = new Grid { Margin = new Thickness(8) };
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Star) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.7, GridUnitType.Star) });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2.35, GridUnitType.Star) });

        _setHost = new Grid { Margin = new Thickness(4) };
        _binderHost = new Grid { Margin = new Thickness(4), AllowDrop = true };
        _binderHost.DragOver += BinderDragOver;
        _binderHost.Drop += BinderDrop;

        _remoteGrid = new UniformGrid { Rows = 2, Columns = 2 };
        _localGrid = new UniformGrid { Rows = 2, Columns = 2 };
        _remoteFrame = FrameAround(_remoteGrid);
        _localFrame = FrameAround(_localGrid);

        _accept = new Button
        {
            Content = "ACCEPT",
            Style = style,
            MinWidth = 128,
            MinHeight = 42,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand
        };
        _accept.Click += (_, _) =>
        {
            if (_mineLocked)
                return;
            _accept.IsEnabled = false;
            AcceptPressed?.Invoke(Copy(_mine));
        };

        Put(_setHost, 0, 0);
        Put(_binderHost, 1, 0);
        Put(_accept, 1, 1);
        var offers = new Grid();
        offers.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        offers.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_remoteFrame, 0);
        Grid.SetRow(_localFrame, 1);
        offers.Children.Add(_remoteFrame);
        offers.Children.Add(_localFrame);
        Put(offers, 0, 2);
        Grid.SetRowSpan(offers, 2);
        EnsureZoom();
        _root.PreviewMouseRightButtonDown += ZoomDown;
        _root.PreviewMouseRightButtonUp += ZoomUp;
        Render();
    }

    public void Mount(Panel host)
    {
        if (!ReferenceEquals(_root.Parent, host))
        {
            if (_root.Parent is Panel old)
                old.Children.Remove(_root);
            host.Children.Add(_root);
        }
    }

    public void Unmount()
    {
        if (_root.Parent is Panel panel)
            panel.Children.Remove(_root);
    }

    public void UseLibrary(CardDatabase? catalog, IReadOnlyDictionary<string, int> owned)
    {
        _owned = new Dictionary<string, int>(owned, StringComparer.Ordinal);
        _byId.Clear();
        var groups = new Dictionary<string, List<Card>>(StringComparer.OrdinalIgnoreCase);
        if (catalog != null)
        {
            foreach (var card in catalog.AllCards)
            {
                if (string.IsNullOrWhiteSpace(card.CardId))
                    continue;
                _byId[card.CardId] = card;
                if (!_owned.TryGetValue(card.CardId, out var qty) || qty <= 0)
                    continue;
                var key = string.IsNullOrWhiteSpace(card.SetFolder) ? "" : card.SetFolder.Trim();
                if (key.Length == 0)
                    continue;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<Card>();
                    groups[key] = list;
                }
                list.Add(card);
            }
        }

        var notes = new List<string>();
        _sets = groups
            .Select(pair => new SetBand(
                pair.Key,
                ExpansionCatalog.DisplayName(pair.Key),
                CollectionOrder.Sort(pair.Value, notes)))
            .OrderBy(set => set.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (_openKey == null || _sets.All(set => !string.Equals(set.Key, _openKey, StringComparison.OrdinalIgnoreCase)))
            _openKey = _sets.Count == 0 ? null : _sets[0].Key;
        var pages = Math.Max(1, (int)Math.Ceiling(_sets.Count / 8d));
        if (_setPage >= pages)
            _setPage = pages - 1;
        if (_setPage < 0)
            _setPage = 0;
        Render();
    }

    public void Apply(string[]? mine, string[]? theirs, bool mineLocked, bool theirsLocked)
    {
        if (mineLocked && mine != null)
        {
            for (var i = 0; i < 4; i++)
                _mine[i] = i < mine.Length ? (mine[i] ?? "") : "";
        }
        if (theirs != null)
        {
            for (var i = 0; i < 4; i++)
                _theirs[i] = i < theirs.Length ? (theirs[i] ?? "") : "";
        }
        _mineLocked = mineLocked;
        _theirsLocked = theirsLocked;
        _accept.IsEnabled = !mineLocked;
        Render();
    }

    public void ClearOffers()
    {
        for (var i = 0; i < 4; i++)
        {
            _mine[i] = "";
            _theirs[i] = "";
        }
        _mineLocked = false;
        _theirsLocked = false;
        _accept.IsEnabled = true;
        Render();
    }

    private void Render()
    {
        var open = Skin("SkinBrush_CyanAccent", Brushes.DeepSkyBlue);
        var locked = Skin("SkinBrush_GreenOnline", Brushes.LimeGreen);
        _localFrame.BorderBrush = _mineLocked ? locked : open;
        _remoteFrame.BorderBrush = _theirsLocked ? locked : open;
        _localFrame.BorderThickness = new Thickness(1.5);
        _remoteFrame.BorderThickness = new Thickness(1.5);
        FillSets();
        FillBinder();
        FillSlots(_remoteGrid, _theirs, local: false);
        FillSlots(_localGrid, _mine, local: true);
    }

    private void FillSets()
    {
        _setHost.Children.Clear();
        _setHost.RowDefinitions.Clear();
        var pages = Math.Max(1, (int)Math.Ceiling(_sets.Count / 8d));
        if (_setPage >= pages)
            _setPage = pages - 1;
        var row = 0;
        if (pages > 1)
        {
            _setHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var bar = Pager("SETS", _setPage > 0, _setPage < pages - 1, () => { _setPage--; Render(); }, () => { _setPage++; Render(); });
            Grid.SetRow(bar, 0);
            _setHost.Children.Add(bar);
            row = 1;
        }
        _setHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var grid = new UniformGrid { Rows = 2, Columns = 4 };
        var start = _setPage * 8;
        for (var i = 0; i < 8; i++)
        {
            var index = start + i;
            var band = index >= 0 && index < _sets.Count ? _sets[index] : null;
            var selected = band != null && string.Equals(band.Key, _openKey, StringComparison.OrdinalIgnoreCase);
            Button? button = null;
            if (band != null)
            {
                var stack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stack.Children.Add(new TextBlock
                {
                    Text = band.Title,
                    Foreground = Skin("SkinBrush_GoldAccent", Brushes.Goldenrod),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                stack.Children.Add(new TextBlock
                {
                    Text = band.Cards.Count + " cards",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                });
                button = new Button
                {
                    Content = stack,
                    Style = Application.Current?.TryFindResource("SkinStyle_SciFiButton") as Style,
                    Margin = new Thickness(3),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderBrush = selected ? Skin("SkinBrush_GoldAccent", Brushes.Goldenrod) : null,
                    BorderThickness = selected ? new Thickness(2) : new Thickness(0)
                };
                var key = band.Key;
                button.Click += (_, _) =>
                {
                    _openKey = key;
                    _spread = 0;
                    Render();
                };
            }
            if (button != null)
                grid.Children.Add(button);
            else
                grid.Children.Add(new Border { Margin = new Thickness(3) });
        }
        Grid.SetRow(grid, row);
        _setHost.Children.Add(grid);
    }

    private void FillBinder()
    {
        _binderHost.Children.Clear();
        _binderHost.RowDefinitions.Clear();
        _binderHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _binderHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var open = _sets.FirstOrDefault(set => string.Equals(set.Key, _openKey, StringComparison.OrdinalIgnoreCase));
        IReadOnlyList<Card> cards = open?.Cards ?? Array.Empty<Card>();
        if (_duplicatesOnly)
            cards = cards.Where(card => _owned.TryGetValue(card.CardId, out var owned) && owned > 1).ToList();
        var spreads = SpreadCount(cards.Count);
        if (_spread >= spreads)
            _spread = spreads - 1;
        if (_spread < 0)
            _spread = 0;
        var (left, right) = SpreadSlices(cards, _spread);
        var host = new Grid();
        if (right == null)
        {
            host.Children.Add(PageFace(left));
        }
        else
        {
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var leftFace = PageFace(left);
            var rightFace = PageFace(right);
            var spine = new Border
            {
                Width = 10,
                Margin = new Thickness(4, 0, 4, 0),
                Background = Skin("SkinBrush_GoldAccent", Brushes.Goldenrod),
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumn(leftFace, 0);
            Grid.SetColumn(spine, 1);
            Grid.SetColumn(rightFace, 2);
            host.Children.Add(leftFace);
            host.Children.Add(spine);
            host.Children.Add(rightFace);
        }
        Grid.SetRow(host, 0);
        _binderHost.Children.Add(host);
        var caption = SpreadLabel(cards.Count, _spread, spreads);
        if (_duplicatesOnly)
            caption += "  |  duplicates";
        var bar = Pager(caption, _spread > 0, _spread < spreads - 1,
            () => { _spread--; Render(); }, () => { _spread++; Render(); });
        var duplicates = new Button
        {
            Content = "Only show Duplicates",
            Style = Application.Current?.TryFindResource("SkinStyle_SciFiButton") as Style,
            MinHeight = 30,
            Margin = new Thickness(12, 6, 0, 0),
            FontWeight = _duplicatesOnly ? FontWeights.Bold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center
        };
        duplicates.Click += (_, _) =>
        {
            _duplicatesOnly = !_duplicatesOnly;
            _spread = 0;
            Render();
        };
        bar.Children.Add(duplicates);
        Grid.SetRow(bar, 1);
        _binderHost.Children.Add(bar);
    }

    private FrameworkElement PageFace(Card[] cards)
    {
        var grid = new UniformGrid { Rows = 3, Columns = 3, Width = 684, Height = 984 };
        foreach (var card in cards)
            grid.Children.Add(BinderTile(card));
        while (grid.Children.Count < PageSize)
            grid.Children.Add(EmptyWhite());
        var page = new Border
        {
            Background = Skin("SkinBrush_HeaderBackground", new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x38))),
            BorderBrush = Skin("SkinBrush_OuterFrame", new SolidColorBrush(Color.FromRgb(0x4B, 0x6B, 0x88))),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            Child = grid
        };
        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = page
        };
    }

    private Viewbox BinderTile(Card card)
    {
        _owned.TryGetValue(card.CardId, out var owned);
        var placed = _mine.Count(id => string.Equals(id, card.CardId, StringComparison.Ordinal));
        var left = owned - placed;
        var image = new Image { Width = 200, Height = 268, Stretch = Stretch.Uniform };
        if (!string.IsNullOrWhiteSpace(card.FullImagePath))
        {
            var source = ImageFor(card.FullImagePath);
            if (source != null)
                image.Source = source;
        }
        var stack = new StackPanel();
        stack.Children.Add(image);
        stack.Children.Add(new TextBlock
        {
            Text = card.Name + " " + owned + "x",
            FontSize = 16,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var rarity = CollectionOrder.FrameFor(card.RarityInfo);
        var tile = new Border
        {
            Margin = new Thickness(3),
            Padding = new Thickness(3),
            Background = Skin("SkinBrush_CenterBackground", new SolidColorBrush(Color.FromRgb(0x0B, 0x10, 0x17))),
            BorderBrush = new SolidColorBrush(rarity.Color),
            BorderThickness = new Thickness(rarity.Burst ? 3 : 2),
            CornerRadius = new CornerRadius(3),
            Opacity = left > 0 && !_mineLocked ? 1 : 0.45,
            Child = stack,
            Cursor = left > 0 && !_mineLocked ? Cursors.Hand : Cursors.Arrow,
            Tag = card
        };
        var id = card.CardId;
        if (left > 0 && !_mineLocked)
        {
            tile.PreviewMouseLeftButtonDown += (_, e) =>
            {
                _press = e.GetPosition(tile);
                _armed = true;
            };
            tile.PreviewMouseMove += (_, e) =>
            {
                if (!_armed || _mineLocked || e.LeftButton != MouseButtonState.Pressed)
                    return;
                var point = e.GetPosition(tile);
                if (Math.Abs(point.X - _press.X) + Math.Abs(point.Y - _press.Y) < 8)
                    return;
                _armed = false;
                DragDrop.DoDragDrop(tile, new DataObject(DataFormats.Text, "card\n" + id), DragDropEffects.Move);
            };
        }
        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            Child = tile
        };
    }

    private static Border EmptyWhite() => new()
    {
        Margin = new Thickness(3),
        Background = Skin("SkinBrush_CenterBackground", new SolidColorBrush(Color.FromRgb(0x0B, 0x10, 0x17))),
        BorderBrush = Skin("SkinBrush_InnerFrame", new SolidColorBrush(Color.FromRgb(0x2D, 0x42, 0x55))),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(3)
    };

    private void FillSlots(UniformGrid grid, string[] ids, bool local)
    {
        grid.Children.Clear();
        for (var i = 0; i < 4; i++)
        {
            var index = i;
            var id = i < ids.Length ? (ids[i] ?? "") : "";
            var slot = new Border
            {
                Margin = new Thickness(4),
                Background = Skin("SkinBrush_CenterBackground", new SolidColorBrush(Color.FromRgb(0x0B, 0x10, 0x17))),
                BorderBrush = Skin("SkinBrush_InnerFrame", new SolidColorBrush(Color.FromRgb(0x2D, 0x42, 0x55))),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                AllowDrop = local && !_mineLocked
            };
            var card = id.Length == 0 ? null : FindCard(id);
            if (card != null)
            {
                var image = new Image { Width = 200, Height = 268, Stretch = Stretch.Uniform };
                if (!string.IsNullOrWhiteSpace(card.FullImagePath))
                {
                    var source = ImageFor(card.FullImagePath);
                    if (source != null)
                        image.Source = source;
                }
                slot.Tag = card;
                slot.Child = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(3), Child = image };
                if (local && index == _snapSlot)
                    PlaySlotLand(image);
            }
            else if (id.Length > 0)
            {
                slot.Child = new TextBlock
                {
                    Text = id,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(6),
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            if (local && !_mineLocked)
            {
                slot.DragOver += SlotDragOver;
                slot.Drop += (_, e) => OnSlotDrop(index, e);
                if (id.Length > 0)
                {
                    slot.PreviewMouseLeftButtonDown += (_, e) =>
                    {
                        _press = e.GetPosition(slot);
                        _armed = true;
                    };
                    slot.PreviewMouseMove += (_, e) =>
                    {
                        if (!_armed || _mineLocked || e.LeftButton != MouseButtonState.Pressed)
                            return;
                        var point = e.GetPosition(slot);
                        if (Math.Abs(point.X - _press.X) + Math.Abs(point.Y - _press.Y) < 8)
                            return;
                        _armed = false;
                        DragDrop.DoDragDrop(slot, new DataObject(DataFormats.Text, "slot\n" + index), DragDropEffects.Move);
                    };
                }
            }
            grid.Children.Add(slot);
        }
    }

    private void OnSlotDrop(int index, DragEventArgs e)
    {
        var text = ReadDrag(e.Data);
        if (text == null || _mineLocked)
            return;
        e.Handled = true;
        if (text.StartsWith("slot\n", StringComparison.Ordinal) && int.TryParse(text.AsSpan(5), out var from))
        {
            MoveSlot(from, index);
            return;
        }
        if (text.StartsWith("card\n", StringComparison.Ordinal))
            PlaceCard(index, text[5..]);
    }

    private void PlaceCard(int index, string id)
    {
        if (_mineLocked || index < 0 || index > 3 || string.IsNullOrWhiteSpace(id))
            return;
        if (!_owned.TryGetValue(id, out var owned) || owned < 1)
            return;
        if (FindCard(id) == null)
            return;
        var used = 0;
        for (var i = 0; i < 4; i++)
        {
            if (i == index)
                continue;
            if (string.Equals(_mine[i], id, StringComparison.Ordinal))
                used++;
        }
        if (used >= owned)
            return;
        SetMine(index, id);
    }

    private void MoveSlot(int from, int to)
    {
        if (_mineLocked || from < 0 || from > 3 || to < 0 || to > 3 || from == to)
            return;
        (_mine[from], _mine[to]) = (_mine[to], _mine[from]);
        _snapSlot = to;
        Render();
        _snapSlot = -1;
        SlotsChanged?.Invoke(Copy(_mine));
    }

    private void SetMine(int index, string id)
    {
        if (_mineLocked || index < 0 || index > 3)
            return;
        if (string.Equals(_mine[index], id, StringComparison.Ordinal))
            return;
        _mine[index] = id ?? "";
        _snapSlot = index;
        Render();
        _snapSlot = -1;
        SlotsChanged?.Invoke(Copy(_mine));
    }

    private void BinderDragOver(object sender, DragEventArgs e)
    {
        var text = ReadDrag(e.Data);
        if (_mineLocked || text == null || !text.StartsWith("slot\n", StringComparison.Ordinal))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void BinderDrop(object sender, DragEventArgs e)
    {
        var text = ReadDrag(e.Data);
        if (_mineLocked || text == null || !text.StartsWith("slot\n", StringComparison.Ordinal))
            return;
        if (!int.TryParse(text.AsSpan(5), out var index))
            return;
        e.Handled = true;
        SetMine(index, "");
    }

    private static void SlotDragOver(object sender, DragEventArgs e)
    {
        var text = ReadDrag(e.Data);
        if (text != null && (text.StartsWith("card\n", StringComparison.Ordinal) || text.StartsWith("slot\n", StringComparison.Ordinal)))
            e.Effects = DragDropEffects.Move;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private static string? ReadDrag(IDataObject data)
    {
        if (data.GetDataPresent(DataFormats.Text))
            return data.GetData(DataFormats.Text) as string;
        if (data.GetDataPresent(DataFormats.UnicodeText))
            return data.GetData(DataFormats.UnicodeText) as string;
        return null;
    }

    private Card? FindCard(string id)
        => _byId.TryGetValue(id, out var card) ? card : null;

    private ImageSource? ImageFor(string path)
    {
        if (_images.TryGetValue(path, out var cached))
            return cached;
        try
        {
            if (!File.Exists(path))
                return null;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.DecodePixelWidth = 480;
            bitmap.EndInit();
            bitmap.Freeze();
            _images[path] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static Brush Skin(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Border FrameAround(UIElement child) => new()
    {
        Margin = new Thickness(4),
        Padding = new Thickness(6),
        BorderThickness = new Thickness(1.5),
        CornerRadius = new CornerRadius(8),
        Background = Skin("SkinBrush_HeaderBackground", new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x38))),
        BorderBrush = Skin("SkinBrush_OuterFrame", new SolidColorBrush(Color.FromRgb(0x4B, 0x6B, 0x88))),
        Child = child
    };

    private void EnsureZoom()
    {
        if (_zoomLayer != null)
            return;
        _zoomImage = new Image
        {
            Stretch = Stretch.Uniform,
            MaxWidth = 460,
            MaxHeight = 640,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var dim = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xB0, 0x00, 0x00, 0x00))
        };
        var face = new Grid();
        face.Children.Add(dim);
        face.Children.Add(_zoomImage);
        _zoomLayer = new Grid { Visibility = Visibility.Collapsed };
        _zoomLayer.Children.Add(face);
        Panel.SetZIndex(_zoomLayer, 80);
        _root.Children.Add(_zoomLayer);
        Grid.SetColumnSpan(_zoomLayer, 3);
        Grid.SetRowSpan(_zoomLayer, 2);
    }

    private void ZoomDown(object sender, MouseButtonEventArgs e)
    {
        var card = CardFrom(e.OriginalSource as DependencyObject);
        if (card == null)
            return;
        OpenZoom(card);
        try { _root.CaptureMouse(); } catch { /* ignore */ }
        e.Handled = true;
    }

    private void ZoomUp(object sender, MouseButtonEventArgs e)
    {
        if (!_zoomOpen)
            return;
        try { _root.ReleaseMouseCapture(); } catch { /* ignore */ }
        CloseZoom(animate: true);
        e.Handled = true;
    }

    private static Card? CardFrom(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is FrameworkElement element && element.Tag is Card card)
                return card;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    private void OpenZoom(Card card)
    {
        EnsureZoom();
        if (_zoomImage == null || _zoomLayer == null)
            return;
        _zoomOpen = true;
        ImageSource? face = null;
        if (!string.IsNullOrWhiteSpace(card.FullImagePath))
            face = ImageFor(card.FullImagePath);
        _zoomImage.Source = face;
        var scale = new ScaleTransform(0.28, 0.28);
        _zoomImage.RenderTransform = scale;
        _zoomImage.RenderTransformOrigin = new Point(0.5, 0.5);
        _zoomLayer.Opacity = 0;
        _zoomLayer.Visibility = Visibility.Visible;
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.28, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.28, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
        _zoomLayer.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
    }

    private void CloseZoom(bool animate)
    {
        if (_zoomLayer == null || _zoomImage == null)
            return;
        _zoomOpen = false;
        if (!animate || _zoomLayer.Visibility != Visibility.Visible)
        {
            _zoomLayer.Visibility = Visibility.Collapsed;
            _zoomImage.Source = null;
            _zoomImage.RenderTransform = Transform.Identity;
            return;
        }
        var scale = _zoomImage.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
        _zoomImage.RenderTransform = scale;
        _zoomImage.RenderTransformOrigin = new Point(0.5, 0.5);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(scale.ScaleX, 0.28, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(scale.ScaleY, 0.28, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease });
        var fade = new DoubleAnimation(_zoomLayer.Opacity, 0, TimeSpan.FromMilliseconds(120));
        fade.Completed += (_, _) =>
        {
            if (_zoomOpen || _zoomLayer == null || _zoomImage == null)
                return;
            _zoomLayer.Visibility = Visibility.Collapsed;
            _zoomImage.Source = null;
            _zoomImage.RenderTransform = Transform.Identity;
        };
        _zoomLayer.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>
    /// Land segment of TableWindow.ShowPlayFlyIn: scale 0.68 and angle -6,
    /// QuadraticEase EaseInOut into the slot over 900ms, EaseOut on the rotation.
    /// </summary>
    private static void PlaySlotLand(Image image)
    {
        const double handScale = 68.0 / 100.0;
        const double startAngle = -6.0;
        var scale = new ScaleTransform(handScale, handScale);
        var rotate = new RotateTransform(startAngle);
        var group = new TransformGroup();
        group.Children.Add(rotate);
        group.Children.Add(scale);
        image.RenderTransform = group;
        image.RenderTransformOrigin = new Point(0.5, 0.5);
        var easeInOut = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
        var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var land = TimeSpan.FromMilliseconds(900);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(handScale, 1, land) { EasingFunction = easeInOut });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(handScale, 1, land) { EasingFunction = easeInOut });
        rotate.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(startAngle, 0, TimeSpan.FromMilliseconds(1000)) { EasingFunction = easeOut });
    }

    private void Put(UIElement child, int row, int column)
    {
        Grid.SetRow(child, row);
        Grid.SetColumn(child, column);
        _root.Children.Add(child);
    }

    private static StackPanel Pager(string label, bool canPrev, bool canNext, Action prev, Action next)
    {
        var style = Application.Current?.TryFindResource("SkinStyle_SciFiButton") as Style;
        var back = new Button
        {
            Content = "PREVIOUS",
            Style = style,
            MinWidth = 96,
            MinHeight = 30,
            Margin = new Thickness(0, 6, 8, 0),
            IsEnabled = canPrev
        };
        back.Click += (_, _) => prev();
        var forward = new Button
        {
            Content = "NEXT",
            Style = style,
            MinWidth = 96,
            MinHeight = 30,
            Margin = new Thickness(0, 6, 8, 0),
            IsEnabled = canNext
        };
        forward.Click += (_, _) => next();
        var text = new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 6, 0, 0)
        };
        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        bar.Children.Add(back);
        bar.Children.Add(forward);
        bar.Children.Add(text);
        return bar;
    }

    private static int SpreadCount(int cards)
    {
        if (cards <= PageSize)
            return 1;
        return 1 + (int)Math.Ceiling((cards - PageSize) / (double)(PageSize * 2));
    }

    private static (Card[] Left, Card[]? Right) SpreadSlices(IReadOnlyList<Card> cards, int spread)
    {
        if (spread <= 0)
            return (Take(cards, 0, PageSize), null);
        int start = PageSize + (spread - 1) * PageSize * 2;
        return (Take(cards, start, PageSize), Take(cards, start + PageSize, PageSize));
    }

    private static Card[] Take(IReadOnlyList<Card> cards, int start, int count)
    {
        if (start >= cards.Count)
            return Array.Empty<Card>();
        int n = Math.Min(count, cards.Count - start);
        var page = new Card[n];
        for (var i = 0; i < n; i++)
            page[i] = cards[start + i];
        return page;
    }

    private static string SpreadLabel(int cards, int spread, int spreads)
    {
        if (cards == 0)
            return "No cards";
        if (spread <= 0)
            return "Page 1  |  " + spreads + (spreads == 1 ? " spread" : " spreads");
        int start = PageSize + (spread - 1) * PageSize * 2;
        int last = Math.Min(cards, start + PageSize * 2);
        int firstPage = 2 + (spread - 1) * 2;
        int lastPage = firstPage + (last - start > PageSize ? 1 : 0);
        string pages = lastPage == firstPage ? "Page " + firstPage : "Pages " + firstPage + "-" + lastPage;
        return pages + "  |  " + (spread + 1) + " / " + spreads;
    }

    private static string[] Copy(string[] source)
    {
        var copy = new string[4];
        for (var i = 0; i < 4; i++)
            copy[i] = i < source.Length ? (source[i] ?? "") : "";
        return copy;
    }

    private sealed class SetBand
    {
        public SetBand(string key, string title, IReadOnlyList<Card> cards)
        {
            Key = key;
            Title = title;
            Cards = cards;
        }

        public string Key { get; }
        public string Title { get; }
        public IReadOnlyList<Card> Cards { get; }
    }
}
