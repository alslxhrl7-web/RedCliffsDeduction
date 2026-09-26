using UnityEditor;

namespace RedCliffMystery.EditorTools
{
    public static class FirstInvestigationChecks
    {
        [MenuItem("적벽추리/첫 조사 장면 실행 검사")]
        public static void Run() => InvestigationRouteChecks.RunTestimonyFirst();

        [MenuItem("적벽추리/첫 조사 장면 실행 검사 (문서 먼저)")]
        public static void RunDocumentFirst() => InvestigationRouteChecks.RunDocumentFirst();
    }
}
