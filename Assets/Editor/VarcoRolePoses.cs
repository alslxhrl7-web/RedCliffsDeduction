using System.IO;
using System.Text;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class VarcoRolePoses
    {
        static readonly string[] Names = { "CaoCao", "Seolpyeong", "Duseok", "Caimao", "Zhangyun", "RecordsClerk", "DocumentCourier", "ReliefGuard", "GamblingSoldier" };
        // Keep the original v2 pose installer limited to its nine source models.
        static readonly string[] InspectionNames = Names.Concat(new[] { "HanJin" }).ToArray();
        [MenuItem("적벽추리/전체 인물 포즈와 대기 움직임 적용")]
        public static void ApplyAll() => Apply();
        [MenuItem("적벽추리/대화 화자 연결 검증")]
        public static void VerifySpeakerRouting()
        {
            if (!EditorApplication.isPlaying) throw new System.Exception("Run in Play Mode");
            var report = new StringBuilder();
            var motions = Object.FindObjectsByType<RedCliffMystery.VarcoIdleMotion>();
            if (motions.Length != InspectionNames.Length
                || Enumerable.Range(0, InspectionNames.Length).Any(profile => motions.Count(m => m.Profile == profile) != 1))
                throw new System.Exception("Expected one character for each of the ten speaker profiles");
            var session = Object.FindAnyObjectByType<RedCliffMystery.Investigation.InvestigationSession>();
            var modal = session.GetType().GetMethod("SetModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            string[] speakers = { "조조", "설평", "두석", "채모", "장윤", "기록 담당자", "전령", "교대 병사", "동석 병사", "한진", "병사" };
            foreach (var speaker in speakers)
            {
                modal.Invoke(session, new object[] { "대화 검증", speaker, "“아아아아아아아아아아아아”", "검증" });
                int expected = speaker == "병사" ? 8 : System.Array.IndexOf(speakers, speaker);
                if (motions.Count(m => m.HasSpeech) != 1 || !motions.Single(m => m.Profile == expected).HasSpeech)
                    throw new System.Exception("Wrong dialogue assignment: " + speaker);
                foreach (var motion in motions) if (motion.Profile == expected) motion.StartSpeech("아아아아아아아아아아아아", Time.time - .07f);
                if (motions.Count(m => m.IsSpeaking) != 1 || !motions.Single(m => m.Profile == expected).IsSpeaking)
                    throw new System.Exception("Wrong speaker: " + speaker);
                report.AppendLine(speaker + " PASS only selected character speaks");
            }
            modal.Invoke(session, new object[] { "기록", "조조", "기록 내용을 확인한다.", "검증" });
            if (motions.Any(m => m.HasSpeech)) throw new System.Exception("Record panel still speaking");
            var sequence = session.GetType().GetMethod("BeginSequence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sequence.Invoke(session, new object[] { "조조", new[] { "아", "오" }, null, false });
            session.AdvanceDialogue();
            if (motions.Count(m => m.HasSpeech) != 1) throw new System.Exception("Line transition lost speaker");
            session.AdvanceDialogue();
            if (motions.Any(m => m.HasSpeech)) throw new System.Exception("Finished sequence still speaking");
            sequence.Invoke(session, new object[] { "조조", new[] { "아" }, null, true });
            if (motions.Any(m => m.HasSpeech)) throw new System.Exception("Player name collision moved NPC mouth");
            RedCliffMystery.VarcoIdleMotion.StopSpeech();
            report.AppendLine("PASS record panel and explicit stop close all mouths");
            report.AppendLine("PASS line advance, sequence completion, player/NPC name collision");
            Directory.CreateDirectory("Logs/VarcoPreviews");
            File.WriteAllText("Logs/VarcoPreviews/speaker-check.txt", report.ToString());
        }
        [MenuItem("적벽추리/대화 입모양 검증")]
        public static void VerifySpeech()
        {
            var report = new StringBuilder();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var motion in Object.FindObjectsByType<RedCliffMystery.VarcoIdleMotion>())
                {
                    var clone = Object.Instantiate(motion.gameObject);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    var sample = clone.GetComponent<RedCliffMystery.VarcoIdleMotion>();
                    sample.InitializeForPreview(); sample.Sample(0);
                    var filter = clone.GetComponentInChildren<MeshFilter>();
                    var closed = filter.sharedMesh.vertices;
                    sample.StartSpeech("아", -.07f); sample.Sample(0);
                    var opened = filter.sharedMesh.vertices;
                    int changed = 0;
                    for (int i = 0; i < closed.Length; i++) if ((closed[i] - opened[i]).sqrMagnitude > 1e-12f) changed++;
                    if (changed == 0) throw new System.Exception("Mouth vertices missing for profile " + sample.Profile);
                    sample.StartSpeech("아", -10); sample.Sample(0);
                    if (!closed.SequenceEqual(filter.sharedMesh.vertices)) throw new System.Exception("Mouth did not return to rest");
                    report.AppendLine(InspectionNames[sample.Profile] + " PASS mouth vertices=" + changed + ", returns to rest");
                    Object.DestroyImmediate(clone);
                }
                if (RedCliffMystery.VarcoIdleMotion.MouthShape("마", .01f) != Vector2.zero
                    || RedCliffMystery.VarcoIdleMotion.MouthShape(".", .07f) != Vector2.zero
                    || RedCliffMystery.VarcoIdleMotion.MouthShape("오", .07f).y >= 0
                    || RedCliffMystery.VarcoIdleMotion.MouthShape("아", .07f).y <= 0)
                    throw new System.Exception("Korean mouth shape check failed");
                report.AppendLine("PASS Korean closed consonant, rounded/wide vowels, punctuation pause");
                Directory.CreateDirectory("Logs/VarcoPreviews");
                File.WriteAllText("Logs/VarcoPreviews/speech-check.txt", report.ToString());
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        [MenuItem("적벽추리/인물 대기 움직임 검증")]
        public static void VerifyMovement()
        {
            var report = new StringBuilder();
            var characters = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => InspectionNames.Contains(t.name) && t.Find("VarcoVisual") != null).ToArray();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var npc in characters)
                {
                    var clone = Object.Instantiate(npc.Find("VarcoVisual").gameObject);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    var motion = clone.GetComponent<RedCliffMystery.VarcoIdleMotion>();
                    if (motion == null) throw new System.Exception("Missing idle motion: " + npc.name);
                    var filter = clone.GetComponentInChildren<MeshFilter>();
                    var original = filter.sharedMesh;
                    var rest = original.vertices;
                    motion.InitializeForPreview(); motion.Sample(0);
                    var first = filter.sharedMesh.vertices;
                    motion.Sample(1.7f); var second = filter.sharedMesh.vertices;
                    float moving = 0, planted = 0;
                    float floor = rest.Min(v => clone.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).y);
                    float top = rest.Max(v => clone.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).y);
                    for (int i = 0; i < first.Length; i++)
                    {
                        moving = Mathf.Max(moving, Vector3.Distance(first[i], second[i]));
                        float y = clone.transform.InverseTransformPoint(filter.transform.TransformPoint(rest[i])).y;
                        if (y < floor + (top - floor) * .3f) planted = Mathf.Max(planted, Vector3.Distance(rest[i], second[i]));
                    }
                    if (moving <= .000001f || planted > .0001f || !original.vertices.SequenceEqual(rest))
                        throw new System.Exception("Idle movement validation failed: " + npc.name);
                    report.AppendLine(npc.name + " PASS moved=" + moving + " feet=" + planted);
                    Object.DestroyImmediate(clone);
                }
                Directory.CreateDirectory("Logs/VarcoPreviews");
                File.WriteAllText("Logs/VarcoPreviews/idle-check.txt", report.ToString());
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        [MenuItem("적벽추리/추가 인물 직책 포즈 적용")]
        public static void Apply() => Apply(false);

        [MenuItem("적벽추리/서기 팔 자세 수정")]
        public static void ApplyClerkPoses() => Apply(true);

        static void Apply(bool clerksOnly)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Investigation.unity" || scene.isDirty || EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Open the saved Investigation scene outside Play Mode.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var npc in root.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Array.IndexOf(Names, npc.name) < 0) continue;
                    if (clerksOnly && npc.name != "RecordsClerk" && npc.name != "Seolpyeong") continue;
                    var visual = npc.Find("VarcoVisual");
                    if (visual == null) continue;
                    string folder = "Assets/Art/Characters/Varco/" + npc.name;
                    var originals = AssetDatabase.LoadAllAssetsAtPath(folder + "/" + npc.name + ".fbx").OfType<Mesh>().ToArray();
                    int index = 0;
                    foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var source = originals.FirstOrDefault(m => m.name == filter.sharedMesh.name) ?? originals.First();
                        var mesh = Object.Instantiate(source);
                        mesh.name = source.name;
                        var vertices = mesh.vertices;
                        var points = vertices.Select(v => visual.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
                        float height = points.Max(p => p.y) - points.Min(p => p.y);
                        float floor = points.Min(p => p.y);
                        bool tPose = points.Max(p => p.x) - points.Min(p => p.x) > height * .8f;
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector3 p = points[i];
                            if (tPose)
                            {
                                float side = Mathf.Sign(p.x);
                                float arm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(height * .10f, height * .22f, Mathf.Abs(p.x)));
                                arm *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(height * .85f, height * .94f, p.y - floor));
                                arm *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(height * .53f, height * .66f, p.y - floor));
                                var shoulder = new Vector3(side * height * .115f, floor + height * .79f, 0);
                                // These meshes have no arm rig. Keep wrist and sleeve together;
                                // folding vertices around an estimated elbow twists the hands.
                                p = Vector3.Lerp(p, shoulder + Quaternion.AngleAxis(-side * 68f, Vector3.forward) * (p - shoulder), arm);
                            }
                            if (npc.name == "DocumentCourier" || npc.name == "GamblingSoldier")
                            {
                                float side = npc.name == "DocumentCourier" ? 1 : -1;
                                float hand = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(height * .12f, height * .19f, p.x * side));
                                hand *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(floor + height * .54f, floor + height * .71f, p.y));
                                hand *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(floor + height * .33f, floor + height * .42f, p.y));
                                p += new Vector3(-side * .035f, npc.name == "DocumentCourier" ? .08f : .035f, -.055f) * height * hand;
                            }
                            // Small whole-body stance changes leave the feet planted and interaction roots intact.
                            float upper = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(floor + height * .30f, floor + height * .64f, p.y));
                            var hips = new Vector3(0, floor + height * .48f, 0);
                            Quaternion lean = npc.name == "DocumentCourier" ? Quaternion.Euler(2.5f, 0, 0)
                                : npc.name == "GamblingSoldier" ? Quaternion.Euler(0, 0, -3.5f)
                                : npc.name == "CaoCao" ? Quaternion.Euler(1.5f, 0, 0)
                                : npc.name == "Seolpyeong" ? Quaternion.Euler(2, 0, 0)
                                : npc.name == "Zhangyun" ? Quaternion.Euler(0, 0, 1.5f) : Quaternion.identity;
                            p = Vector3.Lerp(p, hips + lean * (p - hips), upper);
                            vertices[i] = filter.transform.InverseTransformPoint(visual.TransformPoint(p));
                        }
                        mesh.vertices = vertices;
                        mesh.RecalculateBounds(); mesh.RecalculateNormals(); mesh.RecalculateTangents();
                        string path = folder + "/" + npc.name + "_RolePose" + index++ + ".asset";
                        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
                        else
                        {
                            Undo.RecordObject(existing, "Update role pose mesh");
                            existing.Clear(); existing.vertices = mesh.vertices; existing.normals = mesh.normals;
                            existing.uv = mesh.uv; existing.tangents = mesh.tangents; existing.colors = mesh.colors;
                            existing.subMeshCount = mesh.subMeshCount;
                            for (int sub = 0; sub < mesh.subMeshCount; sub++) existing.SetTriangles(mesh.GetTriangles(sub), sub);
                            existing.bounds = mesh.bounds;
                            Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing);
                        }
                        var meshSettings = new SerializedObject(existing);
                        meshSettings.FindProperty("m_IsReadable").boolValue = true;
                        meshSettings.FindProperty("m_KeepVertices").boolValue = true;
                        meshSettings.FindProperty("m_KeepIndices").boolValue = true;
                        meshSettings.ApplyModifiedPropertiesWithoutUndo();
                        Undo.RecordObject(filter, "Set role pose"); filter.sharedMesh = existing;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                    }
                    Undo.RecordObject(visual, "Set role stance");
                    visual.localRotation = Quaternion.Euler(0, npc.name == "ReliefGuard" || npc.name == "Duseok" ? 8
                        : npc.name == "GamblingSoldier" ? -7 : npc.name == "Zhangyun" ? -4 : npc.name == "Caimao" ? 4 : 0, 0);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                    var motion = visual.GetComponent<RedCliffMystery.VarcoIdleMotion>();
                    if (motion == null) motion = Undo.AddComponent<RedCliffMystery.VarcoIdleMotion>(visual.gameObject);
                    Undo.RecordObject(motion, "Set character idle movement");
                    motion.Profile = System.Array.IndexOf(Names, npc.name);
                    motion.Amplitude = npc.name == "CaoCao" || npc.name == "Caimao" ? .003f
                        : npc.name == "Duseok" || npc.name == "ReliefGuard" ? .004f
                        : npc.name == "GamblingSoldier" ? .007f : .005f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(motion);
                    Debug.Log("VARCO_ROLE_POSE " + npc.name);
                }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Preview();
        }

        [MenuItem("적벽추리/추가 인물 포즈 미리보기")]
        public static void Preview()
        {
            var originalScene = SceneManager.GetActiveScene();
            var characters = originalScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => InspectionNames.Contains(t.name) && t.Find("VarcoVisual") != null).OrderBy(t => System.Array.IndexOf(InspectionNames, t.name)).ToArray();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    var clone = Object.Instantiate(characters[i].Find("VarcoVisual").gameObject); SceneManager.MoveGameObjectToScene(clone, preview);
                    clone.transform.position = new Vector3(i * 1.6f, 0, 0); clone.transform.rotation = Quaternion.identity;
                    foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                }
                var light = new GameObject("RolePreviewLight").AddComponent<Light>();
                SceneManager.MoveGameObjectToScene(light.gameObject, preview);
                light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 31; light.transform.rotation = Quaternion.Euler(30, -25, 0);
                var camera = new GameObject("RolePreviewCamera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, preview); camera.scene = preview;
                camera.orthographic = true; camera.orthographicSize = 1.3f;
                camera.cullingMask = 1 << 31;
                camera.backgroundColor = new Color(.2f, .22f, .25f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3((characters.Length - 1) * .8f, 1.02f, -7);
                camera.transform.LookAt(new Vector3(camera.transform.position.x, 1.02f, 0));
                var target = new RenderTexture(Mathf.Max(800, characters.Length * 500), 800, 24);
                var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target; camera.aspect = target.width / (float)target.height;
                    camera.Render(); camera.Render(); camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                    Directory.CreateDirectory("Logs/VarcoPreviews"); File.WriteAllBytes("Logs/VarcoPreviews/role-poses.png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = null; camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        [MenuItem("적벽추리/인물 포즈 구조 확인")]
        public static void Inspect()
        {
            var report = new StringBuilder();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var npc in root.GetComponentsInChildren<Transform>(true))
                    if (System.Array.IndexOf(InspectionNames, npc.name) >= 0)
                    {
                        report.AppendLine(npc.name);
                        var visual = npc.Find("VarcoVisual");
                        if (visual == null) { report.AppendLine("NO_VISUAL"); continue; }
                        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                        {
                            var skinned = r as SkinnedMeshRenderer;
                            var filter = r.GetComponent<MeshFilter>();
                            var mesh = skinned != null ? skinned.sharedMesh : filter != null ? filter.sharedMesh : null;
                            report.AppendLine(r.name + " type=" + r.GetType().Name + " bounds=" + r.bounds + " bones=" + (skinned == null ? 0 : skinned.bones.Length) + " vertices=" + (mesh == null ? 0 : mesh.vertexCount));
                        }
                    }
            Directory.CreateDirectory("Logs/VarcoPreviews");
            File.WriteAllText("Logs/VarcoPreviews/pose-structure.txt", report.ToString());
        }
    }
}
