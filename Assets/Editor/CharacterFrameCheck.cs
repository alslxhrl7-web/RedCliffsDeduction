using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;
using Object=UnityEngine.Object;

namespace RedCliffMystery.EditorTools
{
    // Editor Play observation, not a standalone-player FPS certification.
    public static class CharacterFrameCheck
    {
        static readonly int[] Profiles={0,4,9};
        static readonly List<float> Samples=new List<float>();
        static InvestigationSession session;
        static int index,lastFrame;
        static double start;
        static string previous;
        const string Log="Logs/CharacterPolish/editor-frames.txt";
        static void Prepare(){
            if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
            previous=SessionState.GetString("RedCliffMystery.TestSaveDirectory","");
            SessionState.SetString("CharacterFrameCheck.Previous",previous);
            SessionState.SetString("RedCliffMystery.TestSaveDirectory",Path.GetFullPath("Temp/OrderChecks/Frames-"+DateTime.Now.ToString("HHmmss")));
            EditorApplication.isPlaying=true;
        }
        static void Begin(){
            if(!Application.isPlaying||!InvestigationSaveStore.SavePath.Contains("OrderChecks/Frames-".Replace('/',Path.DirectorySeparatorChar)))throw new Exception("Isolated Play required");
            Directory.CreateDirectory("Logs/CharacterPolish");
            File.WriteAllText(Log,$"Editor Play only; {Screen.width}x{Screen.height}; target={Application.targetFrameRate}; vsync={QualitySettings.vSyncCount}\n");
            Object.FindAnyObjectByType<InvestigationTitleScreen>().StartButton.onClick.Invoke();
            session=Object.FindAnyObjectByType<InvestigationSession>();session.enabled=false;
            index=0;Position();EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static void Position(){
            var actor=Object.FindObjectsByType<VarcoIdleMotion>().Single(a=>a.Profile==Profiles[index]);
            var anchor=new GameObject("FrameCheckArrival");anchor.transform.position=actor.transform.position-actor.transform.forward*2.1f;
            anchor.transform.rotation=Quaternion.LookRotation(actor.transform.forward);session.introductionStage.player.TeleportTo(anchor.transform);Object.Destroy(anchor);
            var speaker=RecordArchive.SpeakerNames[Profiles[index]];
            session.view.ShowModal("프레임 확인",speaker,"인물 보정 후 대화 화면을 확인하고 있습니다.","자동 검사");VarcoIdleMotion.Speak(speaker,"대화 중의 인물과 화면을 확인하고 있습니다.");
            Samples.Clear();start=EditorApplication.timeSinceStartup;lastFrame=Time.frameCount;
        }
        static void Tick(){
            if(!Application.isPlaying){Finish();return;}
            double elapsed=EditorApplication.timeSinceStartup-start;
            if(Time.frameCount!=lastFrame){lastFrame=Time.frameCount;if(elapsed>=2)Samples.Add(Time.unscaledDeltaTime*1000);}
            if(elapsed<7)return;
            if(Samples.Count>0){var sorted=Samples.OrderBy(x=>x).ToArray();File.AppendAllText(Log,$"profile={Profiles[index]} frames={Samples.Count} meanFPS={1000/Samples.Average():F1} p95ms={sorted[(int)((sorted.Length-1)*.95)]:F2} maxMs={sorted.Last():F2} over50ms={Samples.Count(x=>x>50)}\n");}
            if(++index<Profiles.Length)Position();else{File.AppendAllText(Log,"COMPLETE observation only\n");Finish();}
        }
        static void Finish(){EditorApplication.update-=Tick;VarcoIdleMotion.StopSpeech();EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>{
            previous=SessionState.GetString("CharacterFrameCheck.Previous","");if(string.IsNullOrEmpty(previous))SessionState.EraseString("RedCliffMystery.TestSaveDirectory");else SessionState.SetString("RedCliffMystery.TestSaveDirectory",previous);
        };}
    }
}
