using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Compendium: Actions + valid responses + Stack (Premiere-Kern).
/// Katalog: Amanda Rogers, Kevin Uxbridge, Q2, Energy Vortex, The Devil,
/// Hugh, Asteroid Sanctuary, Subspace Interference.
/// </summary>
public static class TimingRules
{
    public enum ActionKind
    {
        PlayCard,
        EncounterDilemma,
        InitiateShipBattle,
        InitiatePersonnelBattle,
        DrawCard,
        ShipDestroyed,
        /// <summary>Ship span-flying past a location (Hail response window).</summary>
        ShipFlyBy,
        /// <summary>Planet mission just solved (Alien Groupie / other just responses).</summary>
        MissionJustSolved,
        /// <summary>Opponent just successfully played an [AU] card (Distortion Continuum).</summary>
        OpponentJustPlayedAu,
        /// <summary>Shared just-after window (trigger on PendingAction.JustTrigger).</summary>
        JustAfter
    }

    /// <summary>Shared JustAfter(trigger) — consumers: KlingonWithHonorDied (Death Yell), PersonnelBattleKlingonDied (Right of Vengeance).</summary>
    public enum JustAfterTrigger
    {
        None = 0,
        KlingonWithHonorDied = 1,
        PersonnelBattleKlingonDied = 2,
    }

    public enum Destination
    {
        Discard,
        OutOfPlay,
        ReturnToHand,
        Table          // Event/Doorway/Treaty bleibt
    }

    public enum ResponseWindowState
    {
        Closed,
        Silent,
        Think
    }

    public enum ResponseCardSource
    {
        Hand,
        Table,
        Hidden,
        Download,
        Skill
    }

    public sealed class LegalResponseItem
    {
        public required Card Card { get; init; }
        public ResponseCardSource Source { get; init; } = ResponseCardSource.Hand;
        public string Description { get; init; } = "";
        public bool IsMandatory { get; init; }
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
        public bool IsMandatory { get; set; }

        // Ship battle
        public object? AttackerHost { get; set; }
        public Card? AttackerCard { get; set; }
        public object? DefenderHost { get; set; }
        public Card? DefenderCard { get; set; }
        /// <summary>Defender ship is exposed (not cloaked). Irrelevant when DefenderCard is not a ship.</summary>
        public bool DefenderExposed { get; set; } = true;

        // Personnel battle
        public List<object>? AttackerTeam { get; set; }
        public List<object>? DefenderTeam { get; set; }
        public List<Card>? AttackerPresent { get; set; }
        public List<Card>? DefenderPresent { get; set; }
        public int DefenderOwner { get; set; }

        /// <summary>In-play card this action is targeting (e.g. Kevin on an Event already in play).</summary>
        public Card? TargetCard { get; set; }

        /// <summary>When true: Klingon combatants on attacking force have STRENGTH doubled (Klingon Right of Vengeance).</summary>
        public bool KlingonStrengthDoubled { get; set; }

        /// <summary>When Kind==JustAfter: which just-after trigger opened this window.</summary>
        public JustAfterTrigger JustTrigger { get; init; }
    }

    public sealed class ActionStack
    {
        public readonly List<PendingAction> Items = new();
        /// <summary>Spieler, der als Nächstes eine Response spielen oder passen darf.</summary>
        public int ResponsePlayer { get; set; }
        /// <summary>Wie oft hintereinander gepasst wurde (2 = beide, dann Resolve).</summary>
        public int ConsecutivePasses { get; set; }
        public ResponseWindowState State { get; set; } = ResponseWindowState.Closed;
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
        CardKinds.IsInterrupt(c) || ArtifactRules.IsPlaysAsInterruptFromHand(c);

    public static bool IsEvent(Card c) => CardKinds.IsEvent(c);

    public static bool IsDoorway(Card c) => CardKinds.IsDoorway(c);

    public static bool IsAnytimeType(Card c) => CardKinds.IsAnytimeType(c);

    /// <summary>
    /// Premiere JSON + App. A: "Nullifies an [Event] (except a [Shield] or Treaty)."
    /// Static Warp Bubble is a normal Event (Traveler: Transcendence nullifies it;
    /// Rishon / [Shield] are the Kevin immunities, not SWB itself).
    /// </summary>
    /// <summary>
    /// Printed Event OR Artifact-as-Event (Plays as [Event] / Role ArtifactAsEvent). F2 Pepsch-Lock.
    /// </summary>
    public static bool IsEventEquivalentForKevin(Card? ev)
    {
        if (ev == null) return false;
        // Missions/locations are never Kevin targets (play-on host must not count as Event).
        if (CardKinds.IsMission(ev)) return false;
        if (IsEvent(ev)) return true;
        var role = PlayOnRules.ResolvePlayOn(ev).Role;
        if (role == PlayOnRules.CardPlayRole.ArtifactAsEvent)
            return true;
        if (ArtifactRules.IsPlaysAsEventFromHand(ev))
            return true;
        return false;
    }

