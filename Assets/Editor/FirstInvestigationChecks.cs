using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class FirstInvestigationChecks
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [MenuItem("적벽추리/첫 조사 장면 실행 검사")]
        public static void Run() => RunFlow(true);

        [MenuItem("적벽추리/첫 조사 장면 실행 검사 (문서 먼저)")]
        public static void RunDocumentFirst() => RunFlow(false);

        static void RunFlow(bool testimonyFirst)
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            var session=UnityEngine.Object.FindFirstObjectByType<InvestigationSession>();
            var mover=UnityEngine.Object.FindFirstObjectByType<FirstPersonInvestigator>();
            Require(session!=null && mover!=null, "Scene wiring is missing.");
            Require(!session.DocumentInspected && !session.TestimonyHeard,"Start with a fresh Play session.");
            Require(session.BlocksMovement,"Intro must block movement.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Need exactly one EventSystem.");
            Require(session.view.font.HasCharacter('설') && session.view.font.HasCharacter('평'),"Korean font unavailable.");
            session.ShowStageSelection();
            Require(session.view.title.text=="조조","Stage selection must not interrupt the introduction.");
            session.BeginIntroduction("검증용 참모");
            for (int i=0;i<4;i++) session.AdvanceDialogue();
            ActiveButtons(session)[0].onClick.Invoke();
            Require(!session.BlocksMovement,"Closing intro must allow movement.");

            session.ShowJournal();
            Require(!ActiveButtons(session).Any(b=>b.name=="다음 조사 선택"),"Stage selection must be hidden before investigation.");
            session.ShowStageSelection();
            Require(session.view.title.text=="사실과 진술","Incomplete investigation must not open stage selection.");
            session.CloseModal();
            if (!testimonyFirst) { session.InspectDocument(); session.CloseModal(); }

            // Cancel the statement once, then complete it in either investigation order.
            session.TalkToSeolpyeong();
            Require(session.BlocksMovement,"Conversation must stop movement.");
            ActiveButtons(session)[1].onClick.Invoke();
            session.CloseModal();
            Require(!session.TestimonyHeard,"Aborted statement must not be awarded.");
            session.ShowJournal();
            Require(!ActiveButtons(session).Any(b=>b.name=="다음 조사 선택"),"An aborted statement must not unlock selection.");
            session.CloseModal();
            session.TalkToSeolpyeong();
            ActiveButtons(session)[1].onClick.Invoke();
            session.AdvanceDialogue();
            Require(session.TestimonyHeard && session.DocumentInspected==!testimonyFirst,"Investigation order failed.");
            Require(session.FirstInvestigationComplete==!testimonyFirst,"Completion must require both investigation actions.");
            Require(session.view.title.text=="형주에서의 경력","The statement result must remain visible after completion.");
            session.CloseModal();
            session.InspectDocument();
            session.InspectDocument();
            Require(session.DocumentInspected && session.FirstInvestigationComplete,"Inspect + statement must complete first investigation.");
            Require(session.view.title.text=="사라진 화공대비책","The inspection result must remain visible after completion.");
            session.ShowJournal();
            Require(session.view.body.text.Contains(InvestigationSession.Testimony),"Journal must retain the full statement.");
            Require(session.view.body.text.Contains("미검증"),"Statement must be labelled unverified.");

            string playerName=session.PlayerName;
            Vector3 positionBeforeSelection=mover.transform.position;
            for (int i=0;i<3;i++)
            {
                ActiveButtons(session).Single(b=>b.name=="다음 조사 선택").onClick.Invoke();
                Require(session.view.title.text=="다음 조사 선택" && session.BlocksMovement,"Selection must open and block movement.");
                Require(Cursor.lockState==CursorLockMode.None && Cursor.visible,"Selection must release the cursor.");
                var unavailable=ActiveButtons(session).Single(b=>b.name=="채모·장윤 조사 · 준비 중");
                Require(!unavailable.interactable,"Unimplemented destinations must be disabled.");
                unavailable.onClick.Invoke();
                Require(session.view.title.text=="다음 조사 선택","Unavailable destination must have no transition callback.");
                ActiveButtons(session).Single(b=>b.name=="수첩 다시 보기").onClick.Invoke();
                Require(session.view.body.text.Contains(InvestigationSession.Testimony),"Selection must retain the journal.");
                ActiveButtons(session).Single(b=>b.name=="다음 조사 선택").onClick.Invoke();
                ActiveButtons(session).Single(b=>b.name=="문서고로 돌아가기").onClick.Invoke();
                Require(!session.BlocksMovement && !session.view.modal.activeSelf,"Returning must resume investigation.");
                Require(!session.CanProcessInvestigationInput,"The closing key must not be handled twice in the same frame.");
                Require(session.PlayerName==playerName && session.FirstInvestigationComplete,"Selection must preserve identity and progress.");
                Require(mover.transform.position==positionBeforeSelection,"Selection must not move the player.");
                session.ShowJournal();
            }

            // Raycast is the same path used by E; verify range and occlusion.
            var cc=mover.GetComponent<CharacterController>();
            Vector3 original=mover.transform.position;
            Quaternion originalRotation=mover.transform.rotation;
            Quaternion originalView=mover.viewCamera.transform.localRotation;
            cc.enabled=false;
            mover.transform.position=new Vector3(0,.05f,.3f);
            mover.viewCamera.transform.LookAt(new Vector3(0,1.12f,2.4f));
            Physics.SyncTransforms();
            var target=mover.FindTarget();
            Require(target!=null && target.kind==InvestigationTarget.TargetKind.MissingDocument,"Document must be reachable from desk front.");
            mover.transform.position=new Vector3(0,.05f,-4.5f);
            mover.viewCamera.transform.LookAt(new Vector3(0,1.12f,2.4f));
            Require(mover.FindTarget()==null,"Distant objects must not be interactive.");
            mover.transform.position=new Vector3(2.8f,.05f,.7f);
            mover.viewCamera.transform.LookAt(new Vector3(2.8f,1.3f,2.7f));
            Physics.SyncTransforms();
            target=mover.FindTarget();
            Require(target!=null && target.kind==InvestigationTarget.TargetKind.Seolpyeong,"NPC must be reachable.");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position=new Vector3(2.8f,1.3f,1.7f); blocker.transform.localScale=new Vector3(2,2,.2f);
            Physics.SyncTransforms();
            Require(mover.FindTarget()==null,"Solid obstacles must block interaction.");
            UnityEngine.Object.DestroyImmediate(blocker);
            mover.transform.position=original; mover.transform.rotation=originalRotation;
            mover.viewCamera.transform.localRotation=originalView; cc.enabled=true;
            session.ShowJournal();
            Debug.Log("FIRST_INVESTIGATION_CHECKS_PASSED: order="+(testimonyFirst ? "testimony-first" : "document-first")+", intro, cancellation, repeated selection and return, disabled destination, progress retention, journal, Korean font, raycast range and occlusion.");
        }

        static UnityEngine.UI.Button[] ActiveButtons(InvestigationSession s) =>
            s.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeSelf).ToArray();
    }
}
