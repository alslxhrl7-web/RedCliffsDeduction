using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession : MonoBehaviour
    {
        public InvestigationView view;
        public IntroductionStage introductionStage;
        public InvestigationRoute route;
        public string PlayerName { get; private set; } = "이름 없는 참모";
        public bool BlocksMovement { get; private set; } = true;
        public bool CanProcessInvestigationInput => !BlocksMovement && (route==null || !route.IsTravelling) && Time.frameCount > modalClosedFrame;
        string[] sequence;
        string sequenceSpeaker;
        int lineIndex;
        Action onSequenceComplete;
        Action onAdvance;
        Action onEscape;
        int modalOpenedFrame;
        int modalClosedFrame = -1;
        bool enteringName;
        bool titleScreenActive;
        float nextArchiveCapture;
        bool introductionFinished;
        string LocationName => visitingCommandTent || reportActive || route != null && route.AtCommandTent ? "조조군 지휘 천막"
            : route != null && route.OnConnectingRoad ? "진영 연결 길"
            : route != null && route.AtSupplyTent ? "조조군 보급 천막" : route != null && route.AtNavalCamp ? "조조군 수군 진영" : route!=null && route.connectedWalkway && route.OnOutdoorGround ? "수군 진영 연결 길" : "조조군 문서고";

        void Start()
        {
            Application.runInBackground = true;
            // Hidden playtest log; stays off unless "playtest.on" exists in the game folder. Never records the name.
            PlaytestObserver.BeginIfRequested(() => PlayerName);
            titleScreenActive = true;
            view.gameObject.SetActive(false);
            InvestigationTitleScreen.Show(view.font, () => {
                titleScreenActive = false;
                view.gameObject.SetActive(true);
                ShowOpening();
            });
        }

        void ShowOpening()
        {
            if (introductionStage != null) introductionStage.Begin();
            view.SetPrologueLayout(true);
            view.SetPlayerIdentity("알현 대기", "조조군 지휘 천막");
            view.SetObjective("조조의 부름에 응하라");
            SetModal("서막 · 조조군 지휘 천막", "조조", "서기 208년, 적벽대전 직전.\n\n지휘 천막에 들어서자 군략 탁자 앞에 선 조조가 당신을 바라본다.\n\n“가까이 오라. 그대의 이름은 무엇인가?”", "이름은 최대 16자 · 빈칸이면 ‘이름 없는 참모’");
            enteringName = true;
            onEscape = null;
            view.AddNameEntry(BeginIntroduction);
            view.AddChoice("저장한 조사 불러오기", () => ConfirmLoad(ShowOpening));
        }

        public void BeginIntroduction(string enteredName)
        {
            if (!enteringName) return;
            string cleaned = (enteredName ?? "").Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "");
            PlayerName = string.IsNullOrWhiteSpace(cleaned) ? "이름 없는 참모" : cleaned.Substring(0, Mathf.Min(16, cleaned.Length));
            enteringName = false;
            view.SetPlayerIdentity(PlayerName, "조조군 지휘 천막");
            BeginOrderIntroduction();
        }

        void FinishIntroduction()
        {
            if (introductionStage != null) introductionStage.EnterArchive();
            view.SetPrologueLayout(false);
            view.SetPlayerIdentity(PlayerName);
            UpdateObjective();
            introductionFinished = true;
            CloseModal();
            ShowTutorialOffer();
        }

        void Update()
        {
            UpdateInvestigationTutorial();
            RefreshGuidance();
            if (RecordArchiveScreen.IsOpen) return;
            if (titleScreenActive) return;
            if (Time.unscaledTime >= nextArchiveCapture) { nextArchiveCapture=Time.unscaledTime+.5f; RecordArchive.Capture(this); }
            if(route!=null && route.IsTravelling) return;
            RefreshInvestigationTime();
            if (HandleSaveShortcuts()) return;
            if (!Application.isFocused || dayTransitionActive || enteringName || !BlocksMovement || Time.frameCount <= modalOpenedFrame || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.qKey.wasPressedThisFrame && view.title.text == "어느 쪽을 확인할까?") { CloseModal(); return; }
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (onEscape != null) PlaytestObserver.Click("[키보드] Esc", true);
                onEscape?.Invoke();
            }
            else if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                // Keyboard advances count like clicks so quick skimming through dialogue shows up in the playtest log.
                if (onAdvance != null) PlaytestObserver.Click("[키보드] E/Enter 계속", true);
                onAdvance?.Invoke();
            }
        }

        public void SetPrompt(string prompt) { if (!BlocksMovement) view.SetPrompt(prompt); }

        void SetModal(string eyebrow, string title, string body, string footer = "Esc  닫기")
        {
            RecordArchive.Capture(this);
            VarcoIdleMotion.StopSpeech();
            int quote = body.IndexOf('“'), endQuote = body.LastIndexOf('”');
            if (quote >= 0 && endQuote > quote) VarcoIdleMotion.Speak(title, body.Substring(quote + 1, endQuote - quote - 1));
            RefreshInvestigationTime();
            BlocksMovement = true;
            modalOpenedFrame = Time.frameCount;
            onAdvance = null;
            onEscape = reportActive ? CancelFinalReport : introductionFinished ? CloseModal : null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!string.IsNullOrEmpty(pendingInquiryNotice)) {
                body = pendingInquiryNotice + "\n\n" + body;
                pendingInquiryNotice = null;
            }
            view.ShowModal(eyebrow, title, body, footer);
            if (introductionFinished && !FinalReportCompleted)
                view.SetFooterHint(footer + "  ·  F5 저장 / F9 불러오기");
        }

        public void CloseModal()
        {
            if (dayTransitionActive) return;
            if (!introductionFinished) return;
            if (reportActive) { CancelFinalReport(); return; }
            if (visitingCommandTent) { LeaveCommandTent(tentVisitFromNaval, tentVisitFromSupply); return; }
            saveResumePoint = "field";
            onSequenceComplete = null;
            sequence = null;
            onAdvance = null;
            onEscape = null;
            view.HideModal();
            VarcoIdleMotion.StopSpeech();
            BlocksMovement = false;
            modalClosedFrame = Time.frameCount;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ShowPause()
        {
            SetModal("잠시 멈춤", LocationName, "조사를 이어가거나 지금까지 확인한 사실을 살펴볼 수 있습니다.");
            PlaytestObserver.Pause();
            view.AddChoice("조사 계속", CloseModal);
            view.AddChoice("조사 방향 선택", ShowInvestigationDirections);
            view.AddChoice("수첩 열기", ShowJournal);
            view.AddChoice("장소 이동", ShowTravel);
            view.AddChoice("저장 / 불러오기", ShowSaveMenu);
            view.AddChoice("소리 설정", ShowAudioSettings);
            view.AddChoice("조사 방법", ShowInvestigationHelp);
            onAdvance = CloseModal;
        }

        public void InspectDocument() => ShowOrderArchive();
        public void TalkToSeolpyeong() => ShowOrderSeol();
        public void TalkToDuseok() => ShowOrderDuseok();
        public void TalkToCaimao() => ShowOrderOfficer(true);
        public void TalkToZhangyun() => ShowOrderOfficer(false);
        public void InspectMaintenanceOrder() => ShowOrderMap();
        public void TalkToDocumentCourier() => ShowOrderCourier();

        bool sequenceIsPlayer;
        void BeginSequence(string speaker, string[] lines, Action complete, bool playerSpeaking = false)
        {
            sequenceIsPlayer = playerSpeaking;
            sequence = lines;
            sequenceSpeaker = speaker;
            lineIndex = 0;
            onSequenceComplete = complete;
            ShowLine();
        }

        void ShowLine()
        {
            SetModal(introductionFinished ? "대화" : "서막 · 조조의 명령", sequenceSpeaker, "“" + sequence[lineIndex] + "”",
                $"{lineIndex + 1} / {sequence.Length}  ·  E / Enter 계속" + (introductionFinished ? " · Esc 대화 종료" : ""));
            view.AddChoice(lineIndex == sequence.Length - 1 ? (introductionFinished ? "진술 확인" : "명을 받들겠습니다") : "계속", AdvanceDialogue);
            if (!introductionFinished) view.AddChoice("도입 건너뛰기", SkipOpeningBriefing);
            if (sequenceIsPlayer) VarcoIdleMotion.StopSpeech();
            onAdvance = AdvanceDialogue;
        }

        public void AdvanceDialogue()
        {
            if (sequence == null) return;
            VarcoIdleMotion.StopSpeech();
            lineIndex++;
            if (lineIndex < sequence.Length) { ShowLine(); return; }
            var complete = onSequenceComplete;
            sequence = null;
            onSequenceComplete = null;
            complete?.Invoke();
        }

        public void ShowJournal() { PlaytestObserver.Journal(); ShowOrderJournal(); }

        public void ShowTravel()
        {
            if (!introductionFinished) return;
            sequence = null;
            onSequenceComplete = null;
            if (route == null || !route.IsReady)
            {
                SetModal("조사 · 이동", "이동할 수 없음", "이동 경로가 연결되지 않았습니다. 현재 장소에서 조사를 계속할 수 있습니다.");
                view.AddChoice("조사 계속", CloseModal);
                return;
            }
            ShowStageSelection();
        }

        public void ShowStageSelection()
        {
            SetSavePoint("travel");
            if (!introductionFinished) return;
            sequence = null;
            onSequenceComplete = null;
            if (route != null && route.AtSupplyTent)
            {
                SetModal("조사 · 이동", "보급 천막", "기록 담당자와 문서 전령, 휴식 중인 병사가 이곳에 있다. 수군 진영 중앙의 채모·장윤과는 따로 떨어져 있다.\n\n수군 진영으로 돌아갈 수 있다.");
                view.AddChoice("수군 진영으로 돌아가기", EnterNavalCamp, route.IsReady);
                view.AddChoice("보급 천막 조사 계속", CloseModal);
                view.AddChoice("수첩 다시 보기", ShowJournal);
                return;
            }
            bool atNaval = route != null && route.AtNavalCamp;
            SetModal("조사 · 이동", "다음 조사 선택", atNaval
                ? "왼쪽 천막 앞에 채모, 오른쪽에 장윤이 있다. 중앙 탁자에서 배치도를 확인할 수 있다. 전달자, 기록 담당자, 경력 서류 탁자는 옆의 보급 천막에 있다."
                : "수군 진영에서 채모·장윤의 설명을 들을 수 있다.\n\n문서고에 남아 추가로 조사해도 좋다. 뒤쪽 출입문 옆의 창을 든 경비가 두석이다.",
                "이름과 조사 기록은 유지됩니다.  ·  Esc 현재 장소에서 계속");
            if (atNaval)
            {
                view.AddChoice("문서고로 돌아가기", ReturnToArchive);
                view.AddChoice("보급 천막으로 이동", EnterSupplyTent, route != null && route.SupplyTentReady);
                view.AddChoice("조조군 지휘 천막으로 이동", VisitCommandTent);
                view.AddChoice("수군 진영 조사 계속", CloseModal);
            }
            else
            {
                view.AddChoice("수군 진영으로 이동", EnterNavalCamp, route != null && route.IsReady);
                view.AddChoice("조조군 지휘 천막으로 이동", VisitCommandTent);
                view.AddChoice("문서고 추가 조사", CloseModal);
            }
            view.AddChoice("현재 기록으로 조사 정리", ShowOrderJournal);
            view.AddChoice("수첩 다시 보기", ShowJournal);
        }

        public void EnterSupplyTent()
        {
            if (!introductionFinished || route == null || !route.SupplyTentReady) return;
            if (route.IsTravelling) return;
            route.EnterSupplyTent();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        public void EnterNavalCamp()
        {
            if (!introductionFinished || route == null || !route.IsReady) return;
            if(route.IsTravelling) return;
            if (route.AtSupplyTent || route.AtCommandTent)
            {
                route.EnterNavalCamp();
                view.SetPlayerIdentity(PlayerName, LocationName);
                UpdateObjective();
                CloseModal();
                return;
            }
            if(route.animateDeparture && route.curtain!=null && !route.AtNavalCamp)
            {
                view.HideModal(); VarcoIdleMotion.StopSpeech();
                route.DepartThroughCurtain(() => {
                    view.SetPlayerIdentity(PlayerName,LocationName); CloseModal();
                    view.SetObjective("열린 천막을 지나 등불이 있는 수군 진영 입구로 걸어가자");
                });
                return;
            }
            route.EnterNavalCamp();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        public void ReturnToArchive()
        {
            if(route!=null && route.IsTravelling) return;
            if (!introductionFinished || route == null || !route.IsReady) return;
            if(route.connectedWalkway && route.animateDeparture) {
                route.BeginReturnWalk(); CloseModal(); view.SetObjective("연결 길을 따라 문서고의 열린 천막으로 돌아가자"); return;
            }
            route.EnterArchive();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        void UpdateObjective() => view.SetObjective(CaseSummary().Split('\n')[0]);
        public void RefreshWalkLocation() { view.SetPlayerIdentity(PlayerName,LocationName); UpdateObjective(); }

        string NextInvestigationStep() => OrderObjective();
    }
}
