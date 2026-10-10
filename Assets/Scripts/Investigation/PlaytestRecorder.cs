using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RedCliffMystery.Investigation
{
    // Records what a playtester does, for the developer only. Nothing here is shown to the player.
    // Privacy: never records the player's name (it is replaced in every string), typed text, body text of screens,
    // or anything about the computer. Plain C# with an injected clock so it can be tested outside Unity.
    public sealed class PlaytestRecorder
    {
        public const double IdleSeconds = 30, WanderSeconds = 90, SkimSeconds = 1.0, RepeatWindowSeconds = 10;
        public const int SkimMinBodyLength = 60;
        public const string CsvHeader = "seconds,kind,screen,detail,location";
        public const string NamePlaceholder = "[플레이어]";

        readonly Func<double> clock;
        readonly Action<string> sink;
        readonly Func<string> nameProvider;
        readonly double start;

        string screen = "", location = "", objective = "";
        double screenOpenedAt, locationSince, objectiveSince;
        int screenBodyLength;
        bool modalOpen, screenSkimCounted;
        readonly Dictionary<string, double> locationTime = new Dictionary<string, double>();
        readonly Dictionary<string, double> objectiveTime = new Dictionary<string, double>();
        readonly Dictionary<string, int> screenCounts = new Dictionary<string, int>();
        readonly Dictionary<string, int> skimScreens = new Dictionary<string, int>();

        int clicks, skims, longScreens, repeats;
        string lastClickLabel, lastClickScreen;
        double lastClickAt = double.NegativeInfinity;

        double lastInput, lastInteraction, unfocusedTime, lastTick, lastIdleEnd = double.NegativeInfinity;
        bool idle, wandering;
        double idleStart, wanderStart;
        string idleWhere = "", idleObjective = "", wanderWhere = "", wanderObjective = "";
        public readonly List<Stretch> Stretches = new List<Stretch>();

        bool pauseOpen;
        int pauseOpens, pauseNoAction;

        int journalOpens, sourceReads;
        bool inJournal;
        double journalStart, journalTime;

        public readonly SortedDictionary<int, List<Attempt>> Judgments = new SortedDictionary<int, List<Attempt>>();
        readonly Dictionary<int, bool> readSinceLast = new Dictionary<int, bool>();
        bool readSinceAny;

        int reportOpens = 0, reportReturns = 0, reportEdits = 0;
        double firstReportOpen = -1, reportSubmittedAt = -1;
        public SubmittedReport Report { get; private set; }

        public struct Stretch { public string Kind, Where, Objective; public double At, Duration; }
        public struct Attempt { public string Option, Result; public bool Ready, Correct, ReadBefore, Rushed; public double At; }
        public sealed class SubmittedReport { public string Conclusion, Action, Ending; public string[] Proofs; public bool Decoy; }

        public PlaytestRecorder(Func<double> clock, Action<string> sink, Func<string> nameProvider)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.sink = sink ?? (_ => { });
            this.nameProvider = nameProvider ?? (() => null);
            start = lastInput = lastInteraction = locationSince = objectiveSince = lastTick = clock();
            Log("session_start", "", "");
        }

        double Now => clock();
        public int Clicks => clicks;
        public int Skims => skims;
        public int Repeats => repeats;
        public int PauseNoAction => pauseNoAction;
        public int JournalOpens => journalOpens;
        public int BruteForceRetries => Judgments.Values.SelectMany(a => a).Count(a => a.Rushed);

        public string Redact(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            string name = null;
            try { name = nameProvider(); } catch { }
            if (!string.IsNullOrWhiteSpace(name)) text = text.Replace(name.Trim(), NamePlaceholder);
            return text.Replace("\r", " ").Replace("\n", " ");
        }

        static string Csv(string value)
        {
            value = value ?? "";
            return value.IndexOfAny(new[] { ',', '"' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        void Log(string kind, string detail, string screenOverride = null)
        {
            string line = string.Join(",", (Now - start).ToString("0.0", CultureInfo.InvariantCulture), kind,
                Csv(Redact(screenOverride ?? screen)), Csv(Redact(detail)), Csv(Redact(location)));
            sink(line);
        }

        void Interacted()
        {
            double now = Now;
            if (wandering) CloseStretch(ref wandering, "wander", wanderStart, wanderWhere, wanderObjective, now);
            lastInteraction = now;
        }

        void CloseStretch(ref bool open, string kind, double from, string where, string goal, double now)
        {
            open = false;
            if (kind == "idle") lastIdleEnd = now;
            var s = new Stretch { Kind = kind, Where = where, Objective = goal, At = from - start, Duration = now - from };
            Stretches.Add(s);
            Log(kind, Seconds(s.Duration) + " · 목표: " + goal, where);
        }

        string Context => modalOpen ? screen : location;

        // Called every frame by the Unity layer.
        public void Tick(bool anyInput, bool focused)
        {
            double now = Now;
            if (!focused)
            {
                unfocusedTime += Math.Max(0, now - lastTick);
                // Time outside the game window is not counted as being stuck.
                lastInput = lastInteraction = now;
                if (idle) CloseStretch(ref idle, "idle", idleStart, idleWhere, idleObjective, now);
                if (wandering) CloseStretch(ref wandering, "wander", wanderStart, wanderWhere, wanderObjective, now);
                lastTick = now;
                return;
            }
            lastTick = now;
            if (anyInput)
            {
                if (idle) CloseStretch(ref idle, "idle", idleStart, idleWhere, idleObjective, now);
                lastInput = now;
            }
            else if (!idle && now - lastInput >= IdleSeconds)
            {
                idle = true; idleStart = lastInput; idleWhere = Context; idleObjective = objective;
            }
            // Wandering = moving or looking around without opening anything. Idle time is not counted twice.
            double activeSince = Math.Max(lastInteraction, lastIdleEnd);
            if (!modalOpen && !idle && !wandering && now - activeSince >= WanderSeconds)
            {
                wandering = true; wanderStart = activeSince; wanderWhere = location; wanderObjective = objective;
            }
        }

        public void Screen(string category, string heading, int bodyLength)
        {
            screen = Redact((category ?? "") + " / " + (heading ?? ""));
            screenOpenedAt = Now;
            screenBodyLength = bodyLength;
            screenSkimCounted = false;
            modalOpen = true;
            lastClickLabel = null; // a repeat only counts when the same button is pressed again without anything new opening
            screenCounts[screen] = screenCounts.TryGetValue(screen, out var n) ? n + 1 : 1;
            if (bodyLength >= SkimMinBodyLength) longScreens++;
            Interacted();
            Log("screen", "본문 " + bodyLength + "자");
        }

        public void ModalClosed()
        {
            double now = Now;
            if (pauseOpen) { pauseOpen = false; pauseNoAction++; Log("pause_no_action", ""); }
            if (inJournal) { journalTime += now - journalStart; inJournal = false; }
            modalOpen = false;
            Interacted();
            Log("close", "");
        }

        public void Click(string label, bool interactable)
        {
            double now = Now;
            label = Redact(label);
            double onScreen = now - screenOpenedAt;
            clicks++;
            if (!screenSkimCounted && onScreen < SkimSeconds && screenBodyLength >= SkimMinBodyLength)
            {
                screenSkimCounted = true; skims++;
                skimScreens[screen] = skimScreens.TryGetValue(screen, out var n) ? n + 1 : 1;
            }
            if (label == lastClickLabel && screen == lastClickScreen && now - lastClickAt <= RepeatWindowSeconds) repeats++;
            lastClickLabel = label; lastClickScreen = screen; lastClickAt = now;
            if (pauseOpen)
            {
                pauseOpen = false;
                if (label == "조사 계속") { pauseNoAction++; Log("pause_no_action", label); }
            }
            Interacted();
            Log("click", label + " · 화면 " + onScreen.ToString("0.0", CultureInfo.InvariantCulture) + "초" + (interactable ? "" : " · 비활성"));
        }

        public void Location(string name)
        {
            name = Redact(name);
            if (name == location) return;
            double now = Now;
            if (location.Length > 0) Add(locationTime, location, now - locationSince);
            location = name; locationSince = now;
            Log("location", name);
        }

        public void Objective(string text)
        {
            text = Redact(text);
            if (text == objective) return;
            double now = Now;
            if (objective.Length > 0) Add(objectiveTime, objective, now - objectiveSince);
            objective = text; objectiveSince = now;
            Log("objective", text);
        }

        public void Journal()
        {
            journalOpens++;
            if (!inJournal) { inJournal = true; journalStart = Now; }
            Log("journal", "");
        }

        public void Pause()
        {
            pauseOpens++; pauseOpen = true;
            Log("pause", "");
        }

        public void SourceRead(string title)
        {
            sourceReads++;
            readSinceAny = true;
            foreach (var key in readSinceLast.Keys.ToList()) readSinceLast[key] = true;
            Log("source", title);
        }

        public void Judgment(int question, string option, bool ready, bool correct)
        {
            option = Redact(option);
            if (!Judgments.TryGetValue(question, out var list)) Judgments[question] = list = new List<Attempt>();
            bool readBefore = readSinceLast.TryGetValue(question, out var r) ? r : readSinceAny;
            bool rushed = list.Count > 0 && list[list.Count - 1].Ready && !list[list.Count - 1].Correct && !readBefore;
            string result = !ready ? "증거 부족" : correct ? "정답" : "오답";
            list.Add(new Attempt { Option = option, Result = result, Ready = ready, Correct = ready && correct, ReadBefore = readBefore, Rushed = rushed, At = Now - start });
            readSinceLast[question] = false;
            Log("judgment", question + "번 · " + result + " · " + option + (rushed ? " · 자료 확인 없이 다시 고름" : ""));
        }

        public void ReportOpened()
        {
            reportOpens++;
            if (firstReportOpen < 0) firstReportOpen = Now - start;
            Log("report_open", "");
        }

        public void ReportCancelled() { reportReturns++; Log("report_return", ""); }
        public void ReportEdited(string what) { reportEdits++; Log("report_edit", what); }

        public void ReportSubmitted(string conclusion, IEnumerable<string> proofs, bool decoy, string action, string ending)
        {
            Report = new SubmittedReport { Conclusion = Redact(conclusion), Proofs = (proofs ?? Enumerable.Empty<string>()).Select(Redact).ToArray(), Decoy = decoy, Action = Redact(action), Ending = Redact(ending) };
            reportSubmittedAt = Now - start;
            Log("report_submit", Report.Ending + " · 미끼 " + (decoy ? "포함" : "없음") + " · 조치: " + Report.Action);
        }

        static void Add(Dictionary<string, double> map, string key, double value) => map[key] = (map.TryGetValue(key, out var v) ? v : 0) + Math.Max(0, value);

        // Brings running timers up to date without ending them, so a report can be written mid-session.
        void Flush()
        {
            double now = Now;
            if (location.Length > 0) { Add(locationTime, location, now - locationSince); locationSince = now; }
            if (objective.Length > 0) { Add(objectiveTime, objective, now - objectiveSince); objectiveSince = now; }
            if (inJournal) { journalTime += now - journalStart; journalStart = now; }
        }

        public void Finish()
        {
            double now = Now;
            if (idle) CloseStretch(ref idle, "idle", idleStart, idleWhere, idleObjective, now);
            if (wandering) CloseStretch(ref wandering, "wander", wanderStart, wanderWhere, wanderObjective, now);
            Flush();
            Log("session_end", "");
        }

        public static string Seconds(double s)
        {
            if (s < 0) s = 0;
            int total = (int)Math.Round(s);
            return total >= 3600 ? $"{total / 3600}시간 {total % 3600 / 60}분" : total >= 60 ? $"{total / 60}분 {total % 60}초" : $"{total}초";
        }

        static string Cell(string s) => (s ?? "").Replace("|", "/");

        public string BuildReport(string gameVersion, DateTime startedLocal)
        {
            Flush();
            double played = Now - start;
            var sb = new StringBuilder();
            sb.AppendLine("# 플레이테스트 관찰 보고서").AppendLine();
            sb.AppendLine("- 시작: " + startedLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine("- 플레이 시간: " + Seconds(played) + (unfocusedTime >= 1 ? " (게임 창 밖에 있던 " + Seconds(unfocusedTime) + " 포함)" : ""));
            sb.AppendLine("- 게임 버전: " + Cell(gameVersion));
            sb.AppendLine("- 이 기록에는 이름, 입력한 글자, 컴퓨터 정보 같은 개인정보가 들어 있지 않습니다. 플레이어 이름이 나오는 자리는 " + NamePlaceholder + "로 바꿔 적었습니다.");
            sb.AppendLine();

            var idles = Stretches.Where(s => s.Kind == "idle").ToList();
            var wanders = Stretches.Where(s => s.Kind == "wander").ToList();
            sb.AppendLine("## 1. 길을 잃는 순간").AppendLine();
            sb.AppendLine($"- {IdleSeconds:0}초 넘게 아무 입력이 없었던 때: {idles.Count}회 (합계 {Seconds(idles.Sum(s => s.Duration))})");
            sb.AppendLine($"- {WanderSeconds:0}초 넘게 움직이기만 하고 조사·대화를 하지 않은 때: {wanders.Count}회 (합계 {Seconds(wanders.Sum(s => s.Duration))})");
            sb.AppendLine($"- 일시정지 메뉴를 열었다가 아무것도 하지 않고 닫은 횟수: {pauseNoAction}회 (메뉴를 연 횟수 {pauseOpens}회)");
            var longest = Stretches.OrderByDescending(s => s.Duration).Take(6).ToList();
            if (longest.Count > 0)
            {
                sb.AppendLine().AppendLine("가장 오래 멈춘 곳").AppendLine();
                sb.AppendLine("| 시각 | 길이 | 종류 | 장소 또는 화면 | 당시 목표 |").AppendLine("|---|---|---|---|---|");
                foreach (var s in longest)
                    sb.AppendLine($"| {Seconds(s.At)} | {Seconds(s.Duration)} | {(s.Kind == "idle" ? "입력 없음" : "돌아다니기만 함")} | {Cell(s.Where)} | {Cell(s.Objective)} |");
            }
            sb.AppendLine();

            sb.AppendLine("## 2. 세 질문 추리").AppendLine();
            if (Judgments.Count == 0) sb.AppendLine("- 판단을 한 번도 시도하지 않았습니다.");
            else
            {
                sb.AppendLine("| 질문 | 시도 | 증거 전 시도 | 오답 | 오답 뒤 자료 확인 없이 다시 고름 | 정답까지 |").AppendLine("|---|---|---|---|---|---|");
                foreach (var pair in Judgments)
                {
                    var a = pair.Value;
                    int firstCorrect = a.FindIndex(x => x.Correct);
                    sb.AppendLine($"| {pair.Key}번 | {a.Count}회 | {a.Count(x => !x.Ready)}회 | {a.Count(x => x.Ready && !x.Correct)}회 | {a.Count(x => x.Rushed)}회 | {(firstCorrect >= 0 ? (firstCorrect + 1) + "번째" : "아직 못 맞힘")} |");
                }
                sb.AppendLine().AppendLine("고른 순서").AppendLine();
                foreach (var pair in Judgments)
                    sb.AppendLine($"- {pair.Key}번: " + string.Join(" → ", pair.Value.Select(x => $"[{x.Result}{(x.ReadBefore ? ", 자료 확인 후" : "")}] {x.Option}")));
            }
            sb.AppendLine();

            int attempts = Judgments.Values.Sum(a => a.Count);
            sb.AppendLine("## 3. 수첩과 자료").AppendLine();
            sb.AppendLine($"- 수첩을 연 횟수: {journalOpens}회");
            sb.AppendLine($"- 수첩·추리 화면에 머문 시간: {Seconds(journalTime)}");
            sb.AppendLine($"- 자료 원문이나 묶음을 다시 읽은 횟수: {sourceReads}회");
            sb.AppendLine($"- 판단 직전에 자료를 확인한 시도: {Judgments.Values.Sum(a => a.Count(x => x.ReadBefore))} / {attempts}회");
            sb.AppendLine();

            sb.AppendLine("## 4. 최종 보고").AppendLine();
            sb.AppendLine($"- 보고 화면을 연 횟수: {reportOpens}회" + (firstReportOpen >= 0 ? $" (처음 연 시각 {Seconds(firstReportOpen)})" : ""));
            sb.AppendLine($"- 제출하지 않고 조사로 돌아간 횟수: {reportReturns}회");
            sb.AppendLine($"- 결론·근거·조치를 바꾼 횟수: {reportEdits}회");
            if (Report == null) sb.AppendLine("- 아직 최종 제출을 하지 않았습니다.");
            else
            {
                sb.AppendLine($"- 제출 시각: {Seconds(reportSubmittedAt)}");
                sb.AppendLine($"- 결론: {Report.Conclusion}");
                sb.AppendLine($"- 제출 근거 ({Report.Proofs.Length}개): " + (Report.Proofs.Length == 0 ? "없음" : string.Join(", ", Report.Proofs)));
                sb.AppendLine($"- 미끼 근거 포함: {(Report.Decoy ? "예" : "아니오")}");
                sb.AppendLine($"- 시행 조치: {Report.Action}");
                sb.AppendLine($"- 엔딩: {Report.Ending}");
            }
            sb.AppendLine();

            double minutes = Math.Max((played - unfocusedTime) / 60.0, 1.0 / 60);
            sb.AppendLine("## 5. 피로감").AppendLine();
            sb.AppendLine($"- 전체 클릭: {clicks}회 (게임 창을 보고 있던 시간 기준 분당 {(clicks / minutes).ToString("0.0", CultureInfo.InvariantCulture)}회)");
            sb.AppendLine($"- 글이 긴 화면을 {SkimSeconds:0}초 안에 넘긴 횟수: {skims}회 (글이 긴 화면 {longScreens}개 중)");
            sb.AppendLine($"- 아무 화면도 바뀌지 않았는데 같은 버튼을 {RepeatWindowSeconds:0}초 안에 다시 누른 횟수: {repeats}회");
            if (skimScreens.Count > 0)
                sb.AppendLine("- 바로 넘긴 화면: " + string.Join(", ", skimScreens.OrderByDescending(p => p.Value).Take(5).Select(p => $"{Cell(p.Key)} ({p.Value}회)")));
            if (screenCounts.Count > 0)
                sb.AppendLine("- 가장 많이 본 화면: " + string.Join(", ", screenCounts.OrderByDescending(p => p.Value).Take(5).Select(p => $"{Cell(p.Key)} ({p.Value}회)")));
            if (locationTime.Count > 0)
                sb.AppendLine("- 장소별 머문 시간: " + string.Join(", ", locationTime.OrderByDescending(p => p.Value).Select(p => $"{Cell(p.Key)} {Seconds(p.Value)}")));
            if (objectiveTime.Count > 0)
            {
                sb.AppendLine().AppendLine("진행 구간별 시간 (화면 상단 목표 문구 기준)").AppendLine();
                sb.AppendLine("| 목표 문구 | 머문 시간 |").AppendLine("|---|---|");
                foreach (var p in objectiveTime.OrderByDescending(p => p.Value).Take(10)) sb.AppendLine($"| {Cell(p.Key)} | {Seconds(p.Value)} |");
            }
            sb.AppendLine();

            sb.AppendLine("## 관찰자 메모").AppendLine();
            sb.AppendLine("자동으로 기록할 수 없는 부분입니다. 옆에서 본 내용을 적어 주세요.").AppendLine();
            sb.AppendLine("- 표정이나 혼잣말로 드러난 막힘:");
            sb.AppendLine("- 지루해 보이던 구간:");
            sb.AppendLine("- 세 질문에서 고민하던 모습:");
            sb.AppendLine("- 테스트 후 질문에서 들은 말:");
            return sb.ToString();
        }
    }
}
