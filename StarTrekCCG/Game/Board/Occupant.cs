namespace StarTrekCCG;

public enum OccupantKind
{
    Ship,
    Facility
}

/// <summary>Ship or Facility sitting at a Location. Crew is a Force, not a type change.</summary>
public sealed class Occupant
{
    public Occupant(CardInstance shipOrFacility)
    {
        Card = shipOrFacility;
        Kind = shipOrFacility is FacilityInstance ? OccupantKind.Facility : OccupantKind.Ship;
        Crew = new Force(ForceKind.Crew, shipOrFacility.Controller);
    }

    public OccupantKind Kind { get; }
    public CardInstance Card { get; }
    public Force Crew { get; }

    public int InstanceId => Card.InstanceId;

    public override string ToString() => $"{Kind}:{Card}";
}