using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    static InvestigationSaveData finalBaseline;
    static void AddOrderFinalChecks(List<(string, Action)> checks)
    {
        checks.AddRange(new (string, Action)[] {
            ("final rules exhaustive submission/action matrix", () => {
                finalBaseline = Capture();
                Require((((int)OrderFinalReport.Available(S.OrderState)) & OrderFinalReport.EvidenceSetMask) == OrderFinalReport.EvidenceSetMask, "Missing test evidence");
                // proofs now spans 0..127: bit 64 is the access-record decoy.
                for (int suspect = 0; suspect <= 6; suspect++) for (int proofs = 0; proofs <= 127; proofs++) for (int action = 0; action <= 3; action++) {
                    var draft = new OrderFinalReport { suspect = suspect, proofs = proofs, action = action };
                    var outcome = draft.Resolve(S.OrderState);
                    Require((outcome == OrderReportEnding.True) == (suspect == 1 && proofs == 63 && action == 1), "Unexpected true ending");
                    if (suspect >= 2 || proofs == 0) Require(outcome == OrderReportEnding.Bad, "Wrong accusation or absent submission rewarded");
                }
                Require(new OrderFinalReport { suspect=0, proofs=3 }.Resolve(S.OrderState) == OrderReportEnding.Normal, "Valid partial proof not normal");
                var unknown = new OrderFinalReport { proofs=63 }; bool rejected=false;
                try { unknown.Validate(new OrderInvestigationState()); } catch (InvalidDataException) { rejected=true; }
                Require(rejected, "Unowned evidence accepted");
            }),
            ("final draft selected evidence and save reload", () => {
                S.BeginFinalReport(); Click("한진을 책임자로 지목");
                Require(S.OrderReport.proofs == 0, "Owned evidence auto submitted");
                Click("[미선택] 회수본과 확인표의 지시 불일치");
                Click("[미선택] 출처가 확인된 부본과 변경 승인 대장");
                Click("[미선택] 작업 의뢰와 반납·출고를 연결한 제출 기록");
                Click("다음 근거 묶음");
                Click("[미선택] 거절 회신의 수령과 완성물 반납 순서");
                Click("[미선택] 본진 철수로 생기는 강 입구 감시 공백");
                Click("[미선택] 전달자의 반입 기록과 문서 회수 경위");
                Click("조치 선택으로"); Click("제17호·대체 재발행 중지와 강 입구 배치 복원");
                var snapshot = Capture(); Require(snapshot.resumePoint == "order_final" && snapshot.orderReport.proofs == 63, "Wrong save namespace");
                S.OrderReport.proofs = 0; Require(snapshot.orderReport.proofs == 63, "Draft snapshot aliases live state");
                string path=InvestigationSaveStore.ManualPath(3); InvestigationSaveStore.Write(path,snapshot);
                Call("LoadInvestigation", (Action)(() => {}), InvestigationSaveStore.Read(path));
                Require(S.OrderReport.proofs == 63 && S.view.title.text == "최종 제출 전 확인" && !S.FinalReportCompleted, "Draft restore lost selection");
            }),
            ("final true ending and pre-submit autosave", () => {
                Click("최종 제출 · 엔딩으로"); Require(S.FinalReportCompleted && S.OrderReport.Resolve(S.OrderState) == OrderReportEnding.True, "True ending failed");
                Click("명령이 전해진 뒤"); Require(S.view.title.text.StartsWith("TRUE END") && !S.view.body.text.Contains("목연"), "Legacy story leaked");
                Require(!S.view.footer.text.Contains("F5") && !S.view.footer.text.Contains("확정 전 수정"), "Ending advertises disabled editing");
                ScreenCapture.CaptureScreenshot("Logs/OrderChapter/final-true.png");
                var saved = InvestigationSaveStore.Read(InvestigationSaveStore.AutoSavePath);
                Require(saved.orderReport.stage == 1 && saved.orderReport.proofs == 63, "Editable pre-submit save lost");
            }),
            ("unsubmitted evidence bad ending despite owning all", () => RunFinalOutcome(1,0,1,"BAD END")),
            ("partial submitted proof normal ending", () => RunFinalOutcome(0,3,2,"NORMAL END")),
            ("wrong culprit remains bad with full evidence", () => RunFinalOutcome(2,63,1,"BAD END")),
            ("wrong action cannot produce true ending", () => RunFinalOutcome(1,63,3,"NORMAL END")),
            ("access-record decoy breaks the true ending", () => RunFinalOutcome(1,63|64,1,"NORMAL END")),
            ("evidence complete but no order change is not rewarded as missing evidence", () => {
                RestoreFinalBaseline(); S.BeginFinalReport();
                S.OrderReport.suspect=1; S.OrderReport.proofs=63; S.OrderReport.action=0;
                Call("ShowOrderFinalReview"); Click("최종 제출 · 엔딩으로");
                Require(S.view.body.text.Contains("멈추라는 명령은 내리지 않는다"), "Complete evidence with no action still shows missing-evidence text");
                Click("명령이 전해진 뒤");
            }),
            ("legacy interim flags do not grant good ending", () => {
                RestoreFinalBaseline(); S.OrderState.TryRecord(OrderFact.Reported);
                S.OrderState.TryRecordFollowUp(OrderFollowUpFact.Held | OrderFollowUpFact.DeploymentRequested);
                Require(new OrderFinalReport { suspect=1 }.Resolve(S.OrderState) == OrderReportEnding.Bad, "Old interim flags granted ending");
                var legacy=Capture(); legacy.orderReport=null; legacy.resumePoint="order_journal";
                Call("LoadInvestigation", (Action)(() => {}), legacy);
                Require(S.OrderReport.proofs==0 && !S.FinalReportCompleted, "Old save auto completed");
            }),
            ("deadline report ends and does not reopen investigation", () => {
                RestoreFinalBaseline(); S.BeginFinalReport(); typeof(InvestigationSession).GetField("deadlineReportLocked",Flags).SetValue(S,true);
                Call("CancelFinalReport"); Require(S.view.title.text=="최종 제출 전 확인", "Deadline escaped");
                S.OrderReport.suspect=0; S.OrderReport.proofs=0; Call("ShowOrderFinalReview"); Click("최종 제출 · 엔딩으로");
                Require(S.FinalReportCompleted, "Deadline did not finish"); RestoreFinalBaseline();
            })
        });
    }
    static void RestoreFinalBaseline() => Call("LoadInvestigation", (Action)(() => {}), finalBaseline);
    static void RunFinalOutcome(int suspect,int proofs,int action,string title)
    {
        RestoreFinalBaseline(); S.BeginFinalReport();
        S.OrderReport.suspect=suspect; S.OrderReport.proofs=proofs; S.OrderReport.action=action;
        Call("ShowOrderFinalReview"); Click("최종 제출 · 엔딩으로"); Click("명령이 전해진 뒤");
        Require(S.view.title.text.StartsWith(title), "Wrong final ending: "+title);
        ScreenCapture.CaptureScreenshot("Logs/OrderChapter/final-"+title.Split(' ')[0].ToLowerInvariant()+".png");
    }
}
