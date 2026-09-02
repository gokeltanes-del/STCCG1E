using StarTrekCCG.Models;
using System;
using System.Collections.Generic;

namespace StarTrekCCG;

/// <summary>
/// Spielzustand und Zugstruktur – UI-unabhängig.
/// Compendium Kap. 5/6: Play → Execute → Draw.
/// Copy this whole file over the VS project GameSession.cs (PointsToWin, CheckVictory, OncePerGame).
/// </summary>
public sealed class GameSession
{
    public enum GameMode
    {
        Hotseat,
        Network,   // TODO
        SingleAi   // TODO
    }

    public enum MatchPhase
    {
        Setup,
        Seed,
        Play,
        Ended
    }

    /// <summary>
    /// 1) Play – optionale normal card play (eine Karte aus der Hand)
    /// 2) Execute – Orders
    /// 3) Draw – Zugende, dann Gegner
    /// </summary>
    public enum TurnSegment
    {
        Play,
        Execute,
        Draw
    }

    public GameMode Mode { get; set; } = GameMode.Hotseat;
    public MatchPhase Match { get; set; } = MatchPhase.Setup;
    public int ActivePlayer { get; set; } = 1;
    public int TurnNumber { get; set; } = 1;
    public TurnSegment Segment { get; set; } = TurnSegment.Play;

    public bool HasDrawnThisTurn { get; set; }
    public bool SuppressEndOfTurnDraw { get; set; }

    /// <summary>Compendium: höchstens eine „normal card play“ pro Zug.</summary>
    public bool NormalCardPlayUsed { get; private set; }

    /// <summary>True, wenn Execute begann, ohne die Card Play zu nutzen.</summary>
    public bool NormalCardPlayForfeited { get; private set; }

    public bool NormalCardPlayAvailable =>
        Match == MatchPhase.Play
        && Segment == TurnSegment.Play
        && !NormalCardPlayUsed
        && !NormalCardPlayForfeited;

    /// <summary>Compendium default; some cards change the race.</summary>
    public int PointsToWin { get; set; } = 100;