    public static (bool ok, string reason) CanKevinTargetEvent(Card ev)
    {
        if (CardKinds.IsMission(ev))
            return (false, "Kevin Uxbridge cannot nullify a mission.");
        if (!IsEventEquivalentForKevin(ev))
            return (false, "Kevin Uxbridge nullifies only an Event (incl. Artifact that plays as Event).");
        if (HasShieldIcon(ev))
            return (false, "Kevin Uxbridge: that Event has a Shield icon.");
        if (TreatyRules.IsTreatyCard(ev))
            return (false, "Kevin Uxbridge: treaties are immune.");
        return (true, "Nullify that Event.");
    }

    /// <summary>The Devil: Horga'hn, Wind Dancer, or a Treaty — in play or just played.</summary>
    /// <summary>Spock 2026-09-05: Hugh "Borg Ship" branch = Borg Ship Dilemma card only (not Borg-affiliation ships).</summary>
    public static bool IsBorgShipDilemma(Card? c)
    {
        if (c == null) return false;
        string n = (c.Name ?? "").Trim();
        if (!n.Equals("Borg Ship", StringComparison.OrdinalIgnoreCase)
            && !n.StartsWith("Borg Ship", StringComparison.OrdinalIgnoreCase))
            return false;
        string ty = c.Type ?? "";
        return ty.Contains("dilemma", StringComparison.OrdinalIgnoreCase) || ty.Length == 0;
    }

    /// <summary>Hugh battle-cancel / CanRespond source: Borg Ship Dilemma only (Spock 2026-09-05; not [Bor] ships, not Rogue Borg).</summary>
    public static bool IsHughBattleSource(Card? c) => IsBorgShipDilemma(c);

    public static (bool ok, string reason) CanDevilTarget(Card card)
    {
        if (card == null)
            return (false, "The Devil: no target.");
        if (TreatyRules.IsTreatyCard(card) || (card.Name ?? "").Contains("Treaty", StringComparison.OrdinalIgnoreCase))
            return (true, "Nullify that Treaty.");
        string n = (card.Name ?? "").Trim();
        if (n.Contains("Horga'hn", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Horgahn", StringComparison.OrdinalIgnoreCase))
            return (true, "Nullify Horga'hn.");
        if (n.Equals("Wind Dancer", StringComparison.OrdinalIgnoreCase))
            return (true, "Nullify Wind Dancer.");
        return (false, "The Devil nullifies Horga'hn, Wind Dancer, or one Treaty.");
    }

    public static bool HasShieldIcon(Card c) => CardIcons.Parse(c).Shield;


    // SEARCH: Rule: 7.4 · 7.4.2; Glossary: battle · cumulative; Verb: AtStartOfBattle / BattleStage.Responses
    /// <summary>
    /// Battle Stage 2 — responses at initiation ("at start of battle" / just initiated).
    /// Per battle, not SoT/EoT. Ship + personnel; cards may narrow (Honor Challenge = personnel only).
    /// ETA is a broader window — do not treat this gate as ETA.
    /// </summary>
    public static bool IsAtStartOfBattle(PendingAction top) =>
        top.Kind is ActionKind.InitiateShipBattle or ActionKind.InitiatePersonnelBattle;

    /// <summary>Alias for Stage 2 responses window (same as <see cref="IsAtStartOfBattle"/>).</summary>
    public static bool IsBattleStageResponses(PendingAction top) => IsAtStartOfBattle(top);

    // SEARCH: Glossary: actions - "just" / just after; Verb: JustAfter(trigger); AppA: Klingon Death Yell
    public static bool IsJustAfter(PendingAction top) =>
        top.Kind == ActionKind.JustAfter;

    public static bool IsJustAfter(PendingAction top, JustAfterTrigger trigger) =>
        top.Kind == ActionKind.JustAfter && top.JustTrigger == trigger;

    public static bool IsCatalogResponse(Card c)
    {
        string n = (c.Name ?? "").Trim();
        return n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Q2", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase)
            || n.Equals("The Devil", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Hugh", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Asteroid Sanctuary", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Subspace Schism", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Escape Pod", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Subspace Interference", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Hail", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Alien Groupie", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Distortion of Space/Time Continuum", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Emergency Transporter Armbands", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Honor Challenge", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Klingon Death Yell", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Klingon Right of Vengeance", StringComparison.OrdinalIgnoreCase);
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
                return (false, "Amanda Rogers: no Interrupt on the stack.");
            if (!IsInterrupt(top.Card))
                return (false, "Amanda Rogers nullifies only an Interrupt.");
            if (HasShieldIcon(top.Card))
                return (false, "Amanda Rogers: Shield interrupts are immune.");
            return (true, "Nullifies the Interrupt.");
        }

        if (n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Kevin Uxbridge: no Event on the stack.");
            return CanKevinTargetEvent(top.Card);
        }

        // Tox Uthat on table as Event: legal response vs Supernova on stack (Interrupt mode).
        if (ArtifactRules.IsToxUthat(response))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Tox Uthat: no Supernova on the stack.");
            if (!EventRules.IsSupernova(top.Card))
                return (false, "Tox Uthat as Interrupt nullifies only Supernova.");
            return (true, "Nullify Supernova (discard Tox Uthat).");
        }

        if (n.Equals("Q2", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Q2: no valid target on the stack.");
            string tn = (top.Card.Name ?? "").Trim();
            bool amanda = tn.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase);
            bool kevin = tn.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase);
            bool qDil = (top.Card.Type ?? "").Contains("dilemma", StringComparison.OrdinalIgnoreCase)
                        && ((top.Card.Name ?? "").Contains("Q", StringComparison.OrdinalIgnoreCase)
                            || (top.Card.Icons ?? "").Contains("Q", StringComparison.OrdinalIgnoreCase));
            if (!amanda && !kevin && !qDil)
                return (false, "Q2 nullifies only Amanda Rogers, Kevin Uxbridge, or a Q-dilemma.");
            return (true, "Nullifies " + tn + ".");
        }

