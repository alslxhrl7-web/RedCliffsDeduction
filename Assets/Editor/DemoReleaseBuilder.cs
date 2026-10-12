using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class DemoReleaseBuilder
    {
        // 데모 버전은 여기 한 곳만 올리면 됩니다. 출력 폴더 이름도 이 값에서 만들어집니다.
        public const string DemoVersion = "0.1.0-demo.8";

        // 같은 버전 폴더가 이미 있으면 덮어쓰지 않고 시간 표시를 붙인 새 폴더에 빌드합니다.
        // (DemoReleaseZipper의 "RedCliffsDeduction-*-windows-x64" 패턴과 계속 맞습니다.)
        static string NewOutputFolder()
        {
            string path = "Builds/RedCliffsDeduction-" + DemoVersion + "-windows-x64";
            if (Directory.Exists(path))
                path = "Builds/RedCliffsDeduction-" + DemoVersion + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-windows-x64";
            return path;
        }

        // Batch review builds exercise Unity's real JSON save round trip before packaging.
        public static void BuildChecked()
        {
            ProjectBuildChecks.ValidateSources();
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first");
            Debug.Log("SAVE_CHECKS: " + InvestigationSaveChecks.Verify());
            InvestigationBudgetChecks.Verify();
            var args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "-buildOutput");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length)
                throw new InvalidOperationException("Pass -buildOutput with an executable path");
            string executable = Path.GetFullPath(args[outputIndex + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { FirstInvestigationBuilder.ScenePath },
                locationPathName = executable, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Review build failed: " + report.summary.result);
            Debug.Log("REVIEW_BUILD_PASS: " + executable);
        }

        [MenuItem("적벽추리/Windows 빌드")]
        public static void Build()
        {
            ProjectBuildChecks.ValidateSources();
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first");
            string product = PlayerSettings.productName, version = PlayerSettings.bundleVersion;
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            var mode = PlayerSettings.fullScreenMode;
            try
            {
                PlayerSettings.productName = "RedCliffsDeduction Test Demo";
                PlayerSettings.bundleVersion = DemoVersion;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                string output = NewOutputFolder();
                Directory.CreateDirectory(output);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { FirstInvestigationBuilder.ScenePath },
                    locationPathName = output + "/RedCliffsDeduction.exe",
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Demo build failed: " + report.summary.result);
                Debug.Log("DEMO_BUILD_PASS: " + Path.GetFullPath(output));
            }
            finally
            {
                PlayerSettings.productName = product;
                PlayerSettings.bundleVersion = version;
                PlayerSettings.defaultScreenWidth = width;
                PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.fullScreenMode = mode;
            }
        }

        public static void CaptureProps()
        {
            var camera = UnityEngine.Object.FindAnyObjectByType<FirstPersonInvestigator>().viewCamera;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var texture = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                Directory.CreateDirectory("Temp/DemoScreenshots");
                camera.targetTexture = texture;
                var points = new[] { new Vector3(0, 1.15f, 2.4f), new Vector3(4.35f, 1.17f, 4.25f), new Vector3(-40, 1.03f, 2.4f) };
                var names = new[] { "tray", "token", "order" };
                for (int i = 0; i < points.Length; i++)
                {
                    camera.transform.position = points[i] + new Vector3(-.2f, .7f, -1.7f);
                    camera.transform.LookAt(points[i]);
                    camera.Render();
                    RenderTexture.active = texture;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes("Temp/DemoScreenshots/" + names[i] + ".png", pixels.EncodeToPNG());
                }
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            Debug.Log("DEMO_SCREENSHOTS_SAVED");
        }
    }
}
