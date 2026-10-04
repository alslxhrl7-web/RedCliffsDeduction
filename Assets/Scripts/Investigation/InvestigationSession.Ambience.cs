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
            float targetVolume = (.40f + .04f * investigationTime.DayIndex) * InvestigationAudioSettings.Background;
            campAmbience.volume = Mathf.MoveTowards(campAmbience.volume, targetVolume, Time.unscaledDeltaTime * .12f);
        }

        public void ShowAudioSettings()
        {
            SetModal("소리 설정", "소리 크기", "군영 배경음에는 바람과 깃발 소리가 함께 포함됩니다.\n효과음은 발소리와 천막 스침 소리입니다.\n\n변경한 값은 다음 실행에도 유지됩니다.", "Esc  돌아가기");
            view.AddVolumeSlider("배경음 · 바람 / 깃발", InvestigationAudioSettings.Background, value => {
                InvestigationAudioSettings.SetBackground(value);
                if(campAmbience!=null) campAmbience.volume=(.40f+.04f*investigationTime.DayIndex)*value;
            });
            view.AddVolumeSlider("효과음 · 발소리 / 천막", InvestigationAudioSettings.Effects, InvestigationAudioSettings.SetEffects);
            System.Action back=()=> { InvestigationAudioSettings.Save(); ShowPause(); };
            view.AddChoice("돌아가기", ()=>back());
            onEscape=back;
        }
    }
}
