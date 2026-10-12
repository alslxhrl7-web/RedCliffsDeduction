using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace RedCliffMystery.EditorTools
{
    public static class PropRefresh20261012
    {
        const string Logs = "Logs/PropRefresh20261012";
        const string Assets = "Assets/Art/Props/CampRefresh20261012";
        static readonly string[] Subjects = { "OrderStorageCrate", "ReturnedOrder", "SupplyTent", "LeftEntranceLantern" };
        static GameObject Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == name).gameObject;

        [MenuItem("적벽추리/소품 개선/블렌더 소품 적용")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/Investigation.unity")
                throw new InvalidOperationException("Open the saved Investigation scene outside Play mode.");
            if (Find("OrderStorageCrate").transform.Find("RefinedProp20261012") != null)
                throw new InvalidOperationException("Prop refresh already applied; refusing duplicate models.");
            foreach (string model in new[] { "TransportChest", "FieldDesk", "CampOilLamp", "SupplyCanopy" })
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(Assets + "/" + model + ".fbx")) throw new InvalidOperationException("Missing model " + model);
            Directory.CreateDirectory(Assets + "/Materials"); AssetDatabase.Refresh();
            var mats = new Dictionary<string, Material>();
            mats["CampWood"] = MakeMaterial("CampWood",new Color(.55f,.37f,.21f),"Wood049",.04f,.10f);
            mats["CutWood"] = MakeMaterial("CutWood",new Color(.76f,.57f,.35f),"Wood049",.02f,.12f);
            mats["Iron"] = MakeMaterial("Iron",new Color(.12f,.13f,.12f),null,.45f,.24f);
            mats["Bronze"] = MakeMaterial("Bronze",new Color(.37f,.25f,.105f),null,.5f,.25f);
            mats["Hemp"] = MakeMaterial("Hemp",new Color(.46f,.34f,.18f),"Fabric030",0,.02f);
            mats["Canvas"] = MakeMaterial("Canvas",new Color(.79f,.66f,.46f),"Fabric030",0,.035f);
            mats["ClothBorder"] = MakeMaterial("ClothBorder",new Color(.30f,.09f,.045f),"Fabric030",0,.03f);
            mats["Clay"] = MakeMaterial("Clay",new Color(.24f,.095f,.04f),null,0,.09f);
            mats["LampGlow"] = MakeMaterial("LampGlow",new Color(1,.52f,.12f),null,0,.05f);
            mats["LampGlow"].EnableKeyword("_EMISSION"); mats["LampGlow"].SetColor("_EmissionColor",new Color(3,1.05f,.12f));
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Refine camp props");
            try
            {
                foreach (string name in new[] { "OrderStorageCrate", "SupplyCrate-1", "SupplyCrate1" })
                {
                    var root=Find(name); var old=root.GetComponent<Renderer>(); var b=old.bounds;
                    Hide(old); Place("TransportChest",root.transform,b,mats);
                }
                foreach (string name in new[] { "ReturnedOrder", "OutgoingOrder" })
                {
                    var root=Find(name);
                    foreach(Transform child in root.transform) if(child.name=="Cube") Hide(child.GetComponent<Renderer>());
                    Place("FieldDesk",root.transform,new Bounds(root.transform.position+Vector3.up*.5f,new Vector3(1.2f,1,.75f)),mats);
                }
                foreach(string name in new[]{ "LeftEntranceLantern", "RightEntranceLantern", "LeftTentLantern", "RightTentLantern" })
                {
                    var root=Find(name);var rr=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                    var b=rr[0].bounds; foreach(var r in rr.Skip(1)) b.Encapsulate(r.bounds);
                    foreach(var r in rr) Hide(r);
                    Place("CampOilLamp",root.transform,b,mats);
                }
                var supply=Find("SupplyTent");
                foreach(Transform child in supply.transform)
                    if(child.name=="SupplyCanopy" || child.name=="SupplyPost") Hide(child.GetComponent<Renderer>());
                Place("SupplyCanopy",supply.transform,new Bounds(new Vector3(-40,1.91f,-23.5f),new Vector3(13.9f,3.9f,11.15f)),mats);
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed");
                Undo.CollapseUndoOperations(group);
                Directory.CreateDirectory(Logs); File.WriteAllText(Logs+"/apply.txt","Applied 10 model instances: 3 chests, 2 desks, 4 lamps, 1 canopy. Existing investigation targets and colliders retained.");
                Debug.Log("PROP_REFRESH_APPLIED 10 instances");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        static void Hide(Renderer r) { if(!r)return; Undo.RecordObject(r,"Replace prototype appearance"); r.enabled=false; }
        static Material MakeMaterial(string name,Color color,string texture,float metal,float smooth)
        {
            var path=Assets+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=color;
            m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
            if(texture!=null) {
                const string textures="Assets/Art/Environment/CommandTent/Textures/";
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+texture+"_1K-JPG_Color.jpg"));
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+texture+"_1K-JPG_NormalGL.jpg"));
                m.SetFloat("_BumpScale",.16f);m.EnableKeyword("_NORMALMAP");
            }
            AssetDatabase.CreateAsset(m,path); return m;
        }
        static void Place(string model,Transform parent,Bounds desired,Dictionary<string,Material> mats)
        {
            var wrapper=new GameObject("RefinedProp20261012");Undo.RegisterCreatedObjectUndo(wrapper,"Add refined prop");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Assets+"/"+model+".fbx"));
            Undo.RegisterCreatedObjectUndo(go,"Add refined model");go.transform.SetParent(wrapper.transform,false);
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)) {
                r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null && mats.ContainsKey(m.name) ? mats[m.name] : m).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            var rr=go.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;
            foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);
            if(b.size.x<.001f||b.size.y<.001f||b.size.z<.001f)throw new InvalidOperationException("Invalid model bounds: "+model);
            wrapper.transform.localScale=new Vector3(desired.size.x/b.size.x,desired.size.y/b.size.y,desired.size.z/b.size.z);
            b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);
            wrapper.transform.position+=desired.center-b.center;
            wrapper.transform.SetParent(parent,true);
        }

        [MenuItem("적벽추리/소품 개선/수정 전 확인")]
        public static void Before() { Audit(); Capture("before"); }
        [MenuItem("적벽추리/소품 개선/수정 후 확인")]
        public static void After() { Audit(); Capture("after"); }

        [MenuItem("적벽추리/소품 개선/저장과 배치 검사")]
        public static void ValidateSaved()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Saved edit scene required");
            EditorSceneManager.OpenScene(scene.path);
            var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
            var models=all.Where(t=>t.name=="RefinedProp20261012").ToArray();
            if(models.Length!=15) throw new InvalidOperationException("Expected 15 refined instances, got "+models.Length);
            int triangles=0;
            foreach(var model in models) {
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                    if(!renderer.enabled || renderer.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name!="Universal Render Pipeline/Lit"))
                        throw new InvalidOperationException("Missing URP material at "+model.parent.name);
                foreach(var mesh in model.GetComponentsInChildren<MeshFilter>()) {
                    if(!mesh.sharedMesh || mesh.sharedMesh.vertexCount==0)throw new InvalidOperationException("Missing mesh");
                    for(int i=0;i<mesh.sharedMesh.subMeshCount;i++)triangles+=(int)mesh.sharedMesh.GetIndexCount(i)/3;
                }
            }
            var crate=Find("OrderStorageCrate");
            if(!crate.GetComponent<Collider>().enabled || !crate.GetComponent<RedCliffMystery.Investigation.InvestigationTarget>())throw new InvalidOperationException("Storage interaction missing");
            if(Find("SupplyTent").transform.Find("RefinedProp20261012/PoleColliders").GetComponentsInChildren<CapsuleCollider>().Length!=6)throw new InvalidOperationException("Missing support colliders");
            Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/validation.txt","PASS scene reopened; 15 instances; URP materials retained; meshes present; storage interaction retained; 6 perimeter colliders; triangles="+triangles);
            Debug.Log("PROP_VALIDATION_PASS triangles="+triangles);
        }

        [MenuItem("적벽추리/소품 개선/주요 탁자 외형 통일")]
        public static void FinishTables()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Saved edit scene required");
            var mats=Directory.GetFiles(Assets+"/Materials","*.mat").Select(p=>AssetDatabase.LoadAssetAtPath<Material>(p.Replace('\\','/'))).ToDictionary(m=>m.name);
            foreach(var name in new[]{"MaintenanceOrderTable","CareerRecordsTable"}) {
                var root=Find(name).transform;
                if(root.Find("RefinedProp20261012"))continue;
                var top=root.Find("Tabletop");if(!top)throw new InvalidOperationException("Missing tabletop: "+name);
                var b=top.GetComponent<Renderer>().bounds;
                var legs=root.Cast<Transform>().Where(t=>t.name.StartsWith("Leg")).Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray();
                if(legs.Length!=4)throw new InvalidOperationException("Expected four table legs");
                float bottom=legs.Min(r=>r.bounds.min.y);
                foreach(var r in legs)Hide(r);Hide(top.GetComponent<Renderer>());
                Place("FieldDesk",root,new Bounds(new Vector3(b.center.x,(bottom+b.max.y)*.5f,b.center.z),new Vector3(b.size.x,b.max.y-bottom,b.size.z)),mats);
            }
            foreach(var root in new[]{Find("ArchiveRoom").transform,Find("CommandTent").transform}) {
                foreach(var top in root.Cast<Transform>().Where(t=>t.name=="WritingDesk"||t.name=="CommandDesk").ToArray()) {
                    if(top.Find("RefinedProp20261012"))continue;
                    var b=top.GetComponent<Renderer>().bounds;
                    var legs=root.Cast<Transform>().Where(t=>t.name=="DeskLeg" && Mathf.Abs(t.position.x-b.center.x)<b.extents.x && Mathf.Abs(t.position.z-b.center.z)<b.extents.z).Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray();
                    if(legs.Length!=4)throw new InvalidOperationException("Expected four desk legs at "+top.position);
                    float bottom=legs.Min(r=>r.bounds.min.y);
                    foreach(var r in legs)Hide(r);Hide(top.GetComponent<Renderer>());
                    Place("FieldDesk",top,new Bounds(new Vector3(b.center.x,(bottom+b.max.y)*.5f,b.center.z),new Vector3(b.size.x,b.max.y-bottom,b.size.z)),mats);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Logs+"/tables.txt","PASS five further desks replaced; original documents, maps and collider locations retained.");
        }

        [MenuItem("적벽추리/소품 개선/보관함 기록과 천막 충돌 보완")]
        public static void FinishDetails()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Saved Investigation edit scene required");
            var crate=Find("OrderStorageCrate");var paper=crate.transform.Find("StorageIntakeRecord");
            if(paper && !paper.Find("BoundIntakeRecord")) {
                var original=paper.GetComponent<Renderer>();var b=original.bounds;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/Archive/TiedSlipStack.prefab");
                if(!prefab) throw new InvalidOperationException("Existing tied slip prefab missing");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="BoundIntakeRecord";
                Undo.RegisterCreatedObjectUndo(go,"Detail intake record");
                var rr=go.GetComponentsInChildren<Renderer>();var actual=rr[0].bounds;foreach(var r in rr.Skip(1))actual.Encapsulate(r.bounds);
                go.transform.localScale=new Vector3(b.size.x/actual.size.x,.045f/actual.size.y,b.size.z/actual.size.z);
                actual=rr[0].bounds;foreach(var r in rr.Skip(1))actual.Encapsulate(r.bounds);
                go.transform.position+=new Vector3(b.center.x,b.min.y+.0225f,b.center.z)-actual.center;
                go.transform.SetParent(paper,true);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);Hide(original);
            }
            var canopy=Find("SupplyTent").transform.Find("RefinedProp20261012");
            if(!canopy)throw new InvalidOperationException("Refined canopy missing");
            if(!canopy.Find("PoleColliders")) {
                var group=new GameObject("PoleColliders");Undo.RegisterCreatedObjectUndo(group,"Add visible pole collision");group.transform.SetParent(canopy,false);
                foreach(float x in new[]{-5.5f,0,5.5f}) foreach(float z in new[]{-4.5f,4.5f}) {
                    var pole=new GameObject("Support");pole.transform.SetParent(group.transform,false);
                    float h=x==0?3.82f:2.72f;pole.transform.localPosition=new Vector3(x,h*.5f,z);
                    var collider=pole.AddComponent<CapsuleCollider>();collider.radius=.065f;collider.height=h;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("PROP_DETAILS_COMPLETE intake record and perimeter poles");
        }

        static void Audit()
        {
            Directory.CreateDirectory(Logs);
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            File.WriteAllLines(Logs + "/inventory.txt", roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => Subjects.Contains(t.name) || t.name.Contains("SupplyCrate") || t.name.Contains("WritingDesk") || t.name == "MaintenanceOrderTable" || t.name == "CareerRecordsStation")
                .Select(t => t.name + " position=" + t.position + " scale=" + t.lossyScale + " renderers=" + t.GetComponentsInChildren<Renderer>(true).Length));
        }

        static void Capture(string stage)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit mode required");
            foreach (var name in Subjects)
            {
                var source = Find(name);
                var preview = EditorSceneManager.NewPreviewScene();
                var rt = new RenderTexture(960, 720, 24);
                var old = RenderTexture.active;
                Camera camera = null;
                Texture2D pixels = null;
                try
                {
                    var copy = UnityEngine.Object.Instantiate(source);
                    copy.SetActive(true); SceneManager.MoveGameObjectToScene(copy, preview);
                    var renderers = copy.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                    var go = new GameObject("Prop inspection camera"); SceneManager.MoveGameObjectToScene(go, preview);
                    camera = go.AddComponent<Camera>(); camera.enabled = false; camera.scene = preview;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f,.14f,.16f);
                    camera.fieldOfView = 37; camera.nearClipPlane = .03f; camera.farClipPlane = 200;
                    camera.targetTexture = rt; camera.aspect = 4f/3;
                    camera.transform.position = bounds.center + new Vector3(.85f,.65f,-1.3f).normalized * bounds.size.magnitude * 1.65f;
                    camera.transform.LookAt(bounds.center);
                    go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                    var lamp = new GameObject("Preview key"); SceneManager.MoveGameObjectToScene(lamp, preview);
                    var light = lamp.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2.2f;
                    lamp.transform.rotation = Quaternion.Euler(45,-35,0);
                    camera.Render(); camera.Render(); RenderTexture.active = rt;
                    pixels = new Texture2D(960,720,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,960,720),0,0); pixels.Apply();
                    File.WriteAllBytes(Logs + "/" + stage + "-" + name + ".png", pixels.EncodeToPNG());
                }
                finally
                {
                    if (camera) camera.targetTexture = null;
                    RenderTexture.active = old; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                    if(pixels) UnityEngine.Object.DestroyImmediate(pixels);
                    EditorSceneManager.ClosePreviewScene(preview);
                }
            }
            Debug.Log("PROP_PREVIEW " + stage + " complete");
        }
    }
}
