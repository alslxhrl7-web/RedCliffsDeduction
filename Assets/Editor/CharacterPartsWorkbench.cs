using System;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;
namespace RedCliffMystery.EditorTools {
public static partial class CharacterPartsWorkbench {
static string Dir=>Path.Combine(Application.dataPath,"../LocalArchive/CharacterParts20261018");
[MenuItem("Tools/Character Parts/Inventory")]
public static void Inventory(){var b=new StringBuilder();b.AppendLine("Scene="+SceneManager.GetActiveScene().path);foreach(var mo in UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include,FindObjectsSortMode.None)){b.AppendLine("CHARACTER "+mo.transform.parent.name+" visual="+mo.name+" profile="+mo.Profile+" mouth="+mo.MouthHeightOverride);foreach(var f in mo.GetComponentsInChildren<MeshFilter>(true)){b.AppendLine("MESH "+f.name+" asset="+AssetDatabase.GetAssetPath(f.sharedMesh)+" bounds="+f.sharedMesh.bounds+" vertices="+f.sharedMesh.vertexCount+" shapes="+f.sharedMesh.blendShapeCount+" active="+f.gameObject.activeInHierarchy);var re=f.GetComponent<Renderer>();foreach(var m in re.sharedMaterials){if(m==null)continue;b.AppendLine("MAT "+m.name+" shader="+m.shader.name);foreach(var prop in m.GetTexturePropertyNames()){var t=m.GetTexture(prop);if(t!=null)b.AppendLine("TEX "+prop+" "+AssetDatabase.GetAssetPath(t));}}}}Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"live-inventory.txt"),b.ToString());}
[Serializable] public class SurfaceData { public string character,sourceMesh,baseMap,normalMap;public int profile;public float mouth,metallic,smoothness;public float[] vertices,normals,uv;public int[] triangles; }
static string Target=>File.ReadAllText(Path.Combine(Dir,"active-character.txt")).Trim();
static VarcoIdleMotion Motion()=>UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.transform.parent.name==Target);
[MenuItem("Tools/Character Parts/Export Current Character")]
public static void ExportCurrent(){
if(EditorApplication.isPlaying)throw new Exception("Exit Play first");var mo=Motion();var f=mo.transform.Find("Model").GetComponent<MeshFilter>();var mesh=f.sharedMesh;
if(mesh.blendShapeCount!=0||mesh.subMeshCount!=1)throw new Exception("Requires reviewed multi-material export");
var mat=f.GetComponent<Renderer>().sharedMaterial;var tr=mo.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;var vs=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;
var d=new SurfaceData{character=Target,sourceMesh=AssetDatabase.GetAssetPath(mesh),profile=mo.Profile,mouth=mo.MouthHeightOverride,baseMap=AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")),normalMap=AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")),metallic=mat.GetFloat("_Metallic"),smoothness=mat.GetFloat("_Smoothness"),vertices=new float[vs.Length*3],normals=new float[vs.Length*3],uv=new float[vs.Length*2],triangles=mesh.triangles};
for(int i=0;i<vs.Length;i++){var v=tr.MultiplyPoint3x4(vs[i]);var n=tr.inverse.transpose.MultiplyVector(ns[i]).normalized;d.vertices[i*3]=v.x;d.vertices[i*3+1]=-v.z;d.vertices[i*3+2]=v.y;d.normals[i*3]=n.x;d.normals[i*3+1]=-n.z;d.normals[i*3+2]=n.y;d.uv[i*2]=uv[i].x;d.uv[i*2+1]=uv[i].y;}
string dest=Path.Combine(Dir,Target,"Source");Directory.CreateDirectory(dest);var output=Path.Combine(dest,"current-game-surface.json");if(File.Exists(output))throw new Exception("Source already archived");File.WriteAllText(output,JsonUtility.ToJson(d));File.WriteAllText(Path.Combine(dest,"root-components.json"),string.Join("\n",mo.transform.parent.GetComponents<Component>().Select(x=>EditorJsonUtility.ToJson(x))));}
}}

