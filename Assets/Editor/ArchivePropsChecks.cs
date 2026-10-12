using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class ArchivePropsChecks
{
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first");
        var room=UnityEngine.Object.FindObjectsByType<Transform>().Single(t=>t.name=="ArchiveRoom");
        var records=room.GetComponentsInChildren<Transform>().Where(t=>t.name=="BambooRecords" || t.name=="AdjacentRecords").ToArray();
        Require(records.Length==39,"Document placeholder count changed");
        foreach(var record in records) {
            Require(!record.GetComponent<Renderer>().enabled,"Old block still visible");
            var visual=record.Find("ArchiveRecordVisual"); Require(visual!=null,"Missing finished volume");
            var bounds=visual.GetComponent<Renderer>().bounds; Require(bounds.size.magnitude>.20f && bounds.size.magnitude<1,"Incorrect prop scaling");
        }
        foreach(string path in Directory.GetFiles("Assets/Art/Props/Archive","*.prefab")) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;
            Require(mesh.vertexCount>100 && prefab.GetComponent<Renderer>().sharedMaterials.All(m=>m!=null),"Incomplete prop "+path);
        }
        var tray=UnityEngine.Object.FindObjectsByType<InvestigationTarget>().Single(t=>t.kind==InvestigationTarget.TargetKind.MissingDocument);
        Require(!tray.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh.name=="OpenSlipLedger" || f.sharedMesh.name=="TiedSlipStack"),"Missing document tray filled");
        var props=room.Find("ArchiveDeskProps"); Require(props.childCount==8,"Desk/storage furnishing missing");
        Require(props.Find("ArchiveStorageChest").GetComponent<Collider>()!=null,"Chest collision missing");
        Directory.CreateDirectory("Logs/ArchiveProps");
        Render("shelves",new Vector3(-3.5f,1.7f,-2.35f),new Vector3(-5.1f,1.5f,-2.4f),52);
        Render("desk",new Vector3(.2f,2.0f,1.0f),new Vector3(-.25f,1.1f,2.4f),60);
        Render("writing-kit",new Vector3(-.9f,1.5f,2.02f),new Vector3(-1.07f,1.07f,2.46f),46);
        File.WriteAllText("Logs/ArchiveProps/result.txt","PASS 39 finished record props, reusable meshes/materials, preserved empty tray, furnished desks, storage collision and three scene renders\nCOMPLETE failures=0\n");
        Debug.Log("ARCHIVE_PROPS_CHECKS_PASS");
    }
    static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    static void Render(string name,Vector3 position,Vector3 focus,float fov)
    {
        var go=new GameObject("ArchivePropPreview",typeof(Camera)); var camera=go.GetComponent<Camera>();
        camera.transform.position=position; camera.transform.LookAt(focus); camera.fieldOfView=fov; camera.nearClipPlane=.025f; camera.farClipPlane=60;
        camera.backgroundColor=new Color(.08f,.1f,.11f); camera.clearFlags=CameraClearFlags.SolidColor;
        var rt=new RenderTexture(1600,1000,24); var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false); var active=RenderTexture.active;
        try { camera.targetTexture=rt; camera.aspect=1.6f; camera.Render(); camera.Render(); camera.Render(); RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1600,1000),0,0); pixels.Apply(); File.WriteAllBytes("Logs/ArchiveProps/"+name+".png",pixels.EncodeToPNG());
        } finally { RenderTexture.active=active; camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(go); }
    }
}
