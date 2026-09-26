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

        [MenuItem("적벽추리/검증 화면/수군 진영")]
        public static void PreviewNavalCamp()
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            var s = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            s.EnterNavalCamp();
            s.ShowStageSelection();
        }

        [MenuItem("적벽추리/검증 화면/두석 대화")]
        public static void PreviewDuseok()
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            var s = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            s.ReturnToArchive();
            s.TalkToDuseok();
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        static UnityEngine.UI.Button[] Buttons(InvestigationSession s) =>
            s.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b => b.gameObject.activeSelf).ToArray();
        static void Click(InvestigationSession s, string label)
        {
            var button = Buttons(s).Single(b => b.name == label);
            Require(button.interactable, "Disabled button: " + label);
            button.onClick.Invoke();
        }
        static void CheckLayout(InvestigationSession s)
        {
            Canvas.ForceUpdateCanvases();
            var v = s.view;
            Require(v.body.rectTransform.rect.height > 0, "Body has no height.");
            Require(v.body.preferredHeight <= v.body.rectTransform.rect.height + 3,
                "Body clips: " + v.title.text + " (" + v.body.preferredHeight + "/" + v.body.rectTransform.rect.height + ")");
            Require(Buttons(s).All(b => b.GetComponent<RectTransform>().rect.height >= 40), "Choices are too short.");
        }
        static void Run(bool documentFirst)
        {
            Require(Application.isPlaying, "Start a fresh Play session first.");
            var s = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            Require(s != null && s.route != null && s.route.IsReady, "Investigation route is not connected.");
            var mover = s.route.player;
            Require(!s.DocumentInspected && !s.TestimonyHeard && s.BlocksMovement, "Use a fresh Play session.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Exactly one EventSystem required.");
            Require(s.view.font.HasCharacter('설') && s.view.font.HasCharacter('석'), "Korean font unavailable.");
            string introTitle = s.view.title.text;
            s.ShowStageSelection();
            Require(s.view.title.text == introTitle, "Selection interrupted the introduction.");
            s.BeginIntroduction("동선 검증 참모");
            for (int i = 0; i < 4; i++) s.AdvanceDialogue();
            Click(s, "명을 받고 문서고로 향한다");
            Require(!s.BlocksMovement, "Introduction did not release movement.");
            Vector3 archivePosition = mover.transform.position;
            s.EnterNavalCamp();
            Require(!s.route.AtNavalCamp && mover.transform.position == archivePosition, "Incomplete investigation allowed travel.");
            s.ShowJournal();
            Require(!Buttons(s).Any(b => b.name == "다음 조사 선택"), "Premature selection button.");
            s.CloseModal();
            if (documentFirst) { s.InspectDocument(); s.CloseModal(); }
            s.TalkToSeolpyeong();
            CheckLayout(s);
            Click(s, "형주에서 무슨 일을 했는가?");
            s.CloseModal();
            Require(!s.TestimonyHeard, "Cancelled testimony was awarded.");
            s.TalkToSeolpyeong();
            Click(s, "형주에서 무슨 일을 했는가?");
            s.AdvanceDialogue();
            Require(s.TestimonyHeard, "Completed testimony missing.");
            Require(s.view.title.text == "형주에서의 경력", "Completion replaced the statement result.");
            s.CloseModal();
            if (!documentFirst) s.InspectDocument();
            Require(s.FirstInvestigationComplete && !s.DuseokTestimonyHeard, "Duseok must remain optional.");
            s.ShowJournal();
            CheckLayout(s);
            Click(s, "다음 조사 선택");
            CheckLayout(s);
            Click(s, "수군 진영으로 이동");
            Require(s.route.AtNavalCamp && !s.BlocksMovement, "Naval camp travel failed.");
            Require(Vector3.Distance(mover.transform.position, s.route.navalArrival.position) < .1f, "Wrong naval arrival.");
            Require(mover.viewCamera.transform.localRotation == Quaternion.identity, "Travel did not reset view.");
            TalkOfficer(s, documentFirst, true);
            TalkOfficer(s, !documentFirst, false);
            Require(s.CaimaoTestimonyHeard && s.ZhangyunTestimonyHeard, "Officer order failed.");
            string name = s.PlayerName;
            for (int i = 0; i < 3; i++)
            {
                s.ShowStageSelection();
                Click(s, "문서고로 돌아가기");
                Require(!s.route.AtNavalCamp && !s.BlocksMovement, "Archive return failed.");
                Require(!s.CanProcessInvestigationInput, "Closing input must not be consumed twice.");
                Require(Vector3.Distance(mover.transform.position, s.route.archiveArrival.position) < .1f, "Wrong archive arrival.");
                s.ShowStageSelection();
                Click(s, "수군 진영으로 이동");
                Require(s.PlayerName == name && s.FirstInvestigationComplete && s.CaimaoTestimonyHeard && s.ZhangyunTestimonyHeard, "Travel lost progress.");
            }
            s.ReturnToArchive();
            s.TalkToDuseok();
            Click(s, "근무 중 자리를 비운 적이 있는가?");
            s.CloseModal();
            Require(!s.DuseokTestimonyHeard, "Cancelled guard testimony was awarded.");
            s.TalkToDuseok();
            Click(s, "근무 중 자리를 비운 적이 있는가?");
            s.AdvanceDialogue();
            Require(s.DuseokTestimonyHeard, "Guard testimony missing.");
            CheckLayout(s);
            s.CloseModal();
            s.InspectDocument();
            s.InspectDocument();
            s.ShowJournal();
            var pages = new string[5];
            for (int i = 0; i < pages.Length; i++)
            {
                CheckLayout(s);
                pages[i] = s.view.body.text;
                Click(s, "다음 기록");
            }
            Require(pages[0].Contains("확인된 사실") && pages[0].Contains("확인할 일"), "Journal lost fact/question separation.");
            string[] statements = { InvestigationSession.Testimony, InvestigationSession.DuseokTestimony, InvestigationSession.CaimaoTestimony, InvestigationSession.ZhangyunTestimony };
            for (int i = 0; i < statements.Length; i++)
                Require(pages[i + 1].Contains(statements[i]) && pages[i + 1].Contains("미검증"), "Statement missing or labelled as fact.");
            CheckRaycasts(mover);
            s.EnterNavalCamp();
            s.ShowJournal();
            Debug.Log("INVESTIGATION_ROUTE_CHECKS_PASSED: " + (documentFirst ? "document-first" : "testimony-first") + "; optional guard, cancellation, officers, return travel, records, layout and raycasts.");
        }
        static void TalkOfficer(InvestigationSession s, bool caimao, bool cancelFirst)
        {
            Action talk = caimao ? s.TalkToCaimao : s.TalkToZhangyun;
            talk();
            Click(s, "당시 수군 훈련과 기록에 관해");
            if (cancelFirst)
            {
                s.AdvanceDialogue();
                s.CloseModal();
                Require(!(caimao ? s.CaimaoTestimonyHeard : s.ZhangyunTestimonyHeard), "Incomplete officer dialogue was awarded.");
                talk();
                Click(s, "당시 수군 훈련과 기록에 관해");
            }
            s.AdvanceDialogue();
            s.AdvanceDialogue();
            CheckLayout(s);
            s.CloseModal();
        }
        static void CheckRaycasts(FirstPersonInvestigator mover)
        {
            var cc = mover.GetComponent<CharacterController>();
            var position = mover.transform.position;
            var rotation = mover.transform.rotation;
            var cameraRotation = mover.viewCamera.transform.localRotation;
            GameObject blocker = null;
            cc.enabled = false;
            try
            {
                var targets = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsSortMode.None);
                Require(targets.Length == 5, "Exactly five investigation targets expected.");
                foreach (var target in targets)
                {
                    Vector3 point = target.kind == InvestigationTarget.TargetKind.MissingDocument
                        ? new Vector3(0, 1.12f, 2.4f) : target.transform.position + Vector3.up * 1.3f;
                    Vector3 approach = target.kind == InvestigationTarget.TargetKind.Duseok ? Vector3.forward : Vector3.back;
                    mover.transform.position = new Vector3(point.x, .08f, point.z) + approach * 2.1f;
                    mover.viewCamera.transform.LookAt(point);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == target, "Unreachable target: " + target.kind);
                    mover.transform.position += approach * 3;
                    mover.viewCamera.transform.LookAt(point);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == null, "Interaction range exceeded: " + target.kind);
                }
                var guard = targets.Single(t => t.kind == InvestigationTarget.TargetKind.Duseok);
                mover.transform.position = guard.transform.position + new Vector3(0, .08f, 2.1f);
                mover.viewCamera.transform.LookAt(guard.transform.position + Vector3.up * 1.3f);
                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.transform.position = guard.transform.position + new Vector3(0, 1.3f, 1);
                blocker.transform.localScale = new Vector3(2, 2, .2f);
                Physics.SyncTransforms();
                Require(mover.FindTarget() == null, "Wall must block interaction.");
            }
            finally
            {
                if (blocker != null) UnityEngine.Object.DestroyImmediate(blocker);
                mover.transform.SetPositionAndRotation(position, rotation);
                mover.viewCamera.transform.localRotation = cameraRotation;
                cc.enabled = true;
                Physics.SyncTransforms();
            }
        }
    }
}
