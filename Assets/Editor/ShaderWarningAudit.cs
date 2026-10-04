using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ShaderWarningAudit
{
    [MenuItem("적벽추리/렌더링 검증/저장 후 편집기 종료")]
    public static void CloseForCacheReset() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        EditorApplication.update += ExitEditor;
    }
    static void ExitEditor() { EditorApplication.update -= ExitEditor; EditorApplication.Exit(0); }
    const string Folder = "Logs/ShaderAudit";
    static readonly string[] Paths = {
        "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/CameraMotionVectors.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/LensFlareDataDriven.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/GaussianDepthOfField.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/BokehDepthOfField.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/PaniniProjection.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainDetailLit.shader",
        "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/WavingGrassBillboard.shader"
    };
    [MenuItem("적벽추리/렌더링 검증/셰이더 상태 기록")]
    public static void Inspect() {
        Directory.CreateDirectory(Folder);
        var report = new StringBuilder(DateTimeOffset.Now.ToString() + "\n");
        foreach (var path in Paths) {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            report.AppendLine(path + ": " + (shader == null ? "MISSING" : "supported=" + shader.isSupported));
            if (shader != null) foreach (var message in ShaderUtil.GetShaderMessages(shader))
                report.AppendLine(message.severity + ": " + message.message);
        }
        File.WriteAllText(Folder + "/shader-state.txt", report.ToString());
    }
    [MenuItem("적벽추리/렌더링 검증/실패 셰이더 재가져오기")]
    public static void Repair() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        var flags = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;
        AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal-config/Runtime/ShaderConfig.cs.hlsl", flags);
        AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal/ShaderLibrary", flags | ImportAssetOptions.ImportRecursive);
        foreach (var path in Paths) {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null) { ShaderUtil.ClearCachedData(shader); ShaderUtil.ClearShaderMessages(shader); }
            AssetDatabase.ImportAsset(path, flags);
            shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) continue;
            var material = new Material(shader);
            try { for (int pass = 0; pass < material.passCount; pass++) ShaderUtil.CompilePass(material, pass, true); }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }
        Inspect();
    }
    [MenuItem("적벽추리/렌더링 검증/캐시 초기화 검증 빌드")]
    public static void QueueBuild() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/build-result.txt", "RUNNING");
        EditorApplication.update -= BuildNow;
        EditorApplication.update += BuildNow;
    }
    static void BuildNow() {
        EditorApplication.update -= BuildNow;
            try {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/Investigation.unity" },
                    locationPathName = "Builds/ShaderReview-2026-10-04/RedCliffsDeduction.exe",
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.CleanBuildCache
                });
                var text = new StringBuilder(report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings + "\n");
                foreach (var step in report.steps) foreach (var message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Warning)
                        text.AppendLine(message.type + ": " + message.content);
                File.WriteAllText(Folder + "/build-result.txt", text.ToString());
            } catch (Exception error) { File.WriteAllText(Folder + "/build-result.txt", error.ToString()); }
    }
}
