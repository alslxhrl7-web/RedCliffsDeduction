using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationTarget : MonoBehaviour
    {
        public enum TargetKind { MissingDocument = 0, Seolpyeong = 1, Duseok = 2, Caimao = 3, Zhangyun = 4, MaintenanceOrder = 5,
            NavalCampExit = 6, ArchiveReturn = 7, RecordsClerk = 8, CareerRecords = 9 }
        public TargetKind kind;
        public string Prompt => kind switch
        {
            TargetKind.MissingDocument => "E  ·  빈 문서 보관함 조사",
            TargetKind.Seolpyeong => "E  ·  설평과 대화",
            TargetKind.Duseok => "E  ·  두석과 대화",
            TargetKind.Caimao => "E  ·  채모와 대화",
            TargetKind.Zhangyun => "E  ·  장윤과 대화",
            TargetKind.MaintenanceOrder => "E  ·  정비 명령서 확인",
            TargetKind.NavalCampExit => "E  ·  수군 진영으로 가는 문",
            TargetKind.ArchiveReturn => "E  ·  문서고로 돌아가는 길",
            TargetKind.RecordsClerk => "E  ·  기록 담당자와 대화",
            TargetKind.CareerRecords => "E  ·  경력 서류 조사",
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
                case TargetKind.MaintenanceOrder: session.InspectMaintenanceOrder(); break;
                case TargetKind.RecordsClerk: session.TalkToRecordsClerk(); break;
                case TargetKind.CareerRecords: session.ShowCareerRecords(); break;
                case TargetKind.NavalCampExit:
                case TargetKind.ArchiveReturn: session.ShowTravel(); break;
            }
        }
    }
}
