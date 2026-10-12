using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class DistanceFootsteps : MonoBehaviour
    {
        public AudioClip[] woodenSteps, earthSteps;
        public float metresPerStep = 1.2f;
        public bool ExternalMotion { get; set; }
        public int StepsPlayed { get; private set; }
        public float PendingDistance { get; private set; }
        AudioSource source;
        readonly AudioSource[] voices = new AudioSource[4];
        int nextVoice;
        Vector3 previous;
        int variation;
        int previousClip=-1;
        void Awake()
        {
            for (int i=0;i<voices.Length;i++)
            {
                // A contact stays at its world position while its short tail plays.
                var feet = new GameObject("FootstepContact_"+i);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(feet, gameObject.scene);
                source=feet.AddComponent<AudioSource>(); voices[i]=source;
                source.playOnAwake=false; source.spatialBlend=1; source.spatialize=false;
                source.volume=InvestigationAudioSettings.Effects;
                source.minDistance=1.5f; source.maxDistance=14; source.dopplerLevel=0;
                source.spread=45; source.rolloffMode=AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                    new Keyframe(0,1),new Keyframe(1.5f/14,1),new Keyframe(3f/14,.55f),
                    new Keyframe(6f/14,.18f),new Keyframe(1,0)));
            }
            ResetMotion();
        }
        void Update() { foreach(var voice in voices) if(voice!=null) voice.volume=InvestigationAudioSettings.Effects; }
        public void ResetMotion() { previous=transform.position; PendingDistance=0; }
        public void RecordMovement(bool grounded, bool outdoors)
        {
            Vector3 delta=transform.position-previous; previous=transform.position; delta.y=0;
            float distance=delta.magnitude;
            // Teleports, falls and standing against a wall must not count as walking.
            if(!grounded || distance>1.5f) { PendingDistance=0; return; }
            if(distance<.0001f) return;
            PendingDistance+=distance;
            if(PendingDistance+.0001f<metresPerStep) return;
            PendingDistance=Mathf.Max(0,PendingDistance-metresPerStep);
            var clips=outdoors?earthSteps:woodenSteps;
            if(clips==null || clips.Length==0 || source==null) return;
            int selected=Random.Range(0,clips.Length);
            if(clips.Length>1 && selected==previousClip) selected=(selected+1+Random.Range(0,clips.Length-1))%clips.Length;
            previousClip=selected;
            source=voices[nextVoice++%voices.Length];
            source.transform.position=transform.position+Vector3.up*.12f+transform.right*(variation++%2==0?-.10f:.10f);
            source.pitch=Random.Range(.97f,1.03f);
            source.volume=InvestigationAudioSettings.Effects;
            source.PlayOneShot(clips[selected],(outdoors?.85f:.80f)*Random.Range(.97f,1.03f)); StepsPlayed++;
        }
        void OnDisable() { foreach(var voice in voices) if(voice!=null) voice.Stop(); ResetMotion(); }
        void OnDestroy() { foreach(var voice in voices) if(voice!=null) { if(Application.isPlaying) Destroy(voice.gameObject); else DestroyImmediate(voice.gameObject); } }
    }
}
