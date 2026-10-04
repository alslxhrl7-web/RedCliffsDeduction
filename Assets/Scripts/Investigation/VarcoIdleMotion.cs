using UnityEngine;

namespace RedCliffMystery
{
    // Small visual-only breathing and upper-body sway. NPC roots and hit volumes stay fixed.
    public sealed class VarcoIdleMotion : MonoBehaviour
    {
        public int Profile;
        public float Amplitude = .005f;
        struct Surface
        {
            public MeshFilter Filter;
            public Mesh Original, Animated;
            public Vector3[] Rest, Buffer;
            public float[] Weight;
            public float[] MouthWeight;
            public float Height, MouthY;
        }
        Surface[] surfaces;
        float nextSample;
        string speech;
        float speechStart;
        static readonly string[] Speakers = { "조조", "설평", "두석", "채모", "장윤", "기록 담당자", "전령", "교대 병사", "동석 병사" };
        static readonly float[] MouthHeights = { .840f, .872f, .870f, .875f, .855f, .875f, .862f, .866f, .867f };
        public static float MouthHeightFraction(int profile) => MouthHeights[Mathf.Clamp(profile, 0, MouthHeights.Length - 1)];
        public static void StopSpeech()
        {
            foreach (var motion in FindObjectsByType<VarcoIdleMotion>(FindObjectsSortMode.None)) motion.speech = null;
        }
        public static void Speak(string speaker, string text)
        {
            StopSpeech();
            int profile = System.Array.IndexOf(Speakers, speaker);
            if (speaker == "병사") profile = 8;
            foreach (var motion in FindObjectsByType<VarcoIdleMotion>(FindObjectsSortMode.None))
                if (motion.Profile == profile) motion.StartSpeech(text, Time.time);
        }
        public void StartSpeech(string text, float time) { speech = text; speechStart = time; }
        public bool IsSpeaking => !string.IsNullOrEmpty(speech) && MouthShape(speech, Time.time - speechStart) != Vector2.zero;
        public bool HasSpeech => !string.IsNullOrEmpty(speech);
        public static Vector2 MouthShape(string text, float elapsed)
        {
            if (string.IsNullOrEmpty(text) || elapsed < 0) return Vector2.zero;
            foreach (char c in text)
            {
                bool syllable = c >= '\uAC00' && c <= '\uD7A3';
                float duration = char.IsWhiteSpace(c) ? .07f : char.IsPunctuation(c) ? .22f : .14f;
                if (elapsed >= duration) { elapsed -= duration; continue; }
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c)) return Vector2.zero;
                float fraction = elapsed / duration;
                float envelope = Mathf.Sin(fraction * Mathf.PI);
                if (!syllable) return new Vector2(envelope * .6f, 0);
                int code = c - '\uAC00', initial = code / 588, vowel = code % 588 / 28, final = code % 28;
                if ((initial == 6 || initial == 7 || initial == 17) && fraction < .25f) return Vector2.zero;
                if ((final == 16 || final == 17 || final == 18) && fraction > .75f) return Vector2.zero;
                bool round = vowel >= 8 && vowel <= 17;
                return new Vector2(envelope * (vowel >= 18 ? .35f : .85f), envelope * (round ? -.4f : .2f));
            }
            return Vector2.zero;
        }
        void Awake() => InitializeForPreview();
        void OnEnable() { if (Application.isPlaying) InitializeForPreview(); }
        public void InitializeForPreview()
        {
            if (surfaces != null) return;
            var filters = GetComponentsInChildren<MeshFilter>(true);
            surfaces = new Surface[filters.Length];
            for (int index = 0; index < filters.Length; index++)
            {
                var filter = filters[index];
                var mesh = Instantiate(filter.sharedMesh);
                mesh.MarkDynamic();
                var rest = mesh.vertices;
                var weight = new float[rest.Length];
                var mouthWeight = new float[rest.Length];
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                for (int i = 0; i < rest.Length; i++)
                {
                    float y = transform.InverseTransformPoint(filter.transform.TransformPoint(rest[i])).y;
                    min = Mathf.Min(min, y); max = Mathf.Max(max, y);
                }
                float height = max - min;
                float mouthY = min + height * MouthHeightFraction(Profile);
                float front = float.PositiveInfinity;
                for (int i = 0; i < rest.Length; i++)
                {
                    var p = transform.InverseTransformPoint(filter.transform.TransformPoint(rest[i]));
                    if (Mathf.Abs(p.x) < height * .045f && Mathf.Abs(p.y - mouthY) < height * .025f) front = Mathf.Min(front, p.z);
                }
                for (int i = 0; i < rest.Length; i++)
                {
                    var p = transform.InverseTransformPoint(filter.transform.TransformPoint(rest[i]));
                    float y = p.y;
                    weight[i] = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(min + (max - min) * .42f, min + (max - min) * .75f, y));
                    mouthWeight[i] = Mathf.Max(0, 1 - Mathf.Abs(p.x) / (height * .035f))
                        * Mathf.Max(0, 1 - Mathf.Abs(y - mouthY) / (height * .015f))
                        * Mathf.Clamp01(1 - (p.z - front) / (height * .035f));
                }
                surfaces[index] = new Surface { Filter = filter, Original = filter.sharedMesh, Animated = mesh,
                    Rest = rest, Buffer = new Vector3[rest.Length], Weight = weight,
                    MouthWeight = mouthWeight, Height = height, MouthY = mouthY };
                filter.sharedMesh = mesh;
            }
        }
        void LateUpdate()
        {
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + 1f / 20;
            var camera = Camera.main;
            if (camera != null && (camera.transform.position - transform.position).sqrMagnitude > 30 * 30) return;
            Sample(Time.time);
        }
        public void Sample(float time)
        {
            if (surfaces == null) return;
            float phase = Profile * 1.37f;
            float breath = Mathf.Sin(time * (1.35f + Profile * .035f) + phase);
            float sway = Mathf.Sin(time * .61f + phase);
            Vector2 mouth = MouthShape(speech, time - speechStart);
            foreach (var surface in surfaces)
            {
                var toVisual = transform.worldToLocalMatrix * surface.Filter.transform.localToWorldMatrix;
                var toMesh = surface.Filter.transform.worldToLocalMatrix * transform.localToWorldMatrix;
                for (int i = 0; i < surface.Rest.Length; i++)
                {
                    Vector3 p = toVisual.MultiplyPoint3x4(surface.Rest[i]);
                    float lip = surface.MouthWeight[i];
                    p.x *= 1 + mouth.y * lip;
                    p.y -= mouth.x * surface.Height * .006f * lip;
                    p.z -= Mathf.Max(0, -mouth.y) * surface.Height * .004f * lip;
                    float weight = surface.Weight[i];
                    p.x *= 1 + breath * Amplitude * weight;
                    p.z *= 1 + breath * Amplitude * 1.5f * weight;
                    p.x += sway * Amplitude * .45f * weight;
                    p.y += breath * Amplitude * .2f * weight;
                    surface.Buffer[i] = toMesh.MultiplyPoint3x4(p);
                }
                surface.Animated.vertices = surface.Buffer;
            }
        }
        void OnDisable() => Release();
        void OnDestroy() => Release();
        void Release()
        {
            speech = null;
            if (surfaces == null) return;
            foreach (var surface in surfaces)
            {
                if (surface.Filter != null) surface.Filter.sharedMesh = surface.Original;
                if (surface.Animated != null)
                {
                    if (Application.isPlaying) Destroy(surface.Animated);
                    else DestroyImmediate(surface.Animated);
                }
            }
            surfaces = null;
        }
    }
}
