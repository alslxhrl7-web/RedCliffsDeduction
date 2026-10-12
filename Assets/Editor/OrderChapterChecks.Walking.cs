using System;
using UnityEngine;
using RedCliffMystery.Investigation;

public static partial class OrderChapterChecks
{
    static void WalkTo(float x,float z)
    {
        var player=S.route.player;var controller=player.GetComponent<CharacterController>();
        int steps=0;
        while(steps++<1200) {
            var delta=new Vector3(x-player.transform.position.x,0,z-player.transform.position.z);
            if(delta.magnitude<.055f) { S.route.RefreshPhysicalLocation();return; }
            controller.Move(Vector3.ClampMagnitude(delta,.1f)+Vector3.down*.055f);
        }
        throw new Exception("Walking blocked at "+player.transform.position+" on route to "+x+","+z);
    }
    static void CheckConnectedCampWalk()
    {
        Require(S.route.connectedCampNetwork,"Full walking network disabled");
        int used=Capture().inquiryIds.Length;
        S.route.EnterArchive();S.route.BeginReturnWalk();Physics.SyncTransforms();
        WalkTo(0,-18.5f);Require(S.route.AtNavalCamp,"Naval arrival not detected on foot");
        WalkTo(-39.1f,-18.5f);WalkTo(-39.1f,-22);Require(S.route.AtSupplyTent,"Supply arrival not detected on foot");
        WalkTo(-39.1f,-18.5f);WalkTo(0,-18.5f);WalkTo(40,-18.5f);WalkTo(40,-2);
        Require(S.route.AtCommandTent && !S.route.AtNavalCamp && !S.route.AtSupplyTent,"Command location incorrect");
        WalkTo(40,-1.5f);
        S.route.player.viewCamera.transform.LookAt(new Vector3(41.4f,1.65f,.95f));Physics.SyncTransforms();
        var cao=S.route.player.FindTarget();Require(cao!=null && cao.kind==InvestigationTarget.TargetKind.CaoCao,"Cao Cao cannot be interacted with from the approach");
        cao.Interact(S);Click("대화를 마치고 걸어서 돌아가기");
        Require(S.route.AtCommandTent,"Closing command conversation teleported player");
        var saved=Capture();Require(saved.commandTent && saved.PreviewSummary().Contains("지휘 천막"),"Command save preview missing");
        S.route.EnterArchive();Call("LoadInvestigation",(Action)(()=>{}),saved);
        Require(S.route.AtCommandTent,"Command save restored in archive");
        S.BeginOrderFinal();Require(S.OrderReport.fromCommand,"Walking report origin lost");
        var draft=Capture();Require(draft.orderReport.fromCommand,"Draft dropped command origin");
        Call("CancelOrderFinal");Require(S.route.AtCommandTent,"Report cancellation teleported walker");
        Call("LoadInvestigation",(Action)(()=>{}),draft);Call("CancelOrderFinal");
        Require(S.route.AtCommandTent,"Loaded draft cancellation changed walking origin");
        Require(Capture().inquiryIds.Length==used,"Walking consumed investigation time");
        WalkTo(40,-2);WalkTo(40,-18.5f);WalkTo(0,-18.5f);WalkTo(0,-3.5f);
        Require(!S.route.AtCommandTent && !S.route.AtNavalCamp && !S.route.AtSupplyTent,"Walking back to archive kept previous location");
        S.route.EnterArchive();
    }
}
