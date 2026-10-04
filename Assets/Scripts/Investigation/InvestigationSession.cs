using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession : MonoBehaviour
    {
        public const int JournalPageCount = 18;
        public const string Testimony = "저는 형주 동쪽 수채의 서리였습니다. 출입 시설을 정비하던 닷새에도 현장에서 출항하는 배를 확인하고 훈련 내용을 기록했습니다. 그동안 군량도 계속 지급됐습니다.";
        public const string DuseokTestimony = "문서고를 지키던 중 잠깐 자리를 비웠을 뿐입니다. 오래 걸리지는 않았습니다. 제가 자리를 비운 동안 누가 드나들었는지는 보지 못했습니다.";
        public const string CaimaoTestimony = "동쪽 수채의 출입 시설을 정비한 닷새 동안, 그곳에서 밖으로 출항하는 훈련은 중단했소. 정박한 배에서의 작업이나 다른 수채의 훈련을 말하는 것이 아니오.";
        public const string ZhangyunTestimony = "그 정비 닷새에도 군량은 지급했소. 동쪽 수채의 병사들은 선박 보수와 육상 훈련을 하고 있었으니까. 군량 지급이 출항 훈련을 했다는 뜻은 아니오.";
        public const string MaintenanceOrder = "동쪽 수채 출입 시설 정비 · 첫날부터 닷새째까지\n\n이 기간 동쪽 수채 밖으로 출항하는 훈련을 중단한다. 병사들은 선박 보수와 육상 훈련을 계속한다.";
        public const string ReconfirmedTestimony = "명령서에 적힌 그 닷새, 그 동쪽 수채가 맞습니다. 다른 사람의 보고서를 옮긴 것이 아니라 출항하는 배를 제가 직접 보고 기록했습니다.";
        public InvestigationView view;
        public IntroductionStage introductionStage;
        public InvestigationRoute route;
        public bool WorkRouteHeard { get; private set; }
        public bool AccessAuthorityHeard { get; private set; }
        public bool DocumentInspected { get; private set; }
        public bool TestimonyHeard { get; private set; }
        public bool DuseokTestimonyHeard { get; private set; }
        public bool CaimaoTestimonyHeard { get; private set; }
        public bool ZhangyunTestimonyHeard { get; private set; }
        public bool MaintenanceOrderRead { get; private set; }
        public bool TestimonyReconfirmed { get; private set; }
        public bool ContradictionFound { get; private set; }
        public bool InvestigationReviewStarted { get; private set; }
        string selectedInvestigationTarget;
        bool OfficersHeard => CaimaoTestimonyHeard && ZhangyunTestimonyHeard;
        public bool CanCompareTestimonies => TestimonyHeard && CaimaoTestimonyHeard && ZhangyunTestimonyHeard
            && MaintenanceOrderRead && TestimonyReconfirmed;
        public string PlayerName { get; private set; } = "이름 없는 참모";
        public bool BlocksMovement { get; private set; } = true;
        public bool CanProcessInvestigationInput => !BlocksMovement && Time.frameCount > modalClosedFrame;
        public bool FirstInvestigationComplete => DocumentInspected && TestimonyHeard;
        string[] sequence;
        string sequenceSpeaker;
        int lineIndex;
        Action onSequenceComplete;
        Action onAdvance;
        Action onEscape;
        int modalOpenedFrame;
        int modalClosedFrame = -1;
        bool enteringName;
        bool introductionFinished;
        string LocationName => visitingCommandTent || reportActive ? "조조군 지휘 천막"
            : route != null && route.AtNavalCamp ? "조조군 수군 진영" : "조조군 문서고";

        void Start()
        {
            Application.runInBackground = true;
            if (introductionStage != null) introductionStage.Begin();
            view.SetPrologueLayout(true);
            view.SetPlayerIdentity("알현 대기", "조조군 지휘 천막");
            view.SetObjective("조조의 부름에 응하라");
            SetModal("서막 · 조조군 지휘 천막", "조조", "서기 208년, 적벽대전 직전.\n\n지휘 천막에 들어서자 군략 탁자 앞에 선 조조가 당신을 바라본다.\n\n“가까이 오라. 그대의 이름은 무엇인가?”", "이름은 최대 16자 · 빈칸이면 ‘이름 없는 참모’");
            enteringName = true;
            onEscape = null;
            view.AddNameEntry(BeginIntroduction);
            view.AddChoice("저장한 조사 불러오기", () => ConfirmLoad(Start));
        }

        public void BeginIntroduction(string enteredName)
        {
            if (!enteringName) return;
            string cleaned = (enteredName ?? "").Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "");
            PlayerName = string.IsNullOrWhiteSpace(cleaned) ? "이름 없는 참모" : cleaned.Substring(0, Mathf.Min(16, cleaned.Length));
            enteringName = false;
            view.SetPlayerIdentity(PlayerName, "조조군 지휘 천막");
            BeginSequence("조조", new[] {
                $"{PlayerName}, 그대를 이번 사건을 조사할 임시 참모로 삼겠다.",
                "문서고에 보관하던 화공대비책이 사라졌다. 적의 화공에 대비하기 위한 문서다. 군의 안위가 걸린 일이니 가벼이 여길 수 없다.",
                "오늘은 이달 열이튿날이다. 열나흗날 밤까지 사흘을 주겠다. 관련자들의 말을 듣고 증거를 갖추어 보고하라. 기한을 넘기고도 답을 내놓지 못한다면… 그대를 참모로 세운 책임까지 내가 대신 져 주지는 않을 것이다.",
                "우선 문서가 있던 자리부터 확인하라. 문서고 서기 설평에게도 물어보도록 하라." }, ShowInvestigationBrief);
        }

        void ShowInvestigationBrief()
        {
            SetModal("임무 수령 · 조조군 임시 참모", PlayerName + "의 첫 조사", "사라진 「화공대비책」의 행방과 범인을 밝혀라.\n\n문서고에서 빈 보관함을 확인하고, 서기 설평의 말을 들어보자. 입구를 돌아보면 경비 두석도 만날 수 있다.\n\nJ키로 수첩을 연다. 인물과 자료 모두 새 조사 주제를 시작할 때 1회를 쓴다. 하루 오전·오후·저녁·밤, 사흘 총 12회다. 확보한 기록을 다시 읽는 데는 중복 차감하지 않는다.", "E / Enter 계속 · 문서고로 이동한 뒤 조작 안내가 표시됩니다.");
            onEscape = null;
            view.AddChoice("명을 받고 문서고로 향한다", FinishIntroduction);
            onAdvance = FinishIntroduction;
        }

        void FinishIntroduction()
        {
            if (introductionStage != null) introductionStage.EnterArchive();
            view.SetPrologueLayout(false);
            view.SetPlayerIdentity(PlayerName);
            UpdateObjective();
            introductionFinished = true;
            CloseModal();
        }

        void Update()
        {
            RefreshInvestigationTime();
            if (HandleSaveShortcuts()) return;
            if (!Application.isFocused || dayTransitionActive || enteringName || !BlocksMovement || Time.frameCount <= modalOpenedFrame || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.escapeKey.wasPressedThisFrame) onEscape?.Invoke();
            else if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) onAdvance?.Invoke();
        }

        public void SetPrompt(string prompt) { if (!BlocksMovement) view.SetPrompt(prompt); }

        void SetModal(string eyebrow, string title, string body, string footer = "Esc  닫기")
        {
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
            view.ShowModal(eyebrow, title, body, footer);
            if (introductionFinished && !FinalReportCompleted)
                view.SetFooterHint(footer + "  ·  F5 저장 / F9 불러오기");
        }

        public void CloseModal()
        {
            if (dayTransitionActive) return;
            if (!introductionFinished) return;
            if (reportActive) { CancelFinalReport(); return; }
            if (visitingCommandTent) { LeaveCommandTent(tentVisitFromNaval); return; }
            if (investigationTime.DeadlineReached && !skipDeadlinePrompt) { ShowDeadlineChoice(); return; }
            skipDeadlinePrompt = false;
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
            view.AddChoice("조사 계속", CloseModal);
            view.AddChoice("수첩 열기", ShowJournal);
            view.AddChoice("장소 이동", ShowTravel);
            view.AddChoice("날짜와 조사 일정 / 시간 보내기", ShowInvestigationSchedule);
            view.AddChoice("저장 / 불러오기", ShowSaveMenu);
            onAdvance = CloseModal;
        }

        public void InspectDocument()
        {
            SetSavePoint("document");
            if (!DocumentInspected && !EnsureInquiry("document", "문서 보관함 조사", InspectDocument)) return;
            DocumentInspected = true;
            UpdateObjective();
            SetModal("현장 조사 · 문서 보관함", "사라진 화공대비책", "「화공대비책」이 있어야 할 보관 위치가 비어 있다.\n\n확인된 사실은 문서가 사라졌다는 것이다. 이 사실만으로 가져간 사람을 특정할 수는 없다.", "조사 기록이 수첩에 추가되었습니다.  ·  E / Esc 닫기");
            view.AddChoice("조사 계속", CloseModal);
            onAdvance = CloseModal;
        }

        public void TalkToSeolpyeong()
        {
            SetSavePoint("seol");
            if (!EnsureInquiry("seol_initial", "설평 · 업무와 경력", TalkToSeolpyeong)) return;
            SetModal("문서고 서기", "설평", DocumentInspected
                ? "“화공대비책이 사라졌다고요? 무엇을 확인하고 싶으십니까?”"
                : "“문서를 찾으십니까? 저는 이곳의 서기 설평입니다.”");
            view.AddChoice("이곳에서 맡은 일은 무엇인가?", () => BeginSequence("설평", new[] {
                "반납된 문서를 확인하고 지정된 보관함에 넣는 일을 맡았습니다. 보관함과 기록 탁자 사이를 자주 오갔지요.",
                "그렇게 오가면 입구의 경비 자리도 보입니다. 찾으시는 문서가 있다면 보관 위치를 안내해 드리겠습니다." }, () => { WorkRouteHeard = true; TalkToSeolpyeong(); }));
            view.AddChoice("화공대비책은 어디에서 관리했는가?", () => BeginSequence("설평", new[] {
                "화공대비책은 가운데 책상의 지정된 보관함에 두었습니다. 그 자리를 확인하셨습니까?",
                "문서가 사라졌다면 관련자들의 말도 확인하셔야 할 것입니다." }, TalkToSeolpyeong));
            view.AddChoice("형주에서 무슨 일을 했는가?", () => BeginSequence("설평", new[] { Testimony }, () => {
                TestimonyHeard = true;
                UpdateObjective();
                SetModal("진술 기록 · 미검증", "형주에서의 경력", "설평은 동쪽 수채의 정비 닷새에도 출항 훈련을 직접 보고 기록했다고 말했다.\n\n들은 내용을 수첩에 기록했다. 수군 진영에서는 당시 훈련과 군량에 관한 설명도 들을 수 있다.", "진술이 수첩에 추가되었습니다.  ·  E 계속 / Esc 닫기");
                view.AddChoice("다른 질문", TalkToSeolpyeong);
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = TalkToSeolpyeong;
            }));
            view.AddChoice("서류와 개인 물건에 관해", ShowSeolpyeongFollowUp);
            view.AddChoice("대화 마치기", CloseModal);
        }

        void ReconfirmTrainingTestimony()
        {
            if (!EnsureInquiry("seol_reconfirm", "설평 · 현장 경험 재확인", ReconfirmTrainingTestimony)) return;
            BeginSequence("설평", new[] {
                "동쪽 수채의 출입 시설을 정비하던 때를 묻는 것이지요? 정비 전이나 후의 일을 말한 것이 아닙니다.",
                ReconfirmedTestimony }, () => {
                TestimonyReconfirmed = true;
                UpdateObjective();
                SetModal("재확인 · 미검증 진술", "같은 닷새, 같은 수채", ReconfirmedTestimony + "\n\n진술의 시기·장소·직접 목격 여부를 확인했다. 수첩에서 다른 기록과 비교하자.");
                view.AddChoice("수첩 열기", ShowJournal);
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = CloseModal;
            });
        }

        public void InspectMaintenanceOrder()
        {
            SetSavePoint("maintenance");
            if (!MaintenanceOrderRead && !EnsureInquiry("maintenance", "정비 명령서 조사", InspectMaintenanceOrder)) return;
            MaintenanceOrderRead = true;
            UpdateObjective();
            SetModal("자료 확인 · 정비 명령서", "동쪽 수채의 정비 닷새", MaintenanceOrder,
                "명령서 내용이 수첩에 기록되었습니다.  ·  E / Esc 닫기");
            view.AddChoice("조사 계속", CloseModal);
            onAdvance = CloseModal;
        }

        public void TalkToDuseok()
        {
            SetSavePoint("duseok");
            SetModal("문서고 경비", "두석", "“문서고의 경비를 맡은 두석입니다. 출입 상황에 대해 물으시겠습니까?”");
            view.AddChoice("경비 시간대를 더 조사한다", ShowDuseokTimelineInquiry);
            view.AddChoice("맡은 경비에 관해", () => BeginSequence("두석", new[] {
                "문서고 입구에서 경비를 섭니다. 안쪽 문서를 정리하는 일은 서기가 맡습니다.",
                "설평은 서기라 문서를 정리하러 드나들었습니다. 그때마다 따로 허락을 받는 사람은 아니었습니다. 그것이 제가 없는 동안의 행동까지 보았다는 뜻은 아닙니다." }, () => { AccessAuthorityHeard = true; TalkToDuseok(); }));
            view.AddChoice("근무 중 자리를 비운 적이 있는가?", () => BeginPaidSequence("duseok_initial", "두석 · 경비 상황", "두석", new[] { DuseokTestimony }, () => {
                DuseokTestimonyHeard = true;
                SetModal("진술 기록 · 미검증", "경비의 공백", "두석은 경비 중 자리를 비웠다고 말했다.\n\n정확한 시각과 그때의 출입자는 아직 확인하지 못했다. 이 진술만으로 문서를 가져간 사람을 정할 수는 없다.", "수첩에 기록되었습니다.  ·  E / Esc 대화 마치기");
                view.AddChoice("다른 질문", TalkToDuseok);
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = CloseModal;
            }));
            view.AddChoice("수군 진영으로 가려면?", () => BeginSequence("두석", new[] {
                "제 옆의 문을 나서면 수군 진영으로 갈 수 있습니다. 남은 문서고 조사는 돌아와서 이어가셔도 됩니다.",
                "진영 입구에서 바라보면 왼쪽 천막 앞에 채모 장군, 오른쪽 천막 앞에 장윤 장군이 계십니다. 돌아오실 때는 입구의 나무 표지를 살펴보십시오." }, ShowTravel));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void TalkToCaimao() => TalkToNavalOfficer(true);
        public void TalkToZhangyun() => TalkToNavalOfficer(false);

        void TalkToNavalOfficer(bool caimao)
        {
            SetSavePoint(caimao ? "caimao" : "zhangyun");
            if (!EnsureInquiry(caimao ? "caimao" : "zhangyun", caimao ? "채모 · 훈련 설명" : "장윤 · 군량 설명", () => TalkToNavalOfficer(caimao))) return;
            string speaker = caimao ? "채모" : "장윤";
            string testimony = caimao ? CaimaoTestimony : ZhangyunTestimony;
            Action talkAgain = caimao ? TalkToCaimao : TalkToZhangyun;
            SetModal("수군 진영", speaker, "“문서고에서 조사를 위해 왔다고 들었소. 무엇을 확인하고 싶소?”");
            view.AddChoice("당시 수군 훈련과 기록에 관해", () => BeginSequence(speaker, new[] {
                caimao ? "동쪽 수채의 출입 시설을 정비하던 때 말이오? 중앙 기록 탁자에 당시 정비 명령서가 있소."
                    : "동쪽 수채를 정비하던 닷새의 군량 지급과 병사들의 일과를 설명하겠소.",
                testimony }, () => {
                if (caimao) CaimaoTestimonyHeard = true; else ZhangyunTestimonyHeard = true;
                UpdateObjective();
                if (OfficersHeard)
                {
                    ShowFollowUpSelection();
                    return;
                }
                SetModal("진술 기록 · 미검증", speaker + "의 설명", testimony + "\n\n들은 설명을 수첩에 기록했다. 다른 관련자의 설명도 들어보자.", "E / Esc 대화 마치기");
                view.AddChoice("다른 질문", () => talkAgain());
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = CloseModal;
            }));
            view.AddChoice("당시 기록은 어떻게 확인할 수 있소?", () => BeginSequence(speaker, new[] {
                "말만으로 신원을 단정하지 마시오. 당시의 기록과 업무를 아는 사람들의 설명을 함께 확인해야 하오." }, talkAgain));
            view.AddChoice("대화 마치기", CloseModal);
        }

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

        public void ShowJournal() => ShowJournalPage(0);

        void ShowJournalPage(int page)
        {
            page = Mathf.Clamp(page, 0, JournalPageCount - 1);
            var record = GetJournalPage(page);
            SetModal("조사 수첩 · " + (page + 1) + " / " + JournalPageCount, record.title, record.body,
                "사실과 진술을 구분해 기록합니다.  ·  E 다음 기록 / Esc 닫기");
            if (page > 0) view.AddChoice("이전 기록", () => ShowJournalPage(page - 1));
            view.AddChoice("다음 기록", () => ShowJournalPage((page + 1) % JournalPageCount));
            if (page == 0 && introductionFinished)
            {
                view.AddChoice("진술 비교", ShowTestimonyComparison);
                view.AddChoice("다음 조사 선택", OfficersHeard ? ShowFollowUpSelection : ShowStageSelection);
            }
            if (page > 0) view.AddChoice("수첩 처음으로", ShowJournal);
            view.AddChoice("수첩 닫기", CloseModal);
            onAdvance = () => ShowJournalPage((page + 1) % JournalPageCount);
        }

        (string title, string body) GetJournalPage(int page)
        {
            switch (page)
            {
                case 0: return ("사실과 확인할 일", "확인된 사실\n" + (DocumentInspected
                    ? "「화공대비책」이 있어야 할 보관 위치가 비어 있다." : "아직 현장에서 확인한 사실이 없다.")
                    + "\n\n확인할 일\n" + NextInvestigationStep());
                case 1: return ("설평의 경력", TestimonyHeard ? "인물 진술 · 미검증\n“" + Testimony + "”"
                    : "아직 진술을 기록하지 않았다.\n설평에게 형주에서 맡았던 일을 물어보자.");
                case 2: return ("두석의 경비", DuseokTestimonyHeard ? "인물 진술 · 미검증\n“" + DuseokTestimony + "”\n\n정확한 시각과 출입자는 추가 확인이 필요하다."
                    : "아직 진술을 기록하지 않았다.\n문서고 입구의 두석에게 경비 상황을 물을 수 있다.");
                case 3: return ("채모의 설명", CaimaoTestimonyHeard ? "인물 진술 · 미검증\n“" + CaimaoTestimony + "”"
                    : "아직 진술을 기록하지 않았다.\n수군 진영에서 채모를 만날 수 있다.");
                case 4: return ("장윤의 설명", ZhangyunTestimonyHeard ? "인물 진술 · 미검증\n“" + ZhangyunTestimony + "”"
                    : "아직 진술을 기록하지 않았다.\n수군 진영에서 장윤을 만날 수 있다.");
                case 5: return (MaintenanceOrderRead ? "정비 명령서" : "현장 자료 기록", MaintenanceOrderRead ? "확인한 자료\n" + MaintenanceOrder
                    : "아직 이 항목에 기록한 자료가 없다.");
                case 6: return ("추가로 확인한 진술", TestimonyReconfirmed ? "설평의 추가 진술 · 미검증\n“" + ReconfirmedTestimony + "”"
                    : "아직 추가로 확인한 진술이 없다. 누구에게 무엇을 더 물을지는 직접 결정할 수 있다.");
                case 7: return (PersonnelRecordRead ? "형주 인력 인계 명부" : "인력 자료 기록", PersonnelRecordRead ? PersonnelRecord : "아직 이 항목에 기록한 자료가 없다.");
                case 8: return (DutyRecordRead ? "이전 근무 기록" : "업무 자료 기록", DutyRecordRead ? DutyRecord + "\n\n" + CareerFinding : "아직 이 항목에 기록한 자료가 없다.");
                case 9: return ("개인 목패", TokenObserved ? TokenDescription : "아직 확인한 개인 물건이 없다.");
                case 10: return ("신원 대조", HasHouseholdRegistryEvidence ? RegistryFinding : "신고 신원은 아직 대조하지 않았다.");
                case 11: return (LetterFound ? "개인 서류 조사" : "추가 자료 기록", LetterFound ? SecretLetter : "아직 이 항목에 기록한 자료가 없다.");
                case 12: return ("후속 조사", FollowUpProgress());
                case 14: return ("두석 시간대 조사 · 증언", DuseokWitnessChecklist() + "\n\n" + DuseokInvestigationGuide());
                case 17: return (LetterFound ? "밀서의 접선 일정" : "일정 대조 기록", LetterFound ? ContactScheduleFinding : "아직 대조한 일정이 없다.");
                case 16: return ("문서 취급 권한과 업무 동선", WorkAccessFinding());
                case 15: return ("두석 시간대 조사 · 결과", DuseokTimelineComplete ? DuseokTimeline : "주변 증언을 모아 두석에게 재질문하고 결과를 정리할 수 있다. 이 조사는 선택 사항이다.");
                default: return ("모순 검토 결과", ContradictionFound
                    ? "발견한 모순\n중단된 출항 훈련을 설평은 현장에서 직접 기록했다고 주장했다.\n\n직접 목격했다는 주장에 모순이 있다. 서리로 근무한 경력까지 거짓이라고 단정할 수는 없다."
                    : "아직 모순을 판정하지 않았다.\n기록을 검토한 뒤 추가로 조사할 사람을 고르거나, 현재 기록으로 조사를 정리할 수 있다.");
            }
        }

        string WorkAccessFinding() =>
            (WorkRouteHeard ? "설평의 설명: 문서 반납·정리를 맡아 보관함과 기록 탁자를 오가며 입구 경비 자리를 볼 수 있었다." : "설평의 업무 동선은 아직 듣지 않았다.")
            + "\n\n" + (AccessAuthorityHeard ? "두석의 설명: 설평은 문서 정리를 위해 매번 별도 허가 없이 드나들었다. 두석은 자신이 자리를 비운 동안의 행동을 보지 못했다." : "두석에게 문서 출입 권한을 확인하지 않았다.")
            + "\n\n" + (WorkRouteHeard && AccessAuthorityHeard ? "두 설명은 업무상 접근 기회가 있었음을 뒷받침한다. 실제로 부재를 알아차렸는지, 문서를 가져갔는지는 입증되지 않았다." : "들은 진술만 기록한다. 접근 가능성과 실제 절도는 구분해야 한다.");

        public void ShowTestimonyComparison()
        {
            SetSavePoint("comparison");
            if (!introductionFinished) return;
            if (!CanCompareTestimonies)
            {
                SetModal("수첩 · 비교 준비", "출항 훈련 진술 비교",
                    "아직 같은 사건에 관한 설명을 비교할 만큼 기록이 모이지 않았다. 들은 말과 확인한 자료를 수첩에서 살펴보고, 무엇을 더 확인할지 결정하자.");
                view.AddChoice("수첩으로 돌아가기", ShowJournal);
                view.AddChoice("조사 계속", CloseModal);
                return;
            }

            SetModal("수첩 · 진술 비교", "무엇이 모순인가?",
                "같은 기간과 장소에 관한 진술과 기록을 확보했다. 수첩에 적힌 각자의 설명을 바탕으로, 함께 성립하기 어려운 주장을 선택하자.");
            view.AddChoice("훈련을 중단했으니 군량도 지급할 수 없다", () => ShowComparisonMistake(
                "병사들은 선박 보수와 육상 훈련을 계속했다. 출항 훈련을 중단했어도 군량을 지급할 수 있다."));
            view.AddChoice("중단된 출항 훈련을 직접 기록했다고 한다", ConfirmContradiction);
            view.AddChoice("문서고 서기이므로 절도범이다", () => ShowComparisonMistake(
                "문서고에서 일한다는 사실만으로 절도를 입증할 수 없다. 같은 닷새에 관한 현장 경험과 기록을 비교하자."));
            view.AddChoice("수첩으로 돌아가기", ShowJournal);
        }

        static string RecordStatus(bool recorded, string label) => (recorded ? "확인 · " : "미확인 · ") + label + "\n";

        void ShowComparisonMistake(string reason)
        {
            SetModal("비교 다시 생각하기", "기록을 다시 확인하자", reason + "\n\n기록은 그대로 남아 있다. 다시 비교할 수 있다.");
            view.AddChoice("다시 비교", ShowTestimonyComparison);
            view.AddChoice("수첩으로 돌아가기", ShowJournal);
        }

        void ConfirmContradiction()
        {
            if (!CanCompareTestimonies) return;
            ContradictionFound = true;
            UpdateObjective();
            SetModal("모순 발견 · 경력 확인", "직접 목격했다는 주장의 모순",
                "설평은 중단된 출항 훈련을 같은 기간·장소에서 직접 기록했다고 주장했다. 군량 지급만으로 그 경험을 설명할 수는 없다.\n\n실제 담당 업무를 기록과 대조할 필요가 있다. 이 모순만으로 근무 경력 전체나 그의 신원, 절도를 판단할 수는 없다.");
            view.AddChoice("수첩 열기", ShowJournal);
            view.AddChoice("조사 계속", CloseModal);
            onAdvance = CloseModal;
        }

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
            bool atNaval = route != null && route.AtNavalCamp;
            SetModal("조사 · 이동", "다음 조사 선택", atNaval
                ? "왼쪽 천막 앞에 채모, 오른쪽 천막 앞에 장윤이 있다. 입구 왼편에는 기록 담당자와 경력 서류 탁자가 있다.\n\n문서고로 돌아가 다시 질문할 수도 있다."
                : "수군 진영에서 채모·장윤의 설명을 들을 수 있다.\n\n문서고에 남아 추가로 조사해도 좋다. 뒤쪽 출입문 옆의 창을 든 경비가 두석이다.",
                "이름과 조사 기록은 유지됩니다.  ·  Esc 현재 장소에서 계속");
            if (atNaval)
            {
                view.AddChoice("문서고로 돌아가기", ReturnToArchive);
                view.AddChoice("조조군 지휘 천막으로 이동", VisitCommandTent);
                view.AddChoice("수군 진영 조사 계속", CloseModal);
            }
            else
            {
                view.AddChoice("수군 진영으로 이동", EnterNavalCamp, route != null && route.IsReady);
                view.AddChoice("조조군 지휘 천막으로 이동", VisitCommandTent);
                view.AddChoice("문서고 추가 조사", CloseModal);
            }
            view.AddChoice("현재 기록으로 조사 정리", ShowInvestigationReview);
            view.AddChoice("수첩 다시 보기", ShowJournal);
        }

        public void ShowFollowUpSelection()
        {
            SetSavePoint("followup");
            if (!introductionFinished) return;
            SetModal("조사 방향 선택", "누구를 더 조사하겠습니까?",
                "추가로 확인할 사람을 고르거나 현재 기록으로 조사를 정리할 수 있다. 확인하지 않은 진술과 증거는 미확인 상태로 남는다.");
            view.AddChoice("채모 · 출항 훈련", () => SelectInvestigationTarget("채모"));
            view.AddChoice("장윤 · 군량과 일과", () => SelectInvestigationTarget("장윤"));
            view.AddChoice("설평 · 서기 업무와 경력", () => SelectInvestigationTarget("설평"));
            view.AddChoice("두석 · 문서고 경비", () => SelectInvestigationTarget("두석"));
            view.AddChoice("추가 조사하지 않고 넘어간다", ShowInvestigationReview);
        }

        void SelectInvestigationTarget(string target)
        {
            selectedInvestigationTarget = target;
            InvestigationReviewStarted = false;
            UpdateObjective();
            bool needsNavalCamp = target == "채모" || target == "장윤";
            bool atNavalCamp = route != null && route.AtNavalCamp;
            SetModal("추가 조사", target + "에게 다시 묻기",
                (needsNavalCamp ? "수군 진영" : "문서고") + "에서 " + target + "을 만나 원하는 질문을 선택하자.\n\n추가 조사 대상을 선택했다. 이 선택이 범인 지목을 뜻하지는 않는다.");
            if (needsNavalCamp != atNavalCamp)
                view.AddChoice(needsNavalCamp ? "수군 진영으로 이동" : "문서고로 이동",
                    needsNavalCamp ? EnterNavalCamp : ReturnToArchive, route != null && route.IsReady);
            else view.AddChoice("직접 찾아가 대화한다", CloseModal);
            view.AddChoice("다른 조사 선택", ShowFollowUpSelection);
        }

        void ShowInvestigationReview()
        {
            SetSavePoint("review");
            InvestigationReviewStarted = true;
            selectedInvestigationTarget = null;
            UpdateObjective();
            SetModal("조사 정리", "현재까지의 기록",
                "추가 조사 없이 현재 기록을 정리했다.\n\n"
                + RecordStatus(DocumentInspected, "화공대비책의 부재")
                + RecordStatus(DuseokTestimonyHeard, "문서고 경비 상황")
                + RecordStatus(MaintenanceOrderRead, "당시 정비 명령서")
                + RecordStatus(ContradictionFound, "진술 사이의 모순")
                + "\n조조에게 보고하거나 추가 조사를 계속할 수 있다.");
            view.AddChoice("조조에게 최종 보고", BeginFinalReport);
            view.AddChoice("기록 살펴보기", ShowJournal);
            view.AddChoice("추가 조사로 돌아가기", ShowFollowUpSelection);
            view.AddChoice("정리를 마치고 닫기", CloseModal);
        }

        public void EnterNavalCamp()
        {
            if (!introductionFinished || route == null || !route.IsReady) return;
            route.EnterNavalCamp();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        public void ReturnToArchive()
        {
            if (!introductionFinished || route == null || !route.IsReady) return;
            route.EnterArchive();
            view.SetPlayerIdentity(PlayerName, LocationName);
            UpdateObjective();
            CloseModal();
        }

        void UpdateObjective() => view.SetObjective(NextInvestigationStep());

        string NextInvestigationStep()
        {
            if (FinalReportCompleted) return "최종 보고 완료";
            if (InvestigationReviewStarted) return "조사 정리 완료 · J 수첩에서 기록 확인 / 추가 조사 선택";
            if (!string.IsNullOrEmpty(selectedInvestigationTarget))
                return "선택한 추가 조사 · " + selectedInvestigationTarget + "에게 대화 / J 수첩에서 조사 대상 변경";
            return "누구의 말과 어떤 자료를 확인할지 결정하자 · J 기록 / Esc 이동·일정·보고";
        }
    }
}
