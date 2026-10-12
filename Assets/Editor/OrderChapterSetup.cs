using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

public static class OrderChapterSetup
{
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("실행을 정지한 뒤 적용하세요.");
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Investigation.unity")
            throw new InvalidOperationException("Investigation 씬을 열어 주세요. 다른 씬은 수정하지 않습니다.");
        var targets = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include);
        var courier = targets.FirstOrDefault(t => t.kind == InvestigationTarget.TargetKind.DocumentCourier);
        if (courier == null) throw new InvalidOperationException("전달자 대상이 없습니다.");
        var existing = targets.FirstOrDefault(t => t.kind == InvestigationTarget.TargetKind.OrderStorage);
        if (existing == null)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(box, "군령 보관소 추가");
            box.name = "OrderStorageCrate";
            box.transform.SetParent(courier.transform.parent, true);
            var ground = courier.transform.position;
            // The box sits beside the existing courier rather than creating a second NPC.
            box.transform.position = new Vector3(ground.x + 1.55f, .42f, ground.z);
            box.transform.localScale = new Vector3(1.15f, .84f, .75f);
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/DarkTimber.mat");
            if (wood != null) box.GetComponent<Renderer>().sharedMaterial = wood;
            existing = box.AddComponent<InvestigationTarget>();
            existing.kind = InvestigationTarget.TargetKind.OrderStorage;
            var paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "StorageIntakeRecord";
            paper.transform.SetParent(box.transform, false);
            paper.transform.localPosition = new Vector3(0, .515f, 0);
            paper.transform.localScale = new Vector3(.6f, .015f, .55f);
            UnityEngine.Object.DestroyImmediate(paper.GetComponent<Collider>());
            var parchment = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/CommandTent/Materials/Parchment.mat");
            if (parchment != null) paper.GetComponent<Renderer>().sharedMaterial = parchment;
        }
        foreach (var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
        {
            string replacement = label.text.Replace("정비 명령서", "수군 배치도").Replace("경력 서류", "발급 기록").Replace("화공대비책", "군령");
            if (replacement == label.text) continue;
            Undo.RecordObject(label, "군령 조사 안내 변경"); label.text = replacement; EditorUtility.SetDirty(label);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        System.IO.Directory.CreateDirectory("Logs/OrderChapter");
        System.IO.File.WriteAllText("Logs/OrderChapter/setup.txt", "PASS scene saved; courier=" + courier.transform.position + "; storage=" + existing.transform.position);
        Debug.Log("군령 조사 씬 연결 완료: 전달자 옆 수송 물품과 조사 표기 적용.");
    }

    public static void CheckStorageVisibility()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required");
        var target = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include)
            .First(t => t.kind == InvestigationTarget.TargetKind.OrderStorage);
        var origin = target.transform.position + new Vector3(0, 1.23f, -2);
        var direction = target.transform.position - origin;
        Physics.SyncTransforms();
        if (!Physics.Raycast(origin, direction.normalized, out var hit, 3, ~0, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<InvestigationTarget>() != target)
            throw new InvalidOperationException("보관소의 정면 시야가 가려져 있습니다.");
        System.IO.File.WriteAllText("Logs/OrderChapter/visibility.txt", "PASS visible within 3m from standing eye height; target=" + hit.collider.name);
    }
}
