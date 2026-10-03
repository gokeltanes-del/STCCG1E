using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using StarTrekCCG.Models;
using StarTrekCCG.Services;

namespace StarTrekCCG;

/// <summary>
/// Collection binders on the main-window viewscreen. One binder per loaded set.
/// Pages only: nine cards a page, open as a two-page binder, one page per turn. No economy.
/// </summary>
public sealed class CollectionScreen
{
    public const int PageSize = 9;

    private static readonly string[] ReleaseOrder =
    {
        "PR", "Alternate_Universe", "Q_Continuum", "First_Contact", "Deep_Space_Nine",
        "The_Dominion", "Blaze_of_Glory", "Rules_of_Acquisition", "The_Trouble_with_Tribbles",
        "Trouble_with_Tribbles", "Mirror_Mirror", "Voyager", "The_Borg", "Holodeck_Adventures",
        "The_Motion_Pictures", "All_Good_Things", "Enhanced_Premiere", "Enhanced_First_Contact",
        "Starter_Deck_II", "sdII", "Official_Tournament_Sealed_Deck", "Introductory_Two_Player_Game",
        "First_Anthology", "1anth", "Second_Anthology", "warppack", "WPEmissary",
        "Enterprise_Collection", "Captain_Picard_Collection", "Captain_Kirk_Collection", "Promo",
        "IMD", "What_You_Leave_Behind", "Broken_Bow", "Enterprise",
        "Life_From_Lifelessness", "The_Next_Generation", "The_Maquis", "Metamorphosis",
        "Crossover", "crossovers", "Through_the_Looking_Glass", "Looking_Glass",
        "The_Great_Gathering", "Raise_the_Stakes", "RTS", "Cold_Front", "Coming_of_Age",
        "Cage", "ENGAGE", "Emissary", "Emissary+", "Live_Long_and_Prosper", "Terran_Empire",
        "The_Best_of_Both_Worlds", "Pre_Warp", "Genesis", "gift", "tnz", "Nemesis", "NEM",
        "otsdr", "HF", "HF2", "HF3", "HF4", "HF5", "HF6", "20th", "50", "Bah!", "SotL",
        "SoG", "SSttR", "TMPR", "TUC", "TSOP", "TWT", "FTB", "AP", "AUT", "BP", "DM", "DP",
        "IC", "PL", "R2", "Ref", "referee", "SFL", "SFLS", "SW", "SaS", "VP", "X",
        "armade", "awayteam", "coc", "dow", "ecr", "equilibriu", "plw", "qwho", "tstl",
        "ttne", "wp2017", "Ls"
    };

    private readonly Grid _root;
    private readonly TextBlock _title;
    private readonly TextBlock _note;
    private readonly Grid _stage;
    private readonly Dictionary<string, BitmapImage> _images = new(StringComparer.OrdinalIgnoreCase);

    private List<Binder> _binders = new();
    private Binder? _open;
    private int _spread;
    private bool _loggedIn;
    private Dictionary<string, int> _owned = new(StringComparer.Ordinal);
    private bool _duplicatesOnly;

    public event Action<string>? BinderChosen;

    public bool IsMounted => _root.Parent != null;

    public IReadOnlyList<string> SortNotes { get; private set; } = Array.Empty<string>();

