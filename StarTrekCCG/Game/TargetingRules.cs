using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Shared targeting contract. UI never invents a third path.
///
/// 1. ResolvePlayOn / PlayOnRules.Parse (type-agnostic Host + Role).
/// 2. Named overrides stay in InterruptRules / EventRules (Wormhole pair, Kevin, Hugh, Gaps).
/// 3. TargetQuery.NullifySites / CanTarget = legality (Kevin, Devil, later PlayOn).
/// 4. TableWindow.TargetSession maps sites → glow / 1s peek / snap / place-choose.
/// New sets hook in by extending PlayOnRules.Parse or one named override - not a new drag path.
/// </summary>
public static class TargetingRules
{
    public static bool UsesBoardSnap(Card card)
    {
        if (card == null) return false;

        // F1: Spec-driven, typ-agnostisch (Artifact-as-Event, dual AT/Crew, …).
        var spec = PlayOnRules.ResolvePlayOn(card);
        if (spec.NeedsBoardSnap)
            return true;

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
