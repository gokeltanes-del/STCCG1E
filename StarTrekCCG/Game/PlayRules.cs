using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium 6.1.1 Playing for free + 6.2 Entering Play (Premiere-Kern).
/// UI-unabhängig; TableWindow ruft nur die Checks auf.
/// </summary>
public static class PlayRules
{
    public enum UniquenessKind
    {
        /// <summary>❖ universal – beliebig viele Kopien.</summary>
        Universal,
        /// <summary>Unique (Standard für Ship/Personnel/Facility) – max. 1 eigene Kopie im Spiel.</summary>
        Unique,
        /// <summary>Not duplicatable – max. 1 Kopie im gesamten Spiel (alle Spieler).</summary>
        NotDuplicatable,
        /// <summary>✶ Enigma – wie Unique für Besitz, aber kein „unique“-Target.</summary>
        Enigma
    }

    public readonly record struct EnterPlayResult(bool Ok, string Reason, bool PlaysForFree);

    /// <summary>
    /// 6.1.1: Karte spielt „for free“ (eigener Text oder später: erlaubende Karte).
    /// Spielt normal, zählt aber nicht als normal card play; unbegrenzt vor Execute.
    /// </summary>
    public static bool PlaysForFree(Card card)
    {
        string text = (card.Text ?? "").ToLowerInvariant();
        string name = (card.Name ?? "").ToLowerInvariant();
        // Typische Premiere-/1E-Formulierungen
        if (text.Contains("plays for free") || text.Contains("play for free"))
            return true;
        if (text.Contains("reports for free") || text.Contains("report for free"))
            return true;
        // Manche Karten: "for free" am Anfang der Spielanweisung
        if (text.StartsWith("for free") || text.Contains(" for free."))
            return true;
        return false;
    }

    /// <summary>
    /// Compendium 6.2 Defaults:
    /// Ships, Personnel, Facilities → unique unless marked universal/enigma.
    /// Missions / Time Locations → not duplicatable (default).
    /// uniqueness JSON: "univ", "enig", leer, oder Symbole.
    /// </summary>
    public static UniquenessKind GetUniqueness(Card card)
    {
        string u = (card.Uniqueness ?? "").Trim().ToLowerInvariant();
        string type = (card.Type ?? "").ToLowerInvariant();

        if (u is "univ" or "universal" or "❖" or "*")
            return UniquenessKind.Universal;
        if (u is "enig" or "enigma" or "✶")
            return UniquenessKind.Enigma;
        if (u.Contains("not duplicat") || u is "nd" or "non-duplicatable" or "nonduplicatable")
            return UniquenessKind.NotDuplicatable;

        if (type.Contains("mission") || type.Contains("time location"))
            return UniquenessKind.NotDuplicatable;

        // Default unique for these types
        if (type.Contains("ship") || type.Contains("personnel") || type.Contains("facility")
            || type.Contains("outpost") || type.Contains("headquarters") || type.Contains("station")
            || type.Contains("android") || type.Contains("animal"))
            return UniquenessKind.Unique;

        // Events etc. – multiple copies usually OK unless marked
        if (string.IsNullOrEmpty(u))
            return UniquenessKind.Universal;

        return UniquenessKind.Unique;
    }

    /// <summary>
    /// Persona-Name grob: Kartenname ohne Suffixe (später verfeinern).
    /// </summary>
    public static string PersonaKey(Card card)
    {
        string n = (card.Name ?? "").Trim();
        // Einfache Normalisierung
        return n.ToLowerInvariant();
    }

    /// <summary>
    /// Darf diese Karte ins Spiel kommen, gegeben alle bereits im Spiel befindlichen Karten?
    /// controlledInPlay = Karten unter Control des Spielers (Unique/Enigma); allInPlay = beide.
    /// E4: Match per PersonaKey + InstanceId (nicht nur Namens-String / ReferenceEquals).
    /// </summary>
    public static EnterPlayResult CanEnterPlay(
        Card card,
        IEnumerable<Card> controlledInPlay,
        IEnumerable<Card> allInPlay,
        int player = 0)
    {
        bool free = PlaysForFree(card);
        var kind = GetUniqueness(card);
        string key = PersonaKey(card);

        switch (kind)
        {
            case UniquenessKind.Universal:
                return new EnterPlayResult(true, "", free);

            case UniquenessKind.Unique:
            case UniquenessKind.Enigma:
                var ownedConflict = FindPersonaConflict(card, controlledInPlay, key);
                if (ownedConflict != null)
                {
                    LogUniqueDeny(card, ownedConflict, player, kind);
                    return new EnterPlayResult(false,
                        $"„{card.Name}“ ist {(kind == UniquenessKind.Enigma ? "Enigma" : "unique")} - du hast bereits eine Kopie im Spiel.",
                        free);
                }
                return new EnterPlayResult(true, "", free);

            case UniquenessKind.NotDuplicatable:
                var anyConflict = FindPersonaConflict(card, allInPlay, key);
                if (anyConflict != null)
                {
                    LogUniqueDeny(card, anyConflict, player, kind);
                    return new EnterPlayResult(false,
                        $"„{card.Name}“ ist not duplicatable - es liegt bereits eine Kopie im Spiel.",
                        free);
                }
                return new EnterPlayResult(true, "", free);

            default:
                return new EnterPlayResult(true, "", free);
        }
    }

    /// <summary>
    /// E4: Unique/persona against <see cref="BoardStore.InPlay"/> by Controller.
    /// Falls back to empty lists when the store has no spaceline/TABLE surface yet.
    /// </summary>
    public static EnterPlayResult CanEnterPlay(Card card, int player, BoardStore? store = null)
    {
        store ??= BoardStore.Current;
        if (!store.HasInPlaySurface)
            return CanEnterPlay(card, Array.Empty<Card>(), Array.Empty<Card>(), player);

        var controlled = store.InPlay(player, BoardStore.InPlaySide.Controller).ToList();
        var all = store.InPlay().ToList();
        return CanEnterPlay(card, controlled, all, player);
    }

    /// <summary>
    /// Same persona in play, excluding the card about to enter (InstanceId) and ReferenceEquals.
    /// </summary>
    public static Card? FindPersonaConflict(Card card, IEnumerable<Card> inPlay, string? personaKey = null)
    {
        string key = personaKey ?? PersonaKey(card);
        foreach (var other in inPlay)
        {
            if (ReferenceEquals(other, card)) continue;
            if (card.InstanceId > 0 && other.InstanceId == card.InstanceId) continue;
            if (!string.Equals(PersonaKey(other), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return other;
        }
        return null;
    }

    private static void LogUniqueDeny(Card attempting, Card have, int player, UniquenessKind kind)
    {
        int ctrl = have.Controller != 0 ? have.Controller : (have.OwnerPlayer != 0 ? have.OwnerPlayer : player);
        int who = player != 0 ? player : ctrl;
        string label = kind == UniquenessKind.NotDuplicatable ? "not-dup deny" : "unique deny";
        string haveBit = have.InstanceId > 0 ? $"#{have.InstanceId}" : DebugLog.Card(have);
        DebugLog.Play(0, who, $"{label} {DebugLog.Card(attempting)} have={haveBit} controller={ctrl}");
    }
}