using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;
using RedCliffMystery.EditorTools;

[InitializeOnLoad]
public static class InvestigationRegressionRun
{
    const string SaveKey = "RedCliffMystery.TestSaveDirectory";
    const string ReportPath = "Logs/PlayRegression/result.txt";
    static readonly Queue<(string label, Action check, double delay)> steps = new Queue<(string, Action, double)>();
    static readonly StringBuilder results = new StringBuilder();
    static InvestigationSaveData full;
    static double nextAt;
    static int failures;
    static bool originalOptionsEnabled;
    static EnterPlayModeOptions originalOptions;
    static float audioTime;
    static bool running;
    static InvestigationSession S => UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static InvestigationRegressionRun() { EditorApplication.playModeStateChanged += OnPlayState; }

    [MenuItem("적벽추리/플레이 검증/전체 회귀 검사")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Begin in edit mode");
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Investigation.unity") throw new Exception("Open Investigation scene first");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        originalOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        originalOptions = EditorSettings.enterPlayModeOptions;
        running = true;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        Directory.CreateDirectory("Temp/PlayRegression");
        Directory.CreateDirectory("Logs/PlayRegression");
        SessionState.SetString(SaveKey, Path.GetFullPath("Temp/PlayRegression/Saves-" + Guid.NewGuid().ToString("N")));
        results.Clear(); failures = 0; steps.Clear();
        results.AppendLine("Regression start: " + DateTimeOffset.Now);
        results.AppendLine("Isolated saves: " + InvestigationSaveStore.AutoSavePath);
        Check("Unity save serialization", () => Debug.Log(InvestigationSaveChecks.Verify()));
        Check("Budget and ending rules", () => { InvestigationBudgetChecks.Verify(); EndingRuleChecks.Verify(); });
        Check("Scene connections and evidence guards", () => { FollowUpConnectionChecks.Verify(); DuseokTimelineChecks.Verify(); });
        Add("Document-first base route", InvestigationRouteChecks.RunDocumentFirst);
        Add("Follow-up route, 12 slots", InvestigationPlayChecks.FollowUp);
        Add("Manual save UI restore", InvestigationPlayChecks.SaveLoad);
        Add("Autosave file and complete evidence", () => {
            full = Capture();
            var automatic = InvestigationSaveStore.Read(InvestigationSaveStore.AutoSavePath);
            Require(automatic.HasWuLetterEvidence && automatic.inquiryIds.Length == 12, "Autosave missed final evidence or budget");
        });
        Add("Day boundaries, waiting and cancellation", InvestigationDayChecks.Verify);
        Add("Restore full route", () => Call("LoadInvestigation", (Action)(() => {}), full));
        Add("True ending at 12 slots", InvestigationPlayChecks.TrueEnding);
        Add("Restore full route before deadline report", () => Call("LoadInvestigation", (Action)(() => {}), full));
        Add("Deadline, 13th inquiry blocked, unresolved report", InvestigationPlayChecks.Deadline);
        Add("Normal ending transition", InvestigationPlayChecks.NormalEnding, 2);
        Add("Normal ending fade", InvestigationPlayChecks.CheckNormalFade);
        Add("Three wrong accusations and bad ending transition", InvestigationPlayChecks.BadEndings, 2);
        Add("Bad ending fade and restart", InvestigationPlayChecks.CheckBadFade, .8);
        Add("Restart reset", InvestigationPlayChecks.CheckRestart);
        Add("Testimony-first base route", InvestigationRouteChecks.RunTestimonyFirst);
        Add("Reload fresh scene", () => SceneManager.LoadScene("Assets/Scenes/Investigation.unity"), .8);
        Add("Eleven-slot base route without optional guard", InvestigationRouteChecks.RunElevenSlotBase);
        Add("Eleven-slot follow-up", InvestigationPlayChecks.FollowUp);
        Add("Eleven-slot true ending", () => {
            Require(Capture().inquiryIds.Length == 11, "True route should use eleven slots");
            InvestigationPlayChecks.TrueEnding();
        });
        Add("Early report and manual report checkpoint", CheckReportCheckpoint);
        Add("Report autosave preserves manual slot", () => {
            var manual = InvestigationSaveStore.Read(InvestigationSaveStore.ManualPath(2));
            var automatic = InvestigationSaveStore.Read(InvestigationSaveStore.AutoSavePath);
            Require(manual.resumePoint == "report_confirm" && manual.report.suspect == "설평"
                && manual.report.uncertain && manual.report.evidence.Length == 1, "Manual report selections lost");
            Require(automatic.report == null && automatic.DocumentInspected, "Report cancellation not autosaved");
            Call("LoadInvestigation", (Action)(() => {}), manual);
            Require(S.view.body.text.Contains("설평을 의심") && S.view.body.text.Contains("빈 문서 보관함")
                && S.view.body.text.Contains("문서 반출 경로와 전달자 조사 요청"), "Report confirmation not restored");
            Click("이 내용으로 보고 완료");
            Require(!S.FinalReportCompleted, "Uncertain early report incorrectly ends investigation");
            Click("조사로 돌아가기");
            Require(Capture().inquiryIds.Length == 1 && !S.BlocksMovement, "Early report return changed budget or blocked movement");
        });
        Add("Prepare actual date animation and ambience", () => {
            Call("RestoreSave", new InvestigationSaveData { version = 1, playerName = "연출 검증", inquiryIds = new[] { "document", "token", "seol_initial" }, inquiryTitles = new[] { "보관함", "목패", "설평" } });
            S.introductionStage.EnterArchive(); S.route.EnterArchive(); S.view.SetPrologueLayout(false);
            Call("ShowInvestigationSchedule"); Call("ConfirmFinishDay"); Click("다음 날로 넘어간다");
            Require((bool)typeof(InvestigationSession).GetField("dayTransitionActive", Flags).GetValue(S), "No real date animation");
            audioTime = S.GetComponent<AudioSource>().time;
        }, 3.3);
        Add("Actual animation completes and ambience continues", () => {
            Require(!(bool)typeof(InvestigationSession).GetField("dayTransitionActive", Flags).GetValue(S), "Date animation did not finish");
            Require(S.view.eyebrow.text.StartsWith("조사 일정"), "Date animation did not resume schedule");
            var audio = S.GetComponent<AudioSource>();
            Require(audio != null && audio.isPlaying && audio.loop && audio.clip.length > 9 && audio.time != audioTime, "Ambience not advancing through blackout");
            Require(Application.runInBackground, "Background execution disabled");
            ScreenCapture.CaptureScreenshot("Logs/PlayRegression/date-return.png");
        });
        Add("Report without evidence and safe return", () => {
            Call("LoadInvestigation", (Action)(() => {}), new InvestigationSaveData {
                version = 1, playerName = "빈 기록 검증", resumePoint = "field",
                inquiryIds = Array.Empty<string>(), inquiryTitles = Array.Empty<string>()
            });
            S.BeginFinalReport(); Click("범인을 밝히지 못했다");
            Require(S.view.title.text == "제출할 기록 없음", "Empty evidence report unavailable");
            Click("후속 조치 선택"); Click("현재 결과로 보고를 마친다");
            Click("이 내용으로 보고 완료"); Click("조사로 돌아가기");
            Require(!S.FinalReportCompleted && !S.BlocksMovement && Capture().inquiryIds.Length == 0,
                "Empty evidence report blocks further investigation or consumes time");
        });
        EditorApplication.isPlaying = true;
    }

