using System;
using System.Windows;

namespace StarTrekCCG;

/// <summary>
/// Window.Loaded hook. AT-detail effect grouping lives in FillDetailStackSection;
/// this partial patches EffectAndContinue reveal titles (Firestorm EFFECT vs Love Interest RELOCATED).
/// </summary>
public partial class TableWindow
{
    private bool _detailGroupsHooked;

    private void TableWindow_HookDetailGroups(object sender, RoutedEventArgs e)
    {
        if (_detailGroupsHooked) return;
        _detailGroupsHooked = true;
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
        // Also check title remainder / body for Love Interest if subtitle empty
        if (DetailStatusRules.IsRelocateContinue(title, body)) return;
        RevealTitle.Text = "EFFECT" + title["RELOCATED".Length..];
    }
}
