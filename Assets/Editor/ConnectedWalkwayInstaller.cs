using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class ConnectedWalkwayInstaller
{
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();
        var camp=route.transform.Find("NavalCamp");
        Undo.RecordObject(camp,"Connect naval camp"); camp.SetPositionAndRotation(new Vector3(0,0,-23.5f),Quaternion.Euler(0,180,0));
        Undo.RecordObject(route.navalArrival,"Move naval arrival");
        route.navalArrival.SetPositionAndRotation(camp.TransformPoint(new Vector3(0,.08f,-5)),camp.rotation);
        Undo.RecordObject(route,"Enable connected walk"); route.connectedWalkway=true; route.animateDeparture=true; route.campBoundaryZ=-15.7f;
        var fence=camp.Find("SouthFence");
        foreach(Transform child in fence) {
            if(child.name=="Base" || child.name=="UpperRail" || Mathf.Abs(child.localPosition.x)<2.3f) {
                Undo.RecordObject(child.gameObject,"Open camp gate"); child.gameObject.SetActive(false);
            }
        }
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PrototypeMaterials/RouteWood.mat") ?? fence.GetComponentsInChildren<Renderer>(true).First().sharedMaterial;
        var earth=camp.Find("Ground").GetComponent<Renderer>().sharedMaterial;
        var path=camp.Find("ApproachPath").GetComponent<Renderer>().sharedMaterial;
        foreach(int side in new[]{-1,1}) {
            Cube(fence,"GateFenceBase"+side,new Vector3(side*5.65f,.57f,0),new Vector3(6.1f,1.14f,.18f),wood);
            Cube(fence,"GateFenceRail"+side,new Vector3(side*5.65f,1.5f,0),new Vector3(6.1f,.14f,.2f),wood);
        }
        var root=route.transform.Find("ConnectedWalkway");
        if(root==null) { var go=new GameObject("ConnectedWalkway"); go.transform.SetParent(route.transform,false); root=go.transform; Undo.RegisterCreatedObjectUndo(go,"Create connected path"); }
        Cube(root,"Ground",new Vector3(0,-.12f,-11),new Vector3(11,.24f,16),earth);
        Cube(root,"WornPath",new Vector3(0,.01f,-11),new Vector3(3,.018f,13),path,false);
        foreach(int side in new[]{-1,1}) {
            Cube(root,"PathBoundary"+side,new Vector3(side*3.1f,.55f,-11),new Vector3(.14f,1.1f,10.8f),wood);
            for(int i=0;i<5;i++) Cube(root,"PathPost"+side+"_"+i,new Vector3(side*3.1f,.8f,-6.5f-i*2.3f),new Vector3(.15f,1.6f,.15f),wood);
            Cube(root,"SupplyCrate"+side,new Vector3(side*4.05f,.36f,-10),new Vector3(1.1f,.72f,.85f),wood);
        }
        var room=UnityEngine.Object.FindObjectsByType<Transform>().First(t=>t.name=="ArchiveRoom");
        foreach(var t in room.GetComponentsInChildren<Transform>()) if(t.name=="ArchiveExteriorApron" || t.name.StartsWith("ArchiveExteriorPost") || t.name.StartsWith("ArchiveExitLantern") || t.name.StartsWith("ArchiveExitWarmLight")) { Undo.RecordObject(t.gameObject,"Replace empty apron"); t.gameObject.SetActive(false); }
        // Low, warm light carries the visible path to the existing lit camp entrance.
        foreach(int side in new[]{-1,1}) {
            var name="PathLantern"+side; var t=root.Find(name);
            if(t==null) { var go=new GameObject(name,typeof(Light)); go.transform.SetParent(root,false); t=go.transform; }
            t.localPosition=new Vector3(side*2.8f,1.8f,-10.5f);
            var light=t.GetComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.72f,.42f); light.intensity=2; light.range=11; light.shadows=LightShadows.None;
            Cube(root,name+"Post",new Vector3(side*2.8f,.85f,-10.5f),new Vector3(.12f,1.7f,.12f),wood);
            var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Audio/Movement/ExitLanternGlow.mat");
            Cube(root,name+"Shade",new Vector3(side*2.8f,1.8f,-10.5f),new Vector3(.16f,.24f,.16f),glow,false);
        }
        EditorSceneManager.MarkSceneDirty(route.gameObject.scene); EditorSceneManager.SaveScene(route.gameObject.scene); AssetDatabase.SaveAssets();
        Debug.Log("CONNECTED_WALKWAY_APPLIED");
    }
    static void Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=true)
    {
        var t=parent.Find(name); GameObject go;
        if(t==null) { go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); Undo.RegisterCreatedObjectUndo(go,"Build connected path"); }
        else go=t.gameObject;
        go.transform.localPosition=position; go.transform.localRotation=Quaternion.identity; go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material; go.GetComponent<Collider>().enabled=solid;
    }
}
