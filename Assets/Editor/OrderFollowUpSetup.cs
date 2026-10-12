using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

public static class OrderFollowUpSetup
{
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first");
        var targets = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include);
        if (!targets.Any(t => t.kind == InvestigationTarget.TargetKind.HanJin))
        {
            var source = targets.Single(t => t.kind == InvestigationTarget.TargetKind.RecordsClerk);
            var han = UnityEngine.Object.Instantiate(source.gameObject, GameObject.Find("ArchiveRoom").transform);
            Undo.RegisterCreatedObjectUndo(han, "Add Han Jin");
            han.name = "HanJin";
            han.transform.position = new Vector3(-3.2f, 0, 2.7f);
            han.GetComponent<InvestigationTarget>().kind = InvestigationTarget.TargetKind.HanJin;
            foreach (var label in han.GetComponentsInChildren<TextMesh>(true)) label.text = "한진";
            foreach (var motion in han.GetComponentsInChildren<RedCliffMystery.VarcoIdleMotion>(true)) motion.Profile = 9;
        }
        AddStation(InvestigationTarget.TargetKind.ReturnedOrder, "ReturnedOrder", new Vector3(-3.2f, 0, 1.4f));
        AddStation(InvestigationTarget.TargetKind.OutgoingOrder, "OutgoingOrder", new Vector3(1.1f, 0, -22f));
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Inspect();
    }

    static void AddStation(InvestigationTarget.TargetKind kind, string name, Vector3 position)
    {
        if (UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include).Any(t => t.kind == kind)) return;
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Add investigation station");
        root.transform.position = position;
        root.AddComponent<InvestigationTarget>().kind = kind;
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .5f, 0); collider.size = new Vector3(1.2f, 1, .75f);
        var timber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/DarkTimber.mat");
        Part(root.transform, new Vector3(0,.95f,0), new Vector3(1.2f,.1f,.75f), timber);
        foreach (float x in new[] { -.48f, .48f })
            foreach (float z in new[] { -.27f, .27f })
                Part(root.transform, new Vector3(x,.45f,z), new Vector3(.09f,.9f,.09f), timber);
        var ledger = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/Archive/OpenSlipLedger.prefab"));
        ledger.transform.SetParent(root.transform, false);
        ledger.transform.localPosition = new Vector3(0,1.01f,0);
        ledger.transform.localScale = Vector3.one * 1.5f;
        foreach (var c in ledger.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
    }

    static void Part(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        if (material) part.GetComponent<Renderer>().sharedMaterial = material;
    }
    public static void Inspect()
    {
        Directory.CreateDirectory("Logs/OrderFollowUp");
        File.WriteAllLines("Logs/OrderFollowUp/scene.txt", UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include)
            .Select(t => t.kind + " name=" + t.name + " world=" + t.transform.position + " local=" + t.transform.localPosition + " parent=" + t.transform.parent?.name));
        File.WriteAllLines("Logs/OrderFollowUp/colliders.txt", UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include)
            .SelectMany(t => t.GetComponentsInChildren<Collider>(true).Select(c => t.kind + " " + c.name + " enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy + " center=" + c.bounds.center + " size=" + c.bounds.size)));
    }

    public static void VerifyScene()
    {
        Directory.CreateDirectory("Logs/OrderFollowUp");
        Physics.SyncTransforms();
        var lines = new System.Collections.Generic.List<string>();
        foreach (var name in new[] { "HanJin", "ReturnedOrder", "OutgoingOrder" })
        {
            var target = GameObject.Find(name).GetComponent<InvestigationTarget>();
            var point = target.GetComponent<Collider>().bounds.center;
            var origin = point + new Vector3(0, .3f, name == "OutgoingOrder" ? 1.8f : -1.8f);
            if (!Physics.Raycast(origin, point - origin, out var hit, 3f) || hit.collider.GetComponentInParent<InvestigationTarget>() != target)
                throw new InvalidOperationException("Interaction obstructed: " + name + " hit=" + hit.collider);
            lines.Add("PASS reachable interaction " + name);
        }
        Render("archive", new Vector3(-1.9f,1.65f,-1.5f), new Vector3(-3.2f,1,2.5f));
        Render("outgoing", new Vector3(0,1.65f,-19.1f), new Vector3(1.8f,.9f,-22f));
        File.WriteAllLines("Logs/OrderFollowUp/visibility.txt", lines);
    }

    static void Render(string name, Vector3 position, Vector3 focus)
    {
        var go = new GameObject("FollowUpPreview", typeof(Camera)); var camera = go.GetComponent<Camera>();
        camera.transform.position = position; camera.transform.LookAt(focus); camera.fieldOfView = 58;
        camera.nearClipPlane = .03f; camera.farClipPlane = 60; camera.clearFlags = CameraClearFlags.SolidColor;
        var rt = new RenderTexture(1280,800,24); var pixels = new Texture2D(1280,800,TextureFormat.RGB24,false); var previous = RenderTexture.active;
        try {
            camera.targetTexture = rt; camera.aspect = 1.6f; camera.Render(); camera.Render();
            RenderTexture.active = rt; pixels.ReadPixels(new Rect(0,0,1280,800),0,0); pixels.Apply();
            File.WriteAllBytes("Logs/OrderFollowUp/" + name + ".png", pixels.EncodeToPNG());
        } finally {
            RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
