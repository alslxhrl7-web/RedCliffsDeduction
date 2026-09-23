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
        public static void Run()
        {
            Require(Application.isPlaying, "Enter Play Mode first.");
            var session=UnityEngine.Object.FindFirstObjectByType<InvestigationSession>();
            var mover=UnityEngine.Object.FindFirstObjectByType<FirstPersonInvestigator>();
            Require(session!=null && mover!=null, "Scene wiring is missing.");
            Require(!session.DocumentInspected && !session.TestimonyHeard,"Start with a fresh Play session.");
            Require(session.BlocksMovement,"Intro must block movement.");
            Require(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Need exactly one EventSystem.");
            Require(session.view.font.HasCharacter('설') && session.view.font.HasCharacter('평'),"Korean font unavailable.");
            session.BeginIntroduction("검증용 참모");
            for (int i=0;i<4;i++) session.AdvanceDialogue();
            ActiveButtons(session)[0].onClick.Invoke();
            Require(!session.BlocksMovement,"Closing intro must allow movement.");

            // Talk before inspecting: cancel the statement once, then complete it.
            session.TalkToSeolpyeong();
            Require(session.BlocksMovement,"Conversation must stop movement.");
            ActiveButtons(session)[1].onClick.Invoke();
            session.CloseModal();
            Require(!session.TestimonyHeard,"Aborted statement must not be awarded.");
            session.TalkToSeolpyeong();
            ActiveButtons(session)[1].onClick.Invoke();
            session.AdvanceDialogue();
            Require(session.TestimonyHeard && !session.DocumentInspected,"Testimony-first flow failed.");
            Require(!session.FirstInvestigationComplete,"Do not complete before inspection.");
            session.CloseModal();
            session.InspectDocument();
            session.InspectDocument();
            Require(session.DocumentInspected && session.FirstInvestigationComplete,"Inspect + statement must complete first investigation.");
            session.ShowJournal();
            Require(session.view.body.text.Contains(InvestigationSession.Testimony),"Journal must retain the full statement.");
            Require(session.view.body.text.Contains("미검증"),"Statement must be labelled unverified.");

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
            Debug.Log("FIRST_INVESTIGATION_CHECKS_PASSED: intro, conversation lock, cancellation, testimony-first order, repeat inspection, journal, Korean font, raycast range and occlusion.");
        }

        static UnityEngine.UI.Button[] ActiveButtons(InvestigationSession s) =>
            s.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeSelf).ToArray();
    }
}
