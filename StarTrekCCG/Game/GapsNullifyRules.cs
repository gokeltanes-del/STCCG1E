using System;
using System.Collections.Generic;

namespace StarTrekCCG;

/// <summary>
/// Gaps in Normal Space nullify (Kevin etc.): close gap; cards on the event discard;
/// ships/cards at that location relocate to one adjacent spaceline location chosen by the nullifier.
/// Pure decide - View prompts and moves. Spock/Glossary 2026-09-05.
/// </summary>
public static class GapsNullifyRules
{
    public static bool NeedsRelocateOnNullify(bool isGapsInNormalSpace) => isGapsInNormalSpace;

    /// <summary>Host / Host2 endpoints of the Gaps span (not the Gaps card).</summary>
    public static IReadOnlyList<T> AdjacentEndpoints<T>(T? host, T? host2) where T : class
    {
        var list = new List<T>(2);
        if (host != null) list.Add(host);
        if (host2 != null && !ReferenceEquals(host2, host)) list.Add(host2);
        return list;
    }

    /// <summary>
    /// Fallback when Host/Host2 missing: neighbors of Gaps index on the spaceline order.
    /// </summary>
    public static IReadOnlyList<int> AdjacentIndices(int gapsIndex, int spacelineCount)
    {
        var list = new List<int>(2);
        if (gapsIndex < 0 || spacelineCount < 2) return list;
        if (gapsIndex > 0) list.Add(gapsIndex - 1);
        if (gapsIndex + 1 < spacelineCount) list.Add(gapsIndex + 1);
        return list;
    }

    /// <summary>Discard cards played on the Gaps event; relocate ships/facilities at the location.</summary>
    public static bool DiscardOnEventCard(bool isShipOrFacility) => !isShipOrFacility;
}
