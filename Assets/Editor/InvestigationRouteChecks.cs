using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class InvestigationRouteChecks
    {
        const string Reconfirmation = "정비 닷새의 현장 경험 재확인";
        const string CorrectComparison = "중단된 출항 훈련을 직접 기록했다고 한다";
        const string WrongRations = "훈련을 중단했으니 군량도 지급할 수 없다";
        const string WrongAccusation = "문서고 서기이므로 절도범이다";

        [MenuItem("적벽추리/조사 동선 검사 (문서 먼저)")]
        public static void RunDocumentFirst() => Run(true);

        [MenuItem("적벽추리/조사 동선 검사 (진술 먼저)")]
        public static void RunTestimonyFirst() => Run(false);

        [MenuItem("적벽추리/현재 게임 화면 저장")]
        public static void CaptureGameView()
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            System.IO.Directory.CreateDirectory("Temp");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            ScreenCapture.CaptureScreenshot("Temp/InvestigationPreview.png");
        }

        static void Run(bool documentFirst)
        {
            Require(Application.isPlaying, "Start a fresh Play session first.");
            Application.runInBackground = true;
            var s = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            Require(s != null && s.route != null && s.route.IsReady, "Investigation route is not connected.");
            Require(!s.DocumentInspected && !s.TestimonyHeard && !s.MaintenanceOrderRead &&
                !s.TestimonyReconfirmed && !s.ContradictionFound && s.BlocksMovement, "Use a fresh Play session.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1,
                "Exactly one EventSystem required.");
            Require(s.view.font.HasCharacter('설') && s.view.font.HasCharacter('석') && s.view.font.HasCharacter('닫'),
                "Korean font unavailable.");

            CheckIntroductionAndArchive(s, documentFirst);
            Travel(s, true);
            CheckComparisonLocked(s, "Only the archive investigation is complete.");
            if (documentFirst)
            {
                // Obtain the other evidence first so each officer is independently required.
                ReadMaintenanceOrder(s);
                Travel(s, false);
                ReconfirmTestimony(s);
                CheckComparisonLocked(s, "Neither officer has been heard.");
                Travel(s, true);
                TalkOfficer(s, true, true);
                CheckComparisonLocked(s, "Zhangyun has not been heard.");
                TalkOfficer(s, false, false);
            }
            else
            {
                // Reverse the order to exercise the document and reconfirmation prerequisites.
                TalkOfficer(s, false, true);
                CheckComparisonLocked(s, "Caimao has not been heard.");
                TalkOfficer(s, true, false);
                CheckComparisonLocked(s, "The maintenance order has not been read.");
                ReadMaintenanceOrder(s);
                CheckComparisonLocked(s, "Seolpyeong has not reconfirmed his account.");
                Travel(s, false);
                ReconfirmTestimony(s);
            }
            Require(s.CanCompareTestimonies && !s.ContradictionFound && !s.DuseokTestimonyHeard,
                "Comparison must require all five records but not Duseok.");
            CheckComparisonChoices(s);
            CheckReturnTravel(s);
            CheckOptionalGuard(s);
            Interact(s, InvestigationTarget.TargetKind.MissingDocument);
            Interact(s, InvestigationTarget.TargetKind.MissingDocument);
            s.CloseModal();
            Travel(s, true);
            ReadMaintenanceOrder(s);
            Require(s.ContradictionFound, "Re-reading evidence reset the deduction.");
            CheckJournal(s);
            CheckRaycasts(s.route.player);
            s.ShowTestimonyComparison();
            Debug.Log("INVESTIGATION_ROUTE_CHECKS_PASSED: " + (documentFirst ? "document-first" : "testimony-first") +
                "; optional guard, cancellation, comparison prerequisites, wrong answers, retry, deduction, " +
                "return travel, all journal pages, Korean layout and six core target raycasts.");
        }

        static void CheckIntroductionAndArchive(InvestigationSession s, bool documentFirst)
        {
            string introTitle = s.view.title.text;
            s.ShowStageSelection();
            Require(s.view.title.text == introTitle, "Selection interrupted the introduction.");
            var input = s.view.choices.GetComponentInChildren<UnityEngine.UI.InputField>();
            Require(input != null, "Name input is missing.");
            input.text = "동선 검증 참모";
            Click(s, "이 이름으로 시작");
            CompleteDialogue(s);
            Click(s, "명을 받고 문서고로 향한다");
            Require(!s.BlocksMovement, "Introduction did not release movement.");
            s.EnterNavalCamp();
            Require(s.route.AtNavalCamp && !s.FirstInvestigationComplete,
                "Early naval travel must not require or award archive evidence.");
            s.ReturnToArchive();
            s.ShowJournal();
            Require(HasChoice(s, "다음 조사 선택") && HasChoice(s, "진술 비교"), "Journal navigation unavailable before archive completion.");
            s.CloseModal();
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            Click(s, "서류와 개인 물건에 관해");
            Require(!HasChoice(s, "서류 묶음의 목패는 누구의 것인가?") && !s.TokenObserved,
                "Token question leaked before discovery.");
            s.CloseModal();
            Interact(s, InvestigationTarget.TargetKind.PersonalToken);
            s.CloseModal();
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            Click(s, "서류와 개인 물건에 관해");
            Click(s, "서류 묶음의 목패는 누구의 것인가?");
            s.CloseModal();
            Require(!s.TokenOwnershipConfirmed, "Cancelled ownership dialogue was awarded.");
            if (documentFirst)
            {
                Interact(s, InvestigationTarget.TargetKind.MissingDocument);
                s.CloseModal();
            }
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            CheckLayout(s);
            Click(s, "형주에서 무슨 일을 했는가?");
            s.CloseModal();
            Require(!s.TestimonyHeard, "Cancelled testimony was awarded.");
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            Click(s, "형주에서 무슨 일을 했는가?");
            CompleteDialogue(s);
            Require(s.TestimonyHeard, "Completed testimony missing.");
            Require(s.view.title.text == "형주에서의 경력", "Completion replaced the statement result.");
            s.CloseModal();
            if (!documentFirst)
            {
                Interact(s, InvestigationTarget.TargetKind.MissingDocument);
                s.CloseModal();
            }
            Require(s.FirstInvestigationComplete && !s.DuseokTestimonyHeard, "Duseok must remain optional.");
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            Require(!HasChoice(s, Reconfirmation), "Reconfirmation needs the maintenance order.");
            s.CloseModal();
        }

        static void ReadMaintenanceOrder(InvestigationSession s)
        {
            Interact(s, InvestigationTarget.TargetKind.MaintenanceOrder);
            Require(s.MaintenanceOrderRead, "Maintenance order was not recorded.");
            CheckLayout(s);
            Click(s, "조사 계속");
        }

        static void ReconfirmTestimony(InvestigationSession s)
        {
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            CheckLayout(s);
            Click(s, "서류와 개인 물건에 관해");
            Click(s, Reconfirmation);
            Click(s, "계속");
            s.CloseModal();
            Require(!s.TestimonyReconfirmed, "Cancelled reconfirmation was awarded.");
            CheckComparisonLocked(s, "Reconfirmation was cancelled before its final confirmation.");
            Interact(s, InvestigationTarget.TargetKind.Seolpyeong);
            Click(s, "서류와 개인 물건에 관해");
            Click(s, Reconfirmation);
            CompleteDialogue(s);
            Require(s.TestimonyReconfirmed, "Completed reconfirmation missing.");
            CheckLayout(s);
            s.CloseModal();
        }

        static void TalkOfficer(InvestigationSession s, bool caimao, bool cancelFirst)
        {
            var kind = caimao ? InvestigationTarget.TargetKind.Caimao : InvestigationTarget.TargetKind.Zhangyun;
            Interact(s, kind);
            Click(s, "당시 수군 훈련과 기록에 관해");
            if (cancelFirst)
            {
                Click(s, "계속");
                s.CloseModal();
                Require(!(caimao ? s.CaimaoTestimonyHeard : s.ZhangyunTestimonyHeard),
                    "Incomplete officer dialogue was awarded.");
                Interact(s, kind);
                Click(s, "당시 수군 훈련과 기록에 관해");
            }
            CompleteDialogue(s);
            Require(caimao ? s.CaimaoTestimonyHeard : s.ZhangyunTestimonyHeard, "Officer testimony missing.");
            CheckLayout(s);
            s.CloseModal();
        }

        static void CheckComparisonLocked(InvestigationSession s, string reason)
        {
            Require(!s.CanCompareTestimonies && !s.ContradictionFound, "Premature comparison readiness: " + reason);
            OpenComparison(s);
            Require(!HasChoice(s, CorrectComparison) && !HasChoice(s, WrongRations) && !HasChoice(s, WrongAccusation),
                "Incomplete evidence exposed deduction choices: " + reason);
            CheckLayout(s);
            Click(s, "수첩으로 돌아가기");
            Require(!s.ContradictionFound, "Checklist awarded a deduction: " + reason);
            s.CloseModal();
        }

        static void CheckComparisonChoices(InvestigationSession s)
        {
            OpenComparison(s);
            CheckLayout(s);
            Require(HasChoice(s, CorrectComparison) && HasChoice(s, WrongRations) && HasChoice(s, WrongAccusation),
                "Complete evidence did not unlock the three choices.");
            Click(s, "수첩으로 돌아가기");
            Require(!s.ContradictionFound && s.CanCompareTestimonies, "Cancelling comparison altered progress.");
            OpenComparison(s);
            Click(s, WrongRations);
            Require(!s.ContradictionFound && s.CanCompareTestimonies, "Wrong ration inference awarded or blocked deduction.");
            CheckLayout(s);
            Click(s, "다시 비교");
            Click(s, WrongAccusation);
            Require(!s.ContradictionFound && s.CanCompareTestimonies, "Premature accusation awarded or blocked deduction.");
            CheckLayout(s);
            Click(s, "수첩으로 돌아가기");
            OpenComparison(s);
            Click(s, CorrectComparison);
            Require(s.ContradictionFound, "Correct comparison was not recorded.");
            Require(s.view.title.text == "직접 목격했다는 주장의 모순", "Deduction must distinguish career from eyewitness claims.");
            Require(!s.DuseokTestimonyHeard, "The optional guard was silently awarded or required.");
            CheckLayout(s);
            Click(s, "조사 계속");
            OpenComparison(s);
            Require(s.ContradictionFound, "Reopening comparison reset its conclusion.");
            CheckLayout(s);
            Click(s, CorrectComparison);
            Require(s.ContradictionFound, "Repeating the correct deduction reset its conclusion.");
            Click(s, "조사 계속");
        }

        static void OpenComparison(InvestigationSession s)
        {
            s.ShowJournal();
            Click(s, "진술 비교");
        }

        static void CheckReturnTravel(InvestigationSession s)
        {
            string name = s.PlayerName;
            for (int i = 0; i < 3; i++)
            {
                Travel(s, !s.route.AtNavalCamp);
                Travel(s, !s.route.AtNavalCamp);
                Require(s.PlayerName == name && s.FirstInvestigationComplete && s.CaimaoTestimonyHeard &&
                    s.ZhangyunTestimonyHeard && s.MaintenanceOrderRead && s.TestimonyReconfirmed && s.ContradictionFound,
                    "Travel lost investigation progress.");
                Require(!s.DuseokTestimonyHeard, "Travel awarded the optional guard testimony.");
            }
        }

        static void Travel(InvestigationSession s, bool naval)
        {
            if (s.route.AtNavalCamp == naval) return;
            s.ShowTravel();
            CheckLayout(s);
            Click(s, naval ? "수군 진영으로 이동" : "문서고로 돌아가기");
            Require(s.route.AtNavalCamp == naval && !s.BlocksMovement, "Investigation travel failed.");
            Require(!s.CanProcessInvestigationInput, "Closing input must not be consumed twice.");
            var arrival = naval ? s.route.navalArrival : s.route.archiveArrival;
            Require(Vector3.Distance(s.route.player.transform.position, arrival.position) < .1f, "Wrong arrival point.");
            Require(s.route.player.viewCamera.transform.localRotation == Quaternion.identity, "Travel did not reset view.");
        }

        static void CheckOptionalGuard(InvestigationSession s)
        {
            Travel(s, false);
            Interact(s, InvestigationTarget.TargetKind.Duseok);
            Click(s, "근무 중 자리를 비운 적이 있는가?");
            s.CloseModal();
            Require(!s.DuseokTestimonyHeard, "Cancelled guard testimony was awarded.");
            Interact(s, InvestigationTarget.TargetKind.Duseok);
            Click(s, "근무 중 자리를 비운 적이 있는가?");
            CompleteDialogue(s);
            Require(s.DuseokTestimonyHeard && s.ContradictionFound, "Guard testimony altered the completed deduction.");
            CheckLayout(s);
            Click(s, "대화 마치기");
        }

        static void CheckJournal(InvestigationSession s)
        {
            Require(InvestigationSession.JournalPageCount == 18, "Eighteen journal pages expected.");
            s.ShowJournal();
            string firstTitle = s.view.title.text;
            var pages = new string[InvestigationSession.JournalPageCount];
            var titles = new string[pages.Length];
            for (int i = 0; i < pages.Length; i++)
            {
                CheckLayout(s);
                pages[i] = s.view.body.text;
                titles[i] = s.view.title.text;
                Click(s, "다음 기록");
            }
            Require(titles.Distinct().Count() == pages.Length && s.view.title.text == firstTitle,
                "Journal skipped or duplicated a page.");
            Require(pages[0].Contains("확인된 사실") && pages[0].Contains("확인할 일"),
                "Journal lost fact/question separation.");
            string[] statements = { InvestigationSession.Testimony, InvestigationSession.DuseokTestimony,
                InvestigationSession.CaimaoTestimony, InvestigationSession.ZhangyunTestimony };
            for (int i = 0; i < statements.Length; i++)
                Require(pages[i + 1].Contains(statements[i]) && pages[i + 1].Contains("미검증"),
                    "Statement missing or labelled as fact.");
            Require(pages[5].Contains(InvestigationSession.MaintenanceOrder), "Maintenance order missing from the journal.");
            Require(pages[6].Contains(InvestigationSession.ReconfirmedTestimony) && pages[6].Contains("미검증"),
                "Reconfirmed testimony missing or labelled as fact.");
            Require(pages[13].Contains("경력"), "Deduction follow-up missing from the journal.");
        }

        static void Interact(InvestigationSession s, InvestigationTarget.TargetKind kind)
        {
            var target = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsSortMode.None)
                .Single(t => t.kind == kind);
            target.Interact(s);
            AcceptInvestigationCost(s);
        }

        static void CompleteDialogue(InvestigationSession s)
        {
            // Use the visible player actions, not direct evidence flags or completion callbacks.
            for (int step = 0; step < 12; step++)
            {
                var next = Buttons(s).SingleOrDefault(b => b.name == "계속" || b.name == "진술 확인" || b.name == "명을 받들겠습니다");
                if (next == null) return;
                CheckLayout(s);
                Click(s, next.name);
            }
            throw new InvalidOperationException("Dialogue did not finish within twelve lines.");
        }

        static UnityEngine.UI.Button[] Buttons(InvestigationSession s) =>
            s.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.gameObject.activeSelf).ToArray();

        static bool HasChoice(InvestigationSession s, string label) => Buttons(s).Any(b => b.name == label);

        static void Click(InvestigationSession s, string label)
        {
            var button = Buttons(s).SingleOrDefault(b => b.name == label);
            Require(button != null && button.interactable, "Missing or disabled button: " + label);
            button.onClick.Invoke();
            AcceptInvestigationCost(s);
        }

        static void AcceptInvestigationCost(InvestigationSession s)
        {
            var confirm = Buttons(s).SingleOrDefault(b => b.name == "조사 시작 · 1회 사용");
            if (confirm != null) confirm.onClick.Invoke();
        }

        static void CheckLayout(InvestigationSession s)
        {
            if (!s.view.modal.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            var v = s.view;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(v.body.rectTransform);
            Require(v.body.rectTransform.rect.height > 0, "Body has no height.");
            Require(v.body.preferredHeight <= v.body.rectTransform.rect.height + 3,
                "Body clips: " + v.title.text + " (" + v.body.preferredHeight + "/" + v.body.rectTransform.rect.height + ")");
            Require(Buttons(s).All(b => b.GetComponent<RectTransform>().rect.height >= 40), "Choices are too short.");
            Require(Buttons(s).All(b =>
            {
                var label = b.GetComponentInChildren<UnityEngine.UI.Text>();
                return label != null && label.preferredHeight <= label.rectTransform.rect.height + 3;
            }), "Choice text clips: " + v.title.text);
        }

        static void CheckRaycasts(FirstPersonInvestigator mover)
        {
            var controller = mover.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            var position = mover.transform.position;
            var rotation = mover.transform.rotation;
            var cameraRotation = mover.viewCamera.transform.localRotation;
            GameObject blocker = null;
            controller.enabled = false;
            try
            {
                var targets = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsSortMode.None).Where(t => (int)t.kind <= (int)InvestigationTarget.TargetKind.MaintenanceOrder).ToArray();
                Require(targets.Length == 6 && targets.Select(t => t.kind).Distinct().Count() == 6,
                    "Exactly six distinct investigation targets expected.");
                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.transform.localScale = new Vector3(1.5f, 1.5f, .2f);
                blocker.SetActive(false);
                foreach (var target in targets)
                {
                    Vector3 point = target.kind switch
                    {
                        InvestigationTarget.TargetKind.MissingDocument => target.transform.position + Vector3.up * .06f,
                        InvestigationTarget.TargetKind.MaintenanceOrder => target.transform.position + Vector3.up * 1.04f,
                        _ => target.transform.position + Vector3.up * 1.3f
                    };
                    Vector3 approach = target.kind == InvestigationTarget.TargetKind.Duseok ? Vector3.forward : Vector3.back;
                    mover.transform.position = new Vector3(point.x, .08f, point.z) + approach * 2.1f;
                    mover.viewCamera.transform.LookAt(point);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == target, "Unreachable target: " + target.kind);

                    blocker.transform.position = Vector3.Lerp(mover.viewCamera.transform.position, point, .5f);
                    blocker.transform.rotation = Quaternion.LookRotation(point - mover.viewCamera.transform.position);
                    blocker.SetActive(true);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == null, "Wall must block interaction: " + target.kind);
                    blocker.SetActive(false);

                    mover.transform.position += approach * 3;
                    mover.viewCamera.transform.LookAt(point);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() != target, "Interaction range exceeded: " + target.kind);
                }
            }
            finally
            {
                if (blocker != null) UnityEngine.Object.DestroyImmediate(blocker);
                mover.transform.SetPositionAndRotation(position, rotation);
                mover.viewCamera.transform.localRotation = cameraRotation;
                controller.enabled = wasEnabled;
                Physics.SyncTransforms();
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

