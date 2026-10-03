using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        AudioSource campAmbience;
        bool ambienceLoadAttempted;

        void UpdateInvestigationAmbience()
        {
            if (FinalReportCompleted) {
                if (campAmbience != null && campAmbience.isPlaying) campAmbience.Stop();
                return;
            }
            if (!ambienceLoadAttempted) {
                ambienceLoadAttempted = true;
                var clip = Resources.Load<AudioClip>("Audio/CampWindAmbience");
                if (clip == null) {
                    Debug.LogWarning("군영 배경음을 불러오지 못했습니다: Resources/Audio/CampWindAmbience.wav", this);
                    return;
                }
                // Keep the source on the session, independent of the blackout overlay.
                campAmbience = gameObject.AddComponent<AudioSource>();
                campAmbience.playOnAwake = false;
                campAmbience.loop = true;
                campAmbience.spatialBlend = 0;
                campAmbience.clip = clip;
                campAmbience.volume = 0;
            }
            if (campAmbience == null) return;
            if (!campAmbience.isPlaying) campAmbience.Play();
            float targetVolume = .40f + .04f * investigationTime.DayIndex;
            campAmbience.volume = Mathf.MoveTowards(campAmbience.volume, targetVolume, Time.unscaledDeltaTime * .12f);
        }
    }
}
