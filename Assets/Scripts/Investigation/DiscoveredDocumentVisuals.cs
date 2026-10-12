using UnityEngine;

namespace RedCliffMystery.Investigation
{
    // Presentation only: never grants evidence or advances an investigation.
    public sealed class DiscoveredDocumentVisuals : MonoBehaviour
    {
        public InvestigationSession session;
        public GameObject householdRegistry, secretLetter;

        void Awake() => RefreshVisibility();
        void OnEnable() => RefreshVisibility();
        void LateUpdate() => RefreshVisibility();

        public void RefreshVisibility()
        {
            bool legacy=session!=null && !session.OrderChapterActive;
            SetVisible(householdRegistry,legacy && session.HasHouseholdRegistryEvidence);
            SetVisible(secretLetter,legacy && session.SearchAuthorized && session.LetterFound);
        }
        static void SetVisible(GameObject model,bool visible)
        {
            if(model!=null && model.activeSelf!=visible)model.SetActive(visible);
        }
    }
}
