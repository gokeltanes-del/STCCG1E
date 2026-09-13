using System.Collections.Generic;
using System.Linq;

namespace StarTrekCCG;

public enum ForceKind
{
    Crew,
    AwayTeam
}

/// <summary>Personnel + Equipment at one place. Beam = move instances between Forces.</summary>
public sealed class Force
{
    public Force(ForceKind kind, int controller)
    {
        Kind = kind;
        Controller = controller;
    }

    public ForceKind Kind { get; }
    public int Controller { get; set; }

    public List<PersonnelInstance> Personnel { get; } = new();
    public List<EquipmentInstance> Equipment { get; } = new();

    /// <summary>True if any personnel in this force is quarantined.</summary>
    public bool IsQuarantined => Personnel.Any(p => p.Quarantined);

    /// <summary>True if any personnel in this force is blocked from leaving (Quarantine or Stasis).</summary>
    public bool IsLeaveBlocked => Personnel.Any(p => p.IsLeaveBlocked);

    /// <summary>Check if all personnel can beam away (not leave-blocked and not stopped).</summary>
    public bool CanBeamAway => Personnel.Count > 0 && Personnel.All(p => !p.IsLeaveBlocked && !p.Stopped);

    public bool Remove(int instanceId)
    {
        int p = Personnel.FindIndex(x => x.InstanceId == instanceId);
        if (p >= 0) { Personnel.RemoveAt(p); return true; }
        int e = Equipment.FindIndex(x => x.InstanceId == instanceId);
        if (e >= 0) { Equipment.RemoveAt(e); return true; }
        return false;
    }

    public IEnumerable<string> DumpLines()
    {
        string who = string.Join(", ",
            Personnel.Select(p => p.ToString()).Concat(Equipment.Select(e => e.ToString())));
        yield return $"{Kind} P{Controller} [{Personnel.Count}p/{Equipment.Count}e] {who}";
    }
}
