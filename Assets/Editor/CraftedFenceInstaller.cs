using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class CraftedFenceInstaller
    {
        const string Root="Assets/Art/Environment/VarcoDetailBaked/";
        static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        [MenuItem("Tools/Camp Environment/Capture Fence Close")]
        public static void CaptureClose()
        {
            var go=new GameObject("FenceReviewCamera");var camera=go.AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.05f;
            camera.transform.position=new Vector3(1.5f,1.7f,-29.1f);camera.transform.LookAt(new Vector3(1.5f,1f,-32.2f));
            var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;
            Texture2D image=null;
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
                Directory.CreateDirectory("Logs/VarcoFenceWork");
                File.WriteAllBytes("Logs/VarcoFenceWork/close.png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
        [MenuItem("Tools/Camp Environment/Apply Received Fence")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation Edit scene required");
            var all=All();var fence=all.Single(x=>x.name=="NorthFence" && x.parent && x.parent.name=="NavalCamp");
            int colliders=all.Sum(x=>x.GetComponents<Collider>().Length);
            int targets=all.Sum(x=>x.GetComponents<RedCliffMystery.Investigation.InvestigationTarget>().Length);
            var mat=CraftedPropInstaller.PrepareBakedAsset("CampFence");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"CampFence.fbx");
            var meshes=prefab.GetComponentsInChildren<MeshFilter>();
            if(meshes.Length!=1 || meshes[0].sharedMesh==null)throw new Exception("Expected one baked fence mesh");
            var bounds=meshes[0].sharedMesh.bounds;
            if(bounds.size.x<.1f || bounds.size.y<.1f)throw new Exception("Fence has invalid dimensions");
            Directory.CreateDirectory("Assets/Logs/ReceivedFence");
            if(!EditorSceneManager.SaveScene(scene,"Assets/Logs/ReceivedFence/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
            var root=fence.Find("RefinedFencePanels");
            if(!root){root=new GameObject("RefinedFencePanels").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Apply received fence");root.SetParent(fence,false);}
            for(int i=0;i<6;i++)
            {
                var panel=root.Find("Panel"+i);
                if(!panel){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(go,"Add fence panel");panel=go.transform;panel.SetParent(root,false);panel.name="Panel"+i;}
                panel.position=new Vector3(-7.25f+2.9f*i,0,-32.20f);panel.rotation=Quaternion.identity;
                panel.localScale=new Vector3(2.9f/bounds.size.x,1.9f/bounds.size.y,1);
                foreach(var renderer in panel.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            }
            foreach(Transform child in fence)
                if(child!=root)foreach(var renderer in child.GetComponentsInChildren<Renderer>()){Undo.RecordObject(renderer,"Replace primitive fence visual");renderer.enabled=false;}
            if(All().Sum(x=>x.GetComponents<Collider>().Length)!=colliders || All().Sum(x=>x.GetComponents<RedCliffMystery.Investigation.InvestigationTarget>().Length)!=targets)throw new Exception("Gameplay components changed");
            if(root.childCount!=6)throw new Exception("Unexpected duplicate fence panels");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/VarcoFenceWork");
            File.WriteAllText("Logs/VarcoFenceWork/unity-checks.txt",DateTime.Now.ToString("O")+" COMPLETE failures=0; panels=6; colliders="+colliders+"; targets="+targets+"; meshTriangles="+meshes[0].sharedMesh.triangles.Length/3);
        }
    }
}
