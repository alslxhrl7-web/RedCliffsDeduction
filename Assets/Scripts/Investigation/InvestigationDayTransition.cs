using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RedCliffMystery.Investigation
{
    // A scene-like interlude on top of the existing scene preserves the investigation state.
    public sealed class InvestigationDayTransition : MonoBehaviour
    {
        GameObject overlay;
        CanvasGroup shade;
        Text heading, details;
        Action complete;

        public void Play(Font font, string day, string date, Action onComplete)
        {
            Cancel();
            if (overlay == null) CreateOverlay(font);
            heading.text = day;
            details.text = date;
            heading.enabled = details.enabled = false;
            complete = onComplete;
            overlay.SetActive(true);
            shade.alpha = 0;
            StartCoroutine(Animate());
        }

        void CreateOverlay(Font font)
        {
            overlay = new GameObject("DayTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            // Keep a root canvas so its screen coverage is independent of the dialogue panel.
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            shade = overlay.GetComponent<CanvasGroup>();
            var background = new GameObject("Night", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(overlay.transform, false);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            background.GetComponent<Image>().color = Color.black;
            heading = Label("Day", font, 52, new Vector2(.08f, .49f), new Vector2(.92f, .7f));
            details = Label("Date", font, 23, new Vector2(.08f, .25f), new Vector2(.92f, .49f));
        }

        Text Label(string name, Font font, int size, Vector2 min, Vector2 max)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(overlay.transform, false);
            Stretch(child.GetComponent<RectTransform>(), min, max);
            var label = child.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = new Color(.94f, .88f, .7f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        IEnumerator Animate()
        {
            yield return Fade(0, 1, .45f);
            // Background ambience belongs to the session and continues across the blackout.
            heading.enabled = details.enabled = true;
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Fade(1, 0, .55f);
            Finish();
        }
        IEnumerator Fade(float from, float to, float duration)
        {
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime) {
                shade.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            shade.alpha = to;
        }
        void Finish() {
            var next = complete;
            complete = null;
            overlay.SetActive(false);
            next?.Invoke();
        }
        public void Cancel() {
            StopAllCoroutines();
            complete = null;
            if (overlay != null) overlay.SetActive(false);
        }
#if UNITY_EDITOR
        // Logic checks skip animation time; visual timing needs a separate Play check.
        public void FinishForEditorCheck() { StopAllCoroutines(); if (complete != null) Finish(); }
#endif
        void OnDestroy() {
            Cancel();
            if (overlay != null) Destroy(overlay);
        }
    }
}
