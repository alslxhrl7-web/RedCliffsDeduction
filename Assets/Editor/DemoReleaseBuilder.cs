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
        [MenuItem("적벽추리/데모 수정/수정 데모 빌드")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first");
            string product = PlayerSettings.productName, version = PlayerSettings.bundleVersion;
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            var mode = PlayerSettings.fullScreenMode;
            try
            {
                PlayerSettings.productName = "RedCliffsDeduction Test Demo";
                PlayerSettings.bundleVersion = "0.1.0-demo.2";
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                string output = "Builds/RedCliffsDeduction-0.1.0-demo.2-windows-x64";
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

        [MenuItem("적벽추리/데모 수정/배치 화면 저장")]
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
