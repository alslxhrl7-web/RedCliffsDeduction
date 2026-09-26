using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTarget : MonoBehaviour
    {
        public enum TargetKind { MissingDocument = 0, Seolpyeong = 1, Duseok = 2, Caimao = 3, Zhangyun = 4 }
        public TargetKind kind;
        public string Prompt => kind switch
        {
            TargetKind.MissingDocument => "E  ·  빈 문서 보관함 조사",
            TargetKind.Seolpyeong => "E  ·  설평과 대화",
            TargetKind.Duseok => "E  ·  두석과 대화",
            TargetKind.Caimao => "E  ·  채모와 대화",
            TargetKind.Zhangyun => "E  ·  장윤과 대화",
            _ => ""
        };

        public void Interact(InvestigationSession session)
        {
            switch (kind)
            {
                case TargetKind.MissingDocument: session.InspectDocument(); break;
                case TargetKind.Seolpyeong: session.TalkToSeolpyeong(); break;
                case TargetKind.Duseok: session.TalkToDuseok(); break;
                case TargetKind.Caimao: session.TalkToCaimao(); break;
                case TargetKind.Zhangyun: session.TalkToZhangyun(); break;
            }
        }
    }
}
