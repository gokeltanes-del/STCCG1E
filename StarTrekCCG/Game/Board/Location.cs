using System.Collections.Generic;
using StarTrekCCG.Models;

namespace StarTrekCCG;

public enum LocationKind
{
    Mission,
    Span,
    TimeLocation
}

/// <summary>
/// One column on the spaceline. Gaps = Span (landable). Q-Net is a barrier <em>between</em>
/// locations, not a Location.
/// </summary>
public sealed class Location
{
    public int Id { get; init; }
    public LocationKind Kind { get; init; }
    public string? Quadrant { get; set; }

    /// <summary>Printed mission / time location / Gaps card. Null only if we invent a column.</summary>
    public Card? Printed { get; init; }

    public MissionInstance? Mission { get; set; }

    /// <summary>Span paid when moving here. Gaps default 4.</summary>
    public int Span { get; set; } = 1;

    public List<Occupant> Occupants { get; } = new();
    public Force AwayTeamP1 { get; } = new(ForceKind.AwayTeam, 1);
    public Force AwayTeamP2 { get; } = new(ForceKind.AwayTeam, 2);

    /// <summary>Q-Net (or similar) on the edge after this column — not landable.</summary>
    public bool BarrierAfter { get; set; }

    public string Label => Printed != null ? DebugLog.Card(Printed) : $"{Kind}#{Id}";

    public override string ToString() => $"{Kind}:{Label}";
}