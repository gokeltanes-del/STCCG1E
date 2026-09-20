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
        /// <summary>Max cards this download may take (Gift Box = 3).</summary>
        public int MaxCount { get; init; } = 1;
        /// <summary>Printed Gift Box / similar: opponent prevent-download does not apply.</summary>
        public bool IgnoreOpponentDownloadPrevention { get; init; }
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

    /// <summary>Betazoid Gift Box acquire: up to 3 from own draw to hand; ignore opp prevent.</summary>
    public static Request GiftBoxAcquireRequest(int player, Card? sourceArtifact = null) =>
        new Request
        {
            Player = player,
            Source = Source.DrawDeck,
            Dest = Dest.Hand,
            MaxCount = 3,
            IgnoreOpponentDownloadPrevention = true,
            SourceCard = sourceArtifact
        };

    /// <summary>
    /// When opponent has an active prevent-downloading effect, normal downloads fail
    /// unless the request ignores opponent prevention (Gift Box).
    /// </summary>
    public static bool MayDownloadDespiteOpponentPrevention(Request req, bool opponentPreventActive) =>
        !opponentPreventActive || req.IgnoreOpponentDownloadPrevention;

    public static int ClampDownloadCount(Request req, int availableInSource) =>
        System.Math.Max(0, System.Math.Min(req.MaxCount <= 0 ? 1 : req.MaxCount, availableInSource));

    /// <summary>DE mini-test Gift Box download request. Returns null if OK.</summary>
    public static string? VerifyBetazoidGiftBoxDownload()
    {
        var req = GiftBoxAcquireRequest(1);
        if (req.Source != Source.DrawDeck || req.Dest != Dest.Hand)
            return "Gift Box source/dest wrong";
        if (req.MaxCount != 3)
            return "Gift Box MaxCount must be 3";
        if (!req.IgnoreOpponentDownloadPrevention)
            return "Gift Box must ignore opp prevent";
        if (!MayDownloadDespiteOpponentPrevention(req, opponentPreventActive: true))
            return "IgnoreOppPrevent must allow download when opp prevent active";
        if (MayDownloadDespiteOpponentPrevention(
                new Request { Player = 1, Source = Source.DrawDeck },
                opponentPreventActive: true))
            return "Normal download must be blocked when opp prevent active";
        if (ClampDownloadCount(req, 10) != 3)
            return "Clamp max 3";
        if (ClampDownloadCount(req, 2) != 2)
            return "Clamp to available";
        if (ClampDownloadCount(req, 0) != 0)
            return "Clamp empty deck";
        return null;
    }

}
