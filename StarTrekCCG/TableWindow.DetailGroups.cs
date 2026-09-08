using System.Windows;

namespace StarTrekCCG;

/// <summary>
/// Empty on purpose. Revert 2026-09-08: grouping hook removed.
/// Window.Loaded still points here so the project compiles.
/// </summary>
public partial class TableWindow
{
    private void TableWindow_HookDetailGroups(object sender, RoutedEventArgs e)
    {
    }
}
