using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    // First-scene prototype state. No global save/ending implementation is replaced.
    public sealed class InvestigationSession : MonoBehaviour
    {
        public const string MissingDocumentClueId = "clue_document_missing";
        public const string TestimonyFlagId = "seolpyeong_training_testimony_heard";
        public const string Testimony = "저는 형주에서 채모 장군과 장윤 장군이 수군을 훈련하던 곳의 서리였습니다. 군량의 출납과 훈련 과정을 기록하는 일을 맡았습니다.";
        public InvestigationView view;
        public IntroductionStage introductionStage;
        public bool DocumentInspected { get; private set; }
        public bool TestimonyHeard { get; private set; }
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

        void Start()
        {
            if (introductionStage != null) introductionStage.Begin();
            view.SetPrologueLayout(true);
            view.SetPlayerIdentity("알현 대기", "조조군 지휘 천막");
            view.SetObjective("조조의 부름에 응하라");
            SetModal("서막 · 조조군 지휘 천막", "조조", "서기 208년, 적벽대전 직전.\n\n지휘 천막에 들어서자 군략 탁자 앞에 선 조조가 당신을 바라본다.\n\n“가까이 오라. 그대의 이름은 무엇인가?”", "이름은 최대 16자 · 빈칸이면 ‘이름 없는 참모’");
            enteringName = true;
            onEscape = null;
            view.AddNameEntry(BeginIntroduction);
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
                "사흘을 주겠다. 문서고를 살피고 관련자들의 말을 들어라. 누가 문서를 가져갔는지, 증거를 갖추어 내게 보고하라.",
                "우선 문서가 있던 자리부터 확인하라. 문서고 서기 설평에게도 물어보도록 하라." }, ShowInvestigationBrief);
        }

        void ShowInvestigationBrief()
        {
            SetModal("임무 수령 · 조조군 임시 참모", PlayerName + "의 첫 조사", "사라진 「화공대비책」의 행방과 범인을 밝혀라.\n\n문서고에서 빈 보관함을 확인하고, 서기 설평의 말을 들어보자.\n\n확인된 사실과 진술은 J키로 여는 수첩에 기록된다.", "E / Enter 계속 · 문서고로 이동한 뒤 조작 안내가 표시됩니다.");
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
            if (enteringName || !BlocksMovement || Time.frameCount <= modalOpenedFrame || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.escapeKey.wasPressedThisFrame) onEscape?.Invoke();
            else if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) onAdvance?.Invoke();
        }

        public void SetPrompt(string prompt) { if (!BlocksMovement) view.SetPrompt(prompt); }

        void SetModal(string eyebrow, string title, string body, string footer = "Esc  닫기")
        {
            BlocksMovement = true;
            modalOpenedFrame = Time.frameCount;
            onAdvance = null;
            onEscape = introductionFinished ? CloseModal : null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            view.ShowModal(eyebrow, title, body, footer);
        }

        public void CloseModal()
        {
            if (!introductionFinished) return;
            onSequenceComplete = null;
            sequence = null;
            onAdvance = null;
            onEscape = null;
            view.HideModal();
            BlocksMovement = false;
            modalClosedFrame = Time.frameCount;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ShowPause()
        {
            SetModal("잠시 멈춤", "문서고", "조사를 이어가거나 지금까지 확인한 사실을 살펴볼 수 있습니다.");
            view.AddChoice("조사 계속", CloseModal);
            view.AddChoice("수첩 열기", ShowJournal);
            onAdvance = CloseModal;
        }

        public void InspectDocument()
        {
            DocumentInspected = true;
            UpdateObjective();
            SetModal("현장 조사 · 문서 보관함", "사라진 화공대비책", "「화공대비책」이 있어야 할 보관 위치가 비어 있다.\n\n확인된 사실은 문서가 사라졌다는 것이다. 이 사실만으로 가져간 사람을 특정할 수는 없다.", "조사 기록이 수첩에 추가되었습니다.  ·  E / Esc 닫기");
            view.AddChoice("조사 계속", CloseModal);
            onAdvance = CloseModal;
        }

        public void TalkToSeolpyeong()
        {
            SetModal("문서고 서기", "설평", DocumentInspected
                ? "“화공대비책이 사라졌다고요? 무엇을 확인하고 싶으십니까?”"
                : "“문서를 찾으십니까? 저는 이곳의 서기 설평입니다.”");
            view.AddChoice("사라진 화공대비책에 관해", () => BeginSequence("설평", new[] {
                "그 문서라면 지정된 보관함에 있어야 합니다. 자리를 확인하셨습니까?",
                "저는 이곳에서 문서를 정리하고 기록을 맡습니다. 물으실 일이 있다면 말씀하십시오." }, TalkToSeolpyeong));
            view.AddChoice("형주에서 무슨 일을 했는가?", () => BeginSequence("설평", new[] { Testimony }, () => {
                TestimonyHeard = true;
                UpdateObjective();
                SetModal("진술 기록", "형주에서의 경력", "설평은 채모·장윤의 수군 훈련지에서 군량과 훈련 과정을 기록했다고 말했다.\n\n아직 확인되지 않은 본인의 진술이다. 이후 두 사람의 증언과 비교할 필요가 있다.", "진술이 수첩에 추가되었습니다.  ·  E 계속 / Esc 닫기");
                view.AddChoice("다른 질문", TalkToSeolpyeong);
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = TalkToSeolpyeong;
            }));
            view.AddChoice("대화 마치기", CloseModal);
        }

        void BeginSequence(string speaker, string[] lines, Action complete)
        {
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
            onAdvance = AdvanceDialogue;
        }

        public void AdvanceDialogue()
        {
            if (sequence == null) return;
            lineIndex++;
            if (lineIndex < sequence.Length) { ShowLine(); return; }
            var complete = onSequenceComplete;
            sequence = null;
            onSequenceComplete = null;
            complete?.Invoke();
        }

        public void ShowJournal()
        {
            string evidence = DocumentInspected
                ? "확인된 사실\n「화공대비책」이 있어야 할 보관 위치에서 사라졌다."
                : "확인된 사실\n아직 조사한 단서가 없다. 문서 보관함을 확인하자.";
            string testimony = TestimonyHeard
                ? "설평의 진술 · 미검증\n“" + Testimony + "”"
                : "인물 진술\n아직 기록한 진술이 없다. 설평에게 경력을 물어보자.";
            SetModal("조사 수첩", "사실과 진술", evidence + "\n\n" + testimony,
                FirstInvestigationComplete ? "첫 조사 완료 · 다음 조사할 곳을 선택할 수 있습니다." : "기록은 이번 실행 동안 유지됩니다.");
            if (FirstInvestigationComplete) view.AddChoice("다음 조사 선택", ShowStageSelection);
            view.AddChoice("수첩 닫기", CloseModal);
            onAdvance = CloseModal;
        }

        public void ShowStageSelection()
        {
            if (!introductionFinished || !FirstInvestigationComplete) return;
            sequence = null;
            onSequenceComplete = null;
            SetModal("조사 이동", "다음 조사 선택",
                "문서고의 첫 조사를 마쳤다.\n\n설평은 채모·장윤의 수군 훈련지에서 기록을 맡았다고 말했다. 두 사람의 진술을 들어 그의 경력을 확인할 필요가 있다.",
                "이름과 조사 기록은 유지됩니다.  ·  Esc 문서고로 돌아가기");
            view.AddChoice("채모·장윤 조사 · 준비 중", null, false);
            view.AddChoice("수첩 다시 보기", ShowJournal);
            view.AddChoice("문서고로 돌아가기", CloseModal);
        }

        void UpdateObjective()
        {
            string objective = !DocumentInspected ? "빈 문서 보관함을 조사하라"
                : !TestimonyHeard ? "설평에게 형주 시절의 경력을 물어라"
                : "첫 조사 완료 · J키로 수첩에서 다음 조사 선택";
            view.SetObjective(objective);
        }
    }
}
