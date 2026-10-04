namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public bool TokenObserved { get; private set; }
        public bool SearchAuthorized { get; private set; }
        public bool LetterFound { get; private set; }
        public bool TokenMatched { get; private set; }
        public bool LetterDenialHeard { get; private set; }
        public bool TokenOwnershipConfirmed { get; private set; }
        public bool InterrogationCompleted { get; private set; }
        bool registryScopeChecked;
        bool registryNamesChecked;
        public const string TokenDescription = "설평의 개인 서류 묶음을 고정하는 오래된 목패. 물결 무늬와 서로 간격이 다른 세 홈이 있다. 이것만으로 용도나 신원을 판단할 수 없다.";
        public const string RegistryFinding = "신고 출신지인 형주 남군 강릉의 이관 호적을 대조했다. 해당 범위의 누락과 이름 표기 차이를 확인했으나 설평의 신고 신원을 뒷받침하는 기록이 없다. 실제 서리 근무 이력과 별개의 문제다.";
        public const string SecretLetter = "목연에게.\n화공대비책을 확보하라. 접선은 " + InvestigationTime.ContactDate + ", 약정한 곳에서 한 차례뿐이다. 그 전에 따로 연락하거나 문서를 보내지 마라. 인계할 때 지급한 연락패를 지참하라. 수령인은 패의 세 홈과 아래 본을 대조할 것이다. 문서는 인계 전까지 몸에서 떼지 마라.\n\n[확인용 본: 물결 무늬, 비대칭으로 배치된 세 홈]\n이 확인용 본을 인계 때까지 보관하라.\n— 오나라 연락책";

        public const string ContactScheduleFinding = "조조의 명령: 이달 열이튿날부터 열나흗날 밤까지 사흘.\n밀서의 지시: 이달 열닷새 새벽, 약정한 곳에서 접선.\n\n접선 예정은 조사 나흘째 새벽, 보고 기한 직후다. 밀서는 인계 지시를 보여줄 뿐, 실제 절도나 전달 완료를 입증하지 않는다.";

        void FollowUpNotice(string title, string body)
        {
            SetModal("후속 조사", title, body);
            view.AddChoice("조사 계속", CloseModal);
        }

        public void InspectPersonalToken()
        {
            SetSavePoint("token");
            if (FinalReportCompleted) return;
            if (!TokenObserved && !EnsureInquiry("token", "서류 묶음의 목패 조사", InspectPersonalToken)) return;
            TokenObserved = true;
            SetModal("개인 물건 · 수첩 기록", "서류 묶음의 목패", TokenDescription);
            if (LetterFound) view.AddChoice("밀서의 확인용 본과 비교", ShowTokenComparison);
            view.AddChoice("조사 계속", CloseModal);
        }

        void ShowSeolpyeongFollowUp()
        {
            SetSavePoint("seol_followup");
            SetModal("설평 · 추가 질문", "무엇을 확인할까?", "기록과 물건에 관해 질문할 수 있다.");
            if (TokenObserved)
                view.AddChoice("서류 묶음의 목패는 누구의 것인가?", () => BeginSequence("설평", new[] {
                "제 것입니다. 형주에서 일할 때부터 개인 서류를 묶는 데 썼습니다. 다른 서리의 물건과 섞이지 않도록 따로 두고 있지요." }, () => {
                    TokenOwnershipConfirmed = true;
                    FollowUpNotice("목패의 소유 이력", "설평은 형주 근무 때부터 자신의 목패를 사용했다고 진술했다. 수첩에 기록했다.");
                }));
            if (TestimonyHeard && MaintenanceOrderRead)
                view.AddChoice("정비 닷새의 현장 경험 재확인", ReconfirmTrainingTestimony);
            if (DutyRecordRead)
                view.AddChoice("기록에는 보고서 취합 업무라고 적혀 있다", () => BeginSequence("설평", new[] {
                    "장부와 보고서를 정리한 것은 맞습니다. 그렇다고 현장을 몰랐던 것은 아닙니다. 직접 보고 기록했다는 말씀은 그대로입니다." }, ShowSeolpyeongFollowUp));
            if (LetterFound) view.AddChoice("목연 앞으로 온 밀서를 제시한다", ShowLetterInterrogation);
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void ShowRegistryComparison()
        {
            SetSavePoint("registry");
            if (!ContradictionFound || !PersonnelRecordRead || !DutyRecordRead)
            {
                FollowUpNotice("신원 대조에 필요한 기록", "인계 명부와 근무 기록을 확인하고, 현장 경험에 관한 진술과 당시 기록을 먼저 대조하자. 근무 경력 자체와 신고 신원은 구분해야 한다.");
                return;
            }
            if (!EnsureInquiry("registry", "기록 담당자 · 호적 대조", ShowRegistryComparison)) return;
            SetModal("기록 담당자 · 이관 호적", "형주 남군 강릉의 신고 신원 대조", "형주 인력 편입 때 함께 이관된 호적 등본이다. 이름이 보이지 않는다는 이유만으로 결론 내리지 말고 자료 범위와 표기를 확인하자.");
            view.AddChoice((registryScopeChecked ? "확인함 · " : "") + "지역·시기·누락 여부 확인", () => {
                registryScopeChecked = true;
                SetModal("자료 확인", "대조 범위", "담당자는 신고 지역과 편입 당시 대조할 호적 범위가 맞으며 해당 장에 빠진 부분이 없음을 확인한다.");
                view.AddChoice("대조 계속", ShowRegistryComparison);
            });
            view.AddChoice((registryNamesChecked ? "확인함 · " : "") + "이름 표기와 가구 항목 확인", () => {
                registryNamesChecked = true;
                SetModal("자료 확인", "이름과 가구 항목", "이름의 다른 표기와 가구 항목까지 대조했지만 신고한 설평의 신원과 맞는 기록을 찾지 못했다.");
                view.AddChoice("대조 계속", ShowRegistryComparison);
            });
            view.AddChoice("대조 결과 기록", () => {
                if (!registryScopeChecked || !registryNamesChecked) return;
                RecordVerifiedKeyEvidence("clue_household_registry_no_record");
                SetModal("신원 대조 완료", "신고 신원을 확인할 수 없음", RegistryFinding);
                view.AddChoice("수첩 확인", ShowJournal);
                view.AddChoice("조사 계속", CloseModal);
            }, registryScopeChecked && registryNamesChecked);
            view.AddChoice("조사 계속", CloseModal);
        }

        void RequestPersonalSearch()
        {
            if (!HasHouseholdRegistryEvidence || !ContradictionFound) return;
            if (investigationTime.DeadlineReached) { ShowDeadlineChoice(); return; }
            BeginFinalReport();
            if (!reportActive) return;
            ReportModal("중간 보고 · 조사 허가", "조조", "“근무 이력은 있지만 신고 신원은 확인되지 않는다… 현장 경험에 관한 말도 기록과 맞지 않는다는 것이군.”\n\n“설평의 개인 서류를 조사하도록 허락하겠다. 몸수색이나 체포는 이번 명령에 포함하지 않는다.”");
            SetSavePoint("search_permission");
            view.AddChoice("조사 명령을 받고 돌아간다", () => {
                SearchAuthorized = true;
                CancelFinalReport();
            });
        }

        public void InspectPersonalPapers()
        {
            SetSavePoint("papers");
            if (FinalReportCompleted) return;
            if (!SearchAuthorized)
            {
                SetModal("설평의 개인 서류함", "개인 서류 조사에는 허가가 필요하다", "공용 장부와 분리된 설평의 개인 서류함이다. 확보한 조사 근거를 조조에게 보고하고 허가를 받아야 한다.");
                if (HasHouseholdRegistryEvidence) view.AddChoice("조조에게 조사 허가 요청", RequestPersonalSearch);
                view.AddChoice("조사 계속", CloseModal);
                return;
            }
            if (!EnsureInquiry("papers", "허가받은 개인 서류 수색", InspectPersonalPapers)) return;
            SetModal("허가받은 개인 서류 조사", "업무 서신 안쪽의 접지", "설평의 개인 서류함에서 일반 업무 서신과 달리 안쪽에 접어 숨긴 종이를 발견했다. 공용 서신 묶음과 별도로 보관돼 있다.");
            view.AddChoice("안쪽 종이를 펼친다", () => {
                LetterFound = true;
                SetModal("발견한 밀서 · 수신인 미확인", "목연에게 내린 지시", SecretLetter);
                view.AddChoice("목패와 확인용 본 비교", ShowTokenComparison);
                view.AddChoice("접선 날짜를 조사 일정과 대조", () => FollowUpNotice("기한 다음 날의 접선", ContactScheduleFinding));
                view.AddChoice("조사 계속", CloseModal);
            });
            view.AddChoice("조사 계속", CloseModal);
        }

        void ShowTokenComparison()
        {
            SetSavePoint("token_compare");
            if (!LetterFound) return;
            if (!TokenObserved) { FollowUpNotice("대조할 물건이 필요하다", "밀서에는 수신인이 이미 지닌 연락패의 본이 있다. 문서고의 개인 물건을 살펴보자."); return; }
            SetModal("물건 대조", "밀서와 무엇을 비교할까?", "그림은 물결 무늬와 비대칭인 세 홈을 나타낸다. 어떤 기록 또는 물건이 수신인을 연결하는가?");
            view.AddChoice("설평의 개인 목패", () => {
                TokenMatched = true;
                FollowUpNotice("무늬와 세 홈의 일치", "목패의 물결 무늬와 세 홈의 위치가 밀서의 본과 일치한다. 설평에게 소유 경위와 개인 서류에 밀서를 보관한 이유를 물어보자.");
            });
            view.AddChoice("군량 지급 기록", () => FollowUpNotice("직접 연결되지 않는 자료", "군량 기록은 이 연락패의 소유자를 확인하는 자료가 아니다. 다시 대조할 수 있다."));
            view.AddChoice("정비 명령서", () => FollowUpNotice("직접 연결되지 않는 자료", "정비 명령은 출항 훈련에 관한 자료다. 개인 목패와 지시의 수신인을 비교해야 한다."));
            view.AddChoice("조사 계속", CloseModal);
        }

        void ShowLetterInterrogation()
        {
            SetSavePoint("letter");
            if (!LetterFound || !SearchAuthorized) return;
            if (!EnsureInquiry("letter_interrogation", "설평 · 밀서 취조", ShowLetterInterrogation)) return;
            SetModal("설평 · 밀서에 관한 질문", "목연은 누구인가?", "개인 서류에서 발견한 지시와 설평의 설명을 대조하자.");
            view.AddChoice("목연과 이 서신에 관해 묻는다", () => BeginSequence("설평", new[] {
                "목연이라는 자는 모릅니다. 제가 취합하던 서신에 섞여 있었을 것입니다.",
                "보고하지 않은 것은… 제 물건에서 나왔다는 이유만으로 의심받을까 두려웠기 때문입니다." }, () => {
                    LetterDenialHeard = true;
                    ShowLetterInterrogation();
                }));
            view.AddChoice("목패와 밀서의 연결을 제시한다", () => {
                if (!TokenMatched || !TokenOwnershipConfirmed || !LetterDenialHeard) return;
                BeginSequence(PlayerName, new[] {
                    "밀서는 공용 서신이 아니라 네 개인 서류 안쪽에 숨겨져 있었다. 보고하지 않은 이유만으로 죄를 정하려는 것이 아니다.",
                    "너는 이 목패를 형주에서부터 사용했다고 말했다. 목연에게 지급했다는 패의 본과 무늬, 세 홈이 모두 맞는다.",
                    "신고한 신원은 확인되지 않았고, 목연에게 내린 지시는 네 개인 물건과 연결된다. 이 근거를 조조께 보고하겠다." }, () => BeginSequence("설평", new[] {
                        "…더는 답하지 않겠습니다." }, () => {
                            InterrogationCompleted = true;
                            RecordVerifiedKeyEvidence("clue_wu_secret_letter_true_name");
                            FollowUpNotice("밀서의 수신인 연결 확인", "신고 신원 대조, 개인 서류의 출처, 목패의 소유 이력과 확인용 본을 함께 기록했다. 신병 확보와 수색을 요청할 근거가 마련됐다. 절도의 실행과 문서 행방은 아직 확인되지 않았다.");
                        }), true);
            }, TokenMatched && TokenOwnershipConfirmed && LetterDenialHeard);
            view.AddChoice("물건·진술 확인 상태", () => FollowUpNotice("취조 준비", RecordStatus(TokenMatched, "목패와 본 대조") + RecordStatus(TokenOwnershipConfirmed, "목패의 소유 이력 질문") + RecordStatus(LetterDenialHeard, "밀서에 관한 설평의 해명")));
            view.AddChoice("대화 마치기", CloseModal);
        }

        string FollowUpProgress()
        {
            return RecordStatus(TokenObserved, "개인 목패 확인") + RecordStatus(HasHouseholdRegistryEvidence, "신고 신원 대조")
                + RecordStatus(SearchAuthorized, "개인 서류 조사 허가") + RecordStatus(LetterFound, "개인 서류 조사")
                + RecordStatus(TokenMatched, "물건과 기록 대조") + RecordStatus(InterrogationCompleted, "관련 진술 검토 완료");
        }
    }
}
