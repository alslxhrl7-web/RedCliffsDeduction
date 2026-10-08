using System;
using System.Collections.Generic;
namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTime
    {
        public const int Budget = 12;
        public const string ContactDate = "이달 열닷새 새벽";
        readonly HashSet<string> started = new HashSet<string>();
        readonly List<string> orderedIds = new List<string>();
        readonly List<string> history = new List<string>();
        public int ElapsedSlots => started.Count;
        public int Remaining => Budget - ElapsedSlots;
        public bool DeadlineReached => Remaining == 0;
        public int DayIndex => Math.Min(ElapsedSlots, Budget - 1) / 4;
        public int SlotsUntilDayEnd => Math.Min(4 - ElapsedSlots % 4, Remaining);
        public bool HasStarted(string id) => started.Contains(id);
        public string Label {
            get {
                int slot = Math.Min(ElapsedSlots, Budget - 1);
                return new[] { "첫째 날", "둘째 날", "셋째 날" }[slot / 4] + " · "
                    + new[] { "오전", "오후", "저녁", "밤" }[slot % 4]
                    + " · 이달 " + new[] { "열이튿날", "열사흗날", "열나흗날" }[slot / 4]
                    + " · 남은 조사 " + Remaining + "회";
            }
        }
        public bool Complete(string id, string description) {
            if (DeadlineReached || !started.Add(id)) return false;
            orderedIds.Add(id);
            history.Add(description);
            return true;
        }
        public void FinishDay() {
            int slots = SlotsUntilDayEnd;
            for (int i = 0; i < slots; i++)
                Complete("wait_" + ElapsedSlots, "시간 보내기 · 조사하지 않고 휴식");
        }
        public string[] SavedIds => orderedIds.ToArray();
        public string[] SavedTitles => history.ToArray();
        public void Restore(string[] ids, string[] titles) {
            started.Clear(); orderedIds.Clear(); history.Clear();
            for (int i = 0; i < ids.Length; i++) Complete(ids[i], titles[i]);
        }
        public string History => history.Count == 0 ? "아직 시작한 조사가 없다." : string.Join("\n", history);
        public string HistoryPage(int page) => history.Count == 0 ? "아직 시작한 조사가 없다."
            : string.Join("\n", history.GetRange(page * 6, Math.Min(6, history.Count - page * 6)));
        public int HistoryPages => Math.Max(1, (history.Count + 5) / 6);
    }

    public sealed partial class InvestigationSession
    {
        readonly InvestigationTime investigationTime = new InvestigationTime();
        int displayedTimeSlots = -1;
        bool deadlineReportLocked;
        bool skipDeadlinePrompt;
        bool dayTransitionActive;
        InvestigationDayTransition dayTransition;
        string justStartedInquiry;
        string pendingInquiryNotice;
        public string InvestigationDate => investigationTime.Label;
        bool CanReportNow => introductionFinished && !FinalReportCompleted;

        void RefreshInvestigationTime() {
            UpdateInvestigationAmbience();
            if (!introductionFinished || FinalReportCompleted) return;
            if (displayedTimeSlots != investigationTime.ElapsedSlots) {
                displayedTimeSlots = investigationTime.ElapsedSlots;
                view.SetInvestigationDate(investigationTime.Label);
            }
        }
        bool EnsureInquiry(string id, string title, Action resume) {
            if (FinalReportCompleted || deadlineReportLocked) return false;
            if (investigationTime.HasStarted(id)) {
                pendingInquiryNotice = justStartedInquiry == id
                    ? "조사 1회 사용 · 남은 조사 " + investigationTime.Remaining + "회"
                    : "이미 시작한 조사 · 추가 시간 소모 없음";
                justStartedInquiry = null;
                return true;
            }
            if (investigationTime.DeadlineReached) { ShowDeadlineChoice(); return false; }
            SetModal("조사 시작 전 확인", title,
                InquiryPurpose(id) + "\n\n이 조사를 시작하면 1회를 사용합니다. 같은 주제의 질문과 재방문에는 다시 차감하지 않습니다.\n\n남은 조사: "
                + investigationTime.Remaining + "회" + (investigationTime.Remaining == 1 ? "\n마지막 조사입니다. 질문과 결과 확인을 마친 뒤 최종 보고해야 합니다." : ""));
            view.AddChoice("조사 시작 · 1회 사용", () => {
                int previousDay = investigationTime.DayIndex;
                if (!investigationTime.Complete(id, title)) return;
                justStartedInquiry = id;
                RefreshInvestigationTime();
                if (investigationTime.DayIndex != previousDay) ShowNewDay(resume);
                else resume();
            });
            view.AddChoice("지금은 조사하지 않는다", CloseModal);
            return false;
        }
        void ShowNewDay(Action resume) {
            dayTransitionActive = true;
            BlocksMovement = true;
            onAdvance = null;
            onEscape = null;
            if (dayTransition == null)
                dayTransition = view.gameObject.AddComponent<InvestigationDayTransition>();
            string heading = investigationTime.DayIndex == 1 ? "둘째 날" : "셋째 날 · 마지막 조사일";
            string date = investigationTime.DayIndex == 1 ? "이달 열사흗날 · 오전" : "이달 열나흗날 · 오전";
            dayTransition.Play(view.font, heading, date, () => {
                dayTransitionActive = false;
                resume();
            });
        }
        void ConfirmFinishDay() {
            if (investigationTime.DeadlineReached) { ShowDeadlineChoice(); return; }
            bool lastDay = investigationTime.DayIndex == 2;
            SetModal("시간 보내기", lastDay ? "오늘 조사를 마치겠습니까?" : "다음 날로 넘어가겠습니까?",
                "오늘 남은 " + investigationTime.SlotsUntilDayEnd + "회를 조사하지 않고 보냅니다. 확인하지 않은 증거는 추가되지 않습니다.\n\n"
                + (lastDay ? "사흘의 기한이 끝나면 새로운 조사를 시작할 수 없습니다." : "다음 날 오전부터 조사를 이어갈 수 있습니다."));
            bool confirmed = false;
            view.AddChoice(lastDay ? "조사 기한을 마친다" : "다음 날로 넘어간다", () => {
                if (confirmed) return;
                confirmed = true;
                investigationTime.FinishDay();
                RefreshInvestigationTime();
                if (investigationTime.DeadlineReached) ShowDeadlineChoice();
                else ShowNewDay(ShowInvestigationSchedule);
            });
            view.AddChoice("돌아가기", ShowInvestigationSchedule);
            onEscape = ShowInvestigationSchedule;
        }
        static string InquiryPurpose(string id) => id switch {
            "order_archive" => "군령의 반출 기록과 정상 확인 표시를 살펴본다.",
            "order_receipt" => "수령 부대의 미수령 보고와 병사 파견 여부를 확인한다.",
            "order_procedure" => "정상 인계 절차와 대조 완료 표시의 의미를 확인한다.",
            "order_courier" => "전달자가 언제 누구에게 문서를 넘겼다고 주장하는지 확인한다.",
            "order_movement" => "두석의 목격과 당직 기록으로 전달자의 이동을 확인한다.",
            "order_storage" => "반입 기록과 꾸러미 표식을 확인한다.",
            "order_records" => "별도 보관 부본과 변경 승인 경위를 확인한다.",
            "document" => "문서가 보관되던 자리와 남아 있는 흔적을 살펴본다.",
            "maintenance" => "정비 명령서에 적힌 기간과 지시 내용을 읽는다.",
            "token" => "서류를 묶은 목패의 모양과 흔적을 살펴본다.",
            "seol_initial" => "확인할 질문: 서기가 맡은 업무와 이전 근무 경험은 무엇인가?",
            "duseok_initial" => "확인할 질문: 문서고 경비 중 자리를 비운 일이 있었는가?",
            "caimao" => "확인할 질문: 당시 수군 훈련은 어떻게 진행되었는가?",
            "zhangyun" => "확인할 질문: 당시 군량 지급과 병사들의 작업은 어떤 관계였는가?",
            "seol_reconfirm" => "확인할 질문: 설평의 설명은 기록과 같은 시기·장소이며 직접 목격한 일인가?",
            "career" => "확인할 질문: 인계 명부와 근무 기록에는 어떤 담당 업무가 적혀 있는가?",
            "registry" => "확인할 질문: 신고한 신원이 해당 지역의 호적 기록과 일치하는가?",
            "papers" => "확인할 질문: 허가받은 개인 서류에서 사건과 관련된 자료를 찾을 수 있는가?",
            "letter_interrogation" => "확인할 질문: 발견한 밀서와 개인 물건에 관해 설평은 어떻게 설명하는가?",
            "courier" => "확인할 질문: 문서를 마지막으로 확인한 때와 없어진 것을 알게 된 때는 언제인가?",
            "relief_guard" => "확인할 질문: 교대 병사가 직접 본 두석의 이탈·복귀 시점은 언제인가?",
            "duseok_requestion" => "확인할 질문: 교대 병사의 설명을 바탕으로 두석에게 이탈 이유와 행적을 다시 묻는다.",
            "gambling_witness" => "확인할 질문: 두석이 말한 노름 시간과 동석 여부가 병사의 설명과 맞는가?",
            _ => "확인할 질문을 정하고 조사를 시작하자."
        };
        void BeginPaidSequence(string id, string title, string speaker, string[] lines, Action complete) {
            if (!EnsureInquiry(id, title, () => BeginPaidSequence(id, title, speaker, lines, complete))) return;
            BeginSequence(speaker, lines, complete);
        }
        void ShowDeadlineChoice() {
            SetSavePoint("deadline");
            SetModal("사흘째 밤 · 마지막 조사", "조조의 호출",
                "새로운 조사를 시작할 시간은 없습니다. 이미 시작한 조사의 남은 질문과 증거 확인은 마칠 수 있습니다.\n\n조사를 마치고 보고하면 현장으로 돌아올 수 없습니다.", "선택해 주세요.");
            onEscape = null;
            view.AddChoice("마지막 조사의 남은 내용 확인", () => { skipDeadlinePrompt = true; CloseModal(); });
            view.AddChoice("조사를 마치고 최종 보고", () => { deadlineReportLocked = true; BeginFinalReport(); });
        }
        void ShowInvestigationSchedule() => ShowInvestigationSchedulePage(0);
        void ShowInvestigationSchedulePage(int page) {
            SetSavePoint("schedule");
            RefreshInvestigationTime();
            SetModal("조사 일정 · " + (page + 1) + " / " + investigationTime.HistoryPages, InvestigationDate,
                "인물과 자료 모두 새 조사 주제에 1회를 사용합니다. 4회마다 다음 날로 진행합니다.\n이미 확보한 기록을 다시 읽거나 정리할 때는 중복 차감하지 않습니다.\n오늘 남은 시간을 보내고 다음 날로 넘어갈 수도 있습니다.\n\n" + investigationTime.HistoryPage(page), "사흘 · 오전 / 오후 / 저녁 / 밤");
            if (investigationTime.HistoryPages > 1) view.AddChoice("다음 기록", () => ShowInvestigationSchedulePage((page + 1) % investigationTime.HistoryPages));
            if (investigationTime.DeadlineReached) view.AddChoice("최종 보고 준비", ShowDeadlineChoice);
            else view.AddChoice(investigationTime.DayIndex == 2 ? "오늘 조사를 마치고 기한 종료" : "남은 시간을 보내고 다음 날로", ConfirmFinishDay);
            view.AddChoice("조사 계속", CloseModal);
        }
    }
}
