using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Printed icon tokens from Lackey JSON (`[AU][HA][Cmd]` …).
/// Rules attach to tokens, not to expansion names.
/// </summary>
public readonly struct ParsedIcons
{
    public IReadOnlyList<string> Tokens { get; init; }
    public bool AlternateUniverse { get; init; }
    public bool HiddenAgenda { get; init; }
    public bool Hologram { get; init; }
    public bool SelfControlling { get; init; }
    public bool Shield { get; init; }
    public bool CommandStaffing { get; init; }
    public bool StaffStaffing { get; init; }
    public bool BorgCommand { get; init; }
    public bool BorgDefense { get; init; }
    public bool BorgNavigation { get; init; }
    public bool DeltaQuadrant { get; init; }
    public bool GammaQuadrant { get; init; }
    public bool MirrorUniverse { get; init; }
    public bool BattleBridge { get; init; }
    /// <summary>Interphase Generator icon on dilemmas ([IPG]).</summary>
    public bool InterphaseGenerator { get; init; }
    public int? PrintedCountdown { get; init; }

    public bool Has(string token)
    {
        foreach (var t in Tokens)
            if (t.Equals(token, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}

public static class CardIcons
{
    private static readonly Regex TokenRx = new(@"\[([^\]]+)\]", RegexOptions.Compiled);

    public static ParsedIcons Parse(Card? card)
    {
        string raw = (card?.Icons ?? "") + (card?.Staff ?? "") + " " + (card?.Characteristics ?? "");
        var tokens = new List<string>();
        foreach (Match m in TokenRx.Matches(raw))
            tokens.Add(m.Groups[1].Value.Trim());

        int? countdown = null;
        foreach (var t in tokens)
        {
            if (int.TryParse(t, out int n) && n is >= 1 and <= 9)
            {
                countdown = n;
                break;
            }
        }

        bool HasTok(params string[] names)
        {
            foreach (var tok in tokens)
                foreach (var n in names)
                    if (tok.Equals(n, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        return new ParsedIcons
        {
            Tokens = tokens,
            AlternateUniverse = HasTok("AU"),
            HiddenAgenda = HasTok("HA"),
            Hologram = HasTok("Holo", "Hol"),
            SelfControlling = HasTok("Self"),
            Shield = HasTok("Shield", "SHD"),
            CommandStaffing = HasTok("Cmd", "Com"),
            StaffStaffing = HasTok("Stf", "Staff"),
            BorgCommand = HasTok("Com"),
            BorgDefense = HasTok("Def"),
            BorgNavigation = HasTok("Nav"),
            DeltaQuadrant = HasTok("DQ"),
            GammaQuadrant = HasTok("GQ"),
            MirrorUniverse = HasTok("MU", "MQ"),
            BattleBridge = HasTok("BB"),
            InterphaseGenerator = HasTok("IPG"),
            PrintedCountdown = countdown
        };
    }

    public static bool HasSpecialDownload(Card? card)
    {
        if (card == null) return false;
        string blob = (card.Icons ?? "") + " " + (card.Characteristics ?? "") + " " + (card.Text ?? "");
        if (blob.Contains("[SD]", StringComparison.OrdinalIgnoreCase)) return true;
        if (blob.Contains("Special Download", StringComparison.OrdinalIgnoreCase)) return true;
        var p = Parse(card);
        return p.Has("SD") || p.Has("Special Download");
    }

    public static bool HasHiddenAgenda(Card card) => Parse(card).HiddenAgenda;
    public static bool HasAlternateUniverse(Card card) => Parse(card).AlternateUniverse;
    public static bool IsHologram(Card card) => Parse(card).Hologram;
    public static bool IsSelfControlling(Card card) => Parse(card).SelfControlling;

    /// <summary>Dilemma (or other card) printed with [IPG] — nullified where Interphase Generator is present.</summary>
    public static bool HasIpg(Card? card) => card != null && Parse(card).InterphaseGenerator;

    /// <summary>[ETA] token or printed countdown numeral ([1]…[9]) — Armbands / countdown dilemmas.</summary>
    public static bool HasEtaDilemma(Card? card)
    {
        if (card == null) return false;
        var p = Parse(card);
        if (p.PrintedCountdown is >= 1 and <= 9) return true;
        return p.Has("ETA");
    }

    /// <summary>Same as HasIpg; name mirrors rules wording "[IPG] dilemmas".</summary>
    public static bool IsIpgDilemma(Card? card) =>
        card != null && CardKinds.IsDilemma(card) && HasIpg(card);

}