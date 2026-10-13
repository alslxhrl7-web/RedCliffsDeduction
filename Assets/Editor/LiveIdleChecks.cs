using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class LiveIdleChecks
    {
        const string Folder="Logs/LiveIdle";
        [MenuItem("Tools/Character Motion/Capture Refined Hands")]
        public static void CaptureHands()
        {
            if(!Application.isPlaying||!SessionState.GetString("RedCliffMystery.TestSaveDirectory","").Contains("OrderChecks"))throw new Exception("Isolated Play required");
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();session.StartCoroutine(ReviewHands(session));
        }
        static IEnumerator ReviewHands(InvestigationSession session)
        {
            var title=UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();if(title)title.StartButton.onClick.Invoke();
            yield return new WaitForSeconds(1.5f);
            session.CloseModal();session.enabled=false;session.route.player.enabled=false;
            var camera=session.route.player.viewCamera;int failures=0;Directory.CreateDirectory(Folder);
            foreach(var actor in UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include).OrderBy(a=>a.Profile))
            {
                for(var t=actor.transform;t;t=t.parent)t.gameObject.SetActive(true);
                camera.transform.position=actor.transform.position-actor.transform.forward*.95f+Vector3.up*1.08f;
                camera.transform.LookAt(actor.transform.position+Vector3.up*1.02f);
                actor.StartSpeech("확인한 기록을 설명하겠습니다.",Time.time);
                yield return new WaitForSeconds(1);
                camera.transform.position=actor.transform.position-actor.transform.forward*.95f+Vector3.up*1.08f;
                camera.transform.LookAt(actor.transform.position+Vector3.up*1.02f);
                Capture(camera,actor.Profile+"-hands-final");
                var vertices=actor.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;
                foreach(var hand in actor.GetComponentsInChildren<RefinedHand>())if(hand.BoundaryError(vertices)>.001f)failures++;
                actor.StopTalking(Time.time);
            }
            File.WriteAllText(Folder+"/hand-review.txt","Dialogue capture and wrist boundary check: COMPLETE failures="+failures+"\n");
        }
        [MenuItem("Tools/Character Motion/Check Dialogue And Duseok")]
        public static void CheckDialogueAndDuseok()
        {
            if(!Application.isPlaying || !SessionState.GetString("RedCliffMystery.TestSaveDirectory","").Contains("OrderChecks"))throw new Exception("Isolated Play mode required");
            VarcoRolePoses.VerifySpeakerRouting();
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            session.CloseModal();session.enabled=false;
            var actor=UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include).Single(a=>a.Profile==2);
            for(var t=actor.transform;t;t=t.parent)t.gameObject.SetActive(true);
            var player=session.route.player;player.enabled=false;
            var anchor=new GameObject("ActivityCheckArrival");
            anchor.transform.position=actor.transform.position-actor.transform.forward*1.6f;
            anchor.transform.rotation=Quaternion.LookRotation(actor.transform.forward);
            player.TeleportTo(anchor.transform);UnityEngine.Object.Destroy(anchor);
            var camera=player.viewCamera;
            camera.transform.position=anchor.transform.position+Vector3.up*1.5f;
            camera.transform.LookAt(actor.transform.position+Vector3.up*1.1f);
            camera.useOcclusionCulling=false;
            session.StartCoroutine(CaptureDuseok(camera));
        }
        static IEnumerator CaptureDuseok(Camera camera)
        {
            yield return new WaitForSeconds(1);
            Capture(camera,"2-front");
        }
        [MenuItem("Tools/Character Motion/Run Live Idle Checks")]
        public static void Run()
        {
            if(!Application.isPlaying || !SessionState.GetString("RedCliffMystery.TestSaveDirectory","").Contains("OrderChecks"))throw new Exception("Isolated Play mode required");
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            session.StartCoroutine(Check(session));
        }
        static IEnumerator Check(InvestigationSession session)
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/checks.txt","Real Play LateUpdate sampling; camera at 2.4m; no direct Sample calls.\n");
            var title=UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
            if(title)title.StartButton.onClick.Invoke();
            session.enabled=false;session.CloseModal();VarcoIdleMotion.StopSpeech();
            var player=session.route.player;player.enabled=false;
            var camera=player.viewCamera;camera.enabled=true;
            var actors=UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include).OrderBy(a=>a.Profile).ToArray();
            int failures=0;
            if(actors.Length!=10 || actors.Select(a=>a.Profile).Distinct().Count()!=10) { File.AppendAllText(Folder+"/checks.txt","FAIL actor count\nCOMPLETE failures=1\n");yield break; }
            foreach(var actor in actors)
            {
                for(var t=actor.transform;t;t=t.parent)t.gameObject.SetActive(true);
                var filter=actor.GetComponentInChildren<MeshFilter>();
                var b=actor.GetComponentInChildren<Renderer>().bounds;
                camera.transform.position=actor.transform.position-actor.transform.forward*2.4f+Vector3.up*1.5f;
                camera.transform.LookAt(b.center+Vector3.up*.25f);
                yield return new WaitForSeconds(.2f);
                var start=filter.sharedMesh.vertices;
                var origin=start.Select(v=>filter.transform.TransformPoint(v)).ToArray();
                float floor=origin.Min(v=>v.y),height=origin.Max(v=>v.y)-floor;
                float maxMove=0,feet=0,handGap=0;var hands=actor.GetComponentsInChildren<RefinedHand>();
                var bakedHand=new Mesh();var fingerStart=new Vector3[hands.Length][];var fingerMove=new float[hands.Length];
                for(int h=0;h<hands.Length;h++){hands[h].GetComponent<SkinnedMeshRenderer>().BakeMesh(bakedHand);fingerStart[h]=bakedHand.vertices;}
                float nextFingerSample=Time.time;
                int changedFrames=0;float until=Time.time+(actor.RoleActivity != null ? 14 : 4);
                Capture(camera,actor.Profile+"-before");
                while(Time.time<until)
                {
                    yield return null;
                    var vertices=filter.sharedMesh.vertices;bool moved=false;
                    foreach(var hand in hands)
                        handGap=Mathf.Max(handGap,hand.BoundaryError(vertices),Vector3.Distance(hand.transform.position,filter.transform.TransformPoint(vertices[hand.A])));
                    for(int i=0;i<vertices.Length;i+=3)
                    {
                        float distance=Vector3.Distance(origin[i],filter.transform.TransformPoint(vertices[i]));
                        maxMove=Mathf.Max(maxMove,distance);
                        if(origin[i].y<floor+height*.25f)feet=Mathf.Max(feet,distance);
                        if((vertices[i]-start[i]).sqrMagnitude>1e-14f)moved=true;
                    }
                    if(moved)changedFrames++;
                    if(Time.time>=nextFingerSample)
                    {
                        nextFingerSample=Time.time+.7f;
                        for(int h=0;h<hands.Length;h++)
                        {
                            hands[h].GetComponent<SkinnedMeshRenderer>().BakeMesh(bakedHand);var points=bakedHand.vertices;
                            for(int i=0;i<points.Length;i++)if(hands[h].BoundaryBindings[i]<0)
                                fingerMove[h]=Mathf.Max(fingerMove[h],Vector3.Distance(points[i],fingerStart[h][i]));
                        }
                    }
                }
                UnityEngine.Object.Destroy(bakedHand);
                Capture(camera,actor.Profile+"-after");
                bool pass=maxMove>=.006f && maxMove<.12f && feet<.001f && changedFrames>10 && hands.Length==2 && handGap<.001f && fingerMove.All(v=>v>.00001f&&v<.02f);
                if(!pass)failures++;
                File.AppendAllText(Folder+"/checks.txt",$"{(pass?"PASS":"FAIL")} {actor.transform.parent.name} motion={maxMove*1000:F1}mm feet={feet*1000:F3}mm hands={hands.Length} wristGap={handGap*1000:F3}mm fingers={string.Join("/",fingerMove.Select(v=>(v*1000).ToString("F3")))}mm animatedFrames={changedFrames}\n");
                camera.transform.position=actor.transform.position-actor.transform.forward*.95f+Vector3.up*1.08f;
                camera.transform.LookAt(actor.transform.position+Vector3.up*1.02f);
                yield return new WaitForSeconds(.15f);Capture(camera,actor.Profile+"-hands");
            }
            File.AppendAllText(Folder+"/checks.txt","COMPLETE failures="+failures+"\n");
        }
        static void Capture(Camera camera,string name)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(960,720,24);var texture=new Texture2D(960,720,TextureFormat.RGB24,false);
            try { camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,720),0,0);texture.Apply();File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG()); }
            finally {camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}

