using System.Collections.Generic;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public bool FinalReportCompleted { get; private set; }
        public string FinalReportSummary { get; private set; }
        bool reportActive;
        bool reportFromNavalCamp;
        bool reportUncertain;
        string reportSuspect;
        string reportRequest;
        int evidencePage;
        readonly List<ReportEvidence> reportEvidence = new List<ReportEvidence>();
        readonly HashSet<int> submittedEvidence = new HashSet<int>();

        sealed class ReportEvidence
        {
            public string title, text, response;
            public ReportEvidence(string title, string text, string response)
            { this.title = title; this.text = text; this.response = response; }
        }

        public void BeginFinalReport()
        {
            if (!introductionFinished || !OfficersHeard || reportActive || FinalReportCompleted) return;
            if (introductionStage == null || introductionStage.player == null
                || introductionStage.tent == null || introductionStage.tentSpawn == null
                || introductionStage.archiveSpawn == null || route == null || !route.IsReady)
            {
                SetModal("보고 준비", "지휘 천막으로 이동할 수 없음", "천막과 이동 경로 연결을 확인해야 합니다. 조사 기록은 유지됩니다.");
                view.AddChoice("조사로 돌아가기", CloseModal);
                return;
            }
            reportFromNavalCamp = route.AtNavalCamp;
            reportActive = true;
            reportSuspect = null;
            reportUncertain = false;
            reportRequest = "현재 결과로 보고를 마친다";
            submittedEvidence.Clear();
            CollectReportEvidence();
            introductionStage.Begin();
            // Keep the wider investigation layout for evidence and target selection.
            view.SetPlayerIdentity(PlayerName, "조조군 지휘 천막");
            view.SetObjective("조조에게 조사 결과를 보고하라");
            ShowReportConclusion();
        }

        void CollectReportEvidence()
        {
            reportEvidence.Clear();
            if (HasHouseholdRegistryEvidence) reportEvidence.Add(new ReportEvidence("신고 신원 대조 결과",
                "자료 범위와 이름 표기를 확인했으나 신고 신원을 뒷받침하는 호적 기록이 없었습니다.",
                "근무 기록과 신고 신원의 확인은 별개로 보아야겠군."));
            if (HasWuLetterEvidence) reportEvidence.Add(new ReportEvidence("본명 밀서",
                "밀서에서 설평과 목연의 연결, 오나라 연락 및 화공대비책 인계 지시를 확인했습니다.",
                "신원 대조 결과와 함께 이 문서가 가리키는 바를 검토하겠다."));
            if (PersonnelRecordRead) reportEvidence.Add(new ReportEvidence("형주 인력 인계 명부", PersonnelRecord,
                "편입 경로는 확인했군. 신고한 출신지와 실제 신원이 일치하는지는 별도로 확인해야 한다."));
            if (DutyRecordRead) reportEvidence.Add(new ReportEvidence("이전 근무 기록", DutyRecord,
                "서리로 일한 기록은 있구나. 보고서를 취합하는 일과 현장에서 직접 목격하는 일은 구분해야 한다."));
            if (DocumentInspected) reportEvidence.Add(new ReportEvidence("빈 문서 보관함",
                "화공대비책의 지정된 보관 위치가 비어 있었습니다.", "문서가 없어진 것은 확인했구나. 누가 가져갔는지는 별도로 밝혀야 한다."));
            if (TestimonyHeard) reportEvidence.Add(new ReportEvidence("설평의 경력 진술", Testimony,
                "서리로 일했다는 것과 훈련을 직접 봤다는 것은 별개의 일이다. 각각 확인해야 한다."));
            if (DuseokTestimonyHeard) reportEvidence.Add(new ReportEvidence("두석의 경비 공백", DuseokTestimony,
                "경비의 공백은 알겠다. 그 틈에 누가 움직였는지는 확인했느냐?"));
            if (CaimaoTestimonyHeard) reportEvidence.Add(new ReportEvidence("채모의 훈련 설명", CaimaoTestimony,
                "출항 훈련을 중단했다는 설명이로군. 당시 기록과 함께 판단해야겠다."));
            if (ZhangyunTestimonyHeard) reportEvidence.Add(new ReportEvidence("장윤의 군량 설명", ZhangyunTestimony,
                "군량을 지급한 사실과 출항 훈련을 한 사실은 구분해야겠군."));
            if (MaintenanceOrderRead) reportEvidence.Add(new ReportEvidence("정비 명령서", MaintenanceOrder,
                "명령의 기간과 범위는 확인했다. 이 기록과 각자의 설명이 맞는지 살펴야 한다."));
            if (TestimonyReconfirmed) reportEvidence.Add(new ReportEvidence("설평의 추가 진술", ReconfirmedTestimony,
                "같은 기간과 장소에서 직접 목격했다는 주장으로 기록하겠다."));
            if (ContradictionFound) reportEvidence.Add(new ReportEvidence("확인한 진술 모순",
                "중단된 출항 훈련을 설평은 같은 기간과 장소에서 직접 보고 기록했다고 주장했습니다.",
                "직접 봤다는 말에는 문제가 있군. 그렇다고 서리로 일한 경력까지 거짓이라고 할 수는 없다. 실제 업무와 신원을 더 확인해야 한다."));
        }

        void ReportModal(string step, string heading, string text)
        {
            SetModal("최종 보고 · " + step, heading, text, "Esc 보고를 중단하고 조사로 돌아가기");
        }

        void ShowReportConclusion()
        {
            ReportModal("1 / 3 · 조사 결과", "조조", "“문서는 찾았느냐. 누가 가져갔는지 밝혔느냐?”");
            view.AddChoice("특정 인물을 지목한다", () => { reportUncertain = false; ShowReportSuspects(); });
            view.AddChoice("의심되는 인물은 있지만 확정하지 못했다", () => { reportUncertain = true; ShowReportSuspects(); });
            view.AddChoice("범인을 밝히지 못했다", () => { reportSuspect = null; reportUncertain = false; ShowReportEvidence(0); });
            view.AddChoice("추가 조사로 돌아간다", CancelFinalReport);
        }

        void ShowReportSuspects()
        {
            ReportModal("1 / 3 · 대상 선택", reportUncertain ? "누구를 의심하는가?" : "누구를 지목하는가?",
                "이후 단계에서 근거를 제출한다. 보고 완료 전에는 선택을 수정할 수 있다.");
            foreach (string name in new[] { "채모", "장윤", "설평", "두석" })
            {
                string selected = name;
                view.AddChoice(selected, () => { reportSuspect = selected; ShowReportEvidence(0); });
            }
            view.AddChoice("이전 단계", ShowReportConclusion);
        }

        string ReportConclusion => reportSuspect == null ? "범인을 밝히지 못함"
            : reportSuspect + (reportUncertain ? "을 의심하나 확정하지 못함" : "을 범인으로 지목");

        void ShowReportEvidence(int page)
        {
            evidencePage = reportEvidence.Count == 0 ? 0 : page % reportEvidence.Count;
            if (reportEvidence.Count == 0)
            {
                ReportModal("2 / 3 · 근거", "제출할 기록 없음", "확보한 기록이 없다. 근거 없이 보고하거나 조사로 돌아갈 수 있다.");
                view.AddChoice("후속 조치 선택", ShowReportRequest);
                view.AddChoice("조사 결과 수정", ShowReportConclusion);
                return;
            }
            var item = reportEvidence[evidencePage];
            ReportModal("2 / 3 · 근거 " + (evidencePage + 1) + " / " + reportEvidence.Count,
                item.title, item.text);
            bool selected = submittedEvidence.Contains(evidencePage);
            view.AddChoice(selected ? "제출 목록에서 뺀다" : "이 기록을 근거로 제출한다", () => {
                if (submittedEvidence.Remove(evidencePage)) ShowReportEvidence(evidencePage);
                else
                {
                    submittedEvidence.Add(evidencePage);
                    ReportModal("근거 검토", "조조", "“" + item.response + "”");
                    view.AddChoice("기록 선택 계속", () => ShowReportEvidence(evidencePage));
                    view.AddChoice("후속 조치 선택", ShowReportRequest);
                }
            });
            view.AddChoice("다음 기록", () => ShowReportEvidence(evidencePage + 1));
            view.AddChoice("제출 선택 완료 (" + submittedEvidence.Count + "건)", ShowReportRequest);
            view.AddChoice("조사 결과 수정", ShowReportConclusion);
        }

        void ShowReportRequest()
        {
            ReportModal("3 / 3 · 후속 조치", "조조", "“보고는 들었다. 어떤 조치가 필요하다고 보느냐?”\n\n요청은 보고서에 기록된다.");
            AddReportRequest("신원과 경력 기록 확인 요청");
            AddReportRequest("문서 반출 경로와 전달자 조사 요청");
            if (HasDecisiveEvidence && reportSuspect == "설평" && !reportUncertain)
                AddReportRequest("신병 확보와 소지품 확인 요청");
            else view.AddChoice("신병 확보와 소지품 확인 · 근거 부족", null, false);
            AddReportRequest("현재 결과로 보고를 마친다");
            view.AddChoice("근거 선택으로 돌아가기", () => ShowReportEvidence(evidencePage));
        }

        void AddReportRequest(string request)
        {
            view.AddChoice(request, () => { reportRequest = request; ShowReportConfirmation(); });
        }

        string BuildReportSummary()
        {
            var labels = new List<string>();
            for (int i = 0; i < reportEvidence.Count; i++)
                if (submittedEvidence.Contains(i)) labels.Add(reportEvidence[i].title);
            return "결론: " + ReportConclusion + "\n\n제출 근거: "
                + (labels.Count == 0 ? "없음" : string.Join(" · ", labels)) + "\n\n요청: " + reportRequest;
        }

        void ShowReportConfirmation()
        {
            ReportModal("최종 확인", "이 내용으로 보고를 마치겠습니까?", BuildReportSummary());
            view.AddChoice("이 내용으로 보고 완료", CompleteFinalReport);
            view.AddChoice("보고 내용 수정", ShowReportConclusion);
            view.AddChoice("보고를 보류하고 추가 조사", CancelFinalReport);
        }

        void CompleteFinalReport()
        {
            if (!reportActive || FinalReportCompleted) return;
            if (reportRequest == "신원과 경력 기록 확인 요청" && HasHouseholdRegistryEvidence && ContradictionFound)
            {
                ReportModal("중간 보고 · 조사 허가", "조조", "“확인된 근거를 바탕으로 설평의 개인 서류 조사를 허락하겠다. 몸수색과 체포는 별도의 근거가 필요하다.”");
                view.AddChoice("조사 명령을 받고 돌아간다", () => { SearchAuthorized = true; CancelFinalReport(); });
                return;
            }
            if (reportUncertain || reportSuspect == null
                || reportRequest == "신원과 경력 기록 확인 요청"
                || reportRequest == "문서 반출 경로와 전달자 조사 요청")
            {
                ReportModal("조사 보고", "조조", "“확인한 내용과 요청은 들었다. 지금은 결론을 확정하지 않고 조사를 이어가도록 하라.”");
                view.AddChoice("조사로 돌아가기", CancelFinalReport);
                view.AddChoice("보고 내용 수정", ShowReportConclusion);
                return;
            }
            FinalReportSummary = BuildReportSummary();
            FinalReportCompleted = true;
            UpdateObjective();
            CurrentEnding = EndingRules.Resolve(reportSuspect, HasHouseholdRegistryEvidence, HasWuLetterEvidence);
            ShowEndingPage(0);
        }

        void ShowCompletedReport()
        {
            SetModal("조사 결과", EndingTitle, EndingOutcome, "이번 조사가 끝났습니다.");
            onEscape = null;
            view.AddChoice("제출한 보고서 보기", () => {
                SetModal("제출한 최종 보고서", PlayerName + "의 보고", FinalReportSummary, "이번 조사 보고가 완료되었습니다.");
                onEscape = ShowCompletedReport;
                view.AddChoice("돌아가기", ShowCompletedReport);
            });
            view.AddChoice("엔딩 다시 보기", () => ShowEndingPage(0));
            view.AddChoice("처음부터 다시 시작", ConfirmRestart);
        }

        void CancelFinalReport()
        {
            if (!reportActive || FinalReportCompleted) return;
            reportActive = false;
            introductionStage.EnterArchive();
            if (reportFromNavalCamp) route.EnterNavalCamp(); else route.EnterArchive();
            InvestigationReviewStarted = false;
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }
    }
}
