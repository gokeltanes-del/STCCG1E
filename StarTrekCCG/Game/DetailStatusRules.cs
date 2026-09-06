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
        if (DilemmaRules.IsStasisPersist(kind))
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

    public static string FormatHeldStasisSectionLine(string dilemmaName, string personnelNames) =>
        $"Held/Stasis: {dilemmaName} — {personnelNames}";
}
