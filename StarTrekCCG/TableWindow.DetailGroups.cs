using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace StarTrekCCG;

/// <summary>
/// Pepsch 2026-09-08: group Away-Team detail minis by effect-set and fix
/// EffectAndContinue overlay title. Hooked from Window.Loaded so we do not
/// rewrite the 880 KB TableWindow.xaml.cs.
/// </summary>
public partial class TableWindow
{
    private bool _detailGroupsHooked;
    private bool _detailGroupsBusy;

    private void TableWindow_HookDetailGroups(object sender, RoutedEventArgs e)
    {
        if (_detailGroupsHooked) return;
        _detailGroupsHooked = true;
        if (DetailStackCards != null)
            DetailStackCards.LayoutUpdated += (_, _) => RegroupDetailNegatives();
        if (CardRevealOverlay != null)
            CardRevealOverlay.IsVisibleChanged += CardRevealOverlay_FixEffectTitle;
    }

    private void CardRevealOverlay_FixEffectTitle(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (CardRevealOverlay?.Visibility != Visibility.Visible) return;
        if (RevealTitle == null) return;
        string title = RevealTitle.Text ?? "";
        if (!title.StartsWith("RELOCATED", StringComparison.OrdinalIgnoreCase)) return;
        string sub = RevealSubtitle?.Text ?? "";
        string body = RevealBody?.Text ?? "";
        if (DetailStatusRules.IsRelocateContinue(sub, body)) return;
        RevealTitle.Text = "EFFECT" + title["RELOCATED".Length..];
    }

    private void RegroupDetailNegatives()
    {
        if (_detailGroupsBusy || DetailStackCards == null) return;
        var kids = DetailStackCards.Children;
        if (kids.Count == 0) return;

        var singles = new List<(int index, string label, UIElement visual)>();
        for (int i = 0; i < kids.Count - 1; i++)
        {
            if (kids[i] is not TextBlock tb || tb.FontSize != 10) continue;
            string label = (tb.Text ?? "").Trim();
            if (label is not ("Stopped" or "Quarantined" or "Stasis")) continue;
            if (kids[i + 1] is TextBlock) continue;
            singles.Add((i, label, kids[i + 1]));
        }
        if (singles.Count == 0) return;

        _detailGroupsBusy = true;
        try
        {
            var buckets = new Dictionary<string, List<UIElement>>(StringComparer.Ordinal);
            foreach (var row in singles)
            {
                if (!buckets.TryGetValue(row.label, out var list))
                {
                    list = new List<UIElement>();
                    buckets[row.label] = list;
                }
                list.Add(row.visual);
            }

            // Remove per-card labels + their minis (high index first).
            for (int i = singles.Count - 1; i >= 0; i--)
            {
                var row = singles[i];
                kids.Remove(row.visual);
                if (row.index < kids.Count && kids[row.index] is TextBlock tb && tb.FontSize == 10)
                    kids.RemoveAt(row.index);
            }

            int insertAt = FindPersonnelOrEquipmentHeaderIndex();
            if (insertAt < 0) insertAt = kids.Count;

            var ordered = buckets
                .OrderByDescending(kv => kv.Key == "Quarantined" ? 3 : kv.Key == "Stasis" ? 2 : 1)
                .ThenBy(kv => kv.Key);
            // Multi-effect cards are already a single label in the old UI (Stopped OR Quarantined OR Stasis).
            // Combinations appear as one label today; group header still uses count.

            foreach (var kv in ordered)
            {
                var header = MakeGroupHeader(
                    DetailStatusRules.FormatEffectGroupHeader(new[] { kv.Key }, kv.Value.Count),
                    Color.FromRgb(0xE0, 0x6A, 0x6A));
                kids.Insert(insertAt++, header);
                foreach (var visual in kv.Value)
                    kids.Insert(insertAt++, visual);
            }
        }
        finally
        {
            _detailGroupsBusy = false;
        }
    }

    private int FindPersonnelOrEquipmentHeaderIndex()
    {
        var kids = DetailStackCards.Children;
        for (int i = 0; i < kids.Count; i++)
        {
            if (kids[i] is TextBlock tb && tb.FontSize == 11)
            {
                string t = tb.Text ?? "";
                if (t.StartsWith("Personnel", StringComparison.Ordinal) ||
                    t.StartsWith("Equipment", StringComparison.Ordinal))
                    return i;
            }
        }
        return -1;
    }

    private static TextBlock MakeGroupHeader(string text, Color color) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(color),
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(4, 4, 6, 2),
        Effect = new DropShadowEffect
        {
            Color = color,
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = 0.75
        }
    };
}
