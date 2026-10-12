using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RedCliffMystery.Investigation;

public static class NavalRiverInstaller
{
    const string Folder="Assets/Art/Environment/NavalRiver";
    const string Log="Logs/NavalRiver";
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Saved Investigation scene required");
        if(GameObject.Find("NavalRiverBackdrop"))throw new Exception("River already installed");
        Directory.CreateDirectory(Log);File.Copy(scene.path,Log+"/before-"+DateTime.Now.ToString("HHmmss")+".unity");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/Environment","NavalRiver");
        var water=Mat("River",new Color(.10f,.23f,.26f),.32f);
        var bank=Mat("Bank",new Color(.28f,.27f,.20f),0);
        var wood=Mat("Hull",new Color(.18f,.105f,.058f),.05f);
        var deck=Mat("Deck",new Color(.36f,.26f,.15f),.05f);
        var sail=Mat("FurledCanvas",new Color(.50f,.43f,.30f),0);
        var hill=Mat("FarBank",new Color(.16f,.23f,.22f),0);
        var root=new GameObject("NavalRiverBackdrop");Undo.RegisterCreatedObjectUndo(root,"Add river backdrop");
        Box(root.transform,"Water",new Vector3(0,-.65f,-116),new Vector3(330,.08f,162),water);
        Box(root.transform,"NearBank",new Vector3(0,-.43f,-34.4f),new Vector3(150,.8f,3.4f),bank);
        var boatRoots=new List<Transform>();
        for(int i=0;i<4;i++){
            var boat=new GameObject("MooredBoat"+i).transform;boat.SetParent(root.transform,false);
            boat.position=new Vector3(-21+i*15,-.52f,-43-i%2*7);boat.rotation=Quaternion.Euler(0,12+i*8,0);
            boatRoots.Add(boat);
            var hull=Hull();AssetDatabase.CreateAsset(hull,Folder+"/Hull"+i+".asset");
            var body=new GameObject("PlankedHull",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(boat,false);
            body.GetComponent<MeshFilter>().sharedMesh=hull;body.GetComponent<MeshRenderer>().sharedMaterial=wood;
            for(int p=0;p<12;p++)Box(boat,"DeckPlank",new Vector3(0,.55f,-3.1f+p*.56f),new Vector3(2.25f,.08f,.51f),deck);
            foreach(float x in new[]{-1.27f,1.27f})Box(boat,"Gunwale",new Vector3(x,.83f,0),new Vector3(.12f,.20f,7.0f),wood);
            Box(boat,"Mast",new Vector3(0,3.0f,.5f),new Vector3(.15f,5.4f,.15f),wood);
            Box(boat,"Yard",new Vector3(0,4.4f,.5f),new Vector3(3.3f,.13f,.13f),wood);
            Box(boat,"FurledSail",new Vector3(0,4.22f,.5f),new Vector3(3,.25f,.29f),sail);
            Box(boat,"Cabin",new Vector3(0,1,-1.8f),new Vector3(1.6f,.9f,1.8f),sail);
        }
        for(int i=0;i<9;i++){
            var mound=GameObject.CreatePrimitive(PrimitiveType.Sphere);mound.name="FarShore"+i;mound.transform.SetParent(root.transform,false);
            mound.transform.position=new Vector3(-130+i*32,-6,-155-i%2*13);mound.transform.localScale=new Vector3(53,18+i%3*9,32);
            UnityEngine.Object.DestroyImmediate(mound.GetComponent<Collider>());mound.GetComponent<Renderer>().sharedMaterial=hill;
        }
        var sky=new Material(Shader.Find("RedCliffs/RiverSky"));AssetDatabase.CreateAsset(sky,Folder+"/RiverSky.mat");
        var atmosphere=root.AddComponent<NavalRiverAtmosphere>();atmosphere.session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
        atmosphere.playerCamera=Camera.main;atmosphere.skyTemplate=sky;atmosphere.boats=boatRoots.ToArray();
        foreach(var r in root.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Validate();
    }
    static Material Mat(string name,Color color,float smooth)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
        AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
    }
    static void Box(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static Mesh Hull()
    {
        var v=new List<Vector3>();var t=new List<int>();
        Vector3[] rim={new Vector3(0,.65f,-4.3f),new Vector3(-1.35f,.65f,-2.9f),new Vector3(-1.35f,.65f,2.9f),new Vector3(0,.65f,4.3f),new Vector3(1.35f,.65f,2.9f),new Vector3(1.35f,.65f,-2.9f)};
        for(int i=0;i<6;i++){
            var a=rim[i];var b=rim[(i+1)%6];var c=new Vector3(b.x*.56f,-.28f,b.z*.83f);var d=new Vector3(a.x*.56f,-.28f,a.z*.83f);
            int s=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3,s+2,s+1,s,s+3,s+2,s});
        }
        var mesh=new Mesh{name="SixSidedBoatHull"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static void Validate()
    {
        Directory.CreateDirectory(Log);var root=GameObject.Find("NavalRiverBackdrop");var a=root?root.GetComponent<NavalRiverAtmosphere>():null;
        var checks=new List<string>();
        Action<bool,string> test=(ok,msg)=>{checks.Add((ok?"PASS ":"FAIL ")+msg);};
        test(a&&a.session&&a.playerCamera&&a.skyTemplate,"clock and sky references");
        test(a&&a.boats.Length==4&&a.boats.All(b=>b),"four boats");
        test(root&&root.GetComponentsInChildren<Collider>().Length==0,"no obstruction of player movement");
        test(root&&root.GetComponentsInChildren<InvestigationTarget>().Length==0,"no new investigation costs");
        test(root&&root.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial&&r.sharedMaterial.shader&&!r.sharedMaterial.shader.name.Contains("Error")),"valid materials");
        test(root&&root.GetComponentsInChildren<Renderer>().All(r=>r.bounds.max.z<-32.5f),"all scenery outside playable camp");
        int failed=checks.Count(s=>s.StartsWith("FAIL"));checks.Add("COMPLETE failures="+failed);File.WriteAllLines(Log+"/checks.txt",checks);
        if(failed>0)throw new Exception("River validation failed");
    }
    public static void CapturePhases()
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        var a=UnityEngine.Object.FindAnyObjectByType<NavalRiverAtmosphere>();
        Validate();var cameraObject=new GameObject("RiverVerificationCamera",typeof(Camera));var cam=cameraObject.GetComponent<Camera>();
        cam.CopyFrom(a.playerCamera);cam.enabled=false;cam.transform.position=new Vector3(8,1.75f,-31);cam.transform.rotation=Quaternion.Euler(4,180,0);
        cam.fieldOfView=65;cam.farClipPlane=300;cam.clearFlags=CameraClearFlags.Skybox;
        var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
        try{
            for(int i=0;i<4;i++){
                a.ApplyTime(i);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes(Log+"/phase-"+i+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                if(!RenderSettings.fog||RenderSettings.fogStartDistance<25)throw new Exception("Fog obscures investigation area");
            }
            File.WriteAllText(Log+"/play-checks.txt","PASS four clock phases rendered\nPASS near investigation area outside fog start\nCOMPLETE failures=0\n");
        }finally{a.ApplyTime(a.session.InvestigationTimeSlot);RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);}
    }
    static void UpdateSky()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        var sky=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/RiverSky.mat");
        sky.shader=Shader.Find("RedCliffs/RiverSky");EditorUtility.SetDirty(sky);AssetDatabase.SaveAssets();
    }
    static void RestoreTestPath()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        string old=SessionState.GetString("FeedbackArchive.PreviousDirectory","");
        if(string.IsNullOrEmpty(old))SessionState.EraseString("RedCliffMystery.TestSaveDirectory");
        else SessionState.SetString("RedCliffMystery.TestSaveDirectory",old);
    }
}
