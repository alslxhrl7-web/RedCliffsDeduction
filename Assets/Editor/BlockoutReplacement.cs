using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class BlockoutReplacement
    {
        const string Source="ArtSource/BlockoutReplacement20261014";
        const string Output="Assets/Art/Environment/BlockoutReplacement20261014/";
        const string Log="Logs/BlockoutReplacement";
        [Serializable] public class Item { public string id,path,mesh,family,material; public Vector3 size,center,scale; public Color tint; }
        [Serializable] public class Catalog { public int colliders,targets; public Item[] items; }
        [Serializable] public class Geometry { public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; public string family; }
        static GameObject[] Roots()=>SceneManager.GetActiveScene().GetRootGameObjects();
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
        static void RequireEdit(){if(Application.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation edit scene required");}
        static string Family(string path)
        {
            string n=path.Split('/').Last();
            if(n=="Water")return "RiverSurface";
            if(n.StartsWith("FarShore"))return "DistantBank";
            if(n=="NearBank")return "RockyBank";
            if(n.Contains("Trail")||n=="WornPath")return "EarthPath";
            if(n=="AudienceCarpet")return "WovenMat";
            if(n=="TentFloor")return "FloorDeck";
            if(n.Contains("Wall"))return "PlankWall";
            if(n=="Ceiling")return "RoofLining";
            if(n.Contains("LanternBody")||n.EndsWith("Shade")||n=="LampGlow")return "Lantern";
            if(n=="PaperBundle"||n=="Records")return "PaperStack";
            if(n.StartsWith("RolledRecord"))return "ScrollRoll";
            if(n=="WoodenToken")return "WoodToken";
            if(n.StartsWith("Notch"))return "CarvedInlay";
            if(n=="CommandSeal")return "CommandSeal";
            if(n=="MissingDocumentTray")return "TrayBase";
            if(n.StartsWith("Tray"))return "TrayRim";
            if(n=="Cloth"||n=="EntranceBanner")return "Banner";
            if(n=="Stripe")return "BannerTrim";
            if(n=="Board"||n=="Signboard")return "SignBoard";
            if(n.StartsWith("Arrow"))return "CarvedInlay";
            if(n=="Foot")return "StoneFoot";
            if(n.Contains("Beam")||n.Contains("beam")||n.Contains("Rail")||n.Contains("Boundary")||n=="Crossbar"||n=="Lintel"||n.Contains("FenceBase"))return "TimberBeam";
            if(n.Contains("Post")||n.Contains("post")||n.Contains("Pole")||n=="Pole"||n=="Pillar"||n.Contains("Frame")||n=="DoorJamb")return "TimberPost";
            throw new Exception("Unclassified blockout: "+path);
        }
        [MenuItem("Tools/Blockout Replacement/Inventory")]
        public static void Inventory()
        {
            RequireEdit();Directory.CreateDirectory(Source);Directory.CreateDirectory(Log);
            var items=Roots().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).Select(r=>new{r,f=r.GetComponent<MeshFilter>()}).Where(a=>a.f&&a.f.sharedMesh&&AssetDatabase.GetAssetPath(a.f.sharedMesh)=="Library/unity default resources").Select(a=>new Item{id=GlobalObjectId.GetGlobalObjectIdSlow(a.r).ToString(),path=PathOf(a.r.transform),family=Family(PathOf(a.r.transform)),mesh=a.f.sharedMesh.name,size=a.f.sharedMesh.bounds.size,center=a.f.sharedMesh.bounds.center,scale=a.r.transform.lossyScale,material=AssetDatabase.GetAssetPath(a.r.sharedMaterial),tint=a.r.sharedMaterial.HasProperty("_BaseColor")?a.r.sharedMaterial.GetColor("_BaseColor"):Color.white}).ToArray();
            var c=new Catalog{items=items,colliders=Roots().Sum(g=>g.GetComponentsInChildren<Collider>(true).Length),targets=Roots().Sum(g=>g.GetComponentsInChildren<RedCliffMystery.Investigation.InvestigationTarget>(true).Length)};
            string file=Source+"/placements.json";if(File.Exists(file))throw new Exception("Initial inventory already preserved");File.WriteAllText(file,JsonUtility.ToJson(c,true));
            File.WriteAllLines(Log+"/families.txt",items.GroupBy(i=>i.family).Select(g=>g.Key+" "+g.Count()));
        }
        static Matrix4x4 Orientation(Item item)
        {
            if(item.family=="ScrollRoll")return Matrix4x4.Rotate(Quaternion.Euler(90,0,0));
            var d=Vector3.Scale(item.size,new Vector3(Mathf.Abs(item.scale.x),Mathf.Abs(item.scale.y),Mathf.Abs(item.scale.z)));
            if(item.family=="TimberBeam"||item.family=="TrayRim"||item.family=="CarvedInlay"||item.family=="BannerTrim")
                return Matrix4x4.Rotate(d.z>d.x&&d.z>d.y?Quaternion.Euler(0,90,0):d.y>d.x?Quaternion.Euler(0,0,90):Quaternion.identity);
            if(item.family=="PlankWall"||item.family=="Banner"||item.family=="SignBoard")return Matrix4x4.Rotate(d.x<d.z?Quaternion.Euler(0,90,0):Quaternion.identity);
            return Matrix4x4.identity;
        }
        [MenuItem("Tools/Blockout Replacement/Apply Reviewed")]
        public static void Apply()
        {
            RequireEdit();AssetDatabase.Refresh();var c=JsonUtility.FromJson<Catalog>(File.ReadAllText(Source+"/placements.json"));
            var reviewed=File.ReadAllLines(Log+"/reviewed.txt").ToHashSet();
            Directory.CreateDirectory("Assets/Logs/BlockoutBackup");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Logs/BlockoutBackup/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            int count=0;
            foreach(var group in c.items.Where(i=>reviewed.Contains(i.family)).GroupBy(i=>i.family))
            {
                string dir=Output+group.Key+"/";var d=JsonUtility.FromJson<Geometry>(File.ReadAllText(dir+"geometry.json"));
                if(!File.Exists(Source+"/"+group.Key+".blend"))throw new Exception("Missing Blender master");
                foreach(string map in new[]{"BaseColor","Normal","AO","MetalSmooth","Emission"})
                {
                    string p=dir+map+".png";if(!File.Exists(p))continue;var t=(TextureImporter)AssetImporter.GetAtPath(p);var type=map=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                    bool color=map=="BaseColor"||map=="Emission";
                    if(t.textureType==type&&t.sRGBTexture==color&&t.maxTextureSize==1024&&t.textureCompression==TextureImporterCompression.CompressedHQ)continue;
                    t.textureType=type;t.sRGBTexture=color;t.maxTextureSize=1024;t.mipmapEnabled=true;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
                }
                foreach(var item in group)
                {
                    if(!GlobalObjectId.TryParse(item.id,out var id))throw new Exception("Invalid original id");var r=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MeshRenderer;if(!r)throw new Exception("Original object missing: "+item.path);
                    var f=r.GetComponent<MeshFilter>();var rotation=Orientation(item);var vertices=d.vertices.Select(v=>rotation.MultiplyPoint3x4(v)).ToArray();var b=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)b.Encapsulate(v);
                    var targetSize=item.size;var targetCenter=item.center;
                    if(item.family=="ScrollRoll"){targetSize.x*=.45f;targetSize.y*=1.6f;targetCenter.y+=item.size.y*.3f;}
                    var fit=new Vector3(targetSize.x/b.size.x,targetSize.y/b.size.y,targetSize.z/b.size.z);
                    string variant=group.Key+"-"+Mathf.RoundToInt(rotation.m00)+"-"+Mathf.RoundToInt(rotation.m02)+"-"+Mathf.RoundToInt(rotation.m10)+"-"+item.mesh;
                    string meshPath=dir+variant+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(!mesh){mesh=new Mesh{name=variant};mesh.indexFormat=vertices.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;mesh.vertices=vertices.Select(v=>Vector3.Scale(v-b.center,fit)+targetCenter).ToArray();mesh.normals=d.normals.Select(n=>{var q=rotation.MultiplyVector(n);return new Vector3(q.x/fit.x,q.y/fit.y,q.z/fit.z).normalized;}).ToArray();mesh.uv=d.uv;mesh.triangles=d.triangles;mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
                    string materialPath=dir+Path.GetFileNameWithoutExtension(item.material)+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!mat){var old=AssetDatabase.LoadAssetAtPath<Material>(item.material);mat=new Material(old);AssetDatabase.CreateAsset(mat,materialPath);}
                    {
                        if(group.Key!="RiverSurface"){
                            mat.shader=Shader.Find("Universal Render Pipeline/Lit");
                            string[] maps={"BaseColor","Normal","AO","MetalSmooth"},slots={"_BaseMap","_BumpMap","_OcclusionMap","_MetallicGlossMap"};for(int j=0;j<4;j++)mat.SetTexture(slots[j],AssetDatabase.LoadAssetAtPath<Texture2D>(dir+maps[j]+".png"));
                            bool keepTint=group.Key=="Banner"||group.Key=="BannerTrim"||group.Key=="CarvedInlay"||group.Key=="WovenMat";mat.SetColor("_BaseColor",keepTint?item.tint:Color.white);mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);mat.SetFloat("_BumpScale",.65f);mat.SetFloat("_Smoothness",1);mat.SetFloat("_OcclusionStrength",.35f);mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_OCCLUSIONMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                            if(group.Key=="Lantern"){mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"Emission.png"));mat.SetColor("_EmissionColor",new Color(1,.57f,.2f)*1.2f);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");}else{mat.DisableKeyword("_EMISSION");if(mat.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",Color.black);}
                        }
                        EditorUtility.SetDirty(mat);
                    }
                    if(f.sharedMesh==mesh)continue;
                    Undo.RecordObject(f,"Replace primitive with crafted model");Undo.RecordObject(r,"Apply baked crafted material");f.sharedMesh=mesh;r.sharedMaterials=new[]{mat};
                    // Three tally marks now belong to the authored tablet itself.
                    if(item.path.Contains("PersonalToken/Notch"))r.enabled=false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(f);PrefabUtility.RecordPrefabInstancePropertyModifications(r);count++;
                }
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());File.WriteAllText(Log+"/applied.txt",DateTime.Now.ToString("O")+" replaced="+count);
        }
        [MenuItem("Tools/Blockout Replacement/Review Scene")]
        public static void Review()
        {
            RequireEdit();var c=JsonUtility.FromJson<Catalog>(File.ReadAllText(Source+"/placements.json"));
            var go=new GameObject("TemporaryArtReviewCamera");var camera=go.AddComponent<Camera>();camera.fieldOfView=65;camera.nearClipPlane=.03f;camera.farClipPlane=400;
            var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);var lines=new List<string>();
            try {
                camera.targetTexture=rt;
                foreach(var group in c.items.GroupBy(i=>i.family)) {
                    var item=group.First();GlobalObjectId.TryParse(item.id,out var id);var r=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MeshRenderer;var b=r.bounds;lines.Add(item.family+" "+item.path+" "+b);
                    float distance=Mathf.Clamp(Mathf.Max(b.size.x,b.size.y,b.size.z)*1.1f,.6f,25);
                    var direction=new Vector3(.3f,.25f,-1).normalized;
                    if(item.family=="PlankWall")direction=Vector3.back;
                    if(item.family=="EarthPath"||item.family=="FloorDeck"||item.family=="WovenMat"||item.family=="RiverSurface"||item.family=="RockyBank"||item.family=="DistantBank")direction=new Vector3(0,.6f,-1).normalized;
                    go.transform.position=b.center+direction*distance;go.transform.LookAt(b.center);
                    camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(Log+"/Scene-"+item.family+".png",image.EncodeToPNG());
                }
                File.WriteAllLines(Log+"/world-bounds.txt",lines);
            } finally {RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
        }
        [MenuItem("Tools/Blockout Replacement/Validate")]
        public static void Validate()
        {
            var c=JsonUtility.FromJson<Catalog>(File.ReadAllText(Source+"/placements.json"));int failures=0;var lines=new List<string>();
            foreach(var item in c.items){GlobalObjectId.TryParse(item.id,out var id);var r=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as MeshRenderer;var f=r?r.GetComponent<MeshFilter>():null;bool pass=f&&f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Output)&&f.sharedMesh.triangles.Length>0&&r.sharedMaterial;lines.Add((pass?"PASS ":"FAIL ")+item.path);if(!pass)failures++;}
            int remaining=Roots().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Count(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<MeshFilter>()&&AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)=="Library/unity default resources");
            int colliders=Roots().Sum(g=>g.GetComponentsInChildren<Collider>(true).Length),targets=Roots().Sum(g=>g.GetComponentsInChildren<RedCliffMystery.Investigation.InvestigationTarget>(true).Length);
            if(remaining>0||colliders!=c.colliders||targets!=c.targets)failures++;
            lines.Add("remainingVisiblePrimitives="+remaining+" colliders="+colliders+" targets="+targets);lines.Add("COMPLETE failures="+failures);File.WriteAllLines(Log+"/checks.txt",lines);if(failures>0)throw new Exception("Replacement validation failed");
        }
    }
}
