using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 6.5.3–6.5.4. Verb every later set reuses.
/// </summary>
public static class DownloadRules
{
    public enum Source
    {
        DrawDeck,
        Discard,
        QsTent,
        QContinuum,
        Hand,
        Play
    }

    public enum Dest
    {
        Hand,
        Report,
        TableCore,
        InPlaceOfDraw
    }

    public sealed class Request
    {
        public int Player { get; init; }
        public Source Source { get; init; }
        public Dest Dest { get; init; } = Dest.Hand;
        public string? NameEquals { get; init; }
        public CardKind? Kind { get; init; }
        public bool SpecialDownload { get; init; }
        public Card? SourceCard { get; init; }
    }

    public static string SpecialDownloadKey(Card sourceCard) =>
        $"SD|{sourceCard.InstanceId}|{sourceCard.Name}";

    public static (bool ok, string reason) CanSpecialDownload(GameSession session, int player, Card sourceCard)
    {
        if (!CardIcons.HasSpecialDownload(sourceCard))
            return (false, "No Special Download icon / text on that card.");
        string key = SpecialDownloadKey(sourceCard);
        if (session.HasOncePerGame(player, key))
            return (false, "Special Download already used on that card.");
        return (true, "Special Download available.");
    }

    public static void MarkSpecialDownload(GameSession session, int player, Card sourceCard) =>
        session.TryMarkOncePerGame(player, SpecialDownloadKey(sourceCard));

    public static (bool ok, string reason) CanTentDownload(GameSession session, int player, bool tentOpen, int tentCount)
    {
        if (!tentOpen)
            return (false, "Q's Tent is closed (seed the doorway as a cover first).");
        if (tentCount <= 0)
            return (false, "Q's Tent is empty.");
        if (TentDownloadUsedThisTurn(session, player))
            return (false, "Q's Tent download already used this turn.");
        return (true, "May take one card from Q's Tent.");
    }

    public static bool TentDownloadUsedThisTurn(GameSession session, int player) =>
        session.OncePerTurn.Contains($"P{player}|TentDownload");

    public static void MarkTentDownload(GameSession session, int player) =>
        session.TryMarkOncePerTurn(player, "TentDownload");

    /// <summary>
    /// Printed "Special Download X" / "Special Download: X." → card name to look up.
    /// Empty means the player picks from the legal source (draw / tent).
    /// </summary>
    public static string? ParseSpecialDownloadName(Card card)
    {
        string text = card.Text ?? "";
        var m = Regex.Match(text,
            @"Special Download[:\s]+(?:any\s+)?(.+?)(?:\.|$)",
            RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        string name = m.Groups[1].Value.Trim();
        name = Regex.Replace(name, @"\s+", " ");
        if (name.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
            name = name[2..].Trim();
        if (name.Equals("equipment", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ship", StringComparison.OrdinalIgnoreCase)
            || name.Equals("personnel", StringComparison.OrdinalIgnoreCase))
            return null;
        int cut = name.IndexOfAny(['(', '[']);
        if (cut > 0) name = name[..cut].Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static List<Card> FilterSource(IEnumerable<Card> pile, Request req)
    {
        var list = new List<Card>();
        foreach (var c in pile)
        {
            if (req.NameEquals != null
                && !string.Equals(c.Name, req.NameEquals, StringComparison.OrdinalIgnoreCase))
                continue;
            if (req.Kind is CardKind k && CardKinds.Of(c) != k)
                continue;
            list.Add(c);
        }
        return list;
    }
}