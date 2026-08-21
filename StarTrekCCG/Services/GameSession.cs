using StarTrekCCG.Models;
using System;
using System.Collections.Generic;

namespace StarTrekCCG;

/// <summary>
/// Spielzustand und Zugstruktur – UI-unabhängig.
/// Compendium Kap. 5/6: Play (normal card play) → Execute orders → Draw (Zugende).
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

    public ActionLog Log { get; } = new();

    public void StartSeed()
    {
        Match = MatchPhase.Seed;
        ActivePlayer = 1;
        TurnNumber = 0;
        Segment = TurnSegment.Play;
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
    public static bool UsesNormalCardPlay(Card card)
    {
        string t = (card.Type ?? "").ToLowerInvariant();
        if (t.Contains("interrupt")) return false;
        if (t.Contains("doorway")) return false;
        return true;
    }

    public static bool MustReportForDuty(Card card)
    {
        string t = (card.Type ?? "").ToLowerInvariant();
        return t.Contains("personnel")
               || t.Contains("ship")
               || t.Contains("equipment")
               || t.Contains("android")
               || t.Contains("animal");
    }
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
        if (_entries.Count > 800)
            _entries.RemoveRange(0, _entries.Count - 800);
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