using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class ProjectBuildChecks
    {
        public static void ValidateSources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building.");
            if (!File.Exists(FirstInvestigationBuilder.ScenePath))
                throw new FileNotFoundException("Investigation scene is missing.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path == FirstInvestigationBuilder.ScenePath)
            {
                var missing = scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<UnityEngine.Transform>(true))
                    .Where(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    .Select(t => t.name).ToArray();
                if (missing.Length > 0)
                    throw new InvalidOperationException("Missing scripts: " + string.Join(", ", missing));
                if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
                    throw new IOException("Could not save Investigation scene before building.");
            }
            var dependencies = AssetDatabase.GetDependencies(FirstInvestigationBuilder.ScenePath, true)
                .OrderBy(p => p).ToArray();
            Directory.CreateDirectory("Logs/Portability");
            File.WriteAllLines("Logs/Portability/dependencies.txt", dependencies);
            File.WriteAllText("Logs/Portability/editor-audit.txt",
                "PASS build source dependencies=" + dependencies.Length + "; Play=false");
        }
    }
}
