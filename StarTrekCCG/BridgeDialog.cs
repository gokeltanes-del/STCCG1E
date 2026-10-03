using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarTrekCCG;

/// <summary>
/// One in-app prompt for the client. Same decisions as MessageBox (OK, Yes, No, Cancel).
/// Dark panel and the main-menu button style. The message text is shown as given.
/// </summary>
public static class BridgeDialog
{
    public static MessageBoxResult Show(string messageBoxText)
        => Show(null, messageBoxText, "", MessageBoxButton.OK, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption)
        => Show(null, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        => Show(null, messageBoxText, caption, button, MessageBoxImage.None);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        => Show(null, messageBoxText, caption, button, icon);

    public static MessageBoxResult Show(Window? owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        var dialog = new BridgeDialogWindow(messageBoxText ?? "", caption ?? "", button, icon);
        if (owner != null)
            dialog.Owner = owner;
        else if (Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) is Window active)
            dialog.Owner = active;
        dialog.ShowDialog();
        return dialog.Result;
    }
}

sealed class BridgeDialogWindow : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public BridgeDialogWindow(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Title = string.IsNullOrWhiteSpace(caption) ? "Star Trek CCG" : caption;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        MinHeight = 160;
        MaxHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = Brushes.Transparent;

        var skin = Application.Current?.TryFindResource("SkinBrush_HeaderBackground") != null
            ? Application.Current.Resources
            : null;
        if (skin == null)
        {
            var local = new ResourceDictionary { Source = new Uri("ThemeSkin.xaml", UriKind.Relative) };
            Resources.MergedDictionaries.Add(local);
            skin = Resources;
        }

        Brush Take(string key, Brush fallback) => skin[key] as Brush ?? fallback;
        var accent = icon switch
        {
            MessageBoxImage.Error or MessageBoxImage.Stop or MessageBoxImage.Hand
                => Take("SkinBrush_RedOffline", Brushes.IndianRed),
            MessageBoxImage.Warning or MessageBoxImage.Exclamation
                => Take("SkinBrush_GoldAccent", Brushes.Goldenrod),
            _ => Take("SkinBrush_GoldAccent", Brushes.Goldenrod)
        };

        var panel = new Border
        {
            Background = Take("SkinBrush_HeaderBackground", new SolidColorBrush(Color.FromRgb(0x1E, 0x2A, 0x38))),
            BorderBrush = Take("SkinBrush_OuterFrame", new SolidColorBrush(Color.FromRgb(0x4B, 0x6B, 0x88))),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18, 14, 18, 16),
            Margin = new Thickness(10)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = Title,
            Foreground = accent,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
        stack.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var style = skin["SkinStyle_SciFiButton"] as Style;

        void Add(string label, MessageBoxResult value, bool isDefault, bool isCancel)
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 96,
                Height = 34,
                Margin = new Thickness(8, 0, 0, 0),
                Style = style,
                IsDefault = isDefault,
                IsCancel = false /* Escape is handled on the window */
            };
            button.Click += (_, _) =>
            {
                Result = value;
                DialogResult = value is MessageBoxResult.OK or MessageBoxResult.Yes;
            };
            row.Children.Add(button);
        }

        switch (buttons)
        {
            case MessageBoxButton.OKCancel:
                Add("OK", MessageBoxResult.OK, true, false);
                Add("Cancel", MessageBoxResult.Cancel, false, true);
                break;
            case MessageBoxButton.YesNo:
                Add("Yes", MessageBoxResult.Yes, true, false);
                Add("No", MessageBoxResult.No, false, true);
                break;
            case MessageBoxButton.YesNoCancel:
                Add("Yes", MessageBoxResult.Yes, true, false);
                Add("No", MessageBoxResult.No, false, false);
                Add("Cancel", MessageBoxResult.Cancel, false, true);
                break;
            default:
                Add("OK", MessageBoxResult.OK, true, true);
                break;
        }

        stack.Children.Add(row);
        panel.Child = stack;
        Content = panel;

        MessageBoxResult Dismiss() => buttons switch
        {
            MessageBoxButton.YesNo => MessageBoxResult.No,
            MessageBoxButton.OKCancel or MessageBoxButton.YesNoCancel => MessageBoxResult.Cancel,
            _ => MessageBoxResult.OK
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape || Result != MessageBoxResult.None)
                return;
            Result = Dismiss();
            DialogResult = false;
            e.Handled = true;
        };
        Closing += (_, _) =>
        {
            if (Result == MessageBoxResult.None)
                Result = Dismiss();
        };
    }
}
