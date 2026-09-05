using System;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 5: ApplyNamedAuInterrupt name → outcome routing.
/// Pure decide — no WPF. View still nullifies, discards, arms flags, status.
/// Kevin Convergence already partly extracted (ApplyKevinConvergence + KevinEventAtLocation).
/// </summary>
public static class NamedInterruptRules
{
    public enum NamedAuOutcome
    {
        None,
        KevinConvergence,
        Countermanda,
        DestroyScow,
        SeniorStaffMeeting,
        Hail
    }

    public static NamedAuOutcome Decide(Card card)
    {
        if (EventRules.IsKevinConvergence(card))
            return NamedAuOutcome.KevinConvergence;

        string n = (card.Name ?? "").Trim();
        if (n.Equals("Countermanda", StringComparison.OrdinalIgnoreCase))
            return NamedAuOutcome.Countermanda;
        if (n.Equals("Destroy Radioactive Garbage Scow", StringComparison.OrdinalIgnoreCase))
            return NamedAuOutcome.DestroyScow;
        if (n.Equals("Senior Staff Meeting", StringComparison.OrdinalIgnoreCase))
            return NamedAuOutcome.SeniorStaffMeeting;
        if (n.Equals("Hail", StringComparison.OrdinalIgnoreCase))
            return NamedAuOutcome.Hail;
        return NamedAuOutcome.None;
    }
}
