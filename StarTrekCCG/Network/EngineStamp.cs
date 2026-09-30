using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarTrekCCG.Network;

/// <summary>
/// Engine id plus a hash of the cards.json files this process actually loads.
/// Not a deck hash. Compared before a network start.
/// </summary>
public static class EngineStamp
{
    public const string Id = "stccg-1e";

    public static string CardHash { get; } = ComputeCardHash();

    public static string Describe()
        => Id + " cards " + CardHash[..8] + " (" + CardFileCount + " cards.json under " + DataRoot + ")";

    public static int CardFileCount { get; private set; }
    public static string DataRoot { get; private set; } = "";

    private static string ComputeCardHash()
    {
        DataRoot = GamePaths.DataRoot;
        string[] files;
        try
        {
            files = Directory.Exists(DataRoot)
                ? Directory.EnumerateFiles(DataRoot, "cards.json", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();
        }
        catch
        {
            files = Array.Empty<string>();
        }

        CardFileCount = files.Length;
        using var sha = SHA256.Create();
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(DataRoot, file).Replace('\\', '/').ToLowerInvariant();
            var nameBytes = Encoding.UTF8.GetBytes(rel);
            sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
            byte[] data;
            try { data = File.ReadAllBytes(file); }
            catch { data = Array.Empty<byte>(); }
            sha.TransformBlock(data, 0, data.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }
}