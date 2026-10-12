using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RedCliffMystery.Investigation
{
    using UGUI = UnityEngine.UI;

    // Title screen over the command-tent cover art: brush logo, tagline and a two-item
    // ink-brush menu (게임 시작 / 종료). Layout is authored in 1920x1080 reference pixels
    // measured from the top-left; the canvas matches height, so the left column keeps
    // its proportions on 16:9, 16:10 and 4:3 windows alike.
    public sealed class InvestigationTitleScreen : MonoBehaviour
    {
        public const string GameTitle = "적벽추리";
        public const string Subtitle = "진실을 밝혀라, 역사가 바뀌기 전에";
        const string Art = "Title/";
        // The importer rounds the NPOT cover to 2048x1024, so its texture size no longer
        // reflects the authored 16:9 frame.
        const float CoverAspect = 16f / 9f;
        const string Hair = " ", Thin = " ";

        public UGUI.Button StartButton { get; private set; }
        public UGUI.Button QuitButton { get; private set; }
        public UGUI.Button ArchiveButton { get; private set; }
        public UGUI.RawImage TitleLogo { get; private set; }

        static readonly Color Ivory = new Color(.96f, .91f, .81f);
        static readonly Color IvoryDim = new Color(.83f, .78f, .68f);
        static readonly Color Gold = new Color(.79f, .63f, .40f);
        static readonly Color BrightGold = new Color(.95f, .77f, .48f);
        static readonly Color Ink = new Color(.016f, .016f, .024f);
        static readonly Color Crimson = new Color(.56f, .12f, .08f);

        sealed class MenuItem
        {
            public UGUI.Button button;
            public UGUI.RawImage brush;
            public UGUI.Text label;
            public UGUI.Image mark;
            public float lit;
        }

        Font font;
        bool leaving;
        CanvasGroup group;
        RectTransform cover;
        float shownAt;
        UGUI.Button lastSelected;
        readonly List<MenuItem> items = new List<MenuItem>();
        readonly List<Texture2D> generated = new List<Texture2D>();

        public static InvestigationTitleScreen Show(Font font, Action start)
        {
            var root = new GameObject("TitleScreen", typeof(RectTransform), typeof(Canvas),
                typeof(UGUI.CanvasScaler), typeof(UGUI.GraphicRaycaster), typeof(CanvasGroup));
            var screen = root.AddComponent<InvestigationTitleScreen>();
            var serif = Resources.Load<Font>(Art + "RedCliffsSerif-SemiBold");
            screen.font = serif != null ? serif : font;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<UGUI.CanvasScaler>();
            scaler.uiScaleMode = UGUI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UGUI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            screen.group = root.GetComponent<CanvasGroup>();
            screen.group.alpha = 0f;
            screen.shownAt = Time.unscaledTime;
            screen.Build(start);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            screen.Select(screen.StartButton);
            return screen;
        }

        void Build(Action start)
        {
            Fill("Backdrop", transform, Ink);
            var art = Resources.Load<Texture2D>(Art + "RedCliffsTitleBackground");
            if (art != null) {
                var image = new GameObject("CoverArt", typeof(RectTransform), typeof(UGUI.RawImage), typeof(UGUI.AspectRatioFitter));
                image.transform.SetParent(transform, false);
                var raw = image.GetComponent<UGUI.RawImage>();
                raw.texture = art;
                raw.raycastTarget = false;
                var fitter = image.GetComponent<UGUI.AspectRatioFitter>();
                fitter.aspectMode = UGUI.AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = CoverAspect;
                cover = (RectTransform)image.transform;
            }
            // Darken the tent side for the text column, settle the bottom edge, soft vignette.
            Scrim("ShadeLeft", MakeGradient(256, 1, (u, v) => Ramp(u, new[] { 0f, .26f, .46f, .60f }, new[] { .72f, .55f, .18f, 0f })));
            Scrim("ShadeBottom", MakeGradient(1, 128, (u, v) => .55f * Mathf.Clamp01(1f - v / .22f)));
            Scrim("Vignette", MakeGradient(96, 96, (u, v) => {
                float dx = (u - .60f) / .85f, dy = (v - .55f) / .78f;
                float t = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - .55f) / .45f);
                return .35f * t * t * (3f - 2f * t);
            }));

            var column = Rect("TitleContent", transform);
            column.anchorMin = Vector2.zero; column.anchorMax = new Vector2(0, 1); column.pivot = new Vector2(0, 1);
            column.sizeDelta = new Vector2(1000, 0); column.anchoredPosition = Vector2.zero;

            var series = Label(column, "Series", Track("RED CLIFFS DEDUCTION", Thin + Thin + Hair), 19, Gold);
            Place(series.rectTransform, 154, 260, 760, 30);

            var logo = Resources.Load<Texture2D>(Art + "RedCliffsTitleLogo");
            if (logo != null) {
                TitleLogo = new GameObject("TitleLogo", typeof(RectTransform), typeof(UGUI.RawImage)).GetComponent<UGUI.RawImage>();
                TitleLogo.transform.SetParent(column, false);
                TitleLogo.texture = logo;
                TitleLogo.raycastTarget = false;
                Place(TitleLogo.rectTransform, 96, 234, 800, 306);
            } else {
                var title = Label(column, "Title", GameTitle, 150, Ivory);
                title.fontStyle = FontStyle.Bold;
                Place(title.rectTransform, 146, 290, 760, 190);
                Shadowed(title);
            }

            var rule = new GameObject("Rule", typeof(RectTransform), typeof(UGUI.RawImage)).GetComponent<UGUI.RawImage>();
            rule.transform.SetParent(column, false);
            rule.texture = MakeGradient(64, 1, (u, v) => 1f - u, Color.white);
            rule.color = Gold;
            rule.raycastTarget = false;
            Place(rule.rectTransform, 152, 505, 118, 2);

            var subtitle = Label(column, "Subtitle", Subtitle, 34, Ivory);
            Place(subtitle.rectTransform, 150, 522, 820, 54);
            Shadowed(subtitle);

            StartButton = MenuButton(column, "게임 시작", 0, () => {
                if (leaving) return;
                leaving = true;
                gameObject.SetActive(false);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                start();
                Destroy(gameObject);
            });
            ArchiveButton = MenuButton(column, "기록관", 1, () => {
                gameObject.SetActive(false);
                RecordArchiveScreen.Show(font, () => { gameObject.SetActive(true); Select(ArchiveButton); });
            });
            QuitButton = MenuButton(column, "종료", 2, Quit);
            var menu = new[] { StartButton, ArchiveButton, QuitButton };
            for(int i=0;i<menu.Length;i++) { var n=menu[i].navigation;n.mode=UGUI.Navigation.Mode.Explicit;n.selectOnUp=menu[(i+2)%3];n.selectOnDown=menu[(i+1)%3];menu[i].navigation=n; }

            var caption = Label(transform, "Caption", Track("서기 208년 · 조조군 진영", Hair), 18, Gold);
            Corner(caption.rectTransform, new Vector2(0, 0), new Vector2(150, 44), new Vector2(800, 32));
            var version = Label(transform, "Version", "테스트 데모 " + Application.version, 16, new Color(Ivory.r, Ivory.g, Ivory.b, .55f));
            version.alignment = TextAnchor.MiddleRight;
            Corner(version.rectTransform, new Vector2(1, 0), new Vector2(-64, 44), new Vector2(600, 32));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (group != null && group.alpha < 1f) group.alpha = Mathf.Min(1f, group.alpha + dt / .8f);
            if (cover != null) {
                // Very slow push-in so the still frame breathes.
                float s = 1f + .03f * (.5f - .5f * Mathf.Cos((Time.unscaledTime - shownAt) * Mathf.PI * 2f / 48f));
                cover.localScale = new Vector3(s, s, 1f);
            }
            var events = EventSystem.current;
            var selected = events != null ? events.currentSelectedGameObject : null;
            // Clicking empty space clears the selection; put it back so arrows and Enter keep working.
            if (events != null && !leaving && lastSelected != null && !IsMenu(selected)) {
                events.SetSelectedGameObject(lastSelected.gameObject);
                selected = lastSelected.gameObject;
            }
            foreach (var item in items) {
                bool on = selected == item.button.gameObject;
                if (on) lastSelected = item.button;
                item.lit = Mathf.MoveTowards(item.lit, on ? 1f : 0f, dt * 7f);
                float e = item.lit * item.lit * (3f - 2f * item.lit);
                var c = item.brush.color; c.a = e; item.brush.color = c;
                item.brush.rectTransform.localScale = new Vector3(.82f + .18f * e, 1f, 1f);
                item.label.color = Color.Lerp(IvoryDim, new Color(1f, .96f, .88f), e);
                item.mark.color = Color.Lerp(new Color(Gold.r, Gold.g, Gold.b, .5f), BrightGold, e);
            }
        }

        void OnDestroy()
        {
            foreach (var texture in generated) if (texture != null) Destroy(texture);
            generated.Clear();
        }

        void Quit()
        {
            if (leaving) return;
            leaving = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Select(UGUI.Button button)
        {
            if (button == null) return;
            lastSelected = button;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        bool IsMenu(GameObject go)
        {
            if (go == null) return false;
            foreach (var item in items) if (item.button.gameObject == go) return true;
            return false;
        }

        // Text-only menu entry: an ink brush stroke sweeps in behind the selected item.
        UGUI.Button MenuButton(Transform parent, string text, int index, UnityEngine.Events.UnityAction action)
        {
            var hit = Rect(text, parent);
            Place(hit, 116, 648 + index * 82, 400, 68);
            var area = hit.gameObject.AddComponent<UGUI.Image>();
            area.color = new Color(0, 0, 0, .004f);
            area.canvasRenderer.cullTransparentMesh = false;
            var item = new MenuItem();

            item.brush = new GameObject("Brush", typeof(RectTransform), typeof(UGUI.RawImage)).GetComponent<UGUI.RawImage>();
            item.brush.transform.SetParent(hit, false);
            var stroke = Resources.Load<Texture2D>(Art + "RedCliffsTitleBrush");
            item.brush.texture = stroke;
            item.brush.color = stroke != null ? new Color(1, 1, 1, 0) : new Color(Crimson.r, Crimson.g, Crimson.b, 0);
            item.brush.raycastTarget = false;
            Place(item.brush.rectTransform, -18, -14, 436, 96);

            item.mark = Rect("Mark", hit).gameObject.AddComponent<UGUI.Image>();
            item.mark.raycastTarget = false;
            var mark = item.mark.rectTransform;
            mark.anchorMin = mark.anchorMax = new Vector2(0, 1);
            mark.pivot = new Vector2(.5f, .5f);
            mark.anchoredPosition = new Vector2(38, -34);
            mark.sizeDelta = new Vector2(8, 8);
            mark.localRotation = Quaternion.Euler(0, 0, 45);

            item.label = Label(hit, "Label", Track(text, Thin + Hair), 31, IvoryDim);
            Place(item.label.rectTransform, 60, 0, 330, 68);
            Shadowed(item.label, .9f);

            var button = hit.gameObject.AddComponent<UGUI.Button>();
            button.targetGraphic = area;
            button.transition = UGUI.Selectable.Transition.None;
            button.onClick.AddListener(action);
            // Mouse hover moves the selection, so only one entry is ever lit.
            var trigger = hit.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (!leaving) Select(button); });
            trigger.triggers.Add(enter);

            item.button = button;
            items.Add(item);
            return button;
        }

        static void Link(UGUI.Button upper, UGUI.Button lower)
        {
            var a = upper.navigation;
            a.mode = UGUI.Navigation.Mode.Explicit; a.selectOnDown = lower; a.selectOnUp = lower;
            upper.navigation = a;
            var b = lower.navigation;
            b.mode = UGUI.Navigation.Mode.Explicit; b.selectOnUp = upper; b.selectOnDown = upper;
            lower.navigation = b;
        }

        Texture2D MakeGradient(int width, int height, Func<float, float, float> alpha) => MakeGradient(width, height, alpha, Ink);

        Texture2D MakeGradient(int width, int height, Func<float, float, float> alpha, Color tint)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "TitleGradient"
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) {
                    float u = width > 1 ? x / (width - 1f) : 0f, v = height > 1 ? y / (height - 1f) : 0f;
                    pixels[y * width + x] = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alpha(u, v)));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            generated.Add(texture);
            return texture;
        }

        static float Ramp(float t, float[] stops, float[] values)
        {
            if (t <= stops[0]) return values[0];
            for (int i = 1; i < stops.Length; i++)
                if (t <= stops[i]) return Mathf.Lerp(values[i - 1], values[i], (t - stops[i - 1]) / (stops[i] - stops[i - 1]));
            return values[values.Length - 1];
        }

        static string Track(string text, string gap)
        {
            var builder = new System.Text.StringBuilder(text.Length * (gap.Length + 1));
            for (int i = 0; i < text.Length; i++) {
                if (i > 0) builder.Append(gap);
                builder.Append(text[i]);
            }
            return builder.ToString();
        }

        void Scrim(string name, Texture2D texture)
        {
            var scrim = Rect(name, transform).gameObject.AddComponent<UGUI.RawImage>();
            scrim.texture = texture;
            scrim.raycastTarget = false;
        }

        // Top-left placement in 1920x1080 reference pixels.
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Corner(RectTransform rect, Vector2 corner, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static UGUI.Image Fill(string name, Transform parent, Color color)
        {
            var panel = Rect(name, parent).gameObject.AddComponent<UGUI.Image>();
            panel.color = color;
            return panel;
        }

        static void Shadowed(UGUI.Graphic graphic, float alpha = .85f)
        {
            var shadow = graphic.gameObject.AddComponent<UGUI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, alpha);
            shadow.effectDistance = new Vector2(1.5f, -2f);
        }

        UGUI.Text Label(Transform parent, string name, string text, int size, Color color)
        {
            var label = Rect(name, parent).gameObject.AddComponent<UGUI.Text>();
            label.font = font; label.fontSize = size; label.text = text; label.color = color;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
