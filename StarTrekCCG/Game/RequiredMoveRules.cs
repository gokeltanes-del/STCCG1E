using System;

namespace StarTrekCCG;

/// <summary>
/// 7.10 moving required actions + 12.6 far/near.
/// One spaceline = one quadrant. WNOHGB: ends of that spaceline are adjacent
/// (not across quadrants — 7.1.7).
/// </summary>
public static class RequiredMoveRules
{
    public readonly record struct Hop(int Next, bool Wrap);

    public static int HopCount(int from, int to, int count, bool wrap)
    {
        if (count <= 0) return 0;
        int direct = Math.Abs(to - from);
        if (!wrap || count < 2) return direct;
        return Math.Min(direct, count - direct);
    }

    public static Hop? NextToward(int from, int dest, int count, bool wrap, Func<int, int>? spanEntering = null)
    {
        if (count <= 0 || from == dest) return null;
        if (from < 0 || dest < 0 || from >= count || dest >= count) return null;

        int directNext = dest > from ? from + 1 : from - 1;
        int directHops = Math.Abs(dest - from);
        bool atEnd = from == 0 || from == count - 1;
        int wrapNext = from == 0 ? count - 1 : 0;
        int wrapHops = wrap && count >= 2 && atEnd ? count - directHops : int.MaxValue;

        if (wrapHops < directHops)
            return new Hop(wrapNext, true);
        if (wrapHops == directHops && spanEntering != null)
        {
            int wrapSpan = spanEntering(wrapNext) + RemainingSpan(wrapNext, dest, count, wrap, spanEntering);
            int dirSpan = spanEntering(directNext) + RemainingSpan(directNext, dest, count, wrap, spanEntering);
            if (wrapSpan < dirSpan)
                return new Hop(wrapNext, true);
        }
        return new Hop(directNext, false);
    }

    private static int RemainingSpan(int from, int dest, int count, bool wrap, Func<int, int> spanEntering)
    {
        int sum = 0, i = from, guard = 0;
        while (i != dest && guard++ < count + 1)
        {
            var hop = NextToward(i, dest, count, wrap, null);
            if (hop == null) break;
            sum += Math.Max(0, spanEntering(hop.Value.Next));
            i = hop.Value.Next;
        }
        return sum;
    }

    public static bool IsToward(int from, int to, int dest, int count, bool wrap)
    {
        if (to == dest) return true;
        if (from == dest) return false;
        return HopCount(to, dest, count, wrap) < HopCount(from, dest, count, wrap);
    }

    /// <summary>12.6: more missions that way, then more span. −1 = exact tie.</summary>
    public static int FarEndIndex(int from, int count, Func<int, int> spanEntering)
    {
        if (count <= 1) return from;
        int leftCards = from;
        int rightCards = count - 1 - from;
        if (leftCards > rightCards) return 0;
        if (rightCards > leftCards) return count - 1;
        int leftSpan = 0;
        for (int i = from; i > 0; i--) leftSpan += spanEntering(i - 1);
        int rightSpan = 0;
        for (int i = from; i < count - 1; i++) rightSpan += spanEntering(i + 1);
        if (leftSpan > rightSpan) return 0;
        if (rightSpan > leftSpan) return count - 1;
        return -1;
    }
}