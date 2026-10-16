using System.IO;
using UnityEngine;

namespace RedCliffMystery
{
    // Small visual-only breathing and upper-body sway. NPC roots and hit volumes stay fixed.
    public sealed class VarcoIdleMotion : MonoBehaviour
    {
        public int Profile;
        public float Amplitude = .005f;
        // Global visibility floor for older scene instances; do not amplify breathing scale.
        public const float IdlePostureDistance = .014f;
        [Range(0, 1)] public float MouthHeightOverride;
        public TextAsset ConversationMotion;
        public TextAsset RoleActivity;
        public TextAsset DetailMotion;
        DetailMotionClip detailClip;
        RefinedHand[] refinedHands;
        SleeveLining[] sleeveLinings;
        public SkinnedMeshRenderer HeldRecord;
        public Vector3 HeldRecordOffset;
        struct Surface
        {
            public MeshFilter Filter;
            public WristSurface Wrist;
            public Mesh Original, Animated;
            public Vector3[] Rest, Buffer;
            public Vector3[] Normals;
            public RoleActivityClip.Vertex[] ActivityOffsets;
            public float[] Weight;
            public float[] MouthWeight;
            public float Height, MouthY;
            public ConversationMotionClip.Offset[] GestureOffsets;
        }
        Surface[] surfaces;
        float nextSample;
        string speech;
        float speechStart;
        float speechDuration, motionStopTime;
        Vector3 stoppedMotion, entryMotion;
        ConversationMotionClip conversationClip;
        RoleActivityClip activityClip;
        static readonly string[] Speakers = { "조조", "설평", "두석", "채모", "장윤", "기록 담당자", "전령", "교대 병사", "동석 병사", "한진" };
        static readonly float[] MouthHeights = { .840f, .872f, .870f, .875f, .855f, .875f, .862f, .866f, .867f, .875f };
        public static float MouthHeightFraction(int profile) => MouthHeights[Mathf.Clamp(profile, 0, MouthHeights.Length - 1)];
        public static void StopSpeech()
        {
            Investigation.ConversationPresentation.End();
            foreach (var motion in FindObjectsByType<VarcoIdleMotion>()) motion.StopTalking(Time.time);
        }
        public static void Speak(string speaker, string text)
        {
            StopSpeech();
            int profile = System.Array.IndexOf(Speakers, speaker);
            if(speaker=="문서 전달자")profile=6;
            if(speaker=="발급 기록 담당자")profile=5;
            if(speaker=="연락관 한진")profile=9;
            if (speaker == "병사") profile = 8;
            foreach (var motion in FindObjectsByType<VarcoIdleMotion>())
                if (motion.Profile == profile) { motion.StartSpeech(text, Time.time); Investigation.ConversationPresentation.Begin(motion); }
        }
        public void StartSpeech(string text, float time)
        {
            entryMotion = ConversationWeights(time);
            speech = text; speechStart = time; speechDuration = 0;
            if (text != null) foreach (char c in text)
                speechDuration += char.IsWhiteSpace(c) ? .07f : char.IsPunctuation(c) ? .22f : .14f;
        }
        public void StopTalking(float time)
        {
            stoppedMotion = ConversationWeights(time); motionStopTime = time; speech = null;
        }
        public Vector3 ConversationWeights(float time)
        {
            if (conversationClip == null) return Vector3.zero;
            if (string.IsNullOrEmpty(speech))
                return stoppedMotion * (1 - Mathf.SmoothStep(0, 1, (time - motionStopTime) / .3f));
            float elapsed = time - speechStart;
            if (elapsed < 0 || elapsed >= speechDuration) return Vector3.zero;
            float enter = Mathf.SmoothStep(0, 1, elapsed / .3f);
            float leave = Mathf.SmoothStep(0, 1, (speechDuration - elapsed) / .3f);
            return Vector3.Lerp(entryMotion, conversationClip.Sample(elapsed), enter) * leave;
        }
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
            conversationClip = ConversationMotion != null ? ConversationMotionClip.Read(ConversationMotion) : null;
            activityClip = RoleActivity != null ? RoleActivityClip.Read(RoleActivity) : null;
            detailClip = DetailMotion != null ? DetailMotionClip.Read(DetailMotion) : null;
            refinedHands = GetComponentsInChildren<RefinedHand>(true);
            sleeveLinings = GetComponentsInChildren<SleeveLining>(true);
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
                float mouthY = min + height * (MouthHeightOverride > 0 ? MouthHeightOverride : MouthHeightFraction(Profile));
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
                surfaces[index] = new Surface { Filter = filter, Wrist = filter.GetComponent<WristSurface>(), Original = filter.sharedMesh, Animated = mesh,
                    Rest = rest, Buffer = new Vector3[rest.Length], Normals = mesh.normals, Weight = weight,
                    ActivityOffsets = activityClip != null && activityClip.Profile == Profile && activityClip.VertexCount == rest.Length
                        ? activityClip.Bind(filter.transform.worldToLocalMatrix * transform.localToWorldMatrix) : null,
                    MouthWeight = mouthWeight, Height = height, MouthY = mouthY,
                    GestureOffsets = conversationClip != null && conversationClip.Profile == Profile && conversationClip.VertexCount == rest.Length
                        ? conversationClip.Bind(filter.transform.worldToLocalMatrix * transform.localToWorldMatrix) : null };
                filter.sharedMesh = mesh;
                if(surfaces[index].ActivityOffsets != null)
                {
                    var bounds=mesh.bounds;
                    foreach(var v in surfaces[index].ActivityOffsets)
                    {
                        bounds.Encapsulate(rest[v.Index]+v.Rest);
                        bounds.Encapsulate(rest[v.Index]+v.A);
                        bounds.Encapsulate(rest[v.Index]+v.B);
                    }
                    // Authored meshes can have a 100x import scale. Padding is in world metres,
                    // whereas Mesh.bounds is local; using metres directly made the actor ~20m tall.
                    float padding = detailClip != null ? .18f : .001f;
                    Vector3 scale = filter.transform.lossyScale;
                    bounds.Expand(new Vector3(padding / Mathf.Max(Mathf.Abs(scale.x), .0001f),
                        padding / Mathf.Max(Mathf.Abs(scale.y), .0001f), padding / Mathf.Max(Mathf.Abs(scale.z), .0001f)));
                    mesh.bounds=bounds;
                }
            }
        }
        void LateUpdate()
        {
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + 1f / 20;
            var camera = Camera.main;
            if (camera != null && (camera.transform.position - transform.position).sqrMagnitude > 30 * 30) return;
            if (camera != null && !HasSpeech && !InCameraView(camera)) return;
            Sample(Time.time);
        }
        static readonly Plane[] frustum = new Plane[6];
        static int frustumFrame = -1;
        static Camera frustumCamera;
        // Out-of-view figures skip the vertex update; the pose is time-driven so it catches up on the next sample.
        bool InCameraView(Camera camera)
        {
            if (surfaces == null) return false;
            if (frustumFrame != Time.frameCount || frustumCamera != camera)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, frustum);
                frustumFrame = Time.frameCount;
                frustumCamera = camera;
            }
            foreach (var surface in surfaces)
            {
                if (surface.Filter == null || !surface.Filter.TryGetComponent<Renderer>(out var renderer)) continue;
                var bounds = renderer.bounds;
                bounds.Expand(.5f);
                if (GeometryUtility.TestPlanesAABB(frustum, bounds)) return true;
            }
            return false;
        }
        public void Sample(float time)
        {
            if (surfaces == null) return;
            float phase = Profile * 1.37f;
            float breath = Mathf.Sin(time * (1.35f + Profile * .035f) + phase);
            float sway = Mathf.Sin(time * .61f + phase);
            if(HeldRecord != null)
                HeldRecord.transform.localPosition=HeldRecordOffset+new Vector3(sway*Mathf.Max(Amplitude*.9f,IdlePostureDistance)*.5f,breath*Amplitude*.275f,0);
            Vector3 gesture = ConversationWeights(time);
            Vector3 activity = activityClip != null ? activityClip.Sample(time) : Vector3.zero;
            if(activityClip != null)
            {
                activity = Vector3.Lerp(activity,Vector3.right,Mathf.Clamp01(gesture.magnitude)*.8f);
                gesture.z *= .2f;
            }
            if(HeldRecord != null && activityClip != null)
            {
                HeldRecord.SetBlendShapeWeight(0,activity.y*100);
                HeldRecord.SetBlendShapeWeight(1,activity.z*100);
            }
            // Quiet, staggered glances and posture adjustments remain active between lines.
            // Reuse the authored neck/arm masks so feet and the sentry's spear hand stay fixed.
            float idleBlend = activityClip != null ? 0 : Mathf.Clamp01(Amplitude / .005f) * (1 - Mathf.Clamp01(gesture.magnitude * 2));
            float cycle = time * (.32f + Profile * .009f) + phase;
            gesture += new Vector3(.12f * (1 + Mathf.Sin(cycle)),
                .38f * Mathf.Sin(cycle * .73f), .065f * (1 + Mathf.Sin(cycle * .61f))) * idleBlend;
            foreach (var surface in surfaces)
            {
                var toVisual = transform.worldToLocalMatrix * surface.Filter.transform.localToWorldMatrix;
                var toMesh = surface.Filter.transform.worldToLocalMatrix * transform.localToWorldMatrix;
                for (int i = 0; i < surface.Rest.Length; i++)
                {
                    Vector3 p = toVisual.MultiplyPoint3x4(surface.Rest[i]);
                    // No speech-driven mouth deformation: preserve the authored face.
                    float weight = surface.Weight[i];
                    p.x *= 1 + breath * Amplitude * weight;
                    p.z *= 1 + breath * Amplitude * 1.5f * weight;
                    // Visible weight shifts in visual-space metres; foot weights stay zero.
                    float posture = Amplitude > 0 ? Mathf.Max(Amplitude * .9f, IdlePostureDistance) : 0;
                    p.x += sway * posture * weight;
                    p.y += breath * Mathf.Max(Amplitude, 0) * .55f * weight;
                    surface.Buffer[i] = toMesh.MultiplyPoint3x4(p);
                }
                ConversationMotionClip.Apply(surface.Buffer, surface.GestureOffsets, gesture);
                RoleActivityClip.Apply(surface.Buffer,surface.Normals,surface.ActivityOffsets,activity);
                if(detailClip != null && detailClip.Profile == Profile)detailClip.Apply(surface.Buffer,surface.Normals,toVisual,toMesh,time);
                if(surface.Wrist!=null)surface.Wrist.Apply(surface.Buffer,surface.Normals);
                surface.Animated.vertices = surface.Buffer;
                if(surface.ActivityOffsets != null)surface.Animated.normals=surface.Normals;
                foreach(var hand in refinedHands)hand.Follow(surface.Filter,surface.Buffer);
                foreach(var lining in sleeveLinings)lining.Follow(surface.Filter,surface.Buffer);
            }
        }
        void OnDisable() => Release();
        void OnDestroy() => Release();
        void Release()
        {
            speech = null;
            stoppedMotion = entryMotion = Vector3.zero;
            conversationClip = null;
            activityClip = null;
            detailClip = null;
            refinedHands = null;
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

namespace RedCliffMystery
{
    // Blender shape-key deltas in visual-local coordinates, with baked action weights.
    // Sparse offsets preserve the existing mesh, UVs, mouth movement and planted feet.
    public sealed class ConversationMotionClip
    {
        public struct Offset
        {
            public int Index;
            public Vector3 Basis, Nod, Turn, Hand;
        }
        public int Profile { get; private set; }
        public int VertexCount { get; private set; }
        public float Duration => (frames.Length - 1) / (float)fps;
        public Offset[] Offsets { get; private set; }
        Vector3[] frames;
        int fps;

        static Vector3 ReadVector(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static ConversationMotionClip Read(TextAsset asset)
        {
            using (var stream = new MemoryStream(asset.bytes))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != 0x4D444352 || reader.ReadInt32() != 1)
                    throw new InvalidDataException("Invalid conversation motion: " + asset.name);
                var clip = new ConversationMotionClip { Profile = reader.ReadInt32(), VertexCount = reader.ReadInt32(), fps = reader.ReadInt32() };
                int frameCount = reader.ReadInt32(), count = reader.ReadInt32();
                if (clip.VertexCount < 1 || clip.fps < 1 || frameCount < 2 || frameCount > 10000 || count < 0 || count > clip.VertexCount
                    || stream.Length != 28L + frameCount * 12L + count * 52L)
                    throw new InvalidDataException("Invalid motion dimensions: " + asset.name);
                clip.frames = new Vector3[frameCount];
                for (int i = 0; i < frameCount; i++) clip.frames[i] = ReadVector(reader);
                clip.Offsets = new Offset[count];
                int previous = -1;
                for (int i = 0; i < count; i++)
                {
                    var offset = new Offset { Index = reader.ReadInt32(), Basis = ReadVector(reader), Nod = ReadVector(reader), Turn = ReadVector(reader), Hand = ReadVector(reader) };
                    if (offset.Index <= previous || offset.Index >= clip.VertexCount) throw new InvalidDataException("Invalid motion vertex index");
                    clip.Offsets[i] = offset; previous = offset.Index;
                }
                return clip;
            }
        }
        public Vector3 Sample(float elapsed)
        {
            float frame = Mathf.Repeat(Mathf.Max(0, elapsed), Duration) * fps;
            int a = Mathf.Min((int)frame, frames.Length - 2);
            return Vector3.LerpUnclamped(frames[a], frames[a + 1], frame - a);
        }
        public Offset[] Bind(Matrix4x4 visualToMesh)
        {
            var bound = new Offset[Offsets.Length];
            for (int i = 0; i < bound.Length; i++)
            {
                var o = Offsets[i];
                o.Nod = visualToMesh.MultiplyVector(o.Nod);
                o.Turn = visualToMesh.MultiplyVector(o.Turn);
                o.Hand = visualToMesh.MultiplyVector(o.Hand);
                bound[i] = o;
            }
            return bound;
        }
        public static void Apply(Vector3[] vertices, Offset[] offsets, Vector3 weights)
        {
            if (offsets == null || weights.sqrMagnitude < 1e-12f) return;
            foreach (var o in offsets) vertices[o.Index] += o.Nod * weights.x + o.Turn * weights.y + o.Hand * weights.z;
        }
    }
}
