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
        Vector3 previous;
        int variation;
        int previousClip=-1;
        void Awake()
        {
            var feet = new GameObject("FootstepSource"); feet.transform.SetParent(transform,false);
            feet.transform.localPosition = Vector3.up*.12f;
            // Player footsteps stay audible regardless of camera height or viewing direction.
            source=feet.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0;
            source.volume=InvestigationAudioSettings.Effects;
            source.minDistance=2; source.maxDistance=14; source.rolloffMode=AudioRolloffMode.Logarithmic; source.dopplerLevel=0;
            ResetMotion();
        }
        void Update() { if(source!=null) source.volume=InvestigationAudioSettings.Effects; }
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
            source.pitch=Random.Range(.97f,1.03f);
            source.panStereo=(variation++%2==0?-.035f:.035f);
            source.volume=InvestigationAudioSettings.Effects;
            source.PlayOneShot(clips[selected],(outdoors?.85f:.80f)*Random.Range(.97f,1.03f)); StepsPlayed++;
        }
        void OnDisable() { if(source!=null) source.Stop(); ResetMotion(); }
    }
}
