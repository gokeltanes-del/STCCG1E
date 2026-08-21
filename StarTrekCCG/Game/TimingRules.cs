using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium: Actions + valid responses + Stack (Premiere-Kern).
/// Katalog: Amanda Rogers, Kevin Uxbridge, Q2, Energy Vortex, The Devil,
/// Hugh, Asteroid Sanctuary.
/// </summary>
public static class TimingRules
{
    public enum ActionKind
    {
        PlayCard,
        InitiateShipBattle,
        InitiatePersonnelBattle
    }

    public enum Destination
    {
        Discard,
        OutOfPlay,
        ReturnToHand,
        Table          // Event/Doorway/Treaty bleibt
    }

    public sealed class PendingAction
    {
        public ActionKind Kind { get; init; }
        public int Controller { get; init; }
        public Card? Card { get; init; }
        public string Summary { get; init; } = "";
        public bool IsResponse { get; init; }
        public bool Cancelled { get; set; }
        public string? CancelledBy { get; set; }

        // Ship battle
        public object? AttackerHost { get; set; }
        public Card? AttackerCard { get; set; }
        public object? DefenderHost { get; set; }
        public Card? DefenderCard { get; set; }

        // Personnel battle
        public List<object>? AttackerTeam { get; set; }
        public List<object>? DefenderTeam { get; set; }
        public List<Card>? AttackerPresent { get; set; }
        public List<Card>? DefenderPresent { get; set; }
        public int DefenderOwner { get; set; }

        /// <summary>In-play card this action is targeting (e.g. Kevin on an Event already in play).</summary>
        public Card? TargetCard { get; set; }
    }

    public sealed class ActionStack
    {
        public readonly List<PendingAction> Items = new();
        /// <summary>Spieler, der als Nächstes eine Response spielen oder passen darf.</summary>
        public int ResponsePlayer { get; set; }
        /// <summary>Wie oft hintereinander gepasst wurde (2 = beide, dann Resolve).</summary>
        public int ConsecutivePasses { get; set; }
        public bool IsOpen => Items.Count > 0;

        public PendingAction? Top => Items.Count == 0 ? null : Items[^1];

        public void Push(PendingAction a)
        {
            Items.Add(a);
            ConsecutivePasses = 0;
        }

        public PendingAction Pop()
        {
            var a = Items[^1];
            Items.RemoveAt(Items.Count - 1);
            return a;
        }

        public void Clear()
        {
            Items.Clear();
            ConsecutivePasses = 0;
        }
    }

    public static bool IsInterrupt(Card c) =>
        (c.Type ?? "").Contains("interrupt", StringComparison.OrdinalIgnoreCase);

    public static bool IsEvent(Card c) =>
        (c.Type ?? "").Contains("event", StringComparison.OrdinalIgnoreCase);

    public static bool IsDoorway(Card c) =>
        (c.Type ?? "").Contains("doorway", StringComparison.OrdinalIgnoreCase);

    public static bool IsAnytimeType(Card c) => IsInterrupt(c) || IsDoorway(c);

    /// <summary>
    /// Premiere: Kevin may nullify an Event just played OR already in play,
    /// except treaties, [SHD] events, and Static Warp Bubble.
    /// </summary>
    public static (bool ok, string reason) CanKevinTargetEvent(Card ev)
    {
        if (!IsEvent(ev))
            return (false, "Kevin Uxbridge nullifies only an Event.");
        if (HasShieldIcon(ev))
            return (false, "Kevin Uxbridge: that Event has a Shield icon.");
        if (TreatyRules.IsTreatyCard(ev))
            return (false, "Kevin Uxbridge: treaties are immune.");
        string n = (ev.Name ?? "").Trim();
        if (n.Equals("Static Warp Bubble", StringComparison.OrdinalIgnoreCase))
            return (false, "Static Warp Bubble is immune to Kevin Uxbridge.");
        return (true, "Nullify that Event.");
    }

