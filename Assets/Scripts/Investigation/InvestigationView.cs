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
        float choiceHeight;
        UnityEngine.UI.Text location;
        RectTransform modalPanel;
        UnityEngine.UI.Image modalShade;
        GameObject controls;
        static readonly Color LabelColor = new Color(.97f, .93f, .84f);
        static readonly Color MutedLabelColor = new Color(.65f, .69f, .68f);

        public void SetPlayerIdentity(string playerName, string locationName = "조조군 문서고")
        {
            CacheLayoutReferences();
            location.text = locationName + "  /  " + playerName;
        }

        public void SetPrologueLayout(bool prologue)
        {
            CacheLayoutReferences();
            modalPanel.anchorMin = prologue ? new Vector2(.035f,.08f) : new Vector2(.12f,.12f);
            modalPanel.anchorMax = prologue ? new Vector2(.49f,.86f) : new Vector2(.88f,.86f);
            modalPanel.offsetMin = modalPanel.offsetMax = Vector2.zero;
            modalShade.color = new Color(.015f,.025f,.03f,prologue ? .08f : .68f);
            body.fontSize = prologue ? 23 : 25;
            footer.fontSize = prologue ? 15 : 18;
            controls.SetActive(!prologue);
        }

        void Awake()
        {
            // Windows target: use the installed Korean system font without redistributing it.
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 28);
            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                label.font = font;
                label.supportRichText = false;
            }
            CacheLayoutReferences();
        }

        void CacheLayoutReferences()
        {
            // The Editor builders also call the layout methods before Awake runs.
            if (location == null)
                location = transform.Find("ObjectiveBackdrop/Location").GetComponent<UnityEngine.UI.Text>();
            if (modalPanel == null)
                modalPanel = modal.transform.Find("Panel").GetComponent<RectTransform>();
            if (modalShade == null)
                modalShade = modal.GetComponent<UnityEngine.UI.Image>();
            if (controls == null)
                controls = transform.Find("Controls").gameObject;
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
            choiceHeight = 0;
            ResizeChoiceArea();
        }

        public void HideModal()
        {
            modal.SetActive(false);
            crosshair.SetActive(true);
        }

        public void AddChoice(string label, UnityAction action, bool interactable = true)
        {
            ReserveChoiceHeight(52);
            var go = new GameObject(label, typeof(RectTransform), typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(choices, false);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(.20f, .25f, .26f, 1);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.interactable = interactable;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .84f, .53f);
            colors.pressedColor = new Color(.72f, .62f, .42f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(action);
            CreateLabel(go.transform, "Label", label, 23, 0, interactable ? LabelColor : MutedLabelColor);
        }

        public void AddNameEntry(Action<string> submit)
        {
            ReserveChoiceHeight(58);
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
            input.textComponent = CreateLabel(go.transform, "Name", "", 25, 6, LabelColor);
            input.placeholder = CreateLabel(go.transform, "Placeholder", "이름 입력 (기본: 이름 없는 참모)", 25, 6, MutedLabelColor);
            AddChoice("이 이름으로 시작", () => submit(input.text));
            input.ActivateInputField();
        }

        void ReserveChoiceHeight(float height)
        {
            choiceHeight += height + (choiceHeight > 0 ? 8 : 0);
            ResizeChoiceArea();
        }

        void ResizeChoiceArea()
        {
            choices.offsetMax = new Vector2(choices.offsetMax.x, 62 + choiceHeight);
            var rect = body.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(40, 80 + choiceHeight);
            rect.offsetMax = new Vector2(-40, -145);
        }

        UnityEngine.UI.Text CreateLabel(Transform parent, string name, string content, int fontSize, float verticalPadding, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18, verticalPadding); rect.offsetMax = new Vector2(-18, -verticalPadding);
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = font; text.fontSize = fontSize; text.text = content;
            text.color = color; text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false; text.raycastTarget = false;
            return text;
        }
    }
}
