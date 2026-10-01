using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class InvestigationPlayChecks
    {
        static InvestigationSaveData beforeFollowUp;
        static InvestigationSession S => UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
        static void Call(string method, params object[] args) => typeof(InvestigationRouteChecks).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        static void Layout() => Call("CheckLayout", S);
        static void Click(string label) { Call("Click", S, label); Layout(); }
        static void Dialogue() => Call("CompleteDialogue", S);
        static void Cost() => Call("AcceptInvestigationCost", S);
        static void Private(string name) => typeof(InvestigationSession).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(S, null);

        [MenuItem("적벽추리/플레이 검증/추가 진술과 후속 조사")]
        public static void FollowUp()
        {
            Require(Application.isPlaying && S.ContradictionFound, "Run base route first");
            beforeFollowUp = (InvestigationSaveData)typeof(InvestigationSession).GetMethod("CaptureSave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(S, null);
            S.CloseModal(); S.ReturnToArchive();
            S.TalkToSeolpyeong(); Cost(); Click("이곳에서 맡은 일은 무엇인가?");
            S.CloseModal(); Require(!S.WorkRouteHeard, "Cancelled work account awarded");
            S.TalkToSeolpyeong(); Click("이곳에서 맡은 일은 무엇인가?"); Dialogue();
            Require(S.WorkRouteHeard, "Work account missing"); S.CloseModal();
            S.TalkToDuseok(); Click("맡은 경비에 관해"); Dialogue();
            Require(S.AccessAuthorityHeard, "Access account missing"); S.CloseModal();
            S.TalkToSeolpyeong(); Click("서류와 개인 물건에 관해");
            Click("서류 묶음의 목패는 누구의 것인가?"); Dialogue(); S.CloseModal();
            S.EnterNavalCamp(); S.ShowCareerRecords(); Cost(); Layout();
            Click("형주 인력 인계 명부"); Click("다른 서류 확인");
            Click("이전 근무 기록"); Click("확인한 내용 정리");
            Click("기록 담당자에게 신원 대조 요청");
            Click("지역·시기·누락 여부 확인"); Click("대조 계속");
            Click("이름 표기와 가구 항목 확인"); Click("대조 계속");
            Click("대조 결과 기록"); Click("조조에게 개인 서류 조사 허가 요청");
            Click("조사 명령을 받고 돌아간다"); Require(S.SearchAuthorized, "Search permission missing");
            S.ReturnToArchive(); S.InspectPersonalPapers(); Cost();
            Click("안쪽 종이를 펼친다");
            Click("접선 날짜를 조사 일정과 대조");
            Require(S.view.body.text.Contains("열닷새") && S.view.body.text.Contains("입증하지 않는다"), "Schedule distinction missing");
            S.CloseModal(); S.InspectPersonalToken(); Click("밀서의 확인용 본과 비교");
            Click("설평의 개인 목패"); S.CloseModal();
            S.TalkToSeolpyeong(); Click("서류와 개인 물건에 관해"); Click("목연 앞으로 온 밀서를 제시한다");
            Click("목연과 이 서신에 관해 묻는다"); Dialogue();
            Click("목패와 밀서의 연결을 제시한다"); Dialogue();
            Require(S.HasWuLetterEvidence && S.HasHouseholdRegistryEvidence, "Verified evidence missing");
            S.CloseModal(); Call("CheckJournal", S);
            typeof(InvestigationSession).GetMethod("ShowJournalPage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(S, new object[] { 11 });
            Layout();
            var scroll = S.view.body.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            Require(scroll != null && scroll.viewport.rect.height > 0, "Missing body viewport");
            if (scroll.content.rect.height > scroll.viewport.rect.height) {
                var wheel = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                wheel.scrollDelta = new Vector2(0, -100);
                scroll.OnScroll(wheel); Canvas.ForceUpdateCanvases();
                Require(scroll.content.anchoredPosition.y > 0, "Mouse wheel did not reveal lower text");
            }
            S.ShowJournal(); Canvas.ForceUpdateCanvases();
            Require(Mathf.Abs(scroll.content.anchoredPosition.y) < .1f, "New page did not reset scroll");
            Debug.Log("PLAY_FOLLOWUP_PASS: new accounts, cancellation, permission, letter date, interrogation, all journal pages");
        }

        [MenuItem("적벽추리/플레이 검증/저장 복원")]
        public static void SaveLoad()
        {
            Require(Application.isPlaying && S.HasWuLetterEvidence, "Complete follow-up first");
            var paths = new[] { InvestigationSaveStore.SavePath, InvestigationSaveStore.SavePath + ".bak", InvestigationSaveStore.SavePath + ".tmp" };
            var old = paths.Select(p => File.Exists(p) ? File.ReadAllBytes(p) : null).ToArray();
            try {
                string date = S.InvestigationDate;
                S.CloseModal(); S.EnterNavalCamp(); S.ShowPause(); Click("저장 / 불러오기"); Click("현재 조사 저장");
                if (S.view.title.text.Contains("덮어쓸")) Click("덮어쓰기");
                Require(S.view.title.text.Contains("보관"), "Save failed");
                Click("돌아가기"); S.CloseModal(); S.ReturnToArchive();
                S.ShowPause(); Click("저장 / 불러오기"); Click("저장한 조사 불러오기");
                Require(S.view.body.text.Contains(S.PlayerName) && S.view.body.text.Contains(date) && S.view.body.text.Contains("수군 진영") && S.view.body.text.Contains("저장 시각"), "Preview missing saved details");
                Click("취소"); Require(!S.route.AtNavalCamp && S.InvestigationDate == date, "Preview cancel altered gameplay");
                Click("저장한 조사 불러오기"); Click("불러오기");
                Require(S.route.AtNavalCamp && S.WorkRouteHeard && S.AccessAuthorityHeard && S.HasWuLetterEvidence && S.InvestigationDate == date, "Save restore lost state");
                S.ReturnToArchive(); S.TalkToSeolpyeong();
                Require(S.InvestigationDate == date, "Repeated inquiry charged after load");
                S.CloseModal();
                File.WriteAllText(paths[0], "{}");
                S.ShowPause(); Click("저장 / 불러오기"); Click("저장한 조사 불러오기");
                Require(!S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.gameObject.activeSelf && b.name == "불러오기"), "Invalid save can be loaded");
                Require(S.HasWuLetterEvidence && S.InvestigationDate == date, "Invalid preview altered progress");
                Click("돌아가기"); S.CloseModal();
                Debug.Log("PLAY_SAVE_PASS: UI save/load, location, new flags, paid investigations; original save restored");
            } finally {
                for (int i=0;i<paths.Length;i++) { if (old[i] != null) File.WriteAllBytes(paths[i], old[i]); else if (File.Exists(paths[i])) File.Delete(paths[i]); }
            }
        }

        [MenuItem("적벽추리/플레이 검증/진엔딩 보고")]
        public static void TrueEnding()
        {
            Require(Application.isPlaying && S.HasWuLetterEvidence, "Complete follow-up first");
            S.CloseModal(); Private("BeginFinalReport"); Click("특정 인물을 지목한다"); Click("설평");
            Click("이 기록을 근거로 제출한다"); Click("후속 조치 선택");
            Click("신병 확보와 소지품 확인 요청"); Click("이 내용으로 보고 완료");
            Require(S.CurrentEnding == InvestigationEnding.True, "True ending missing");
            Click("그 뒤"); Require(S.view.body.text.Contains("열닷새"), "True ending date missing");
            Capture(); Debug.Log("PLAY_TRUE_ENDING_PASS");
        }

        static void RestoreBeforeFollowUp() {
            Require(beforeFollowUp != null, "Missing reached-gameplay snapshot");
            typeof(InvestigationSession).GetMethod("RestoreSave", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(S, new object[] { beforeFollowUp });
            S.introductionStage.EnterArchive(); S.route.EnterArchive();
            S.view.SetPrologueLayout(false); S.CloseModal();
        }
        static void Report(string suspect) {
            S.BeginFinalReport(); Click("특정 인물을 지목한다"); Click(suspect);
            Click("제출 선택 완료 (0건)"); Click("현재 결과로 보고를 마친다"); Click("이 내용으로 보고 완료");
        }
        [MenuItem("적벽추리/플레이 검증/노멀엔딩")]
        public static void NormalEnding() {
            RestoreBeforeFollowUp(); Report("설평");
            Require(S.CurrentEnding == InvestigationEnding.Normal, "Normal ending missing");
            Click("그 뒤"); Click("보고서를 덮는다");
        }
        [MenuItem("적벽추리/플레이 검증/노멀 암전 확인")]
        public static void CheckNormalFade() {
            Require(S.view.title.text == "엔딩?", "Normal fade/hint missing"); Layout();
            Debug.Log("PLAY_NORMAL_PASS: incomplete proof, unknown whereabouts, fade and alternate-ending hint");
        }
        [MenuItem("적벽추리/플레이 검증/오지목 배드엔딩")]
        public static void BadEndings() {
            foreach (var suspect in new[] { "채모", "장윤", "두석" }) {
                RestoreBeforeFollowUp(); Report(suspect);
                Require(S.CurrentEnding.ToString() == (suspect == "채모" ? "Caimao" : suspect == "장윤" ? "Zhangyun" : "Duseok"), "Wrong bad ending");
                Click("그 뒤"); Require(S.view.body.text.Contains("열닷새"), "Bad ending time missing");
            }
            Click("마지막 명령"); Click("계속");
        }
        [MenuItem("적벽추리/플레이 검증/배드 암전 확인")]
        public static void CheckBadFade() {
            Require(S.view.title.text == "다시 조사하시겠습니까?", "Bad fade/retry missing"); Layout();
            Debug.Log("PLAY_BAD_PASS: three wrong accusations, fourth-day handoff, execution fade and retry");
            Click("예 · 처음부터 다시 조사");
        }
        [MenuItem("적벽추리/플레이 검증/재시작 확인")]
        public static void CheckRestart() {
            Require(!S.FinalReportCompleted && !S.DocumentInspected && S.view.choices.GetComponentInChildren<UnityEngine.UI.InputField>() != null, "Restart did not reset to intro");
            Debug.Log("PLAY_RESTART_PASS");
        }

        [MenuItem("적벽추리/플레이 검증/기한 만료")]
        public static void Deadline() {
            Require(Application.isPlaying && S.HasWuLetterEvidence, "Complete follow-up first");
            S.CloseModal(); S.ReturnToArchive(); S.TalkToReliefGuard(); Cost();
            Click("두석이 떠나는 것을 봤나?"); Dialogue(); Click("다른 질문");
            Click("두석은 언제 돌아왔나?"); Dialogue(); S.CloseModal();
            S.EnterNavalCamp(); S.TalkToDocumentCourier(); Cost(); S.CloseModal();
            S.ReturnToArchive(); S.TalkToDuseok(); Click("경비 시간대를 더 조사한다");
            Click("배급 북 이후부터 교대 직전까지 어디 있었나?"); Dialogue();
            Require(S.GamblingAdmissionHeard && S.InvestigationDate.Contains("남은 조사 0회"), "Last inquiry interrupted or budget wrong");
            S.CloseModal(); Click("마지막 조사의 남은 내용 확인");
            S.TalkToGamblingSoldier();
            Require(S.view.title.text == "조조의 호출", "Thirteenth inquiry was allowed");
            Click("조사를 마치고 최종 보고");
            Require(!S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.gameObject.activeSelf && b.name == "추가 조사로 돌아간다"), "Locked report allows return");
            Click("범인을 밝히지 못했다"); Click("제출 선택 완료 (0건)");
            Click("현재 결과로 보고를 마친다"); Click("이 내용으로 보고 완료");
            Require(S.CurrentEnding == InvestigationEnding.Unresolved, "Expired unresolved ending missing");
            Click("그 뒤"); Require(S.view.eyebrow.text.Contains("열닷새"), "Bad aftermath date stale");
            Debug.Log("PLAY_DEADLINE_PASS: 12th inquiry completed, 13th blocked, report locked, unresolved ending and fourth-day label");
        }

        [MenuItem("적벽추리/플레이 검증/화면 캡처")]
        public static void Capture() {
            Directory.CreateDirectory("Temp");
            ScreenCapture.CaptureScreenshot("Temp/InvestigationPlayCheck.png");
        }
    }
}
