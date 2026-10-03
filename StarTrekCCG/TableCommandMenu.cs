using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarTrekCCG;

/// <summary>
/// Table COMMAND menu. Same dark panel and buttons as BridgeDialog. Not a Windows menu.
/// </summary>
public sealed class TableCommandMenu : Window
{
    readonly List<Page> _stack = new();

    public TableCommandMenu(IReadOnlyList<Entry> root)
    {
        Title = "COMMAND";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 720;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = Brushes.Transparent;
        _stack.Add(new Page("COMMAND", root));

        var skin = Application.Current?.TryFindResource("SkinBrush_HeaderBackground") != null
            ? Application.Current.Resources
            : null;
        Brush Take(string key, Brush fallback) => skin?[key] as Brush ?? fallback;

        var panel = new Border
        {
            Background = Take("SkinBrush_HeaderBackground", new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x38))),
            BorderBrush = Take("SkinBrush_OuterFrame", new SolidColorBrush(Color.FromRgb(0x4B, 0x6B, 0x88))),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(8)
        };
        var stack = new StackPanel();
        var title = new TextBlock
        {
            Foreground = Take("SkinBrush_GoldAccent", Brushes.Goldenrod),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        stack.Children.Add(title);
        var listHost = new StackPanel();
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 560,
            Content = listHost
        };
        stack.Children.Add(scroller);
        var back = new Button
        {
            Height = 34,
            Margin = new Thickness(0, 10, 0, 0),
            Style = skin?["SkinStyle_SciFiButton"] as Style,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        back.Click += (_, _) =>
        {
            if (_stack.Count > 1)
            {
                _stack.RemoveAt(_stack.Count - 1);
                ShowPage(title, listHost, back, skin);
            }
            else
            {
                Close();
            }
        };
        stack.Children.Add(back);
        panel.Child = stack;
        Content = panel;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape)
                return;
            Close();
            e.Handled = true;
        };
        Loaded += (_, _) => ShowPage(title, listHost, back, skin);
    }

    public static void Show(Window owner, FrameworkElement? anchor, IReadOnlyList<Entry> root)
    {
        var dialog = new TableCommandMenu(root) { Owner = owner };
        if (anchor != null)
        {
            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            var origin = anchor.PointToScreen(new Point(0, anchor.ActualHeight + 6));
            var source = PresentationSource.FromVisual(anchor);
            if (source?.CompositionTarget != null)
            {
                var dip = source.CompositionTarget.TransformFromDevice.Transform(origin);
                dialog.Left = dip.X;
                dialog.Top = dip.Y;
            }
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        dialog.ShowDialog();
    }

    void ShowPage(TextBlock title, StackPanel host, Button back, ResourceDictionary? skin)
    {
        var page = _stack[^1];
        title.Text = page.Title;
        host.Children.Clear();
        var style = skin?["SkinStyle_SciFiButton"] as Style;
        foreach (var entry in page.Entries)
        {
            var label = entry.Label;
            if (entry.Children != null)
                label += "   ›";
            else if (entry.Checked != null)
                label = (entry.Checked() ? "✓  " : "·  ") + label;
            var button = new Button
            {
                Content = label,
                Height = 34,
                Margin = new Thickness(0, 0, 0, 6),
                Style = style,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 0, 12, 0)
            };
            var captured = entry;
            button.Click += (_, _) =>
            {
                if (captured.Children != null)
                {
                    _stack.Add(new Page(captured.Label, captured.Children));
                    ShowPage(title, host, back, skin);
                    return;
                }
                if (captured.Checked != null)
                {
                    captured.Run?.Invoke();
                    ShowPage(title, host, back, skin);
                }
                else
                {
                    Close();
                    captured.Run?.Invoke();
                }
            };
            host.Children.Add(button);
        }
        back.Content = _stack.Count > 1 ? "BACK" : "CLOSE";
    }

    sealed record Page(string Title, IReadOnlyList<Entry> Entries);

    public sealed class Entry
    {
        public string Label { get; init; } = "";
        public IReadOnlyList<Entry>? Children { get; init; }
        public Action? Run { get; init; }
        public Func<bool>? Checked { get; init; }
    }
}
