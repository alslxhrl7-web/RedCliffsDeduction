using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;
using Object=UnityEngine.Object;
namespace RedCliffMystery.EditorTools
{
    public static class FeedbackArchiveChecks
    {
        static string folder="Logs/FeedbackArchive";static int step;static double next;static InvestigationSession session;static RecordArchiveScreen screen;static Camera camera;static float fov;static Quaternion rotation;static string oldDirectory;static bool captured;
        static void Require(bool yes,string msg){if(!yes)throw new Exception(msg);File.AppendAllText(folder+"/checks.txt","PASS "+msg+"\n");}
        static void Prepare(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");oldDirectory=SessionState.GetString("RedCliffMystery.TestSaveDirectory","");SessionState.SetString("FeedbackArchive.PreviousDirectory",oldDirectory);SessionState.SetString("RedCliffMystery.TestSaveDirectory",Path.GetFullPath("Temp/OrderChecks/Archive-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")));EditorApplication.isPlaying=true;}
        static void Begin(){if(!EditorApplication.isPlaying)throw new Exception("Play required");Directory.CreateDirectory(folder);File.WriteAllText(folder+"/checks.txt","");step=0;next=0;captured=false;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder+"/"+name+".png"));
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
            try{
                if(step>=1&&step<=6&&!captured){string[] shots={"archive-locked","archive-ending","archive-person","archive-evidence","archive-bonus","conversation-zhangyun"};Capture(shots[step-1]);captured=true;return;}
                if(step==0){
                    Require(InvestigationSaveStore.SavePath.Contains("OrderChecks"),"isolated save and archive paths");
                    session=Object.FindAnyObjectByType<InvestigationSession>();session.enabled=false;
                    var title=Object.FindAnyObjectByType<InvestigationTitleScreen>();Require(title&&title.ArchiveButton,"title archive entry");
                    Require(RecordArchive.Entries.All(e=>!RecordArchive.Has(e.id)),"fresh archive reveals no entries");
                    title.ArchiveButton.onClick.Invoke();screen=Object.FindAnyObjectByType<RecordArchiveScreen>();Require(screen&&RecordArchiveScreen.IsOpen,"title opens archive");screen.OpenEntry("ending-True");
                    Require(!screen.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("한진의 제출")),"locked ending does not leak result");
                    screen.ShowIllustration("ending-True");Require(!screen.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(),"locked illustration does not load or reveal art");
                }else if(step==1){screen.Close();Require(!RecordArchiveScreen.IsOpen,"archive returns to title");Object.FindAnyObjectByType<InvestigationTitleScreen>().StartButton.onClick.Invoke();
                    session.OrderState.facts=1048575;session.OrderState.followUp=131071;session.OrderState.Validate();RecordArchive.Capture(session);
                    for(int i=0;i<10;i++)RecordArchive.RecordPerson(i);foreach(OrderReportEnding e in Enum.GetValues(typeof(OrderReportEnding)))RecordArchive.Ending(e);
                    Require(RecordArchive.Entries.All(e=>RecordArchive.Has(e.id)),"all current-chapter entries have unlock routes");
                    RecordArchive.Viewed("copy");session.OrderState.facts=0;session.OrderState.followUp=0;RecordArchive.Capture(session);RecordArchive.Reload();
                    Require(RecordArchive.Has("copy")&&!RecordArchive.IsNew("copy")&&RecordArchive.Has("ending-True"),"unlocks and viewed state survive reload and earlier investigation state");
                    Require(Path.GetFileName(RecordArchive.PathName)!=Path.GetFileName(InvestigationSaveStore.SavePath),"archive is separate from save slot");
                    screen=RecordArchiveScreen.Show(session.view.font,()=>{});screen.OpenEntry("ending-True");screen.ShowIllustration("ending-True");
                }else if(step==2){screen.SelectTab("인물");screen.OpenEntry("person-4");}
                else if(step==3){var portrait=screen.GetComponentInChildren<ArchivePortrait>();Require(portrait!=null,"unlocked character has 3D portrait");Require(portrait.RenderCount>0&&portrait.RenderCount<=90,"portrait rendering is active and rate limited");screen.SelectTab("증거");screen.OpenEntry("delivery");screen.ShowIllustration("delivery");}
                else if(step==4){
                    foreach(var e in RecordArchive.Entries){screen.OpenEntry(e.id);Canvas.ForceUpdateCanvases();var body=screen.GetComponentsInChildren<UnityEngine.UI.Text>().FirstOrDefault(t=>t.name=="Body");Require(body==null||body.preferredHeight<=body.rectTransform.rect.height+1,"text fits "+e.id);}
                    foreach(var e in RecordArchive.Entries.Where(e=>e.tab=="증거"||e.tab=="결말")){
                        screen.ShowIllustration(e.id);Canvas.ForceUpdateCanvases();var art=screen.GetComponentsInChildren<UnityEngine.UI.RawImage>().Single();
                        Require(art.texture!=null&&art.uvRect.width>0&&art.uvRect.xMax<=1&&art.uvRect.yMax<=1,"illustration linked "+e.id);
                        screen.OpenEntry(e.id);Require(!screen.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(),"illustration released on return "+e.id);
                    }
                    screen.SelectTab("결말");
                    for(int q=0;q<4;q++){screen.Bonus(q);Canvas.ForceUpdateCanvases();var text=screen.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t=>t.name=="Body");Require(text.preferredHeight<=text.rectTransform.rect.height+1,"bonus text fits "+q);}
                    screen.Bonus(2);
                }else if(step==5){screen.Close();
                    session.view.gameObject.SetActive(true);session.view.SetPrologueLayout(false);
                    var actor=Object.FindObjectsByType<VarcoIdleMotion>().Single(m=>m.Profile==4);var player=session.introductionStage.player;
                    var spawn=new GameObject("PreviewArrival");spawn.transform.position=actor.transform.position-actor.transform.forward*2.1f;spawn.transform.rotation=Quaternion.LookRotation(actor.transform.forward);player.TeleportTo(spawn.transform);Object.Destroy(spawn);
                    camera=player.viewCamera;fov=camera.fieldOfView;rotation=camera.transform.localRotation;
                    session.view.ShowModal("수군 진영","장윤","“본문과 인계 확인표는 대조해야 하오. 남겨진 표시만으로 확인을 대신할 수는 없소.”","검사 중");session.view.AddChoice("대화 마치기",()=>{});VarcoIdleMotion.Speak("장윤","본문과 인계 확인표는 대조해야 하오.");
                }else if(step==6){var p=session.GetComponent<ConversationPresentation>();Require(p.Active&&camera.fieldOfView<fov,"dialogue zoom active");Require(p.GetComponentsInChildren<Light>().Count(l=>l.enabled)==3,"three shadow-free dialogue lights");VarcoIdleMotion.StopSpeech();session.view.HideModal();}
                else if(step==7){Require(Mathf.Abs(camera.fieldOfView-fov)<.01f&&Quaternion.Angle(camera.transform.localRotation,rotation)<.01f,"camera restored after dialogue");Require(session.GetComponentsInChildren<Light>().All(l=>!l.enabled),"dialogue lights disabled after close");
                    session.view.ShowModal("대화","장윤","“확인하시오.”","검사");VarcoIdleMotion.Speak("장윤","확인하시오.");}
                else if(step==8){
                    var arrival=new GameObject("TravelCheck");arrival.transform.position=session.introductionStage.player.transform.position+Vector3.right;arrival.transform.rotation=Quaternion.Euler(0,71,0);session.introductionStage.player.TeleportTo(arrival.transform);Object.Destroy(arrival);
                    Require(!session.GetComponent<ConversationPresentation>().Active&&Mathf.Abs(camera.fieldOfView-fov)<.01f,"travel cancels dialogue presentation before teleport");
                    Require(Quaternion.Angle(camera.transform.localRotation,Quaternion.identity)<.01f,"travel retains destination camera orientation");
                    string archive=RecordArchive.PathName;File.WriteAllText(archive,"broken test archive");RecordArchive.Reload();Require(RecordArchive.Has("ending-True")&&RecordArchive.Error!=null,"damaged archive falls back to backup");
                    File.AppendAllText(folder+"/checks.txt","COMPLETE failures=0\n");Finish();return;}
                step++;captured=false;
            }catch(Exception e){File.AppendAllText(folder+"/checks.txt","FAIL "+e+"\nCOMPLETE failures=1\n");Debug.LogException(e);Finish();}
        }
        static void Finish(){EditorApplication.update-=Tick;if(screen)screen.Close();VarcoIdleMotion.StopSpeech();EditorApplication.isPlaying=false;EditorApplication.delayCall+=RestorePath;}
        static void RestorePath(){string previous=SessionState.GetString("FeedbackArchive.PreviousDirectory","");if(string.IsNullOrEmpty(previous))SessionState.EraseString("RedCliffMystery.TestSaveDirectory");else SessionState.SetString("RedCliffMystery.TestSaveDirectory",previous);}
    }
}

