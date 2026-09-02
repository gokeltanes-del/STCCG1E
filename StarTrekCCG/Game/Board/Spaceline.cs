using System;
using System.Collections.Generic;

namespace StarTrekCCG;

/// <summary>Ordered locations. No owner. Hop cost includes each destination Span.</summary>
public sealed class Spaceline
{
    private readonly List<Location> _locations = new();
    private int _nextLocationId = 1;

    public IReadOnlyList<Location> Locations => _locations;

    public int NextId() => _nextLocationId++;

    public void Insert(int index, Location loc)
    {
        if (index < 0) index = 0;
        if (index > _locations.Count) index = _locations.Count;
        _locations.Insert(index, loc);
    }

    public void Add(Location loc) => _locations.Add(loc);

    public bool Remove(Location loc) => _locations.Remove(loc);

    public Location? FindByInstanceId(int instanceId)
    {
        foreach (var loc in _locations)
        {
            if (loc.Printed?.InstanceId == instanceId) return loc;
            if (loc.Mission?.InstanceId == instanceId) return loc;
        }
        return null;
    }

    /// <summary>Sum of Span of columns moved onto (from exclusive .. to inclusive).</summary>
    public int HopCost(int fromIndex, int toIndex)
    {
        if (_locations.Count == 0) return 0;
        if (fromIndex == toIndex) return 0;
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= _locations.Count || toIndex >= _locations.Count)
            return 0;

        int step = toIndex > fromIndex ? 1 : -1;
        int cost = 0;
        for (int i = fromIndex + step; ; i += step)
        {
            cost += Math.Max(0, _locations[i].Span);
            if (i == toIndex) break;
        }
        return cost;
    }

    public bool PathBlocked(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return false;
        int step = toIndex > fromIndex ? 1 : -1;
        for (int i = fromIndex; i != toIndex; i += step)
        {
            if (_locations[i].BarrierAfter && step > 0) return true;
            if (step < 0 && i > 0 && _locations[i - 1].BarrierAfter) return true;
        }
        return false;
    }
}