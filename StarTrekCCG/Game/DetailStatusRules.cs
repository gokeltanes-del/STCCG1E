using System;

namespace StarTrekCCG;

/// <summary>
/// Card-detail Status-Block tones (Buff / Debuff / Timer / Stasis). Pure decide — no WPF.
/// </summary>
public enum DetailStatusTone
{
    Buff,
    Debuff,
    Timer,
    Stasis,
    Info
}

public static class DetailStatusRules
{
    public static DetailStatusTone ToneForDilemma(DilemmaRules.PersistKind kind, int countdown)
    {
        if (DilemmaRules.IsStasisPersist(kind) || DilemmaRules.IsQuarantinePersist(kind))
            return DetailStatusTone.Stasis;
        if (countdown > 0)
            return DetailStatusTone.Timer;
        return DetailStatusTone.Debuff;
    }

    public static DetailStatusTone ToneForEvent(EventRules.Persist kind, int countdown)
    {
        if (countdown > 0)
            return DetailStatusTone.Timer;
        return kind switch
        {
            EventRules.Persist.Metaphasic
                or EventRules.Persist.Nutational
                or EventRules.Persist.Bynars
                or EventRules.Persist.Spacedock
                or EventRules.Persist.CaptainsLog
                or EventRules.Persist.Distortion
                or EventRules.Persist.YellowAlert
                => DetailStatusTone.Buff,
            EventRules.Persist.PlasmaFire
                or EventRules.Persist.WarpCore
                or EventRules.Persist.Baryon
                or EventRules.Persist.NeuralServo
                or EventRules.Persist.ParticleScatter
                or EventRules.Persist.LoreReturns
                or EventRules.Persist.Thermal
                or EventRules.Persist.IncomingMessage
                => DetailStatusTone.Debuff,
            _ => DetailStatusTone.Info
        };
    }

    public static string FormatInStasisLine(string dilemmaName, string? cureHint = null)
    {
        string name = string.IsNullOrWhiteSpace(dilemmaName) ? "stasis" : dilemmaName.Trim();
        if (string.IsNullOrWhiteSpace(cureHint))
            return $"In stasis ({name})";
        return $"In stasis ({name} — {cureHint})";
    }

    public static string StasisCureHint(DilemmaRules.PersistKind kind) => kind switch
    {
        DilemmaRules.PersistKind.Abduction => "cure: Leadership x3 OR mission completed",
        DilemmaRules.PersistKind.Phased => "cure: ENGINEER + SCIENCE",
        _ => ""
    };

    public static string FormatDisabledLine(string dilemmaName, string? cureHint = null)
    {
        string name = string.IsNullOrWhiteSpace(dilemmaName) ? "disabled" : dilemmaName.Trim();
        if (string.IsNullOrWhiteSpace(cureHint))
            return $"Disabled ({name})";
        return $"Disabled ({name} — {cureHint})";
    }

    public static string DisabledCureHint(DilemmaRules.PersistKind kind) => kind switch
    {
        DilemmaRules.PersistKind.Ktarian => "cure: CUNNING>30 OR Android",
        _ => ""
    };

    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>
        $"Held/Stasis: {dilemmaName} — {personnelNames}";

    public static string FormatQuarantineLine(string dilemmaName, string? cureHint = null)
    {
        string name = string.IsNullOrWhiteSpace(dilemmaName) ? "quarantine" : dilemmaName.Trim();
        if (string.IsNullOrWhiteSpace(cureHint))
            return $"Quarantined ({name}) — cannot leave/beam away";
        return $"Quarantined ({name} - {cureHint}) — cannot leave/beam away";
    }

    public static string QuarantineCureHint(DilemmaRules.PersistKind kind) => kind switch
    {
        DilemmaRules.PersistKind.HyperAging => "cure: SCIENCE + 2 MEDICAL",
        _ => ""
    };

    public static string FormatHeldQuarantineSectionLine(string dilemmaName, string personnelNames) =>
        $"Quarantine: {dilemmaName} — {personnelNames}";

    /// <summary>
    /// Away-Team detail groups: same labels = same group.
    /// Order inside a set: Disabled, Quarantined, Stasis, Stopped.
    /// Empty list = no negative personnel effect.
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<string> PersonnelNegEffects(bool stopped, bool stasis, bool quarantined, bool disabled = false)
    {
        var list = new System.Collections.Generic.List<string>(4);
        if (disabled) list.Add("Disabled");
        if (quarantined) list.Add("Quarantined");
        if (stasis) list.Add("Stasis");
        if (stopped) list.Add("Stopped");
        return list;
    }

    public static string EffectGroupKey(System.Collections.Generic.IReadOnlyList<string> effects) =>
        string.Join("|", effects ?? System.Array.Empty<string>());

    public static string FormatEffectGroupHeader(System.Collections.Generic.IReadOnlyList<string> effects, int count)
    {
        string label = (effects == null || effects.Count == 0)
            ? "Personnel"
            : string.Join(" + ", effects);
        return $"{label} ({count})";
    }

    public static bool IsRelocateContinue(string? cardName, string? message)
    {
        string n = cardName ?? "";
        string m = message ?? "";
        return n.IndexOf("Love Interest", StringComparison.OrdinalIgnoreCase) >= 0
            || m.IndexOf("relocat", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string EffectContinueHeader(string? cardName, string? message) =>
        IsRelocateContinue(cardName, message)
            ? "RELOCATED - attempt continues"
            : "EFFECT - attempt continues";

    public static string EffectContinueLogVerb(string? cardName, string? message) =>
        IsRelocateContinue(cardName, message) ? "RELOCATED" : "EFFECT";
}
