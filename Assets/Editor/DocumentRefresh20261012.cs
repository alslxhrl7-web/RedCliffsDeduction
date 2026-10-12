using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace RedCliffMystery.EditorTools
{
    public static class DocumentRefresh20261012
    {
        const string Folder="Assets/Art/Evidence/VarcoRefined20261012";
        const string Logs="Logs/DocumentRefresh20261012";
        static GameObject Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name==name).gameObject;
        static void Guard() { var s=SceneManager.GetActiveScene(); if(Application.isPlaying||s.isDirty||s.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Saved Investigation scene outside Play required"); }
        static Bounds BoundsOf(GameObject go) { var rr=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b; }

        [MenuItem("적벽추리/문서 개선/군령 모델 적용")]
        public static void Apply()
        {
            Guard();var root=Find("MaintenanceOrder");
            if(root.transform.Find("RefinedDocument20261012")) throw new InvalidOperationException("Document already applied");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/MaintenanceOrderRefined.fbx");
            if(!model)throw new InvalidOperationException("Refined FBX missing");
            var old=root.GetComponent<Renderer>();var desired=old.bounds;
            desired.size=new Vector3(desired.size.x,.026f,desired.size.z);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/OrderBambooMatte.mat");
            if(!mat) {
                mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="OrderBambooMatte";
                mat.SetColor("_BaseColor",new Color(.55f,.40f,.24f));mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.1f);
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/CommandTent/Textures/Wood049_1K-JPG_Color.jpg"));
                AssetDatabase.CreateAsset(mat,Folder+"/OrderBambooMatte.mat");
            }
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try {
                var wrapper=new GameObject("RefinedDocument20261012");Undo.RegisterCreatedObjectUndo(wrapper,"Add refined military order");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(model);Undo.RegisterCreatedObjectUndo(go,"Add document mesh");go.transform.SetParent(wrapper.transform,false);
                foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                var b=BoundsOf(go);if(b.size.y>.1f)throw new InvalidOperationException("Unexpected FBX up axis");
                wrapper.transform.localScale=new Vector3(desired.size.x/b.size.x,desired.size.y/b.size.y,desired.size.z/b.size.z);
                wrapper.transform.position+=desired.center-BoundsOf(go).center;wrapper.transform.SetParent(root.transform,true);
                Undo.RecordObject(old,"Hide original document surface");old.enabled=false;
                AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed");
                Undo.CollapseUndoOperations(group);Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/apply.txt","Applied VARCO military order; original renderer disabled; clue text, seal, target and collider retained.");
            } catch {Undo.RevertAllDownToGroup(group);throw;}
        }

        [MenuItem("적벽추리/문서 개선/군령 저장 검사")]
        public static void Validate()
        {
            Guard();EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);
            var root=Find("MaintenanceOrder");var model=root.transform.Find("RefinedDocument20261012");
            if(!model||root.GetComponent<Renderer>().enabled)throw new InvalidOperationException("Replacement missing");
            var collider=root.GetComponent<Collider>();
            if(!collider||!collider.enabled||!root.GetComponentInParent<RedCliffMystery.Investigation.InvestigationTarget>())throw new InvalidOperationException("Interaction missing");
            if(model.GetComponentsInChildren<Renderer>().Any(r=>!r.enabled||!r.sharedMaterial||r.sharedMaterial.shader.name!="Universal Render Pipeline/Lit"))throw new InvalidOperationException("Material missing");
            Physics.SyncTransforms();if(!collider.Raycast(new Ray(collider.bounds.center+Vector3.up*2,Vector3.down),out var hit,3))throw new InvalidOperationException("Document ray missed");
            var table=Find("MaintenanceOrderTable");int writing=table.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Writing"));
            if(writing!=4||!table.transform.Find("CommandSeal"))throw new InvalidOperationException("Existing markings missing");
            Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/validation.txt","PASS saved scene reopened; URP material; collider ray; target; four writing overlays and seal retained. "+DateTime.Now.ToString("s"));
            Capture(table);
        }
        static void Capture(GameObject source)
        {
            var preview=EditorSceneManager.NewPreviewScene();var rt=new RenderTexture(960,720,24);var old=RenderTexture.active;Texture2D pixels=null;Camera camera=null;
            try {
                var copy=UnityEngine.Object.Instantiate(source);copy.SetActive(true);SceneManager.MoveGameObjectToScene(copy,preview);
                var go=new GameObject("Document inspection");SceneManager.MoveGameObjectToScene(go,preview);camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=preview;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.16f);camera.fieldOfView=37;camera.nearClipPlane=.03f;camera.targetTexture=rt;camera.aspect=4f/3;
                var center=copy.GetComponentsInChildren<Transform>(true).First(t=>t.name=="MaintenanceOrder").position;camera.transform.position=center+new Vector3(.6f,1.4f,-1.25f);camera.transform.LookAt(center);go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                var lamp=new GameObject("Preview light");SceneManager.MoveGameObjectToScene(lamp,preview);var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2.2f;lamp.transform.rotation=Quaternion.Euler(45,-35,0);
                camera.Render();camera.Render();RenderTexture.active=rt;pixels=new Texture2D(960,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();File.WriteAllBytes(Logs+"/military-order.png",pixels.EncodeToPNG());
            } finally {if(camera)camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(pixels)UnityEngine.Object.DestroyImmediate(pixels);EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
}
