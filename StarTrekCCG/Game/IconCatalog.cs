using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// PNG glyphs in Assets/Icons/Icon_{Token}.png (e.g. Icon_Cmd.png for [Cmd]).
/// // [IPG] → Icon_IPG.png (Interphase Generator; source INTERPHASE_GENERATOR.png)
/// Missing file → text fallback [Token]. Safe to add icons one at a time.
/// </summary>
public static class IconCatalog
{
    private static readonly Dictionary<string, ImageSource?> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public static string Folder => Path.Combine(GamePaths.AssetsRoot, "Icons");

    public static ImageSource? TryGet(string token)
    {
        token = Sanitize(token);
        if (token.Length == 0) return null;
        if (Cache.TryGetValue(token, out var hit)) return hit;

        ImageSource? src = null;
        try
        {
            // Prefer Icon_{Token}.png (Cmd/Stf). Alt names: [IPG] → INTERPHASE_GENERATOR.png
            string path = Path.Combine(Folder, "Icon_" + token + ".png");
            if (!File.Exists(path) && AltFileName(token) is string alt)
                path = Path.Combine(Folder, alt);
            if (File.Exists(path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.DecodePixelWidth = 64;
                bmp.EndInit();
                bmp.Freeze();
                src = bmp;
            }
        }
        catch
        {
            src = null;
        }

        Cache[token] = src;
        return src;
    }

    public static IReadOnlyList<string> TokensOn(Card? card) =>
        card == null ? Array.Empty<string>() : CardIcons.Parse(card).Tokens;

    /// <summary>[Cmd][Stf][Stf] → Cmd, Stf, Stf (order and duplicates kept).</summary>
    public static List<string> StaffTokens(Card? card) => BracketTokens(card?.Staff);

    public static void FillStaffing(Panel? row, Card? card, double size = 20)
    {
        if (row == null) return;
        row.Children.Clear();
        var toks = StaffTokens(card);
        if (toks.Count == 0)
        {
            row.Visibility = Visibility.Collapsed;
            return;
        }

        row.Children.Add(new TextBlock
        {
            Text = "Staffing:",
            Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xDC, 0xFE)),
            FontSize = Math.Max(11, size * 0.65),
            Margin = new Thickness(0, 2, 8, 1),
            VerticalAlignment = VerticalAlignment.Center
        });
        foreach (var tok in toks)
            AddGlyph(row, tok, size);
        row.Visibility = Visibility.Visible;
    }

    public static void Fill(Panel? row, Card? card, double size = 20)
    {
        if (row == null) return;
        row.Children.Clear();
        if (card == null)
        {
            row.Visibility = Visibility.Collapsed;
            return;
        }

        var staff = new HashSet<string>(StaffTokens(card), StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in BracketTokens(card.Icons))
        {
            string tok = Sanitize(raw);
            if (tok.Length == 0 || staff.Contains(tok) || !seen.Add(tok)) continue;
            AddGlyph(row, tok, size);
        }

        row.Visibility = row.Children.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private static List<string> BracketTokens(string? raw)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return list;
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(raw, @"\[([^\]]+)\]"))
        {
            string tok = Sanitize(m.Groups[1].Value);
            if (tok.Length > 0) list.Add(tok);
        }
        return list;
    }

    private static void AddGlyph(Panel row, string tok, double size)
    {
        var src = TryGet(tok);
        if (src != null)
        {
            var img = new Image
            {
                Source = src,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 1, 4, 1),
                ToolTip = "[" + tok + "]"
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            row.Children.Add(img);
            return;
        }

        row.Children.Add(new TextBlock
        {
            Text = "[" + tok + "]",
            Foreground = new SolidColorBrush(Color.FromRgb(0xC5, 0x86, 0xC0)),
            FontSize = Math.Max(10, size * 0.7),
            Margin = new Thickness(0, 1, 6, 1),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "No PNG yet — add Assets/Icons/Icon_" + tok + ".png"
        });
    }

    /// <summary>How many present cards show each special icon. Empty if none.</summary>
    public static List<(string Token, int Count)> CountOn(IEnumerable<Card> cards)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in cards)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tok in BracketTokens(c.Icons))
            {
                if (tok.Length == 0 || IsCountdown(tok) || !seen.Add(tok)) continue;
                map[tok] = map.GetValueOrDefault(tok) + 1;
            }
        }
        return map
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    public static void AppendCounts(System.Windows.Documents.InlineCollection inlines, IEnumerable<Card> cards, double size = 12)
    {
        var counts = CountOn(cards);
        if (counts.Count == 0) return;
        var green = new SolidColorBrush(Color.FromRgb(0xB5, 0xCE, 0xA8));
        foreach (var (tok, n) in counts)
        {
            inlines.Add(new System.Windows.Documents.Run("  ·  ") { Foreground = green });
            inlines.Add(new System.Windows.Documents.Run($"{n} ") { Foreground = green });
            var src = TryGet(tok);
            if (src != null)
            {
                var img = new Image
                {
                    Source = src,
                    Width = size,
                    Height = size,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 0, 2, 0),
                    ToolTip = n + " [" + tok + "]",
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                inlines.Add(new System.Windows.Documents.InlineUIContainer(img)
                {
                    BaselineAlignment = BaselineAlignment.Center
                });
            }
            else
            {
                inlines.Add(new System.Windows.Documents.Run("[" + tok + "]") { Foreground = green });
            }
        }
    }

    private static bool IsCountdown(string tok) =>
        tok.Length == 1 && char.IsDigit(tok[0]);


    /// <summary>Non-Icon_* filenames. // [IPG] → INTERPHASE_GENERATOR.png</summary>
    private static string? AltFileName(string token) =>
        token.Equals("IPG", StringComparison.OrdinalIgnoreCase) ? "INTERPHASE_GENERATOR.png" : null;
    private static string Sanitize(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "";
        var t = token.Trim().Trim('[', ']');
        foreach (var c in Path.GetInvalidFileNameChars())
            t = t.Replace(c, '_');
        t = t.Replace(' ', '_');
        return t;
    }
}
