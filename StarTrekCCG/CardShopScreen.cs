using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using StarTrekCCG.Services;

namespace StarTrekCCG;

/// <summary>
/// One pack slot on the shop row. Art is optional: drop a file at Assets/{ImageFile}
/// (Assets/Shop/premiere.png or Assets/Shop/alternate_universe.png). Until that file
/// exists the button shows <see cref="Title"/>. PackType is the server pack name.
/// Null means this set has no pack on the server, so Buy stays disabled.
/// </summary>
public sealed class ShopPackSlot
{
    public ShopPackSlot(string title, string imageFile, string? packType)
    {
        Title = title;
        ImageFile = imageFile;
        PackType = packType;
    }

    public string Title { get; }

    /// <summary>Path under the Assets folder. Drop the image here without moving the button.</summary>
    public string ImageFile { get; }

    public string? PackType { get; }
}

/// <summary>
/// Card pack shop drawn in the main-window viewscreen. Not a separate window.
/// </summary>
public sealed class CardShopScreen
{
    public const string PremiereArtFile = "Shop/premiere.png";
    public const string AlternateUniverseArtFile = "Shop/alternate_universe.png";
    public const string PremierePackType = "premiere_booster";

    private double _cardW = 86;
    private double _cardH = 120;
    private double _cardMargin = 6;

    private readonly Grid _root;
    private readonly WrapPanel _slots;
    private readonly Canvas _flyLayer;
    private readonly TextBlock _latinum;
    private readonly TextBlock _note;
    private readonly Button _buy;
    private readonly Dictionary<ShopPackSlot, Border> _chrome = new();
    private ShopPackSlot? _selected;
    private bool _busy;
    private int _flyGen;
    private DispatcherTimer? _flyTimer;

    public event Action<ShopPackSlot>? BuyRequested;

