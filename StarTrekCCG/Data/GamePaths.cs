using System;
using System.IO;
using System.Linq;

namespace StarTrekCCG;

/// <summary>
/// Resolves repo-relative data/asset folders (works from VS bin\ and from a published exe).
///
/// Expected layout next to the .csproj / in output:
///   Data/Sets/<SetName>/   cards.json + images  (legacy: Data/<SetName>/)
///   Assets/card_back.jpg
///   Assets/BoardBackgrounds/*.png
///   Assets/Icons/Icon_{Token}.png   ([Cmd] → Icon_Cmd.png)
///
/// Override: environment variable STCCG_DATA (legacy C:\STCCG_Data still works as fallback).
/// </summary>
public static class GamePaths
{
    public static string DataRoot { get; } = ResolveDataRoot();
    public static string AssetsRoot { get; } = ResolveAssetsRoot();
    public static string BoardBackgrounds => Path.Combine(AssetsRoot, "BoardBackgrounds");

    /// <summary>Data/ folder that contains Sets, Decks, SaveGames.</summary>
    public static string DataFolder
    {
        get
        {
            string root = DataRoot;
            string name = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (name.Equals("Sets", StringComparison.OrdinalIgnoreCase))
            {
                string? parent = Path.GetDirectoryName(root);
                if (!string.IsNullOrEmpty(parent)) return parent;
            }
            return root;
        }
    }

    public static string DecksRoot => EnsureDir(Path.Combine(DataFolder, "Decks"));
    public static string SaveGamesRoot => EnsureDir(Path.Combine(DataFolder, "SaveGames"));
    public static string LogsRoot => EnsureDir(Path.Combine(DataFolder, "Logs"));

    private static string EnsureDir(string path)
    {
        try { Directory.CreateDirectory(path); } catch { }
        return path;
    }

    public static string? FindCardBack()
    {
        foreach (var p in new[]
                 {
                     Path.Combine(AssetsRoot, "card_back.jpg"),
                     Path.Combine(AssetsRoot, "card_back.png"),
                     Path.Combine(DataRoot, "card_back.jpg"),
                     Path.Combine(DataRoot, "Common", "card_back.jpg"),
                     Path.Combine(DataRoot, "Assets", "card_back.jpg"),
                 })
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static string ResolveDataRoot()
    {
        string? env = Environment.GetEnvironmentVariable("STCCG_DATA");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            return env;

        foreach (var root in CandidateRoots())
        {
            string dataSets = Path.Combine(root, "Data", "Sets");
            if (LooksLikeCardData(dataSets)) return dataSets;
            string data = Path.Combine(root, "Data");
            if (LooksLikeCardData(data)) return data;
            if (LooksLikeCardData(root)) return root;
        }

        const string legacy = @"C:\STCCG_Data";
        if (Directory.Exists(legacy)) return legacy;

        return Path.Combine(AppContext.BaseDirectory, "Data");
    }

    private static string ResolveAssetsRoot()
    {
        foreach (var root in CandidateRoots())
        {
            string assets = Path.Combine(root, "Assets");
            if (Directory.Exists(assets)) return assets;
        }
        string underData = Path.Combine(DataRoot, "Assets");
        if (Directory.Exists(underData)) return underData;
        return Path.Combine(AppContext.BaseDirectory, "Assets");
    }

    private static bool LooksLikeCardData(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        try
        {
            return Directory.EnumerateFiles(dir, "cards.json", SearchOption.AllDirectories).Any();
        }
        catch
        {
            return false;
        }
    }

    private static System.Collections.Generic.IEnumerable<string> CandidateRoots()
    {
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new System.Collections.Generic.List<string>();
        void Add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            try { p = Path.GetFullPath(p); } catch { return; }
            if (seen.Add(p)) list.Add(p);
        }

        Add(AppContext.BaseDirectory);
        Add(Directory.GetCurrentDirectory());

        string? walk = AppContext.BaseDirectory;
        for (int i = 0; i < 7 && !string.IsNullOrEmpty(walk); i++)
        {
            Add(walk);
            walk = Directory.GetParent(walk)?.FullName;
        }
        return list;
    }
}