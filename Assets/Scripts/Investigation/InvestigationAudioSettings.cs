using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public static class InvestigationAudioSettings
    {
        const string BackgroundKey="RedCliffs.Audio.Background", EffectsKey="RedCliffs.Audio.Effects";
        public static float Background => Mathf.Clamp01(PlayerPrefs.GetFloat(BackgroundKey,.45f));
        public static float Effects => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey,.75f));
        public static void SetBackground(float value) { PlayerPrefs.SetFloat(BackgroundKey,Mathf.Clamp01(value)); }
        public static void SetEffects(float value) { PlayerPrefs.SetFloat(EffectsKey,Mathf.Clamp01(value)); }
        public static void Save() { PlayerPrefs.Save(); }
    }
}
