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

        int left = from - 1;
        int right = from + 1;
        if (wrap && count >= 2)
        {
            if (left < 0) left = count - 1;
            if (right >= count) right = 0;
        }

        Hop? Pick(int idx, bool isWrap) =>
            idx >= 0 && idx < count ? new Hop(idx, isWrap) : null;

        var l = Pick(left, wrap && from == 0 && left == count - 1);
        var r = Pick(right, wrap && from == count - 1 && right == 0);
        if (l == null) return r;
        if (r == null) return l;

        int hopsL = HopCount(l.Value.Next, dest, count, wrap);
        int hopsR = HopCount(r.Value.Next, dest, count, wrap);
        if (hopsL < hopsR) return l;
        if (hopsR < hopsL) return r;
        if (spanEntering != null)
        {
            int sl = spanEntering(l.Value.Next);
            int sr = spanEntering(r.Value.Next);
            if (sl < sr) return l;
            if (sr < sl) return r;
        }
        return dest > from ? r : l;
    }

    /// <summary>
    /// Shortest hop that this ship can actually pay for this turn.
    /// If the wrap/short hop costs more RANGE than remains, take the other way.
    /// </summary>
    public static Hop? NextAffordable(
        int from, int dest, int count, bool wrap, int remain, Func<int, int> spanEntering)
    {
        var pref = NextToward(from, dest, count, wrap, spanEntering);
        if (pref == null) return null;
        if (spanEntering(pref.Value.Next) <= remain) return pref;

        int left = from - 1, right = from + 1;
        if (wrap && count >= 2)
        {
            if (left < 0) left = count - 1;
            if (right >= count) right = 0;
        }
        int other = pref.Value.Next == left ? right : left;
        if (other >= 0 && other < count && other != pref.Value.Next
            && spanEntering(other) <= remain)
        {
            bool w = wrap && ((from == 0 && other == count - 1) || (from == count - 1 && other == 0));
            return new Hop(other, w);
        }
        return pref;
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
