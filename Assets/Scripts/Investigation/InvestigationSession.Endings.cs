using UnityEngine.SceneManagement;

namespace RedCliffMystery.Investigation
{
    public enum InvestigationEnding { None, True, Normal, Caimao, Zhangyun, Duseok, Unresolved }

    public static class EndingRules
    {
        public static InvestigationEnding ResolveFinal(string suspect, bool uncertain, bool expired, bool registry, bool letter)
        {
            if (uncertain || string.IsNullOrEmpty(suspect))
                return expired ? InvestigationEnding.Unresolved : InvestigationEnding.None;
            return Resolve(suspect, registry, letter);
        }
        // Ownership, rather than report checkbox selection, is the agreed condition.
        public static InvestigationEnding Resolve(string suspect, bool registry, bool letter)
        {
            switch (suspect)
            {
                case "설평": return registry && letter ? InvestigationEnding.True : InvestigationEnding.Normal;
                case "채모": return InvestigationEnding.Caimao;
                case "장윤": return InvestigationEnding.Zhangyun;
                case "두석": return InvestigationEnding.Duseok;
                default: return InvestigationEnding.None;
            }
        }
    }

    public sealed partial class InvestigationSession
    {
        public InvestigationEnding CurrentEnding { get; private set; }
        public bool HasHouseholdRegistryEvidence { get; private set; }
        public bool HasWuLetterEvidence { get; private set; }
        bool HasDecisiveEvidence => HasHouseholdRegistryEvidence && HasWuLetterEvidence;
        bool IsBadEnding => CurrentEnding == InvestigationEnding.Caimao
            || CurrentEnding == InvestigationEnding.Zhangyun
            || CurrentEnding == InvestigationEnding.Duseok
            || CurrentEnding == InvestigationEnding.Unresolved;
        bool executionTransition;
        bool normalEndingTransition;

        // Future investigation interactions call this only after validating the clue.
        // Career records and testimony contradictions never award these clues.
        public void RecordVerifiedKeyEvidence(string clueId)
        {
            if (FinalReportCompleted || reportActive) return;
            if (clueId == "clue_household_registry_no_record" && registryScopeChecked && registryNamesChecked
                && ContradictionFound && PersonnelRecordRead && DutyRecordRead) HasHouseholdRegistryEvidence = true;
            if (clueId == "clue_wu_secret_letter_true_name" && SearchAuthorized && LetterFound
                && TokenMatched && TokenOwnershipConfirmed && InterrogationCompleted && HasHouseholdRegistryEvidence)
                HasWuLetterEvidence = true;
        }

        string EndingTitle => CurrentEnding switch
        {
            InvestigationEnding.True => "진엔딩 · 되찾은 대비책",
            InvestigationEnding.Normal => "엔딩? · 끝내 닿지 못한 증거",
            InvestigationEnding.Caimao => "배드엔딩 · 빗나간 지휘선",
            InvestigationEnding.Zhangyun => "배드엔딩 · 장부 밖으로 사라진 문서",
            InvestigationEnding.Duseok => "배드엔딩 · 빈 경비 자리",
            InvestigationEnding.Unresolved => "배드엔딩 · 늦어 버린 보고",
            _ => "조사 보고"
        };

        string EndingJudgment => CurrentEnding switch
        {
            InvestigationEnding.True => "조조는 호적 대조 결과와 밀서를 차례로 살핀다.\n\n“서리로 일한 것은 사실이었다. 그러나 그 이름으로 감춘 충성까지 우리 것이었던 것은 아니구나.”\n\n설평의 신병을 확보하라는 명령이 내려진다.",
            InvestigationEnding.Normal => "“그자를 지목한 까닭은 들었다. 하지만 아직 밝혀지지 않은 일을 사실로 기록할 수는 없다.”\n\n조조는 보고에 남은 공백을 짚는다. 설평을 지목했지만 사건의 전모를 입증하지는 못했다.",
            InvestigationEnding.Caimao => "“훈련을 지휘한 책임과 문서를 훔친 죄는 같은 것이 아니다.”\n\n채모를 중심으로 지휘 계통과 훈련 기록을 살폈지만 절도를 입증할 근거는 나오지 않았다.",
            InvestigationEnding.Zhangyun => "“군량 장부를 살핀 것만으로 사라진 문서의 행방이 밝혀지지는 않았다.”\n\n장윤을 중심으로 조사가 이어졌지만 군량 기록은 문서의 반출 경로를 밝혀 주지 못했다.",
            InvestigationEnding.Duseok => "“자리를 비운 책임은 물을 것이다. 그러나 그자가 문서를 가져갔다는 증거는 어디 있느냐?”\n\n두석의 경비 공백은 문서를 훔쳤다는 증거가 되지 못했다.",
            _ => "“주어진 시간은 끝났다. 사라진 문서에 대비해야 할 때다.”\n\n조사는 사건을 해결하지 못한 채 끝났다."
        };

