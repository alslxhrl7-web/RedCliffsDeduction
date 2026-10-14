using System;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    static void CheckGuidance()
    {
        var before=Capture(); int slots=before.inquiryIds.Length;
        try {
            Require(!S.IsLeadAvailable("storage") && !S.IsLeadAvailable("outgoing"), "Unseen lead leaked");
            Require(S.IsLeadAvailable("courier"), "Briefed witness unavailable");
            S.SelectInvestigationLead("courier");
            Require(S.SelectedLead=="courier" && Capture().inquiryIds.Length==slots,"Selecting lead spent time");
            S.SelectInvestigationLead("storage");Require(S.SelectedLead=="courier","Hidden lead accepted");
            var saved=JsonUtility.FromJson<InvestigationSaveData>(JsonUtility.ToJson(Capture()));
            Call("RestoreSave",saved);Require(S.SelectedLead=="courier","Selection did not survive save");
            saved.selectedLead=null;Call("RestoreSave",saved);Require(S.SelectedLead=="","Old save default failed");
            saved.selectedLead="unknown";Call("RestoreSave",saved);Require(S.SelectedLead=="","Unknown lead not sanitized");
            S.ShowInvestigationDirections();Require(S.view.body.text.Contains("대상을 고르면"),"Direction menu missing");
            Require(!S.CaseSummary().Contains("한진") && !S.CaseSummary().Contains("고의"),"Objective reveals conclusion");
        } finally {Call("RestoreSave",before);S.CloseModal();}
    }
    [MenuItem("Tools/Red Cliffs/Guidance/Preview fresh investigation")]
    public static void PreviewGuidance()
    {
        if(!Application.isPlaying || !SessionState.GetString(SaveKey,"").Contains("OrderChecks"))throw new Exception("Isolated title Play required");
        var title=UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
        Require(title!=null,"Title required");title.StartButton.onClick.Invoke();S.BeginIntroduction("조사 안내 확인");
        S.AdvanceDialogue();S.AdvanceDialogue();S.AdvanceDialogue();Click("문서고로 향한다"); Click("안내 건너뛰고 자유 수사");S.SelectInvestigationLead("courier");
    }
}
