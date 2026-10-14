using System;
using System.Collections.Generic;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public OrderInvestigationState OrderState { get; private set; } = new OrderInvestigationState();
        public const string OrderBody = "군령 — 수군 제17호\n수령: 강 입구 감시선 부대\n\n감시선을 본진으로 철수시키고, 본진에서 배치 완료를 보고할 것.";
        public const string OrderReceipt = "인계 확인표 — 수군 제17호\n수령: 강 입구 감시선 부대\n\n강 입구에서 감시선 배치를 확인받을 것.\n전달자는 완료 서명을 받아 문서고로 회신한다.";
        public const string OrderCopy = "발급 담당 보관 부본 — 수군 제17호\n수령: 강 입구 감시선 부대\n\n감시선을 강 입구에 유지하고, 현지에서 배치 완료를 보고할 것.\n\n발급 대장 번호 및 보관자 인계 기록과 일치한다.";

        bool HasOrder(OrderFact fact) => OrderState.Has(fact);
        void OrderModal(string title, string body)
        {
            SetSavePoint("order_journal");
            SetModal("군령 분실 조사", title, body);
        }
        void OrderRecord(OrderFact fact, string title, string body, Action back)
        {
            bool newlyRecorded = !HasOrder(fact);
            if (!OrderState.TryRecord(fact)) return;
            if (newlyRecorded) RecordGuidanceNotice((fact & (OrderFact.Compared | OrderFact.Verified | OrderFact.Motive | OrderFact.Map | OrderFact.ClaimChallenged)) != 0 ? "판단 기록" : (fact & (OrderFact.CourierClaim | OrderFact.NoMessenger | OrderFact.Movement | OrderFact.Explanation | OrderFact.OptionalCareer | OrderFact.OptionalProtection)) != 0 ? "새 진술" : "새 기록", title);
            ObserveTutorialRecord();
            UpdateObjective();
            back();
            string speaker = view.title.text == "문서 전달자" ? "전령" : view.title.text;
            RedCliffMystery.VarcoIdleMotion.StopSpeech();
            if (body.Contains("“")) RedCliffMystery.VarcoIdleMotion.Speak(speaker, body);
            view.title.text = title; view.body.text = body;
            view.SetFooterHint("수첩에 자동 기록됨 · 다음 질문 또는 조사 계속");
        }
        bool OrderInquiry(string id, string title, Action resume) => EnsureInquiry("order_" + id, title, resume);

        void BeginOrderIntroduction()
        {
            // Temporary in-world briefing. A later cutscene should call the same completion flow.
            BeginSequence("조조", new[] {
                PlayerName + ", 그대를 군령 분실 사건을 조사할 임시 참모로 삼겠다.",
                "문서고에서는 내보냈다 하고, 받아야 할 부대에서는 받지 못했다 한다. 어디서 인계가 끊겼는지 밝혀라. 누구의 잘못인지는 그 뒤에 따져도 늦지 않다.",
                "열나흗날 밤까지 경위를 보고하라. 사소한 전달 착오로 여겨 덮어둘 일이 아니다. 관련자의 말과 남은 기록을 함께 확인하라."
            }, () => {
                SetModal("임무 수령", "군령의 인계 경로를 밝혀라",
                    "문서고는 군령을 내보냈지만, 수령 부대는 받지 못했다고 한다.\n문서와 관계자의 말을 확인해 인계 경로를 밝혀라.\n\n조사를 마치면 조조에게 최종 보고할 결론과 근거를 선택한다.");
                view.AddChoice("문서고로 향한다", FinishIntroduction);
                onAdvance = FinishIntroduction;
            });
        }

        public bool HandleOrderTarget(InvestigationTarget.TargetKind kind)
        {
            switch (kind)
            {
                case InvestigationTarget.TargetKind.MissingDocument: ShowOrderArchive(); break;
                case InvestigationTarget.TargetKind.Seolpyeong: ShowOrderSeol(); break;
                case InvestigationTarget.TargetKind.Duseok: ShowOrderDuseok(); break;
                case InvestigationTarget.TargetKind.Caimao: ShowOrderOfficer(true); break;
                case InvestigationTarget.TargetKind.Zhangyun: ShowOrderOfficer(false); break;
                case InvestigationTarget.TargetKind.DocumentCourier: ShowOrderCourier(); break;
                case InvestigationTarget.TargetKind.RecordsClerk: ShowOrderClerk(); break;
                case InvestigationTarget.TargetKind.CareerRecords: ShowOrderRecords(); break;
                case InvestigationTarget.TargetKind.OrderStorage: ShowOrderStorage(); break;
                case InvestigationTarget.TargetKind.HanJin: ShowOrderHan(); break;
                case InvestigationTarget.TargetKind.ReturnedOrder: ShowOrderRewrite(); break;
                case InvestigationTarget.TargetKind.OutgoingOrder: ShowOrderOutgoing(); break;
                case InvestigationTarget.TargetKind.MaintenanceOrder: ShowOrderMap(); break;
                case InvestigationTarget.TargetKind.PersonalToken:
                case InvestigationTarget.TargetKind.PersonalPapers:
                    OrderModal("개인 물품", "개인 물품만으로 군령의 이동이나 조작자를 판단할 수 없다. 이번에는 반출 기록과 수령 여부부터 확인하자.");
                    view.AddChoice("조사 계속", CloseModal); break;
                case InvestigationTarget.TargetKind.ReliefGuard:
                case InvestigationTarget.TargetKind.GamblingSoldier:
                    RecordArchive.RecordPerson(kind==InvestigationTarget.TargetKind.ReliefGuard?7:8);
                    OrderModal("근무와 인계", "이 병사의 개인 사정만으로 군령의 행방을 알 수 없다. 두석의 동선 설명과 보관소 반입 기록을 확인할 수 있다.");
                    view.AddChoice("조사 계속", CloseModal); break;
                default: return false;
            }
            return true;
        }

        public void ShowOrderArchive()
        {
            if (!OrderInquiry("archive", "문서고 · 보관 자리와 반출 기록", ShowOrderArchive)) return;
            OrderModal("비어 있는 보관 자리", "군령을 두던 자리는 비어 있다. 반출 자체가 정상 업무였을 가능성도 있다. 탁자에 해당 군령의 반출 대장이 남아 있다.\n\n떠나기 전 서쪽 한진 앞 탁자의 반환 묶음도 확인할 수 있다. 확보하면 수군 진영에서 비교할 때 다시 가지 않아도 된다.");
            view.AddChoice("반출 대장 확인", () => OrderRecord(OrderFact.Dispatch, "반출 대장", "수군 제17호 · 강 입구 감시선 부대 앞으로 발급.\n전달자 수령 표시와 ‘대조 완료’ 표시가 남아 있다.\n\n이 기록은 출고를 보여 주지만 목적지 도착을 증명하지는 않는다.", ShowOrderArchive));
            view.AddChoice("떠나기 전 반환 묶음 위치 확인", () => OrderDirections("제17호의 정서와 인계는 연락관 한진이 맡았다. 문서고의 발급 탁자 옆에서 만날 수 있다. 재작성을 마친 반환 묶음도 그 옆에 보관되어 있다."));
            view.AddChoice("조사 계속", CloseModal);
        }

        public void ShowOrderOfficer(bool caimao)
        {
            if (!OrderInquiry(caimao ? "receipt" : "procedure", caimao ? "채모 · 수령 여부" : "장윤 · 인계 절차", () => ShowOrderOfficer(caimao))) return;
            OrderModal(caimao ? "채모" : "장윤", caimao
                ? "“부대에서는 군령을 기다리고 있소. 누가 가져갔다는 말과 실제로 받았다는 말은 구분해야 하오.”"
                : "“수령 표시와 대조 완료는 다르오. 본문과 인계 확인표를 대조한 뒤 완료 표시를 남겨야 하오.”");
            if (caimao)
            {
                view.AddChoice("수령 부대의 보고 확인", () => OrderRecord(OrderFact.NoReceipt, "미수령 보고", "강 입구 감시선 부대는 수군 제17호를 받지 못했다고 보고했다. 반출 기록과 도착 여부가 어긋난다.", () => ShowOrderOfficer(true)));
                if (HasOrder(OrderFact.CourierClaim)) view.AddChoice("대신 받을 병사를 보냈는가?", () => OrderRecord(OrderFact.NoMessenger, "부대 확인", "“부대에 재확인했소. 군령을 대신 받으러 병사를 보낸 적은 없소.”\n\n전달자가 말한 병사의 신원과 실제 인계 장소를 다시 확인할 근거다.", () => ShowOrderOfficer(true)));
            }
            else
            {
                view.AddChoice("정상 인계 절차 확인", () => OrderRecord(OrderFact.Procedure | OrderFact.Dispatch, "인계 절차와 반출 확인", "본문과 확인표의 번호·수령 부대·완료 회신 지시를 대조한 뒤 정상 확인을 남긴다. 확인표는 부대의 서명을 받아 가져오는 자료다.\n\n장윤이 제시한 반출 대장에는 제17호의 전달자 수령과 대조 완료 표시가 있다. 보관 부본은 발급 담당이 별도로 관리한다.", () => ShowOrderOfficer(false)));
                view.AddChoice("설평의 과거 정정 사유 · 선택 조사", () => OrderRecord(OrderFact.OptionalProtection, "장윤의 설명 · 선택 기록", "“설평의 옛 소속을 정정한 일에는 내 책임도 있소. 그러나 그 일은 이번 군령이 나오기 전의 일이오.”\n\n과거 기록 정정과 이번 군령 변경은 별도로 검증해야 한다. 이 선택 기록은 문서 회수·내용 대조의 필수 조건이 아니다.", () => ShowOrderOfficer(false)));
            }
            if (HasOrder(OrderFact.NoReceipt | OrderFact.Procedure)) view.AddChoice("누구를 더 조사할지 정한다", ShowOrderSelection);
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void ShowOrderSelection() => ShowInvestigationDirections();
        void OrderDirections(string body) { OrderModal("조사 대상", body); view.AddChoice("직접 찾아간다", CloseModal); view.AddChoice("장소 이동", ShowTravel); }

        public void ShowOrderDuseok()
        {
            if (!OrderInquiry("movement", "두석 · 전달자의 동선", ShowOrderDuseok)) return;
            OrderModal("두석", "“잠시 안내인을 만나러 자리를 비웠습니다. 문서고 안에서 일어난 일까지 보았다고 할 수는 없습니다.”");
            view.AddChoice("돌아올 때 본 것을 묻는다", () => OrderRecord(OrderFact.Movement, "두석의 목격 · 진술", "“전달자가 돌아 나와 수송 물품 보관소 쪽으로 갔습니다. 손에는 꾸러미가 있었고요.”\n\n당직 인수 기록상 이 목격은 전달자가 인계했다고 주장한 교대 전보다 뒤다. 다만 두석은 꾸러미 안의 문서를 보지 못했다.", ShowOrderDuseok));
            view.AddChoice("대화 마치기", CloseModal);
        }
        public void ShowOrderSeol()
        {
            OrderModal("설평", "“반출 기록은 보관 탁자에 있습니다. 제가 서류를 다룬다는 이유만으로 누가 문서를 가져갔는지 알 수 있는 것은 아닙니다.”");
            view.AddChoice("과거 서류를 다시 본 이유 · 선택", () => OrderRecord(OrderFact.OptionalCareer, "설평의 선택 진술", "“예전 소속이 드러날까 두려워 편입 서류를 다시 보았습니다. 처음부터 사실대로 말하지 못했습니다.”\n\n열람 대상은 편입 서류이고 사건 전날의 일이라는 진술이다. 군령 교체의 증거는 아니며 과거의 자세한 사정은 필수 조사가 아니다.", ShowOrderSeol));
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void ShowOrderCourier()
        {
            if (!OrderInquiry("courier", "전달자 · 인계 경위", ShowOrderCourier)) return;
            OrderModal("문서 전달자", HasOrder(OrderFact.Recovered)
                ? "회수한 군령을 보여 주자 전달자가 시선을 내린다. 은닉과 내용 변경은 따로 확인해야 한다."
                : "“군령을 받은 것은 맞습니다. 무엇을 확인하러 오셨습니까?”");
            if (!HasOrder(OrderFact.Recovered))
            {
                view.AddChoice("언제 누구에게 넘겼는가?", () => OrderRecord(OrderFact.CourierClaim, "전달자의 주장 · 미검증", "“교대 전에 수령 부대에서 보냈다는 병사에게 넘겼습니다.”\n\n상대의 신원과 실제 수령 여부는 아직 확인하지 않았다.", ShowOrderCourier));
                if (OrderState.CanChallenge) view.AddChoice("인계 주장과 확인 자료를 대조한다", () => OrderRecord(OrderFact.ClaimChallenged, "인계 경위 재질문", "별도 병사 파견 여부 또는 인계 뒤의 이동을 제시하자 전달자는 말을 바꾼다.\n“보관소에 잠깐 두었습니다. 아직 그 안에 있습니다.”\n\n직접 찾아 반출 기록과 일치하는 군령인지 확인해야 한다.", ShowOrderCourier));
            }
            else
            {
                view.AddChoice("무엇이 달라 전달하지 않았는가?", () => OrderRecord(OrderFact.Explanation, "전달자의 설명 · 검증 필요", "“출발 전에 완료 서명을 받을 장소를 확인했습니다. 본문은 본진, 확인표는 강 입구였습니다. 제가 다른 표를 받은 줄 알고 먼저 맞춰 보려 했습니다.”\n\n전달자는 내용 조작을 알아냈다고 주장하지 않는다.", ShowOrderCourier));
                if (OrderState.CanExplainMotive) view.AddChoice("대조 완료라 적고 왜 보고하지 않았나?", () => OrderRecord(OrderFact.Motive, "거짓 확인과 은닉 · 진술", "“늘 하던 일이라 번호와 부대 이름만 봤습니다. 제대로 대조하지 않고 완료라고 적었습니다. 혼자 바로잡으면 될 줄 알았습니다.”\n\n“미수령 조사에 조조 승상까지 나서셨다고 들었습니다. 두려워 이미 넘겼다고 둘러대고 숨겼습니다.”\n\n고백은 정상 확인 기록과 구분해 남긴다. 은닉 이유가 조작 책임까지 증명하지는 않는다.", ShowOrderCourier));
            }
            view.AddChoice("대화 마치기", CloseModal);
        }

        public void ShowOrderStorage()
        {
            if (!OrderInquiry("storage", "수송 물품 · 반입 기록과 꾸러미", ShowOrderStorage)) return;
            OrderModal("수송 물품 보관소", "반입 기록과 꾸러미 표식을 확인할 수 있다. 물건을 발견했다는 사실만으로 누가 군령 내용을 바꿨는지는 알 수 없다.");
            view.AddChoice("반입 기록과 표식 확인", () => OrderRecord(OrderFact.StorageTrail, "보관소 반입 기록", "교대 후 전달자가 수군 제17호 꾸러미를 임시 보관한 기록이 있다. 군령 꾸러미 표식과 반출 번호를 대조할 수 있다.\n\n꾸러미 안쪽에 문서가 남아 있는지 살펴보자.", ShowOrderStorage));
            view.AddChoice(HasOrder(OrderFact.Recovered) ? "회수한 군령 다시 읽기" : "해당 꾸러미 조사", RecoverOrder, OrderState.CanRecover);
            if (!OrderState.CanRecover) view.SetFooterHint("반출 번호와 반입 기록, 전달자 주장에 대한 확인 또는 두석의 동선을 확보하면 꾸러미를 특정할 수 있습니다.");
            view.AddChoice("조사 계속", CloseModal);
        }
        void RecoverOrder()
        {
            if (!OrderState.TryRecord(OrderFact.Recovered)) return;
            UpdateObjective();
            OrderModal("회수한 군령", OrderBody + "\n\n군령은 수송 물품 사이에 숨겨져 있었다. 회수 사실만으로 조작자를 특정하지 않는다.");
            view.AddChoice("함께 남은 인계 확인표", () => OrderRecord(OrderFact.Receipt, "인계 확인표", OrderReceipt, RecoverOrder));
            view.AddChoice("조사 수첩에서 비교", ShowOrderComparison);
            view.AddChoice("조사 계속", CloseModal);
        }

        public void ShowOrderComparison()
        {
            OrderModal("자료 대조", !OrderState.CanCompare ? "회수한 군령과 인계 확인표를 모두 확인해야 비교할 수 있다. 확인표는 회수한 꾸러미에서 읽을 수 있다."
                : OrderBody + "\n\n" + OrderReceipt + "\n\n두 자료로 어디까지 말할 수 있는가?");
            if (OrderState.CanCompare)
            {
                view.AddChoice("다른 부대의 서류를 잘못 가져왔다", () => OrderFeedback("번호와 수령 부대는 같다. 다른 부대 자료의 단순 혼입이라고 단정할 수 없다."));
                view.AddChoice("같은 번호인데 완료 회신 장소가 다르다", () => OrderRecord(OrderFact.Compared, "불일치 확인", "수령 부대와 번호는 같지만 회신 장소가 다르다.\n\n정식으로 명령을 변경하고 확인표만 갱신하지 않은 경우도 가능하다. 발급 담당의 부본과 승인 경위를 더 확인하자.", ShowOrderComparison));
                view.AddChoice("전달자가 군령을 조작했다", () => OrderFeedback("은닉 사실은 내용 변경의 증거가 아니다. 변경 시점과 작성·승인 경위를 따로 확인해야 한다."));
            }
            view.AddChoice("수첩으로", ShowOrderJournal);
        }
        void OrderFeedback(string text) { OrderModal("가설 다시 살펴보기", text); view.AddChoice("다시 비교", ShowOrderComparison); }

        public void ShowOrderRecords()
        {
            if (!OrderInquiry("records", "발급 기록 탁자 · 부본과 승인 확인", ShowOrderRecords)) return;
            OrderState.TryRecord(OrderFact.RetainedCopy | OrderFact.Approval);
            UpdateObjective();
            OrderModal("발급 기록 탁자", "[보관 부본] 제17호 / 강 입구 감시선 유지, 현지에서 보고.\n발급 번호·보관자 인계 기록이 일치하며, 본문 출고 뒤에도 발급 측에 남았다.\n\n[변경 승인 대장] 제17호 승인 내용: 강 입구 유지. 본진 철수로 변경한 승인 항목은 없다.");
            view.AddChoice("정식 변경 가능성을 판단한다", ShowOrderVerification, OrderState.CanVerify);
            view.AddChoice("옆에 묶인 제17호 회신철 열람", ShowOrderReplies);
            view.AddChoice("부본 전문", () => ShowFollowSource("보관 부본", OrderCopy, ShowOrderRecords));
            view.AddChoice("조사 계속", CloseModal);
        }
        void ShowOrderVerification()
        {
            if (!OrderState.CanVerify) return;
            OrderModal("변경 경위 판단", "회수본은 본진 철수, 같은 번호의 부본과 확인표는 강 입구 유지다. 발급 담당의 승인 확인과 변경 대장에도 본진 철수 승인은 확인되지 않는다.");
            view.AddChoice("회수본은 확인한 승인 내용과 다르다", () => OrderRecord(OrderFact.Verified, "확인한 범위", "승인된 내용과 회수본의 내용이 다르다. 단순 문서 혼입만으로는 설명되지 않는다.\n\n누가 언제 바꿨는지, 원래 본문은 어디 있는지는 아직 모른다. 수군 진영의 배치도를 통해 변경의 영향을 살펴볼 수 있다.", ShowOrderRecords));
            view.AddChoice("접근 권한이 있는 사람을 범인으로 확정", () => { OrderModal("입증되지 않은 결론", "접근할 수 있었다는 사실과 실제 변경 행위는 다르다. 작성·교체·유통 근거가 더 필요하다."); view.AddChoice("다시 판단", ShowOrderVerification); });
            view.AddChoice("판단 보류", CloseModal);
        }

        public void ShowOrderMap()
        {
            OrderModal("수군 배치도", "강 입구 — 접근하는 선단을 살피는 감시선 대기 지점\n본진 — 강 입구에서 떨어진 집결지\n\n현재 준비 기록은 강 입구에 감시선을 배치하는 내용이다. 준비 기록만으로 새 명령이 위조라고 단정할 수는 없다.");
            if (HasOrder(OrderFact.Verified)) view.AddChoice("회수본대로 이동하면 생기는 차이", () => OrderRecord(OrderFact.Map, "변경의 영향 · 추론", "감시선이 본진으로 철수하면 강 입구의 감시 공백이 생길 수 있다.\n\n전달자는 서명을 받을 장소를 걱정했지만, 이 변경은 실제 감시 배치에도 영향을 준다. 조작자의 목적과 전투 결과 전체는 아직 입증하지 못했다.", ShowOrderMap));
            view.AddChoice("조사 계속", CloseModal);
        }

        string OrderObjective() => CaseSummary();
        public void ShowOrderJournal()
        {
            if (HasOrder(OrderFact.Verified)) { ShowOrderFollowUp(); return; }
            var lines = new List<string>();
            AddOrderLine(lines, OrderFact.Dispatch, "자료: 제17호 반출·정상 확인 표시.");
            AddOrderLine(lines, OrderFact.NoReceipt, "자료: 수령 부대는 미수령 보고.");
            AddOrderLine(lines, OrderFact.Procedure, "절차: 본문과 확인표 대조 후 정상 확인.");
            AddOrderLine(lines, OrderFact.CourierClaim, "주장: 전달자는 교대 전에 다른 병사에게 넘겼다고 함.");
            AddOrderLine(lines, OrderFact.NoMessenger, "대조: 부대는 인수 병사 파견 사실을 부인.");
            AddOrderLine(lines, OrderFact.Movement, "진술: 두석은 교대 후 보관소로 가는 전달자를 목격.");
            AddOrderLine(lines, OrderFact.StorageTrail, "자료: 보관소 반입 기록과 제17호 표식.");
            AddOrderLine(lines, OrderFact.Recovered, "현장: 군령 회수. 은닉과 내용 변경은 별개.");
            AddOrderLine(lines, OrderFact.Receipt, "자료: 인계 확인표 확보.");
            AddOrderLine(lines, OrderFact.Compared, "대조: 같은 번호·부대, 다른 완료 회신 장소.");
            AddOrderLine(lines, OrderFact.RetainedCopy, "자료: 발급 담당 보관 부본의 출처 확인.");
            AddOrderLine(lines, OrderFact.Approval, "확인: 본진 철수로 바꾸라는 승인 없음.");
            AddOrderLine(lines, OrderFact.Verified, "판단: 회수본은 확인한 승인 내용과 다름.");
            AddOrderLine(lines, OrderFact.Motive, "진술: 대조 생략을 숨기려 보류했다가 거짓말·은닉.");
            AddOrderLine(lines, OrderFact.Map, "추론: 본진 철수로 강 입구 감시 공백 발생 가능.");
            AddOrderLine(lines, OrderFact.OptionalCareer, "선택 진술: 설평은 과거 소속 노출을 두려워했다고 함.");
            AddOrderLine(lines, OrderFact.OptionalProtection, "선택 진술: 장윤은 과거 군적 정정에 관여했다고 함.");
            OrderModal("사실 · 주장 · 남은 의문", (lines.Count == 0 ? "아직 직접 확인한 기록이 없다." : string.Join("\n", lines)) + "\n\n" + OrderObjective());
            view.AddChoice("군령과 확인표 비교", ShowOrderComparison, OrderState.CanCompare);
            view.AddChoice("확보 자료 다시 읽기", ShowOrderEvidence);
            view.AddChoice("다음 조사 선택", ShowOrderSelection);
            view.AddChoice("제17호 후속 수사", ShowOrderFollowUp);
            view.AddChoice("수첩 닫기", CloseModal);
        }
        void AddOrderLine(List<string> lines, OrderFact fact, string text) { if (HasOrder(fact)) lines.Add(text); }
        void ShowOrderEvidence()
        {
            OrderModal("확보 자료 재열람", "재열람과 비교에는 추가 시간이 들지 않는다.");
            if (HasOrder(OrderFact.Recovered)) view.AddChoice("회수한 군령", () => { OrderModal("군령", OrderBody); PlaytestObserver.SourceRead("군령"); view.AddChoice("돌아가기", ShowOrderEvidence); });
            if (HasOrder(OrderFact.Receipt)) view.AddChoice("인계 확인표", () => { OrderModal("확인표", OrderReceipt); PlaytestObserver.SourceRead("확인표"); view.AddChoice("돌아가기", ShowOrderEvidence); });
            if (HasOrder(OrderFact.RetainedCopy)) view.AddChoice("보관 부본", () => { OrderModal("부본", OrderCopy); PlaytestObserver.SourceRead("부본"); view.AddChoice("돌아가기", ShowOrderEvidence); });
            view.AddChoice("수첩으로", ShowOrderJournal);
        }
    }
}
