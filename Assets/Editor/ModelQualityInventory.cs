using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class ModelQualityInventory
    {
        [Serializable] public class Surface {public string color,normal;public Color tint;public float bump,roughness,metallic;public Vector2 tiling,offset;public int[] triangles;}
        [Serializable] public class Geometry {public string key,path,meshName;public bool environment;public Vector3[] vertices,normals;public Vector2[] uv;public Surface[] surfaces;public float[] toAuthoring;}
        [MenuItem("Tools/Model Quality/Export Generated Textures")]
        public static void ExportGeneratedTextures()
        {
            const string folder="ArtSource/ModelQuality20261014/Textures";Directory.CreateDirectory(folder);
            foreach(var file in Directory.GetFiles("ArtSource/ModelQuality20261014/Input","*.json"))
            foreach(var path in JsonUtility.FromJson<Geometry>(File.ReadAllText(file)).surfaces.SelectMany(s=>new[]{s.color,s.normal}).Where(p=>!string.IsNullOrEmpty(p)&&p.EndsWith(".asset")).Distinct())
            {
                string output=folder+"/"+Path.GetFileNameWithoutExtension(path)+".png";if(File.Exists(output))continue;
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture)throw new Exception("Source texture missing: "+path);
                var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;Texture2D copy=null;
                try{Graphics.Blit(texture,rt);RenderTexture.active=rt;copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();File.WriteAllBytes(output,copy.EncodeToPNG());}
                finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(copy);}
            }
        }
        [MenuItem("Tools/Model Quality/Export Environment Sources")]
        public static void ExportEnvironment()
        {
            if(Application.isPlaying)throw new Exception("Stop Play before export");
            string folder="ArtSource/ModelQuality20261014/Input";Directory.CreateDirectory(folder);
            foreach(var r in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
            {
                var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;var mesh=filter.sharedMesh;
                string path=AssetDatabase.GetAssetPath(mesh);
                if(!path.StartsWith("Assets/Art/")||path.Contains("Characters/")||path.Contains("ModelQuality20261014/")||path.Contains("VarcoDetailBaked/"))continue;
                string key="Env_"+Path.GetFileNameWithoutExtension(path)+"_"+mesh.name;
                foreach(char c in Path.GetInvalidFileNameChars())key=key.Replace(c,'_');
                string file=folder+"/"+key+".json";if(File.Exists(file))continue;
                var surfaces=Enumerable.Range(0,mesh.subMeshCount).Select(i=>{
                    var m=r.sharedMaterials[Mathf.Min(i,r.sharedMaterials.Length-1)];
                    return new Surface{color=m.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")):"",normal=m.HasProperty("_BumpMap")?AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")):"",tint=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white,bump=m.HasProperty("_BumpScale")?m.GetFloat("_BumpScale"):0,roughness=m.HasProperty("_Smoothness")?1-m.GetFloat("_Smoothness"):.8f,metallic=m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):0,tiling=m.HasProperty("_BaseMap")?m.GetTextureScale("_BaseMap"):Vector2.one,offset=m.HasProperty("_BaseMap")?m.GetTextureOffset("_BaseMap"):Vector2.zero,triangles=mesh.GetTriangles(i)};
                }).ToArray();
                var scale=r.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
                var transform=Matrix4x4.TRS(Vector3.zero,r.transform.rotation,scale);
                File.WriteAllText(file,JsonUtility.ToJson(new Geometry{key=key,path=path,meshName=mesh.name,environment=true,vertices=mesh.vertices,normals=mesh.normals,uv=mesh.uv,surfaces=surfaces,toAuthoring=Enumerable.Range(0,16).Select(i=>transform[i/4,i%4]).ToArray()}));
            }
        }
        [MenuItem("Tools/Model Quality/Export Character Sources")]
        public static void ExportCharacters()
        {
            if(Application.isPlaying)throw new Exception("Stop Play before authoring export");
            string folder="ArtSource/ModelQuality20261014/Input";Directory.CreateDirectory(folder);
            var renderers=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true));
            foreach(var r in renderers.Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
            {
                var filter=r.GetComponent<MeshFilter>();
                var mesh=r is SkinnedMeshRenderer skin?skin.sharedMesh:(filter?filter.sharedMesh:null);if(!mesh)continue;
                string path=AssetDatabase.GetAssetPath(mesh);
                if(!path.StartsWith("Assets/Art/Characters/RefinedHands/")&&!(path.StartsWith("Assets/Art/Characters/RoleActivities/")&&path.EndsWith("_HeldRecord.asset")))continue;
                string key=Path.GetFileNameWithoutExtension(path),file=folder+"/"+key+".json";
                if(File.Exists(file))continue;
                var surfaces=Enumerable.Range(0,mesh.subMeshCount).Select(i=>{
                    var m=r.sharedMaterials[Mathf.Min(i,r.sharedMaterials.Length-1)];
                    return new Surface{color=AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")),normal=AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")),tint=m.GetColor("_BaseColor"),bump=m.GetFloat("_BumpScale"),roughness=1-m.GetFloat("_Smoothness"),triangles=mesh.GetTriangles(i)};
                }).ToArray();
                var actor=r.GetComponentInParent<VarcoIdleMotion>();
                if(!actor)throw new Exception("Actor root missing: "+key);
                var transform=actor.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                var matrix=Enumerable.Range(0,16).Select(i=>transform[i/4,i%4]).ToArray();
                File.WriteAllText(file,JsonUtility.ToJson(new Geometry{key=key,path=path,vertices=mesh.vertices,normals=mesh.normals,uv=mesh.uv,surfaces=surfaces,toAuthoring=matrix}));
            }
        }
        [Serializable] public class Entry { public string objectPath,meshPath,meshName; public bool active,skinned; public int vertices; public string[] materials; }
        [Serializable] public class Report { public string scene,time; public Entry[] entries; }
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        [MenuItem("Tools/Model Quality/Inventory")]
        public static void Inspect()
        {
            var scene=SceneManager.GetActiveScene();
            var entries=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Renderer>(true))
                .Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).Select(r=>{
                    var filter=r.GetComponent<MeshFilter>();
                    var mesh=r is SkinnedMeshRenderer s?s.sharedMesh:(filter?filter.sharedMesh:null);
                    return new Entry{objectPath=PathOf(r.transform),meshPath=AssetDatabase.GetAssetPath(mesh),meshName=mesh?mesh.name:"MISSING",active=r.enabled&&r.gameObject.activeInHierarchy,skinned=r is SkinnedMeshRenderer,vertices=mesh?mesh.vertexCount:0,materials=r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray()};
                }).Where(e=>e.meshName!="MISSING").ToArray();
            Directory.CreateDirectory("Logs/ModelQuality");File.WriteAllText("Logs/ModelQuality/inventory.json",JsonUtility.ToJson(new Report{scene=scene.path,time=DateTime.Now.ToString("O"),entries=entries},true));
        }
    }
}
