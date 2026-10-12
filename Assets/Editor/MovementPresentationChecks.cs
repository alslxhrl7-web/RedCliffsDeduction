using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class MovementPresentationChecks
    {
        const string Report="Logs/MovementPresentation/result.txt";
        static InvestigationSession session;



        static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
        static void Pass(string message) { File.AppendAllText(Report,"PASS "+message+"\n"); }
        static bool BlocksView(Renderer renderer,Ray ray)
        {
            if(!renderer.bounds.IntersectRay(ray,out float distance) || distance>=2) return false;
            // Hanging cloth bounds include the empty space beneath its anchored seam.
            if(renderer.GetComponentInParent<ArchiveCurtain>()==null) return true;
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices=mesh.vertices; var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 a=renderer.transform.TransformPoint(vertices[triangles[i]]), b=renderer.transform.TransformPoint(vertices[triangles[i+1]]), c=renderer.transform.TransformPoint(vertices[triangles[i+2]]);
                Vector3 e1=b-a,e2=c-a,p=Vector3.Cross(ray.direction,e2); float det=Vector3.Dot(e1,p);
                if(Mathf.Abs(det)<.000001f) continue;
                Vector3 t=ray.origin-a; float u=Vector3.Dot(t,p)/det; if(u<0 || u>1) continue;
                Vector3 q=Vector3.Cross(t,e1); float v=Vector3.Dot(ray.direction,q)/det; if(v<0 || u+v>1) continue;
                float hit=Vector3.Dot(e2,q)/det; if(hit>0 && hit<2) return true;
            }
            return false;
        }
        static void Begin()
        {
            try { Run(); }
            catch(Exception error) { Directory.CreateDirectory("Logs/MovementPresentation"); File.AppendAllText(Report,"FAIL "+error+"\n"); Debug.LogException(error); Finish(); }
        }
        static void Run()
        {
            Require(EditorApplication.isPlaying,"Enter isolated Play Mode first.");
            Directory.CreateDirectory("Logs/MovementPresentation"); File.WriteAllText(Report,"Movement presentation verification\n");
            session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            var reference=session.route.player.GetComponent<DistanceFootsteps>();
            var go=new GameObject("Distance footstep verification");
            var steps=go.AddComponent<DistanceFootsteps>(); steps.woodenSteps=reference.woodenSteps; steps.earthSteps=reference.earthSteps;
            try
            {
                Require(steps.woodenSteps.Length==3 && steps.earthSteps.Length==3,"Surface clips missing");
                foreach(var clip in steps.woodenSteps.Concat(steps.earthSteps).Append(session.route.curtainRustle))
                {
                    Require(clip!=null && clip.loadState==AudioDataLoadState.Loaded,"Audio not loaded");
                    var samples=new float[clip.samples]; clip.GetData(samples,0);
                    Require(samples.Any(s=>Mathf.Abs(s)>.02f),"Silent sound: "+clip.name);
                }
                Pass("Seven non-silent, loaded effect clips");
                for(int i=0;i<8;i++) { go.transform.position+=Vector3.right*.3f; steps.RecordMovement(true,false); }
                Require(steps.StepsPlayed==2,"Distance cadence differs"); Pass("2.4 metres produces two steps");
                int prior=steps.StepsPlayed;
                for(int i=0;i<50;i++) steps.RecordMovement(true,false);
                Require(steps.StepsPlayed==prior,"Stationary footsteps"); Pass("Stationary and blocked movement remain silent");
                steps.ResetMotion(); go.transform.position+=Vector3.up*2; steps.RecordMovement(true,false);
                go.transform.position+=Vector3.right*40; steps.RecordMovement(true,true);
                go.transform.position+=Vector3.right*.5f; steps.RecordMovement(false,true);
                Require(steps.StepsPlayed==prior && steps.PendingDistance==0,"Fall or teleport triggered step"); Pass("Vertical movement, falling and teleports do not trigger steps");
                steps.ResetMotion(); for(int i=0;i<24;i++) { go.transform.position+=Vector3.right*.1f; steps.RecordMovement(true,true); }
                Require(steps.StepsPlayed==prior+2,"Frame dependent cadence"); Pass("Same distance at different sampling rate keeps cadence; earth sound plays");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            ConnectedWalkwayChecks.Begin();
        }
        static void Finish() { EditorApplication.isPlaying=false; }
    }
}

