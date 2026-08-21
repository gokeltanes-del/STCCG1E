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
    /// ownedInPlay = Karten des Besitzers; allInPlay = beide Spieler.
    /// </summary>
    public static EnterPlayResult CanEnterPlay(
        Card card,
        IEnumerable<Card> ownedInPlay,
        IEnumerable<Card> allInPlay)
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
                bool ownedCopy = ownedInPlay.Any(c =>
                    string.Equals(PersonaKey(c), key, StringComparison.OrdinalIgnoreCase));
                if (ownedCopy)
                {
                    return new EnterPlayResult(false,
                        $"„{card.Name}“ ist {(kind == UniquenessKind.Enigma ? "Enigma" : "unique")} – du hast bereits eine Kopie im Spiel.",
                        free);
                }
                return new EnterPlayResult(true, "", free);

            case UniquenessKind.NotDuplicatable:
                bool anyCopy = allInPlay.Any(c =>
                    string.Equals(PersonaKey(c), key, StringComparison.OrdinalIgnoreCase));
                if (anyCopy)
                {
                    return new EnterPlayResult(false,
                        $"„{card.Name}“ ist not duplicatable – es liegt bereits eine Kopie im Spiel.",
                        free);
                }
                return new EnterPlayResult(true, "", free);

            default:
                return new EnterPlayResult(true, "", free);
        }
    }
}