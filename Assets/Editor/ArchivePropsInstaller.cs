using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class ArchivePropsInstaller
{
    const string Root="Assets/Art/Props/Archive";
    static Material bamboo,edge,cord,ink,wood,stone,clay;
    static readonly string[] Names={"BoundSlipRoll","OpenSlipLedger","TiedSlipStack","WritingKit","WoodDocumentCase","StorageChest","ClaySealBundle"};
    [MenuItem("적벽추리/문서고 소품 제작과 적용")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Open Investigation first");
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        bamboo=Mat("AgedBamboo",new Color(.57f,.40f,.19f),true);
        edge=Mat("CutBamboo",new Color(.72f,.56f,.31f),true);
        cord=Mat("HempBinding",new Color(.30f,.25f,.16f)); ink=Mat("BrushInk",new Color(.055f,.049f,.039f));
        wood=Mat("WornCaseWood",new Color(.24f,.12f,.065f),true); stone=Mat("Inkstone",new Color(.075f,.08f,.085f));
        clay=Mat("SealingClay",new Color(.34f,.20f,.12f));
        foreach(string name in Names) BuildPrefab(name);
        var room=scene.GetRootGameObjects().Single(r=>r.name=="ArchiveRoom").transform;
        int count=0;
        foreach(var original in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BambooRecords").ToArray())
        {
            var renderer=original.GetComponent<Renderer>(); Undo.RecordObject(renderer,"Replace block records"); renderer.enabled=false;
            bool rolled=count%3==0;
            var visual=Place(rolled?"BoundSlipRoll":"TiedSlipStack",original,"ArchiveRecordVisual",original.position+new Vector3(0,rolled?-.035f:-.15f,0),Quaternion.Euler(0,rolled?90:(count%3-1)*3,0));
            if(rolled) visual.transform.localScale=Vector3.Scale(visual.transform.localScale,new Vector3(1.15f,1.05f,1.1f));
            count++;
        }
        foreach(var original in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="AdjacentRecords").ToArray())
        {
            var renderer=original.GetComponent<Renderer>(); Undo.RecordObject(renderer,"Replace document stack"); renderer.enabled=false;
            Place("TiedSlipStack",original,"ArchiveRecordVisual",original.position+new Vector3(.02f,-.024f,0),Quaternion.Euler(0,(count++%3-1)*7,0),.66f);
        }
        var props=room.Find("ArchiveDeskProps");
        if(props==null) { var go=new GameObject("ArchiveDeskProps"); go.transform.SetParent(room,false); props=go.transform; Undo.RegisterCreatedObjectUndo(go,"Furnish archive"); }
        Place("WritingKit",props,"ClerkWritingKit",new Vector3(-1.07f,1.025f,2.45f),Quaternion.Euler(0,-8,0));
        Place("OpenSlipLedger",props,"ClerkOpenLedger",new Vector3(-.98f,1.025f,2.03f),Quaternion.Euler(0,-6,0),.83f);
        Place("BoundSlipRoll",props,"DeskRoll",new Vector3(1.22f,1.105f,2.8f),Quaternion.Euler(0,90,0),.85f);
        Place("ClaySealBundle",props,"SealedDispatch",new Vector3(4.28f,1.025f,4.78f),Quaternion.Euler(0,12,0),.8f);
        Place("WritingKit",props,"PersonalDeskKit",new Vector3(5.26f,1.025f,4.36f),Quaternion.Euler(0,5,0),.75f);
        // Keep the missing document tray empty and all original evidence hit volumes.
        var papers=UnityEngine.Object.FindObjectsByType<InvestigationTarget>().Single(t=>t.kind==InvestigationTarget.TargetKind.PersonalPapers);
        foreach(var r in papers.GetComponentsInChildren<Renderer>()) if(r.name=="Case" || r.name=="Lid" || r.name=="Clasp") { Undo.RecordObject(r,"Finish evidence case"); r.enabled=false; }
        var oldCase=papers.transform.Find("Case");
        Place("WoodDocumentCase",papers.transform,"FinishedCaseVisual",new Vector3(oldCase.position.x,1.025f,oldCase.position.z),Quaternion.identity);
        Place("StorageChest",props,"ArchiveStorageChest",new Vector3(5.03f,0,-.7f),Quaternion.Euler(0,-90,0));
        Place("StorageChest",props,"ArchiveStorageChestSmall",new Vector3(5.1f,0,.75f),Quaternion.Euler(0,-90,0),.78f);
        Place("TiedSlipStack",props,"ChestRecords",new Vector3(5.03f,.67f,-.7f),Quaternion.Euler(0,90,0),.88f);
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        Debug.Log("ARCHIVE_PROPS_APPLIED: shelf records="+36+", adjacent stacks=3, reusable prefabs="+Names.Length);
    }

    static Material Mat(string name,Color color,bool grain=false)
    {
        string path=Root+"/"+name+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",.08f); mat.SetFloat("_Metallic",0);
        if(grain) {
            string texPath=Root+"/"+name+"Grain.asset"; var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if(texture==null) { texture=new Texture2D(128,256,TextureFormat.RGB24,true); texture.name=name+" grain"; var random=new System.Random(128);
                float[] strands=Enumerable.Range(0,128).Select(_=>(float)random.NextDouble()).ToArray();
                for(int y=0;y<256;y++) for(int x=0;x<128;x++) { float shade=.83f+.13f*strands[x]+.05f*(float)random.NextDouble()+.035f*Mathf.Sin(y*.09f+x*.8f); texture.SetPixel(x,y,new Color(shade,shade,shade)); }
                texture.Apply(); texture.wrapMode=TextureWrapMode.Repeat; AssetDatabase.CreateAsset(texture,texPath); }
            mat.SetTexture("_BaseMap",texture);
        }
        EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Place(string asset,Transform parent,string name,Vector3 position,Quaternion rotation,float scale=1)
    {
        var previous=parent.Find(name); if(previous!=null) Undo.DestroyObjectImmediate(previous.gameObject);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+asset+".prefab"));
        go.name=name; go.transform.SetParent(parent,true); go.transform.SetPositionAndRotation(position,rotation);
        go.transform.localScale=new Vector3(scale/parent.lossyScale.x,scale/parent.lossyScale.y,scale/parent.lossyScale.z);
        Undo.RegisterCreatedObjectUndo(go,"Apply finished archive prop"); return go;
    }
    static void BuildPrefab(string name)
    {
        var go=new GameObject(name); var t=go.transform;
        try {
            switch(name) {
                case "BoundSlipRoll": Roll(t); break;
                case "OpenSlipLedger": Ledger(t); break;
                case "TiedSlipStack": Stack(t); break;
                case "WritingKit": Kit(t); break;
                case "WoodDocumentCase": Case(t,.70f,.38f,.62f); break;
                case "StorageChest": Case(t,1.15f,.65f,.78f); break;
                case "ClaySealBundle": Stack(t); Box(t,"ClaySeal",new Vector3(0,.085f,0),new Vector3(.10f,.026f,.08f),clay); break;
            }
            Combine(t,name);
            if(name=="StorageChest") { var collider=go.AddComponent<BoxCollider>(); collider.center=new Vector3(0,.325f,0); collider.size=new Vector3(1.15f,.7f,.78f); }
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+name+".prefab");
        } finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    static void Roll(Transform t)
    {
        // Filled, wound ends distinguish a rolled volume from an empty wooden tube.
        Cylinder(t,"InnerRoll",Vector3.zero,new Vector3(.155f,.185f,.155f),bamboo,Quaternion.Euler(90,0,0));
        foreach(int side in new[]{-1,1}) {
            Cylinder(t,"WoundEnd",new Vector3(0,0,side*.19f),new Vector3(.16f,.002f,.16f),edge,Quaternion.Euler(90,0,0));
            foreach(float radius in new[]{.025f,.044f,.064f,.079f}) Ring(t,"WoundLayers",new Vector3(0,0,side*.193f),radius,.0023f,wood,Quaternion.Euler(90,0,0));
        }
        for(int i=0;i<20;i++) {
            float a=i*Mathf.PI*2/20; var stave=Box(t,"Slip",new Vector3(Mathf.Cos(a)*.092f,Mathf.Sin(a)*.092f,0),new Vector3(.027f,.012f,.38f+(i%3)*.003f),i%4==0?edge:bamboo);
            stave.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg+90);
        }
        foreach(float z in new[]{-.12f,.12f}) Ring(t,"Cord",new Vector3(0,0,z),.105f,.008f,cord,Quaternion.Euler(90,0,0));
        Box(t,"VolumeTag",new Vector3(.106f,.008f,.035f),new Vector3(.009f,.066f,.14f),edge);
        for(int i=0;i<4;i++) Box(t,"TagInk",new Vector3(.112f,.008f,-.012f+i*.024f),new Vector3(.002f,.027f,.006f),ink);
        Box(t,"BindingKnot",new Vector3(.10f,.03f,-.12f),new Vector3(.022f,.02f,.027f),cord);
    }
    static void Ledger(Transform t)
    {
        for(int i=0;i<20;i++) {
            float x=-.266f+i*.028f;
            Box(t,"WrittenSlip",new Vector3(x,.014f,0),new Vector3(.025f,.025f,.43f+(i%3-1)*.005f),i%5==0?edge:bamboo);
            // Small irregular strokes are decorative; evidence text stays in the reading UI.
            for(int j=0;j<7;j++) {
                float z=-.155f+j*.044f;
                Box(t,"InkStroke",new Vector3(x,.0275f,z),new Vector3(.014f,.001f,.004f),ink);
                if((i+j)%3!=0) Box(t,"InkStroke",new Vector3(x+.003f,.0277f,z+.007f),new Vector3(.004f,.001f,.018f),ink);
                if((i+j)%2==0) Box(t,"InkStroke",new Vector3(x-.005f,.0277f,z+.012f),new Vector3(.012f,.001f,.003f),ink);
            }
        }
        foreach(float z in new[]{-.115f,.115f}) Box(t,"BindingCord",new Vector3(0,.030f,z),new Vector3(.575f,.009f,.009f),cord);
    }
    static void Stack(Transform t)
    {
        for(int layer=0;layer<3;layer++) for(int i=0;i<18;i++) Box(t,"FoldedSlip",new Vector3(-.237f+i*.028f+(layer%2)*.008f,.012f+layer*.021f,(layer%2)*.009f),new Vector3(.025f,.018f,.38f),layer==1?edge:bamboo);
        foreach(float x in new[]{-.17f,.17f}) { Box(t,"TieTop",new Vector3(x,.073f,0),new Vector3(.012f,.009f,.395f),cord); foreach(int side in new[]{-1,1}) Box(t,"TieEdge",new Vector3(x,.038f,side*.197f),new Vector3(.012f,.075f,.008f),cord); }
        Box(t,"CoverTag",new Vector3(.02f,.076f,0),new Vector3(.16f,.008f,.055f),edge);
        for(int i=0;i<5;i++) Box(t,"TagInk",new Vector3(-.035f+i*.025f,.081f,0),new Vector3(.009f,.001f,.021f),ink);
    }
    static void Kit(Transform t)
    {
        Box(t,"InkstoneBase",new Vector3(-.08f,.017f,0),new Vector3(.24f,.034f,.16f),stone);
        Box(t,"GrindingSurface",new Vector3(-.08f,.035f,-.018f),new Vector3(.19f,.003f,.09f),ink);
        foreach(int side in new[]{-1,1}) Box(t,"StoneRim",new Vector3(-.08f+side*.111f,.042f,0),new Vector3(.018f,.021f,.16f),stone);
        Box(t,"InkCake",new Vector3(-.06f,.052f,.01f),new Vector3(.082f,.028f,.035f),ink);
        BrushBody(t);
        Cone(t,"BrushTip",new Vector3(.11f,.034f,-.158f),.012f,.054f,ink);
        foreach(float z in new[]{-.07f,.05f}) Box(t,"BrushRest",new Vector3(.11f,.014f,z),new Vector3(.105f,.023f,.016f),wood);
        Cylinder(t,"WaterCup",new Vector3(-.11f,.036f,.14f),new Vector3(.09f,.036f,.09f),clay,Quaternion.identity);
        Cylinder(t,"WaterSurface",new Vector3(-.11f,.071f,.14f),new Vector3(.067f,.002f,.067f),stone,Quaternion.identity);
        Ring(t,"CupRim",new Vector3(-.11f,.073f,.14f),.043f,.004f,clay,Quaternion.identity);
    }
    static void Case(Transform t,float width,float height,float depth)
    {
        for(int i=0;i<5;i++) Box(t,"CasePlank",new Vector3(-width*.4f+i*width*.2f,height*.45f,0),new Vector3(width*.195f,height*.9f,depth),wood);
        for(int i=0;i<5;i++) Box(t,"LidPlank",new Vector3(-width*.4f+i*width*.2f,height,0),new Vector3(width*.195f,.045f,depth+.025f),bamboo);
        foreach(int side in new[]{-1,1}) {
            Box(t,"LidBrace",new Vector3(side*width*.34f,height+.028f,0),new Vector3(.045f,.019f,depth+.03f),wood);
            Box(t,"CornerRail",new Vector3(side*width*.46f,height*.45f,-depth*.505f),new Vector3(.045f,height*.9f,.025f),bamboo);
        }
        Box(t,"CordClosure",new Vector3(0,height*.77f,-depth*.52f),new Vector3(.032f,.12f,.015f),cord);
        Box(t,"ClayClosure",new Vector3(0,height*.69f,-depth*.54f),new Vector3(.055f,.037f,.018f),clay);
        Box(t,"CaseLabel",new Vector3(width*.20f,height*.59f,-depth*.51f),new Vector3(width*.28f,.065f,.009f),edge);
        for(int i=0;i<4;i++) Box(t,"LabelInk",new Vector3(width*.11f+i*width*.06f,height*.59f,-depth*.517f),new Vector3(.013f,.025f,.002f),ink);
    }
    static void BrushBody(Transform t)
    {
        // One continuous mesh makes the shaft and collar share exact joints.
        float[] z={.107f,.102f,-.128f,-.139f,-.158f}; float[] radii={.006f,.007f,.007f,.0105f,.0105f};
        var vertices=new List<Vector3>(); var triangles=new List<int>(); const int sides=16;
        for(int ring=0;ring<z.Length;ring++) for(int i=0;i<sides;i++) { float angle=i*2*Mathf.PI/sides; vertices.Add(new Vector3(.11f+Mathf.Cos(angle)*radii[ring],.034f+Mathf.Sin(angle)*radii[ring],z[ring])); }
        for(int ring=0;ring<z.Length-1;ring++) for(int i=0;i<sides;i++) { int a=ring*sides+i,b=ring*sides+(i+1)%sides,c=(ring+1)*sides+i,d=(ring+1)*sides+(i+1)%sides; triangles.AddRange(new[]{a,c,b,b,c,d}); }
        MeshPart(t,"ContinuousBrush",Vector3.zero,Quaternion.identity,bamboo,vertices,triangles);
    }
    static GameObject Box(Transform t,string name,Vector3 p,Vector3 size,Material mat)=>Primitive(t,name,PrimitiveType.Cube,p,size,mat,Quaternion.identity);
    static void Cylinder(Transform t,string name,Vector3 p,Vector3 size,Material mat,Quaternion rotation)=>Primitive(t,name,PrimitiveType.Cylinder,p,size,mat,rotation);
    static GameObject Primitive(Transform t,string name,PrimitiveType type,Vector3 p,Vector3 size,Material mat,Quaternion rotation)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(t,false); go.transform.localPosition=p; go.transform.localScale=size; go.transform.localRotation=rotation; go.GetComponent<Renderer>().sharedMaterial=mat; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    static void Ring(Transform parent,string name,Vector3 p,float radius,float tube,Material mat,Quaternion rotation)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>(); const int n=32,m=6;
        for(int i=0;i<n;i++) for(int j=0;j<m;j++) { float a=i*2*Mathf.PI/n,b=j*2*Mathf.PI/m; vertices.Add(new Vector3(Mathf.Cos(a)*(radius+tube*Mathf.Cos(b)),tube*Mathf.Sin(b),Mathf.Sin(a)*(radius+tube*Mathf.Cos(b)))); }
        for(int i=0;i<n;i++) for(int j=0;j<m;j++) { int a=i*m+j,b=((i+1)%n)*m+j,c=i*m+(j+1)%m,d=((i+1)%n)*m+(j+1)%m; triangles.AddRange(new[]{a,c,b,b,c,d}); }
        MeshPart(parent,name,p,rotation,mat,vertices,triangles);
    }
    static void Cone(Transform parent,string name,Vector3 p,float radius,float length,Material mat)
    {
        var vertices=new List<Vector3>{new Vector3(0,0,-length)}; var triangles=new List<int>();
        for(int i=0;i<12;i++) { float a=i*2*Mathf.PI/12; vertices.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0)); }
        for(int i=0;i<12;i++) triangles.AddRange(new[]{0,1+i,1+(i+1)%12});
        MeshPart(parent,name,p,Quaternion.identity,mat,vertices,triangles);
    }
    static void MeshPart(Transform parent,string name,Vector3 p,Quaternion rotation,Material mat,List<Vector3> vertices,List<int> triangles)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localRotation=rotation;
        var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=mat;
    }
    static void Combine(Transform root,string name)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>(); var materials=filters.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct().ToArray();
        var groupMeshes=new List<Mesh>();
        foreach(var material in materials) {
            var combines=filters.Where(f=>f.GetComponent<Renderer>().sharedMaterial==material).Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh {indexFormat=UnityEngine.Rendering.IndexFormat.UInt32}; mesh.CombineMeshes(combines,true,true); groupMeshes.Add(mesh);
        }
        var result=new Mesh {name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        result.CombineMeshes(groupMeshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);
        string path=Root+"/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null) AssetDatabase.CreateAsset(result,path); else { EditorUtility.CopySerialized(result,existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(result); result=existing; }
        foreach(var mesh in groupMeshes) UnityEngine.Object.DestroyImmediate(mesh);
        var children=root.Cast<Transform>().ToArray(); foreach(var child in children) UnityEngine.Object.DestroyImmediate(child.gameObject);
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=result; root.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;
    }
}
