using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class InvestigationDayChecks
{
    static readonly string[] InquiryIds = { "seol_initial", "duseok_initial", "caimao", "zhangyun", "career", "seol_reconfirm", "registry", "papers", "letter_interrogation", "relief_guard", "courier", "duseok_requestion" };
    static object Call(InvestigationSession s, string method, params object[] args) =>
        typeof(InvestigationSession).GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(s, args);
    static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    static void Click(InvestigationSession s, string label) {
        var button = s.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.gameObject.activeSelf && b.name == label);
        Check(button.interactable, "Disabled: " + label);
        button.onClick.Invoke();
    }
    static InvestigationSaveData State(int slots) => new InvestigationSaveData {
        version = 1, playerName = "날짜 검증 참모", inquiryIds = InquiryIds.Take(slots).ToArray(),
        inquiryTitles = InquiryIds.Take(slots).ToArray()
    };

    public static void Verify() {
        Check(Application.isPlaying, "Enter Play Mode first");
        var s = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        var snapshot = (InvestigationSaveData)Call(s, "CaptureSave");
        try {
            foreach (int slots in new[] { 3, 7 }) {
                Call(s, "RestoreSave", State(slots));
                s.view.SetPrologueLayout(false);
                int resumed = 0;
                Call(s, "EnsureInquiry", InquiryIds[slots], "경계 조사", (Action)(() => resumed++));
                Click(s, "조사 시작 · 1회 사용");
                Check(resumed == 0 && s.BlocksMovement, "Transition must block input before resuming");
                s.view.GetComponent<InvestigationDayTransition>().FinishForEditorCheck();
                s.view.GetComponent<InvestigationDayTransition>().FinishForEditorCheck();
                Check(resumed == 1, "Continuation must run once");
                var after = (InvestigationSaveData)Call(s, "CaptureSave");
                Check(after.inquiryIds.Length == slots + 1, "Day boundary charged twice");
                Check((bool)Call(s, "EnsureInquiry", InquiryIds[slots], "재방문", (Action)(() => resumed++)), "Paid topic cannot reopen");
                Call(s, "RestoreSave", after);
                Call(s, "ShowInvestigationSchedule");
                Check(!s.view.eyebrow.text.StartsWith("날짜 변경"), "Loading replayed day notice");
            }
            foreach (int slots in new[] { 0, 1, 3, 4, 7, 8, 11 }) {
                Call(s, "RestoreSave", State(slots));
                Call(s, "ShowInvestigationSchedule");
                string choice = slots < 8 ? "남은 시간을 보내고 다음 날로" : "오늘 조사를 마치고 기한 종료";
                Click(s, choice); Click(s, "돌아가기");
                Check(((InvestigationSaveData)Call(s, "CaptureSave")).inquiryIds.Length == slots, "Cancelled wait charged time");
                Click(s, choice);
                Click(s, slots < 8 ? "다음 날로 넘어간다" : "조사 기한을 마친다");
                int expected = (slots / 4 + 1) * 4;
                var after = (InvestigationSaveData)Call(s, "CaptureSave");
                after.Validate();
                Check(after.inquiryIds.Length == expected && !s.ContradictionFound && !s.HasWuLetterEvidence, "Waiting changed evidence or skipped wrong time");
                if (slots < 8) {
                    s.view.GetComponent<InvestigationDayTransition>().FinishForEditorCheck();
                    Check(s.view.eyebrow.text.StartsWith("조사 일정"), "Wait did not resume schedule automatically");
                } else Check(s.view.title.text == "조조의 호출", "Third day did not reach deadline");
                Call(s, "RestoreSave", after);
                Check(((InvestigationSaveData)Call(s, "CaptureSave")).inquiryIds.Length == expected, "Wait progress lost on load");
            }
            Debug.Log("DAY_TRANSITION_PASS: paid day boundaries, single continuation, revisit, wait/cancel, deadline, save/load");
        } finally {
            Call(s, "RestoreSave", snapshot);
            s.introductionStage.EnterArchive();
            if (snapshot.navalCamp) s.route.EnterNavalCamp(); else s.route.EnterArchive();
            s.view.SetPrologueLayout(false);
            s.ShowPause();
        }
    }
}