    static void Add(string label, Action check, double delay = .25) => steps.Enqueue((label, check, delay));
    static void CheckReportCheckpoint() {
        Call("LoadInvestigation", (Action)(() => {}), new InvestigationSaveData {
            version = 1, playerName = "보고 검증", resumePoint = "field", DocumentInspected = true,
            inquiryIds = new[] { "document" }, inquiryTitles = new[] { "보관함" }
        });
        S.BeginFinalReport();
        Click("의심되는 인물은 있지만 확정하지 못했다"); Click("설평");
        Click("이 기록을 근거로 제출한다"); Click("후속 조치 선택");
        Click("문서 반출 경로와 전달자 조사 요청");
        Call("OpenManualSaveSlots"); ClickPrefix("슬롯 2에 저장");
        if (S.view.title.text.Contains("바꿀")) Click("덮어쓰기");
        Require(S.view.title.text.Contains("저장했습니다"), "Report manual save failed");
        Click("저장한 분기에서 계속"); Click("보고를 보류하고 추가 조사");
    }
    static void ClickPrefix(string prefix) {
        foreach (var button in S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>())
            if (button.gameObject.activeSelf && button.name.StartsWith(prefix) && button.interactable) { button.onClick.Invoke(); return; }
        throw new Exception("Missing button prefix: " + prefix);
    }
    static object Call(string method, params object[] args) => typeof(InvestigationSession).GetMethod(method, Flags).Invoke(S, args);
    static InvestigationSaveData Capture() => (InvestigationSaveData)Call("CaptureSave");
    static void Require(bool ok, string why) { if (!ok) throw new Exception(why); }
    static void Click(string label) {
        foreach (var button in S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>())
            if (button.gameObject.activeSelf && button.name == label && button.interactable) { button.onClick.Invoke(); return; }
        throw new Exception("Missing button: " + label);
    }
    static void Check(string label, Action action) {
        // Branch checks run synchronously; the travel presentation has its own timed play test.
        if(Application.isPlaying && S!=null && S.route!=null) S.route.animateDeparture=false;
        try { action(); results.AppendLine("PASS " + label); }
        catch (Exception error) {
            failures++; while (error.InnerException != null) error = error.InnerException;
            results.AppendLine("FAIL " + label + ": " + error);
            Debug.LogWarning("REGRESSION_FAIL " + label + ": " + error.Message);
        }
        File.WriteAllText(ReportPath, results.ToString());
    }
    static void OnPlayState(PlayModeStateChange state) {
        if (!running) return;
        if (state == PlayModeStateChange.EnteredPlayMode && steps.Count > 0) {
            nextAt = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode) {
            EditorApplication.update -= Tick;
            SessionState.EraseString(SaveKey);
            EditorSettings.enterPlayModeOptionsEnabled = originalOptionsEnabled;
            EditorSettings.enterPlayModeOptions = originalOptions;
            running = false;
            steps.Clear();
        }
    }
    static void Tick() {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextAt) return;
        // Coroutine completion is observed on the next rendered frame. A fixed
        // two-second Editor timer can race the ending's two-second fade.
        if(steps.Count>0 && steps.Peek().label=="Normal ending fade" && S!=null && S.view.title.text!="엔딩?"
            && EditorApplication.timeSinceStartup<nextAt+6) return;
        if (steps.Count == 0) {
            results.AppendLine("COMPLETE failures=" + failures);
            File.WriteAllText(ReportPath, results.ToString());
            Debug.Log("REGRESSION_COMPLETE failures=" + failures);
            EditorApplication.update -= Tick; EditorApplication.isPlaying = false; return;
        }
        var step = steps.Dequeue(); Check(step.label, step.check);
        nextAt = EditorApplication.timeSinceStartup + step.delay;
    }
}
