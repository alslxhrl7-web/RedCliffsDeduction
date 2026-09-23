using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTarget : MonoBehaviour
    {
        public enum TargetKind { MissingDocument, Seolpyeong }
        public TargetKind kind;
        public string Prompt => kind == TargetKind.MissingDocument
            ? "E  ·  빈 문서 보관함 조사" : "E  ·  설평과 대화";

        public void Interact(InvestigationSession session)
        {
            if (kind == TargetKind.MissingDocument) session.InspectDocument();
            else session.TalkToSeolpyeong();
        }
    }
}
