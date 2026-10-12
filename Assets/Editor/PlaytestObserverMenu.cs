using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    // Switches the hidden playtest log on or off. The log runs only where a "playtest.on" file exists.
    public static class PlaytestObserverMenu
    {
        static string ProjectFlag => Path.Combine(PlaytestObserver.GameFolder, PlaytestObserver.FlagFileName);

        public static void ToggleEditor()
        {
            if (File.Exists(ProjectFlag)) { File.Delete(ProjectFlag); Debug.Log("PLAYTEST_LOG_OFF: 에디터 플레이 기록을 껐습니다."); }
            else { File.WriteAllText(ProjectFlag, "playtest log on\n"); Debug.Log("PLAYTEST_LOG_ON: 다음 Play부터 " + PlaytestObserver.LogFolderName + " 폴더에 기록합니다."); }
        }

        public static void EnableLatestBuild()
        {
            var build = new DirectoryInfo("Builds").Exists
                ? new DirectoryInfo("Builds").GetDirectories("RedCliffsDeduction-*-windows-x64")
                    .Where(d => File.Exists(Path.Combine(d.FullName, "RedCliffsDeduction.exe")))
                    .OrderByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d.FullName, "RedCliffsDeduction.exe"))).FirstOrDefault()
                : null;
            if (build == null) { Debug.LogError("[Playtest] 빌드 폴더가 없습니다. 먼저 데모 빌드를 만드세요."); return; }
            File.WriteAllText(Path.Combine(build.FullName, PlaytestObserver.FlagFileName), "playtest log on\n");
            Debug.Log("PLAYTEST_BUILD_ON: " + build.FullName + " · 배포 zip에는 이 파일과 기록 폴더가 들어가지 않습니다.");
        }

        public static void OpenLogs()
        {
            string folder = Path.Combine(PlaytestObserver.GameFolder, PlaytestObserver.LogFolderName);
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }
    }
}
