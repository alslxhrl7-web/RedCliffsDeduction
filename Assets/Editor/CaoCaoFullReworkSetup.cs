using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class CaoCaoFullReworkSetup
{
    const string Folder="Assets/Art/Characters/VarcoV3/CaoCao/FullRework";
    const string Archive="LocalArchive/CaoCaoHairEnemies20261016/Revision16";
    const string Prefab="Assets/Art/Characters/VarcoV3/CaoCao/CaoCaoVisual.prefab";
    [Serializable] public class Entry { public string part, material, @base, normal, metalSmooth; public float roughness,metallic; public bool alphaClip; }
    [Serializable] public class Catalog { public Entry[] items; }
    static Camera reviewCamera; static Vector3 reviewPosition; static Quaternion reviewRotation; static float reviewFov;
    static Transform Visual()=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t=>t.name=="CaoCao"&&t.Find("VarcoVisual")).Find("VarcoVisual");
    static string InteractionState(Transform npc)=>string.Join("\n",npc.GetComponents<Component>().Select(c=>c.GetType().FullName+EditorJsonUtility.ToJson(c)))+string.Join("\n",npc.GetComponentsInChildren<Collider>(true).Select(c=>EditorJsonUtility.ToJson(c)));
    static void WriteGameMesh(Mesh destination,Mesh source)
    {
        // Recreate the vertex buffers through Mesh APIs. CopySerialized can retain a stale
        // native vertex layout when a rebake changes UV/color channels on an existing asset.
        // Only UV0 is consumed by the delivered Lit and strand shaders.
        destination.Clear(false);destination.indexFormat=source.indexFormat;destination.MarkDynamic();
        destination.vertices=source.vertices;destination.normals=source.normals;destination.tangents=source.tangents;destination.uv=source.uv;
        destination.subMeshCount=source.subMeshCount;
        for(int i=0;i<source.subMeshCount;i++)destination.SetIndices(source.GetIndices(i),source.GetTopology(i),i,false);
        destination.bounds=source.bounds;destination.UploadMeshData(false);
        if(!destination.uv.SequenceEqual(source.uv)||!destination.vertices.SequenceEqual(source.vertices))throw new InvalidOperationException("Mesh upload data mismatch");
    }
    static Material MaterialFor(Entry entry)
    {
        string path=Folder+"/"+entry.part+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        bool strand=entry.part=="TempleHair"||entry.part=="OriginalHair"||entry.part=="OriginalKnot"||entry.part=="DetailGroom"||entry.part=="BeardGroom";
        var shader=Shader.Find(strand?"RedCliffs/Character Hair":"Universal Render Pipeline/Lit");
        if(!shader||!shader.isSupported)throw new InvalidOperationException("Character shader unavailable: "+entry.part);
        mat.shader=shader;
        foreach(var pair in new[]{(entry.@base,"_BaseMap"),(entry.normal,"_BumpMap"),(entry.metalSmooth,"_MetallicGlossMap")})
        {
            if(string.IsNullOrEmpty(pair.Item1)){mat.SetTexture(pair.Item2,null);continue;}
            string texPath=Folder+"/"+pair.Item1;var imp=(TextureImporter)AssetImporter.GetAtPath(texPath);
            if(imp==null)throw new InvalidOperationException("Missing texture "+texPath);
            imp.sRGBTexture=pair.Item2=="_BaseMap";imp.mipmapEnabled=true;imp.maxTextureSize=entry.part=="Body"?4096:2048;
            if(pair.Item2=="_BumpMap")imp.textureType=TextureImporterType.NormalMap;
            if(entry.alphaClip&&pair.Item2=="_BaseMap")imp.alphaIsTransparency=true;
            imp.SaveAndReimport();mat.SetTexture(pair.Item2,AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        }
        mat.SetColor("_BaseColor",entry.part=="OriginalHair"?new Color(.55f,.55f,.55f,1):entry.part=="OriginalKnot"?new Color(.68f,.68f,.68f,1):Color.white);mat.SetFloat("_Metallic",entry.metallic);
        mat.SetFloat("_Smoothness",entry.part=="Body" ? .9f : string.IsNullOrEmpty(entry.metalSmooth)?1-entry.roughness:1);
        mat.SetFloat("_BumpScale",1f);
        if(string.IsNullOrEmpty(entry.normal))mat.DisableKeyword("_NORMALMAP");else mat.EnableKeyword("_NORMALMAP");
        if(strand)mat.SetFloat("_SpecularStrength",entry.part=="DetailGroom"?.035f:entry.part=="BeardGroom"?.03f:.035f);
        if(entry.part=="FacialStrands"||entry.part=="OriginalFlyaways"){mat.SetTexture("_BumpMap",null);mat.DisableKeyword("_NORMALMAP");mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
        if(entry.alphaClip){mat.SetFloat("_EnvironmentReflections",1);mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
        // The body casts the main silhouette; millimetre overlays must not self-shadow into stippling.
        mat.SetShaderPassEnabled("ShadowCaster",entry.part=="Body"||entry.part=="Face"||entry.part=="Hands");
        if(!string.IsNullOrEmpty(entry.metalSmooth))mat.EnableKeyword("_METALLICSPECGLOSSMAP");else mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Cull",strand||entry.alphaClip||entry.part=="FacialStrands"||entry.part=="OriginalFlyaways"?0:2);mat.SetFloat("_Surface",0);mat.SetFloat("_ZWrite",1);
        mat.SetFloat("_AlphaClip",entry.alphaClip?1:0);mat.SetFloat("_Cutoff",entry.part=="BeardGroom"?.48f:.18f);mat.SetFloat("_AlphaToMask",entry.alphaClip?1:0);
        if(entry.alphaClip){mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;}
        else{mat.DisableKeyword("_ALPHATEST_ON");mat.renderQueue=-1;}
        EditorUtility.SetDirty(mat);return mat;
    }
    static void Attach(Transform visual,GameObject source,Dictionary<string,Material> materials)
    {
        var model=visual.Find("Model");var toModel=(visual.worldToLocalMatrix*model.localToWorldMatrix).inverse;
        var temp=Object.Instantiate(source);temp.hideFlags=HideFlags.HideAndDontSave;
        var combines=new List<CombineInstance>();var ordered=new List<Material>();var disposable=new List<Mesh>();
        try
        {
            foreach(var f in temp.GetComponentsInChildren<MeshFilter>())
            {
                string part=f.name.Replace("Game_","");if(!materials.ContainsKey(part))throw new InvalidOperationException("Unknown mesh part "+f.name);
                var mesh=Object.Instantiate(f.sharedMesh);disposable.Add(mesh);var vertices=mesh.vertices;var normals=mesh.normals;
                var local=temp.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
                var normalToVisual=local.inverse.transpose;var normalToModel=toModel.inverse.transpose;
                var hips=new Vector3(0,1.78f*.48f,0);var lean=Quaternion.Euler(1.5f,0,0);var facing=Quaternion.Euler(0,180,0);
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=facing*local.MultiplyPoint3x4(vertices[i]);
                    float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.78f*.30f,1.78f*.64f,p.y));
                    p=Vector3.Lerp(p,hips+lean*(p-hips),weight);vertices[i]=toModel.MultiplyPoint3x4(p);
                    // Preserve Blender's smooth normals across UV splits instead of rebuilding faceted normals.
                    var n=facing*normalToVisual.MultiplyVector(normals[i]);
                    normals[i]=normalToModel.MultiplyVector(Quaternion.Slerp(Quaternion.identity,lean,weight)*n).normalized;
                }
                mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();
                combines.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});ordered.Add(materials[part]);
            }
            var result=new Mesh{name="CaoCao_FullRework_Posed",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            result.CombineMeshes(combines.ToArray(),false,false);result.RecalculateBounds();
            string meshPath=Folder+"/CaoCao_FullRework_Posed.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(saved){WriteGameMesh(saved,result);Object.DestroyImmediate(result);EditorUtility.SetDirty(saved);}
            else{saved=new Mesh{name=result.name};WriteGameMesh(saved,result);Object.DestroyImmediate(result);AssetDatabase.CreateAsset(saved,meshPath);}
            model.GetComponent<MeshFilter>().sharedMesh=saved;model.GetComponent<MeshRenderer>().sharedMaterials=ordered.ToArray();
            var oldHair=visual.Find("EnemiesHair");if(oldHair)Object.DestroyImmediate(oldHair.gameObject);
            var motion=visual.GetComponent<RedCliffMystery.VarcoIdleMotion>();
            // The posed lips are around 1.538 m; .90 placed the old speech band at eye height.
            motion.MouthHeightOverride=.864f;
            motion.DisableLipMotion=true;
        }
        finally{foreach(var mesh in disposable)Object.DestroyImmediate(mesh);Object.DestroyImmediate(temp);}
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Apply")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first");
        string interactionBefore=InteractionState(Visual().parent);
        Directory.CreateDirectory(Archive+"/PreviousUnity");
        foreach(string path in new[]{Prefab,"Assets/Scenes/Investigation.unity"})
        {string backup=Archive+"/PreviousUnity/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);}
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/CaoCao_Rework.fbx");
        importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        var catalog=JsonUtility.FromJson<Catalog>("{\"items\":"+File.ReadAllText(Folder+"/materials.json")+"}");
        var materials=catalog.items.ToDictionary(e=>e.part,MaterialFor);var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CaoCao_Rework.fbx");
        var prefab=PrefabUtility.LoadPrefabContents(Prefab);
        try{Attach(prefab.transform,source,materials);PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var visual=Visual();Attach(visual,source,materials);EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
        if(interactionBefore!=InteractionState(visual.parent))throw new InvalidOperationException("Interaction or collider state changed");
        var model=visual.Find("Model");var renderer=model.GetComponent<Renderer>();
        File.WriteAllText(Archive+"/unity-applied.txt","Applied full revision\nVertices="+model.GetComponent<MeshFilter>().sharedMesh.vertexCount+"\nBounds="+renderer.bounds+"\nMaterials="+renderer.sharedMaterials.Length+"\nCollider count="+visual.parent.GetComponentsInChildren<Collider>(true).Length);
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Import Baked Revision")]
    public static void ImportBakedRevision()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before importing a complete revision");
        string source=Archive+"/GameExport";var catalog=JsonUtility.FromJson<Catalog>("{\"items\":"+File.ReadAllText(source+"/materials.json")+"}");
        var files=new HashSet<string>{"CaoCao_Rework.fbx","materials.json"};
        foreach(var e in catalog.items)foreach(var f in new[]{e.@base,e.normal,e.metalSmooth})if(!string.IsNullOrEmpty(f))files.Add(f);
        foreach(var f in files)if(!File.Exists(source+"/"+f))throw new FileNotFoundException(source+"/"+f);
        AssetDatabase.DisallowAutoRefresh();
        try{foreach(var f in files)File.Copy(source+"/"+f,Folder+"/"+f,true);}
        finally{AssetDatabase.AllowAutoRefresh();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Apply();InspectDelivery();
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Review Full")]
    public static void ReviewFull()=>Review(new Vector3(.9f,.12f,-2.7f),1.0f,40);
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Review Portrait")]
    public static void ReviewPortrait()=>Review(new Vector3(.35f,.025f,-.95f),1.59f,34);
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Review Rear")]
    public static void ReviewRear()=>Review(new Vector3(.8f,.12f,2.4f),1.0f,40);
    static void Review(Vector3 offset,float height,float fov)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Review in Play");
        var cam=Camera.main;var v=Visual();
        if(!reviewCamera){reviewCamera=cam;reviewPosition=cam.transform.position;reviewRotation=cam.transform.rotation;reviewFov=cam.fieldOfView;}
        var target=v.TransformPoint(new Vector3(0,height,0));cam.transform.position=target+v.TransformDirection(offset);cam.transform.LookAt(target);cam.fieldOfView=fov;
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Restore Camera")]
    public static void RestoreCamera(){if(!reviewCamera)return;reviewCamera.transform.SetPositionAndRotation(reviewPosition,reviewRotation);reviewCamera.fieldOfView=reviewFov;reviewCamera=null;}
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Validate Live")]
    public static void ValidateLive()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Validate in Play");
        var v=Visual();var motion=v.GetComponent<RedCliffMystery.VarcoIdleMotion>();var model=v.Find("Model");var mf=model.GetComponent<MeshFilter>();
        motion.Sample(0);var rest=mf.sharedMesh.vertices;var watch=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<10;i++)motion.Sample(i*.1f);watch.Stop();
        motion.Sample(1);var animated=mf.sharedMesh.vertices;float footDelta=0;
        for(int i=0;i<rest.Length;i++)if(v.InverseTransformPoint(model.TransformPoint(rest[i])).y<.08f)footDelta=Mathf.Max(footDelta,Vector3.Distance(rest[i],animated[i])*model.lossyScale.x);
        motion.StartSpeech("조조의 군령을 확인하라.",0);motion.Sample(.07f);var speaking=mf.sharedMesh.vertices;int changed=0;float sumHeight=0;
        motion.StartSpeech(null,0);motion.Sample(.07f);var silent=mf.sharedMesh.vertices;
        for(int i=0;i<silent.Length;i++)if(Vector3.Distance(silent[i],speaking[i])>0.00000001f){changed++;sumHeight+=v.InverseTransformPoint(model.TransformPoint(silent[i])).y;}
        motion.Sample(Time.time);
        File.WriteAllText(Archive+"/live-validation.txt","Play mode actual character\nVertices="+rest.Length+"\nAverage Sample milliseconds="+watch.Elapsed.TotalMilliseconds/10+"\nFoot movement metres="+footDelta+"\nSpeech affected vertices="+changed+"\nMean speech vertex height="+(changed>0?sumHeight/changed:0)+"\nMouth fraction="+motion.MouthHeightOverride+"\nRenderer bounds="+model.GetComponent<Renderer>().bounds);
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Inspect Delivery")]
    public static void InspectDelivery()
    {
        var model=Visual().Find("Model");var mesh=model.GetComponent<MeshFilter>().sharedMesh;var materials=model.GetComponent<Renderer>().sharedMaterials;
        var errors=new List<string>();var shader=Shader.Find("RedCliffs/Character Hair");
        if(!shader||!shader.isSupported)errors.Add("Hair shader unavailable");
        if(shader)errors.AddRange(ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message));
        if(mesh.subMeshCount!=materials.Length)errors.Add("Submesh/material mismatch");
        if(mesh.uv.Length!=mesh.vertexCount)errors.Add("Missing game UV");
        foreach(var m in materials)if(!m||!m.GetTexture("_BaseMap"))errors.Add("Missing material or base color");
        File.WriteAllText(Archive+"/delivery-inspection.txt","Vertices="+mesh.vertexCount+"\nSubmeshes="+mesh.subMeshCount+"\nMaterials="+string.Join(",",materials.Select(m=>m?m.name:"null"))+"\nShaders="+string.Join(",",materials.Select(m=>m?m.shader.name:"null"))+"\nErrors="+string.Join(";",errors));
        if(errors.Count>0)throw new InvalidOperationException(string.Join(";",errors));
    }
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Capture Static Mesh")]
    public static void CaptureStaticMesh()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("This check must run after Play stops");
        var v=Visual();var go=new GameObject("CaoCao Static Render Check"){hideFlags=HideFlags.HideAndDontSave};
        var rt=new RenderTexture(2048,1152,24);Texture2D image=null;var old=RenderTexture.active;
        try{
            var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);camera.aspect=16f/9;camera.fieldOfView=36;camera.nearClipPlane=.03f;
            var target=v.TransformPoint(new Vector3(0,1.48f,0));camera.transform.position=target+v.TransformDirection(new Vector3(.10f,.015f,-1.12f));camera.transform.LookAt(target);camera.Render();
            RenderTexture.active=rt;image=new Texture2D(2048,1152,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2048,1152),0,0);image.Apply();File.WriteAllBytes(Archive+"/static-after-play.png",image.EncodeToPNG());
        }finally{RenderTexture.active=old;if(image)Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
    }
}
















