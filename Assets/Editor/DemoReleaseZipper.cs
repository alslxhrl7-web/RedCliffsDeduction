using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedCliffMystery.EditorTools
{
    // Packs the newest demo build folder into an itch.io-ready zip.
    // Files sit at the zip root (same layout as the demo.2 upload) and Unity's
    // *_BackUpThisFolder_ButDontShipItWithYourGame / *_DoNotShip folders are left out.
    public static class DemoReleaseZipper
    {
        [MenuItem("적벽추리/데모 수정/배포 zip 만들기")]
        public static void Zip()
        {
            var builds = new DirectoryInfo("Builds");
            var source = builds.Exists
                ? builds.GetDirectories("RedCliffsDeduction-*-windows-x64").OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault()
                : null;
            if (source == null || !File.Exists(Path.Combine(source.FullName, "RedCliffsDeduction.exe")))
                throw new InvalidOperationException("Build the demo first (적벽추리/데모 수정/수정 데모 빌드)");

            string zipPath = source.FullName + "-" + DateTime.Now.ToString("yyyy-MM-dd") + ".zip";
            if (File.Exists(zipPath))
                throw new IOException("Zip already exists, rename or remove it first: " + zipPath);

            int count = 0;
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(source.FullName, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(source.FullName.Length + 1).Replace('\\', '/');
                    if (relative.Contains("DontShip") || relative.Contains("DoNotShip")) continue;
                    archive.CreateEntryFromFile(file, relative, System.IO.Compression.CompressionLevel.Optimal);
                    count++;
                }
            }
            Debug.Log($"DEMO_ZIP_PASS: {zipPath} ({count} files, {new FileInfo(zipPath).Length / 1048576.0:F1} MB)");
        }
    }
}