        string EndingAftermath => CurrentEnding switch
        {
            InvestigationEnding.True => "군사들이 설평의 몸에 숨겨진 화공대비책을 찾아낸다.\n\n설평, 본명 목연은 함께 근무하던 두석의 경비 자리가 빈 것을 직접 확인하고 서기의 접근 권한을 이용했다. 두석의 복귀 시각까지 알았던 것은 아니다.\n\n접선은 이달 열닷새, 나흘째 새벽 한 차례. 그 전에 따로 보낼 수 없어 문서를 몸에 지닌 채 기다리던 그의 임무는 예정된 인계 전에 막혔다.\n\n당신은 사라진 문서를 되찾았다.",
            InvestigationEnding.Normal => "보고가 끝난 뒤에도 화공대비책은 돌아오지 않았다.\n\n누구에게 있는지, 어디로 옮겨졌는지 확인되지 않는다.\n\n당신의 조사에는 끝내 메우지 못한 공백이 남았다.",
            _ => "이달 열닷새, 나흘째 새벽의 접선 시각이 지나고.\n\n오나라 측의 책상 위에 한 묶음의 문서가 놓인다. 펼쳐진 표지에는 ‘화공대비책’이라고 적혀 있다.\n\n누가 건넸는지는 보이지 않는다. 조조군의 대비책은 이미 강 건너에 도착했다."
        };

        string EndingOutcome => CurrentEnding == InvestigationEnding.True
            ? "범인과 위장 신원 확인\n화공대비책 회수\n오나라 전달 저지"
            : CurrentEnding == InvestigationEnding.Normal
                ? "설평 지목\n사건 입증 미완료\n문서 행방 불명"
                : "조사 미해결\n화공대비책 회수 실패\n나흘째 새벽 이후 · 오나라 전달 완료";

        void ShowEndingPage(int page)
        {
            view.SetInvestigationDate(page == 1 && IsBadEnding ? "나흘째 새벽 이후 · 이달 열닷새" : "");
            SetModal("결말 · " + (page + 1) + " / 2", EndingTitle,
                page == 0 ? EndingJudgment : EndingAftermath, "E / Enter 계속");
            onEscape = null;
            if (page == 0)
            {
                view.AddChoice("그 뒤", () => ShowEndingPage(1));
                onAdvance = () => ShowEndingPage(1);
            }
            else
            {
                if (IsBadEnding)
                {
                    view.AddChoice("마지막 명령", ShowExecutionSentence);
                    onAdvance = ShowExecutionSentence;
                }
                else if (CurrentEnding == InvestigationEnding.Normal)
                {
                    view.AddChoice("보고서를 덮는다", BeginNormalEndingEpilogue);
                    onAdvance = BeginNormalEndingEpilogue;
                }
                else
                {
                    view.AddChoice("조사 결과 보기", ShowCompletedReport);
                    onAdvance = ShowCompletedReport;
                }
            }
        }

        void BeginNormalEndingEpilogue()
        {
            if (normalEndingTransition) return;
            normalEndingTransition = true;
            onAdvance = null;
            onEscape = null;
            StartCoroutine(NormalEndingEpilogue());
        }

        System.Collections.IEnumerator NormalEndingEpilogue()
        {
            yield return view.FadeEndingToBlack();
            normalEndingTransition = false;
            SetModal("끝내 닿지 못한 증거", "엔딩?",
                "보고서는 덮였지만, 사라진 문서의 행방은 여전히 알 수 없다.\n\n그때 다른 질문을 했다면, 놓친 말을 다시 살폈다면…\n\n이 사건은 다른 결말에 닿을 수 있었을까?",
                "당신의 조사에는 아직 풀리지 않은 의문이 남아 있습니다.");
            onEscape = null;
            view.AddChoice("다시 조사한다", ConfirmRestart);
            view.AddChoice("이번 조사 결과를 확인한다", ShowCompletedReport);
        }

        void ShowExecutionSentence()
        {
            SetModal("배드엔딩 · 마지막 명령", "조조의 명령",
                "조조는 문서 유출과 조사 실패의 책임을 임시 참모인 당신에게 묻는다.\n\n“군의 기밀을 지키지 못한 책임을 물어 참모를 참형에 처하라.”\n\n군사들이 당신을 천막 밖으로 데려간다.", "E / Enter 계속");
            onEscape = null;
            view.AddChoice("계속", BeginExecutionBlackout);
            onAdvance = BeginExecutionBlackout;
        }

        void BeginExecutionBlackout()
        {
            if (executionTransition) return;
            executionTransition = true;
            onAdvance = null;
            onEscape = null;
            StartCoroutine(ExecutionBlackout());
        }

        System.Collections.IEnumerator ExecutionBlackout()
        {
            yield return view.FadeEndingToBlack();
            executionTransition = false;
            ShowBadEndingRetry();
        }

        void ShowBadEndingRetry()
        {
            SetModal("조사 종료", "다시 조사하시겠습니까?",
                "당신의 조사는 여기서 끝났습니다.\n\n다시 시작하면 이름 입력부터 진행하며, 현재 조사 기록은 초기화됩니다.", "원하는 항목을 선택하세요.");
            onEscape = null;
            view.AddChoice("예 · 처음부터 다시 조사", () => SceneManager.LoadScene(SceneManager.GetActiveScene().path));
            view.AddChoice("아니요 · 결과 확인", ShowCompletedReport);
        }

        void ConfirmRestart()
        {
            SetModal("다시 시작", "이번 조사를 끝내고 처음으로 돌아갑니까?",
                "이름 입력부터 다시 시작합니다. 현재 조사 기록과 보고 내용은 초기화됩니다.");
            onEscape = ShowCompletedReport;
            view.AddChoice("처음부터 시작", () => SceneManager.LoadScene(SceneManager.GetActiveScene().path));
            view.AddChoice("결과 화면으로 돌아가기", ShowCompletedReport);
        }
    }
}
