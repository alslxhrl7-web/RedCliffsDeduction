using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedCliffMystery.Investigation;

public static class WalkNetwork20261012
{
    const string Log = "Logs/WalkNetwork20261012";
    static Transform[] Objects => UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    static string PathOf(Transform t) => t.parent==null ? t.name : PathOf(t.parent)+"/"+t.name;
    [MenuItem("적벽추리/연결 진영/배치 기록")]
    public static void Audit()
    {
        Directory.CreateDirectory(Log);
        File.WriteAllLines(Log+"/layout.txt", Objects.Where(t=>t.GetComponent<Collider>()!=null || t.name.Contains("Arrival") || t.name.Contains("Spawn") || t.name.Contains("Tent_")).Select(t=>PathOf(t)+" active="+t.gameObject.activeInHierarchy+" pos="+t.position+" bounds="+(t.GetComponent<Collider>()!=null?t.GetComponent<Collider>().bounds.ToString():t.GetComponent<Renderer>()!=null?t.GetComponent<Renderer>().bounds.ToString():"")));
    }

    [MenuItem("적벽추리/연결 진영/걸어서 이동하는 길 적용")]
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!="Assets/Scenes/Investigation.unity" || scene.isDirty) throw new InvalidOperationException("Saved Investigation scene in Edit mode required");
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();
        if(route.transform.Find("CampWalkNetwork")!=null) throw new InvalidOperationException("Network already installed");
        var stage=UnityEngine.Object.FindAnyObjectByType<IntroductionStage>();
        Undo.RecordObject(route,"Connect all camps"); route.connectedCampNetwork=true;
        Undo.RecordObject(stage,"Keep command tent accessible"); stage.keepTentAccessible=true;
        Undo.RecordObject(stage.tent,"Open command tent"); stage.tent.SetActive(true);
        var root=new GameObject("CampWalkNetwork").transform; root.SetParent(route.transform,false); Undo.RegisterCreatedObjectUndo(root.gameObject,"Build walk network");
        var camp=route.transform.Find("NavalCamp");
        var earth=camp.Find("Ground").GetComponent<Renderer>().sharedMaterial;
        var trail=camp.Find("ApproachPath").GetComponent<Renderer>().sharedMaterial;
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/DarkTimber.mat");
        foreach(string side in new[]{"WestFence","EastFence"})
        {
            var fence=camp.Find(side);
            foreach(var c in fence.GetComponentsInChildren<Collider>())
            {
                if(c.name=="Base" || c.name=="UpperRail" || Mathf.Abs(c.transform.position.z+18.5f)<2.15f)
                { Undo.RecordObject(c.gameObject,"Open side gate"); c.gameObject.SetActive(false); }
            }
            float x=fence.position.x;
            foreach(var part in new[]{new Vector2(-32.25f,-20.65f),new Vector2(-16.35f,-14.75f)})
                foreach(float y in new[]{.57f,1.5f}) Box(root,side+"GateRail",new Vector3(x,y,(part.x+part.y)/2),new Vector3(.18f,y<1?1.14f:.14f,part.y-part.x),wood);
        }
        Box(root,"SupplyRoadGround",new Vector3(-20,-.15f,-18.5f),new Vector3(27,.3f,6),earth);
        Box(root,"SupplyRoadTrail",new Vector3(-20,.012f,-18.5f),new Vector3(27,.016f,3.6f),trail,false);
        Box(root,"CommandRoadGround",new Vector3(25,-.15f,-18.5f),new Vector3(36,.3f,6),earth);
        Box(root,"CommandRoadTrail",new Vector3(24,.012f,-18.5f),new Vector3(33,.016f,3.6f),trail,false);
        Box(root,"CommandApproachGround",new Vector3(40,-.15f,-10.5f),new Vector3(6,.3f,18),earth);
        Box(root,"CommandApproachTrail",new Vector3(40,.013f,-10.5f),new Vector3(3.6f,.016f,17),trail,false);
        Rail(root,new Vector3(-31,0,-21.2f),new Vector3(-9.1f,0,-21.2f),wood);
        Rail(root,new Vector3(-31,0,-15.8f),new Vector3(-9.1f,0,-15.8f),wood);
        Rail(root,new Vector3(9.1f,0,-21.2f),new Vector3(42.7f,0,-21.2f),wood);
        Rail(root,new Vector3(9.1f,0,-15.8f),new Vector3(37.3f,0,-15.8f),wood);
        Rail(root,new Vector3(37.3f,0,-15.8f),new Vector3(37.3f,0,-5.1f),wood);
        Rail(root,new Vector3(42.7f,0,-21.2f),new Vector3(42.7f,0,-5.1f),wood);
        // The outer edges of the supply square stay fenced; the new east gate remains open.
        Rail(root,new Vector3(-48.5f,0,-32),new Vector3(-48.5f,0,-15),wood);
        Rail(root,new Vector3(-48.5f,0,-32),new Vector3(-31.5f,0,-32),wood);
        Rail(root,new Vector3(-48.5f,0,-15),new Vector3(-31.5f,0,-15),wood);
        Rail(root,new Vector3(-31.5f,0,-32),new Vector3(-31.5f,0,-21.2f),wood);
        Rail(root,new Vector3(-31.5f,0,-16),new Vector3(-31.5f,0,-15),wood);
        foreach(float x in new[]{-27f,-17f,16f,27f,40f}) Lamp(root,new Vector3(x,2,-20.7f),wood);
        Lamp(root,new Vector3(42.2f,2,-8),wood);
        OpenCommandDoor(stage,root,wood);
        var cao=stage.tent.transform.Find("CaoCao");
        var target=cao.GetComponent<InvestigationTarget>() ?? Undo.AddComponent<InvestigationTarget>(cao.gameObject);
        Undo.RecordObject(target,"Enable commander conversation"); target.kind=InvestigationTarget.TargetKind.CaoCao;
        Sign(root,"← 보급 천막",new Vector3(-7,1.4f,-17.5f),180,wood);
        Sign(root,"지휘 천막 →",new Vector3(7,1.4f,-17.5f),180,wood);
        Sign(root,"수군 진영 →",new Vector3(-32,1.4f,-17),180,wood);
        Sign(root,"문서고 / 수군 진영 ←",new Vector3(39,1.4f,-5.2f),0,wood);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Audit(); Debug.Log("WALK_NETWORK_APPLIED");
    }
    static void Box(Transform root,string name,Vector3 p,Vector3 size,Material material,bool solid=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Collider>().enabled=solid;
    }
    static void Rail(Transform root,Vector3 a,Vector3 b,Material wood)
    {
        var delta=b-a;
        Box(root,"RoadBoundary",(a+b)/2+Vector3.up*.48f,new Vector3(Mathf.Max(.10f,Mathf.Abs(delta.x)),.96f,Mathf.Max(.10f,Mathf.Abs(delta.z))),wood);
        int count=Mathf.CeilToInt(delta.magnitude/2.8f);
        for(int i=0;i<=count;i++) Box(root,"FencePost",Vector3.Lerp(a,b,i/(float)count)+Vector3.up*.65f,new Vector3(.13f,1.3f,.13f),wood);
    }
    static void Lamp(Transform root,Vector3 p,Material wood)
    {
        Box(root,"LampPost",p+Vector3.down, new Vector3(.12f,2,.12f),wood);
        var go=new GameObject("RoadLamp",typeof(Light));go.transform.SetParent(root,false);go.transform.position=p;
        var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.74f,.46f);l.range=10;l.intensity=1.7f;l.shadows=LightShadows.None;
        Box(root,"LampGlow",p,new Vector3(.20f,.27f,.20f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Audio/Movement/ExitLanternGlow.mat"),false);
    }
    static void Sign(Transform root,string label,Vector3 p,float yaw,Material wood)
    {
        Box(root,"Signboard",p,new Vector3(2.5f,.5f,.1f),wood,false);
        Box(root,"Signpost",p+Vector3.down*.7f,new Vector3(.12f,1.4f,.12f),wood);
        var t=new GameObject("Direction_"+label).transform;t.SetParent(root,false);t.position=p+new Vector3(0,0,yaw==180?.07f:-.07f);t.rotation=Quaternion.Euler(0,yaw,0);
        var tm=t.gameObject.AddComponent<TextMesh>();tm.text=label;tm.fontSize=48;tm.characterSize=.048f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.94f,.84f,.65f);
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Title/RedCliffsSerif-SemiBold.ttf");tm.font=font;t.GetComponent<Renderer>().sharedMaterial=font.material;
        t.gameObject.AddComponent<CampDirectionSign>();
    }
    [MenuItem("적벽추리/연결 진영/표지판 마무리와 저장")]
    public static void FinishSigns()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Edit mode required");
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();var root=route.transform.Find("CampWalkNetwork");
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/DarkTimber.mat");
        foreach(var tm in root.GetComponentsInChildren<TextMesh>()) {
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Title/RedCliffsSerif-SemiBold.ttf");tm.font=font;tm.GetComponent<Renderer>().sharedMaterial=font.material;
            if(tm.GetComponent<CampDirectionSign>()==null) tm.gameObject.AddComponent<CampDirectionSign>();
        }
        if(root.Find("Direction_지휘 천막 →")==null) Sign(root,"지휘 천막 →",new Vector3(7,1.4f,-17.5f),180,wood);
        if(root.Find("Direction_수군 진영 →")==null) Sign(root,"수군 진영 →",new Vector3(-32,1.4f,-17),180,wood);
        if(root.Find("Direction_문서고 / 수군 진영 ←")==null) Sign(root,"문서고 / 수군 진영 ←",new Vector3(39,1.4f,-5.2f),0,wood);
        EditorSceneManager.MarkSceneDirty(route.gameObject.scene);EditorSceneManager.SaveScene(route.gameObject.scene);AssetDatabase.SaveAssets();Audit();
    }

    [MenuItem("적벽추리/연결 진영/통행 검사")]
    public static void CheckPaths()
    {
        Directory.CreateDirectory(Log);Physics.SyncTransforms();var results=new System.Collections.Generic.List<string>();int failures=0;
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();
        var player=route.player.GetComponent<CharacterController>();bool enabled=player.enabled;player.enabled=false;
        var door=route.curtain.transform.Find("ClosedDoor").GetComponent<Collider>();bool doorEnabled=door.enabled;door.enabled=false;
        try {
            var points=new[]{new Vector3(0,0,-3.5f),new Vector3(0,0,-18.5f),new Vector3(-39.1f,0,-18.5f),new Vector3(-39.1f,0,-22),new Vector3(0,0,-18.5f),new Vector3(40,0,-18.5f),new Vector3(40,0,-2)};
            int[][] paths={new[]{0,1},new[]{1,2,3},new[]{4,5,6}};
            foreach(var path in paths) for(int j=1;j<path.Length;j++) {
                Vector3 a=points[path[j-1]],b=points[path[j]],d=b-a;
                bool blocked=Physics.CapsuleCast(a+Vector3.up*.38f,a+Vector3.up*1.65f,.29f,d.normalized,out var hit,d.magnitude,~0,QueryTriggerInteraction.Ignore);
                if(blocked) {failures++;results.Add("FAIL path "+a+" -> "+b+" blocked by "+PathOf(hit.transform)+" at "+hit.point);}
                else results.Add("PASS clearance "+a+" -> "+b);
                for(float step=0;step<=d.magnitude;step+=.4f) {
                    var p=a+d.normalized*step;
                    if(!Physics.Raycast(p+Vector3.up*.2f,Vector3.down,out var ground,.5f,~0,QueryTriggerInteraction.Ignore)) {failures++;results.Add("FAIL missing ground "+p);break;}
                }
            }
            results.Add("COMPLETE failures="+failures);
            File.WriteAllLines(Log+"/paths.txt",results);
        } finally {player.enabled=enabled;door.enabled=doorEnabled;}
    }
    static void OpenCommandDoor(IntroductionStage stage,Transform root,Material wood)
    {
        var front=stage.tent.transform.Find("RearEntrance");Undo.RecordObject(front.gameObject,"Open command entry");front.gameObject.SetActive(false);
        var mf=stage.tent.transform.Find("TentPolish/Tent_WovenCanvas").GetComponent<MeshFilter>();
        var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name="CommandCanvasOpenDoor";
        var v=mesh.vertices;var old=mesh.triangles;var tris=new System.Collections.Generic.List<int>();
        for(int i=0;i<old.Length;i+=3) {
            var a=v[old[i]];var b=v[old[i+1]];var c=v[old[i+2]];var mid=(a+b+c)/3;
            if(a.z<-4 && b.z<-4 && c.z<-4 && Mathf.Abs(mid.x)<1.8125f && Mathf.Min(a.y,b.y,c.y)<3.16f) continue;
            tris.Add(old[i]);tris.Add(old[i+1]);tris.Add(old[i+2]);
        }
        mesh.triangles=tris.ToArray();mesh.RecalculateBounds();
        const string asset="Assets/Art/Environment/CommandTent/Meshes/CommandCanvasOpenDoor.asset";
        AssetDatabase.CreateAsset(mesh,asset);Undo.RecordObject(mf,"Open canvas doorway");mf.sharedMesh=mesh;
        var cloth=mf.GetComponent<Renderer>().sharedMaterial;
        foreach(float x in new[]{36.2f,43.8f}) Box(root,"EntrySideCollision",new Vector3(x,1.4f,-4.5f),new Vector3(4.4f,2.8f,.16f),cloth);
        foreach(float x in new[]{38.3f,41.7f}) Box(root,"DoorJamb",new Vector3(x,1.45f,-4.35f),new Vector3(.14f,2.9f,.14f),wood);
    }
    [MenuItem("적벽추리/연결 진영/출입구 외형 마무리")]
    public static void FinishDoor()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Edit mode required");
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();var root=route.transform.Find("CampWalkNetwork");
        var stage=UnityEngine.Object.FindAnyObjectByType<IntroductionStage>();
        var mf=stage.tent.transform.Find("TentPolish/Tent_WovenCanvas").GetComponent<MeshFilter>();
        var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Environment/CommandTent/Meshes/Tent_WovenCanvas.asset");
        var mesh=mf.sharedMesh;var v=source.vertices;var old=source.triangles;var tris=new System.Collections.Generic.List<int>();
        for(int i=0;i<old.Length;i+=3) {
            var a=v[old[i]];var b=v[old[i+1]];var c=v[old[i+2]];var mid=(a+b+c)/3;
            if(a.z<-4 && b.z<-4 && c.z<-4 && Mathf.Abs(mid.x)<1.8125f && Mathf.Min(a.y,b.y,c.y)<3.16f) continue;
            tris.Add(old[i]);tris.Add(old[i+1]);tris.Add(old[i+2]);
        }
        mesh.triangles=tris.ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        foreach(var t in root.GetComponentsInChildren<Transform>()) {
            if(t.name=="EntrySideCollision") t.GetComponent<Renderer>().enabled=false;
            if((t.name=="Signboard" || t.name=="Signpost" || t.name.StartsWith("Direction_")) && Mathf.Abs(t.position.z+5.2f)<.15f)
                t.position+=Vector3.left*3.5f;
        }
        EditorSceneManager.MarkSceneDirty(route.gameObject.scene);EditorSceneManager.SaveScene(route.gameObject.scene);AssetDatabase.SaveAssets();
        Capture();CheckPaths();
    }
    [MenuItem("적벽추리/연결 진영/안내와 바닥 가장자리 보완")]
    public static void FinishEdges()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Edit mode required");
        var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();var root=route.transform.Find("CampWalkNetwork");
        foreach(var sign in root.GetComponentsInChildren<TextMesh>())
            sign.text=sign.text.Replace("← ","").Replace(" →","").Replace(" ←","");
        if(root.Find("CommandApronBoundary")==null) {
            var edge=new GameObject("CommandApronBoundary").transform;edge.SetParent(root,false);
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/DarkTimber.mat");
            Rail(edge,new Vector3(34.2f,0,-5.8f),new Vector3(37.3f,0,-5.8f),wood);
            Rail(edge,new Vector3(42.7f,0,-5.8f),new Vector3(45.8f,0,-5.8f),wood);
            Rail(edge,new Vector3(34.2f,0,-5.8f),new Vector3(34.2f,0,-4.5f),wood);
            Rail(edge,new Vector3(45.8f,0,-5.8f),new Vector3(45.8f,0,-4.5f),wood);
        }
        EditorSceneManager.MarkSceneDirty(route.gameObject.scene);EditorSceneManager.SaveScene(route.gameObject.scene);
        CheckPaths();
    }
    [MenuItem("적벽추리/연결 진영/장면 확인 이미지")]
    public static void Capture()
    {
        Directory.CreateDirectory(Log);
        foreach(var shot in new[]{("supply-road",new Vector3(-8,1.65f,-18.5f),new Vector3(-36,1,-20)),("command-road",new Vector3(12,1.65f,-18.5f),new Vector3(40,1,-17)),("command-door",new Vector3(40,1.65f,-8),new Vector3(40,1.65f,0))}) {
            var go=new GameObject("WalkPreviewCamera");go.hideFlags=HideFlags.HideAndDontSave;var c=go.AddComponent<Camera>();
            c.CopyFrom(UnityEngine.Object.FindAnyObjectByType<FirstPersonInvestigator>().viewCamera);c.enabled=false;c.fieldOfView=65;c.nearClipPlane=.05f;
            c.transform.position=shot.Item2;c.transform.LookAt(shot.Item3);
            var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;
            try {c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Log+"/"+shot.Item1+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
            finally{RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
