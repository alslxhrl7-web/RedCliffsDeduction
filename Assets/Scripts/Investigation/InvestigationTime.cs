using System.Collections.Generic;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTime
    {
        readonly HashSet<string> completed = new HashSet<string>();
        readonly List<string> history = new List<string>();
        public int ElapsedSlots => completed.Count;
        public bool DeadlineReached => ElapsedSlots >= 9;
        public string Label
        {
            get
            {
                int slot = System.Math.Min(ElapsedSlots, 8);
                return new[] { "첫째 날", "둘째 날", "셋째 날" }[slot / 3]
                    + " · " + new[] { "오전", "오후", "밤" }[slot % 3]
                    + (DeadlineReached ? " · 최종 보고 시간" : "");
            }
        }
        public bool Complete(string id, string description)
        {
            if (DeadlineReached || !completed.Add(id)) return false;
            history.Add(description + " → " + Label);
            return true;
        }
        public string History => history.Count == 0 ? "아직 완료한 조사 묶음이 없다." : string.Join("\n", history);
    }

    public sealed partial class InvestigationSession
    {
        readonly InvestigationTime investigationTime = new InvestigationTime();
        int displayedTimeSlots = -1;
        public string InvestigationDate => investigationTime.Label;

        void RefreshInvestigationTime()
        {
            if (!introductionFinished || FinalReportCompleted) return;
            if (FirstInvestigationComplete) investigationTime.Complete("archive", "문서고 초동 조사");
            if (OfficersHeard) investigationTime.Complete("officers", "채모·장윤 설명 확보");
            if (PersonnelRecordRead && DutyRecordRead) investigationTime.Complete("career", "경력 서류 조사");
            if (HasHouseholdRegistryEvidence) investigationTime.Complete("registry", "호적 대조");
            if (LetterFound) investigationTime.Complete("papers", "개인 서류 조사");
            if (InterrogationCompleted) investigationTime.Complete("interrogation", "목패 대조와 취조");
            if (displayedTimeSlots != investigationTime.ElapsedSlots)
            {
                displayedTimeSlots = investigationTime.ElapsedSlots;
                view.SetInvestigationDate(investigationTime.Label);
            }
        }

        void ShowInvestigationSchedule()
        {
            RefreshInvestigationTime();
            SetModal("조사 일정", InvestigationDate,
                "새로운 조사 묶음이 끝나면 한 시간대가 흐릅니다.\n이동·재열람·대화 취소·허가 요청·최종 보고는 시간을 쓰지 않습니다.\n\n" + investigationTime.History,
                "사흘 · 오전 / 오후 / 밤");
            view.AddChoice("돌아가기", ShowPause);
            view.AddChoice("조사 계속", CloseModal);
        }
    }
}
