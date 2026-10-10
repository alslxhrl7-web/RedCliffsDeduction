using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    static void AddFollowUpChecks(List<(string, Action)> checks)
    {
        checks.AddRange(new (string, Action)[] {
            ("follow-up guards and missing old-save field", CheckFollowUpGuards),
            ("follow-up has no interim-report prerequisite", () => {
                Require(!S.OrderState.Has(OrderFact.Reported) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Held), "Interim flags unexpectedly granted");
                S.ShowOrderFollowUp(); Require(!S.view.body.text.Contains("保留") && S.view.body.text.Contains("최종 보고"), "Interim action UI remains");
            }),
            ("Han introduction free and focused", () => {
                int count = Capture().inquiryIds.Length; S.ShowOrderHan();
                Require(Capture().inquiryIds.Length == count && !S.OrderState.HasFollowUp(OrderFollowUpFact.Submitted), "Introduction grants proof or charges");
                Require(Choices().Length <= 3 && !Choices().Any(x => x.Contains("회신") || x.Contains("접수")), "Han owns remote record menus");
            }),
            ("returned bundle one read no confirmation", () => {
                Pay(S.ShowOrderRewrite);
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original | OrderFollowUpFact.Intake), "Bundle lost facts");
                Require(Choices().Length == 3 && !Choices().Contains("돌아가기"), "Extra confirmation screen");
                int count = Capture().inquiryIds.Length; Click("세 문서 전문 펼치기"); Click("간추린 자료로");
                Require(Capture().inquiryIds.Length == count, "Full text recharged");
            }),
            ("optional original comparison", () => { S.ShowOrderOriginalComparison(); Click("재작성 전후 내용 차이, 변경 허가 확인이 필요"); Require(S.OrderState.HasFollowUp(OrderFollowUpFact.OriginalCompared), "Original comparison failed"); }),
            ("outgoing read same inquiry and physical table", () => {
                int count = Capture().inquiryIds.Length; Target(InvestigationTarget.TargetKind.OutgoingOrder).Interact(S);
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Outgoing) && Capture().inquiryIds.Length == count, "Outgoing missing or recharged");
            }),
            ("clerk limited to two direct observations", () => {
                Target(InvestigationTarget.TargetKind.RecordsClerk).Interact(S);
                Require(Choices().SequenceEqual(new[] { "한진에게 직접 받은 물건을 묻는다", "한진에게 답신을 직접 건넸는가?", "대화 마치기" }), "Clerk overloaded or misrouted");
                Click("한진에게 직접 받은 물건을 묻는다");
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.ClerkConfirmation) && Choices().Length == 3, "Witness needs return menu");
            }),
            ("first deduction advances directly to second", () => {
                S.ShowOrderSubmission(); int before = S.OrderState.followUp;
                Click(InvestigationSession.JudgeCourierChanged); Require(S.OrderState.followUp == before, "Wrong timing mutates facts");
                S.ShowOrderSubmission(); Click(InvestigationSession.JudgeAlwaysWithdraw); Require(S.OrderState.followUp == before && S.view.title.text == "근거 다시 살펴보기", "Unchanged-text claim accepted");
                S.ShowOrderSubmission(); Click(InvestigationSession.JudgeBeforeCourier);
                Require(S.view.title.text.StartsWith("2 / 3") && !Choices().Contains("돌아가기"), "Confirmation interrupts deduction");
                before = S.OrderState.followUp; Click(InvestigationSession.JudgeHanByAssignment); Require(S.OrderState.followUp == before, "Assignment alone proves submission");
                S.ShowOrderSubmission(); Click(InvestigationSession.JudgeSwapAfterIntake); Require(S.OrderState.followUp == before, "Swap theory accepted");
                S.ShowOrderSubmission(); Click(InvestigationSession.JudgeHanSubmitted);
                Require(S.view.title.text.StartsWith("3 / 3") && !S.OrderState.CanProveKnowledge, "Submission proves intent");
            }),
            ("reply bundle eighth inquiry day transition", () => Pay(S.ShowOrderReplies)),
            ("read reply does not select conclusion", () => {
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Rejection | OrderFollowUpFact.Delivery) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Sequence) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Knowing), "Reading grants inference");
                Require(Choices().Length == 3 && !Choices().Any(x => x.Contains("대면")), "Record table impersonates witness");
            }),
            ("wrong intent inference leaves evidence", () => {
                Click("확보한 자료 비교"); int before = S.OrderState.followUp;
                Require(S.view.body.text.Contains("[수령 기록]") && S.view.body.text.Contains("[거절 회신]"), "Comparison split across screens");
                Click(InvestigationSession.JudgeKnowingByDispatch); Require(S.OrderState.followUp == before, "Dispatch accepted as receipt");
                S.ShowOrderReplyOrder(); Click(InvestigationSession.JudgeMisreadReply); Require(S.OrderState.followUp == before, "Misreading theory accepted");
            }),
            ("third question records chronology and intent", () => {
                S.ShowOrderReplyOrder(); Click(InvestigationSession.JudgeKnowingByReceipt);
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Knowing | OrderFollowUpFact.Sequence) && S.view.title.text == "제17호 후속 수사", "Third judgment needs extra confirmation");
                ScreenCapture.CaptureScreenshot("Logs/OrderChapter/follow-up-journal.png");
            }),
            ("Han admits only when confronted with the receipt record", () => {
                S.ShowOrderHan(); Require(!S.view.body.text.Contains("답신"), "Opening line changed by judgment");
                Click("제출 기록에 대한 해명을 듣는다"); Require(!S.view.body.text.Contains("답신을 읽고도"), "Confession without confrontation");
                Click("거절 답신의 수령 기록을 들이민다"); Require(S.view.body.text.Contains("답신을 읽고도"), "Confrontation did not produce admission");
                S.CloseModal();
            }),
            ("completed deduction does not auto-report", () => {
                Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Knowing) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Reported) && !S.FinalReportCompleted, "Deduction auto reported");
                Require(Capture().inquiryIds.Length == 8, "Expected 8 inquiries");
            }),
            ("follow-up save snapshot and reload", () => {
                var saved = Capture(); int before = saved.order.followUp;
                S.OrderState.TryRecordFollowUp(OrderFollowUpFact.DeliveryWitness); Require(saved.order.followUp == before, "Snapshot alias");
                string path = InvestigationSaveStore.ManualPath(3); InvestigationSaveStore.Write(path, saved);
                Call("LoadInvestigation", (Action)(() => {}), InvestigationSaveStore.Read(path));
                Require(S.OrderState.followUp == before && S.OrderState.HasFollowUp(OrderFollowUpFact.Knowing), "Follow-up lost");
            }),
            ("no confession no original card alternate path", CheckFollowUpAlternate),
            ("documents and clerk use separate physical interactions", () => {
                foreach (var kind in new[] { InvestigationTarget.TargetKind.HanJin, InvestigationTarget.TargetKind.ReturnedOrder, InvestigationTarget.TargetKind.OutgoingOrder, InvestigationTarget.TargetKind.CareerRecords })
                    Require(Target(kind).GetComponentsInChildren<Collider>().Any(c => c.enabled), "Missing collider " + kind);
                Target(InvestigationTarget.TargetKind.CareerRecords).Interact(S);
                Require(S.view.title.text == "발급 기록 탁자" && !Choices().Contains("한진에게 직접 받은 물건을 묻는다"), "Table routes to NPC");
                var table = Target(InvestigationTarget.TargetKind.CareerRecords);
                var top = table.GetComponentsInChildren<Collider>().First(c => c.name == "Tabletop");
                var origin = top.bounds.center + new Vector3(0, .7f, 1.8f);
                Physics.SyncTransforms();
                Require(Physics.Raycast(origin, top.bounds.center - origin, out var hit, 3f) && hit.collider.GetComponentInParent<InvestigationTarget>() == table, "Document table is not reachable");
            }),
            ("notebook reread and comparison require no revisit", () => {
                int count = Capture().inquiryIds.Length, facts = S.OrderState.followUp;
                S.ShowJournal(); Require(S.view.title.text == "제17호 후속 수사", "Extra journal hub");
                Click("자료 전문 · 선택 열람"); Click("수첩으로"); Click("판단 근거 다시 보기");
                Require(Capture().inquiryIds.Length == count && S.OrderState.followUp == facts, "Reread changed facts or charged");
                ScreenCapture.CaptureScreenshot("Logs/OrderChapter/follow-up-comparison.png");
            }),
            ("core summaries fit without scrolling", () => {
                S.ShowOrderFollowUp(); RequireBodyFits();
                int saved = S.OrderState.followUp;
                try {
                    S.OrderState.followUp &= ~(int)(OrderFollowUpFact.Knowing | OrderFollowUpFact.Sequence | OrderFollowUpFact.Reported);
                    S.ShowOrderReplyOrder(); RequireBodyFits();
                    Require(Choices().Contains(InvestigationSession.JudgeKnowingByReceipt), "Missing active comparison");
                    ScreenCapture.CaptureScreenshot("Logs/OrderChapter/follow-up-comparison.png");
                } finally { S.OrderState.followUp = saved; }
            })
        });
    }
    static string[] Choices() => S.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.gameObject.activeInHierarchy).Select(b => b.name).ToArray();
    static void RequireBodyFits()
    {
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(S.view.modal.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        Require(S.view.body.preferredHeight <= ((RectTransform)S.view.body.transform.parent).rect.height + 1, "Core summary requires scrolling");
    }
    static InvestigationTarget Target(InvestigationTarget.TargetKind kind) => UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include).Single(t => t.kind == kind);
    static void CheckFollowUpGuards()
    {
        var empty = new OrderInvestigationState();
        foreach (var gated in new[] { OrderFollowUpFact.BeforeCourier, OrderFollowUpFact.Submitted, OrderFollowUpFact.Sequence, OrderFollowUpFact.Knowing, OrderFollowUpFact.Held, OrderFollowUpFact.DeploymentRequested, OrderFollowUpFact.Reported, OrderFollowUpFact.OriginalCompared })
            Require(!empty.TryRecordFollowUp(gated) && empty.followUp == 0, "Ungated follow-up: " + gated);
        Require(!empty.TryRecordFollowUp(OrderFollowUpFact.Knowing | OrderFollowUpFact.MetHan), "Combined flags bypass");
        var legacy = JsonUtility.FromJson<OrderInvestigationState>("{\"facts\":" + S.OrderState.facts + "}"); legacy.Validate();
        Require(legacy.followUp == 0, "Old save unexpectedly gains progress");
        var corrupt = S.OrderState.Copy(); corrupt.followUp = (int)OrderFollowUpFact.Reported;
        bool rejected = false; try { corrupt.Validate(); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "Invalid follow-up save accepted");
    }
    static void CheckFollowUpAlternate()
    {
        var preserved = Capture();
        try {
            S.OrderState.followUp = 0;
            foreach (var label in new[] { InvestigationSession.JudgeCourierChanged, InvestigationSession.JudgeBeforeCourier, InvestigationSession.JudgeAlwaysWithdraw }) {
                S.ShowOrderSubmission(); Require(Choices().Contains(label), "Judgment option hidden before evidence: " + label);
                Click(label); Require(S.OrderState.followUp == 0 && S.view.body.text.Contains("확정할 수 없다"), "Option reacts differently before evidence: " + label);
            }
            S.OrderState.facts &= ~(int)(OrderFact.OptionalCareer | OrderFact.OptionalProtection | OrderFact.ClaimChallenged | OrderFact.Explanation | OrderFact.Motive);
            S.ShowOrderAssignment(); S.ShowOrderIntake(); S.ShowOrderOutgoing();
            S.ShowOrderSubmission(); Click(InvestigationSession.JudgeBeforeCourier);
            Click(InvestigationSession.JudgeHanSubmitted);
            // A legacy save may contain the rejection without its delivery card.
            S.OrderState.TryRecordFollowUp(OrderFollowUpFact.Rejection);
            Require(!S.OrderState.CanOrderReply, "Dispatch alone proves receipt");
            S.ShowOrderReplyWitness(); S.ShowOrderReplyOrder(); Click(InvestigationSession.JudgeKnowingByReceipt);
            Require(S.OrderState.HasFollowUp(OrderFollowUpFact.Knowing) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Original) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Intake) && !S.OrderState.HasFollowUp(OrderFollowUpFact.Delivery), "Alternate path used missing cards");
            S.OrderState.Validate();
        } finally { Call("LoadInvestigation", (Action)(() => {}), preserved); }
    }
}
