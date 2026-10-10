using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    static void AddPlaytestObserverChecks(List<(string, Action)> checks)
    {
        checks.AddRange(new (string, Action)[] {
            ("playtest log stays off without the flag file", () => {
                if (PlaytestObserver.FlagPresent) return; // a developer may have turned it on for this project
                PlaytestObserver.BeginIfRequested(() => S.PlayerName);
                Require(!PlaytestObserver.Active, "Playtest log started without playtest.on");
            }),
            ("hidden playtest log records play without the player's name", () => {
                const string secret = "가람누리시험";
                var nameProperty = typeof(InvestigationSession).GetProperty("PlayerName");
                string folder = null;
                RestoreFinalBaseline();
                string oldName = S.PlayerName;
                string root = Path.GetFullPath("Temp/OrderChecks/Playtest-" + Guid.NewGuid().ToString("N"));
                try
                {
                    nameProperty.SetValue(S, secret);
                    Require(PlaytestObserver.TryBegin(root, () => S.PlayerName), "Playtest log did not start");
                    folder = PlaytestObserver.SessionFolder;
                    S.view.ShowModal("검사", secret + "의 기록", new string('가', 80), "");
                    S.ShowPause(); Click("조사 계속");
                    S.ShowJournal();
                    int saved = S.OrderState.followUp;
                    S.OrderState.followUp &= ~(int)(OrderFollowUpFact.Knowing | OrderFollowUpFact.Sequence | OrderFollowUpFact.Reported);
                    S.ShowOrderReplyOrder(); Click(InvestigationSession.JudgeKnowingByDispatch); Click("다시 판단");
                    Click(InvestigationSession.JudgeMisreadReply);
                    S.OrderState.followUp = saved;
                    Require(PlaytestObserver.Recorder.BruteForceRetries == 1 && PlaytestObserver.Recorder.PauseNoAction == 1, "Retry or pause not counted");
                    RunFinalOutcome(1, 63 | 64, 1, "NORMAL END");
                    PlaytestObserver.End();
                    string events = File.ReadAllText(Path.Combine(folder, PlaytestObserver.EventsFileName));
                    string report = File.ReadAllText(Path.Combine(folder, PlaytestObserver.ReportFileName));
                    Require(!events.Contains(secret) && !report.Contains(secret), "Player name written to playtest log");
                    Require(events.Contains(PlaytestRecorder.NamePlaceholder), "Name placeholder missing");
                    Require(events.Contains(",click,") && events.Contains(",judgment,") && events.Contains(",report_submit,"), "Playtest events missing");
                    Require(report.Contains("| 3번 |") && report.Contains("미끼 근거 포함: 예") && report.Contains("엔딩: NORMAL"), "Playtest report incomplete");
                    Require(!events.Contains(new string('가', 80)), "Screen body text written to playtest log");
                }
                finally
                {
                    PlaytestObserver.End();
                    nameProperty.SetValue(S, oldName);
                    RestoreFinalBaseline();
                    if (folder != null) File.Copy(Path.Combine(folder, PlaytestObserver.ReportFileName), "Logs/OrderChapter/playtest-report-sample.md", true);
                }
            })
        });
    }
}
