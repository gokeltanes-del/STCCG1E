using System;

namespace StarTrekCCG;

/// <summary>
/// TableWindow extract Slice 5: ApplyInstantEvent decide gates.
/// Pure PlayResult → effect plan — no WPF. View still asks players, draws, discards.
/// </summary>
public static class InstantEventRules
{
    /// <summary>Which instant side-effects to run from EventRules.PlayResult.</summary>
    public readonly record struct InstantPlan(int DrawCards, bool Masaka, bool ResQ)
    {
        public bool HasAny => DrawCards > 0 || Masaka || ResQ;
    }

    public static InstantPlan Decide(EventRules.PlayResult r) =>
        new(r.DrawCards, r.Masaka, r.ResQ);
}
