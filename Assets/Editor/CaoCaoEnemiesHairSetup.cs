using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class CaoCaoEnemiesHairSetup
{
    const string Folder="Assets/Art/Characters/VarcoV3/CaoCao/EnemiesHair";
    static Camera reviewCamera;
    static Vector3 reviewPosition;
    static Quaternion reviewRotation;
    static float reviewFov;
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Restore Game Camera")]
    public static void RestoreGameCamera()
    {
        if(!reviewCamera)return;
        reviewCamera.transform.SetPositionAndRotation(reviewPosition,reviewRotation);
        reviewCamera.fieldOfView=reviewFov;reviewCamera=null;
    }
    static Transform Visual()
    {
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if(t.name=="CaoCao" && t.Find("VarcoVisual"))return t.Find("VarcoVisual");
        throw new System.InvalidOperationException("CaoCao visual missing");
    }
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Review Front")]
    public static void ReviewFront()=>Review(false);
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Review Side")]
    public static void ReviewSide()=>Review(true);
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Review Rear")]
    public static void ReviewRear()
    {
        Review(true);var v=Visual();var target=v.TransformPoint(new Vector3(0,1.65f,0));
        Camera.main.transform.position=target+v.TransformDirection(new Vector3(.45f,.035f,.8f));
        Camera.main.transform.LookAt(target);
    }
    static void Review(bool side)
    {
        if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Review only in Play mode");
        var v=Visual();var cam=Camera.main;
        if(!reviewCamera){reviewCamera=cam;reviewPosition=cam.transform.position;reviewRotation=cam.transform.rotation;reviewFov=cam.fieldOfView;}
        var target=v.TransformPoint(new Vector3(0,1.64f,0));
        cam.transform.position=target+v.TransformDirection(side?new Vector3(.65f,.03f,-.65f):new Vector3(0,.02f,-.9f));
        cam.transform.LookAt(target);cam.fieldOfView=38;
        var sb=new StringBuilder("Play Mode review "+(side?"side":"front")+"\n");
        foreach(var r in v.GetComponentsInChildren<Renderer>())
            sb.AppendLine(r.name+" enabled="+r.enabled+" bounds="+r.bounds+" material="+r.sharedMaterial.name);
        var motion=v.GetComponent<RedCliffMystery.VarcoIdleMotion>();
        var hair=v.Find("EnemiesHair");
        float maxError=0;
        foreach(var mf in hair.GetComponentsInChildren<MeshFilter>())
        {
            motion.Sample(0);var a=mf.sharedMesh.vertices[0];
            var rest=v.InverseTransformPoint(mf.transform.TransformPoint(a));
            motion.Sample(1);var b=v.InverseTransformPoint(mf.transform.TransformPoint(mf.sharedMesh.vertices[0]));
            float breath=Mathf.Sin(1.35f);float sway=Mathf.Sin(.61f);float amp=motion.Amplitude;
            var expected=new Vector3(rest.x*(1+breath*amp)+sway*amp*.45f,rest.y+breath*amp*.2f,rest.z*(1+breath*amp*1.5f));
            maxError=Mathf.Max(maxError,Vector3.Distance(expected,b));
        }
        motion.Sample(Time.time);
        sb.AppendLine("Head motion maximum error metres="+maxError);
        sb.AppendLine("Hair collider count="+hair.GetComponentsInChildren<Collider>().Length);
        File.WriteAllText("LocalArchive/CaoCaoHairEnemies20261016/runtime-"+(side?"side":"front")+".txt",sb.ToString());
    }
    static Material MaterialFor(string part)
    {
        string path=Folder+"/"+part+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        foreach(string map in new[]{"BaseColor","Normal","AO"})
        {
            string p=Folder+"/"+part+"_"+map+".png";
            var imp=(TextureImporter)AssetImporter.GetAtPath(p);
            imp.sRGBTexture=map=="BaseColor";
            if(map=="Normal")imp.textureType=TextureImporterType.NormalMap;
            imp.maxTextureSize=map=="AO"?1024:2048;imp.SaveAndReimport();
            mat.SetTexture(map=="BaseColor"?"_BaseMap":map=="Normal"?"_BumpMap":"_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(p));
        }
        mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.4f);mat.SetFloat("_Metallic",0);
        mat.SetFloat("_BumpScale",.85f);mat.SetFloat("_OcclusionStrength",.3f);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_OCCLUSIONMAP");
        EditorUtility.SetDirty(mat);return mat;
    }
    static void Attach(Transform parent,GameObject source,Material scalp,Material knot)
    {
        RefitExistingKnot(parent);
        var hair=parent.Find("EnemiesHair");
        if(!hair)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);
            go.name="EnemiesHair";hair=go.transform;
            Undo.RegisterCreatedObjectUndo(go,"Apply Cao Cao hair");
        }
        // The existing Varco pose faces -Z; the Blender FBX importer faces +Z.
        var lean=Quaternion.Euler(1.5f,0,0);var hips=new Vector3(0,1.78f*.48f,0);
        hair.localPosition=hips-lean*hips;hair.localRotation=lean*Quaternion.Euler(0,180,0);hair.localScale=Vector3.one;
        if(!hair.GetComponent<RedCliffMystery.VarcoHairAttachment>())hair.gameObject.AddComponent<RedCliffMystery.VarcoHairAttachment>();
        var strands=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Strands.mat");
        if(!strands)
        {
            strands=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            strands.SetColor("_BaseColor",new Color(.14f,.123f,.112f));
            strands.SetFloat("_Smoothness",.32f);AssetDatabase.CreateAsset(strands,Folder+"/Strands.mat");
        }
        strands.SetFloat("_SpecularHighlights",0);strands.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        strands.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(strands);
        var tie=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HairTie.mat");
        if(!tie){tie=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(tie,Folder+"/HairTie.mat");}
        tie.SetColor("_BaseColor",new Color(.17f,.13f,.10f));tie.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(tie);
        foreach(var r in hair.GetComponentsInChildren<MeshRenderer>())
        {
            bool detail=r.name.Contains("Strands");
            // Preserve optional source strands, but use their bake in game to avoid subpixel stippling.
            r.gameObject.SetActive(!detail);
            r.sharedMaterial=detail?strands:r.name.Contains("HairTie")?tie:r.name.Contains("Topknot")?knot:scalp;
            // Submillimetre strands cause shadow acne; the baked scalp casts the silhouette.
            r.shadowCastingMode=detail?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows=!detail;
        }
    }
    static void RefitExistingKnot(Transform visual)
    {
        var model=visual.Find("Model");if(!model)throw new System.InvalidOperationException("Original Model missing");
        var filter=model.GetComponent<MeshFilter>();
        var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Characters/VarcoV3/CaoCao/CaoCao_RolePose0.asset");
        if(!original)throw new System.InvalidOperationException("Original role pose missing");
        // Reproduce the Blender-authored affine refit on the existing posed mesh, retaining its pose/UVs.
        var copy=Object.Instantiate(original);copy.name="CaoCao_RolePose_HairRefit";
        var toVisual=visual.worldToLocalMatrix*model.localToWorldMatrix;var toMesh=toVisual.inverse;
        var lean=Quaternion.Euler(1.5f,0,0);var hips=new Vector3(0,1.78f*.48f,0);
        var vertices=copy.vertices;
        for(int i=0;i<vertices.Length;i++)
        {
            var p=toVisual.MultiplyPoint3x4(vertices[i]);
            p=hips+Quaternion.Inverse(lean)*(p-hips);if(p.y<=1.708f)continue;
            float t=Mathf.Clamp01((p.y-1.708f)/.018f);t=t*t*(3-2*t);
            p.x*=1-.36f*t;p.z=.009f+(p.z-.009f)*(1-.36f*t);p.y=1.708f+(p.y-1.708f)*.63f;
            vertices[i]=toMesh.MultiplyPoint3x4(hips+lean*(p-hips));
        }
        copy.vertices=vertices;copy.RecalculateBounds();
        string path=Folder+"/CaoCao_RolePose_HairRefit.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(copy,existing);Object.DestroyImmediate(copy);EditorUtility.SetDirty(existing);}
        else{AssetDatabase.CreateAsset(copy,path);existing=copy;}
        filter.sharedMesh=existing;
    }
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Apply")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Apply in edit mode");
        AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/CaoCao_EnemiesHair.fbx");
        importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CaoCao_EnemiesHair.fbx");
        var scalp=MaterialFor("Scalp");var knot=MaterialFor("Knot");
        string prefabPath="Assets/Art/Characters/VarcoV3/CaoCao/CaoCaoVisual.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try{Attach(prefab.transform,source,scalp,knot);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        int count=0;
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if(t.name!="CaoCao")continue;
            var visual=t.Find("VarcoVisual");if(!visual)continue;
            Attach(visual,source,scalp,knot);count++;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        File.WriteAllText("LocalArchive/CaoCaoHairEnemies20261016/unity-applied.txt","Applied prefab and scene instances: "+count);
    }
    [MenuItem("Tools/RedCliffs/CaoCao Hair/Inspect")]
    public static void Inspect()
    {
        var sb = new StringBuilder();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "CaoCao") continue;
            foreach(var child in t.GetComponentsInChildren<Transform>(true))
            {
                sb.AppendLine(child.name + " local=" + child.localPosition + " rot=" + child.localEulerAngles + " scale=" + child.localScale);
                var mf=child.GetComponent<MeshFilter>();
                if(mf && mf.sharedMesh) sb.AppendLine(" mesh="+AssetDatabase.GetAssetPath(mf.sharedMesh)+" bounds="+mf.sharedMesh.bounds);
            }
        }
        Directory.CreateDirectory("LocalArchive/CaoCaoHairEnemies20261016");
        File.WriteAllText("LocalArchive/CaoCaoHairEnemies20261016/unity-inspection.txt",sb.ToString());
        Debug.Log("CaoCao hair inspection completed");
    }
}