    public static bool HasShieldIcon(Card c)
    {
        string icons = (c.Icons ?? "") + " " + (c.Characteristics ?? "");
        return icons.Contains("shield", StringComparison.OrdinalIgnoreCase)
               || icons.Contains("[SHD]", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCatalogResponse(Card c)
    {
        string n = (c.Name ?? "").Trim();
        return n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Q2", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase)
            || n.Equals("The Devil", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Hugh", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Asteroid Sanctuary", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Darf <paramref name="response"/> als valid response auf <paramref name="top"/> gespielt werden?
    /// </summary>
    public static (bool ok, string reason) CanRespond(
        Card response,
        PendingAction top,
        int responseOwner)
    {
        string n = (response.Name ?? "").Trim();

        if (n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Amanda Rogers: kein Interrupt auf dem Stack.");
            if (!IsInterrupt(top.Card))
                return (false, "Amanda Rogers nullifiziert nur einen Interrupt.");
            if (HasShieldIcon(top.Card))
                return (false, "Amanda Rogers: Shield-Interrupts sind immun.");
            return (true, "Nullifiziert den Interrupt.");
        }

        if (n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Kevin Uxbridge: kein Event auf dem Stack.");
            return CanKevinTargetEvent(top.Card);
        }

        if (n.Equals("Q2", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Q2: kein gültiges Ziel auf dem Stack.");
            string tn = (top.Card.Name ?? "").Trim();
            bool amanda = tn.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase);
            bool kevin = tn.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase);
            bool qDil = (top.Card.Type ?? "").Contains("dilemma", StringComparison.OrdinalIgnoreCase)
                        && ((top.Card.Name ?? "").Contains("Q", StringComparison.OrdinalIgnoreCase)
                            || (top.Card.Icons ?? "").Contains("Q", StringComparison.OrdinalIgnoreCase));
            if (!amanda && !kevin && !qDil)
                return (false, "Q2 nullifiziert nur Amanda Rogers, Kevin Uxbridge oder ein Q-Dilemma.");
            return (true, "Nullifiziert " + tn + ".");
        }

        if (n.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Energy Vortex: keine Card Play auf dem Stack.");
            if (top.Controller == responseOwner)
                return (false, "Energy Vortex nur gegen die Card Play des Gegners.");
            if (!GameSession.UsesNormalCardPlay(top.Card))
                return (false, "Energy Vortex nur gegen eine normal card play (kein Interrupt/Doorway).");
            return (true, "Bricht die Card Play; Karte zurück auf die Hand.");
        }

        if (n.Equals("The Devil", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "The Devil: kein Treaty auf dem Stack.");
            if (!TreatyRules.IsTreatyCard(top.Card)
                && !(top.Card.Name ?? "").Contains("Treaty", StringComparison.OrdinalIgnoreCase)
                && !(top.Card.Name ?? "").Contains("Horga'hn", StringComparison.OrdinalIgnoreCase)
                && !(top.Card.Name ?? "").Contains("Wind Dancer", StringComparison.OrdinalIgnoreCase))
                return (false, "The Devil nullifiziert Horga'hn, Wind Dancer oder ein Treaty.");
            return (true, "Nullifiziert " + (top.Card.Name ?? "Ziel") + ".");
        }

        if (n.Equals("Hugh", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind is not (ActionKind.InitiateShipBattle or ActionKind.InitiatePersonnelBattle))
                return (false, "Hugh: keine Battle-Initiation auf dem Stack.");
            bool borg = top.AttackerCard != null
                        && ReportingRules.GetAffiliations(top.AttackerCard).Contains("BORG");
            string tn = top.AttackerCard?.Name ?? "";
            if (!borg && !tn.Contains("Borg Ship", StringComparison.OrdinalIgnoreCase))
                return (false, "Hugh: Battle muss von einer Borg-Karte / Borg Ship ausgehen.");
            return (true, "Bricht die Borg-Battle.");
        }

        if (n.Equals("Asteroid Sanctuary", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.InitiateShipBattle)
                return (false, "Asteroid Sanctuary: keine Ship-Battle auf dem Stack.");
            if (top.DefenderCard == null)
                return (false, "Kein Verteidiger-Schiff.");
            if (top.DefenderOwner != 0 && top.DefenderOwner != responseOwner
                && GetOwnerGuess(top) != responseOwner)
            {
                // DefenderOwner kann 0 sein – dann erlauben wir, wenn der Spieler der Verteidiger ist
            }
            return (true, "Kann die Battle gegen dein Schiff canceln (2 Navigation an Bord beim Resolve).");
        }

        return (false, $"„{n}“ ist in diesem Fenster keine gültige Response.");
    }

    private static int GetOwnerGuess(PendingAction top) => top.DefenderOwner;

    /// <summary>Wohin geht die Response-Karte selbst nach erfolgreichem Resolve?</summary>
    public static Destination SelfDestination(Card response)
    {
        string n = (response.Name ?? "").Trim();
        if (n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Q2", StringComparison.OrdinalIgnoreCase)
            || n.Equals("The Devil", StringComparison.OrdinalIgnoreCase))
            return Destination.OutOfPlay;

        // Sanctuary plays on the ship – vereinfacht: Discard nach Cancel (Effekt ist der Cancel)
        return Destination.Discard;
    }

    /// <summary>Wohin geht das getroffene Ziel, wenn die Response resolved?</summary>
    public static Destination TargetDestination(Card response)
    {
        string n = (response.Name ?? "").Trim();
        if (n.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase))
            return Destination.ReturnToHand;
        return Destination.Discard;
    }

    public static bool ShouldOpenStackForPlay(Card card, bool seedPhase)
    {
        if (seedPhase) return false;
        // Interrupts immer (Responses möglich)
        if (IsInterrupt(card)) return true;
        // Events (Kevin, Devil, Treaties)
        if (IsEvent(card)) return true;
        // Doorways: at any time als neue Action – Stack, damit Responses möglich sind
        if (IsDoorway(card)) return true;
        // Normal card play (Personnel/Ship/Eq) – Energy Vortex
        if (GameSession.UsesNormalCardPlay(card)) return true;
        return false;
    }

    public static List<Card> LegalResponsesInHand(IEnumerable<Card> hand, PendingAction top, int owner)
    {
        return hand.Where(c => CanRespond(c, top, owner).ok).ToList();
    }

    public static string FormatStack(ActionStack stack)
    {
        if (!stack.IsOpen) return "Stack leer.";
        var lines = new List<string>();
        for (int i = 0; i < stack.Items.Count; i++)
        {
            var a = stack.Items[i];
            string mark = i == stack.Items.Count - 1 ? "▲ " : "  ";
            string cx = a.Cancelled ? " [CANCELLED]" : "";
            lines.Add($"{mark}{a.Summary}{cx}");
        }
        return string.Join("\n", lines);
    }
}