    public CardShopScreen()
    {
        _latinum = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47)),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(8, 8, 8, 0)
        };
        _note = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 4, 8, 4)
        };
        _slots = new WrapPanel { Orientation = Orientation.Horizontal };
        _flyLayer = new Canvas { IsHitTestVisible = false };
        var reveal = new Grid { ClipToBounds = true };
        reveal.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _slots,
            Margin = new Thickness(8)
        });
        reveal.Children.Add(_flyLayer);

        var packs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        AddPack(packs, new ShopPackSlot("PREMIERE", PremiereArtFile, PremierePackType));
        // Cards exist for this set. The server has no pack type and no price, so this button is not wired.
        AddPack(packs, new ShopPackSlot("ALTERNATE UNIVERSE", AlternateUniverseArtFile, null));
        packs.Children.Add(ReservedSlot());
        packs.Children.Add(ReservedSlot());

        _buy = new Button
        {
            Content = "BUY",
            Width = 160,
            Height = 72,
            Margin = new Thickness(12, 6, 8, 6),
            IsEnabled = false,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PackStyle()
        };
        _buy.Click += (_, _) =>
        {
            if (_busy || _selected?.PackType == null)
                return;
            BuyRequested?.Invoke(_selected);
        };

        var row = new Grid { Margin = new Thickness(4, 8, 4, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(packs, 0);
        Grid.SetColumn(_buy, 1);
        row.Children.Add(packs);
        row.Children.Add(_buy);

        _root = new Grid();
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_latinum, 0);
        Grid.SetRow(row, 1);
        Grid.SetRow(_note, 2);
        Grid.SetRow(reveal, 3);
        _root.Children.Add(_latinum);
        _root.Children.Add(row);
        _root.Children.Add(_note);
        _root.Children.Add(reveal);
        SetLatinum(null);
        _note.Text = "Select a pack.";
        _root.MouseLeftButtonDown += (_, _) => AbortReveal();
    }

    public void Mount(Panel host)
    {
        if (_root.Parent is Panel old && !ReferenceEquals(old, host))
            old.Children.Remove(_root);
        if (!host.Children.Contains(_root))
            host.Children.Add(_root);
    }

    public void Unmount()
    {
        _flyGen++;
        _flyTimer?.Stop();
        _flyTimer = null;
        _flyLayer.Children.Clear();
        if (_root.Parent is Panel parent)
            parent.Children.Remove(_root);
    }

    public void SetLatinum(int? latinum)
    {
        _latinum.Text = latinum is int n
            ? "Latinum " + n.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "Latinum";
    }

    public void ShowNote(string text)
    {
        _note.Text = text ?? "";
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _buy.IsEnabled = !busy && _selected?.PackType != null;
    }

    public void ShowCards(IReadOnlyList<string> cardIds, CardDatabase? catalog)
    {
        _flyGen++;
        var gen = _flyGen;
        _flyTimer?.Stop();
        _flyTimer = null;
        _flyLayer.Children.Clear();
        _slots.Children.Clear();

        var rows = new List<RevealCard>();
        for (var i = 0; i < cardIds.Count; i++)
        {
            var id = cardIds[i] ?? "";
            var card = catalog?.FindByCardId(id);
            rows.Add(new RevealCard(
                id,
                string.IsNullOrWhiteSpace(card?.Name) ? LastName(id) : card!.Name!,
                card?.RarityInfo,
                card?.FullImagePath));
        }
        var shuffled = rows.ToArray();
        Random.Shared.Shuffle(shuffled);
        _root.UpdateLayout();
        SizeForNineAcross();

        var visuals = new List<(Border Slot, RevealCard Card)>();
        foreach (var row in shuffled)
        {
            var slot = BuildCard(row, land: true);
            slot.Opacity = 0;
            _slots.Children.Add(slot);
            visuals.Add((slot, row));
        }
        _note.Text = rows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " cards.";
        _root.UpdateLayout();
        FlyAt(gen, visuals, 0);
    }

    private void AddPack(Panel packs, ShopPackSlot slot)
    {
        var face = PackFace(slot);
        var button = new Button
        {
            Content = face,
            Width = 150,
            Height = 78,
            Margin = new Thickness(4),
            Style = PackStyle()
        };
        var chrome = new Border
        {
            Child = button,
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(4, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) => Select(slot, chrome);
        _chrome[slot] = chrome;
        packs.Children.Add(chrome);
    }

    private void Select(ShopPackSlot slot, Border chrome)
    {
        _selected = slot;
        foreach (var pair in _chrome)
        {
            pair.Value.BorderBrush = new SolidColorBrush(
                ReferenceEquals(pair.Value, chrome)
                    ? Color.FromRgb(0xFD, 0xE0, 0x47)
                    : Color.FromRgb(0x33, 0x41, 0x55));
        }
        _buy.Content = "BUY " + slot.Title;
        _buy.IsEnabled = !_busy && slot.PackType != null;
        _note.Text = slot.PackType == null
            ? slot.Title + " has no pack on the server."
            : "Premiere. Price 50. 1 Rare, 3 Uncommon, 11 Common.";
    }

    private static Border ReservedSlot()
    {
        return new Border
        {
            Width = 150,
            Height = 78,
            Margin = new Thickness(8, 4, 8, 4),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF)),
            Child = new TextBlock
            {
                Text = "RESERVED",
                Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            }
        };
    }

    private void FlyAt(int gen, List<(Border Slot, RevealCard Card)> visuals, int index, bool deferred = false)
    {
        if (gen != _flyGen)
            return;
        if (index >= visuals.Count)
            return;

        var (slot, card) = visuals[index];
        _root.UpdateLayout();
        if (_flyLayer.ActualWidth < 2 || _flyLayer.ActualHeight < 2 || slot.ActualWidth < 2)
        {
            if (!deferred)
            {
                _root.Dispatcher.BeginInvoke(
                    () => FlyAt(gen, visuals, index, deferred: true),
                    DispatcherPriority.Loaded);
                return;
            }
            foreach (var pair in visuals)
                pair.Slot.Opacity = 1;
            return;
        }

        var flyer = BuildCard(card, land: false);
        _flyLayer.Children.Add(flyer);

        var start = new Point(_cardW, _flyLayer.ActualHeight / 2.0);
        var mid = new Point(_flyLayer.ActualWidth / 2.0, _flyLayer.ActualHeight / 2.0);
        Point end;
        try
        {
            var center = slot.TransformToVisual(_flyLayer).Transform(new Point(slot.ActualWidth / 2.0, slot.ActualHeight / 2.0));
            end = center;
        }
        catch
        {
            end = mid;
        }

        const double handScale = 68.0 / 100.0;
        const double startAngle = -6.0;
        // Zoomed card, frame, and name stay inside the lower area, with about 5% gap above and below.
        double fitScale = (_flyLayer.ActualHeight * 0.90) / Math.Max(1, flyer.Height);
        double midScale = Math.Min(3.5, fitScale);
        var endScaleX = Math.Max(0.2, slot.ActualWidth / _cardW);
        var endScaleY = Math.Max(0.2, slot.ActualHeight / _cardH);

        var translate = new TranslateTransform();
        var scale = new ScaleTransform(handScale, handScale);
        var rotate = new RotateTransform(startAngle);
        flyer.RenderTransformOrigin = new Point(0.5, 0.5);
        flyer.RenderTransform = new TransformGroup { Children = { rotate, scale, translate } };
        Canvas.SetLeft(flyer, start.X - _cardW / 2.0);
        Canvas.SetTop(flyer, start.Y - _cardH / 2.0);

        var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var easeInOut = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
        var d1 = TimeSpan.FromMilliseconds(700);
        var hold = TimeSpan.FromMilliseconds(490);
        var d2 = TimeSpan.FromMilliseconds(630);
        var tHold = d1;
        var t2 = d1 + hold;
        var tEnd = t2 + d2;
        var dx1 = mid.X - start.X;
        var dy1 = mid.Y - start.Y;
        var dx2 = end.X - mid.X;
        var dy2 = end.Y - mid.Y;

        DoubleAnimationUsingKeyFrames Keys(params (TimeSpan at, double val, IEasingFunction? ease)[] keys)
        {
            var anim = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.HoldEnd };
            foreach (var (at, val, ease) in keys)
            {
                var frame = new EasingDoubleKeyFrame(val, KeyTime.FromTimeSpan(at));
                if (ease != null)
                    frame.EasingFunction = ease;
                anim.KeyFrames.Add(frame);
            }
            return anim;
        }

        translate.BeginAnimation(TranslateTransform.XProperty, Keys(
            (TimeSpan.Zero, 0, null), (tHold, dx1, easeOut), (t2, dx1, null), (tEnd, dx1 + dx2, easeInOut)));
        translate.BeginAnimation(TranslateTransform.YProperty, Keys(
            (TimeSpan.Zero, 0, null), (tHold, dy1, easeOut), (t2, dy1, null), (tEnd, dy1 + dy2, easeInOut)));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, Keys(
            (TimeSpan.Zero, handScale, null), (tHold, midScale, easeOut), (t2, midScale, null), (tEnd, endScaleX, easeInOut)));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Keys(
            (TimeSpan.Zero, handScale, null), (tHold, midScale, easeOut), (t2, midScale, null), (tEnd, endScaleY, easeInOut)));
        rotate.BeginAnimation(RotateTransform.AngleProperty, Keys(
            (TimeSpan.Zero, startAngle, null), (tHold, 0, easeOut), (tEnd, 0, null)));

        _flyTimer = new DispatcherTimer { Interval = tEnd + TimeSpan.FromMilliseconds(105) };
        _flyTimer.Tick += (_, _) =>
        {
            _flyTimer?.Stop();
            _flyTimer = null;
            if (gen != _flyGen)
                return;
            _flyLayer.Children.Remove(flyer);
            slot.Opacity = 1;
            StartBurst(slot);
            FlyAt(gen, visuals, index + 1);
        };
        _flyTimer.Start();
    }

    private Border BuildCard(RevealCard card, bool land)
    {
        var frame = FrameFor(card.Rarity);
        var image = new Image
        {
            Width = _cardW,
            Height = Math.Max(24, _cardH - 22),
            Stretch = Stretch.Uniform
        };
        if (!string.IsNullOrEmpty(card.ImagePath) && File.Exists(card.ImagePath))
            image.Source = LoadBitmap(card.ImagePath);
        var stack = new StackPanel();
        stack.Children.Add(image);
        stack.Children.Add(new TextBlock
        {
            Text = card.Name,
            Width = _cardW,
            FontSize = 10,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center
        });
        var border = new Border
        {
            Width = _cardW + 8,
            Height = _cardH + 8,
            Margin = new Thickness(_cardMargin),
            IsHitTestVisible = false,
            Padding = new Thickness(2),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(frame.Burst ? 3 : 2),
            BorderBrush = new SolidColorBrush(frame.Color),
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x12, 0x1A)),
            Child = stack,
            Tag = frame,
            Effect = new DropShadowEffect
            {
                Color = frame.Color,
                BlurRadius = frame.Burst ? 16 : 8,
                ShadowDepth = 0,
                Opacity = 0.95
            }
        };
        if (!land)
            border.Margin = new Thickness(0);
        return border;
    }

    private static void StartBurst(Border slot)
    {
        if (slot.Tag is not RarityFrame frame || !frame.Burst)
            return;
        if (slot.Effect is not DropShadowEffect effect)
            return;
        var anim = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.HoldEnd };
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(16, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(48, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(26, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(520))));
        effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, anim);
    }

    private static UIElement PackFace(ShopPackSlot slot)
    {
        var path = Path.Combine(GamePaths.AssetsRoot, slot.ImageFile.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            return new Image
            {
                Source = LoadBitmap(path),
                Width = 132,
                Height = 64,
                Stretch = Stretch.Uniform
            };
        }
        return new TextBlock
        {
            Text = slot.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.White
        };
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path, UriKind.Absolute);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static Style? PackStyle()
    {
        return Application.Current?.TryFindResource("SkinStyle_SciFiButton") as Style;
    }

    private static string LastToken(string? rarityInfo)
    {
        var parts = (rarityInfo ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "" : parts[^1].ToUpperInvariant();
    }

    /// <summary>
    /// Common gray, Uncommon green, Rare and Rare* blue, Rare+ purple, Ultra Rare gold.
    /// Red, SR, P, and PV each keep their own color. Burst from Rare upward, including those four.
    /// </summary>
    private static RarityFrame FrameFor(string? rarityInfo)
    {
        return LastToken(rarityInfo) switch
        {
            "C" => new RarityFrame(Color.FromRgb(0x9C, 0xA3, 0xAF), false),
            "U" => new RarityFrame(Color.FromRgb(0x22, 0xC5, 0x5E), false),
            "R" or "R*" => new RarityFrame(Color.FromRgb(0x38, 0xBD, 0xF8), true),
            "R+" => new RarityFrame(Color.FromRgb(0xA8, 0x55, 0xF7), true),
            "UR" => new RarityFrame(Color.FromRgb(0xF5, 0xC5, 0x42), true),
            "RED" => new RarityFrame(Color.FromRgb(0xEF, 0x44, 0x44), true),
            "SR" => new RarityFrame(Color.FromRgb(0xF8, 0xFA, 0xFC), true),
            "P" => new RarityFrame(Color.FromRgb(0xF4, 0x72, 0xB6), true),
            "PV" => new RarityFrame(Color.FromRgb(0x2D, 0xD4, 0xBF), true),
            _ => new RarityFrame(Color.FromRgb(0x9C, 0xA3, 0xAF), false)
        };
    }

    private static string LastName(string cardId)
    {
        var cut = cardId.LastIndexOf('/');
        return cut >= 0 && cut < cardId.Length - 1 ? cardId[(cut + 1)..] : cardId;
    }

    private readonly record struct RarityFrame(Color Color, bool Burst);

    private readonly record struct RevealCard(string Id, string Name, string? Rarity, string? ImagePath);

    private void SizeForNineAcross()
    {
        double viewport = _flyLayer.ActualWidth - 16;
        if (viewport < 200)
            return;
        double footprint = viewport / 9.0;
        double margin = Math.Max(6, footprint * 0.06);
        double borderW = footprint - margin * 2.0;
        double borderH = borderW * (128.0 / 94.0);
        _cardW = Math.Max(48, borderW - 8);
        _cardH = Math.Max(64, borderH - 8);
        _cardMargin = margin;
    }

    private void AbortReveal()
    {
        if (_flyTimer == null && _flyLayer.Children.Count == 0)
            return;
        _flyGen++;
        _flyTimer?.Stop();
        _flyTimer = null;
        _flyLayer.Children.Clear();
        foreach (var child in _slots.Children)
        {
            if (child is not Border slot)
                continue;
            slot.Opacity = 1;
            if (slot.Tag is RarityFrame frame && frame.Burst && slot.Effect is DropShadowEffect effect)
            {
                effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
                effect.BlurRadius = 26;
            }
        }
    }
}
