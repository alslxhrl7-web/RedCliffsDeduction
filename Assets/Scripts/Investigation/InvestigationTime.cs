using System;
using System.Collections.Generic;
namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTime
    {
        public const int Budget = 12;
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
        bool dayTransitionActive;
        InvestigationDayTransition dayTransition;
        string justStartedInquiry;
        string pendingInquiryNotice;
        public string InvestigationDate => investigationTime.Label;
        public int InvestigationTimeSlot => Math.Min(investigationTime.ElapsedSlots, InvestigationTime.Budget - 1) % 4;
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
        static string InquiryPurpose(string id) => id switch {
            "order_archive" => "군령의 반출 기록과 정상 확인 표시를 살펴본다.",
            "order_receipt" => "수령 부대의 미수령 보고와 병사 파견 여부를 확인한다.",
            "order_procedure" => "정상 인계 절차와 대조 완료 표시의 의미를 확인한다.",
            "order_courier" => "전달자가 언제 누구에게 문서를 넘겼다고 주장하는지 확인한다.",
            "order_movement" => "두석의 목격과 당직 기록으로 전달자의 이동을 확인한다.",
            "order_storage" => "반입 기록과 꾸러미 표식을 확인한다.",
            "order_records" => "별도 보관 부본과 변경 승인 경위를 확인한다.",
            "order_rewrite" => "재작성 의뢰와 반환·출고 기록으로 본문이 달라진 지점을 확인한다.",
            "order_reply" => "변경 승인 회신이 언제 전달되었는지 반납 순서와 대조한다.",
            _ => "확인할 질문을 정하고 조사를 시작하자."
        };
        void ShowDeadlineChoice() {
            SetSavePoint("order_journal");
            SetModal("사흘째 밤 · 마지막 조사", "조조의 호출",
                "새로운 조사를 시작할 시간은 없습니다. 이미 시작한 조사의 남은 질문과 증거 확인은 마칠 수 있습니다.\n\n조사를 마치고 보고하면 현장으로 돌아올 수 없습니다.", "선택해 주세요.");
            onEscape = null;
            view.AddChoice("마지막 조사의 남은 내용 확인", CloseModal);
            view.AddChoice("조사를 마치고 최종 보고", () => { deadlineReportLocked = true; BeginFinalReport(); });
        }
    }
}
