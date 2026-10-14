using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    // Visual replacements retain existing investigation objects and hit volumes.
    public static class ReceivedPropsInstaller
    {
        const string AssetsRoot="Assets/Art/Environment/VarcoDetailBaked/";
        const string Log="Logs/VarcoPropsWork";
        static readonly string[] Names={"CanvasDetails","SupplyLoad","FieldTable","ArchiveShelf"};
        static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        [MenuItem("Tools/Camp Environment/Review Received Props")]
        public static void Review()
        {
            if(SceneManager.GetActiveScene().path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation required");
            Directory.CreateDirectory(Log);
            var names=new[]{"SupplyTent","GrainLoad0","Desk0","ArchiveShelf0"};
            foreach(var name in names)
            {
                var target=All().Single(t=>t.name==name&&t.parent&&t.parent.name=="RefinedReceivedProps");
                var b=BoundsOf(target.GetComponentsInChildren<Renderer>());
                var go=new GameObject("TemporaryPropReviewCamera");var camera=go.AddComponent<Camera>();
                camera.enabled=false;camera.fieldOfView=60;camera.nearClipPlane=.03f;
                float distance=Mathf.Max(b.size.x,b.size.z)*1.15f;
                var offset=name.StartsWith("ArchiveShelf")?new Vector3(distance,.6f,distance*.55f):new Vector3(distance*.35f,.7f,distance);
                camera.transform.position=b.center+offset;camera.transform.LookAt(b.center);
                var rt=new RenderTexture(1200,800,24);var previous=RenderTexture.active;Texture2D image=null;
                try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();File.WriteAllBytes(Log+"/"+name+".png",image.EncodeToPNG());}
                finally {RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
            }
        }
        static Bounds BoundsOf(IEnumerable<Renderer> renderers)
        {
            var list=renderers.ToArray();if(list.Length==0)throw new Exception("Missing source visuals");
            var b=list[0].bounds;foreach(var r in list.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        static void Hide(IEnumerable<Renderer> renderers)
        {foreach(var r in renderers){Undo.RecordObject(r,"Replace prop visual");r.enabled=false;}}
        static Transform Fit(string asset,string name,Transform root,Bounds target,float yaw=0)
        {
            var t=root.Find(name);
            if(!t){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot+asset+".fbx"));Undo.RegisterCreatedObjectUndo(g,"Place refined prop");t=g.transform;t.SetParent(root,false);t.name=name;}
            Undo.RecordObject(t,"Fit refined prop");t.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));t.localScale=Vector3.one;
            var rs=t.GetComponentsInChildren<Renderer>();var initial=BoundsOf(rs);
            var size=new Vector3(target.size.x/initial.size.x,target.size.y/initial.size.y,target.size.z/initial.size.z);
            t.localScale=Mathf.Abs(yaw%180)>45?new Vector3(size.z,size.y,size.x):size;
            var scaled=BoundsOf(rs);t.position+=target.center-scaled.center;
            foreach(var r in rs){r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+asset+".mat");PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);return t;
        }
        [MenuItem("Tools/Camp Environment/Apply Four Received Props")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying||scene.path!="Assets/Scenes/Investigation.unity")throw new Exception("Investigation Edit scene required");
            if(All().Any(t=>t.name=="RefinedReceivedProps"))throw new Exception("Already installed; inspect and adjust existing props rather than duplicate them.");
            foreach(var name in Names)
            {
                long bytes=Directory.GetFiles(AssetsRoot,name+"*").Where(p=>p.EndsWith(".fbx")||p.EndsWith(".png")).Sum(p=>new FileInfo(p).Length);
                if(bytes==0||bytes>10*1024*1024)throw new Exception("Missing or oversized game asset: "+name);
                var material=CraftedPropInstaller.PrepareBakedAsset(name);
                material.SetFloat("_BumpScale",.3f);EditorUtility.SetDirty(material);
            }
            Directory.CreateDirectory("Assets/Logs/ReceivedProps");
            if(!EditorSceneManager.SaveScene(scene,"Assets/Logs/ReceivedProps/before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var all=All();int colliders=all.Sum(t=>t.GetComponents<Collider>().Length);
            var targets=all.SelectMany(t=>t.GetComponents<InvestigationTarget>()).ToArray();
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Apply four refined props");
            try
            {
                var root=new GameObject("RefinedReceivedProps").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Place refined props");
                int tables=0;
                foreach(var old in all.Where(t=>t.name=="FieldDesk"&&t.parent&&t.parent.name=="RefinedProp20261012"))
                {
                    var rs=old.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                    if(rs.Length==0)continue; // Already superseded/hidden legacy desk.
                    Fit("FieldTable","Desk"+tables++,root,BoundsOf(rs));Hide(rs);
                }
                foreach(var table in all.Where(t=>t.name=="WritingTable"&&t.parent&&(t.parent.name=="CaimaoTent"||t.parent.name=="ZhangyunTent")))
                {
                    var rs=table.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Tabletop"||r.name.StartsWith("Leg")).ToArray();
                    if(rs.Length==0)throw new Exception("Writing table has no replacement surfaces: "+table.parent.name);
                    Fit("FieldTable","Desk"+tables++,root,BoundsOf(rs));Hide(rs);
                }
                // Place sacks beside the existing rear supply pile, clear of the investigation station.
                for(int i=0;i<2;i++)
                {
                    var t=Fit("SupplyLoad","GrainLoad"+i,root,new Bounds(new Vector3(-46.3f+i*1.4f,.48f,-27.85f),new Vector3(1.25f,.96f,1.1f)),i==0?0:180);
                    var c=Undo.AddComponent<BoxCollider>(t.gameObject);var b=BoundsOf(t.GetComponentsInChildren<Renderer>());
                    c.center=t.InverseTransformPoint(b.center);c.size=new Vector3(b.size.x/t.lossyScale.x,b.size.y/t.lossyScale.y,b.size.z/t.lossyScale.z);
                }
                // CanvasDetails now contains the entire tent, not a flat rear sheet.
                var canopy=all.Single(t=>t.name=="SupplyCanopy"&&t.parent&&t.parent.name=="BakedCampEnvironment");
                var canopyVisuals=canopy.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                Fit("CanvasDetails","SupplyTent",root,BoundsOf(canopyVisuals));Hide(canopyVisuals);
                var room=all.Single(t=>t.name=="ArchiveRoom");
                for(int i=0;i<3;i++)
                {
                    float z=-2.5f+i*3;
                    var shelf=Fit("ArchiveShelf","ArchiveShelf"+i,root,new Bounds(new Vector3(-5.15f,1.12f,z),new Vector3(.9f,2.24f,2.3f)),90);
                    var mesh=shelf.GetComponentInChildren<MeshFilter>();
                    var probe=mesh.gameObject.AddComponent<MeshCollider>();probe.sharedMesh=mesh.sharedMesh;Physics.SyncTransforms();
                    var levels=new List<float>();float start=3;
                    for(int j=0;j<12&&levels.Count<3;j++)
                    {
                        if(!probe.Raycast(new Ray(new Vector3(-5.15f,start,z),Vector3.down),out var hit,4))break;
                        if(hit.normal.y>.5f&&(levels.Count==0||Mathf.Abs(levels.Last()-hit.point.y)>.12f))levels.Add(hit.point.y);
                        start=hit.point.y-.025f;
                    }
                    UnityEngine.Object.DestroyImmediate(probe);
                    if(levels.Count!=3)throw new Exception("Could not locate all three shelf surfaces; keep previous scene and inspect shelf mesh");
                    levels.Sort();
                    foreach(var record in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BambooRecords"&&Mathf.Abs(t.position.z-z)<1.2f))
                    {
                        var visual=record.Find("ArchiveRecordVisual");if(!visual||!visual.gameObject.activeInHierarchy)continue;
                        int tier=Mathf.Clamp(Mathf.RoundToInt((record.position.y-.5f)/.8f),0,2);
                        var b=BoundsOf(visual.GetComponentsInChildren<Renderer>());Undo.RecordObject(visual,"Rest records on new shelf");
                        visual.position+=Vector3.up*(levels[tier]+.003f-b.min.y);
                    }
                }
                Hide(room.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="ShelfBack"||r.name=="ShelfBoard"));
                if(All().Sum(t=>t.GetComponents<Collider>().Length)!=colliders+2)throw new Exception("Unexpected collider change");
                var after=All().SelectMany(t=>t.GetComponents<InvestigationTarget>()).ToArray();
                if(targets.Length!=after.Length||targets.Except(after).Any())throw new Exception("Investigation targets changed");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Directory.CreateDirectory(Log);File.WriteAllText(Log+"/unity-checks.txt",DateTime.Now.ToString("O")+" COMPLETE failures=0; tables="+tables+"; shelves=3; sacks=2; canvas=1; original colliders="+colliders+"; new sack colliders=2; targets preserved="+targets.Length);
            }
            catch {Undo.RevertAllDownToGroup(undo);throw;}
        }
    }
}
