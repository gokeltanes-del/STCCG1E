using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Shared targeting contract. UI never invents a third path.
///
/// 1. Parse printed "Plays on …" via PlayOnRules (ship / mission / facility / exposed / own).
/// 2. Named overrides stay in InterruptRules / EventRules (Wormhole pair, Kevin, Hugh, Gaps).
/// 3. TableWindow.CollectLegalSnapHosts is the only list:
///      halo = that list, snap-in-field = nearest in range, drop = must hit that list.
/// New sets hook in by extending PlayOnRules.Parse or one named override — not a new drag path.
/// </summary>
public static class TargetingRules
{
    public static bool UsesBoardSnap(Card card)
    {
        if (card == null) return false;
        if (EventRules.IsEvent(card))
        {
            var tk = EventRules.GetTargetKind(EventRules.ResolvePlay(card));
            return EventRules.NeedsTableHost(tk);
        }
        if (InterruptRules.IsInterrupt(card))
            return InterruptRules.NeedsDropTarget(card)
                   || InterruptRules.IsWormhole(card)
                   || InterruptRules.IsKevinNullify(card)
                   || InterruptRules.IsDevil(card)
                   || InterruptRules.IsHugh(card);
        return false;
    }
}