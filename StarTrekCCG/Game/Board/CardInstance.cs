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

    /// <summary>True if leave/beam is blocked (e.g. Quarantine or Stasis).</summary>
    public virtual bool IsLeaveBlocked => false;

    public override string ToString() => DebugLog.Card(Printed);
}

public sealed class PersonnelInstance : CardInstance
{
    public PersonnelInstance(Card printed) : base(printed) { }

    /// <summary>Personnel is quarantined (e.g. Hyper-Aging). Cannot leave/beam away.</summary>
    public bool Quarantined { get; set; }

    /// <summary>Personnel is in stasis (e.g. Phased Matter, Alien Abduction). Cannot leave or act.</summary>
    public bool InStasis { get; set; }

    /// <summary>Personnel is disabled (e.g. Ktarian Game). Cannot act or use skills/attributes.</summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Glossary: hologram — deactivated (not erased). May exist aboard any ship/facility;
    /// planet needs Holo-Projectors/MHE. Distinct from dilemma Disabled (Ktarian/TwoDim).
    /// </summary>
    public bool HologramDeactivated { get; set; }

    /// <summary>True if leave/beam is blocked (Quarantine, Stasis, or Disabled).</summary>
    public override bool IsLeaveBlocked => Quarantined || InStasis || Disabled;
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

    /// <summary>P0-S1: repair turns at outpost (0-2). UI <c>_repairTurnsAtOutpost</c> mirrors.</summary>
    public int RepairTurns { get; set; } = 0;

    /// <summary>P0-S1: cloaking locked (e.g. Tachyon Detection Grid). UI <c>_cloakLocked</c> mirrors.</summary>
    public bool CloakLocked { get; set; }
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
