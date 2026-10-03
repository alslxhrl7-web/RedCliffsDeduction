namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public bool PersonnelRecordRead { get; private set; }
        public bool DutyRecordRead { get; private set; }
        // The reported origin is a fictional case detail, not verified identity.
        public const string PersonnelRecord = "형주 인력 인계 명부\n\n성명: 설평\n신고 출신지: 형주 남군 강릉\n이전 소속: 동쪽 수채 서리\n인계 구분: 형주 서리 인력 편입\n배치: 조조군 문서고\n\n출신지는 편입 당시 신고한 내용이다.";
        public const string DutyRecord = "동쪽 수채 서리 근무 기록\n\n성명: 설평\n근무: 출입 시설 정비 닷새를 포함하여 서리 업무 수행\n담당: 군량 지급 장부 정리 및 각 담당자가 제출한 훈련 보고서 취합\n인계: 관련 장부와 보고서 정리 후 인력 인계 명부에 등재";
        public const string CareerFinding = "서리로 근무한 사실과 서류 업무는 확인됐다. 훈련을 직접 목격했는지와 신고 신원이 맞는지는 별도로 확인해야 한다.";

        public void TalkToRecordsClerk()
        {
            SetSavePoint("records");
            SetModal("수군 진영 · 기록 담당자", "기록 담당자", "“형주에서 인계된 인력과 근무 기록을 보관하고 있습니다. 앞의 탁자에서 명부와 근무 기록을 함께 살펴보십시오.”");
            view.AddChoice("경력 서류를 살펴본다", ShowCareerRecords);
            view.AddChoice("신고 출신지의 호적 대조", ShowRegistryComparison);
            view.AddChoice("이 기록으로 무엇을 확인할 수 있나?", () => BeginSequence("기록 담당자", new[] {
                "근무 기록은 그 이름으로 맡아 온 업무를 확인하는 자료입니다. 인계 명부의 출신지는 본인이 신고한 내용이지요.",
                "근무 사실과 신원은 구분해서 살펴야 합니다. 출신지와 신원을 확인하려면 해당 지역의 호적을 별도로 대조해야 합니다." }, TalkToRecordsClerk));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void ShowCareerRecords()
        {
            SetSavePoint("career");
            if (!EnsureInquiry("career", "기록 담당자 · 경력 확인", ShowCareerRecords)) return;
            SetModal("기록 탁자", "형주 인력의 경력 서류", "인계 명부와 이전 근무 기록이 나란히 놓여 있다. 확인할 서류를 선택하자.");
            view.AddChoice("형주 인력 인계 명부" + (PersonnelRecordRead ? " · 확인함" : ""), () => ReadCareerRecord(true));
            view.AddChoice("이전 근무 기록" + (DutyRecordRead ? " · 확인함" : ""), () => ReadCareerRecord(false));
            if (PersonnelRecordRead && DutyRecordRead)
                view.AddChoice("확인한 내용 정리", ShowCareerFinding);
            view.AddChoice("조사 계속", CloseModal);
        }

        void ReadCareerRecord(bool personnel)
        {
            if (personnel) PersonnelRecordRead = true; else DutyRecordRead = true;
            SetModal("서류 조사 · 수첩 기록", personnel ? "형주 인력 인계 명부" : "이전 근무 기록", personnel ? PersonnelRecord : DutyRecord);
            view.AddChoice("다른 서류 확인", ShowCareerRecords);
            if (PersonnelRecordRead && DutyRecordRead) view.AddChoice("확인한 내용 정리", ShowCareerFinding);
            view.AddChoice("조사 계속", CloseModal);
        }

        void ShowCareerFinding()
        {
            SetModal("서류 조사 결과", "근무 경력과 신고 신원", CareerFinding + "\n\n명부에 신고된 출신지는 형주 남군 강릉이다. 이 기록만으로 가명이나 범행 여부를 판단할 수 없다.");
            view.AddChoice("수첩 확인", ShowJournal);
            view.AddChoice("기록 담당자와 대화", TalkToRecordsClerk);
            view.AddChoice("조사 계속", CloseModal);
        }
    }
}
