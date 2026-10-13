using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class CharacterDetailAuthoring
    {
        const string Source="ArtSource/CharacterDetail20261013";
        const string Output="Assets/Art/Characters/RoleActivities";
        [MenuItem("Tools/Character Detail/Apply Face Bakes")]
        public static void ApplyFaces()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before applying face bakes");
            const string folder="Assets/Art/Characters/CharacterDetails";
            Directory.CreateDirectory(folder);
            var actors=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VarcoIdleMotion>(true)).ToArray();
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name;
                File.Copy(Source+"/Output/"+name+"_BaseColor.png",folder+"/"+name+"_BaseColor.png",true);
            }
            AssetDatabase.Refresh();
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name,path=folder+"/"+name+"_BaseColor.png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture=true;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                var renderer=actor.GetComponentInChildren<MeshRenderer>(true);
                var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");
                if(mat==null){mat=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(mat,folder+"/"+name+".mat");}
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));EditorUtility.SetDirty(mat);
                Undo.RecordObject(renderer,"Apply face bake");renderer.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        [MenuItem("Tools/Character Detail/Apply")]
        public static void Apply()
        {
            RoleActivityAuthoring.ApplyFrom(Source);
            var actors=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VarcoIdleMotion>(true)).ToArray();
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name;
                File.Copy(Source+"/Output/"+name+"-detail.bytes",Output+"/"+name+"-detail.bytes",true);
            }
            AssetDatabase.Refresh();
            foreach(var actor in actors)
            {
                var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(Output+"/"+actor.transform.parent.name+"-detail.bytes");
                var clip=DetailMotionClip.Read(asset);
                if(clip.Profile!=actor.Profile || clip.VertexCount!=actor.GetComponentInChildren<MeshFilter>(true).sharedMesh.vertexCount)throw new Exception("Detail source mismatch");
                Undo.RecordObject(actor,"Apply character detail");actor.DetailMotion=asset;EditorUtility.SetDirty(actor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();RoleActivityAuthoring.Validate();
        }
    }
}
