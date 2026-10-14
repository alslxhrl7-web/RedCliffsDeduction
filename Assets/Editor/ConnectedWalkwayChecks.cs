using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class ConnectedWalkwayChecks
{
    static InvestigationSession session;
    static double started,last;
    static int phase,steps;
    static Vector3 start;
    static bool captured;
    const string Report="Logs/ConnectedWalkway/result.txt";
    static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    static void Pass(string message) { File.AppendAllText(Report,"PASS "+message+"\n"); }
    [MenuItem("Tools/Model Quality/Check Connected Walkway")]
    public static void Begin()
    {
        Require(EditorApplication.isPlaying,"Play Mode required");
        Directory.CreateDirectory("Logs/ConnectedWalkway"); File.WriteAllText(Report,"Connected walkway play verification\n");
        session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        typeof(InvestigationSession).GetField("introductionFinished",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,true);
        typeof(InvestigationSession).GetField("enteringName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,false);
        session.introductionStage.EnterArchive(); session.route.EnterArchive(); session.view.SetPrologueLayout(false); session.CloseModal();
        var spawn=new GameObject("Walkway test start"); spawn.transform.SetPositionAndRotation(new Vector3(0,.08f,-4.4f),Quaternion.Euler(0,180,0));
        session.route.player.TeleportTo(spawn.transform); UnityEngine.Object.DestroyImmediate(spawn);
        start=session.route.player.transform.position; steps=session.route.player.GetComponent<DistanceFootsteps>().StepsPlayed;
        session.EnterNavalCamp(); phase=0; captured=false; started=last=EditorApplication.timeSinceStartup; EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        try {
            double now=EditorApplication.timeSinceStartup; float dt=Mathf.Clamp((float)(now-last),0,.05f); last=now;
            Require(now-started<30,"Walkway phase stuck: "+phase);
            var route=session.route; var player=route.player; var controller=player.GetComponent<CharacterController>();
            if(phase==0) {
                if(route.IsTravelling) return;
                Require(!route.AtNavalCamp && Vector3.Distance(start,player.transform.position)<.03f,"Opening auto-moved player");
                Require(route.curtain.Opening>.99f && controller.enabled && !session.BlocksMovement,"Opening did not restore control");
                Require(session.view.transform.Find("CurtainTravelFade")==null,"Unexpected blackout");
                Pass("Short curtain opening leaves player in place with controls restored and no blackout");
                player.GetComponent<DistanceFootsteps>().ExternalMotion=true;
                phase=1; started=now; return;
            }
            if(phase==1) {
                controller.Move(new Vector3(0,-2,-3.1f)*dt); player.GetComponent<DistanceFootsteps>().RecordMovement(controller.isGrounded,route.OnOutdoorGround);
                if(player.transform.position.z<-9 && !captured) { ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/ConnectedWalkway/path-to-camp.png")); captured=true; }
                if(player.transform.position.z>-18.3f) return;
                Require(route.AtNavalCamp && Mathf.Abs(player.transform.position.y)<.3f,"Camp arrival or ground failed");
                Require(player.GetComponent<DistanceFootsteps>().StepsPlayed>steps+7,"Outdoor walking did not play steps");
                Pass("CharacterController walks from archive through the open gate into camp without teleport or obstruction");
                Pass("Outdoor ground and distance footsteps remain active");
                var before=player.transform.position; session.ReturnToArchive(); Require(Vector3.Distance(before,player.transform.position)<.01f,"Return request teleported player");
                phase=2; started=now; return;
            }
            controller.Move(new Vector3(0,-2,3.1f)*dt); player.GetComponent<DistanceFootsteps>().RecordMovement(controller.isGrounded,route.OnOutdoorGround);
            if(player.transform.position.z<-4.35f) return;
            Require(!route.AtNavalCamp && !route.OnOutdoorGround && controller.enabled,"Archive return state failed");
            Pass("Same physical path supports return to archive and updates location");
            File.AppendAllText(Report,"COMPLETE failures=0\n"); Finish();
        } catch(Exception error) { File.AppendAllText(Report,"FAIL "+error+"\n"); Debug.LogException(error); Finish(); }
    }
    static void Finish() { EditorApplication.update-=Tick; if(session!=null) session.route.player.GetComponent<DistanceFootsteps>().ExternalMotion=false; EditorApplication.isPlaying=false; }
}
