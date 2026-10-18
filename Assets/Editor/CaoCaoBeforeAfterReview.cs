using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RedCliffMystery;
public static class CaoCaoBeforeAfterReview
{
    static MeshFilter filter;static Mesh current;static Mesh canonical;static Mesh previous;static Material[] currentMaterials;static VarcoIdleMotion motion;static bool wasEnabled;
    static void Init()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Comparison requires Play mode");
        if(filter)return;
        var visual=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t=>t.name=="CaoCao"&&t.Find("VarcoVisual")).Find("VarcoVisual");
        motion=visual.GetComponent<VarcoIdleMotion>();wasEnabled=motion.enabled;motion.Sample(0);motion.enabled=false;
        filter=visual.Find("Model").GetComponent<MeshFilter>();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/VarcoV3/CaoCao/CaoCaoVisual.prefab");var model=prefab.transform.Find("Model");canonical=model.GetComponent<MeshFilter>().sharedMesh;current=UnityEngine.Object.Instantiate(canonical);current.MarkDynamic();currentMaterials=model.GetComponent<Renderer>().sharedMaterials;
    }
    [MenuItem("Tools/RedCliffs/CaoCao Comparison/Previous Demo Model")]
    public static void Previous()
    {
        Init();var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Characters/VarcoV3/CaoCao/CaoCao_RolePose0.asset");var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Characters/VarcoV3/CaoCao/CaoCao_0.mat");
        if(!mesh||!mat)throw new InvalidOperationException("Previous source assets missing");if(previous)UnityEngine.Object.Destroy(previous);previous=UnityEngine.Object.Instantiate(mesh);previous.MarkDynamic();filter.sharedMesh=previous;filter.GetComponent<Renderer>().sharedMaterials=new[]{mat};
    }
    [MenuItem("Tools/RedCliffs/CaoCao Comparison/Current Model")]
    public static void Current(){Init();filter.sharedMesh=current;filter.GetComponent<Renderer>().sharedMaterials=currentMaterials;File.AppendAllText("LocalArchive/CaoCaoHairEnemies20261016/Revision16/comparison-state.txt",current.name+" "+current.subMeshCount+" "+string.Join(",",currentMaterials.Select(m=>m.name))+"\\n");}
    [MenuItem("Tools/RedCliffs/CaoCao Comparison/Finish")]
    public static void Finish(){if(!filter)return;filter.sharedMesh=canonical;filter.GetComponent<Renderer>().sharedMaterials=currentMaterials;motion.enabled=wasEnabled;if(current)UnityEngine.Object.Destroy(current);if(previous)UnityEngine.Object.Destroy(previous);filter=null;current=null;canonical=null;previous=null;currentMaterials=null;}
}



