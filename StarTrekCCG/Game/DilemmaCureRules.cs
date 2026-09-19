using StarTrekCCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarTrekCCG;

/// <summary>
/// Compendium 7.2.2.3 & Dilemma Cure System (pure rules / decide logic — no WPF).
/// Cure occurs after conditions are met (conditions first, then cure).
/// Dilemmas with cure requirements can be cured at any time the conditions are satisfied
/// (e.g., right after being placed on a ship/mission/Away Team, or later when present personnel change).
/// </summary>
public static class DilemmaCureRules
{
    public enum CureAction
    {
        None,
        CureAndDiscard
    }

    public readonly record struct CurePlan(
        CureAction Action,
        int PointsAwarded,
        string LogMessage,
        string StatusMessage);

    /// <summary>
    /// Checks whether the persist dilemma can be cured given the present cards and mission completed state.
    /// Conditions must be evaluated first.
    /// </summary>
    public static bool CanCure(
        DilemmaRules.PersistKind kind,
        IEnumerable<Card> present,
        int owner,
        bool missionCompleted = false)
    {
        return DilemmaRules.CanCure(kind, present, owner, missionCompleted);
    }

    /// <summary>
    /// Evaluates if a cure plan should be executed for an attached dilemma.
    /// Returns CurePlan with Action = CureAndDiscard if cured, or Action = None.
    /// </summary>
    public static CurePlan DecideCure(
        DilemmaRules.PersistKind kind,
        string cardName,
        IEnumerable<Card> present,
        int owner,
        bool missionCompleted = false,
        bool dockedAtOutpost = false)
    {
        // REM Fatigue: Outpost dock is an alternate cure path (Captain/Spock LOCK).
        if (kind == DilemmaRules.PersistKind.RemFatigue && dockedAtOutpost)
        {
            return new CurePlan(
                CureAction.CureAndDiscard,
                5,
                $"REM Fatigue cured (docked at Outpost) +5 pts: {cardName}",
                $"{cardName} cured (Outpost dock).");
        }

        if (!CanCure(kind, present, owner, missionCompleted))
        {
            return new CurePlan(CureAction.None, 0, "", "");
        }

        int points = 0;
        if (EndOfTurnRestRules.CureAward(kind == DilemmaRules.PersistKind.HyperAging || kind == DilemmaRules.PersistKind.RemFatigue)
            == EndOfTurnRestRules.DilemmaCurePoints.Five)
        {
            points = 5;
        }

        string logMsg = kind switch
        {
            DilemmaRules.PersistKind.Abduction => $"Alien Abduction cured (Leadership x3 present or mission completed): {cardName}",
            DilemmaRules.PersistKind.Menthar => $"Menthar Booby Trap cured (2 ENGINEER): {cardName}",
            DilemmaRules.PersistKind.Phased => $"Phased Matter cured (ENGINEER + SCIENCE): {cardName}",
            DilemmaRules.PersistKind.Ktarian => $"Ktarian Game cured (CUNNING>30 or Android): {cardName}",
            DilemmaRules.PersistKind.HyperAging => $"Hyper-Aging cured (SCIENCE + 2 MEDICAL) +5 pts: {cardName}",
            DilemmaRules.PersistKind.RemFatigue => $"REM Fatigue cured (3 MEDICAL) +5 pts: {cardName}",
            DilemmaRules.PersistKind.Nitrium => $"Nitrium Metal Parasites cured (2 SCIENCE or 2 ENGINEER): {cardName}",
            DilemmaRules.PersistKind.Junior => $"Birth of \"Junior\" nullified (3 ENGINEER): {cardName}",
            DilemmaRules.PersistKind.Tsiolkovsky => $"Tsiolkovsky Infection cured (3 MEDICAL): {cardName}",
            DilemmaRules.PersistKind.TwoDim => $"Two-Dimensional Creatures cured (ENGINEER + SCIENCE): {cardName}",
            DilemmaRules.PersistKind.FrameOfMind => $"Frame of Mind cured (3 Empathy): {cardName}",
            _ => $"Cured {cardName}"
        };

        string statusMsg = $"{cardName} cured.";

        return new CurePlan(CureAction.CureAndDiscard, points, logMsg, statusMsg);
    }

