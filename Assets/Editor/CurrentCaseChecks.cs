using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

// Maintenance for the current case. Play tests use isolated slots, never player saves.
public static class CurrentCaseChecks
{
    const string SaveKey = "RedCliffMystery.TestSaveDirectory";
    [MenuItem("Tools/Red Cliffs/Current Case/Prepare and clean retired components")]
    public static void Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Investigation.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Investigation scene outside Play Mode first.");
        int removed = 0;
        foreach (var script in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray())
        {
            if (script == null) throw new InvalidOperationException("Scene already has a missing script.");
            string type = script.GetType().Name;
            if (type != "DiscoveredDocumentVisuals" && type != "EvidenceDocumentView") continue;
            if (type == "DiscoveredDocumentVisuals") {
                var serialized = new SerializedObject(script);
                foreach (string field in new[] { "householdRegistry", "secretLetter" }) {
                    var item = serialized.FindProperty(field).objectReferenceValue as GameObject;
                    if (item != null) { Undo.RecordObject(item, "Keep retired clue hidden"); item.SetActive(false); }
                }
            }
            Undo.DestroyObjectImmediate(script); removed++;
        }
        if (removed > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        foreach (string file in new[] { "DiscoveredDocumentVisuals", "EvidenceDocumentView" })
            AssetDatabase.DeleteAsset("Assets/Scripts/Investigation/" + file + ".cs");
        SessionState.SetString(SaveKey, Path.GetFullPath("Temp/CurrentCaseChecks"));
        Directory.CreateDirectory("Logs/CurrentCase");
        File.WriteAllText("Logs/CurrentCase/scene.txt", "Retired components removed=" + removed + "\nscene=" + scene.path);
        Status();
    }
    [MenuItem("Tools/Red Cliffs/Current Case/Run Play checks")]
    public static void Run() => OrderChapterChecks.RunPlay();
    [MenuItem("Tools/Red Cliffs/Current Case/Finish checks")]
    public static void Finish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        SessionState.EraseString(SaveKey); Status();
    }
    [MenuItem("Tools/Red Cliffs/Current Case/Status")]
    public static void Status()
    {
        var scene = SceneManager.GetActiveScene();
        int missing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        Directory.CreateDirectory("Logs/CurrentCase");
        File.WriteAllText("Logs/CurrentCase/status.txt", "project=" + Application.dataPath + "\nplaying=" + EditorApplication.isPlaying
            + "\ncompiling=" + EditorApplication.isCompiling + "\nmissingScripts=" + missing + "\nsceneDirty=" + scene.isDirty
            + "\nsave=" + InvestigationSaveStore.SavePath);
    }
}
