using UnityEngine;

namespace RedCliffMystery.Investigation
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class CampDirectionSign : MonoBehaviour
    {
        static Font sharedFont;
        void OnEnable()
        {
            if(sharedFont==null) sharedFont=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","맑은 고딕","Arial"},48);
            var text=GetComponent<TextMesh>();text.font=sharedFont;
            GetComponent<Renderer>().sharedMaterial=sharedFont.material;
        }
    }
}