        if (n.Equals("Subspace Schism", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.DrawCard || top.Card == null)
                return (false, "Subspace Schism: plays when a player would draw a card.");
            return (true, "Discard that card; they draw the next one.");
        }

        if (n.Equals("Escape Pod", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.ShipDestroyed || top.Card == null)
                return (false, "Escape Pod: plays just after your ship is destroyed.");
            if (top.Controller != responseOwner)
                return (false, "Escape Pod: that was not your ship.");
            return (true, "Crew relocates onto Escape Pod.");
        }

        if (n.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Energy Vortex: no card play on the stack.");
            if (top.Controller == responseOwner)
                return (false, "Energy Vortex plays only against opponent's card play.");
            if (!GameSession.UsesNormalCardPlay(top.Card))
                return (false, "Energy Vortex only against a normal card play (not Interrupt/Doorway).");
            return (true, "Cancels the card play; card returns to hand.");
        }

        if (n.Equals("The Devil", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Card == null)
                return (false, "The Devil: no target on the stack.");
            if (top.Kind is not (ActionKind.PlayCard or ActionKind.EncounterDilemma))
                return (false, "The Devil: no Treaty / Horga'hn / Wind Dancer on the stack.");
            return CanDevilTarget(top.Card);
        }

        if (n.Equals("Hugh", StringComparison.OrdinalIgnoreCase))
        {
            // Ship or facility battle OK — Hugh nullifies the Borg Ship dilemma source, not "ship-only".
            if (top.Kind is not (ActionKind.InitiateShipBattle or ActionKind.InitiatePersonnelBattle))
                return (false, "Hugh: no battle initiation on the stack.");
            if (!IsBorgShipDilemma(top.AttackerCard) && !IsBorgShipDilemma(top.Card))
                return (false, "Hugh: battle must originate from the Borg Ship dilemma.");
            return (true, "Nullifies the Borg Ship dilemma attack.");
        }

        if (n.Equals("Asteroid Sanctuary", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.InitiateShipBattle)
                return (false, "Asteroid Sanctuary: no ship battle on the stack.");
            if (top.DefenderCard == null)
                return (false, "No defending ship.");
            // Facility/outpost battles reuse InitiateShipBattle — Sanctuary is ship-only.
            bool hostIsShip = CardKinds.IsShip(top.DefenderCard);
            bool isYours = top.DefenderOwner == responseOwner
                           || (top.DefenderOwner == 0 && GetOwnerGuess(top) == responseOwner);
            var deny = InterruptShipEffectRules.SanctuaryDeny(
                hostIsShip, isYours, top.DefenderExposed, has2Navigation: false);
            if (deny != null)
                return (false, deny);
            return (true, "Can cancel the battle against your ship (2 Navigation aboard at resolution).");
        }

