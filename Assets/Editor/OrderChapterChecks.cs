using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    const string SaveKey = "RedCliffMystery.TestSaveDirectory";
    static InvestigationSession S => UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> results = new List<string>();
    static bool running;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Click(string text)
    {
        var button = S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>()
            .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name == text && b.interactable);
        Require(button != null, "Missing enabled choice: " + text + " at " + S.view.title.text);
        button.onClick.Invoke();
    }
    static object Call(string name, params object[] args) => typeof(InvestigationSession).GetMethod(name, Flags).Invoke(S, args);
    static InvestigationSaveData Capture() => (InvestigationSaveData)Call("CaptureSave");
    static void Pay(Action open) { open(); Click("조사 시작 · 1회 사용"); }

    public static void RunPlay()
    {
        if (!Application.isPlaying || S == null || running) throw new InvalidOperationException("타이틀 화면이 있는 Play 상태에서 실행하세요.");
        S.StartCoroutine(Run());
    }
    static IEnumerator Run()
    {
        running = true;
        results.Clear();
        Directory.CreateDirectory("Logs/OrderChapter");
        string old = SessionState.GetString(SaveKey, "");
        SessionState.SetString(SaveKey, Path.GetFullPath("Temp/OrderChecks/Saves-" + Guid.NewGuid().ToString("N")));
        int failures = 0;
        var checks = new List<(string, Action)> {
            ("current save contract and retired version rejection", () => InvestigationSaveChecks.Verify()),
            ("budget and day boundaries", InvestigationBudgetChecks.Verify),
            ("state guards and alternate path", CheckState),
            ("title, name and new briefing", () => {
                var title = UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
                Require(title != null, "Start on title screen"); title.StartButton.onClick.Invoke();
                S.BeginIntroduction("군령 검증");
                S.AdvanceDialogue(); S.AdvanceDialogue(); S.AdvanceDialogue();
                Require(S.view.body.text.Contains("최종 보고"), "Wrong briefing");
                Click("문서고로 향한다"); Require(Capture().version == 2, "Wrong story");
            }),
            ("old save warning preserves current progress", CheckRetiredSave),
            ("early report cannot end game", () => { S.BeginFinalReport(); Require(!S.FinalReportCompleted && !S.OrderState.ReadyToReport, "Early ending"); S.CloseModal(); }),
            ("free travel before archive completion", () => { S.route.EnterNavalCamp(); Require(S.route.AtNavalCamp, "Naval travel blocked"); S.route.EnterArchive(); }),
            ("supply tent travel and save round trip", () => {
                Require(S.route.SupplyTentReady, "Supply arrival missing");
                S.route.EnterNavalCamp(); S.EnterSupplyTent();
                Require(S.route.AtSupplyTent && !S.route.AtNavalCamp, "Supply location incorrect");
                var saved = Capture();
                Require(saved.supplyTent && saved.PreviewSummary().Contains("보급 천막"), "Supply save preview missing");
                string path = InvestigationSaveStore.ManualPath(3);
                InvestigationSaveStore.Write(path, saved);
                S.route.EnterArchive();
                Call("LoadInvestigation", (Action)(() => {}), InvestigationSaveStore.Read(path));
                Require(S.route.AtSupplyTent, "Supply load returned elsewhere");
                S.EnterNavalCamp(); Require(S.route.AtNavalCamp && !S.route.AtSupplyTent, "Supply exit failed");
                Require(Capture().inquiryIds.Length == 0, "Travel consumed inquiry"); S.route.EnterArchive();
            }),
            ("supply report cancel and saved draft preserve return location", () => {
                S.route.EnterSupplyTent(); S.BeginOrderFinal();
                Require(S.OrderReport.fromSupply && !S.OrderReport.fromNaval, "Supply report origin lost");
                var saved = Capture();
                Require(saved.orderReport.fromSupply, "Draft copy lost supply origin");
                string path = InvestigationSaveStore.ManualPath(3);
                InvestigationSaveStore.Write(path, saved);
                Click("제출하지 않고 조사로 돌아가기");
                Require(S.route.AtSupplyTent, "Report cancelled to archive");
                Call("LoadInvestigation", (Action)(() => {}), InvestigationSaveStore.Read(path));
                Call("CancelOrderFinal"); Require(S.route.AtSupplyTent, "Loaded report cancelled elsewhere");
                S.VisitCommandTent(); S.CloseModal();
                Require(S.route.AtSupplyTent, "Command tent escape lost supply origin");
                S.route.EnterArchive(); S.BeginOrderFinal();
                Require(!S.OrderReport.fromSupply, "Supply origin leaked into archive report");
                Click("제출하지 않고 조사로 돌아가기");
                Require(!S.route.AtSupplyTent && !S.route.AtNavalCamp, "Archive return changed");
            }),
            ("walk between all four camps and preserve command save", CheckConnectedCampWalk),
            ("archive first inquiry consumes one", () => { Pay(S.InspectDocument); Require(Capture().inquiryIds.Length == 1, "Inquiry not charged"); Click("반출 대장 확인"); }),
            ("revisit and optional inquiry free", () => { S.InspectDocument(); Require(Capture().inquiryIds.Length == 1, "Repeat charged"); S.ShowOrderSeol(); Click("과거 서류를 다시 본 이유 · 선택"); Require(Capture().inquiryIds.Length == 1, "Optional charged"); }),
            ("courier claim", () => { S.route.EnterNavalCamp(); Pay(S.TalkToDocumentCourier); Click("언제 누구에게 넘겼는가?"); }),
            ("receipt branch contradiction", () => { Pay(S.TalkToCaimao); Click("수령 부대의 보고 확인"); S.TalkToCaimao(); Click("대신 받을 병사를 보냈는가?"); }),
            ("fourth inquiry and day transition", () => Pay(S.ShowOrderStorage)),
            ("recover without confession", () => { Click("반입 기록과 표식 확인"); S.ShowOrderStorage(); Click("해당 꾸러미 조사"); Require(S.OrderState.Has(OrderFact.Recovered) && !S.OrderState.Has(OrderFact.ClaimChallenged), "Confession required"); }),
            ("receipt available at recovery", () => { Click("함께 남은 인계 확인표"); Require(S.OrderState.CanCompare, "Receipt missing"); }),
            ("wrong comparison leaves evidence intact", () => { int before = S.OrderState.facts; S.ShowOrderComparison(); Click("전달자가 군령을 조작했다"); Require(S.OrderState.facts == before && !S.FinalReportCompleted, "Wrong guess mutated state"); }),
            ("compare unlock", () => { S.ShowOrderComparison(); Click("같은 번호인데 완료 회신 장소가 다르다"); Require(S.OrderState.Has(OrderFact.Compared), "Comparison missing"); }),
            ("procedure and choice of suspect", () => { Pay(S.TalkToZhangyun); Click("정상 인계 절차 확인"); S.TalkToZhangyun(); Click("누구를 더 조사할지 정한다"); Click("추가 질문 없이 돌아간다"); Require(!S.BlocksMovement, "Decline inquiry blocked"); }),
            ("motive gated behind recovered document", () => { S.ShowOrderCourier(); Click("무엇이 달라 전달하지 않았는가?"); S.ShowOrderCourier(); Click("대조 완료라 적고 왜 보고하지 않았나?"); Require(S.OrderState.Has(OrderFact.Motive), "Motive missing"); }),
            ("independent copy and approval", () => { Pay(S.ShowOrderRecords); Require(S.OrderState.Has(OrderFact.RetainedCopy | OrderFact.Approval), "Document bundle not recorded"); }),
            ("verify refuses access equals guilt", () => { S.ShowOrderRecords(); Click("정식 변경 가능성을 판단한다"); Click("접근 권한이 있는 사람을 범인으로 확정"); Require(!S.FinalReportCompleted && !S.OrderState.Has(OrderFact.Verified), "Unsupported accusation accepted"); }),
            ("verified mismatch", () => { S.ShowOrderRecords(); Click("정식 변경 가능성을 판단한다"); Click("회수본은 확인한 승인 내용과 다르다"); }),
            ("map links effect", () => { S.InspectMaintenanceOrder(); Click("회수본대로 이동하면 생기는 차이"); Require(S.OrderState.ReadyToReport, "Report still gated"); }),
            ("save round trip and no snapshot alias", CheckSave),
            ("physical storage collider", () => { var target = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include).FirstOrDefault(t => t.kind == InvestigationTarget.TargetKind.OrderStorage); Require(target != null && target.GetComponent<Collider>() != null, "Missing storage scene target"); target.Interact(S); Require(S.view.title.text.Contains("보관소"), "Storage not routed"); }),
            ("new inquiry journal excludes old plot", () => { S.ShowJournal(); Require(!S.view.body.text.Contains("목연") && !S.view.body.text.Contains("화공대비책"), "Old plot leaked"); ScreenCapture.CaptureScreenshot("Logs/OrderChapter/journal.png"); }),
            ("final report replaces interim report", () => { S.VisitCommandTent(); Click("조사 결과 보고"); Require(S.view.title.text.StartsWith("1 / 3") && !S.OrderState.Has(OrderFact.Reported), "Interim gate remains"); Click("제출하지 않고 조사로 돌아가기"); Require(!S.FinalReportCompleted && Capture().inquiryIds.Length == 6, "Report preview ended game or charged"); }),
            ("field autosave without interim flags", () => { Call("AutoSaveProgress"); var saved = InvestigationSaveStore.Read(InvestigationSaveStore.AutoSavePath); Require(saved.version == 2 && !saved.order.Has(OrderFact.Reported), "Interim progress still required"); S.CloseModal(); Require(!S.BlocksMovement, "Return blocked"); })
        };
        AddFollowUpChecks(checks);
        AddOrderFinalChecks(checks);
        AddPlaytestObserverChecks(checks);
        try
        {
            foreach (var check in checks)
            {
                try { check.Item2(); results.Add("PASS " + check.Item1); }
                catch (Exception e) { failures++; results.Add("FAIL " + check.Item1 + ": " + e); }
                File.WriteAllLines("Logs/OrderChapter/checks.txt", results);
                yield return new WaitForSecondsRealtime(.2f);
                double until = Time.realtimeSinceStartupAsDouble + 8;
                while ((bool)typeof(InvestigationSession).GetField("dayTransitionActive", Flags).GetValue(S)
                    && Time.realtimeSinceStartupAsDouble < until) yield return null;
                if (failures > 0) break;
            }
            results.Add("COMPLETE failures=" + failures);
            File.WriteAllLines("Logs/OrderChapter/checks.txt", results);
        }
        finally
        {
            if (string.IsNullOrEmpty(old)) SessionState.EraseString(SaveKey); else SessionState.SetString(SaveKey, old);
            running = false;
            // Prevent the synthetic test session from auto-saving to the player's real slots.
            if (S != null) S.enabled = false;
        }
    }

    static void CheckState()
    {
        var state = new OrderInvestigationState();
        foreach (var gated in new[] { OrderFact.Recovered, OrderFact.Compared, OrderFact.Verified, OrderFact.Motive, OrderFact.Map, OrderFact.Reported })
            Require(!state.TryRecord(gated), "Unlocked early: " + gated);
        state.TryRecord(OrderFact.Dispatch | OrderFact.StorageTrail | OrderFact.Movement);
        Require(state.TryRecord(OrderFact.Recovered), "Movement alternative failed");
        Require(!state.Has(OrderFact.ClaimChallenged | OrderFact.Motive), "Confession inferred");
        state.TryRecord(OrderFact.Receipt); Require(state.TryRecord(OrderFact.Compared), "Compare failed");
        state.TryRecord(OrderFact.RetainedCopy); Require(!state.TryRecord(OrderFact.Verified), "Approval bypass");
        state.TryRecord(OrderFact.Approval); Require(state.TryRecord(OrderFact.Verified), "Verify failed");
        state.TryRecord(OrderFact.Map); Require(state.TryRecord(OrderFact.Reported), "Report needs optional secrets");
        state.Validate();
        bool rejected = false;
        try { new OrderInvestigationState { facts = (int)OrderFact.Reported }.Validate(); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "Corrupt dependency accepted");
    }
    static void CheckRetiredSave()
    {
        var snapshot = Capture();
        var legacy = JsonUtility.FromJson<InvestigationSaveData>(JsonUtility.ToJson(snapshot));
        legacy.version = 1;
        string path = InvestigationSaveStore.ManualPath(3);
        string json = JsonUtility.ToJson(legacy); File.WriteAllText(path, json);
        Call("ShowLoadSlots", (Action)(() => S.CloseModal()));
        Click("수동 슬롯 3 · 기록 없음 / 읽기 실패");
        Require(S.view.body.text.Contains("구버전 설평 사건") && S.view.body.text.Contains("현재 진행은 유지"), "Legacy explanation missing");
        Require(S.PlayerName == snapshot.playerName && S.OrderState.facts == snapshot.order.facts
            && Capture().inquiryIds.Length == snapshot.inquiryIds.Length, "Failed load changed progress");
        Require(File.ReadAllText(path) == json, "Legacy slot altered");
        Click("돌아가기"); Click("돌아가기");
    }
    static void CheckSave()
    {
        var saved = Capture(); saved.Validate();
        int before = saved.order.facts;
        S.OrderState.TryRecord(OrderFact.OptionalProtection);
        Require(saved.order.facts == before, "Save snapshot aliases state");
        var path = InvestigationSaveStore.ManualPath(2);
        InvestigationSaveStore.Write(path, saved);
        var read = InvestigationSaveStore.Read(path);
        Require(read.version == 2 && read.order.facts == before && read.inquiryIds.Length == saved.inquiryIds.Length, "Save mismatch");
        Call("LoadInvestigation", (Action)(() => {}), read);
        Require(S.OrderState.facts == before && !S.FinalReportCompleted, "Load mismatch");
        var bad = JsonUtility.FromJson<InvestigationSaveData>(JsonUtility.ToJson(saved));
        bad.inquiryIds[0] = "seol_initial";
        bool rejected = false;
        try { bad.Validate(); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "Old inquiry accepted into new chapter");
        Require(InvestigationSaveStore.SavePath.EndsWith("order-investigation-save.json"), "Old save path overwritten");
    }
}
