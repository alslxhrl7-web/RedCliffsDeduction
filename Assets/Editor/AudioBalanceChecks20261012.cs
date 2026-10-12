using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class AudioBalanceChecks20261012
{
    const string Folder="Logs/AudioBalance20261012";
    static GameObject probe;
    static DistanceFootsteps steps;
    static AudioSource source, ambience;
    static int phase;
    static double next;
    static float background, effects;
    static void Require(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
    static void Log(string message) { File.AppendAllText(Folder+"/checks.txt",message+"\n"); }

    static void Apply()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder+"/settings.txt",$"Previous background={InvestigationAudioSettings.Background}, effects={InvestigationAudioSettings.Effects}\nApplied background=0.45, effects=0.75\n");
        InvestigationAudioSettings.SetBackground(.45f);
        InvestigationAudioSettings.SetEffects(.75f);
        InvestigationAudioSettings.Save();
    }

    static void Begin()
    {
        Require(EditorApplication.isPlaying,"Play Mode required");
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder+"/checks.txt","Audio balance verification\n");
        background=InvestigationAudioSettings.Background; effects=InvestigationAudioSettings.Effects;
        try {
            var title=UnityEngine.Object.FindAnyObjectByType<InvestigationTitleScreen>();
            if(title!=null) title.StartButton.onClick.Invoke();
            var session=UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            var reference=session.route.player.GetComponent<DistanceFootsteps>();
            ambience=session.GetComponents<AudioSource>().First(s=>s.clip!=null && s.clip.name=="CampWindAmbience");
            Require(ambience.isPlaying && ambience.loop && !ambience.mute,"Ambience must be playing");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l=>l.enabled)==1,"One active listener required");
            Require(!AudioListener.pause && AudioListener.volume>0,"Listener muted");
            probe=new GameObject("Audio balance probe"); steps=probe.AddComponent<DistanceFootsteps>();
            steps.woodenSteps=reference.woodenSteps; steps.earthSteps=reference.earthSteps;
            source=probe.GetComponentInChildren<AudioSource>();
            float bed=Rms(ambience.clip)*.4f*.45f;
            foreach(var clip in steps.woodenSteps.Concat(steps.earthSteps)) {
                float gain=clip.name.StartsWith("Earth")?.85f:.80f;
                float ratio=Rms(clip)*gain*.75f/bed;
                Require(ratio>1 && ratio<2,"Footstep/background balance out of range: "+clip.name);
                Log($"PASS {clip.name}: step RMS / ambience RMS = {ratio:F2}");
            }
            Require(source.spatialBlend==0,"Camera-dependent attenuation");
            phase=0; next=EditorApplication.timeSinceStartup+.1; EditorApplication.update+=Tick;
        } catch(Exception e) { Fail(e); }
    }
    static float Rms(AudioClip clip)
    {
        var data=new float[clip.samples*clip.channels]; Require(clip.GetData(data,0),"Cannot read "+clip.name);
        double sum=0; foreach(float x in data) sum+=x*x; return (float)Math.Sqrt(sum/data.Length);
    }
    static void Step(bool outdoors)
    {
        steps.ResetMotion(); probe.transform.position+=Vector3.right*1.2f; steps.RecordMovement(true,outdoors);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try {
            Require(EditorApplication.isPlaying,"Play stopped during audio test");
            if(phase==0) { Step(false); Require(source.isPlaying && ambience.isPlaying,"Indoor simultaneous playback failed"); Log("PASS indoor footstep and ambience playing together"); }
            if(phase==1) { Step(true); Require(source.isPlaying && ambience.isPlaying,"Outdoor simultaneous playback failed"); Log("PASS outdoor footstep and ambience playing together"); InvestigationAudioSettings.SetEffects(0); }
            if(phase==2) { Require(source.volume==0 && ambience.volume>0,"Effects mute affected ambience or failed"); Log("PASS effects mute independent of ambience"); InvestigationAudioSettings.SetEffects(effects); InvestigationAudioSettings.SetBackground(0); }
            if(phase==3) { Require(ambience.volume==0 && source.volume>0,"Background mute failed"); Log("PASS background mute independent of effects"); InvestigationAudioSettings.SetBackground(background); }
            if(phase==4) {
                Require(Mathf.Abs(ambience.volume-.4f*background)<.001f,"Background gain failed");
                int before=steps.StepsPlayed; steps.RecordMovement(true,false); Require(before==steps.StepsPlayed,"Idle footstep");
                probe.transform.position+=Vector3.right*40; steps.RecordMovement(true,false); Require(before==steps.StepsPlayed,"Teleport footstep");
                Log("PASS fixed ambience gain, stationary and teleport silence"); Log("COMPLETE failures=0"); Finish(); return;
            }
            phase++; next=EditorApplication.timeSinceStartup+(phase>=3?2:.3);
        } catch(Exception e) { Fail(e); }
    }
    static void Fail(Exception e) { Log("FAIL "+e); Log("COMPLETE failures=1"); Debug.LogException(e); Finish(); }
    static void Finish()
    {
        EditorApplication.update-=Tick;
        InvestigationAudioSettings.SetBackground(background); InvestigationAudioSettings.SetEffects(effects); InvestigationAudioSettings.Save();
        if(probe!=null) UnityEngine.Object.DestroyImmediate(probe);
        EditorApplication.isPlaying=false;
    }
}
