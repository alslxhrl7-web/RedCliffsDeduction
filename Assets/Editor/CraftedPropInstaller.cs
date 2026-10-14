using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class CraftedPropInstaller
    {
        const string Root="Assets/Art/Environment/VarcoDetailBaked/";
        static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        internal static Material PrepareBakedAsset(string name)
        {
            var model=(ModelImporter)AssetImporter.GetAtPath(Root+name+".fbx");
            if(model==null)throw new Exception("Baked model missing");
            model.importAnimation=false;model.addCollider=false;model.bakeAxisConversion=true;model.materialImportMode=ModelImporterMaterialImportMode.None;model.SaveAndReimport();
            foreach(var suffix in new[]{"BaseColor","Normal","AO","MetalSmooth"})
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(Root+name+"_"+suffix+".png");
                if(t==null)throw new Exception("Baked map missing: "+suffix);
                t.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.sRGBTexture=suffix=="BaseColor";t.maxTextureSize=1024;t.mipmapEnabled=true;t.isReadable=false;
                t.alphaSource=TextureImporterAlphaSource.FromInput;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+name+".mat");
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Root+name+".mat");}
            string[] slots={"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"};string[] maps={"BaseColor","Normal","AO","MetalSmooth"};
            for(int i=0;i<4;i++)mat.SetTexture(slots[i],AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+"_"+maps[i]+".png"));
            mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");
            mat.SetFloat("_Smoothness",1);mat.SetFloat("_BumpScale",.45f);mat.SetFloat("_OcclusionStrength",.35f);mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);
            mat.enableInstancing=true;
            return mat;
        }
        [MenuItem("Tools/Camp Environment/Apply Crafted Standard")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation scene required outside Play.");
            // Preserve unsaved scene edits as a copy before changing any visual.
            Directory.CreateDirectory("Assets/Logs/CraftedStandardReapply");
            if(!EditorSceneManager.SaveScene(scene,"Assets/Logs/CraftedStandardReapply/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))
                throw new Exception("Could not back up current scene edits");
            var all=All();var target=all.Single(x=>x.name=="CampStandard" && x.parent && x.parent.name=="NavalCamp");
            int colliders=all.Sum(x=>x.GetComponents<Collider>().Length);
            Directory.CreateDirectory("Logs/VarcoDetailReview20261014");
            File.Copy(scene.path,"Logs/VarcoDetailReview20261014/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            var mat=PrepareBakedAsset("CampStandard");
            var replacement=target.Find("CraftedStandard");
            if(!replacement){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"CampStandard.fbx"));Undo.RegisterCreatedObjectUndo(go,"Refine standard");replacement=go.transform;replacement.SetParent(target,false);replacement.name="CraftedStandard";}
            // Anchor the upright on the original pole, keeping existing collision.
            replacement.localPosition=new Vector3(.732f,0,0);replacement.localRotation=Quaternion.identity;replacement.localScale=Vector3.one*1.2f;
            foreach(var r in replacement.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            foreach(Transform child in target)
                if(child!=replacement && new[]{"Foot","Pole","Crossbar","Cloth","Stripe"}.Contains(child.name))
                    foreach(var r in child.GetComponentsInChildren<Renderer>()){Undo.RecordObject(r,"Replace standard visuals");r.enabled=false;}
            if(All().Sum(x=>x.GetComponents<Collider>().Length)!=colliders)throw new Exception("Collision changed");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/VarcoDetailReview20261014/unity-checks.txt","time="+DateTime.Now.ToString("O")+"; COMPLETE failures=0; maps=4; colliders unchanged="+colliders+"; instances="+target.Cast<Transform>().Count(x=>x.name=="CraftedStandard"));
        }
    }
}