    public CollectionScreen()
    {
        var skin = Application.Current?.Resources;
        Brush Take(string key, Brush fallback) => skin?[key] as Brush ?? fallback;
        var buttonStyle = skin?["SkinStyle_SciFiButton"] as Style;

        _title = new TextBlock
        {
            Text = "COLLECTION",
            Foreground = Take("SkinBrush_GoldAccent", Brushes.Goldenrod),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(8, 4, 8, 0)
        };
        _note = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 4, 8, 8)
        };
        _stage = new Grid();
        _root = new Grid { Margin = new Thickness(12) };
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_title, 0);
        Grid.SetRow(_note, 1);
        Grid.SetRow(_stage, 2);
        _root.Children.Add(_title);
        _root.Children.Add(_note);
        _root.Children.Add(_stage);
        _buttonStyle = buttonStyle;
    }

    private readonly Style? _buttonStyle;

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

    public void UseCatalog(CardDatabase? catalog)
    {
        var notes = new List<string>();
        var groups = new Dictionary<string, List<Card>>(StringComparer.OrdinalIgnoreCase);
        if (catalog != null)
        {
            foreach (var card in catalog.AllCards)
            {
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

        var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < ReleaseOrder.Length; i++)
        {
            if (!order.ContainsKey(ReleaseOrder[i]))
                order[ReleaseOrder[i]] = i;
        }

        _binders = groups
            .Select(pair => new Binder(pair.Key, ExpansionCatalog.DisplayName(pair.Key), CollectionOrder.Sort(pair.Value, notes)))
            .OrderBy(b => order.TryGetValue(b.Key, out var index) ? index : int.MaxValue)
            .ThenBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unknown = _binders.Where(b => !order.ContainsKey(b.Key)).Select(b => b.Key).ToList();
        if (unknown.Count > 0)
            notes.Add("Sets not in the release list were placed after the known sets: " + string.Join(", ", unknown) + ".");
        SortNotes = notes;
        _open = null;
        _spread = 0;
    }

    public void SetOwnership(bool loggedIn, IReadOnlyDictionary<string, int>? owned, string? failure)
    {
        _loggedIn = loggedIn;
        _owned = owned == null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : new Dictionary<string, int>(owned, StringComparer.Ordinal);
        if (!loggedIn)
            _note.Text = "Nobody is logged in. The catalog is shown darkened. No pool was invented.";
        else if (!string.IsNullOrWhiteSpace(failure))
            _note.Text = failure + " Cards stay darkened. No pool was invented.";
        else
            _note.Text = "Owned cards are shown full, with the rarity frame and the duplicate count. Cards you do not own are darkened.";
        if (_open == null)
            ShowShelf();
        else
            ShowPages();
    }

    public void ShowShelf()
    {
        _open = null;
        _spread = 0;
        _title.Text = "COLLECTION";
        _stage.Children.Clear();
        if (_binders.Count == 0)
        {
            _stage.Children.Add(Label("No set in the catalog has cards yet."));
            return;
        }

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var binder in _binders)
        {
            var captured = binder;
            var button = ActionButton(captured.Title, captured.Cards.Count + " cards");
            button.Width = 220;
            button.Height = 72;
            button.Margin = new Thickness(0, 0, 12, 12);
            button.Click += (_, _) =>
            {
                _open = captured;
                _spread = 0;
                _root.Dispatcher.BeginInvoke(new Action(() => BinderChosen?.Invoke(captured.Key)));
            };
            wrap.Children.Add(button);
        }
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = wrap
        };
        _stage.Children.Add(scroll);
    }

    public void ShowPages()
    {
        if (_open == null)
        {
            ShowShelf();
            return;
        }

        IReadOnlyList<Card> cards = _open.Cards;
        if (_duplicatesOnly)
            cards = cards.Where(card => _owned.TryGetValue(card.CardId, out var owned) && owned > 1).ToList();
        var spreads = SpreadCount(cards.Count);
        if (_spread < 0)
            _spread = 0;
        if (_spread >= spreads)
            _spread = spreads - 1;

        _title.Text = _open.Title;
        _stage.Children.Clear();

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var previous = ActionButton("PREVIOUS", "");
        previous.Width = 140;
        previous.IsEnabled = _spread > 0;
        previous.Click += (_, _) =>
        {
            if (_spread <= 0)
                return;
            _spread--;
            ShowPages();
        };
        var next = ActionButton("NEXT", "");
        next.Width = 140;
        next.Margin = new Thickness(8, 0, 0, 0);
        next.IsEnabled = _spread < spreads - 1;
        next.Click += (_, _) =>
        {
            if (_spread >= spreads - 1)
                return;
            _spread++;
            ShowPages();
        };
        bar.Children.Add(previous);
        bar.Children.Add(next);
        bar.Children.Add(new TextBlock
        {
            Text = SpreadLabel(cards.Count, _spread, spreads),
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0)
        });
        var (left, right) = SpreadSlices(cards, _spread);
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(bar, 0);
        layout.Children.Add(bar);

        var host = new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0),
            Child = BinderBook(left, right ?? Array.Empty<Card>())
        };
        Grid.SetRow(host, 1);
        layout.Children.Add(host);
        var duplicates = ActionButton("Only show Duplicates", _duplicatesOnly ? "on" : "");
        duplicates.HorizontalAlignment = HorizontalAlignment.Left;
        duplicates.Margin = new Thickness(0, 8, 0, 0);
        duplicates.Click += (_, _) =>
        {
            _duplicatesOnly = !_duplicatesOnly;
            _spread = 0;
            ShowPages();
        };
        Grid.SetRow(duplicates, 2);
        layout.Children.Add(duplicates);
        _stage.Children.Add(layout);
    }

    private static int SpreadCount(int cards)
    {
        var pages = cards <= 0 ? 1 : (int)Math.Ceiling(cards / (double)PageSize);
        if (pages <= 1)
            return 1;
        return pages - 1;
    }

    private static (Card[] Left, Card[]? Right) SpreadSlices(IReadOnlyList<Card> cards, int spread)
    {
        var left = Take(cards, spread * PageSize, PageSize);
        var right = Take(cards, (spread + 1) * PageSize, PageSize);
        return (left, right);
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
        int pages = (int)Math.Ceiling(cards / (double)PageSize);
        int leftPage = spread + 1;
        int rightPage = spread + 2;
        if (pages <= 1)
            return "Page 1  ·  1 / 1";
        string shown = rightPage <= pages ? "Pages " + leftPage + "–" + rightPage : "Page " + leftPage;
        return shown + "  ·  " + (spread + 1) + " / " + spreads;
    }

    private FrameworkElement BinderBook(Card[] left, Card[] right)
    {
        var pages = new Grid();
        pages.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pages.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pages.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var leftPage = LeatherPage(left);
        var rightPage = LeatherPage(right);
        var gutter = new Grid { Width = 34 };
        gutter.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(0x07, 0x0C, 0x16)) });
        gutter.Children.Add(new Border
        {
            Width = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)),
            Opacity = 0.7
        });
        Grid.SetColumn(leftPage, 0);
        Grid.SetColumn(gutter, 1);
        Grid.SetColumn(rightPage, 2);
        pages.Children.Add(leftPage);
        pages.Children.Add(gutter);
        pages.Children.Add(rightPage);

        var leather = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        leather.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x28, 0x42), 0));
        leather.GradientStops.Add(new GradientStop(Color.FromRgb(0x0B, 0x14, 0x24), 0.42));
        leather.GradientStops.Add(new GradientStop(Color.FromRgb(0x18, 0x24, 0x3A), 0.72));
        leather.GradientStops.Add(new GradientStop(Color.FromRgb(0x08, 0x0E, 0x18), 1));

        var book = new Grid();
        book.Children.Add(new Border
        {
            Background = leather,
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(22, 18, 22, 18),
            Child = pages
        });
        book.Children.Add(Stitch(9));
        return book;
    }

    private FrameworkElement LeatherPage(Card[] cards)
    {
        var grid = new UniformGrid
        {
            Columns = 3,
            Rows = 3,
            Width = 720,
            Height = 1020
        };
        foreach (var card in cards)
            grid.Children.Add(CardTile(card));
        while (grid.Children.Count < PageSize)
            grid.Children.Add(EmptyPocket());
        var board = new Grid();
        board.Children.Add(grid);
        board.Children.Add(Stitch(5));
        return board;
    }

    private static Rectangle Stitch(double margin) => new()
    {
        Stroke = new SolidColorBrush(Color.FromRgb(0xC4, 0xA5, 0x74)),
        StrokeThickness = 1.25,
        StrokeDashArray = new DoubleCollection { 1.1, 1.45 },
        RadiusX = 12,
        RadiusY = 12,
        Margin = new Thickness(margin),
        IsHitTestVisible = false,
        Fill = Brushes.Transparent
    };

    private static Border EmptyPocket() => new()
    {
        Margin = new Thickness(4),
        Background = Brushes.Transparent
    };

    private Viewbox CardTile(Card card)
    {
        int copies = 0;
        bool owned = _loggedIn && _owned.TryGetValue(card.CardId, out copies) && copies > 0;
        var frame = CollectionOrder.FrameFor(card.RarityInfo);
        // Design size only. The viewbox scales this tile to the viewscreen cell.
        var image = new Image
        {
            Width = 200,
            Height = 268,
            Stretch = Stretch.Uniform
        };
        if (!string.IsNullOrWhiteSpace(card.FullImagePath))
        {
            var source = ImageFor(card.FullImagePath);
            if (source != null)
                image.Source = source;
        }

        var caption = owned
            ? card.Name + " " + copies + "x"
            : card.Name;
        var stack = new StackPanel();
        stack.Children.Add(image);
        stack.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 18,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        });

        var tile = new Border
        {
            Width = 220,
            Height = 320,
            Margin = new Thickness(4),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x12, 0x1A)),
            Child = stack,
            Opacity = owned ? 1 : 0.38
        };
        if (owned)
        {
            tile.BorderThickness = new Thickness(frame.Burst ? 3 : 2);
            tile.BorderBrush = new SolidColorBrush(frame.Color);
            tile.Effect = new DropShadowEffect
            {
                Color = frame.Color,
                BlurRadius = frame.Burst ? 16 : 8,
                ShadowDepth = 0,
                Opacity = 0.95
            };
        }
        else
        {
            tile.BorderThickness = new Thickness(1);
            tile.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
        }
        return new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = tile
        };
    }

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
            bitmap.DecodePixelWidth = 640;
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

    private Button ActionButton(string title, string subtitle)
    {
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Application.Current?.Resources["SkinBrush_GoldAccent"] as Brush ?? Brushes.Goldenrod,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        if (!string.IsNullOrEmpty(subtitle))
        {
            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        return new Button
        {
            Content = stack,
            Style = _buttonStyle,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
        FontSize = 14,
        Margin = new Thickness(8)
    };

    private sealed class Binder
    {
        public Binder(string key, string title, IReadOnlyList<Card> cards)
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

static class CollectionOrder
{
    private static readonly string[] Affiliations =
    {
        "Federation", "Klingon", "Romulan", "Non-Aligned", "Neutral", "Borg",
        "Bajoran", "Cardassian", "Dominion", "Ferengi", "Kazon", "Vidiians",
        "Hirogen", "Starfleet", "Vulcan", "Xindi"
    };

    private static readonly string[] AfterAffiliations =
    {
        "Equipment", "Doorway", "Artifact", "Mission", "Dilemma",
        "Interrupt", "Event", "Objective", "Incident", "Site"
    };

    private static readonly string[] KnownSkills =
    {
        "OFFICER", "ENGINEER", "SCIENCE", "MEDICAL", "SECURITY", "CIVILIAN", "VIP", "V.I.P.", "ANIMAL",
        "Diplomacy", "Leadership", "Navigation", "Honor", "Treachery", "Physics",
        "Computer Skill", "Anthropology", "Archaeology", "Biology", "Geology",
        "Exobiology", "Astrometrics", "Stellar Cartography", "Transporters", "Transporter Skill",
        "Empathy", "Mindmeld", "Music", "Law", "Greed", "Acquisition", "Youth",
        "Smuggling", "Resistance", "Intelligence", "Programming", "Barbering", "Astrophysics"
    };

    public static IReadOnlyList<Card> Sort(IReadOnlyList<Card> cards, List<string> notes)
    {
        var rows = cards.Select(card => KeyFor(card, notes)).ToList();
        rows.Sort(static (a, b) =>
        {
            int phase = a.Phase.CompareTo(b.Phase);
            if (phase != 0) return phase;
            int aff = a.Affiliation.CompareTo(b.Affiliation);
            if (aff != 0) return aff;
            int kind = a.Kind.CompareTo(b.Kind);
            if (kind != 0) return kind;
            int star = a.Star.CompareTo(b.Star);
            if (star != 0) return star;
            int q = a.Q.CompareTo(b.Q);
            if (q != 0) return q;
            int missing = a.Missing.CompareTo(b.Missing);
            if (missing != 0) return missing;
            int score = a.Score.CompareTo(b.Score);
            if (score != 0) return score;
            return string.Compare(a.Card.Name, b.Card.Name, StringComparison.OrdinalIgnoreCase);
        });
        return rows.Select(row => row.Card).ToList();
    }

    private static Row KeyFor(Card card, List<string> notes)
    {
        if (IsTribble(card))
            return new Row(card, 4, 0, 0, 0, IsQ(card) ? 1 : 0, 0, 0);

        var type = BaseType(card);
        int kind;
        int? score = null;
        int missing = 0;
        bool inAffiliation = false;

        if (type.Equals("Personnel", StringComparison.OrdinalIgnoreCase))
        {
            inAffiliation = true;
            kind = 0;
            var skills = SkillCount(card);
            if (skills == null)
            {
                missing = 1;
                Note(notes, "No skill text on " + card.Name + " (" + card.Set + "). Placed at the end of that affiliation's personnel.");
            }
            else
                score = -skills.Value; // descending: 6 skills, then 5, then 4
        }
        else if (type.Equals("Ship", StringComparison.OrdinalIgnoreCase))
        {
            inAffiliation = true;
            kind = 1;
            var icons = StaffingIcons(card);
            if (icons == null)
            {
                missing = 1;
                Note(notes, "No Staff field on " + card.Name + " (" + card.Set + "). Placed at the end of that affiliation's ships.");
            }
            else
            {
                score = -icons.Value;
                if (icons.Value == 0)
                    Note(notes, "Staff on " + card.Name + " (" + card.Set + ") has no [Cmd] or [Stf] icons (" + (card.Staff ?? "").Trim() + "). Counted as 0 and placed with the weakest ships.");
            }
        }
        else if (type.Equals("Facility", StringComparison.OrdinalIgnoreCase) || type.Equals("Outpost", StringComparison.OrdinalIgnoreCase))
        {
            inAffiliation = true;
            kind = 2;
        }
        else
        {
            int after = IndexOf(AfterAffiliations, type);
            if (after >= 0)
            {
                int groupScore = 0;
                int groupMissing = 0;
                if (type.Equals("Mission", StringComparison.OrdinalIgnoreCase))
                {
                    var points = MissionPoints(card);
                    if (points == null)
                    {
                        groupMissing = 1;
                        Note(notes, "No numeric points on " + card.Name + " (" + card.Set + ", points \"" + (card.Points ?? "") + "\"). Placed after numbered missions. Span was not used.");
                    }
                    else
                        groupScore = -points.Value;
                }
                else if (type.Equals("Dilemma", StringComparison.OrdinalIgnoreCase))
                {
                    var bucket = DilemmaBucket(card);
                    if (bucket == null)
                    {
                        groupMissing = 1;
                        Note(notes, "No Space/Planet mark on " + card.Name + " (" + card.Set + ", mission_dilemma_type \"" + (card.MissionDilemmaType ?? "") + "\"). Placed after Planet dilemmas.");
                    }
                    else
                        groupScore = bucket.Value;
                }
                return new Row(card, 2, 0, after, 0, IsQ(card) ? 1 : 0, groupMissing, groupScore);
            }
            Note(notes, "No sort group for type \"" + (card.Type ?? "") + "\" on " + card.Name + " (" + card.Set + "). Placed after the named types.");
            return new Row(card, 3, 0, 0, 0, IsQ(card) ? 1 : 0, 0, 0);
        }

        int aff = AffiliationIndex(card);
        if (!inAffiliation)
            aff = int.MaxValue;
        int phase = aff >= 0 ? 0 : 1;
        if (aff < 0)
        {
            aff = 0;
            var printed = (card.Affiliation ?? "").Trim();
            Note(notes, "No listed affiliation"
                + (printed.Length == 0 ? "" : " (\"" + printed + "\")")
                + " on " + card.Name + " (" + card.Set + "). Placed after the affiliation groups.");
        }
        int star = kind == 0 ? PersonnelStar(card, notes) : 0;
        return new Row(card, phase, aff, kind, star, IsQ(card) ? 1 : 0, missing, score ?? 0);
    }

    private static void Note(List<string> notes, string line)
    {
        if (!notes.Contains(line))
            notes.Add(line);
    }

    private static int AffiliationIndex(Card card)
    {
        var raw = card.Affiliation ?? "";
        if (string.IsNullOrWhiteSpace(raw))
            return -1;
        int best = -1;
        foreach (var token in SplitAffiliation(raw))
        {
            int index = CanonicalAffiliation(token);
            if (index >= 0 && (best < 0 || index < best))
                best = index;
        }
        return best;
    }

    private static IEnumerable<string> SplitAffiliation(string raw)
    {
        var parts = Regex.Split(raw, @"\[|\]|/|,|;|\+|\s+");
        foreach (var part in parts)
        {
            var token = part.Trim();
            if (token.Length > 0)
                yield return token;
        }
    }

    private static int CanonicalAffiliation(string token)
    {
        if (token.Equals("FED", StringComparison.OrdinalIgnoreCase))
            return IndexOf(Affiliations, "Federation");
        if (token.Equals("KLI", StringComparison.OrdinalIgnoreCase))
            return IndexOf(Affiliations, "Klingon");
        if (token.Equals("ROM", StringComparison.OrdinalIgnoreCase))
            return IndexOf(Affiliations, "Romulan");
        if (token.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Nonaligned", StringComparison.OrdinalIgnoreCase)
            || token.Equals("Non", StringComparison.OrdinalIgnoreCase))
            return IndexOf(Affiliations, "Non-Aligned");
        if (token.Equals("Aligned", StringComparison.OrdinalIgnoreCase))
            return -1;
        if (token.Equals("Vidiian", StringComparison.OrdinalIgnoreCase))
            return IndexOf(Affiliations, "Vidiians");
        return IndexOf(Affiliations, token);
    }

    private static int IndexOf(string[] list, string token)
    {
        for (var i = 0; i < list.Length; i++)
        {
            if (list[i].Equals(token, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string BaseType(Card card)
    {
        var type = (card.Type ?? "").Trim();
        if (type.StartsWith("Q ", StringComparison.OrdinalIgnoreCase))
            return type[2..].Trim();
        if (type.Equals("Q", StringComparison.OrdinalIgnoreCase))
            return "";
        return type;
    }

    private static bool IsQ(Card card)
    {
        var type = (card.Type ?? "").Trim();
        if (type.StartsWith("Q ", StringComparison.OrdinalIgnoreCase) || type.Equals("Q", StringComparison.OrdinalIgnoreCase))
            return true;
        if ((card.Icons ?? "").Contains("[Q]", StringComparison.OrdinalIgnoreCase))
            return true;
        var name = (card.Name ?? "").Trim();
        return name.Equals("Q", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Q ", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Q's", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Q’s", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTribble(Card card)
        => (card.Name ?? "").Contains("Tribble", StringComparison.OrdinalIgnoreCase)
           || (card.Type ?? "").Contains("Tribble", StringComparison.OrdinalIgnoreCase);

    /// <summary>Null when Text, Class, and Characteristics are all empty. Not a stored skill-count field.</summary>
    private static int? SkillCount(Card card)
    {
        if (string.IsNullOrWhiteSpace(card.Text)
            && string.IsNullOrWhiteSpace(card.Class)
            && string.IsNullOrWhiteSpace(card.Characteristics))
            return null;
        string blob = (card.Text ?? "") + " " + (card.Class ?? "") + " " + (card.Characteristics ?? "");
        int n = 0;
        foreach (var skill in KnownSkills)
        {
            if (blob.Contains(skill, StringComparison.OrdinalIgnoreCase))
                n++;
        }
        return n;
    }

    /// <summary>Null when Staff is empty. Count is printed [Cmd] plus [Stf] only.</summary>
    private static int? StaffingIcons(Card card)
    {
        if (string.IsNullOrWhiteSpace(card.Staff))
            return null;
        var (cmd, stf) = MovementRules.ParseStaffingRequirement(card);
        return cmd + stf;
    }

    public static RarityFrame FrameFor(string? rarityInfo)
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

    private static string LastToken(string? rarityInfo)
    {
        var parts = (rarityInfo ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "" : parts[^1];
    }

    public readonly record struct RarityFrame(Color Color, bool Burst);

    private readonly record struct Row(Card Card, int Phase, int Affiliation, int Kind, int Star, int Q, int Missing, int Score);

    /// <summary>
    /// Gold star is the command icon [Cmd] on Icons (Officer). Silver is the staff icon [Stf] (Crew).
    /// A card with both is gold only. No [Cmd] and no [Stf] is the third group, including [Holo] alone.
    /// integrity_or_range is a number and is not a star. uniqueness is not this icon.
    /// Within a star group, skill count is descending: most skills first.
    /// </summary>
    private static int PersonnelStar(Card card, List<string> notes)
    {
        _ = notes;
        var icons = card.Icons ?? "";
        bool gold = icons.Contains("[Cmd]", StringComparison.OrdinalIgnoreCase)
            || icons.Contains("[Com]", StringComparison.OrdinalIgnoreCase);
        bool silver = icons.Contains("[Stf]", StringComparison.OrdinalIgnoreCase)
            || icons.Contains("[Staff]", StringComparison.OrdinalIgnoreCase);
        if (gold)
            return 0;
        if (silver)
            return 1;
        return 2;
    }

    /// <summary>Printed mission points. Null when Points is empty or not a number. Span is ignored.</summary>
    private static int? MissionPoints(Card card)
    {
        var raw = (card.Points ?? "").Trim();
        if (raw.Length == 0)
            return null;
        var match = Regex.Match(raw, @"-?\d+");
        if (!match.Success || !int.TryParse(match.Value, out var points))
            return null;
        return points;
    }

    /// <summary>0 space/planet, 1 space, 2 planet. From MissionDilemmaType only.</summary>
    private static int? DilemmaBucket(Card card)
    {
        var raw = (card.MissionDilemmaType ?? "").Trim();
        if (raw.Length == 0)
            return null;
        if (raw.Contains("S/P", StringComparison.OrdinalIgnoreCase) || raw.Contains("P/S", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (raw.Equals("[S]", StringComparison.OrdinalIgnoreCase) || raw.Equals("S", StringComparison.OrdinalIgnoreCase) || raw.Equals("Space", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (raw.Equals("[P]", StringComparison.OrdinalIgnoreCase) || raw.Equals("P", StringComparison.OrdinalIgnoreCase) || raw.Equals("Planet", StringComparison.OrdinalIgnoreCase))
            return 2;
        return null;
    }
}
