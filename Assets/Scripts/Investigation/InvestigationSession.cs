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
        public const string DuseokTestimony = "문서고를 지키던 중 잠시 자리를 비운 적은 있습니다. 제가 자리를 비운 동안 누가 드나들었는지는 보지 못했습니다.";
        public const string CaimaoTestimony = "수군의 훈련 기록을 확인하려면 어느 시기의 어떤 훈련인지부터 좁혀야 하오. 군량이 지급됐다는 사실만으로 수상 훈련이 있었다고 단정할 수는 없소.";
        public const string ZhangyunTestimony = "수상 훈련을 하지 않는 날에도 병사에게 군량은 지급하오. 훈련 기록과 군량 출납은 같은 일을 기록하는 문서가 아니오.";
        public InvestigationView view;
        public IntroductionStage introductionStage;
        public InvestigationRoute route;
        public bool DocumentInspected { get; private set; }
        public bool TestimonyHeard { get; private set; }
        public bool DuseokTestimonyHeard { get; private set; }
        public bool CaimaoTestimonyHeard { get; private set; }
        public bool ZhangyunTestimonyHeard { get; private set; }
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
        string LocationName => route != null && route.AtNavalCamp ? "조조군 수군 진영" : "조조군 문서고";

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
            SetModal("잠시 멈춤", LocationName, "조사를 이어가거나 지금까지 확인한 사실을 살펴볼 수 있습니다.");
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
            view.AddChoice("이곳에서 맡은 일은 무엇인가?", () => BeginSequence("설평", new[] {
                "저는 이곳의 서기입니다. 문서를 정리하고 기록하는 일을 맡고 있습니다.",
                "찾으시는 문서가 있다면 말씀하십시오. 어느 보관 자리를 확인해야 할지 안내해 드리겠습니다." }, TalkToSeolpyeong));
            view.AddChoice("화공대비책은 어디에서 관리했는가?", () => BeginSequence("설평", new[] {
                "화공대비책은 가운데 책상의 지정된 보관함에 두었습니다. 그 자리를 확인하셨습니까?",
                "문서가 사라졌다면 관련자들의 말도 확인하셔야 할 것입니다." }, TalkToSeolpyeong));
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

        public void TalkToDuseok()
        {
            SetModal("문서고 경비", "두석", "“문서고의 경비를 맡은 두석입니다. 출입 상황에 대해 물으시겠습니까?”");
            view.AddChoice("맡은 경비에 관해", () => BeginSequence("두석", new[] {
                "문서고 입구에서 경비를 섭니다. 안쪽 문서를 정리하는 일은 서기가 맡습니다." }, TalkToDuseok));
            view.AddChoice("근무 중 자리를 비운 적이 있는가?", () => BeginSequence("두석", new[] { DuseokTestimony }, () => {
                DuseokTestimonyHeard = true;
                SetModal("진술 기록 · 미검증", "경비의 공백", "두석은 경비 중 자리를 비웠다고 말했다.\n\n정확한 시각과 그때의 출입자는 아직 확인하지 못했다. 이 진술만으로 문서를 가져간 사람을 정할 수는 없다.", "수첩에 기록되었습니다.  ·  E / Esc 대화 마치기");
                view.AddChoice("다른 질문", TalkToDuseok);
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = CloseModal;
            }));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void TalkToCaimao() => TalkToNavalOfficer(true);
        public void TalkToZhangyun() => TalkToNavalOfficer(false);

        void TalkToNavalOfficer(bool caimao)
        {
            string speaker = caimao ? "채모" : "장윤";
            string testimony = caimao ? CaimaoTestimony : ZhangyunTestimony;
            Action talkAgain = caimao ? TalkToCaimao : TalkToZhangyun;
            SetModal("수군 진영", speaker, "“문서고에서 조사를 위해 왔다고 들었소. 무엇을 확인하고 싶소?”");
            view.AddChoice("당시 수군 훈련과 기록에 관해", () => BeginSequence(speaker, new[] {
                caimao ? "설평이 형주에서 수군 훈련을 기록했다고 말했다는 것이오? 그가 맡았다는 업무부터 차근차근 대조해 봅시다."
                    : "군량 출납과 훈련 기록을 맡았다는 말만으로는 어느 현장에 있었는지 알기 어렵소.",
                testimony }, () => {
                if (caimao) CaimaoTestimonyHeard = true; else ZhangyunTestimonyHeard = true;
                UpdateObjective();
                SetModal("진술 기록 · 미검증", speaker + "의 설명", testimony + "\n\n설평의 진술과 함께 수첩에 기록했다. 구체적인 시기와 사건을 대조해야 한다.", "E / Esc 대화 마치기");
                view.AddChoice("다른 질문", () => talkAgain());
                view.AddChoice("대화 마치기", CloseModal);
                onAdvance = CloseModal;
            }));
            view.AddChoice("설평의 경력을 확인하려면?", () => BeginSequence(speaker, new[] {
                "말만으로 신원을 단정하지 마시오. 당시의 기록과 업무를 아는 사람들의 설명을 함께 확인해야 하오." }, talkAgain));
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

        public void ShowJournal() => ShowJournalPage(0);

        void ShowJournalPage(int page)
        {
            string question = !FirstInvestigationComplete ? "문서의 보관 자리와 담당자의 설명을 확인하자."
                : !CaimaoTestimonyHeard || !ZhangyunTestimonyHeard ? "채모·장윤에게 당시 훈련과 기록 업무를 물어보자."
                : "훈련 여부와 군량 지급을 구분해, 설평이 말한 경력의 구체적인 시기와 사건을 대조하자.";
            string[] titles = { "사실과 확인할 일", "설평의 경력", "두석의 경비", "채모의 설명", "장윤의 설명" };
            string[] records = {
                "확인된 사실\n" + (DocumentInspected ? "「화공대비책」이 있어야 할 보관 위치가 비어 있다." : "아직 현장에서 확인한 사실이 없다.") + "\n\n확인할 일\n" + question,
                TestimonyHeard ? "인물 진술 · 미검증\n“" + Testimony + "”" : "아직 진술을 기록하지 않았다.\n설평에게 형주에서 맡았던 일을 물어보자.",
                DuseokTestimonyHeard ? "인물 진술 · 미검증\n“" + DuseokTestimony + "”\n\n정확한 시각과 출입자는 추가 확인이 필요하다." : "아직 진술을 기록하지 않았다.\n문서고 입구의 두석에게 경비 상황을 물을 수 있다.",
                CaimaoTestimonyHeard ? "인물 진술 · 미검증\n“" + CaimaoTestimony + "”" : "아직 진술을 기록하지 않았다.\n수군 진영에서 채모를 만날 수 있다.",
                ZhangyunTestimonyHeard ? "인물 진술 · 미검증\n“" + ZhangyunTestimony + "”" : "아직 진술을 기록하지 않았다.\n수군 진영에서 장윤을 만날 수 있다."
            };
            page = Mathf.Clamp(page, 0, records.Length - 1);
            int current = page;
            SetModal("조사 수첩 · " + (page + 1) + " / " + records.Length, titles[page], records[page],
                "사실과 진술을 구분해 기록합니다.  ·  E 다음 기록 / Esc 닫기");
            view.AddChoice("이전 기록", () => ShowJournalPage((current + records.Length - 1) % records.Length));
            view.AddChoice("다음 기록", () => ShowJournalPage((current + 1) % records.Length));
            if (FirstInvestigationComplete) view.AddChoice("다음 조사 선택", ShowStageSelection);
            view.AddChoice("수첩 닫기", CloseModal);
            onAdvance = () => ShowJournalPage((current + 1) % records.Length);
        }

        public void ShowStageSelection()
        {
            if (!introductionFinished || !FirstInvestigationComplete) return;
            sequence = null;
            onSequenceComplete = null;
            bool atNaval = route != null && route.AtNavalCamp;
            SetModal("조사 · 이동", "다음 조사 선택", atNaval
                ? "채모와 장윤은 어느 순서로 만나도 된다.\n\n문서고로 돌아가 보관함을 다시 살피거나 설평과 두석에게 질문할 수도 있다."
                : "수군 진영에서 채모·장윤의 설명을 들을 수 있다.\n\n문서고에 남아 추가로 조사해도 좋다. 두석은 입구에서 경비를 맡고 있다.",
                "이름과 조사 기록은 유지됩니다.  ·  Esc 현재 장소에서 계속");
            if (atNaval)
            {
                view.AddChoice("문서고로 돌아가기", ReturnToArchive);
                view.AddChoice("수군 진영 조사 계속", CloseModal);
            }
            else
            {
                view.AddChoice("수군 진영으로 이동", EnterNavalCamp, route != null && route.IsReady);
                view.AddChoice("문서고 추가 조사", CloseModal);
            }
            view.AddChoice("수첩 다시 보기", ShowJournal);
        }

        public void EnterNavalCamp()
        {
            if (!introductionFinished || !FirstInvestigationComplete || route == null || !route.IsReady) return;
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

        void UpdateObjective()
        {
            string objective = !DocumentInspected ? "빈 문서 보관함을 조사하라"
                : !TestimonyHeard ? "설평에게 형주 시절의 경력을 물어라"
                : "다음 조사 가능 · J키로 이동 선택 / 문서고 추가 조사";
            if (route != null && route.AtNavalCamp)
                objective = !CaimaoTestimonyHeard && !ZhangyunTestimonyHeard ? "채모와 장윤에게 훈련·기록에 관해 물어라 · 조사 순서 자유"
                    : !CaimaoTestimonyHeard ? "채모의 설명을 들어라"
                    : !ZhangyunTestimonyHeard ? "장윤의 설명을 들어라"
                    : "두 사람의 진술 확보 · J키로 기록 검토 / 문서고 재방문";
            view.SetObjective(objective);
        }
    }
}