        if (n.Equals("Subspace Interference", StringComparison.OrdinalIgnoreCase))
        {
            // Nullifies Incoming Message OR Hail OR Subspace Schism as they are played.
            if (top.Kind != ActionKind.PlayCard || top.Card == null)
                return (false, "Subspace Interference: no matching Interrupt on the stack.");
            if (!InterruptRules.IsInterferenceNullifyTarget(top.Card))
                return (false, "Subspace Interference nullifies only Incoming Message, Hail, or Subspace Schism.");
            return (true, "Nullifies " + (top.Card.Name ?? "that Interrupt") + ".");
        }


        if (n.Equals("Hail", StringComparison.OrdinalIgnoreCase))
        {
            // Fly-by: play on the flying-by ship while ShipFlyBy is on the stack.
            if (top.Kind != ActionKind.ShipFlyBy)
                return (false, "Hail: no ship flying by on the stack.");
            if (top.AttackerHost == null || top.AttackerCard == null)
                return (false, "Hail: flying-by ship missing.");
            return (true, "Stop the flying-by ship at your location (may not move further this turn).");
        }

        if (n.Equals("Alien Groupie", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.MissionJustSolved)
                return (false, "Alien Groupie: plays just after a planet mission is solved.");
            if (top.Controller != responseOwner)
                return (false, "Alien Groupie: that was not your Away Team's solve.");
            var present = top.AttackerPresent;
            if (present == null || present.Count == 0)
                return (false, "Alien Groupie: Away Team missing.");
            if (!present.Any(DilemmaRules.IsFemale))
                return (false, "Alien Groupie requires a Female present.");
            if (!present.Any(DilemmaRules.IsMale))
                return (false, "Alien Groupie: no male present to stop.");
            return (true, "Stop one male present until countdown 2 expires.");
        }


        if (n.Equals("Distortion of Space/Time Continuum", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind != ActionKind.OpponentJustPlayedAu)
                return (false, "Distortion: plays just after opponent plays an [AU] card.");
            if (top.Controller == responseOwner)
                return (false, "Distortion: that was your own [AU] play.");
            return (true, "Attach to your non-[AU] ship (Unique).");
        }


        if (n.Equals("Emergency Transporter Armbands", StringComparison.OrdinalIgnoreCase))
        {
            if (top.Kind is ActionKind.InitiateShipBattle or ActionKind.InitiatePersonnelBattle)
                return (true, "Beam your personnel away (not mid-combat).");
            if (top.Kind == ActionKind.EncounterDilemma)
            {
                if (top.Card == null || !CardIcons.HasEtaDilemma(top.Card))
                    return (false, "Armbands: only while facing an [ETA] dilemma.");
                return (true, "Beam your personnel away while facing this dilemma.");
            }
            return (false, "Armbands: play during battle or while facing an [ETA] dilemma (icon).");
        }


        // SEARCH: Rule: 7.4 · 7.4.2; Glossary: battle · cumulative; AppA: Honor Challenge; Verb: AtStartOfBattle / BattleStage.Responses
        if (n.Equals("Honor Challenge", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsBattleStageResponses(top) || top.Kind != ActionKind.InitiatePersonnelBattle)
                return (false, "Honor Challenge: plays at start of a personnel battle only.");
            // Presence pair checked in TW CollectAllLegalResponses (needs Attacker/DefenderPresent).
            return (true, "Each of your Klingons with Honor may kill an opposing personnel present with Treachery.");
        }


        // SEARCH: Glossary: actions - "just" / just after; AppA: Klingon Death Yell; Verb: JustAfter(KlingonWithHonorDied)
        if (n.Equals("Klingon Death Yell", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsJustAfter(top, JustAfterTrigger.KlingonWithHonorDied))
                return (false, "Klingon Death Yell: plays just after a Klingon with Honor dies.");
            // Either player; limit one each = one JustAfter window per such death.
            return (true, "Score 5 points (just after that Klingon with Honor died).");
        }

