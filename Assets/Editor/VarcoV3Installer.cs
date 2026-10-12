using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RedCliffMystery.EditorTools
{
    public static class VarcoV3Installer
    {
        const string Root = "Assets/Art/Characters/VarcoV3";
        static readonly string[] Names = { "CaoCao", "Seolpyeong", "Duseok", "Caimao", "Zhangyun", "RecordsClerk", "DocumentCourier", "ReliefGuard", "GamblingSoldier", "HanJin" };
        static readonly float[] Heights = { 1.78f, 1.70f, 1.78f, 1.80f, 1.77f, 1.68f, 1.72f, 1.76f, 1.70f, 1.74f };
        static Transform Npc(string name) => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == name);

        [MenuItem("적벽추리/v3 인물/적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("Unsaved scene changes; save before replacing visuals.");
            if (scene.path != "Assets/Scenes/Investigation.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/Investigation.unity");
            Directory.CreateDirectory("LocalArchive/BeforeVarcoV3");
            string backup = "LocalArchive/BeforeVarcoV3/Investigation.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new StringBuilder();
            for (int index = 0; index < Names.Length; index++)
            {
                string name = Names[index], folder = Root + "/" + name, path = folder + "/" + name + ".fbx";
                var npc = Npc(name); var old = npc.Find("VarcoVisual");
                if (old == null) throw new InvalidOperationException("Missing original visual: " + name);
                string interactions = InteractionState(npc);
                Vector3 position = old.localPosition; Quaternion rotation = old.localRotation;
                float amplitude = old.GetComponent<VarcoIdleMotion>()?.Amplitude ?? .005f;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException(path);
                importer.isReadable = true; importer.importAnimation = false; importer.addCollider = false;
                importer.bakeAxisConversion = true; importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                string textures = folder + "/Textures";
                if (!AssetDatabase.IsValidFolder(textures)) AssetDatabase.CreateFolder(folder, "Textures");
                importer.ExtractTextures(textures);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                importer.SaveAndReimport();
                var texPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { textures }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
                string normalPath = texPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).StartsWith("normal", StringComparison.OrdinalIgnoreCase));
                if (normalPath != null)
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(normalPath); ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var visual = new GameObject("VarcoVisual"); visual.transform.SetParent(npc, false);
                visual.transform.localPosition = position; visual.transform.localRotation = rotation;
                var instance = Object.Instantiate(model, visual.transform); instance.name = "Model";
                // Blender export already faces visual-local -Z after axis conversion.
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                // Export is meter scale. Preserve height; only ensure feet meet the original floor.
                instance.transform.position += Vector3.up * (visual.transform.position.y - bounds.min.y);
                int mi = 0;
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int m = 0; m < materials.Length; m++)
                    {
                        var source = materials[m];
                        Texture albedo = source != null ? source.mainTexture : null;
                        if (albedo == null)
                            albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texPaths.First(p => Path.GetFileNameWithoutExtension(p).StartsWith("base_color", StringComparison.OrdinalIgnoreCase)));
                        if (albedo == null) throw new InvalidOperationException("Missing texture: " + name);
                        string matPath = folder + "/" + name + "_" + mi++ + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, matPath); }
                        material.SetTexture("_BaseMap", albedo); material.SetColor("_BaseColor", Color.white);
                        material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .18f);
                        material.DisableKeyword("_METALLICSPECGLOSSMAP");
                        if (normalPath != null) { material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)); material.SetFloat("_BumpScale", .35f); material.EnableKeyword("_NORMALMAP"); }
                        materials[m] = material; EditorUtility.SetDirty(material);
                    }
                    renderer.sharedMaterials = materials;
                }
                var motion = visual.AddComponent<VarcoIdleMotion>(); motion.Profile = index; motion.Amplitude = amplitude;
                motion.MouthHeightOverride = .90f;
                Undo.RegisterCreatedObjectUndo(visual, "Apply v3 character");
                Undo.DestroyObjectImmediate(old.gameObject);
                if (interactions != InteractionState(npc)) throw new InvalidOperationException("Interaction changed: " + name);
                PrefabUtility.SaveAsPrefabAsset(visual, folder + "/" + name + "Visual.prefab");
                report.AppendLine(name + " replaced; NPC root/colliders/interaction unchanged; profile=" + index);
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/VarcoV3"); File.WriteAllText("Logs/VarcoV3/apply.txt", report.ToString());
            CorrectStances(); Debug.Log("VARCO_V3_APPLY_COMPLETE 10");
        }
        static string InteractionState(Transform npc) => npc.position + "|" + npc.rotation + "|" + string.Join("|", npc.GetComponents<Component>().Where(c => !(c is Transform)).Select(c => EditorJsonUtility.ToJson(c)));

        [MenuItem("적벽추리/v3 인물/방향과 자세 보정")]
        public static void CorrectStances()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first");
            foreach (string name in Names)
            {
                var visual = Npc(name).Find("VarcoVisual"); var model = visual.Find("Model");
                string folder = Root + "/" + name;
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + name + ".fbx");
                // FBX keeps its Z-up to Y-up conversion on the imported root.
                // Removing that rotation lays the entire character on its back.
                Undo.RecordObject(model, "Restore v3 import axis");
                model.localRotation = imported.transform.localRotation;
                model.localScale = imported.transform.localScale;
                var sources = AssetDatabase.LoadAllAssetsAtPath(folder + "/" + name + ".fbx").OfType<Mesh>().ToArray();
                int index = 0;
                foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    var source = sources.Length == 1 ? sources[0] : sources.Single(m => m.name == filter.sharedMesh.name);
                    var mesh = Object.Instantiate(source);
                    var points = mesh.vertices.Select(v => visual.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
                    float floor = points.Min(p => p.y), height = points.Max(p => p.y) - floor;
                    var hips = new Vector3(0, floor + height * .48f, 0);
                    var lean = name == "DocumentCourier" ? Quaternion.Euler(2.5f,0,0)
                        : name == "GamblingSoldier" ? Quaternion.Euler(0,0,-3.5f)
                        : name == "CaoCao" ? Quaternion.Euler(1.5f,0,0)
                        : name == "Seolpyeong" ? Quaternion.Euler(2,0,0)
                        : name == "Zhangyun" ? Quaternion.Euler(0,0,1.5f) : Quaternion.identity;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        float weight = Mathf.SmoothStep(0,1,Mathf.InverseLerp(floor+height*.30f,floor+height*.64f,p.y));
                        p = Vector3.Lerp(p, hips + lean * (p-hips), weight);
                        points[i] = filter.transform.InverseTransformPoint(visual.TransformPoint(p));
                    }
                    mesh.vertices = points; mesh.RecalculateBounds(); mesh.RecalculateNormals(); mesh.RecalculateTangents();
                    string path = folder + "/" + name + "_RolePose" + index++ + ".asset";
                    mesh.name = Path.GetFileNameWithoutExtension(path);
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing == null) { AssetDatabase.CreateAsset(mesh,path); existing=mesh; }
                    else { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); }
                    Undo.RecordObject(filter,"Set v3 stance"); filter.sharedMesh=existing;
                }
                var bounds = VisualBounds(visual);
                model.localPosition -= Vector3.up * bounds.min.y;
                PrefabUtility.SaveAsPrefabAsset(visual.gameObject,folder+"/"+name+"Visual.prefab");
            }
            AssetDatabase.SaveAssets(); var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Preview();Validate();
        }

        static Bounds VisualBounds(Transform visual)
        {
            var points = visual.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => visual.InverseTransformPoint(f.transform.TransformPoint(v))));
            var bounds = new Bounds(points.First(), Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            return bounds;
        }

        [MenuItem("적벽추리/v3 인물/검증")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run asset validation outside Play mode.");
            var report = new StringBuilder();
            Directory.CreateDirectory("Logs/VarcoV3");
            report.AppendLine("Started " + DateTime.Now.ToString("s") + "; incomplete until COMPLETE appears.");
            File.WriteAllText("Logs/VarcoV3/validation.txt", report.ToString());
            for (int i = 0; i < Names.Length; i++)
            {
                var visual = Npc(Names[i]).Find("VarcoVisual");
                var motion = visual.GetComponent<VarcoIdleMotion>();
                if (motion.Profile != i) throw new Exception("Wrong profile " + Names[i]);
                foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
                    if (!filter.sharedMesh.isReadable || !(AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(Root+"/"+Names[i]+"/")
                        || AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/Art/Characters/Feedback20261012/"+Names[i]+"_Rest")
                        || AssetDatabase.GetAssetPath(filter.sharedMesh)==CharacterBlenderPolish.Output+"/"+Names[i]+".asset")) throw new Exception("Wrong mesh " + Names[i]);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    if (renderer.sharedMaterials.Any(m => m == null || m.shader.name != "Universal Render Pipeline/Lit" || m.GetTexture("_BaseMap") == null)) throw new Exception("Material " + Names[i]);
                var bounds = VisualBounds(visual);
                if (Mathf.Abs(bounds.size.y - Heights[i]) > .04f || Mathf.Abs(bounds.min.y) > .01f || bounds.size.y < bounds.size.z * 2)
                    throw new Exception("Invalid standing axis/height/feet: " + Names[i] + " bounds=" + bounds);
                // Sample a disposable clone, never mutate the saved mesh or live character.
                var clone = Object.Instantiate(visual.gameObject);
                try
                {
                    var sample = clone.GetComponent<VarcoIdleMotion>(); sample.InitializeForPreview(); sample.Sample(0);
                    var mesh = clone.GetComponentInChildren<MeshFilter>().sharedMesh; var rest = mesh.vertices;
                    sample.StartSpeech("아", -.07f); sample.Sample(0); var speaking = mesh.vertices;
                    int changed = rest.Where((v, n) => (v - speaking[n]).sqrMagnitude > 1e-12f).Count();
                    if (changed == 0) throw new Exception("No mouth vertices: " + Names[i]);
                    sample.StartSpeech("아", -10); sample.Sample(0);
                    if (!rest.SequenceEqual(mesh.vertices)) throw new Exception("Speech did not stop: " + Names[i]);
                    sample.Sample(1); if (rest.SequenceEqual(mesh.vertices)) throw new Exception("Idle motion missing: " + Names[i]);
                    report.AppendLine(Names[i] + " PASS standing axis, height=" + bounds.size.y.ToString("F3") + ", feet=" + bounds.min.y.ToString("F4") + ", material, mesh, profile, idle, speech/rest; mouth vertices=" + changed);
                    File.WriteAllText("Logs/VarcoV3/validation.txt", report.ToString());
                }
                finally { Object.DestroyImmediate(clone); }
            }
            report.AppendLine("COMPLETE PASS 10"); File.WriteAllText("Logs/VarcoV3/validation.txt", report.ToString()); Debug.Log("VARCO_V3_VALIDATION_PASS 10");
        }
        [MenuItem("적벽추리/v3 인물/미리보기")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs/VarcoV3");
            for (int row = 0; row < 2; row++)
            {
                var scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var obj = Object.Instantiate(Npc(Names[row * 5 + i]).Find("VarcoVisual").gameObject);
                        SceneManager.MoveGameObjectToScene(obj, scene); obj.transform.position = new Vector3(i * 1.3f, 0, 0); obj.transform.rotation = Quaternion.identity;
                        foreach (var t in obj.GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
                    }
                    var light = new GameObject("Light").AddComponent<Light>(); SceneManager.MoveGameObjectToScene(light.gameObject, scene);
                    light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 31; light.transform.rotation = Quaternion.Euler(30,-25,0);
                    var camera = new GameObject("Camera").AddComponent<Camera>(); SceneManager.MoveGameObjectToScene(camera.gameObject, scene); camera.scene = scene;
                    camera.transform.position = new Vector3(2.6f, .95f, -7); camera.transform.LookAt(new Vector3(2.6f,.95f,0));
                    camera.orthographic = true; camera.orthographicSize = 1.15f; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.2f,.22f,.25f);
                    var rt = new RenderTexture(1800,650,24); var image = new Texture2D(1800,650,TextureFormat.RGB24,false);
                    try { camera.targetTexture = rt; camera.aspect = 1800f/650; camera.Render(); camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,1800,650),0,0); image.Apply(); File.WriteAllBytes("Logs/VarcoV3/row" + row + ".png", image.EncodeToPNG()); }
                    finally { RenderTexture.active = null; camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image); }
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }
    }
}
