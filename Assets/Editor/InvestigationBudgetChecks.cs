using System;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class InvestigationBudgetChecks
{
    public static void Verify()
    {
        var clock = new InvestigationTime();
        Require(clock.Remaining == 12 && clock.Label.StartsWith("첫째 날 · 오전"), "Initial budget");
        for (int i = 0; i < 12; i++)
        {
            Require(clock.Complete("inquiry" + i, "조사"), "New investigation rejected");
            Require(!clock.Complete("inquiry" + i, "조사") && clock.ElapsedSlots == i + 1, "Repeat charged");
            if (i == 3) Require(clock.Label.StartsWith("둘째 날 · 오전"), "Day two transition");
            if (i == 7) Require(clock.Label.StartsWith("셋째 날 · 오전"), "Day three transition");
        }
        Require(clock.DeadlineReached && !clock.Complete("thirteenth", "추가") && clock.HasStarted("inquiry11"), "Deadline/revisit policy");
        Require(clock.HistoryPages == 2 && !string.IsNullOrEmpty(clock.HistoryPage(1)), "History pagination");
        Require(EndingRules.ResolveFinal(null, false, false, false, false) == InvestigationEnding.None, "Early unresolved report");
        Require(EndingRules.ResolveFinal(null, false, true, false, false) == InvestigationEnding.Unresolved, "Expired unresolved report");
        Require(EndingRules.ResolveFinal("설평", true, true, true, true) == InvestigationEnding.Unresolved, "Suspicion is not accusation");
        Require(EndingRules.ResolveFinal("설평", false, true, false, false) == InvestigationEnding.Normal, "Normal ending preserved");
        Require(EndingRules.ResolveFinal("설평", false, true, true, true) == InvestigationEnding.True, "True ending preserved");
        Require(EndingRules.ResolveFinal("두석", false, true, true, true) == InvestigationEnding.Duseok, "Wrong accusation preserved");
        Debug.Log("INVESTIGATION_BUDGET_VERIFIED: 12 starts, repeats, day boundaries, 13th blocked, report branches.");
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
