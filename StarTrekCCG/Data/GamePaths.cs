using System;
using System.IO;
using System.Linq;

namespace StarTrekCCG;

/// <summary>
/// Resolves repo-relative data/asset folders (works from VS bin\ and from a published exe).
///
/// Expected layout next to the .csproj / in output:
///   Data/                  set folders with cards.json + card images
///   Assets/card_back.jpg
///   Assets/BoardBackgrounds/*.png
///
/// Override: environment variable STCCG_DATA (legacy C:\STCCG_Data still works as fallback).
/// </summary>
public static class GamePaths
{
    public static string DataRoot { get; } = ResolveDataRoot();
    public static string AssetsRoot { get; } = ResolveAssetsRoot();
    public static string BoardBackgrounds => Path.Combine(AssetsRoot, "BoardBackgrounds");

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