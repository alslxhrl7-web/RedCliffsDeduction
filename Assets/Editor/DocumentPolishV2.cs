using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class DocumentPolishV2
    {
        const string Folder="Assets/Art/Evidence/VarcoRefined20261012V2/";
        const string Logs="Logs/DocumentPolishV2";
        static readonly string[] Names={"MaintenanceOrder","PersonnelRecord"};
        static GameObject Find(string name)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name).gameObject;
        static Bounds BoundsOf(GameObject go) {var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
        static void Guard() {var s=SceneManager.GetActiveScene();if(Application.isPlaying||s.isDirty||s.path!="Assets/Scenes/Investigation.unity")throw new InvalidOperationException("Saved Investigation scene in Edit mode required");}
        static void FocusPersonnel()=>Focus("PersonnelRecord");
        static void FocusOrder()=>Focus("MaintenanceOrder");
        static void Focus(string name)
        {
            var root=Find(name);var view=EditorWindow.GetWindow<SceneView>();
            view.LookAt(root.transform.position,Quaternion.LookRotation(new Vector3(-.4f,-1,.8f)),1.05f,false,true);view.Repaint();
        }
        static void ResetTestPath()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
            const string key="RedCliffMystery.TestSaveDirectory";string path=SessionState.GetString(key,"");
            string testRoot=Path.GetFullPath("Temp/OrderChecks")+Path.DirectorySeparatorChar;
            if(!string.IsNullOrEmpty(path)&&Path.GetFullPath(path).StartsWith(testRoot,StringComparison.OrdinalIgnoreCase))SessionState.EraseString(key);
            Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/save-path.txt","Previous override: "+path+"\nCurrent override: "+SessionState.GetString(key,"(none)")+"\n");
        }
        static Color Palette(string name)
        {
            if(name.StartsWith("BambooHoney")) {int i=int.Parse(name.Substring("BambooHoney".Length));return new Color(.48f+i*.025f,.31f+i*.02f,.14f+i*.012f);}
            if(name=="PaperIvory")return new Color(.78f,.69f,.52f);
            if(name=="PaperEdge")return new Color(.66f,.54f,.36f);
            if(name=="HempCord")return new Color(.38f,.29f,.17f);
            return new Color(.10f,.085f,.055f);
        }
        static Material Surface(string name)
        {
            string path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat)return mat;
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            mat.SetColor("_BaseColor",Palette(name));mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.06f);
            // Thin decorative ink has no back face in the source mesh.
            if(name=="InkFaded")mat.SetFloat("_Cull",0);
            AssetDatabase.CreateAsset(mat,path);return mat;
        }
        public static void Apply()
        {
            Guard();Directory.CreateDirectory(Logs);
            string backup=Logs+"/Investigation-before-polish.unity";
            if(!File.Exists(backup))File.Copy(SceneManager.GetActiveScene().path,backup);
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try {
                foreach(string name in Names) {
                    var root=Find(name);if(root.transform.Find("DocumentPolishV2"))continue;
                    var original=root.GetComponent<Renderer>();var desired=original.bounds;
                    float height=name=="MaintenanceOrder"?.028f:.017f;
                    desired.center=new Vector3(desired.center.x,desired.max.y-height/2,desired.center.z);
                    desired.size=new Vector3(desired.size.x,height,desired.size.z);
                    string modelName=name=="MaintenanceOrder"?"MaintenanceOrderRefinedV2":"PersonnelRecordRefined";
                    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+modelName+".fbx");if(!asset)throw new InvalidOperationException("Missing FBX: "+modelName);
                    var wrapper=new GameObject("DocumentPolishV2");Undo.RegisterCreatedObjectUndo(wrapper,"Polish documents");
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);Undo.RegisterCreatedObjectUndo(model,"Polish documents");model.transform.SetParent(wrapper.transform,false);
                    foreach(var r in model.GetComponentsInChildren<Renderer>()) {
                        r.sharedMaterials=r.sharedMaterials.Select(m=>Surface(m.name)).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                    var b=BoundsOf(model);if(b.size.y>.05f)throw new InvalidOperationException("Unexpected document orientation");
                    wrapper.transform.localScale=new Vector3(desired.size.x/b.size.x,desired.size.y/b.size.y,desired.size.z/b.size.z);
                    wrapper.transform.position+=desired.center-BoundsOf(model).center;wrapper.transform.SetParent(root.transform,true);
                    Undo.RecordObject(original,"Retain previous document");original.enabled=false;
                    var previous=root.transform.Find("RefinedDocument20261012");if(previous){Undo.RecordObject(previous.gameObject,"Retain previous military order");previous.gameObject.SetActive(false);}
                    foreach(Transform child in root.transform.parent) {
                        bool legacyInk=name=="MaintenanceOrder"?child.name.StartsWith("Writing"):child.name.StartsWith("Writing-1");
                        var renderer=child.GetComponent<Renderer>();if(legacyInk&&renderer){Undo.RecordObject(renderer,"Replace block ink with fine strokes");renderer.enabled=false;}
                    }
                }
                Undo.CollapseUndoOperations(group);AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed");
            }catch{Undo.RevertAllDownToGroup(group);throw;}
            File.WriteAllText(Logs+"/apply.txt","Applied both models; original renderers and previous revision retained. Run saved-scene validation after imports settle.");
        }
        public static void Validate()
        {
            Guard();EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/checks.txt","Document polish V2\n");
            foreach(string name in Names) {
                var root=Find(name);var replacement=root.transform.Find("DocumentPolishV2");
                if(!replacement||!replacement.gameObject.activeInHierarchy||root.GetComponent<Renderer>().enabled)throw new InvalidOperationException("Model missing: "+name);
                var rr=replacement.GetComponentsInChildren<Renderer>();if(rr.Length==0||rr.Any(r=>r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit"||m.GetFloat("_Metallic")!=0)))throw new InvalidOperationException("Material error");
                var collider=root.GetComponent<Collider>();Physics.SyncTransforms();
                if(!collider||!collider.enabled||!collider.Raycast(new Ray(collider.bounds.center+Vector3.up,Vector3.down),out var hit,2)||!collider.GetComponentInParent<InvestigationTarget>())throw new InvalidOperationException("Clue interaction lost");
                if(BoundsOf(replacement.gameObject).size.y>.04f)throw new InvalidOperationException("Document too thick");
                File.AppendAllText(Logs+"/checks.txt","PASS "+name+": saved model, matte URP materials, thin proportions, collider ray and investigation target retained\n");
            }
            if(!Find("MaintenanceOrderTable").transform.Find("CommandSeal"))throw new InvalidOperationException("Existing seal lost");
            File.AppendAllText(Logs+"/checks.txt","PASS original seal retained; scene reopened\nCOMPLETE failures=0\n");
        }
    }
}
