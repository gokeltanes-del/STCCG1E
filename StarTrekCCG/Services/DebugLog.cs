using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Session file logger (BOARD_MODEL Schritt 0).
/// Action History stays the short player list. This file gets both player lines
/// and structured dual-run lines (ui: / board:).
///
/// CheckTrace.Cmp is NOT forwarded here — skill-token noise stays UI-optional.
/// </summary>
public static class DebugLog
{
    public enum Channel
    {
        Play,
        Move,
        Beam,
        Target,
        Board,
        Layout,
        Engine,
        Save,
        Seed,
        System
    }

    private static readonly object Gate = new();
    private static StreamWriter? _writer;
    private static bool _enabled = true;
    private static string? _path;

    /// <summary>Optional: show Move/Beam/Target lines in Action History (debug filter).</summary>
    public static Action<int, string, string>? HistorySink { get; set; }

    /// <summary>Default on in Dev. Toggle from Developer menu.</summary>
    public static bool Enabled
    {
        get { lock (Gate) return _enabled; }
        set { lock (Gate) _enabled = value; }
    }

    public static string? CurrentPath
    {
        get { lock (Gate) return _path; }
    }

    public static bool IsOpen
    {
        get { lock (Gate) return _writer != null; }
    }

    /// <summary>One file per app session: Data/Logs/stccg-yyyyMMdd-HHmmss.txt</summary>
    public static void StartSession(string reason = "TableWindow")
    {
        lock (Gate)
        {
            if (_writer != null)
            {
                WriteUnlocked(Channel.System, 0, 0, $"session already open ({reason})");
                return;
            }

            try
            {
                string dir = GamePaths.LogsRoot;
                _path = Path.Combine(dir, $"stccg-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                _writer = new StreamWriter(_path, append: false, Encoding.UTF8)
                {
                    AutoFlush = true
                };
                _writer.WriteLine($"# STCCG 1E debug log");
                _writer.WriteLine($"# started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"# reason  {reason}");
                _writer.WriteLine($"# data    {GamePaths.DataFolder}");
                _writer.WriteLine($"# format  HH:mm:ss.fff T{{turn}} P{{n}} [Channel] text");
                _writer.WriteLine("#");
            }
            catch
            {
                _writer = null;
                _path = null;
            }
        }
    }

    public static void Stop()
    {
        lock (Gate)
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
        }
    }

    public static void Divider(string title) =>
        Write(Channel.System, 0, 0, "==== " + title + " ====");

    public static void Write(Channel channel, int turn, int player, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (Gate)
        {
            if (!_enabled || _writer == null) return;
            WriteUnlocked(channel, turn, player, text);
        }
        if (channel is Channel.Move or Channel.Beam or Channel.Target)
        {
            try { HistorySink?.Invoke(turn, channel.ToString(), text); }
            catch { /* history is optional */ }
        }
    }

    /// <summary>Hook from ActionLog — player lines and AddDebug, never CheckTrace.Cmp.</summary>
    public static void FromActionLog(int turn, string actor, string text, bool debug)
    {
        var ch = debug ? Channel.Engine : Channel.Play;
        Write(ch, turn, ParsePlayer(actor), text);
    }

    public static void Play(int turn, int player, string text) => Write(Channel.Play, turn, player, text);
    public static void Move(int turn, int player, string text) => Write(Channel.Move, turn, player, text);
    public static void Beam(int turn, int player, string text) => Write(Channel.Beam, turn, player, text);
    public static void Target(int turn, int player, string text) => Write(Channel.Target, turn, player, text);
    public static void Board(int turn, int player, string text) => Write(Channel.Board, turn, player, text);
    public static void Layout(int turn, int player, string text) => Write(Channel.Layout, turn, player, text);
    public static void Engine(int turn, int player, string text) => Write(Channel.Engine, turn, player, text);
    public static void Save(int turn, int player, string text) => Write(Channel.Save, turn, player, text);
    public static void Seed(int turn, int player, string text) => Write(Channel.Seed, turn, player, text);

    /// <summary>Dual-run probe: same verb, two sources. Mismatch = sync bug, not Relayout.</summary>
    public static void Dual(int turn, int player, string verb, string uiLine, string boardLine)
    {
        Write(Channel.Board, turn, player, $"ui:    {verb} {uiLine}");
        Write(Channel.Board, turn, player, $"board: {verb} {boardLine}");
    }

    public static void Exception(string where, Exception ex) =>
        Write(Channel.Engine, 0, 0, $"EX {where}: {ex.GetType().Name}: {ex.Message}");

    /// <summary>Multi-line dump (BoardStore later). Each line indented under a header.</summary>
    public static void Block(Channel channel, int turn, int player, string title, IEnumerable<string> lines)
    {
        Write(channel, turn, player, "--- " + title);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                Write(channel, turn, player, "  " + line);
        }
        Write(channel, turn, player, "--- end " + title);
    }

    public static string Card(Card? c)
    {
        if (c == null) return "?";
        string name = string.IsNullOrWhiteSpace(c.Name) ? "?" : c.Name;
        return c.InstanceId > 0 ? $"{name} #{c.InstanceId}" : name;
    }

    public static string Site(string kind, Card? c) => $"{kind}:{Card(c)}";

    public static void BeamCard(int turn, int player, Card who, string fromKind, Card? from, string toKind, Card? to) =>
        Beam(turn, player, $"{Card(who)} {Site(fromKind, from)} → {Site(toKind, to)}");

    public static void MoveShip(int turn, int player, Card ship, Card? from, Card? to, int cost, int rangeLeft) =>
        Move(turn, player, $"{Card(ship)} {Card(from)} → {Card(to)} cost={cost} left={rangeLeft}");

    public static bool TryOpenFile()
    {
        string? path;
        lock (Gate) path = _path;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryOpenFolder()
    {
        try
        {
            string dir = GamePaths.LogsRoot;
            Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteUnlocked(Channel channel, int turn, int player, string text)
    {
        if (_writer == null) return;
        try
        {
            string t = turn <= 0 ? "-" : turn.ToString();
            string p = player <= 0 ? "-" : player.ToString();
            _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} T{t} P{p} [{channel}] {text}");
        }
        catch
        {
            // Never throw into game code.
        }
    }

    private static int ParsePlayer(string? actor)
    {
        if (string.IsNullOrEmpty(actor) || actor.Length < 2) return 0;
        char prefix = actor[0];
        if ((prefix == 'P' || prefix == 'S' || prefix == 'p' || prefix == 's')
            && char.IsDigit(actor[1]))
            return actor[1] - '0';
        return 0;
    }
}