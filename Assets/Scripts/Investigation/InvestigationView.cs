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
        UnityEngine.UI.ScrollRect bodyScroll;
        RectTransform bodyScrollRoot;
        string modalHint = "";
        UnityEngine.UI.Text location;
        RectTransform modalPanel;
        UnityEngine.UI.Image modalShade;
        GameObject controls;
        string investigationDate = "";
        string identityName = "";
        string identityLocation = "조조군 문서고";
        string modalCategory = "";
        static readonly Color LabelColor = new Color(.97f, .93f, .84f);
        static readonly Color MutedLabelColor = new Color(.65f, .69f, .68f);

        public void SetPlayerIdentity(string playerName, string locationName = "조조군 문서고")
        {
            CacheLayoutReferences();
            identityName = playerName;
            identityLocation = locationName;
            RefreshDateLabels();
        }

        public void SetInvestigationDate(string date)
        {
            if (investigationDate == date) return;
            investigationDate = date;
            CacheLayoutReferences();
            RefreshDateLabels();
        }

        void RefreshDateLabels()
        {
            string suffix = string.IsNullOrEmpty(investigationDate) ? "" : "  /  " + investigationDate;
            location.text = identityLocation + "  /  " + identityName + suffix;
            eyebrow.text = modalCategory + suffix;
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
            SetupBodyScroll();
        }

        void SetupBodyScroll()
        {
            if (bodyScroll != null) return;
            var root = new GameObject("BodyScroll", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            root.transform.SetParent(body.transform.parent, false);
            bodyScrollRoot = root.GetComponent<RectTransform>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            body.transform.SetParent(viewport.transform, false);
            var content = body.rectTransform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            body.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            body.raycastTarget = true;
            bodyScroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
            bodyScroll.viewport = viewportRect; bodyScroll.content = content;
            bodyScroll.horizontal = false; bodyScroll.vertical = true;
            bodyScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            bodyScroll.scrollSensitivity = 30;
            ResizeChoiceArea();
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

        public System.Collections.IEnumerator FadeEndingToBlack()
        {
            var overlay = new GameObject("EndingBlackout", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            overlay.transform.SetParent(modal.transform.parent, false);
            overlay.transform.SetAsLastSibling();
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = overlay.GetComponent<UnityEngine.UI.Image>();
            shade.raycastTarget = true;
            float elapsed = 0;
            while (elapsed < 1.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                shade.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / 1.2f));
                yield return null;
            }
            shade.color = Color.black;
            yield return new WaitForSecondsRealtime(.8f);
            CacheLayoutReferences();
            modalShade.color = Color.black;
            controls.SetActive(false);
            modal.transform.SetAsLastSibling();
            Destroy(overlay);
        }
        public void SetPrompt(string text) => prompt.text = text;
        public void SetFooterHint(string text) { modalHint = text; footer.text = text; }

        public void ShowModal(string category, string heading, string content, string hint)
        {
            prompt.text = "";
            crosshair.SetActive(false);
            modal.SetActive(true);
            modalCategory = category;
            RefreshDateLabels();
            title.text = heading;
            body.text = content;
            modalHint = hint;
            footer.text = hint;
            foreach (Transform child in choices) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            choiceHeight = 0;
            ResizeChoiceArea();
            if (bodyScroll != null) { bodyScroll.StopMovement(); bodyScroll.verticalNormalizedPosition = 1; body.rectTransform.anchoredPosition = Vector2.zero; }
        }

        public void HideModal()
        {
            modal.SetActive(false);
            crosshair.SetActive(true);
        }

        void LateUpdate()
        {
            if (bodyScroll == null || !modal.activeInHierarchy) return;
            bool overflow = body.preferredHeight > bodyScroll.viewport.rect.height + 1;
            footer.text = overflow ? "휠로 본문 스크롤  ·  " + modalHint : modalHint;
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
            var rect = bodyScrollRoot != null ? bodyScrollRoot : body.rectTransform;
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
