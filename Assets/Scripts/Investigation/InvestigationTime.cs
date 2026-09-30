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
        bool skipDeadlinePrompt;
        public string InvestigationDate => investigationTime.Label;
        bool CanReportNow => OfficersHeard || investigationTime.DeadlineReached;

        void RefreshInvestigationTime() {
            if (!introductionFinished || FinalReportCompleted) return;
            if (displayedTimeSlots != investigationTime.ElapsedSlots) {
                displayedTimeSlots = investigationTime.ElapsedSlots;
                view.SetInvestigationDate(investigationTime.Label);
            }
        }
        bool EnsureInquiry(string id, string title, Action resume) {
            if (FinalReportCompleted || deadlineReportLocked) return false;
            if (investigationTime.HasStarted(id)) return true;
            if (investigationTime.DeadlineReached) { ShowDeadlineChoice(); return false; }
            SetModal("조사 시작 전 확인", title,
                "이 조사를 시작하면 1회를 사용합니다. 같은 주제의 질문과 재방문에는 다시 차감하지 않습니다.\n\n남은 조사: "
                + investigationTime.Remaining + "회" + (investigationTime.Remaining == 1 ? "\n마지막 조사입니다. 질문과 결과 확인을 마친 뒤 최종 보고해야 합니다." : ""));
            view.AddChoice("조사 시작 · 1회 사용", () => {
                if (!investigationTime.Complete(id, title)) return;
                RefreshInvestigationTime();
                resume();
            });
            view.AddChoice("지금은 조사하지 않는다", CloseModal);
            return false;
        }
        void BeginPaidSequence(string id, string title, string speaker, string[] lines, Action complete) {
            if (!EnsureInquiry(id, title, () => BeginPaidSequence(id, title, speaker, lines, complete))) return;
            BeginSequence(speaker, lines, complete);
        }
        void ShowDeadlineChoice() {
            SetModal("사흘째 밤 · 마지막 조사", "조조의 호출",
                "새로운 조사를 시작할 시간은 없습니다. 이미 시작한 조사의 남은 질문과 증거 확인은 마칠 수 있습니다.\n\n조사를 마치고 보고하면 현장으로 돌아올 수 없습니다.", "선택해 주세요.");
            onEscape = null;
            view.AddChoice("마지막 조사의 남은 내용 확인", () => { skipDeadlinePrompt = true; CloseModal(); });
            view.AddChoice("조사를 마치고 최종 보고", () => { deadlineReportLocked = true; BeginFinalReport(); });
        }
        void ShowInvestigationSchedule() => ShowInvestigationSchedulePage(0);
        void ShowInvestigationSchedulePage(int page) {
            RefreshInvestigationTime();
            SetModal("조사 일정 · " + (page + 1) + " / " + investigationTime.HistoryPages, InvestigationDate,
                "조사 시작에 1회 사용 · 같은 주제 재방문은 무료\n이동·증거 비교·수첩·보고는 무료\n\n" + investigationTime.HistoryPage(page), "사흘 · 오전 / 오후 / 저녁 / 밤");
            if (investigationTime.HistoryPages > 1) view.AddChoice("다음 기록", () => ShowInvestigationSchedulePage((page + 1) % investigationTime.HistoryPages));
            if (investigationTime.DeadlineReached) view.AddChoice("최종 보고 준비", ShowDeadlineChoice);
            view.AddChoice("조사 계속", CloseModal);
        }
    }
}
