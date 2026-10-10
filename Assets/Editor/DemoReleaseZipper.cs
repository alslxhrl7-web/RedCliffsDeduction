using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedCliffMystery.EditorTools
{
    // Packs the demo build folder of the current DemoReleaseBuilder.DemoVersion into an itch.io-ready zip.
    // - Only a build folder that matches DemoVersion is packed, so an older build is never zipped by mistake.
    // - A zip that already exists is never an error: -2, -3 ... is appended to the name.
    // - The zip is written to a .tmp file first and renamed only when it is complete and verified,
    //   so a failed run cannot leave a half-written zip behind.
    // - Files sit at the zip root and Unity's *_BackUpThisFolder_ButDontShipItWithYourGame / *_DoNotShip folders are left out.
    public static class DemoReleaseZipper
    {
        [MenuItem("적벽추리/데모 수정/배포 zip 만들기")]
        public static void Zip()
        {
            string prefix = "RedCliffsDeduction-" + DemoReleaseBuilder.DemoVersion;
            var builds = new DirectoryInfo("Builds");
            var source = builds.Exists
                ? builds.GetDirectories("RedCliffsDeduction-*-windows-x64")
                    .Where(d => d.Name == prefix + "-windows-x64" || d.Name.StartsWith(prefix + "-"))
                    .Where(d => File.Exists(Path.Combine(d.FullName, "RedCliffsDeduction.exe")))
                    .OrderByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d.FullName, "RedCliffsDeduction.exe")))
                    .FirstOrDefault()
                : null;
            if (source == null)
            {
                Debug.LogError("[DemoZip] " + DemoReleaseBuilder.DemoVersion + " 빌드 폴더가 없습니다. 먼저 '적벽추리/데모 수정/수정 데모 빌드'를 실행하세요.");
                return;
            }

            string baseName = source.FullName + "-" + DateTime.Now.ToString("yyyy-MM-dd");
            string zipPath = baseName + ".zip";
            for (int n = 2; File.Exists(zipPath); n++) zipPath = baseName + "-" + n + ".zip";
            string tempPath = zipPath + ".tmp";

            int rootLength = source.FullName.Length + 1;
            var files = Directory.GetFiles(source.FullName, "*", SearchOption.AllDirectories)
                .Where(f => { string r = f.Substring(rootLength); return !r.Contains("DontShip") && !r.Contains("DoNotShip")
                    // The playtest switch and its logs must never reach the public download.
                    && !string.Equals(Path.GetFileName(r), RedCliffMystery.Investigation.PlaytestObserver.FlagFileName, StringComparison.OrdinalIgnoreCase)
                    && !r.Replace('\\', '/').StartsWith(RedCliffMystery.Investigation.PlaytestObserver.LogFolderName + "/", StringComparison.OrdinalIgnoreCase); })
                .ToArray();

            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                using (var zipStream = new FileStream(tempPath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (i % 10 == 0)
                            EditorUtility.DisplayProgressBar("배포 zip 만들기", Path.GetFileName(files[i]), (float)i / files.Length);
                        string relative = files[i].Substring(rootLength).Replace('\\', '/');
                        var entry = archive.CreateEntry(relative, System.IO.Compression.CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var input = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            input.CopyTo(entryStream);
                    }
                }
                using (var check = ZipFile.OpenRead(tempPath))
                {
                    if (check.Entries.Count != files.Length)
                        throw new IOException("zip 항목 수 불일치: " + check.Entries.Count + " / " + files.Length);
                }
                File.Move(tempPath, zipPath);
            }
            catch (Exception e)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                Debug.LogError("[DemoZip] zip 만들기 실패: " + e.Message);
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.Log($"DEMO_ZIP_PASS: {zipPath} ({files.Length} files, {new FileInfo(zipPath).Length / 1048576.0:F1} MB)");
        }
    }
}
