using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class EvidenceDocumentView : MonoBehaviour
    {
        public Texture2D maintenanceOrder, personnelRecord, dutyRecord, householdRegistry, secretLetter;
        RectTransform panel;
        RectTransform imageArea;
        UnityEngine.UI.RawImage image;
        GameObject diagram;
        Texture2D tokenPattern;
        public bool Visible => panel != null && panel.gameObject.activeSelf;

        public void Show(string heading, RectTransform parent, Font font)
        {
            Texture2D texture = null;
            if (heading == "동쪽 수채의 정비 닷새") texture = maintenanceOrder;
            else if (heading == "형주 인력 인계 명부") texture = personnelRecord;
            else if (heading == "이전 근무 기록") texture = dutyRecord;
            else if (heading == "형주 남군 강릉의 신고 신원 대조" || heading == "대조 범위" || heading == "이름과 가구 항목") texture = householdRegistry;
            else if (heading == "목연에게 내린 지시" || heading == "밀서와 무엇을 비교할까?") texture = secretLetter;
            if (panel == null && texture != null)
            {
                panel = NewRect("EvidenceDocumentPreview", parent);
                var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
                background.color = new Color(.12f,.15f,.15f); background.raycastTarget = false;
                imageArea = NewRect("DocumentArea", panel);
                var picture = NewRect("Document", imageArea);
                picture.anchorMin = Vector2.zero; picture.anchorMax = Vector2.one;
                picture.offsetMin = new Vector2(8, 8); picture.offsetMax = new Vector2(-8,-8);
                image = picture.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false;
                var aspect = picture.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
                diagram = NewRect("ContactTokenPattern", panel).gameObject;
                var dr = (RectTransform)diagram.transform;
                dr.anchorMin = new Vector2(.05f,0); dr.anchorMax = new Vector2(.95f,.35f);
                dr.offsetMin = new Vector2(0,4); dr.offsetMax = Vector2.zero;
                var drawing = diagram.AddComponent<UnityEngine.UI.RawImage>();
                tokenPattern = CreateTokenPattern(); drawing.texture = tokenPattern; drawing.raycastTarget = false;
                var labelRect = NewRect("PatternLabel", dr);
                labelRect.anchorMin = new Vector2(0,0); labelRect.anchorMax = new Vector2(1,.25f);
                var label = labelRect.gameObject.AddComponent<UnityEngine.UI.Text>();
                label.font = font; label.fontSize = 16; label.text = "연락패 확인용 본";
                label.color = new Color(.15f,.12f,.08f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            }
            if (panel == null) return;
            panel.gameObject.SetActive(texture != null);
            if (texture == null) return;
            image.texture = texture;
            image.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
            diagram.SetActive(texture == secretLetter);
            imageArea.anchorMin = new Vector2(0, texture == secretLetter ? .35f : 0);
        }

        public void Resize(float choiceHeight)
        {
            if (panel == null) return;
            panel.anchorMin = new Vector2(.59f,0); panel.anchorMax = Vector2.one;
            panel.offsetMin = new Vector2(10,80+choiceHeight); panel.offsetMax = new Vector2(-40,-145);
        }

        void OnDestroy() { if (tokenPattern != null) Destroy(tokenPattern); }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }

        // The three marks correspond to the physical token's unequal notch spacing.
        static Texture2D CreateTokenPattern()
        {
            var texture = new Texture2D(360,200,TextureFormat.RGBA32,false);
            var pixels = new Color[360*200];
            for (int y=0;y<200;y++) for (int x=0;x<360;x++)
            {
                bool edge = x>=140 && x<=220 && y>=55 && y<=180 && (x<143 || x>217 || y<58 || y>177);
                bool notch = x>207 && x<220 && (Mathf.Abs(y-73)<3 || Mathf.Abs(y-92)<3 || Mathf.Abs(y-150)<3);
                float wave = 180 + 12*Mathf.Sin(((y-55)/125f-.5f)*24);
                bool ink = y>75 && y<160 && Mathf.Abs(x-wave)<2;
                pixels[y*360+x] = edge || notch || ink ? new Color(.19f,.13f,.07f) : new Color(.81f,.73f,.56f);
            }
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }
    }
}
