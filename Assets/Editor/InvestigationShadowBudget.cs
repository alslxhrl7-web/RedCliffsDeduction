using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RedCliffMystery.EditorTools
{
    public static class InvestigationShadowBudget
    {
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Investigation.unity")
                throw new System.InvalidOperationException("Open Investigation outside Play Mode.");
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || pipeline.additionalLightsShadowResolutionTierLow != 256)
                throw new System.InvalidOperationException("Expected URP low shadow tier of 256.");
            var report = new StringBuilder(); int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.name != "WarmLantern" || light.type != LightType.Point || light.shadows == LightShadows.None) continue;
                    var data = light.GetComponent<UniversalAdditionalLightData>();
                    if (data == null) throw new System.Exception("Missing URP light data");
                    var settings = new SerializedObject(data);
                    var tier = settings.FindProperty("m_AdditionalLightsShadowResolutionTier");
                    if (tier == null) throw new System.Exception("Missing URP shadow tier property");
                    Undo.RecordObject(data, "Set lantern shadow budget");
                    int before = tier.intValue; tier.intValue = 0; settings.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(data);
                    report.AppendLine(light.name + " at " + light.transform.position + " tier " + before + " -> 0 (256)"); count++;
                }
            if (count != 3) throw new System.Exception("Expected three shadow-casting lanterns, found " + count);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            report.AppendLine("18 point-light shadow faces at 256 fit in the unchanged 2048 atlas. Shadows remain enabled.");
            Directory.CreateDirectory("Logs/VarcoPreviews"); File.WriteAllText("Logs/VarcoPreviews/shadow-budget.txt", report.ToString());
        }
    }
}
