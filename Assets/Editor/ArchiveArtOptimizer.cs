using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using RedCliffMystery.Investigation;

public static class ArchiveArtOptimizer
{
    const string Folder="Assets/Resources/ArchiveArt";
    const string Log="Logs/ArchiveArt";
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Saved Investigation scene required");
        Directory.CreateDirectory(Log);File.Copy(scene.path,Log+"/before-"+DateTime.Now.ToString("HHmmss")+".unity");
        var report=new List<string>();long before=0,after=0;
        foreach(string path in Directory.GetFiles(Folder,"*.png")){
            string asset=path.Replace('\\','/');var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            before+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            var importer=(TextureImporter)AssetImporter.GetAtPath(asset);importer.textureType=TextureImporterType.Default;
            importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.npotScale=asset.Contains("atlas")?TextureImporterNPOTScale.None:TextureImporterNPOTScale.ToNearest;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=asset.Contains("atlas")?2048:1024;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var platform=importer.GetPlatformTextureSettings("Standalone");platform.name="Standalone";platform.overridden=true;
            platform.maxTextureSize=importer.maxTextureSize;platform.format=TextureImporterFormat.DXT1;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);after+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            report.Add(Path.GetFileName(asset)+" "+texture.width+"x"+texture.height+" "+texture.format+" bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture));
        }
        report.Add("Texture loaded bytes before="+before+" after="+after);
        var river=UnityEngine.Object.FindAnyObjectByType<NavalRiverAtmosphere>();
        if(river){
            int oldCount=river.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled);
            foreach(var boat in river.boats){
                if(boat.Find("Combined0"))continue;
                var groups=boat.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).GroupBy(r=>r.sharedMaterial).ToArray();int i=0;
                foreach(var group in groups){
                    var parts=group.ToArray();var combine=parts.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=boat.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
                    var mesh=new Mesh{name=boat.name+"Combined"+i};mesh.CombineMeshes(combine,true,true);
                    AssetDatabase.CreateAsset(mesh,"Assets/Art/Environment/NavalRiver/"+mesh.name+".asset");
                    var go=new GameObject("Combined"+i++,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(boat,false);
                    go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    foreach(var part in parts){Undo.RecordObject(part,"Combine boat rendering");part.enabled=false;}
                }
            }
            report.Add("River active renderers before="+oldCount+" after="+river.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled));
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Log+"/optimization.txt",report);
    }
    public static void Validate()
    {
        Directory.CreateDirectory(Log);var results=new List<string>();
        foreach(string name in new[]{"ending-True","ending-Normal","ending-Bad","evidence-atlas"}){
            string path=Folder+"/"+name+".png";var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool valid=t&&importer&&!importer.isReadable&&!importer.mipmapEnabled&&t.format==TextureFormat.DXT1;
            results.Add((valid?"PASS ":"FAIL ")+name+" compressed, GPU-only and no mipmaps");
        }
        var river=UnityEngine.Object.FindAnyObjectByType<NavalRiverAtmosphere>();
        results.Add((river&&river.boats.All(b=>b.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==3)?"PASS ":"FAIL ")+"three renderers per boat");
        int fail=results.Count(s=>s.StartsWith("FAIL"));results.Add("COMPLETE failures="+fail);File.WriteAllLines(Log+"/checks.txt",results);
        if(fail>0)throw new Exception("Archive art validation failed");
    }
}
