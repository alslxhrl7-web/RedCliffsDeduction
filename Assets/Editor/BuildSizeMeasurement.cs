using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RedCliffMystery.EditorTools
{
    // Comparison only: never replaces release output or changes player/import settings.
    public static class BuildSizeMeasurement
    {
        [MenuItem("적벽추리/데모 수정/빌드 용량 비교 측정")]
        static void Queue()
        {
            Directory.CreateDirectory("Logs/BuildSize");
            File.WriteAllText("Logs/BuildSize/queued.txt", DateTime.Now.ToString("O"));
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play and save the scene before measuring.");
            EditorApplication.update -= Start;
            EditorApplication.update += Start;
        }

        static void Start() { EditorApplication.update -= Start; Measure(); }

        static void Measure()
        {
            string root = "Builds/SizeComparison-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(root);
            Directory.CreateDirectory("Logs/BuildSize");
            File.WriteAllText("Logs/BuildSize/latest.txt", Path.GetFullPath(root));
            string log = root + "/measurement.txt";
            try
            {
                foreach (bool compressed in new[] { false, true })
                {
                    string label = compressed ? "Lz4HC" : "Default";
                    File.AppendAllText(log, "START " + label + "\n");
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                        scenes = new[] { FirstInvestigationBuilder.ScenePath },
                        locationPathName = root + "/" + label + "/RedCliffsDeduction.exe",
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.DetailedBuildReport | (compressed ? BuildOptions.CompressWithLz4HC : BuildOptions.None)
                    });
                    var summary = report.summary;
                    File.AppendAllText(log, $"{label} result={summary.result} reportedBytes={summary.totalSize} buildSeconds={summary.totalTime.TotalSeconds:F2} errors={summary.totalErrors} warnings={summary.totalWarnings}\n");
                    if (summary.result != BuildResult.Succeeded) throw new Exception(label + " build failed");
                    var sizes = new Dictionary<string, ulong>();
                    foreach (var packed in report.packedAssets)
                        foreach (var item in packed.contents)
                        {
                            string path = item.sourceAssetPath ?? "(built-in)";
                            if (!sizes.ContainsKey(path)) sizes[path] = 0;
                            sizes[path] += item.packedSize;
                        }
                    File.WriteAllLines(root + "/" + label + "-assets.csv", new[] { "bytes,path" }.Concat(
                        sizes.OrderByDescending(p => p.Value).Select(p => p.Value + ",\"" + p.Key.Replace("\"", "\"\"") + "\"")));
                    File.WriteAllLines(root + "/" + label + "-warnings.txt", report.steps.SelectMany(s => s.messages)
                        .Where(m => m.type == LogType.Warning || m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content));
                }
                File.AppendAllText(log, "COMPLETE failures=0\n");
            }
            catch (Exception e) { File.AppendAllText(log, "FAILED " + e + "\n"); Debug.LogException(e); }
        }
    }
}
