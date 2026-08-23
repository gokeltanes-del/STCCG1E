using System.IO;
using System.Windows.Media.Imaging;

namespace StarTrekCCG;

/// <summary>
/// Gemeinsamer Kartenrücken für verdeckte Stapel (Draw, Seed unter Mission für Gegner, …).
/// Datei: Assets/card_back.jpg (siehe GamePaths). Fallback: DataRoot / Common / legacy DataPath.
/// </summary>
public static class CardBack
{
    public const string FileName = "card_back.jpg";

    public static string? ResolvePath(string? dataPath = null)
    {
        string? viaAssets = GamePaths.FindCardBack();
        if (viaAssets != null) return viaAssets;

        if (!string.IsNullOrWhiteSpace(dataPath))
        {
            string primary = Path.Combine(dataPath, FileName);
            if (File.Exists(primary)) return primary;
            string alt = Path.Combine(dataPath, "Common", FileName);
            if (File.Exists(alt)) return alt;
        }
        return null;
    }

    public static BitmapImage? Load(string? dataPath = null)
    {
        string? path = ResolvePath(dataPath);
        if (path == null) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}