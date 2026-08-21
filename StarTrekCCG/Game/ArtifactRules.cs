using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Premiere Artifacts (9): Erwerb beim Encounter + spätere Nutzung.
/// </summary>
public static class ArtifactRules
{
    public enum AcquireKind
    {
        /// <summary>Sofort-Effekt, Artifact wird discarded.</summary>
        ImmediateDiscard,
        /// <summary>Sofort auf den Tisch (permanent).</summary>
        PlaceOnTable,
        /// <summary>In die Hand (später als Event/Interrupt/Equipment spielbar).</summary>
        ToHand,
        /// <summary>Als Equipment am Team / Schiff bleiben.</summary>
        UseAsEquipment
    }

    public sealed class AcquireResult
    {
        public required string Name { get; init; }
        public AcquireKind Kind { get; init; }
        public string Message { get; init; } = "";
        /// <summary>Betazoid Gift Box: Karten vom Draw Deck.</summary>
        public int DownloadFromDraw { get; init; }
        /// <summary>Horga'hn: Extra-Normal-Play ODER Extra-Draw am Zugende.</summary>
        public bool GrantsHorgahn { get; init; }
    }

    public static bool IsArtifact(Card c) =>
        (c.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase);

    public static AcquireResult ResolveAcquire(Card artifact)
    {
        string n = (artifact.Name ?? "").Trim();
        return n switch
        {
            "Betazoid Gift Box" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ImmediateDiscard,
                DownloadFromDraw = 3,
                Message = "Bis zu 3 Karten vom Draw Deck auf die Hand (Download). Artifact discarded."
            },
            "Horga'hn" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.PlaceOnTable,
                GrantsHorgahn = true,
                Message = "Horga'hn auf den Tisch: je Zug +1 normale Card Play ODER am Zugende +1 Draw."
            },
            "Interphase Generator" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.UseAsEquipment,
                Message = "Als Equipment: nullifiziert [IPG]-Dilemmas wo present."
            },
            "Varon-T Disruptor" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.UseAsEquipment,
                Message = "Als Equipment: verdoppelt STRENGTH deines Personals present."
            },
            "Kurlan Naiskos" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "In die Hand – später als Event auf ein Schiff (Attribute ×3 bei allen Classifications)."
            },
            "Thought Maker" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "In die Hand – Typ nennen, passende Karten aus Gegner-Draw nach unten legen."
            },
            "Tox Uthat" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "In die Hand – Event/Interrupt vs. Supernova (wenn gespielt)."
            },
            "Vulcan Stone of Gol" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "In die Hand – Event auf Away Team: ohne Youth und CUNNING≤7 sterben."
            },
            "Time Travel Pod" => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.PlaceOnTable,
                Message = "Time Travel Pod auf den Tisch (Sandbox: Zeitort-Marker; Relocate vereinfacht)."
            },
            _ => new AcquireResult
            {
                Name = n,
                Kind = AcquireKind.ToHand,
                Message = "Artifact verdient → Hand."
            }
        };
    }

    /// <summary>Varon-T: STRENGTH ×2 für own personnel present.</summary>
    public static bool IsVaronT(Card c) =>
        (c.Name ?? "").Equals("Varon-T Disruptor", StringComparison.OrdinalIgnoreCase);

    /// <summary>Interphase Generator als Equipment-Flag.</summary>
    public static bool IsInterphaseGenerator(Card c) =>
        (c.Name ?? "").Equals("Interphase Generator", StringComparison.OrdinalIgnoreCase);

    /// <summary>Kurlan: alle 7 Classifications an Bord?</summary>
    public static bool KurlanFullyStaffed(IEnumerable<Card> aboard)
    {
        var need = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OFFICER", "ENGINEER", "MEDICAL", "SCIENCE", "SECURITY", "V.I.P.", "CIVILIAN"
        };
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in aboard.Where(ModifierRules.IsPersonnelCard))
        {
            string cls = (p.Class ?? "").Trim();
            if (!string.IsNullOrEmpty(cls))
                have.Add(cls);
            // auch aus Text / Skills
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (need.Contains(kv.Key))
                    have.Add(kv.Key);
            }
        }
        // VIP / CIVILIAN oft als Classification
        foreach (var p in aboard)
        {
            string t = ((p.Class ?? "") + " " + (p.Text ?? "")).ToUpperInvariant();
            if (t.Contains("V.I.P") || t.Contains("VIP")) have.Add("V.I.P.");
            if (t.Contains("CIVILIAN")) have.Add("CIVILIAN");
        }
        return need.All(n => have.Any(h =>
            h.Equals(n, StringComparison.OrdinalIgnoreCase)
            || (n == "V.I.P." && h.Contains("VIP", StringComparison.OrdinalIgnoreCase))));
    }
}