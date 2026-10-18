using System;using System.IO;using UnityEditor;using UnityEngine;
namespace RedCliffMystery.EditorTools {public static partial class CharacterPartsWorkbench {
[MenuItem("Tools/Character Parts/Check Handedness")]
public static void CheckHandedness(){var d=JsonUtility.FromJson<SurfaceData>(File.ReadAllText(Path.Combine(Dir,Target,"Source/current-game-surface.json")));var f=Motion().transform.Find("Model").GetComponent<MeshFilter>();var a=f.sharedMesh.vertices;double same=0,mirror=0;int count=0;for(int i=0;i<d.vertices.Length/3;i+=47){var q=new Vector3(d.vertices[i*3],d.vertices[i*3+2],-d.vertices[i*3+1]);float x=float.MaxValue,y=float.MaxValue;foreach(var v in a){x=Mathf.Min(x,(q-v).sqrMagnitude);var m=new Vector3(-v.x,v.y,v.z);y=Mathf.Min(y,(q-m).sqrMagnitude);}same+=Math.Sqrt(x);mirror+=Math.Sqrt(y);count++;}Directory.CreateDirectory(Reports);File.WriteAllText(Path.Combine(Reports,"handedness.txt"),"originalAxisMean="+same/count+" mirroredAxisMean="+mirror/count);}
}}