    /// <summary>"Once per game" keys, e.g. "P1|SpecialDownload|Miles O'Brien".</summary>
    public HashSet<string> OncePerGame { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cleared at each EndTurn. Keys like "P1|RedAlert".</summary>
    public HashSet<string> OncePerTurn { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Effects that expire at end of the current turn (card instance ids or names).</summary>
    public HashSet<string> UntilEndOfTurn { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Structured until-EOT effects (Transwarp, temporary RANGE, …).</summary>
    public List<ExpiringEffect> Expiring { get; } = new();

    public int? Winner { get; private set; }

    public ActionLog Log { get; } = new();

    public bool TryMarkOncePerGame(int player, string key)
    {
        string k = $"P{player}|{key}";
        return OncePerGame.Add(k);
    }

    public bool HasOncePerGame(int player, string key) =>
        OncePerGame.Contains($"P{player}|{key}");

    public bool TryMarkOncePerTurn(int player, string key)
    {
        string k = $"P{player}|{key}";
        return OncePerTurn.Add(k);
    }

    public int? CheckVictory(int scoreP1, int scoreP2)
    {
        if (Winner is > 0) return Winner;
        if (scoreP1 >= PointsToWin && scoreP1 > scoreP2) Winner = 1;
        else if (scoreP2 >= PointsToWin && scoreP2 > scoreP1) Winner = 2;
        if (Winner is > 0)
        {
            Match = MatchPhase.Ended;
            Log.Add(TurnNumber, "System", $"P{Winner} wins ({scoreP1}–{scoreP2}, target {PointsToWin}).");
        }
        return Winner;
    }

    public void StartSeed()
    {
        Match = MatchPhase.Seed;
        ActivePlayer = 1;
        TurnNumber = 0;
        Segment = TurnSegment.Play;
        Winner = null;
        OncePerGame.Clear();
        OncePerTurn.Clear();
        UntilEndOfTurn.Clear();
        Expiring.Clear();
        ResetTurnFlags();
        Log.Add(0, "System", "Seed-Phase gestartet");
    }

    public void StartPlayFromSeed()
    {
        Match = MatchPhase.Play;
        ActivePlayer = 1;
        TurnNumber = 1;
        Segment = TurnSegment.Play;
        ResetTurnFlags();
        Log.Add(1, "System", "Seed beendet – Zug 1 S1: optional normal card play, dann Execute");
    }

    private void ResetTurnFlags()
    {
        HasDrawnThisTurn = false;
        SuppressEndOfTurnDraw = false;
        NormalCardPlayUsed = false;
        NormalCardPlayForfeited = false;
    }

    /// <summary>Play → Execute. Ungenutzte Card Play verfällt.</summary>
    public void AdvanceSegment()
    {
        if (Match != MatchPhase.Play) return;

        switch (Segment)
        {
            case TurnSegment.Play:
                if (!NormalCardPlayUsed)
                {
                    NormalCardPlayForfeited = true;
                    Log.Add(TurnNumber, $"S{ActivePlayer}",
                        "Normal card play verfallen (Execute begonnen)");
                }
                Segment = TurnSegment.Execute;
                Log.Add(TurnNumber, $"S{ActivePlayer}", "Segment → Execute (Orders)");
                break;
            case TurnSegment.Execute:
                Segment = TurnSegment.Draw;
                Log.Add(TurnNumber, $"S{ActivePlayer}", "Segment → Draw (Zugende)");
                break;
            case TurnSegment.Draw:
                EndTurn();
                break;
        }
    }

    public void EndTurn()
    {
        if (Match != MatchPhase.Play) return;

        Log.Add(TurnNumber, $"S{ActivePlayer}", "Zug beendet");

        if (ActivePlayer == 1)
            ActivePlayer = 2;
        else
        {
            ActivePlayer = 1;
            TurnNumber++;
        }

        Segment = TurnSegment.Play;
        ResetTurnFlags();
        OncePerTurn.Clear();
        // Until-EOT bag is drained in ProcessUntilEndOfTurnBag for the finishing player only.
        // Do not clear Expiring / UntilEndOfTurn here — other player's effects must survive.
        Log.Add(TurnNumber, $"S{ActivePlayer}", "Zugbeginn – Play (optional 1 Karte aus der Hand)");
    }

    public void MarkDrawn()
    {
        HasDrawnThisTurn = true;
        Log.Add(TurnNumber, $"S{ActivePlayer}", "Karte gezogen");
    }

    public void MarkNormalCardPlay(string cardName)
    {
        NormalCardPlayUsed = true;
        Log.Add(TurnNumber, $"S{ActivePlayer}", $"Normal card play: {cardName}");
    }

    /// <summary>Used by save/load — does not write a log line.</summary>
    public void Restore(
        MatchPhase match, TurnSegment segment, int activePlayer, int turn,
        bool drawn, bool suppressDraw, bool playUsed, bool playForfeit)
    {
        Match = match;
        Segment = segment;
        ActivePlayer = activePlayer;
        TurnNumber = turn;
        HasDrawnThisTurn = drawn;
        SuppressEndOfTurnDraw = suppressDraw;
        NormalCardPlayUsed = playUsed;
        NormalCardPlayForfeited = playForfeit;
    }

    public string SegmentLabel() => Segment switch
    {
        TurnSegment.Play => "Play",
        TurnSegment.Execute => "Execute",
        TurnSegment.Draw => "Draw",
        _ => "?"
    };

    public string StatusLine()
    {
        if (Match == MatchPhase.Seed)
            return "Seed-Phase";
        if (Match != MatchPhase.Play)
            return "–";

        string extra = Segment == TurnSegment.Play
            ? (NormalCardPlayUsed
                ? " · Card Play ✓"
                : " · 1× Card Play möglich")
            : Segment == TurnSegment.Execute
                ? (NormalCardPlayUsed ? "" : " · Card Play verfallen")
                : "";

        return $"Zug {TurnNumber} · S{ActivePlayer} · {SegmentLabel()}{extra}";
    }

    /// <summary>
    /// Compendium: Interrupt &amp; Doorway brauchen keine normal card play („at any time“).
    /// Personnel/Ship/Equipment reporten – zählen aber als die eine Card Play, wenn aus der Hand.
    /// </summary>
    public static bool UsesNormalCardPlay(Card card) => CardKinds.UsesNormalCardPlay(card);

    public static bool MustReportForDuty(Card card) => CardKinds.MustReportForDuty(card);
}

/// <summary>
/// Rules engines write comparison traces here; TableWindow wires it to ActionLog.AddDebug.
/// </summary>
public static class CheckTrace
{
    public static Action<string>? Emit { get; set; }

    public static void Line(string text) => Emit?.Invoke(text);

    public static void Cmp(string kind, string need, string have, bool ok)
        => Emit?.Invoke($"{kind}: need '{need}'  vs  have '{have}'  → {(ok ? "MATCH" : "NO MATCH")}");
}

public sealed class ActionLog
{
    public sealed record Entry(DateTime Utc, int Turn, string Actor, string Text, bool Debug = false);

    private readonly List<Entry> _entries = new();
    public IReadOnlyList<Entry> Entries => _entries;

    /// <summary>UI can subscribe (refresh Action History).</summary>
    public Action? Changed { get; set; }

    /// <summary>Player-facing and always-on log line (actor e.g. P1, System).</summary>
    public void Add(int turn, string actor, string text)
    {
        AddCore(turn, NormalizeActor(actor), text, debug: false);
    }

    /// <summary>Developer/debug detail — prefix Debug: in UI; filterable.</summary>
    public void AddDebug(int turn, string actor, string text)
    {
        AddCore(turn, NormalizeActor(actor), text, debug: true);
    }

    private void AddCore(int turn, string actor, string text, bool debug)
    {
        _entries.Add(new Entry(DateTime.UtcNow, turn, actor, text, debug));
        if (_entries.Count > 2500)
            _entries.RemoveRange(0, _entries.Count - 2500);
        try { DebugLog.FromActionLog(turn, actor, text, debug); } catch { }
        Changed?.Invoke();
    }

    private static string NormalizeActor(string actor)
    {
        if (string.IsNullOrEmpty(actor)) return actor;
        // Legacy German "S1"/"S2" → English "P1"/"P2"
        if (actor.Equals("S1", StringComparison.OrdinalIgnoreCase)) return "P1";
        if (actor.Equals("S2", StringComparison.OrdinalIgnoreCase)) return "P2";
        if (actor.StartsWith("S", StringComparison.Ordinal) && actor.Length >= 2
            && char.IsDigit(actor[1]))
            return "P" + actor[1..];
        return actor;
    }

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }

    public void Restore(IEnumerable<Entry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);
        Changed?.Invoke();
    }

    public IEnumerable<string> FormatLines(int max = 80, bool includeDebug = true)
    {
        IEnumerable<Entry> src = includeDebug ? _entries : _entries.Where(e => !e.Debug);
        var list = src.ToList();
        var slice = list.Count <= max ? list : list.Skip(list.Count - max);
        foreach (var e in slice)
        {
            string t = e.Turn <= 0 ? "-" : e.Turn.ToString();
            string time = e.Utc.ToLocalTime().ToString("HH:mm:ss");
            string prefix = e.Debug ? "Debug: " : "";
            yield return $"[{time}] T{t} {e.Actor}: {prefix}{e.Text}";
        }
    }
}