using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class ModelQualityInstaller
    {
        [MenuItem("Tools/Model Quality/Review Environment Models")]
        public static void ReviewEnvironment()
        {
            var seen=new System.Collections.Generic.HashSet<string>();
            foreach(var r in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
            {
                var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                string path=AssetDatabase.GetAssetPath(filter.sharedMesh);if(!path.StartsWith("Assets/Art/ModelQuality20261014/Env_")||!seen.Add(path))continue;
                var b=r.bounds;float size=Mathf.Max(b.size.x,b.size.y,b.size.z);var go=new GameObject("TemporaryPropReview");var cam=go.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=size*.7f;cam.nearClipPlane=.005f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.09f,.09f);
                cam.useOcclusionCulling=false;
                var others=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(other=>other!=r&&!other.forceRenderingOff).ToArray();
                foreach(var other in others)other.forceRenderingOff=true;
                cam.transform.position=b.center+new Vector3(size*1.2f,size*.9f,size*1.8f);cam.transform.LookAt(b.center);
                var rt=new RenderTexture(900,700,24);var previous=RenderTexture.active;Texture2D image=null;
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image=new Texture2D(900,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,700),0,0);image.Apply();File.WriteAllBytes("Logs/ModelQuality/Unity-"+Path.GetFileNameWithoutExtension(path)+".png",image.EncodeToPNG());}
                finally{foreach(var other in others)if(other)other.forceRenderingOff=false;RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        [MenuItem("Tools/Model Quality/Validate Applied Models")]
        public static void Validate()
        {
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();var lines=new System.Collections.Generic.List<string>();int failures=0;
            foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
            {
                var f=r.GetComponent<MeshFilter>();var mesh=r is SkinnedMeshRenderer s?s.sharedMesh:(f?f.sharedMesh:null);if(!mesh)continue;
                string path=AssetDatabase.GetAssetPath(mesh);if(!path.StartsWith("Assets/Art/ModelQuality20261014/"))continue;
                var d=JsonUtility.FromJson<Geometry>(File.ReadAllText(Path.GetDirectoryName(path)+"/geometry.json"));
                bool good=mesh.vertexCount>0&&mesh.triangles.Length>0&&mesh.subMeshCount==r.sharedMaterials.Length;
                if(!d.environment){var source=AssetDatabase.LoadAssetAtPath<Mesh>(d.path);good&=source&&source.vertexCount==mesh.vertexCount&&source.blendShapeCount==mesh.blendShapeCount&&source.triangles.SequenceEqual(mesh.triangles);}
                foreach(var m in r.sharedMaterials)good&=m&&new[]{"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"}.All(slot=>m.GetTexture(slot));
                lines.Add((good?"PASS ":"FAIL ")+d.key+" vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3);if(!good)failures++;
            }
            lines.Add("colliders="+roots.Sum(g=>g.GetComponentsInChildren<Collider>(true).Length)+" targets="+roots.Sum(g=>g.GetComponentsInChildren<RedCliffMystery.Investigation.InvestigationTarget>(true).Length));
            lines.Add("COMPLETE failures="+failures);File.WriteAllLines("Logs/ModelQuality/checks.txt",lines);
            if(failures>0)throw new Exception("Model validation failed");
        }
        [MenuItem("Tools/Model Quality/Review Applied Models")]
        public static void Review()
        {
            foreach(var actor in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VarcoIdleMotion>(true)))
            {
                var rs=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                if(!rs.Any(r=>r.sharedMaterials.Any(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/Art/ModelQuality20261014/"))))continue;
                var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                var go=new GameObject("TemporaryModelReview");var cam=go.AddComponent<Camera>();cam.enabled=false;cam.fieldOfView=45;cam.nearClipPlane=.02f;
                cam.transform.position=b.center+actor.transform.TransformDirection(new Vector3(.35f,.1f,-1))*b.size.y*1.5f;cam.transform.LookAt(b.center);
                var rt=new RenderTexture(800,1000,24);var previous=RenderTexture.active;Texture2D image=null;
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image=new Texture2D(800,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,1000),0,0);image.Apply();File.WriteAllBytes("Logs/ModelQuality/Unity-"+actor.Profile+".png",image.EncodeToPNG());}
                finally{RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        [Serializable] class Geometry { public string key,path,meshName; public bool environment; public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; public int size,surface_count; }
        [MenuItem("Tools/Model Quality/Apply Reviewed Models")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation edit scene required");
            const string reviews="Logs/ModelQuality/reviewed.txt";
            if(!File.Exists(reviews))throw new Exception("Review each Blender result first");
            Directory.CreateDirectory("Assets/Logs/ModelQualityBackup");
            if(!EditorSceneManager.SaveScene(scene,"Assets/Logs/ModelQualityBackup/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
            AssetDatabase.Refresh();
            int count=0;
            var identities=new System.Collections.Generic.Dictionary<string,string>();
            foreach(var key in File.ReadAllLines(reviews).Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct())
            {
                string folder="Assets/Art/ModelQuality20261014/"+key+"/";
                if(!File.Exists("ArtSource/ModelQuality20261014/"+key+".blend"))throw new Exception("Missing editable master: "+key);
                var data=JsonUtility.FromJson<Geometry>(File.ReadAllText(folder+"geometry.json"));
                var source=data.environment?AssetDatabase.LoadAllAssetsAtPath(data.path).OfType<Mesh>().Single(m=>m.name==data.meshName):AssetDatabase.LoadAssetAtPath<Mesh>(data.path);
                if(!source || data.vertices.Length!=data.normals.Length || (!data.environment&&source.vertexCount!=data.vertices.Length))throw new Exception("Vertex correspondence failed: "+key);
                var original=source.vertices;var normals=source.normals;
                if(!data.environment&&data.vertices.Where((v,i)=>Vector3.Distance(v,original[i])>.001f).Any())throw new Exception("Unexpected displacement: "+key);
                if(!data.environment&&data.normals.Where((v,i)=>Vector3.Dot(v,normals[i])<0).Count()>source.vertexCount*.01f)throw new Exception("Normal direction failed: "+key);
                string meshPath=folder+key+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(!mesh){mesh=data.environment?new Mesh():UnityEngine.Object.Instantiate(source);mesh.name=key+"_Quality";if(data.environment)mesh.indexFormat=data.vertices.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;mesh.vertices=data.vertices;mesh.normals=data.normals;if(data.environment){mesh.uv=data.uv;mesh.triangles=data.triangles;}mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
                if(!data.environment&&mesh.triangles.Length!=source.triangles.Length){EditorUtility.CopySerialized(source,mesh);mesh.name=key+"_Quality";mesh.vertices=data.vertices;mesh.normals=data.normals;mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);}
                if(!data.environment&&(mesh.subMeshCount!=source.subMeshCount||mesh.blendShapeCount!=source.blendShapeCount||!mesh.triangles.SequenceEqual(source.triangles)))throw new Exception("Rig or topology changed: "+key);
                foreach(var prefix in data.surface_count>1?Enumerable.Range(0,data.surface_count).Select(i=>"Surface"+i+"_"):new[]{""})
                foreach(var map in new[]{"BaseColor","Normal","AO","MetalSmooth"})
                {
                    var importer=AssetImporter.GetAtPath(folder+prefix+map+".png") as TextureImporter;
                    if(!importer)throw new Exception("Missing map: "+key+"/"+map);
                    var expectedType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                    bool changed=importer.textureType!=expectedType||importer.sRGBTexture!=(map=="BaseColor")||importer.maxTextureSize!=data.size||!importer.mipmapEnabled||importer.alphaSource!=TextureImporterAlphaSource.FromInput||importer.textureCompression!=TextureImporterCompression.CompressedHQ||importer.isReadable;
                    if(!changed)continue;
                    importer.textureType=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                    importer.sRGBTexture=map=="BaseColor";importer.maxTextureSize=data.size;importer.mipmapEnabled=true;
                    importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                    importer.isReadable=false;importer.SaveAndReimport();
                }
                foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
                {
                    var filter=r.GetComponent<MeshFilter>();var skin=r as SkinnedMeshRenderer;
                    var current=skin?skin.sharedMesh:(filter?filter.sharedMesh:null);
                    if(current==mesh)continue;
                    if(current!=source)
                    {
                        var currentPath=AssetDatabase.GetAssetPath(current);
                        if(string.IsNullOrEmpty(currentPath)||!currentPath.StartsWith("Assets/Art/ModelQuality20261014/"))continue;
                        var previousData=Path.GetDirectoryName(currentPath)+"/geometry.json";
                        if(!File.Exists(previousData))continue;
                        if(!identities.TryGetValue(previousData,out var identity)){var previous=JsonUtility.FromJson<Geometry>(File.ReadAllText(previousData));identity=previous.path+"|"+previous.meshName;identities[previousData]=identity;}
                        if(identity!=data.path+"|"+data.meshName)continue;
                    }
                    var materials=data.environment?new[]{r.sharedMaterials[0]}:r.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        string matPath=folder+"Surface"+i+".mat";
                        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if(!mat){mat=new Material(materials[i]);AssetDatabase.CreateAsset(mat,matPath);}
                        string[] slots={"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"},maps={"BaseColor","Normal","AO","MetalSmooth"};
                        string mapPrefix=data.surface_count>1?"Surface"+i+"_":"";
                        for(int j=0;j<4;j++)mat.SetTexture(slots[j],AssetDatabase.LoadAssetAtPath<Texture2D>(folder+mapPrefix+maps[j]+".png"));
                        mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_BumpScale",1);mat.SetFloat("_Smoothness",1);mat.SetFloat("_OcclusionStrength",.25f);
                        if(data.environment){mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);}
                        mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");EditorUtility.SetDirty(mat);materials[i]=mat;
                    }
                    Undo.RecordObject(r,"Refine model surface");r.sharedMaterials=materials;
                    if(skin){Undo.RecordObject(skin,"Refine hand");skin.sharedMesh=mesh;}
                    else {Undo.RecordObject(filter,"Refine body");filter.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);}
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);count++;
                }
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/ModelQuality/applied.txt",DateTime.Now.ToString("O")+" applied="+count+"; originals preserved; runtime review pending");
        }
    }
}
