namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public bool DocumentReturnedHeard { get; private set; }
        public bool MissingDiscoveryHeard { get; private set; }
        public bool GuardDepartureHeard { get; private set; }
        public bool GuardReturnHeard { get; private set; }
        public bool GamblingAdmissionHeard { get; private set; }
        public bool GamblingWitnessHeard { get; private set; }
        public bool ArchivePresenceHeard { get; private set; }
        public bool DuseokTimelineComplete { get; private set; }
        public bool CanCompleteDuseokTimeline => DuseokTestimonyHeard && DocumentReturnedHeard && MissingDiscoveryHeard
            && GuardDepartureHeard && GuardReturnHeard && GamblingAdmissionHeard && GamblingWitnessHeard && ArchivePresenceHeard;
        public const string DuseokTimeline = "배급 전: 전령이 화공대비책을 보관함에 반납했다고 진술.\n배급 북 이후: 두석이 경비 자리에서 이탈.\n교대 직전: 두석 복귀.\n교대 후: 전령이 문서의 부재를 발견.\n\n두석과 동석 병사의 설명은 노름으로 인한 공백을 뒷받침한다. 설평이 이탈 전후 안에 있었다는 것은 두석의 진술이다. 공백 중 출입자와 실제 절도 시각은 확인되지 않았다.";

        void RecordDuseokStatement(string speaker, string statement, System.Action record, System.Action back)
        {
            BeginSequence(speaker, new[] { statement }, () => {
                record();
                SetModal("진술 기록 · 확인 범위 구분", speaker, statement, "진술을 기록했습니다. 재열람에는 시간이 들지 않습니다.");
                view.AddChoice("다른 질문", () => back());
                view.AddChoice("대화 마치기", CloseModal);
            });
        }

        public void TalkToDocumentCourier()
        {
            SetSavePoint("courier");
            if (!EnsureInquiry("courier", "전령 · 문서 확인 시점", TalkToDocumentCourier)) return;
            SetModal("수군 진영 · 문서 전달 전령", "전령", "“사건 당일 대비책을 전달하고 돌려놓은 일을 말씀드리겠습니다.”");
            view.AddChoice("마지막으로 문서를 본 때는?", () => RecordDuseokStatement("전령",
                "저녁 군량 배급 전에 화공대비책을 사용처에서 받아 지정된 보관함에 돌려놓았습니다. 표제를 확인했습니다. 그 뒤로 보관함을 계속 지켜본 것은 아닙니다.",
                () => DocumentReturnedHeard = true, TalkToDocumentCourier));
            view.AddChoice("문서가 없어진 것을 언제 알았나?", () => RecordDuseokStatement("전령",
                "교대가 끝난 뒤 문서를 다시 가져오라는 말을 듣고 돌아갔습니다. 그때 보관함이 비어 있어 경비에게 알렸습니다. 누가 가져가는 모습은 보지 못했습니다.",
                () => MissingDiscoveryHeard = true, TalkToDocumentCourier));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void TalkToReliefGuard()
        {
            SetSavePoint("relief");
            if (!EnsureInquiry("relief_guard", "교대 병사 · 이탈과 복귀", TalkToReliefGuard)) return;
            SetModal("문서고 · 교대 병사", "교대 병사", "“그날 두석과 근무를 교대했습니다. 제가 직접 본 범위에서 답하겠습니다.”");
            view.AddChoice("두석이 떠나는 것을 봤나?", () => RecordDuseokStatement("교대 병사",
                "배급 북이 울린 뒤 식기를 돌려주러 지나가다가 두석이 경비 자리를 떠나는 것을 봤습니다. 저는 교대 준비를 하러 갔으므로 이후 입구를 계속 감시하지는 못했습니다.",
                () => GuardDepartureHeard = true, TalkToReliefGuard));
            view.AddChoice("두석은 언제 돌아왔나?", () => RecordDuseokStatement("교대 병사",
                "교대를 위해 입구에 왔을 때 두석은 없었습니다. 교대 직전에야 서둘러 돌아왔습니다. 그가 없는 동안 드나든 사람까지 알지는 못합니다.",
                () => GuardReturnHeard = true, TalkToReliefGuard));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void TalkToGamblingSoldier()
        {
            SetSavePoint("gambling");
            if (GamblingAdmissionHeard && !EnsureInquiry("gambling_witness", "동석 병사 · 노름 시간 확인", TalkToGamblingSoldier)) return;
            SetModal("수군 진영 · 휴식 중인 병사", "동석 병사", "“어떤 일을 확인하러 오셨습니까?”");
            if (GamblingAdmissionHeard)
                view.AddChoice("두석과 노름한 때를 확인한다", () => RecordDuseokStatement("동석 병사",
                    "배급을 마친 뒤 진영 입구 옆 빈터에서 두석과 노름했습니다. 그는 교대에 늦겠다며 급히 떠났습니다. 함께 앉아 있던 동안은 보았지만, 그가 오기 전과 떠난 뒤의 행적은 모릅니다.",
                    () => GamblingWitnessHeard = true, TalkToGamblingSoldier));
            else view.AddChoice("두석에 대해 묻는다", () => FollowUpNotice("확인할 내용", "구체적으로 어느 때의 일을 묻는지 확인하자. 두석과 문서고 교대 병사에게 먼저 경비 상황을 물을 수 있다."));
            view.AddChoice("대화 마치기", CloseModal);
        }

        void ShowDuseokTimelineInquiry()
        {
            SetSavePoint("duseok_timeline");
            SetModal("두석 · 경비 시간대 조사", "경비 공백을 확인한다", DuseokInvestigationGuide()
                + "\n\n교대 병사는 문서고 입구 맞은편, 전령과 병사는 수군 진영 입구 부근에 있다. 새 조사와 시간대 재질문은 각각 1회다. 확보한 증언은 수첩에서 정리할 수 있다.");
            if (DuseokTestimonyHeard && GuardDepartureHeard && GuardReturnHeard)
                view.AddChoice("배급 북 이후부터 교대 직전까지 어디 있었나?", StartDuseokRequestion);
            if (DuseokTestimonyHeard)
                view.AddChoice("이탈 전후 문서고 안에는 누가 있었나?", () => RecordDuseokStatement("두석",
                    "설평은 안에서 서류를 정리하고 있었습니다. 제가 돌아왔을 때도 있었고요. 그사이에 계속 있었는지, 다른 사람이 드나들었는지는 모릅니다.",
                    () => ArchivePresenceHeard = true, ShowDuseokTimelineInquiry));
            view.AddChoice("확인한 증언 목록", () => {
                SetModal("시간대 조사", "확인 상태", DuseokWitnessChecklist() + "\n\n" + DuseokInvestigationGuide());
                view.AddChoice("두석에게 돌아가기", ShowDuseokTimelineInquiry);
            });
            view.AddChoice(DuseokTimelineComplete ? "정리한 시간대 다시 보기" : "조사 결과 정리", CompleteDuseokTimeline, CanCompleteDuseokTimeline);
            view.AddChoice("대화 마치기", CloseModal);
        }

        void StartDuseokRequestion()
        {
            if (!EnsureInquiry("duseok_requestion", "두석 · 시간대 재질문", StartDuseokRequestion)) return;
            RecordDuseokStatement("두석",
                    "…수군 진영 입구 옆 빈터에서 병사들과 노름하다 시간을 놓쳤습니다. 그곳에 있던 병사에게 물어보셔도 됩니다. 근무를 비운 벌이 두려워 잠깐이라고 했습니다.",
                    () => GamblingAdmissionHeard = true, ShowDuseokTimelineInquiry);
        }

        void CompleteDuseokTimeline()
        {
            if (!CanCompleteDuseokTimeline || FinalReportCompleted) return;
            DuseokTimelineComplete = true;
            SetModal("선택 조사 완료", "경비 공백과 절도는 별개의 문제", DuseokTimeline);
            view.AddChoice("수첩 보기", ShowJournal);
            view.AddChoice("조사 계속", CloseModal);
        }

        string DuseokWitnessChecklist() => RecordStatus(DocumentReturnedHeard, "전령 · 마지막 문서 확인")
            + RecordStatus(MissingDiscoveryHeard, "전령 · 부재 발견")
            + RecordStatus(GuardDepartureHeard && GuardReturnHeard, "교대 병사 · 이탈과 복귀")
            + RecordStatus(DuseokTestimonyHeard && GamblingAdmissionHeard, "두석 · 이탈 이유 재질문")
            + RecordStatus(GamblingWitnessHeard, "동석 병사 · 노름 시간 확인")
            + RecordStatus(ArchivePresenceHeard, "두석 · 이탈 전후 문서고 상황");

        string DuseokInvestigationGuide()
        {
            string known = GuardDepartureHeard && GuardReturnHeard
                ? "교대 병사의 목격 진술로 두석의 이탈·복귀 시점과 경비 공백을 확인했다."
                : GuardDepartureHeard ? "교대 병사는 배급 북 이후 두석이 떠나는 것을 봤다고 말했다. 복귀 시점은 아직 확인하지 않았다."
                : GuardReturnHeard ? "교대 병사는 두석이 교대 직전에 돌아왔다고 말했다. 이탈 시점은 아직 확인하지 않았다."
                : DuseokTestimonyHeard ? "두석은 자리를 비웠다고 말했다. 이탈·복귀 시점은 주변 진술과 대조해야 한다."
                : "두석의 경비 상황과 주변 목격 진술을 확인할 수 있다.";
            string next = GamblingWitnessHeard ? "동석 병사의 설명도 확보했다. 각 진술에서 목격한 범위를 구분해 보자."
                : GamblingAdmissionHeard ? "두석이 말한 노름 시간과 동석 여부를 병사에게 확인할 수 있다."
                : DuseokTestimonyHeard && GuardDepartureHeard && GuardReturnHeard
                    ? "두석에게 이탈 이유와 그동안의 행적을 다시 물을 수 있다."
                    : "두석과 교대 병사가 각각 어느 시간대를 직접 보았는지 확인할 수 있다.";
            string other = ContradictionFound
                ? "\n\n다른 조사 기록: 현장 경험 진술과 기록의 모순을 발견했다. 수첩에서 경력·신원 조사 진행도 함께 검토할 수 있다."
                : CanCompareTestimonies
                    ? "\n\n다른 조사 기록: 훈련에 관한 진술과 자료가 모였다. 수첩에서 직접 비교할 수 있다."
                    : TestimonyHeard && (CaimaoTestimonyHeard || ZhangyunTestimonyHeard || MaintenanceOrderRead)
                        ? "\n\n다른 조사 기록: 설평의 경험 설명과 수군 관련 자료를 확보했다. 수첩에서 아직 확인하지 않은 비교 자료를 살펴볼 수 있다." : "";
            return "지금까지 확인한 범위\n" + known
                + "\n\n아직 모르는 점\n경비 공백만으로 그동안 누가 문서를 가져갔는지는 판단할 수 없다."
                + "\n\n더 확인할 수 있는 내용\n" + next + other
                + "\n\n계속할 조사와 순서는 직접 선택한다. 수첩 재검토와 이동에는 조사 횟수가 들지 않는다.";
        }
    }
}
