namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public bool FinalReportCompleted { get; private set; }
        public string FinalReportSummary { get; private set; }
        bool reportActive;
        bool visitingCommandTent;
        bool tentVisitFromNaval;
        bool tentVisitFromSupply;
        public void TalkToCaoCaoOnFoot()
        {
            if(!introductionFinished || reportActive || FinalReportCompleted) return;
            SetSavePoint("field");
            SetModal("조조군 지휘 천막", "조조", "“확인한 내용을 보고하겠느냐?”", "Esc 대화 닫기 · 천막 입구로 걸어 나갈 수 있습니다.");
            view.AddChoice("조사 결과 보고", BeginFinalReport, CanReportNow);
            view.AddChoice("대화를 마치고 걸어서 돌아가기", CloseModal);
        }

        public void VisitCommandTent()
        {
            if(route!=null && route.AtCommandTent && !visitingCommandTent) { TalkToCaoCaoOnFoot(); return; }
            SetSavePoint("tent");
            if (!introductionFinished || reportActive || FinalReportCompleted) return;
            if (introductionStage == null || introductionStage.player == null || introductionStage.tent == null
                || introductionStage.tentSpawn == null || introductionStage.archiveSpawn == null || route == null || !route.IsReady)
            {
                SetModal("이동", "지휘 천막으로 이동할 수 없음", "천막과 이동 경로 연결을 확인해야 합니다. 조사 기록은 유지됩니다.");
                view.AddChoice("조사 계속", CloseModal);
                return;
            }
            tentVisitFromNaval = route.AtNavalCamp;
            tentVisitFromSupply = route.AtSupplyTent;
            visitingCommandTent = true;
            introductionStage.Begin();
            view.SetPlayerIdentity(PlayerName, LocationName);
            view.SetObjective("조조에게 보고하거나 조사 장소로 돌아갈 수 있다");
            SetModal("조조군 지휘 천막", "조조", "“돌아왔느냐. 확인한 내용을 보고하겠느냐, 아니면 조사를 더 이어가겠느냐?”",
                "Esc 이전 조사 장소로 복귀 · 이동에는 시간이 소모되지 않습니다.");
            view.AddChoice("조사 결과 보고", BeginFinalReport, CanReportNow);
            view.AddChoice("수군 진영으로 돌아가기", () => LeaveCommandTent(true));
            view.AddChoice("문서고로 돌아가기", () => LeaveCommandTent(false));
        }

        void LeaveCommandTent(bool naval, bool supply = false)
        {
            visitingCommandTent = false;
            introductionStage.EnterArchive();
            if (supply && route.SupplyTentReady) route.EnterSupplyTent();
            else if (naval) route.EnterNavalCamp(); else route.EnterArchive();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        public void BeginFinalReport() => BeginOrderFinal();
        void CancelFinalReport() => CancelOrderFinal();
    }
}
