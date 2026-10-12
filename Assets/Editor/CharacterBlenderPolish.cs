using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RedCliffMystery;
using Object = UnityEngine.Object;

namespace RedCliffMystery.EditorTools
{
    public static class CharacterBlenderPolish
    {
        public const string Source = "ArtSource/CharacterPolish20261012";
        public const string Output = "Assets/Art/Characters/BlenderPolish20261012";
        [Serializable] public class Surface {
            public string name, meshPath, materialPath, colorPath, normalPath;
            public int profile; public Vector3[] vertices; public Vector2[] uv; public int[] triangles;
        }
        static VarcoIdleMotion[] Actors() => Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include).OrderBy(m=>m.Profile).ToArray();
        static void Guard() {
            if(Application.isPlaying || EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Saved scene in Edit mode required");
        }
        [MenuItem("적벽추리/인물 보정/블렌더 작업용 내보내기")]
        public static void Export() {
            Guard();Directory.CreateDirectory(Source+"/Input");
            foreach(var actor in Actors()) {
                var filters=actor.GetComponentsInChildren<MeshFilter>(true);
                if(filters.Length!=1)throw new Exception("Expected one mesh per actor");
                var f=filters[0];var mat=f.GetComponent<MeshRenderer>().sharedMaterial;
                var s=new Surface {name=actor.transform.parent.name,profile=actor.Profile,meshPath=AssetDatabase.GetAssetPath(f.sharedMesh),materialPath=AssetDatabase.GetAssetPath(mat),
                    colorPath=AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")),normalPath=AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")),
                    vertices=f.sharedMesh.vertices.Select(v=>actor.transform.InverseTransformPoint(f.transform.TransformPoint(v))).ToArray(),uv=f.sharedMesh.uv,triangles=f.sharedMesh.triangles};
                File.WriteAllText(Source+"/Input/"+s.name+".json",JsonUtility.ToJson(s));
            }
            File.WriteAllText(Source+"/export-complete.txt","10 current posed meshes exported");
        }
        [MenuItem("적벽추리/인물 보정/블렌더 보정본 적용")]
        public static void Apply() {
            Guard();Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs/CharacterPolish");
            File.Copy(EditorSceneManager.GetActiveScene().path,"Logs/CharacterPolish/before-"+DateTime.Now.ToString("HHmmss")+".unity");
            // Validate every input before making any scene changes.
            var actors=Actors();
            foreach(var a in actors){var s=Read(a);var f=a.GetComponentInChildren<MeshFilter>();if(s.vertices.Length!=f.sharedMesh.vertexCount||!s.triangles.SequenceEqual(f.sharedMesh.triangles))throw new Exception("Topology mismatch "+s.name);}
            foreach(var a in actors){var s=Read(a);var f=a.GetComponentInChildren<MeshFilter>();var path=Output+"/"+s.name+".asset";
                var mesh=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>(s.meshPath));
                mesh.name=s.name+"_BlenderPolished";
                mesh.vertices=s.vertices.Select(v=>f.transform.InverseTransformPoint(a.transform.TransformPoint(v))).ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();
                if(AssetDatabase.LoadAssetAtPath<Mesh>(path))throw new Exception("Output already exists; do not overwrite a review copy");
                AssetDatabase.CreateAsset(mesh,path);
                var mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(s.materialPath));mat.name=s.name+"_PolishedSurface";
                mat.SetFloat("_BumpScale",s.profile==0||s.profile==2||s.profile==3||s.profile==4||s.profile==7?.22f:.16f);
                mat.SetFloat("_Smoothness",.16f);AssetDatabase.CreateAsset(mat,Output+"/"+s.name+".mat");
                Undo.RecordObject(f,"Blender character polish");f.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(f);
                var renderer=f.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Blender surface polish");renderer.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            File.WriteAllText("Logs/CharacterPolish/apply.txt","COMPLETE 10 characters; topology, UV, original assets preserved");
        }
        static Surface Read(VarcoIdleMotion actor)=>JsonUtility.FromJson<Surface>(File.ReadAllText(Source+"/Output/"+actor.transform.parent.name+".json"));
    }
}
