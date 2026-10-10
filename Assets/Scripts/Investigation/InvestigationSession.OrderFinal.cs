using System;
using System.Collections.Generic;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public OrderFinalReport OrderReport { get; private set; } = new OrderFinalReport();
        static readonly string[] OrderSuspects = { "변경 사실만 보고 · 범인 미확정", "한진", "전달자", "설평", "두석", "채모", "장윤" };
        // Index 6 is a decoy (access record): it does not prove alteration or intent and must not be submitted with the six evidence items.
        static readonly string[] OrderProofNames = { "회수본과 확인표의 지시 불일치", "출처가 확인된 부본과 변경 승인 대장", "작업 의뢰와 반납·출고를 연결한 제출 기록", "거절 회신의 수령과 완성물 반납 순서", "본진 철수로 생기는 강 입구 감시 공백", "전달자의 반입 기록과 문서 회수 경위", "문서실 출입 기록과 한진의 취급 권한" };
        static readonly string[] OrderActions = { "별도 시행 조치를 요청하지 않는다", "제17호·대체 재발행 중지와 강 입구 배치 복원", "제17호·대체 재발행 중지만 요청", "회수본대로 본진 철수를 요청" };
        const int OrderProofPageSize = 3;
        static readonly int OrderProofPageCount = (OrderProofNames.Length + OrderProofPageSize - 1) / OrderProofPageSize;

        void OrderFinalModal(string title, string body)
        {
            SetSavePoint("order_final");
            SetModal("조조 · 최종 보고", title, body, FinalReportCompleted
                ? "최종 보고 확정 · 아래 버튼으로 보고 직전 기록을 불러올 수 있습니다."
                : "F5 저장 / F9 불러오기 · 확정 전 수정 가능");
            onEscape = FinalReportCompleted ? null : CancelOrderFinal;
        }
        public void BeginOrderFinal()
        {
            if (!introductionFinished) return;
            if (FinalReportCompleted) { ShowOrderEnding(); return; }
            if (!reportActive)
            {
                OrderReport.fromNaval = visitingCommandTent ? tentVisitFromNaval : route.AtNavalCamp;
                introductionStage.Begin();
                visitingCommandTent = false; reportActive = true;
                deadlineReportLocked = investigationTime.DeadlineReached;
                view.SetPlayerIdentity(PlayerName, "조조군 지휘 천막");
            }
            OrderReport.stage = 1;
            PlaytestObserver.ReportOpened();
            ShowOrderFinalConclusion();
        }
        void ResumeOrderFinal()
        {
            introductionStage.Begin(); visitingCommandTent = false; reportActive = true;
            deadlineReportLocked = investigationTime.DeadlineReached;
            view.SetPlayerIdentity(PlayerName, "조조군 지휘 천막");
            if (OrderReport.stage == 2) {
                FinalReportCompleted = true; FinalReportSummary = OrderFinalSummary();
                var ending = OrderReport.Resolve(OrderState);
                CurrentEnding = ending == OrderReportEnding.True ? InvestigationEnding.True : ending == OrderReportEnding.Normal ? InvestigationEnding.Normal : InvestigationEnding.Unresolved;
                ShowOrderEnding();
            }
            else { OrderReport.stage = 1; ShowOrderFinalReview(); }
        }
        void CancelOrderFinal()
        {
            if (FinalReportCompleted) return;
            if (deadlineReportLocked) { ShowOrderFinalReview(); return; }
            reportActive = false; visitingCommandTent = false; OrderReport.stage = 0;
            PlaytestObserver.ReportCancelled();
            introductionStage.EnterArchive();
            if (OrderReport.fromNaval) route.EnterNavalCamp(); else route.EnterArchive();
            view.SetPlayerIdentity(PlayerName, LocationName); UpdateObjective(); CloseModal();
        }
        void ShowOrderFinalConclusion()
        {
            OrderFinalModal("1 / 3 · 누구의 어떤 책임인가", "“문서를 숨긴 책임과 본문을 바꿔 제출한 책임을 구분해서 보고하라.”\n\n현재 결론: " + OrderSuspects[OrderReport.suspect]);
            // Neutral wording on both accusations: the correct one is not identifiable by its sentence shape.
            view.AddChoice("한진을 책임자로 지목", () => SelectOrderSuspect(1));
            view.AddChoice("전달자를 책임자로 지목", () => SelectOrderSuspect(2));
            view.AddChoice("다른 인물을 지목", ShowOrderOtherSuspects);
            view.AddChoice("변경 사실만 보고 · 범인 미확정", () => SelectOrderSuspect(0));
            if (!deadlineReportLocked) view.AddChoice("제출하지 않고 조사로 돌아가기", CancelOrderFinal);
        }
        void ShowOrderOtherSuspects()
        {
            OrderFinalModal("지목할 인물", "접근 권한이나 개인 사정만으로 군령 변경을 입증할 수는 없다.");
            for (int i = 3; i < OrderSuspects.Length; i++) { int selected = i; view.AddChoice(OrderSuspects[i], () => SelectOrderSuspect(selected)); }
            view.AddChoice("결론으로", ShowOrderFinalConclusion);
        }
        void SelectOrderSuspect(int suspect)
        {
            if (OrderReport.suspect != suspect) PlaytestObserver.ReportEdited("결론");
            OrderReport.suspect = suspect; ShowOrderFinalProofs(0);
        }
        void ShowOrderFinalProofs(int page)
        {
            OrderFinalModal("2 / 3 · 제출할 근거 " + (page + 1) + " / " + OrderProofPageCount, "수집한 자료 중 이번 보고에 사용할 근거를 선택한다. 선택하지 않은 근거는 판정에 포함되지 않는다. 확보하지 못한 근거는 이름이 보이지 않는다.\n\n결론: " + OrderSuspects[OrderReport.suspect]);
            var available = OrderFinalReport.Available(OrderState);
            int start = page * OrderProofPageSize;
            int end = Math.Min(start + OrderProofPageSize, OrderProofNames.Length);
            for (int i = start; i < end; i++)
            {
                var proof = (OrderReportProof)(1 << i);
                bool owned = (available & proof) != 0;
                // Unowned proofs stay hidden so the list cannot double as a checklist of what to submit.
                string label = owned
                    ? (OrderReport.Has(proof) ? "[제출] " : "[미선택] ") + OrderProofNames[i]
                    : "[미확보] 아직 찾지 못한 근거";
                view.AddChoice(label, () => { OrderReport.proofs ^= (int)proof; PlaytestObserver.ReportEdited("근거"); ShowOrderFinalProofs(page); }, owned);
            }
            if (page < OrderProofPageCount - 1) view.AddChoice("다음 근거 묶음", () => ShowOrderFinalProofs(page + 1));
            if (page > 0) view.AddChoice("이전 근거 묶음", () => ShowOrderFinalProofs(page - 1));
            view.AddChoice("조치 선택으로", ShowOrderFinalAction);
        }
        void ShowOrderFinalAction()
        {
            OrderFinalModal("3 / 3 · 시행 조치", "“보고한 근거에 따라 무엇을 바로잡아야 하느냐?”\n\n현재 요청: " + OrderActions[OrderReport.action]);
            for (int i = 0; i < OrderActions.Length; i++) { int action = i; view.AddChoice(OrderActions[i], () => { if (OrderReport.action != action) PlaytestObserver.ReportEdited("조치"); OrderReport.action = action; ShowOrderFinalReview(); }); }
            view.AddChoice("근거 수정", () => ShowOrderFinalProofs(0));
        }
        string OrderFinalSummary()
        {
            var lines = new List<string> { "결론: " + OrderSuspects[OrderReport.suspect], "제출 근거:" };
            for (int i = 0; i < OrderProofNames.Length; i++) if (OrderReport.Has((OrderReportProof)(1 << i))) lines.Add("· " + OrderProofNames[i]);
            if (OrderReport.proofs == 0) lines.Add("· 없음");
            lines.Add("요청: " + OrderActions[OrderReport.action]);
            return string.Join("\n", lines);
        }
        void ShowOrderFinalReview()
        {
            OrderFinalModal("최종 제출 전 확인", OrderFinalSummary() + "\n\n최종 제출하면 이번 조사가 끝나고 이 보고에 따른 엔딩으로 진행한다.");
            view.AddChoice("최종 제출 · 엔딩으로", CompleteOrderFinal);
            view.AddChoice("제출 근거 수정", () => ShowOrderFinalProofs(0));
            view.AddChoice("결론 수정", ShowOrderFinalConclusion);
            view.AddChoice("시행 조치 수정", ShowOrderFinalAction);
            if (!deadlineReportLocked) view.AddChoice("제출하지 않고 조사로 돌아가기", CancelOrderFinal);
        }
        string OrderCommandText()
        {
            // The command text follows the requested action, so each action is distinguishable on screen.
            if (OrderReport.HoldsOrder) return "“제17호와 이를 대신하는 재발행을 중지하라.”";
            if (OrderReport.action == 3) return "“회수본대로 본진을 철수하라.”\n\n보고는 조작된 명령의 내용을 확인했지만, 그 명령을 취소하는 조치는 내려지지 않았다.";
            if (OrderReport.action == 0 && OrderReport.EvidenceComplete) return "“보고는 확인했다. 그러나 제17호를 멈추라는 명령은 내리지 않는다.”";
            return "제17호의 변경을 바로잡는 데 충분한 근거와 중지 요청이 보고에 갖춰지지 않았다.";
        }
        void CompleteOrderFinal()
        {
            if (!reportActive || FinalReportCompleted) return;
            OrderReport.Validate(OrderState);
            // Loading a manual slot updates the signature without writing the auto slot.
            // Force a real pre-ending checkpoint even when that draft has not changed.
            lastAutoSaveSignature = null; failedAutoSaveSignature = null;
            AutoSaveProgress(); // Preserve the editable report before committing the ending.
            FinalReportSummary = OrderFinalSummary();
            OrderReport.stage = 2; FinalReportCompleted = true;
            var ending = OrderReport.Resolve(OrderState);
            CurrentEnding = ending == OrderReportEnding.True ? InvestigationEnding.True : ending == OrderReportEnding.Normal ? InvestigationEnding.Normal : InvestigationEnding.Unresolved;
            view.SetObjective("최종 보고 완료 · 제출한 근거에 따른 결말");
            var submittedProofs = new List<string>();
            for (int i = 0; i < OrderProofNames.Length; i++) if (OrderReport.Has((OrderReportProof)(1 << i))) submittedProofs.Add(OrderProofNames[i]);
            PlaytestObserver.ReportSubmitted(OrderSuspects[OrderReport.suspect], submittedProofs, OrderReport.Has(OrderReportProof.AccessRecord),
                OrderActions[OrderReport.action], ending == OrderReportEnding.True ? "TRUE" : ending == OrderReportEnding.Normal ? "NORMAL" : "BAD");
            OrderFinalModal("조조의 명령", OrderCommandText()
                + "\n\n" + (OrderReport.RestoresDeployment ? "강 입구 유지 지시를 부대에 재확인하고 현지 배치를 복원하라는 명령이 내려진다." : "강 입구 배치를 복원하는 조치는 확정되지 않았다."));
            view.AddChoice("명령이 전해진 뒤", ShowOrderEnding);
        }
        string NormalEndingDetail()
        {
            if (OrderReport.RestoresDeployment && OrderReport.HoldsOrder) return "강 입구 배치는 복원됐지만 변경자의 책임이나 사건 경위에는 미해결 부분이 남았다.";
            if (OrderReport.HoldsOrder) return "의심스러운 군령은 멈췄지만 부대에 무엇을 시행할지 완전하게 전달되지 않았다. 대응은 지연됐다.";
            if (OrderReport.action == 3) return "회수본대로 철수 명령이 시행됐다. 강 입구의 감시선이 빠져 접근하는 선단을 놓칠 위험이 커졌다.";
            if (OrderReport.action == 0 && OrderReport.EvidenceComplete) return "근거는 모두 맞혔지만 군령을 멈추는 요청이 없어 잘못된 군령이 그대로 시행된다.";
            return "보고는 기록됐지만 잘못된 군령의 시행 위험을 끝내 제거하지 못했다.";
        }
        void ShowOrderEnding()
        {
            var ending = OrderReport.Resolve(OrderState);
            string title = ending == OrderReportEnding.True ? "TRUE END · 지켜낸 강 입구" : ending == OrderReportEnding.Normal ? "NORMAL END · 남겨진 공백" : "BAD END · 빗나간 보고";
            string result = ending == OrderReportEnding.True
                ? "한진의 제출과 거절 회신 수령 순서가 입증된다. 문서를 숨긴 전달자의 책임은 별도로 기록된다.\n\n잘못된 군령과 대체 재발행이 멈춘다. 강 입구에 남은 감시선이 접근하는 선단을 발견하고 경고를 전한다. 당신은 제17호가 만들 감시 공백을 막았다."
                : ending == OrderReportEnding.Normal
                    ? "제출한 근거로 군령 변경의 일부는 밝혀졌다. 그러나 책임 입증과 명령 복원·저지까지 완성된 보고는 아니었다.\n\n" + NormalEndingDetail()
                    : (OrderReport.suspect >= 2 ? "다른 행위의 책임을 군령 변경의 죄로 묶었다. 지목을 뒷받침하는 근거가 없어 조사가 빗나갔다." : OrderReport.suspect == 1 ? "이름을 맞혔지만 제출과 고의를 뒷받침할 근거를 보고하지 못했다." : "군령 변경을 입증할 근거가 보고에 제시되지 않았다.")
                        + "\n\n" + (OrderReport.RestoresDeployment && OrderReport.HoldsOrder ? "배치 조치는 감시 공백을 막았지만 근거 없는 지목은 남았다. 한 사람의 책임을 올바르게 묻는 데 실패했다." : "지휘부는 사건을 바로잡지 못했다. 현장의 혼선과 늦어진 경고 속에 이번 조사는 실패로 끝났다.");
            OrderFinalModal(title, result);
            view.AddChoice("제출한 보고서 보기", () => { OrderFinalModal("확정한 보고서", FinalReportSummary ?? OrderFinalSummary()); view.AddChoice("엔딩으로", ShowOrderEnding); });
            view.AddChoice("보고 직전 기록 불러오기", () => ConfirmLoadPath(InvestigationSaveStore.AutoSavePath, ShowOrderEnding));
            view.AddChoice("처음부터 다시 시작", ConfirmRestart);
        }
    }
}
