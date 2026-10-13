using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class HandRefinementAuthoring
    {
        const string Source="ArtSource/HandRefinement20261013/Output",Output="Assets/Art/Characters/RefinedHands";
        [Serializable] public class FingerData {public string name;public Vector3[] delta,normalDelta;}
        [Serializable] public class HandData {public string name;public bool holding;public int[] anchors,triangles,bindings;public Vector3[] vertices,normals;public Vector2[] uv;public FingerData[] fingers;}
        [Serializable] public class BodyData {public string name;public int vertexCount;public Vector3 paperOffset;public Vector3[] vertices;public Vector2[] uv;public WristSurface.Cut[] cuts;public int[] triangles;public HandData[] hands;}
        static VarcoIdleMotion[] Actors()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VarcoIdleMotion>(true)).OrderBy(a=>a.Profile).ToArray();
        static string FaceKey(System.Collections.Generic.IEnumerable<int> vertices)=>string.Join("/",vertices.OrderBy(i=>i));
        static void SetBodyTriangles(Mesh body,BodyData data)
        {
            string path="Assets/Art/Characters/Repair20261012/"+data.name+".asset";
            var original=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(original==null||original.vertexCount!=data.vertexCount)throw new InvalidDataException("Original repaired mesh mismatch: "+data.name);
            var materials=new System.Collections.Generic.Dictionary<string,int>();
            for(int s=0;s<original.subMeshCount;s++)
            {
                var triangles=original.GetTriangles(s);
                for(int i=0;i<triangles.Length;i+=3)materials[FaceKey(new[]{triangles[i],triangles[i+1],triangles[i+2]})]=s;
            }
            var cutByIndex=data.cuts.ToDictionary(c=>c.index);var groups=Enumerable.Range(0,original.subMeshCount).Select(_=>new System.Collections.Generic.List<int>()).ToArray();
            for(int i=0;i<data.triangles.Length;i+=3)
            {
                var source=new System.Collections.Generic.HashSet<int>();
                for(int j=0;j<3;j++)
                {
                    int index=data.triangles[i+j];
                    if(cutByIndex.TryGetValue(index,out var cut)){source.Add(cut.a);source.Add(cut.b);}else source.Add(index);
                }
                if(!materials.TryGetValue(FaceKey(source),out int material))throw new InvalidDataException("Unmapped source face: "+data.name);
                for(int j=0;j<3;j++)groups[material].Add(data.triangles[i+j]);
            }
            body.subMeshCount=groups.Length;for(int i=0;i<groups.Length;i++)body.SetTriangles(groups[i],i);
        }
        [MenuItem("Tools/Character Hands/Apply")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/Investigation.unity")throw new InvalidOperationException("Saved Investigation scene outside Play required");
            var actors=Actors();if(actors.Length!=10)throw new InvalidOperationException("Expected ten actors");
            // Validate every transfer before modifying the first actor.
            foreach(var actor in actors)
            {
                var d=JsonUtility.FromJson<BodyData>(File.ReadAllText(Source+"/"+actor.transform.parent.name+".json"));
                if(d.vertexCount!=actor.GetComponentInChildren<MeshFilter>(true).sharedMesh.vertexCount||d.hands.Length!=2||d.vertices.Length!=d.vertexCount||d.uv.Length!=d.vertexCount||d.cuts==null)throw new InvalidDataException(d.name);
                var destinations=d.cuts.Select(c=>c.index).ToHashSet();
                if(destinations.Count!=d.cuts.Length||d.cuts.Any(c=>c.index<0||c.index>=d.vertexCount||c.a<0||c.a>=d.vertexCount||c.b<0||c.b>=d.vertexCount||destinations.Contains(c.a)||destinations.Contains(c.b)||c.t<0||c.t>1))throw new InvalidDataException("Invalid cut dependencies: "+d.name);
                foreach(var hand in d.hands)
                {
                    if(hand.anchors.Length!=3||hand.anchors.Any(i=>i<0||i>=d.vertexCount)||hand.bindings==null||hand.bindings.Length!=hand.vertices.Length||hand.bindings.Any(i=>i< -1||i>=d.vertexCount)||hand.fingers==null||hand.fingers.Length!=4)throw new InvalidDataException(hand.name);
                    foreach(string suffix in new[]{"","_Normal","_Mask"})if(!File.Exists(Source+"/"+hand.name+suffix+".png"))throw new InvalidDataException(hand.name+suffix);
                    foreach(var finger in hand.fingers)if(finger.delta.Length!=hand.vertices.Length||finger.normalDelta.Length!=hand.vertices.Length||finger.delta.Any(v=>!float.IsFinite(v.sqrMagnitude)||v.magnitude>.025f))throw new InvalidDataException("Invalid finger pose: "+hand.name);
                }
            }
            Directory.CreateDirectory(Output);
            foreach(var actor in actors)
            {
                string name=actor.transform.parent.name;
                var d=JsonUtility.FromJson<BodyData>(File.ReadAllText(Source+"/"+name+".json"));
                actor.HeldRecordOffset=d.paperOffset;EditorUtility.SetDirty(actor);PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                var filter=actor.GetComponentInChildren<MeshFilter>(true);
                var body=AssetDatabase.LoadAssetAtPath<Mesh>(Output+"/"+name+"_Body.asset");
                if(body==null){body=UnityEngine.Object.Instantiate(filter.sharedMesh);AssetDatabase.CreateAsset(body,Output+"/"+name+"_Body.asset");}
                body.vertices=d.vertices.Select(v=>filter.transform.InverseTransformPoint(actor.transform.TransformPoint(v))).ToArray();body.uv=d.uv;
                SetBodyTriangles(body,d);EditorUtility.SetDirty(body);Undo.RecordObject(filter,"Replace fused hand surface");filter.sharedMesh=body;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                var wrist=filter.GetComponent<WristSurface>();if(wrist==null)wrist=filter.gameObject.AddComponent<WristSurface>();wrist.Cuts=d.cuts;EditorUtility.SetDirty(wrist);
                var positions=body.vertices;var normals=body.normals;wrist.Apply(positions,normals);body.vertices=positions;body.normals=normals;
                foreach(var h in d.hands)
                {
                    string texturePath=Output+"/"+h.name+".png";File.Copy(Source+"/"+h.name+".png",texturePath,true);AssetDatabase.ImportAsset(texturePath);
                    var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=512;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(Output+"/"+h.name+".mat");
                    if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Output+"/"+h.name+".mat");}
                    mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetFloat("_Smoothness",.3f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
                    foreach(string suffix in new[]{"_Normal","_Mask"})
                    {
                        string path=Output+"/"+h.name+suffix+".png";File.Copy(Source+"/"+h.name+suffix+".png",path,true);AssetDatabase.ImportAsset(path);
                        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=suffix=="_Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                        mat.SetTexture(suffix=="_Normal"?"_BumpMap":"_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                    }
                    mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_BumpScale",.6f);mat.SetFloat("_Smoothness",1);mat.SetFloat("_SmoothnessTextureChannel",0);
                    var existing=actor.transform.Find(h.name);var go=existing!=null?existing.gameObject:new GameObject(h.name);
                    if(existing==null)Undo.RegisterCreatedObjectUndo(go,"Create refined hand");
                    go.transform.SetParent(actor.transform,false);
                    var component=go.GetComponent<RefinedHand>();if(component==null)component=go.AddComponent<RefinedHand>();component.Source=filter;component.A=h.anchors[0];component.B=h.anchors[1];component.C=h.anchors[2];component.BoundaryBindings=h.bindings;component.MotionPhase=actor.Profile*.71f+(h.name.Contains("Left")?0:1.9f);component.Follow(filter,body.vertices);
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Output+"/"+h.name+".asset");
                    if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,Output+"/"+h.name+".asset");}else mesh.Clear();
                    mesh.name=h.name;
                    mesh.vertices=h.vertices.Select(v=>go.transform.InverseTransformPoint(actor.transform.TransformPoint(v))).ToArray();
                    mesh.normals=h.normals.Select(v=>go.transform.InverseTransformDirection(actor.transform.TransformDirection(v))).ToArray();
                    mesh.uv=h.uv;mesh.triangles=h.triangles;
                    var authoredNormals=mesh.normals;mesh.RecalculateNormals();var windingNormals=mesh.normals;
                    float agreement=0;for(int i=0;i<authoredNormals.Length;i++)agreement+=Vector3.Dot(authoredNormals[i],windingNormals[i]);
                    if(agreement<0)for(int i=0;i<authoredNormals.Length;i++)authoredNormals[i]=-authoredNormals[i];
                    mesh.normals=authoredNormals;mesh.ClearBlendShapes();
                    foreach(var finger in h.fingers)
                    {
                        var delta=finger.delta.Select(v=>go.transform.InverseTransformVector(actor.transform.TransformVector(v))).ToArray();
                        var dn=finger.normalDelta.Select(v=>go.transform.InverseTransformDirection(actor.transform.TransformDirection(v))*(agreement<0?-1:1)).ToArray();
                        mesh.AddBlendShapeFrame(finger.name,100,delta,dn,null);
                    }
                    mesh.RecalculateTangents();mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(.025f);mesh.bounds=bounds;EditorUtility.SetDirty(mesh);
                    // Skinned renderer prevents the body deformation loop treating this as another body.
                    var renderer=go.GetComponent<SkinnedMeshRenderer>();if(renderer==null)renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=mat;renderer.localBounds=mesh.bounds;renderer.updateWhenOffscreen=false;
                    EditorUtility.SetDirty(component);EditorUtility.SetDirty(renderer);
                }
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Validate();
        }
        [MenuItem("Tools/Character Hands/Validate")]
        public static void Validate()
        {
            Directory.CreateDirectory("Logs/HandRefinement");var lines=new System.Collections.Generic.List<string>();int failures=0;
            foreach(var actor in Actors())
            {
                var hands=actor.GetComponentsInChildren<RefinedHand>(true);bool good=hands.Length==2;
                foreach(var hand in hands)
                {
                    var renderer=hand.GetComponent<SkinnedMeshRenderer>();var mesh=renderer.sharedMesh;
                    good&=mesh!=null&&mesh.vertexCount>100&&mesh.triangles.Length>300&&renderer.sharedMaterial!=null&&hand.Source!=null;
                    good&=mesh.bounds.size.magnitude<.5f&&mesh.blendShapeCount==4&&renderer.sharedMaterial.GetTexture("_BumpMap")&&renderer.sharedMaterial.GetTexture("_MetallicGlossMap");
                }
                lines.Add((good?"PASS ":"FAIL ")+actor.transform.parent.name+" hands="+hands.Length);if(!good)failures++;
            }
            lines.Add("COMPLETE failures="+failures);File.WriteAllLines("Logs/HandRefinement/checks.txt",lines);
            if(failures>0)throw new Exception("Hand validation failed");
        }
    }
}
