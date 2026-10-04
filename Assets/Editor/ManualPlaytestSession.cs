using System;
using System.IO;
using UnityEditor;

namespace RedCliffMystery.EditorTools
{
    [InitializeOnLoad]
    public static class ManualPlaytestSession
    {
        static bool active, originalEnabled;
        static EnterPlayModeOptions originalOptions;
        static ManualPlaytestSession() { EditorApplication.playModeStateChanged += Finish; }
        [MenuItem("적벽추리/플레이 검증/화면 조작 테스트 시작")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start outside play mode");
            originalEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            originalOptions = EditorSettings.enterPlayModeOptions;
            SessionState.SetString("RedCliffMystery.TestSaveDirectory", Path.GetFullPath("Temp/PlayRegression/Manual-" + Guid.NewGuid().ToString("N")));
            active = true;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.isPlaying = true;
        }
        static void Finish(PlayModeStateChange state)
        {
            if (!active || state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.EraseString("RedCliffMystery.TestSaveDirectory");
            EditorSettings.enterPlayModeOptionsEnabled = originalEnabled;
            EditorSettings.enterPlayModeOptions = originalOptions;
            active = false;
        }
    }
}
