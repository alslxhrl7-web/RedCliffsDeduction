using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class RoleActivityAuthoring
    {
        public const string Source = "ArtSource/RoleActivities20261013";
        public const string Output = "Assets/Art/Characters/RoleActivities";
        [Serializable] public class SourceMesh
        {
            public string name, colorPath, normalPath;
            public int profile;
            public Vector3[] vertices;
            public Vector2[] uv;
            public int[] triangles;
        }
        [Serializable] public class PropMesh
        {
            public Vector3[] vertices,a,b;
            public Vector2[] uv;
            public int[] triangles;
        }
        static VarcoIdleMotion[] Actors() => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<VarcoIdleMotion>(true)).OrderBy(m=>m.Profile).ToArray();
        static void CheckScene()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Investigation.unity")
                throw new InvalidOperationException("Saved Investigation scene required, outside Play mode.");
        }
        [MenuItem("Tools/Character Motion/Snapshot Current Scene")]
        public static void Snapshot()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Temp/ActivitySceneSnapshot.unity",true);
        }
        [MenuItem("Tools/Character Motion/Export Activity Sources")]
        public static void Export()
        {
            CheckScene(); Directory.CreateDirectory(Source+"/Input");
            foreach(var actor in Actors())
            {
                var f=actor.GetComponentInChildren<MeshFilter>(true); var mesh=f.sharedMesh;
                var mat=f.GetComponent<Renderer>().sharedMaterial;
                var source=new SourceMesh { name=actor.transform.parent.name, profile=actor.Profile,
                    vertices=mesh.vertices.Select(v=>actor.transform.InverseTransformPoint(f.transform.TransformPoint(v))).ToArray(),
                    triangles=mesh.triangles, uv=mesh.uv,
                    colorPath=AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")),
                    normalPath=AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")) };
                File.WriteAllText(Source+"/Input/"+source.name+".json",JsonUtility.ToJson(source));
            }
            Debug.Log("Activity sources exported: "+Actors().Length);
        }
        [MenuItem("Tools/Character Motion/Apply Baked Activities")]
        public static void Apply() => ApplyFrom(Source);
        public static void ApplyFrom(string source)
        {
            CheckScene();Directory.CreateDirectory(Output);
            var actors=Actors();
            if(actors.Length!=10)throw new InvalidOperationException("Expected ten actors");
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name;
                if(!File.Exists(source+"/Output/"+name+".bytes"))throw new FileNotFoundException(name);
            }
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name;
                File.Copy(source+"/Output/"+name+".bytes",Output+"/"+name+".bytes",true);
            }
            AssetDatabase.Refresh();
            File.Copy(source+"/Output/HeldPaper_BaseColor.png",Output+"/HeldPaper_BaseColor.png",true);
            AssetDatabase.Refresh();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Output+"/HeldPaper.mat");
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material,Output+"/HeldPaper.mat");
            }
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Output+"/HeldPaper_BaseColor.png"));
            material.SetFloat("_Smoothness",.05f);material.SetFloat("_Metallic",0);material.SetFloat("_Cull",0);EditorUtility.SetDirty(material);
            foreach(var actor in actors)
            {
                var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(Output+"/"+actor.transform.parent.name+".bytes");
                var clip=RoleActivityClip.Read(asset);
                if(clip.Profile!=actor.Profile || clip.VertexCount!=actor.GetComponentInChildren<MeshFilter>(true).sharedMesh.vertexCount)
                    throw new InvalidDataException("Mismatched activity: "+asset.name);
                Undo.RecordObject(actor,"Apply Blender role activity");actor.RoleActivity=asset;EditorUtility.SetDirty(actor);
                string propFile=source+"/Output/"+actor.transform.parent.name+"-prop.json";
                if(File.Exists(propFile))
                {
                    var data=JsonUtility.FromJson<PropMesh>(File.ReadAllText(propFile));
                    string meshPath=Output+"/"+actor.transform.parent.name+"_HeldRecord.asset";
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}
                    mesh.Clear();mesh.ClearBlendShapes();mesh.vertices=data.vertices;mesh.triangles=data.triangles;mesh.uv=data.uv;mesh.RecalculateNormals();mesh.RecalculateTangents();
                    mesh.AddBlendShapeFrame("ActivityA",100,data.a,null,null);mesh.AddBlendShapeFrame("ActivityB",100,data.b,null,null);
                    mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                    var child=actor.transform.Find("HeldRecord");
                    if(child==null){var go=new GameObject("HeldRecord");Undo.RegisterCreatedObjectUndo(go,"Add held record");child=go.transform;child.SetParent(actor.transform,false);}
                    var renderer=child.GetComponent<SkinnedMeshRenderer>();if(renderer==null)renderer=Undo.AddComponent<SkinnedMeshRenderer>(child.gameObject);
                    renderer.sharedMesh=mesh;renderer.sharedMaterial=material;renderer.localBounds=mesh.bounds;actor.HeldRecord=renderer;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();Debug.Log("Applied ten baked role activities");
        }
        [MenuItem("Tools/Character Motion/Validate Baked Activities")]
        public static void Validate()
        {
            CheckScene();int count=0,props=0;
            var report=new System.Text.StringBuilder();
            foreach(var actor in Actors())
            {
                if(actor.RoleActivity==null)throw new InvalidDataException("Missing activity "+actor.name);
                var clip=RoleActivityClip.Read(actor.RoleActivity);var filter=actor.GetComponentInChildren<MeshFilter>(true);
                if(clip.Profile!=actor.Profile || clip.VertexCount!=filter.sharedMesh.vertexCount)throw new InvalidDataException("Activity mismatch");
                var bound=clip.Bind(filter.transform.worldToLocalMatrix*actor.transform.localToWorldMatrix);
                var originalNormals=filter.sharedMesh.normals;
                double normalDot=bound.Average(v=>(double)Vector3.Dot(originalNormals[v.Index],v.NormalRest));
                if(normalDot<.35)throw new InvalidDataException("Inverted activity normals: "+actor.transform.parent.name);
                for(int i=0;i<400;i++)
                {
                    var w=clip.Sample(i*.05f);
                    if(float.IsNaN(w.x) || Mathf.Abs(w.x+w.y+w.z-1)>.001f)throw new InvalidDataException("Invalid activity curve");
                }
                if(actor.Profile==1 || actor.Profile==5 || actor.Profile==6)
                {
                    if(actor.HeldRecord==null || actor.HeldRecord.sharedMesh.blendShapeCount!=2)throw new InvalidDataException("Missing held record");
                    props++;
                }
                report.AppendLine("PASS "+actor.transform.parent.name+" normals="+normalDot.ToString("F3"));count++;
            }
            if(count!=10 || props!=3)throw new InvalidDataException("Activity count mismatch");
            report.AppendLine("COMPLETE failures=0; actors=10; held records=3");
            Directory.CreateDirectory("Logs/RoleActivities");File.WriteAllText("Logs/RoleActivities/validation.txt",report.ToString());
        }
    }
}

