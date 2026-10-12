using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class RemainingDocumentInstaller
    {
        const string Folder="Assets/Art/Evidence/VarcoRemaining20261012/";
        const string Materials="Assets/Art/Evidence/VarcoRefined20261012V2/";
        const string Logs="Logs/RemainingDocuments";
        static readonly string[] Models={"DutyRecordRefined","HouseholdRegistryRefined","SecretLetterRefined"};
        static GameObject Find(string name)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name).gameObject;
        static void Guard(){if(Application.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/Investigation.unity"||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Investigation scene in Edit mode required");}
        static Bounds BoundsOf(GameObject model){var rr=model.GetComponentsInChildren<Renderer>(true);var bounds=rr[0].bounds;foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);return bounds;}
        static GameObject Prefab(string name)
        {
            string path=Folder+name+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".fbx");if(!source)throw new InvalidOperationException("Missing model: "+name);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try{
                foreach(var renderer in root.GetComponentsInChildren<Renderer>()){
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                        var material=AssetDatabase.LoadAssetAtPath<Material>(Materials+m.name+".mat");
                        if(!material)throw new InvalidOperationException("Missing shared material "+m.name);return material;
                    }).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static GameObject Place(string name,Transform parent,Bounds desired)
        {
            var found=parent.Find(name);if(found)return found.gameObject;
            var wrapper=new GameObject(name);Undo.RegisterCreatedObjectUndo(wrapper,"Apply remaining document");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(Prefab(name));Undo.RegisterCreatedObjectUndo(model,"Apply remaining document");model.transform.SetParent(wrapper.transform,false);
            var bounds=BoundsOf(model);
            if(bounds.size.y>.05f)throw new InvalidOperationException("Unexpected document axis: "+name);
            wrapper.transform.localScale=new Vector3(desired.size.x/bounds.size.x,desired.size.y/bounds.size.y,desired.size.z/bounds.size.z);
            wrapper.transform.position+=desired.center-BoundsOf(model).center;wrapper.transform.SetParent(parent,true);return wrapper;
        }
        static Texture2D Preview(string name)
        {
            string path=Folder+name+"Preview.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Missing preview: "+name);
            importer.maxTextureSize=1024;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void Apply()
        {
            Guard();Directory.CreateDirectory(Logs);string backup=Logs+"/Investigation-before-remaining.unity";if(!File.Exists(backup))File.Copy(SceneManager.GetActiveScene().path,backup);
            foreach(string name in Models)Prefab(name);
            var textures=Models.Select(Preview).ToArray();
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{
                var duty=Find("DutyRecord");var original=duty.GetComponent<Renderer>();var desired=original.bounds;desired.center=new Vector3(desired.center.x,desired.max.y-.006f,desired.center.z);desired.size=new Vector3(desired.size.x,.012f,desired.size.z);
                Place(Models[0],duty.transform,desired);Undo.RecordObject(original,"Retain original duty record");original.enabled=false;
                foreach(Transform child in duty.transform.parent){var r=child.GetComponent<Renderer>();if(child.name.StartsWith("Writing1")&&r){Undo.RecordObject(r,"Replace block lettering");r.enabled=false;}}
                var registryBounds=new Bounds(desired.center+new Vector3(0,.019f,.11f),new Vector3(.35f,.025f,.29f));
                var registry=Place(Models[1],duty.transform.parent,registryBounds);
                var papers=Find("PersonalPapers");var lid=papers.transform.Find("Lid").GetComponent<Renderer>().bounds;
                var letter=Place(Models[2],papers.transform,new Bounds(new Vector3(lid.center.x,lid.max.y+.005f,lid.center.z),new Vector3(.40f,.008f,.34f)));
                Undo.RecordObject(registry,"Hide undiscovered registry");registry.SetActive(false);Undo.RecordObject(letter,"Hide undiscovered letter");letter.SetActive(false);
                var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
                var visibility=session.GetComponent<DiscoveredDocumentVisuals>()??Undo.AddComponent<DiscoveredDocumentVisuals>(session.gameObject);
                Undo.RecordObject(visibility,"Bind discovered documents");visibility.session=session;visibility.householdRegistry=registry;visibility.secretLetter=letter;
                var view=session.view.GetComponent<EvidenceDocumentView>();Undo.RecordObject(view,"Apply refined document previews");view.dutyRecord=textures[0];view.householdRegistry=textures[1];view.secretLetter=textures[2];
                Undo.CollapseUndoOperations(group);AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed");
                File.WriteAllText(Logs+"/apply.txt","Duty model on existing table; registry and letter assigned with discovery gates; all three evidence previews assigned. Original assets retained.\n");
            }catch{Undo.RevertAllDownToGroup(group);throw;}
        }
        public static void Validate()
        {
            Guard();EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","Remaining documents saved-scene checks\n");
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();var gate=session.GetComponent<DiscoveredDocumentVisuals>();var view=session.view.GetComponent<EvidenceDocumentView>();
            if(!gate||gate.session!=session||!gate.householdRegistry||!gate.secretLetter)throw new InvalidOperationException("Visibility binding missing");
            if(gate.householdRegistry.activeSelf||gate.secretLetter.activeSelf)throw new InvalidOperationException("Undiscovered clues visible");
            var duty=Find("DutyRecord");if(!duty.transform.Find(Models[0])||duty.GetComponent<Renderer>().enabled)throw new InvalidOperationException("Duty visual missing");
            var collider=duty.GetComponent<Collider>();Physics.SyncTransforms();if(!collider.enabled||!collider.Raycast(new Ray(collider.bounds.center+Vector3.up,Vector3.down),out var hit,2)||!duty.GetComponentInParent<InvestigationTarget>())throw new InvalidOperationException("Duty interaction missing");
            var previews=new[]{view.dutyRecord,view.householdRegistry,view.secretLetter};
            for(int i=0;i<Models.Length;i++){
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+Models[i]+".prefab");
                if(!prefab||prefab.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"||m.GetFloat("_Metallic")!=0)))throw new InvalidOperationException("Invalid materials");
                if(AssetDatabase.GetAssetPath(previews[i])!=Folder+Models[i]+"Preview.png")throw new InvalidOperationException("Wrong evidence preview");
                File.AppendAllText(Logs+"/checks.txt","PASS "+Models[i]+": prefab, matte material, evidence preview\n");
            }
            File.AppendAllText(Logs+"/checks.txt","PASS duty collision and interaction; registry/letter start hidden; saved scene reopened\nCOMPLETE failures=0\n");
        }
        static void Focus(){var view=EditorWindow.GetWindow<SceneView>();view.LookAt(Find("DutyRecord").transform.position,Quaternion.LookRotation(new Vector3(-.4f,-1,.8f)),1.05f,false,true);view.Repaint();}
    }
}
