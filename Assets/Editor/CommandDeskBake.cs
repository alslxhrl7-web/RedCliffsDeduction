using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace RedCliffMystery.EditorTools
{
    public static class CommandDeskBake
    {
        public const string Output = "Assets/Art/Props/CommandDeskBaked";
        public const string Logs = "Logs/CommandDeskBaked";
        [MenuItem("Tools/Command Desk/Validate")]
        public static void Validate()
        {
            ProjectBuildChecks.ValidateSources();VarcoV3Installer.Validate();
            var scene=SceneManager.GetActiveScene();
            var desk=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="BakedCommandDesk");
            var renderers=desk.GetComponentsInChildren<Renderer>();
            if(renderers.Length!=1 || renderers[0].sharedMaterials.Length!=1)throw new Exception("Desk draw grouping changed");
            foreach(string slot in new[]{"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"})if(!renderers[0].sharedMaterial.GetTexture(slot))throw new Exception("Missing bake: "+slot);
            Capture("after");
            Capture("player-view",true);
            File.WriteAllText(Logs+"/validation.txt","PASS scene sources, 10 V3 characters, single desk mesh/material, all four baked textures.\nCOMPLETE failures=0\n");
        }
        [MenuItem("Tools/Command Desk/Prepare Play Checks")]
        public static void PreparePlay()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
            SessionState.SetString("DeskBake.PreviousSave",SessionState.GetString("RedCliffMystery.TestSaveDirectory",""));
            SessionState.SetString("RedCliffMystery.TestSaveDirectory",Path.GetFullPath("Temp/OrderChecks/Desk-"+DateTime.Now.ToString("yyyyMMddHHmmss")));
            EditorApplication.isPlaying=true;
        }
        [MenuItem("Tools/Command Desk/Run Play Checks")]
        public static void RunPlay() => OrderChapterChecks.RunPlay();
        [MenuItem("Tools/Command Desk/Finish Play Checks")]
        public static void FinishPlay()
        {
            EditorApplication.isPlaying=false;
            EditorApplication.update+=RestoreAfterPlay;
        }
        static void RestoreAfterPlay()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorApplication.update-=RestoreAfterPlay;
            var old=SessionState.GetString("DeskBake.PreviousSave","");
            if(old.Length==0)SessionState.EraseString("RedCliffMystery.TestSaveDirectory");else SessionState.SetString("RedCliffMystery.TestSaveDirectory",old);
            File.WriteAllText(Logs+"/save-restored.txt","Test save override restored: "+(old.Length==0?"none":old));
        }
        [MenuItem("Tools/Command Desk/Inspect")]
        public static void Inspect()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/Investigation.unity" || Application.isPlaying || scene.isDirty) throw new Exception("Open saved Investigation scene in Edit mode.");
            Directory.CreateDirectory(Logs);
            var report=new StringBuilder();
            foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
                if(r.bounds.Intersects(new Bounds(new Vector3(41.4f,1,-.35f),new Vector3(3.4f,1.8f,1.8f))))
                    report.AppendLine(PathOf(r.transform)+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" center="+r.bounds.center.ToString("F3")+" size="+r.bounds.size.ToString("F3")+" mesh="+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh)+" mats="+string.Join(",",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m))));
            File.WriteAllText(Logs+"/inventory.txt",report.ToString());
            Capture("before");
        }
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
        [MenuItem("Tools/Command Desk/Apply Baked Desk")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Investigation.unity") throw new Exception("Open saved Investigation scene in Edit mode.");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            if(all.Any(t=>t.name=="BakedCommandDesk")) throw new Exception("Baked desk already installed.");
            var desk=all.Single(t=>t.name=="CommandDesk"); var tent=desk.parent;
            var colliders=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Collider>(true).Length);
            Directory.CreateDirectory(Logs);File.Copy(scene.path,Logs+"/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            var importer=(ModelImporter)AssetImporter.GetAtPath(Output+"/CommandDeskBaked.fbx");
            if(importer==null) throw new Exception("Baked FBX missing");
            importer.importAnimation=false;importer.addCollider=false;importer.bakeAxisConversion=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            foreach(string name in new[]{"BaseColor","Normal","AO","MetalSmooth"}) {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Output+"/Desk_"+name+".png");
                ti.textureType=name=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=name=="BaseColor";ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.isReadable=false;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
            }
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Desk_BaseColor.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Desk_Normal.png"));mat.SetFloat("_BumpScale",.7f);mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Desk_AO.png"));mat.SetFloat("_OcclusionStrength",.45f);
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/Desk_MetalSmooth.png"));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(mat,Output+"/DeskSurface.mat");
            int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace command desk decoration");
            try {
                // Replace only desk visuals. Original collision and dialogue objects stay intact.
                foreach(var r in desk.GetComponentsInChildren<Renderer>(true)){Undo.RecordObject(r,"Hide old desk");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                foreach(Transform t in tent) if(t.name=="CampaignMap" || t.name=="MapMarker") { var r=t.GetComponent<Renderer>();Undo.RecordObject(r,"Hide old map");r.enabled=false; }
                // Tent decorations share meshes. Remove only triangles wholly inside the tabletop volume.
                var region=new Bounds(new Vector3(41.4f,1.24f,-.35f),new Vector3(2.97f,.69f,1.30f));
                int removed=0;
                foreach(var f in tent.Find("TentPolish").GetComponentsInChildren<MeshFilter>(true)) {
                    var original=f.sharedMesh;var verts=original.vertices;var indices=original.triangles;var keep=new List<int>();int cut=0;
                    for(int i=0;i<indices.Length;i+=3) {
                        if(region.Contains(f.transform.TransformPoint(verts[indices[i]])) && region.Contains(f.transform.TransformPoint(verts[indices[i+1]])) && region.Contains(f.transform.TransformPoint(verts[indices[i+2]]))) {cut++;continue;}
                        keep.Add(indices[i]);keep.Add(indices[i+1]);keep.Add(indices[i+2]);
                    }
                    if(cut==0)continue;
                    var copy=UnityEngine.Object.Instantiate(original);copy.name=original.name+"_WithoutDesk";copy.triangles=keep.ToArray();copy.RecalculateBounds();AssetDatabase.CreateAsset(copy,Output+"/"+copy.name+".asset");Undo.RecordObject(f,"Remove old tabletop decoration");f.sharedMesh=copy;removed+=cut;
                }
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/CommandDeskBaked.fbx");var go=(GameObject)PrefabUtility.InstantiatePrefab(model);Undo.RegisterCreatedObjectUndo(go,"Add baked desk");go.name="BakedCommandDesk";go.transform.SetParent(tent,false);go.transform.position=new Vector3(41.4f,0,-.35f);
                foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                var bounds=go.GetComponentInChildren<Renderer>().bounds;
                if(Mathf.Abs(bounds.size.x-2.9f)>.05f || bounds.size.y>1.4f || bounds.min.y<-.01f)throw new Exception("Unexpected FBX scale/orientation: "+bounds);
                var after=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Collider>(true).Length);
                if(colliders!=after)throw new Exception("Collision count changed");
                if(removed<100)throw new Exception("Old decoration was not removed");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
                File.WriteAllText(Logs+"/apply.txt","PASS saved Investigation; collider count unchanged="+after+"; replaced decoration triangles="+removed+"; new bounds="+bounds+"; baked textures and one mesh/material.\nCOMPLETE failures=0\n");
                Capture("after");
            } catch { Undo.RevertAllDownToGroup(undo);throw; }
        }
        public static void Capture(string name,bool playerHeight=false)
        {
            Directory.CreateDirectory(Logs);
            var tent=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="CommandDesk").root.gameObject;
            bool active=tent.activeSelf; tent.SetActive(true);
            var go=new GameObject("DeskReviewCamera"); var camera=go.AddComponent<Camera>();
            go.transform.position=playerHeight?new Vector3(41.4f,1.65f,-2.2f):new Vector3(41.4f,2.25f,-2.65f); go.transform.LookAt(new Vector3(41.4f,playerHeight?1.15f:.95f,-.25f)); camera.fieldOfView=59; camera.nearClipPlane=.05f;
            var rt=new RenderTexture(1280,900,24); var old=RenderTexture.active; camera.targetTexture=rt;
            try { camera.Render(); RenderTexture.active=rt; var image=new Texture2D(1280,900,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,900),0,0); image.Apply(); File.WriteAllBytes(Logs+"/"+name+".png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); }
            finally { RenderTexture.active=old; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go); tent.SetActive(active); }
        }
    }
}
