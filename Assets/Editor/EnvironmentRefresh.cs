using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class EnvironmentRefresh
    {
        public const string Root="Assets/Art/Environment/CampRefreshBaked";
        const string Log="Logs/EnvironmentRefresh";
        static Transform[] All() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        [MenuItem("Tools/Camp Environment/Inspect")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Log);var report=new System.Text.StringBuilder();
            foreach(var t in All())
            {
                var r=t.GetComponent<Renderer>();
                if(r==null || !r.enabled || !t.gameObject.activeInHierarchy || t.GetComponentInParent<VarcoIdleMotion>()!=null)continue;
                
                    report.AppendLine(PathOf(t)+" position="+t.position+" bounds="+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
            }
            File.WriteAllText(Log+"/inventory.txt",report.ToString());
            File.WriteAllText(Log+"/roots.txt",string.Join("\n",All().Where(t=>t.parent==null || t.name.Contains("Tent") || t.name.Contains("Camp")).Select(t=>PathOf(t)+" "+t.position)));
        }
        static void RequireScene()
        {
            if(Application.isPlaying || SceneManager.GetActiveScene().isDirty || SceneManager.GetActiveScene().path!="Assets/Scenes/Investigation.unity")throw new InvalidOperationException("Saved Investigation scene required outside Play");
        }
        [MenuItem("Tools/Camp Environment/Capture Before")]
        public static void CaptureBefore()=>Capture("before");
        [MenuItem("Tools/Camp Environment/Capture After")]
        public static void CaptureAfter()=>Capture("after");
        static void Capture(string stage)
        {
            Directory.CreateDirectory(Log);
            var positions=new[]{new Vector3(0,1.7f,-17),new Vector3(-40,1.7f,-17.5f),new Vector3(40,1.7f,-3.8f)};
            var targets=new[]{new Vector3(0,1.3f,-28),new Vector3(-40,1.3f,-24.5f),new Vector3(40,1.3f,2)};
            for(int i=0;i<positions.Length;i++)
            {
                var go=new GameObject("EnvironmentReviewCamera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.useOcclusionCulling=false;
                camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Log+"/"+stage+"-"+i+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
                finally{RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        [MenuItem("Tools/Camp Environment/Fix Orientation")]
        public static void FixOrientation()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play first");
            foreach(var t in All().Where(t=>t.name=="FieldTent" && t.parent && t.parent.name=="BakedCampEnvironment"))
            {Undo.RecordObject(t,"Orient tent entrance");t.rotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        [MenuItem("Tools/Camp Environment/Soften Canvas")]
        public static void SoftenCanvas()
        {
            foreach(string name in new[]{"FieldTent","SupplyCanopy"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
                mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_BumpScale",.2f);
                mat.SetFloat("_SpecularHighlights",1);mat.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Camp Environment/Capture Playing Scene")]
        public static void CapturePlayingScene()
        {
            if(!Application.isPlaying || !SessionState.GetString("RedCliffMystery.TestSaveDirectory","").Contains("OrderChecks"))throw new Exception("Isolated Play required");
            var title=UnityEngine.Object.FindAnyObjectByType<RedCliffMystery.Investigation.InvestigationTitleScreen>();
            if(title)title.StartButton.onClick.Invoke();
            var session=UnityEngine.Object.FindAnyObjectByType<RedCliffMystery.Investigation.InvestigationSession>();
            session.CloseModal();session.enabled=false;
            session.StartCoroutine(CaptureSettled());
        }
        static System.Collections.IEnumerator CaptureSettled()
        { yield return new WaitForSeconds(1);Capture("final-play"); }
        [MenuItem("Tools/Camp Environment/Validate")]
        public static void Validate()
        {
            int failures=0;
            var root=All().Single(t=>t.name=="BakedCampEnvironment");
            foreach(var r in root.GetComponentsInChildren<Renderer>())
                if(r.sharedMaterial==null || r.GetComponent<MeshFilter>()?.sharedMesh==null)failures++;
            foreach(string name in new[]{"SupplyCrate","StoragePot","RiverRock","FieldTent","SupplyCanopy"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
                foreach(string slot in new[]{"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"})if(!mat.GetTexture(slot))failures++;
            }
            if(All().Sum(t=>t.GetComponents<Collider>().Length)!=663)failures++;
            Directory.CreateDirectory(Log);File.WriteAllText(Log+"/validation.txt","COMPLETE failures="+failures+"; environment renderers="+root.GetComponentsInChildren<Renderer>().Length);
            if(failures>0)throw new Exception("Environment validation failed");
        }
        [MenuItem("Tools/Camp Environment/Add Interior Fill")]
        public static void AddInteriorFill()
        {
            if(Application.isPlaying)throw new Exception("Edit mode required");
            var root=All().Single(t=>t.name=="BakedCampEnvironment");
            var points=new[]{new Vector3(4,2.05f,-27.9f),new Vector3(-4,2.05f,-27.9f),new Vector3(-40,2.5f,-23.5f)};
            for(int i=0;i<points.Length;i++)
            {
                string name="CanvasBounce"+i;var t=root.Find(name);
                if(!t){t=new GameObject(name,typeof(Light)).transform;t.SetParent(root);}
                t.position=points[i];var light=t.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.86f,.68f);light.intensity=1.6f;light.range=i==2?8:5.5f;light.shadows=LightShadows.None;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static Material BakedMaterial(string name)
        {
            foreach(string suffix in new[]{"BaseColor","Normal","AO","MetalSmooth"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+name+"_"+suffix+".png");
                importer.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=suffix=="BaseColor";importer.maxTextureSize=name.Contains("Tent")||name.Contains("Canopy")?2048:1024;
                importer.mipmapEnabled=true;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Root+"/"+name+".mat");}
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+"_BaseColor.png"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+"_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.65f);
            m.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+"_AO.png"));m.EnableKeyword("_OCCLUSIONMAP");m.SetFloat("_OcclusionStrength",.45f);
            m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+"_MetalSmooth.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);
            m.SetFloat("_Cull",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Place(string asset,Transform parent,Vector3 pos,float yaw=0,float scale=1)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+asset+".fbx"));
            Undo.RegisterCreatedObjectUndo(go,"Refine camp environment");go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;
            foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+asset+".mat");PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            return go;
        }
        static Material Tiled(string name,string source,Color tint)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Root+"/"+name+".mat");}
            string textures="Assets/Art/Environment/CommandTent/Textures/";
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+source+"_1K-JPG_Color.jpg"));m.SetColor("_BaseColor",tint);
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+source+"_1K-JPG_NormalGL.jpg"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.22f);m.SetFloat("_Smoothness",.035f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        [MenuItem("Tools/Camp Environment/Apply Baked Environment")]
        public static void Apply()
        {
            RequireScene();var all=All();if(all.Any(t=>t.name=="BakedCampEnvironment"))throw new InvalidOperationException("Already installed; adjust existing objects instead of duplicating");
            Directory.CreateDirectory(Log);File.Copy(SceneManager.GetActiveScene().path,Log+"/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            int colliderCount=all.Sum(t=>t.GetComponents<Collider>().Length);
            foreach(string name in new[]{"SupplyCrate","StoragePot","RiverRock","FieldTent","SupplyCanopy"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/"+name+".fbx");if(importer==null)throw new Exception("Missing "+name);
                importer.importAnimation=false;importer.addCollider=false;importer.bakeAxisConversion=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();BakedMaterial(name);
            }
            var ground=Tiled("CampEarth","Ground054",new Color(.83f,.78f,.66f));var timber=Tiled("CampTimber","Wood049",new Color(.65f,.50f,.32f));
            int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Refine camp environment");
            try
            {
                var root=new GameObject("BakedCampEnvironment").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Refine camp environment");
                foreach(var tent in all.Where(t=>t.name=="CaimaoTent"||t.name=="ZhangyunTent"))
                {
                    foreach(Transform child in tent)
                        if(new[]{"BackCanvas","LeftCanvas","RightCanvas","LeftRoof","RightRoof","Ridge","FrontPost-1","FrontPost1","FrontTrim-1","FrontTrim1"}.Contains(child.name))
                        {var r=child.GetComponent<Renderer>();if(r){Undo.RecordObject(r,"Replace tent surface");r.enabled=false;}}
                    var replacement=Place("FieldTent",root,tent.position);replacement.transform.rotation=tent.rotation*Quaternion.Euler(0,180,0);
                }
                var supply=all.Single(t=>t.name=="SupplyTent");
                foreach(var r in supply.GetComponentsInChildren<Renderer>(true))
                {
                    if(r.GetComponentInParent<VarcoIdleMotion>()!=null)continue;
                    if(r.name=="SupplyCanopy" || r.name=="SupplyPost" || (r.bounds.center.y>2.5f && r.bounds.size.y<1 && Mathf.Abs(r.bounds.center.x+40)<4 && Mathf.Abs(r.bounds.center.z+23.5f)<3))
                    {Undo.RecordObject(r,"Replace supply canopy");r.enabled=false;}
                }
                Place("SupplyCanopy",root,new Vector3(-40,0,-23.5f));
                int changed=0;
                foreach(var t in all)
                {
                    var r=t.GetComponent<Renderer>();var f=t.GetComponent<MeshFilter>();if(!r||!f||!r.enabled)continue;
                    bool floor=(t.name.Contains("Ground")||t.name=="Floor"||t.name=="FloorMat"||t.name=="ApproachPath"||t.name=="CrossPath") && r.bounds.size.y<.5f && !PathOf(t).Contains("NavalRiverBackdrop");
                    if(floor)
                    {
                        Undo.RecordObject(f,"Ground UVs");Undo.RecordObject(r,"Ground material");var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);
                        mesh.uv=mesh.vertices.Select(v=>{var p=f.transform.TransformPoint(v);return new Vector2(p.x*.35f,p.z*.35f);}).ToArray();mesh.RecalculateTangents();
                        AssetDatabase.CreateAsset(mesh,Root+"/Ground_"+(changed++)+".asset");f.sharedMesh=mesh;r.sharedMaterial=ground;
                    }
                    else if(t.name.Contains("Fence")||t.name=="RoofBeam") {Undo.RecordObject(r,"Timber surface");r.sharedMaterial=timber;}
                }
                // Supply clusters are placed against back/side boundaries, outside NPC and route space.
                Place("SupplyCrate",root,new Vector3(-46.7f,0,-29.7f),12,1.3f);
                Place("SupplyCrate",root,new Vector3(-45.55f,0,-29.6f),-8,1.3f);
                Place("SupplyCrate",root,new Vector3(-46.2f,.42f,-29.7f),3,1.1f);
                Place("StoragePot",root,new Vector3(-44.4f,0,-29.65f),22,1.7f);
                Place("StoragePot",root,new Vector3(-43.5f,0,-29.7f),-14,1.25f);
                Place("SupplyCrate",root,new Vector3(7.3f,0,-29.8f),-7,1.2f);
                Place("StoragePot",root,new Vector3(7.55f,0,-28.85f),5,1.45f);
                Place("SupplyCrate",root,new Vector3(35.1f,0,3.35f),4,1.25f);
                Place("StoragePot",root,new Vector3(36.25f,0,3.2f),8,1.35f);
                for(int i=0;i<12;i++)Place("RiverRock",root,new Vector3(-45+i*8,-.07f,-33.2f+(i%3)*.13f),i*47,2.5f+(i%3)*.55f);
                for(int i=0;i<6;i++)Place("RiverRock",root,new Vector3(-29+i*12,-.045f,-21.65f),i*59,1.4f+(i%2)*.35f);
                // Visual land below the existing walking floors; no new gameplay collision.
                var land=new Mesh {name="Camp surrounding earth"};land.vertices=new[]{new Vector3(-62,-.08f,-33),new Vector3(-62,-.08f,15),new Vector3(62,-.08f,15),new Vector3(62,-.08f,-33)};
                land.triangles=new[]{0,1,2,0,2,3};land.uv=land.vertices.Select(v=>new Vector2(v.x*.35f,v.z*.35f)).ToArray();land.RecalculateNormals();land.RecalculateTangents();AssetDatabase.CreateAsset(land,Root+"/SurroundingEarth.asset");
                var terrain=new GameObject("SurroundingEarth",typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(terrain,"Add surrounding ground");terrain.transform.SetParent(root,false);terrain.GetComponent<MeshFilter>().sharedMesh=land;terrain.GetComponent<Renderer>().sharedMaterial=ground;
                if(All().Sum(t=>t.GetComponents<Collider>().Length)!=colliderCount)throw new Exception("Colliders changed");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                File.WriteAllText(Log+"/apply.txt","COMPLETE failures=0; collider count unchanged="+colliderCount+"; ground surfaces="+changed);
                Undo.CollapseUndoOperations(undo);
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
    }
}





