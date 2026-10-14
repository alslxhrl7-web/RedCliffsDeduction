using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    [MenuItem("Tools/Red Cliffs/Guidance/Verify tutorial flow")]
    public static void VerifyTutorialFlow()
    {
        if(!Application.isPlaying || !SessionState.GetString(SaveKey,"").Contains("OrderChecks"))throw new Exception("Isolated title Play required");
        S.StartCoroutine(TutorialFlow());
    }
    static IEnumerator TutorialFlow()
    {
        Directory.CreateDirectory("Logs/Tutorial");
        var title=UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
        Require(title!=null,"Title required");title.StartButton.onClick.Invoke();S.BeginIntroduction("첫 조사 확인");
        Click("도입 건너뛰기");Require(S.view.body.text.Contains("최종 보고"),"Skipping lost mission");
        Click("문서고로 향한다");Require(S.view.title.text=="조사를 시작하기 전에","Tutorial offer missing");
        Click("첫 조사 배우기");yield return null;
        Require(S.TutorialStep==1 && Capture().inquiryIds.Length==0,"Tutorial start charges time");
        ScreenCapture.CaptureScreenshot("Logs/Tutorial/movement.png");
        yield return null;
        var player=S.route.player;var controller=player.GetComponent<CharacterController>();
        controller.enabled=false;player.transform.position+=Vector3.right*.8f;player.transform.Rotate(0,20,0);controller.enabled=true;
        yield return null;yield return null;
        Require(S.TutorialStep==2,"Movement and look did not advance");
        S.ObserveTutorialInteraction(InvestigationTarget.TargetKind.MissingDocument);
        Require(S.TutorialStep==3,"Interaction step missing");
        Pay(S.InspectDocument);Require(Capture().inquiryIds.Length==1,"First real inquiry cost differs");
        Click("반출 대장 확인");S.CloseModal();yield return null;
        Require(S.TutorialStep==4,"Reading did not advance");
        ScreenCapture.CaptureScreenshot("Logs/Tutorial/selection.png");yield return null;
        var snapshot=Capture();Call("RestoreSave",snapshot);Require(S.TutorialStep==4,"Tutorial save resume failed");
        S.ShowInvestigationDirections();S.SelectInvestigationLead("caimao");
        Require(S.TutorialStep==5 && Capture().inquiryIds.Length==1,"Choice completion or cost failed");
        S.StartInvestigationTutorial();S.SkipInvestigationTutorial();Require(S.TutorialStep==0,"Skip failed");
        snapshot.tutorialVersion=0;snapshot.tutorialStep=4;Call("RestoreSave",snapshot);Require(S.TutorialStep==0,"Old saves forced tutorial");
        S.ShowInvestigationHelp();Require(S.view.body.text.Contains("진술"),"Replay help missing");
        File.WriteAllText("Logs/Tutorial/checks.txt","COMPLETE failures=0\nVerified: intro skip, tutorial offer, move/look, interact, first paid inquiry, record, choose, save resume, skip, legacy save, help.");
    }
}
