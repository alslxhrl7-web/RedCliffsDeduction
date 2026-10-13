using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TestDemoBuild
{
    // Build-specific settings are restored so making a demo does not alter the working game.
    public static void BuildWindows()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-buildOutput");
        if (index < 0 || index + 1 >= args.Length)
            throw new ArgumentException("Supply -buildOutput with an executable path.");
        string output = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        InvestigationBudgetChecks.Verify();
        Debug.Log(InvestigationSaveChecks.Verify());

        string product = PlayerSettings.productName;
        string version = PlayerSettings.bundleVersion;
        int width = PlayerSettings.defaultScreenWidth;
        int height = PlayerSettings.defaultScreenHeight;
        var mode = PlayerSettings.fullScreenMode;
        bool resize = PlayerSettings.resizableWindow;
        try
        {
            // A separate product name also keeps demo saves separate from editor saves.
            PlayerSettings.productName = "RedCliffsDeduction Test Demo";
            PlayerSettings.bundleVersion = "0.1.0-demo.1";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Investigation.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            });
            Debug.Log($"TEST_DEMO_BUILD: {report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; bytes={report.summary.totalSize}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Test demo build failed: " + report.summary.result);
        }
        finally
        {
            PlayerSettings.productName = product;
            PlayerSettings.bundleVersion = version;
            PlayerSettings.defaultScreenWidth = width;
            PlayerSettings.defaultScreenHeight = height;
            PlayerSettings.fullScreenMode = mode;
            PlayerSettings.resizableWindow = resize;
            AssetDatabase.SaveAssets();
        }
    }
}
