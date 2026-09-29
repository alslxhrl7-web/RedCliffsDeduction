using System;
using RedCliffMystery.Investigation;
using UnityEditor;
using UnityEngine;

public static class EndingRuleChecks
{
    [MenuItem("적벽추리/엔딩 조건 확인 (실행 없음)")]
    public static void Verify()
    {
        int count = 0;
        foreach (bool registry in new[] { false, true })
            foreach (bool letter in new[] { false, true })
            {
                Check("설평", registry, letter, registry && letter ? InvestigationEnding.True : InvestigationEnding.Normal); count++;
                Check("채모", registry, letter, InvestigationEnding.Caimao); count++;
                Check("장윤", registry, letter, InvestigationEnding.Zhangyun); count++;
                Check("두석", registry, letter, InvestigationEnding.Duseok); count++;
                Check(null, registry, letter, InvestigationEnding.None); count++;
                Check("미정", registry, letter, InvestigationEnding.None); count++;
            }
        Debug.Log("ENDING_RULES_VERIFIED cases=" + count);
    }

    static void Check(string suspect, bool registry, bool letter, InvestigationEnding expected)
    {
        if (EndingRules.Resolve(suspect, registry, letter) != expected)
            throw new InvalidOperationException("Ending rule mismatch: " + suspect);
    }
}
