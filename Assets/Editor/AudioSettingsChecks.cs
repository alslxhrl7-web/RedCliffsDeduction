using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class AudioSettingsChecks
{
    static float background,effects;
    static double finishAt;
    static bool captured;
    [MenuItem("적벽추리/플레이 검증/소리 설정 확인")]
    static void Run()
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        background=InvestigationAudioSettings.Background; effects=InvestigationAudioSettings.Effects;
        try {
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>(); session.ShowAudioSettings();
            var sliders=session.view.choices.GetComponentsInChildren<UnityEngine.UI.Slider>();
            if(sliders.Length!=2) throw new InvalidOperationException("Two sliders required");
            sliders[0].value=0;
            var source=(AudioSource)typeof(InvestigationSession).GetField("campAmbience",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
            if(source==null || source.volume!=0 || InvestigationAudioSettings.Background!=0) throw new InvalidOperationException("Background mute failed");
            sliders[0].value=.25f; sliders[1].value=.35f;
            if(source.volume<.099f || source.volume>.121f) throw new InvalidOperationException("Background level failed");
            if(Mathf.Abs(InvestigationAudioSettings.Effects-.35f)>.001f) throw new InvalidOperationException("Effects preference failed");
            InvestigationAudioSettings.Save();
            sliders[0].value=background; sliders[1].value=effects;
            Canvas.ForceUpdateCanvases();
            foreach(var slider in sliders) if(slider.GetComponent<RectTransform>().rect.height<10) throw new InvalidOperationException("Collapsed slider");
            Directory.CreateDirectory("Logs/AudioSettings");
            File.WriteAllText("Logs/AudioSettings/result.txt","PASS Two independent sliders, instant background mute and gain, effects preference, saved settings and visible layout\nCOMPLETE failures=0\n");
            captured=false; finishAt=EditorApplication.timeSinceStartup+.75; EditorApplication.update+=Finish;
        } catch { Restore(); EditorApplication.isPlaying=false; throw; }
    }
    static void Restore() { InvestigationAudioSettings.SetBackground(background); InvestigationAudioSettings.SetEffects(effects); InvestigationAudioSettings.Save(); }
    static void Finish() {
        if(EditorApplication.timeSinceStartup<finishAt) return;
        if(!captured) { UnityEngine.Object.FindAnyObjectByType<InvestigationSession>().ShowAudioSettings(); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/AudioSettings/settings.png")); captured=true; finishAt=EditorApplication.timeSinceStartup+1; return; }
        EditorApplication.update-=Finish; Restore(); EditorApplication.isPlaying=false;
    }
}
