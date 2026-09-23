using System;
using UnityEngine;
using UnityEngine.Events;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationView : MonoBehaviour
    {
        public UnityEngine.UI.Text objective;
        public UnityEngine.UI.Text prompt;
        public GameObject crosshair;
        public GameObject modal;
        public UnityEngine.UI.Text eyebrow;
        public UnityEngine.UI.Text title;
        public UnityEngine.UI.Text body;
        public UnityEngine.UI.Text footer;
        public RectTransform choices;
        public Font font;

        public void SetPlayerIdentity(string playerName)
        {
            var location = transform.Find("ObjectiveBackdrop/Location").GetComponent<UnityEngine.UI.Text>();
            location.text = "조조군 문서고  /  임시 참모 · " + playerName;
        }

        void Awake()
        {
            // Windows target: use the installed Korean system font without redistributing it.
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 28);
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.font = font;
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.supportRichText = false;
        }

        public void SetObjective(string text) => objective.text = text;
        public void SetPrompt(string text) => prompt.text = text;

        public void ShowModal(string category, string heading, string content, string hint)
        {
            prompt.text = "";
            crosshair.SetActive(false);
            modal.SetActive(true);
            eyebrow.text = category;
            title.text = heading;
            body.text = content;
            footer.text = hint;
            foreach (Transform child in choices) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        public void HideModal()
        {
            modal.SetActive(false);
            crosshair.SetActive(true);
        }

        public void AddChoice(string label, UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(choices, false);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(.20f, .25f, .26f, 1);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .84f, .53f);
            colors.pressedColor = new Color(.72f, .62f, .42f);
            button.colors = colors;
            button.onClick.AddListener(action);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(go.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18, 0); rect.offsetMax = new Vector2(-18, 0);
            var text = textGo.GetComponent<UnityEngine.UI.Text>();
            text.font = font; text.fontSize = 23; text.color = new Color(.97f, .93f, .84f);
            text.alignment = TextAnchor.MiddleLeft; text.text = label; text.raycastTarget = false;
            text.supportRichText = false;
        }

        public void AddNameEntry(Action<string> submit)
        {
            var go = new GameObject("PlayerNameInput", typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.InputField), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(choices, false);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(.13f, .19f, .20f);
            var input = go.GetComponent<UnityEngine.UI.InputField>();
            input.characterLimit = 16;
            input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            input.contentType = UnityEngine.UI.InputField.ContentType.Standard;
            input.customCaretColor = true;
            input.caretColor = new Color(1f,.85f,.5f);
            var label = InputLabel(go.transform, "Name", "");
            input.textComponent = label;
            var placeholder = InputLabel(go.transform, "Placeholder", "이름 입력 (기본: 이름 없는 참모)");
            placeholder.color = new Color(.65f,.69f,.68f);
            input.placeholder = placeholder;
            AddChoice("이 이름으로 시작", () => submit(input.text));
            input.ActivateInputField();
        }

        UnityEngine.UI.Text InputLabel(Transform parent, string name, string content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18, 6); rect.offsetMax = new Vector2(-18, -6);
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = font; text.fontSize = 25; text.text = content;
            text.color = new Color(.97f,.93f,.84f); text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false; text.raycastTarget = false;
            return text;
        }
    }
}