    /// <summary>
    /// Verification test for DilemmaCureRules decide logic.
    /// </summary>
    public static string? VerifyDilemmaCureRules()
    {
        static Card P(string name, string cls, string text, string cunn = "5") => new()
        {
            Name = name,
            Type = "Personnel",
            Class = cls,
            Text = text,
            Characteristics = "Human; Male;",
            IntegrityOrRange = "5",
            CunningOrWeapons = cunn,
            StrengthOrShields = "5"
        };

        var eng1 = P("Eng1", "ENGINEER", "ENGINEER");
        var eng2 = P("Eng2", "ENGINEER", "ENGINEER");
        var lead1 = P("Lead1", "OFFICER", "Leadership");
        var lead2 = P("Lead2", "OFFICER", "Leadership");
        var lead3 = P("Lead3", "OFFICER", "Leadership x2");

        // Menthar test: 2 ENGINEER cures
        var mentharPlan = DecideCure(DilemmaRules.PersistKind.Menthar, "Menthar Booby Trap", new[] { eng1, eng2 }, 1);
        if (mentharPlan.Action != CureAction.CureAndDiscard)
            return "Menthar: expected CureAndDiscard with 2 ENGINEER";

        var mentharFail = DecideCure(DilemmaRules.PersistKind.Menthar, "Menthar Booby Trap", new[] { eng1 }, 1);
        if (mentharFail.Action != CureAction.None)
            return "Menthar: 1 ENGINEER should not cure";

        // Abduction test: 3 Leadership cures OR missionCompleted
        var abdFail = DecideCure(DilemmaRules.PersistKind.Abduction, "Alien Abduction", new[] { lead1 }, 1, missionCompleted: false);
        if (abdFail.Action != CureAction.None)
            return "Abduction: 1 Leadership should not cure";

        var abdSuccessLead = DecideCure(DilemmaRules.PersistKind.Abduction, "Alien Abduction", new[] { lead1, lead3 }, 1, missionCompleted: false);
        if (abdSuccessLead.Action != CureAction.CureAndDiscard)
            return "Abduction: 3 Leadership should cure";

        var abdSuccessMission = DecideCure(DilemmaRules.PersistKind.Abduction, "Alien Abduction", Array.Empty<Card>(), 1, missionCompleted: true);
        if (abdSuccessMission.Action != CureAction.CureAndDiscard)
            return "Abduction: missionCompleted should cure even with no personnel";

        // Hyper-Aging points test
        var sci = P("Sci", "SCIENCE", "SCIENCE");
        var med1 = P("Med1", "MEDICAL", "MEDICAL");
        var med2 = P("Med2", "MEDICAL", "MEDICAL");
        var hyperPlan = DecideCure(DilemmaRules.PersistKind.HyperAging, "Hyper-Aging", new[] { sci, med1, med2 }, 1);
        if (hyperPlan.Action != CureAction.CureAndDiscard || hyperPlan.PointsAwarded != 5)
            return "Hyper-Aging: expected CureAndDiscard with 5 points";

        var remMed3 = P("RemMed3", "MEDICAL", "MEDICAL");
        var remPlan = DecideCure(DilemmaRules.PersistKind.RemFatigue, "REM Fatigue", new[] { med1, med2, remMed3 }, 1);
        if (remPlan.Action != CureAction.CureAndDiscard || remPlan.PointsAwarded != 5)
            return "REM Fatigue: expected CureAndDiscard with 5 points (3 MEDICAL)";
        var remDock = DecideCure(DilemmaRules.PersistKind.RemFatigue, "REM Fatigue", Array.Empty<Card>(), 1, dockedAtOutpost: true);
        if (remDock.Action != CureAction.CureAndDiscard || remDock.PointsAwarded != 5)
            return "REM Fatigue: Outpost dock should cure +5";
        var remFail = DecideCure(DilemmaRules.PersistKind.RemFatigue, "REM Fatigue", new[] { med1 }, 1);
        if (remFail.Action != CureAction.None)
            return "REM Fatigue: 1 MEDICAL should not cure";

        // Frame of Mind test: 3 Empathy cures
        var emp1 = P("Emp1", "CIVILIAN", "Empathy x2");
        var emp2 = P("Emp2", "CIVILIAN", "Empathy");
        var fomPlan = DecideCure(DilemmaRules.PersistKind.FrameOfMind, "Frame of Mind", new[] { emp1, emp2 }, 1);
        if (fomPlan.Action != CureAction.CureAndDiscard)
            return "Frame of Mind: 3 Empathy should cure";

        // Abduction: victim in stasis must be excluded from present cards for cure check
        var kirkWith3Lead = P("Kirk", "OFFICER", "Leadership x3", "10");
        var presentWithKirkHeld = new[] { kirkWith3Lead, lead1 };
        var presentExcludingKirk = DilemmaRules.ExcludeHeld(presentWithKirkHeld, new[] { kirkWith3Lead });
        var abdSelfCureBlocked = DecideCure(DilemmaRules.PersistKind.Abduction, "Alien Abduction", presentExcludingKirk, 1, missionCompleted: false);
        if (abdSelfCureBlocked.Action != CureAction.None)
            return "Abduction: victim in stasis should not be able to self-cure";

        // Two-Dimensional Creatures test: ENGINEER + SCIENCE
        var twoDimPlan = DecideCure(DilemmaRules.PersistKind.TwoDim, "Two-Dimensional Creatures", new[] { eng1, sci }, 1);
        if (twoDimPlan.Action != CureAction.CureAndDiscard)
            return "TwoDim: expected CureAndDiscard with ENG+SCI";

        // Tsiolkovsky test: 3 MEDICAL
        var med3 = P("Med3", "MEDICAL", "MEDICAL");
        var tsioPlan = DecideCure(DilemmaRules.PersistKind.Tsiolkovsky, "Tsiolkovsky Infection", new[] { med1, med2, med3 }, 1);
        if (tsioPlan.Action != CureAction.CureAndDiscard)
            return "Tsiolkovsky: expected CureAndDiscard with 3 MEDICAL";

        return null;
    }
}
