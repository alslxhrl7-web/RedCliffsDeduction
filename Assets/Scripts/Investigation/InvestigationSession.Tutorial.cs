using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        // 0: inactive/skipped, 1: move/look, 2: interact, 3: read, 4: choose, 5: complete.
        public int TutorialStep { get; private set; }
        Vector3 tutorialOrigin;
        float tutorialYaw;
        bool tutorialMoved, tutorialLooked, comparisonTipSeen;
        void ShowTutorialOffer()
        {
            SetModal("첫 조사", "조사를 시작하기 전에",
                "문서고에서 이동과 첫 조사를 익힙니다.\n조작을 안다면 바로 수사를 시작하세요.");
            view.AddChoice("첫 조사 배우기", StartInvestigationTutorial);
            view.AddChoice("안내 건너뛰고 자유 수사", SkipInvestigationTutorial);
            onEscape=SkipInvestigationTutorial;
        }
        public void StartInvestigationTutorial()
        {
            TutorialStep=1; ResetTutorialMovement(); CloseModal(); nextGuidanceRefresh=0;
        }
        void ResetTutorialMovement()
        {
            tutorialMoved=tutorialLooked=false;
            if(route!=null && route.player!=null) { tutorialOrigin=route.player.transform.position; tutorialYaw=route.player.transform.eulerAngles.y; }
        }
        public void SkipInvestigationTutorial()
        {
            TutorialStep=0; CloseModal(); nextGuidanceRefresh=0;
        }
        void UpdateInvestigationTutorial()
        {
            if(TutorialStep!=1 || !CanProcessInvestigationInput || route==null || route.player==null)return;
            var delta=route.player.transform.position-tutorialOrigin;delta.y=0;
            tutorialMoved |= delta.sqrMagnitude>.36f;
            tutorialLooked |= Mathf.Abs(Mathf.DeltaAngle(tutorialYaw,route.player.transform.eulerAngles.y))>12;
            if(tutorialMoved && tutorialLooked) {TutorialStep=2;nextGuidanceRefresh=0;}
        }
        public void ObserveTutorialInteraction(InvestigationTarget.TargetKind kind)
        {
            if(TutorialStep==2 && kind!=InvestigationTarget.TargetKind.NavalCampExit && kind!=InvestigationTarget.TargetKind.ArchiveReturn && kind!=InvestigationTarget.TargetKind.CaoCao)
                TutorialStep=3;
        }
        void ObserveTutorialRecord()
        {
            if(TutorialStep==2 || TutorialStep==3) {TutorialStep=4;nextGuidanceRefresh=0;}
            if(!comparisonTipSeen && OrderState.CanCompare) {
                comparisonTipSeen=true;
                guidanceNotice+="\n자료 비교 가능 · J 수첩";noticeSeconds=10;
            }
        }
        void CompleteTutorialSelection(string id)
        {
            if(TutorialStep!=4 || string.IsNullOrEmpty(id))return;
            TutorialStep=5;
            RecordGuidanceNotice("자유 수사 시작", "다음 조사는 자유롭게 선택하세요.");
        }
        string TutorialHint()
        {
            switch(TutorialStep) {
                case 1: return "첫 조사 1/4\n"+(!tutorialMoved ? "WASD로 걸어보세요." : "마우스로 좌우를 둘러보세요.");
                case 2: return "첫 조사 2/4\n사람이나 보관함에 다가가 E를 누르세요.";
                case 3: return "첫 조사 3/4\n대화나 자료의 확인 항목을 하나 읽어보세요.";
                case 4: return "첫 조사 4/4\nQ를 눌러 다음 조사 대상을 고르세요.";
                default: return "";
            }
        }
        public void ShowInvestigationHelp()
        {
            SetModal("조사 방법", "필요할 때 다시 확인하세요",
                "WASD 이동 · 마우스 시점 · E 조사/대화\nQ 조사 대상 선택 · J 기록/비교 · Esc 메뉴\n\n대상 선택·이동·재열람·비교는 무료입니다. 새 조사 묶음은 현장에서 시작을 확정할 때 1회 사용합니다. 같은 묶음의 질문에는 추가 차감이 없습니다.\n\n진술은 인물이 한 말입니다. 다른 진술이나 자료와 맞는지 직접 판단하세요. 모든 대상을 만날 필요는 없습니다.\n\n비교할 자료가 모이면 J 수첩에서 대조할 수 있습니다. 최종 보고는 조조에게 결론과 근거를 제출하는 단계입니다.");
            view.AddChoice("조작 안내 다시 시작",StartInvestigationTutorial);
            if(TutorialStep>0 && TutorialStep<5)view.AddChoice("안내 종료하고 자유 수사",SkipInvestigationTutorial);
            view.AddChoice("조사 계속",CloseModal);
        }
        void SkipOpeningBriefing()
        {
            var complete=onSequenceComplete; sequence=null;onSequenceComplete=null;onAdvance=null;
            VarcoIdleMotion.StopSpeech();complete?.Invoke();
        }
    }
}