        // SEARCH: Rule: 7.4.2 · 7.4.4; Glossary: actions - "just" / just after; AppA: Klingon Right of Vengeance; Verb: JustAfter(PersonnelBattleKlingonDied)
        if (n.Equals("Klingon Right of Vengeance", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsJustAfter(top, JustAfterTrigger.PersonnelBattleKlingonDied))
                return (false, "Klingon Right of Vengeance: plays just after a personnel battle where a Klingon died.");
            return (true, "Your Klingons present may immediately attack same opponents, even without a leader (STRENGTH doubled).");
        }

        return (false, $"\"{n}\" is not a valid response in this window.");
    }

    private static int GetOwnerGuess(PendingAction top) => top.DefenderOwner;

    /// <summary>Wohin geht die Response-Karte selbst nach erfolgreichem Resolve?</summary>
    public static Destination SelfDestination(Card response)
    {
        string n = (response.Name ?? "").Trim();
        if (n.Equals("Amanda Rogers", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Q2", StringComparison.OrdinalIgnoreCase))
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
        if (IsEvent(card) || CardKinds.IsObjective(card)) return true;
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

    public static List<LegalResponseItem> CollectLegalResponses(
        IEnumerable<Card> hand,
        IEnumerable<Card>? tableCards,
        PendingAction top,
        int owner)
    {
        var result = new List<LegalResponseItem>();
        if (hand != null)
        {
            foreach (var c in hand)
            {
                var cr = CanRespond(c, top, owner);
                if (cr.ok)
                {
                    result.Add(new LegalResponseItem
                    {
                        Card = c,
                        Source = ResponseCardSource.Hand,
                        Description = cr.reason,
                        IsMandatory = top.IsMandatory
                    });
                }
            }
        }

        if (tableCards != null)
        {
            foreach (var c in tableCards)
            {
                var cr = CanRespond(c, top, owner);
                if (cr.ok)
                {
                    result.Add(new LegalResponseItem
                    {
                        Card = c,
                        Source = ResponseCardSource.Table,
                        Description = cr.reason,
                        IsMandatory = top.IsMandatory
                    });
                }
            }
        }

        return result;
    }

    public static string FormatStack(ActionStack stack)
    {
        if (!stack.IsOpen) return "Stack empty.";
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


    // ------------------------------------------------------------------
    // Start-of-turn play window (Compendium 6.1 Exception / Glossary "turn")
    // Rule: turn · 6.1 · 6.1 Exception Start of turn · 6.5.1 · at any time · 7
    // Glossary: turn · start of turn · at any time · normal card play
    // Verb: StartOfTurn / StartOfTurnWindow · NormalCardPlay · BeginPlay Gate · TurnSegment · IsAnytimeType
    // Segments: 1 SoT → 2 normal card play → 3 execute orders → 4 EoT → 5 draw.
    // SoT-restricted cards stay bound to segment 1 even if type is Interrupt (no normal-card-play cost).
    // ------------------------------------------------------------------

    /// <summary>
    /// Printed play timing "plays at / at the start of your turn" (phrase system).
    /// Full Planet Scan is the first consumer; prefer gametext phrase over name-only apply paths.
    /// </summary>
    public static bool RequiresStartOfTurnWindow(Card? card)
    {
        if (card == null) return false;
        if (HasStartOfTurnPlayPhrase(card)) return true;
        // First consumer (Premiere) until more phrase cards share the window.
        return InterruptRules.IsFullPlanetScan(card);
    }

    /// <summary>Gametext / text contains start-of-turn play wording.</summary>
    public static bool HasStartOfTurnPlayPhrase(Card card)
    {
        string text = card.Text ?? "";
        if (text.Length == 0) return false;
        if (text.IndexOf("at the start of your turn", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (text.IndexOf("plays at the start of your turn", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (text.IndexOf("play at the start of your turn", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    /// <summary>
    /// Segment-1 window still open: Play segment and normal card play neither used nor forfeited.
    /// </summary>
    public static bool IsStartOfTurnWindowOpen(
        GameSession.TurnSegment segment,
        bool normalCardPlayUsed,
        bool normalCardPlayForfeited = false)
    {
        return segment == GameSession.TurnSegment.Play
            && !normalCardPlayUsed
            && !normalCardPlayForfeited;
    }

    public static bool IsStartOfTurnWindowOpen(GameState state) =>
        IsStartOfTurnWindowOpen(state.Segment, state.NormalCardPlayUsed, normalCardPlayForfeited: false);

    /// <summary>
    /// Gate before BeginPlay / stack / responses. Immediate deny — no Amanda window.
    /// </summary>
    public static (bool ok, string reason) CanPlayStartOfTurnCard(
        Card card,
        int player,
        int activePlayer,
        GameSession.TurnSegment segment,
        bool normalCardPlayUsed,
        bool normalCardPlayForfeited = false)
    {
        if (!RequiresStartOfTurnWindow(card))
            return (true, "");

        if (player != activePlayer)
            return (false, "Start of turn: only on your turn (before your normal card play).");

        if (!IsStartOfTurnWindowOpen(segment, normalCardPlayUsed, normalCardPlayForfeited))
            return (false,
                "Start of turn: only before your normal card play "
                + "(Compendium 6.1 Exception). No stack or response window.");

        return (true, "");
    }

    public static (bool ok, string reason) CanPlayStartOfTurnCard(GameState state, Card card, int player) =>
        CanPlayStartOfTurnCard(
            card,
            player,
            state.ActivePlayer,
            state.Segment,
            state.NormalCardPlayUsed,
            normalCardPlayForfeited: false);

    // ------------------------------------------------------------------
    // Turn phrasing (Compendium glossary / §5, §12.2)
    // ------------------------------------------------------------------

    /// <summary>
    /// How a delayed effect counts turns.
    /// </summary>
    public enum TurnScope
    {
        /// <summary>
        /// "next turn" (unqualified): the chronologically next turn in the game
        /// (usually the opponent's turn after you play the card).
        /// </summary>
        NextTurn,

        /// <summary>
        /// "your next turn" / "owner's next turn" / "controller's next turn":
        /// only that specific player's upcoming turn(s).
        /// </summary>
        SpecificPlayerNextTurn,

        /// <summary>
        /// "every turn": every individual turn of every player.
        /// </summary>
        EveryTurn,

        /// <summary>
        /// "each turn" / "per turn" of a subject: only that player's own turns
        /// (opponent's turns are skipped for the counter).
        /// </summary>
        EachSubjectTurn
    }

    public enum TurnPhasePoint
    {
        StartOfTurn,
        EndOfTurn
    }

    /// <summary>
    /// Whether a delayed effect should process/tick on this turn boundary.
    /// </summary>
    /// <param name="scope">Card wording scope.</param>
    /// <param name="phasePoint">When the effect checks (start vs end of turn).</param>
    /// <param name="currentPhase">The phase point currently being processed.</param>
    /// <param name="turnPlayer">Player whose turn is starting or ending.</param>
    /// <param name="scopePlayer">Owner/controller/subject for SpecificPlayer / EachSubject (1 or 2).</param>
    public static bool ShouldProcessOnTurn(
        TurnScope scope,
        TurnPhasePoint phasePoint,
        TurnPhasePoint currentPhase,
        int turnPlayer,
        int? scopePlayer)
    {
        if (phasePoint != currentPhase)
            return false;

        return scope switch
        {
            TurnScope.NextTurn => true,
            TurnScope.EveryTurn => true,
            TurnScope.SpecificPlayerNextTurn =>
                scopePlayer is > 0 && turnPlayer == scopePlayer.Value,
            TurnScope.EachSubjectTurn =>
                scopePlayer is > 0 && turnPlayer == scopePlayer.Value,
            _ => true
        };
    }

    /// <summary>
    /// Decrement a countdown if this turn boundary matches the effect's scope.
    /// Returns true when the countdown reaches 0 (effect should resolve/expire).
    /// </summary>
    public static bool TickCountdown(
        ref int countdown,
        TurnScope scope,
        TurnPhasePoint phasePoint,
        TurnPhasePoint currentPhase,
        int turnPlayer,
        int? scopePlayer)
    {
        if (!ShouldProcessOnTurn(scope, phasePoint, currentPhase, turnPlayer, scopePlayer))
            return false;
        if (countdown <= 0)
            return true;
        countdown--;
        return countdown <= 0;
    }

    public static string DescribeScope(TurnScope scope, int? scopePlayer = null)
    {
        return scope switch
        {
            TurnScope.NextTurn => "next turn (chronological)",
            TurnScope.EveryTurn => "every turn",
            TurnScope.SpecificPlayerNextTurn =>
                scopePlayer is > 0
                    ? $"P{scopePlayer}'s next turn"
                    : "owner's/controller's next turn",
            TurnScope.EachSubjectTurn =>
                scopePlayer is > 0
                    ? $"each of P{scopePlayer}'s turns"
                    : "each turn of the subject",
            _ => scope.ToString()
        };
    }
}