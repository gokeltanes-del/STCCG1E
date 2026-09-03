using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// In-play copy. Printed ink stays on <see cref="Card"/>; this object is identity + later status.
/// Do not copy Region/Span/STRENGTH here - ask Printed or ModifierRules.
/// </summary>
public abstract class CardInstance
{
    protected CardInstance(Card printed)
    {
        Printed = printed;
    }

    public Card Printed { get; }

    public int InstanceId => Printed.InstanceId;
    public int Owner => Printed.OwnerPlayer;
    public int Controller => Printed.Controller;
    public string Name => Printed.Name ?? "?";

    /// <summary>E3: stopped until start of next turn. UI <c>_stoppedBorders</c> mirrors this.</summary>
    public bool Stopped { get; set; }

    public override string ToString() => DebugLog.Card(Printed);
}

public sealed class PersonnelInstance : CardInstance
{
    public PersonnelInstance(Card printed) : base(printed) { }
}

public sealed class ShipInstance : CardInstance
{
    public ShipInstance(Card printed) : base(printed) { }

    /// <summary>E3: remaining RANGE this turn. -1 = unset (UI dict fills). UI <c>_shipRangeLeft</c> mirrors.</summary>
    public int RangeLeft { get; set; } = -1;

    /// <summary>E3b: cloaked. UI <c>_cloakedShips</c> mirrors this.</summary>
    public bool Cloaked { get; set; }

    /// <summary>E3b: facility InstanceId when docked; 0 = not docked. UI <c>_dockedAt</c> mirrors.</summary>
    public int DockedAtId { get; set; }

    /// <summary>E3b: hull damage 0-100. -1 = unset (UI dict fills). UI <c>_hullDamagePercent</c> mirrors.</summary>
    public int HullPercent { get; set; } = -1;
}

public sealed class FacilityInstance : CardInstance
{
    public FacilityInstance(Card printed) : base(printed) { }
}

public sealed class EventInstance : CardInstance
{
    public EventInstance(Card printed) : base(printed) { }
}

public sealed class EquipmentInstance : CardInstance
{
    public EquipmentInstance(Card printed) : base(printed) { }
}

public sealed class MissionInstance : CardInstance
{
    public MissionInstance(Card printed) : base(printed) { }
}

/// <summary>Dilemma, Artifact, Doorway, Interrupt, . until a typed role is needed.</summary>
public sealed class OtherInstance : CardInstance
{
    public OtherInstance(Card printed) : base(printed) { }
}
