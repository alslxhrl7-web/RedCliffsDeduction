using System;
using System.Collections.Generic;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        public const string RewriteAssignment = "재작성 의뢰 · 제17호\n담당: 군령 정서·인계 연락관 한진\n끝부분 목간이 갈라져 취급이 어렵다. 본문 내용은 그대로 옮겨 적고 기존 묶음은 완료 시 반환할 것.\n반환 위치: 문서고 발급 탁자 옆. 내용 변경은 별도 승인 사항이다.";
        public const string ReturnedOriginal = "반환된 출고용 원래 본문 · 제17호\n강 입구 감시선 부대 앞으로\n감시선을 강 입구에 유지하고 현지에서 배치 완료를 보고할 것.\n끝부분 손상과 묶음 표시가 작업 의뢰에 기록된 내용과 맞는다. 이것만으로 진본이라고 단정하지 않고 보관 부본과 대조한다.";
        public const string RewriteIntake = "발급 담당의 반납 접수 목록\n앞 항목: 제17호 변경 문의 회신 전달 완료.\n다음 항목: 한진에게서 기존 묶음과 재작성 완성물을 받음. 완성물의 핵심 지시 — 감시선 본진 철수.\n접수 뒤 출고 담당에게 같은 묶음을 넘겼다. 접수는 내용 변경의 승인을 뜻하지 않는다.";
        public const string OutgoingRecord = "출고 담당 인계 기록 · 제17호\n한진이 제출한 완성물을 받아 전달자에게 인계. 지시: 감시선 본진 철수.\n발급 접수 목록을 베끼지 않고 실제 넘긴 묶음을 보고 기록했다. 번호·대상 부대·묶음 표시가 회수본과 일치한다.\n전달자 수령 전부터 본문에 철수 지시가 있었다는 자료다.";
        public const string RejectionReply = "승인 측 보관 회신 · 제17호 변경 문의\n문의자: 한진. 감시선을 본진으로 철수시키는가?\n회신: 강 입구 유지를 바꾸지 않는다. 내용 보존 재작성만 진행하라.\n회신 식별: 제17호 변경 문의 답신. 이 발송 기록만으로 한진에게 도착했다고 단정하지 않는다.";
        public const string ReplyDelivery = "제17호 변경 문의 답신 · 전달 및 수령 기록\n발급 담당이 회신 원문을 한진에게 건네고 수령 표시를 받음.\n뒤이은 접수 항목에 한진의 재작성 완성물 반납이 있다. 반납물의 핵심 지시는 본진 철수.\n승인 측 회신의 번호·문의 내용과 맞는다. 발송이 아니라 실제 수령의 순서다.";
        public const string ReplyWitness = "발급 담당의 대면 확인\n“강 입구 유지를 바꾸지 말라는 답신을 한진에게 직접 건넸습니다. 수령 표시를 받은 뒤 그가 완성물을 반납했습니다.”\n승인 측 보관 회신과 출고 인계 순서가 이 설명과 맞는다. 반납 접수와 같은 담당자의 설명이므로 독립 증언 두 개로 세지 않는다.";

        bool HasFollowUp(OrderFollowUpFact value) => OrderState.HasFollowUp(value);
        void FollowRecord(OrderFollowUpFact fact, string title, string text, Action back)
        {
            if (!OrderState.TryRecordFollowUp(fact)) return;
            UpdateObjective();
            back();
            view.SetFooterHint("수첩에 기록됨 · " + title + "  /  Esc 닫기");
        }
        void ReadFollowBundle(OrderFollowUpFact facts, string title, string text)
        {
            if (!OrderState.TryRecordFollowUp(facts)) return;
            PlaytestObserver.SourceRead(title);
            UpdateObjective();
            OrderModal(title, text);
            view.SetFooterHint("열람한 자료는 수첩에 자동 기록됩니다 · Esc 닫기");
        }

        void FollowFeedback(string text, Action back)
        {
            OrderModal("근거 다시 살펴보기", text);
            view.AddChoice("다시 판단", () => back());
            view.AddChoice("판단을 보류한다", CloseModal);
        }

        // Judgment options. Each question shows one correct option and two near-misses side by side.
        // A near-miss cites something true but skips one record, so it can only be ruled out by asking what was missed.
        public const string JudgeCourierChanged = "전달자가 받은 뒤 바뀌었다 · 그가 묶음을 따로 숨겨 두었다";
        public const string JudgeBeforeCourier = "전달자가 받기 전에 바뀌었다 · 넘기기 전 기록부터 이미 철수다";
        public const string JudgeAlwaysWithdraw = "처음부터 철수였다 · 한진은 받은 대로 옮겨 적었다";
        public const string JudgeHanByAssignment = "제출자는 한진 · 의뢰서에 재작성 담당으로 적혀 있다";
        public const string JudgeSwapAfterIntake = "단정할 수 없다 · 접수 뒤 출고 사이에 바뀌었을 수 있다";
        public const string JudgeHanSubmitted = "제출자는 한진 · 그가 반납한 완성물이 그대로 출고됐다";
        public const string JudgeKnowingByReceipt = "알고 제출했다 · 답신을 직접 받은 뒤에 철수 본문을 냈다";
        public const string JudgeKnowingByDispatch = "알고 제출했다 · 거절 회신이 나간 뒤에 철수 본문이 들어왔다";
        public const string JudgeMisreadReply = "착오였다 · 회신의 ‘재작성 진행’을 수정 허가로 읽었다";
        const string JudgeNotReady = "지금 수첩에 있는 자료만으로는 이 판단을 확정할 수 없다. 판단은 보류하고 자료를 더 모으자.";

        // Until the question's evidence is in hand, every option gets the same neutral reply,
        // so availability never reveals which option is correct.
        void JudgeFollowUp(int question, string option, bool ready, Action correct, string missed, Action back)
        {
            PlaytestObserver.Judgment(question, option, ready, correct != null);
            if (!ready) { FollowFeedback(JudgeNotReady + "\n\n" + FollowRouteHint(), back); return; }
            if (correct != null) correct();
            else FollowFeedback(missed, back);
        }
        public void ShowOrderHan()
        {
            OrderState.TryRecordFollowUp(OrderFollowUpFact.MetHan);
            // The opening line never changes with the player's judgments; only presenting evidence changes what Han admits.
            string words = "“군령을 옮겨 적고 인계하는 일을 맡았소. 제17호의 이전 묶음은 내 앞 탁자에 반환했소.”";
            OrderModal("연락관 한진", words);
            RedCliffMystery.VarcoIdleMotion.Speak("한진", words);
            view.AddChoice("맡았던 재작성 업무를 확인한다", ShowOrderAssignment);
            if (HasFollowUp(OrderFollowUpFact.Submitted))
            {
                view.AddChoice("제출 기록에 대한 해명을 듣는다", () => {
                    ShowOrderHan();
                    const string denial = "“내가 넘긴 것은 맞소. 변경 문의도 보냈소. 답신 내용은 보관 기록에서 확인하시오.”";
                    view.body.text = denial;
                    RedCliffMystery.VarcoIdleMotion.Speak("한진", denial);
                });
                // Han admits only when confronted with the receipt record, not when a judgment is merely recorded.
                if (HasFollowUp(OrderFollowUpFact.Knowing) && (HasFollowUp(OrderFollowUpFact.Delivery) || HasFollowUp(OrderFollowUpFact.DeliveryWitness)))
                    view.AddChoice("거절 답신의 수령 기록을 들이민다", () => {
                        ShowOrderHan();
                        const string confession = "“…받은 답신을 읽고도 내 판단대로 철수 본문을 넘겼소.”";
                        view.body.text = confession;
                        RedCliffMystery.VarcoIdleMotion.Speak("한진", confession);
                    });
            }
            view.AddChoice("대화 마치기", CloseModal);
        }
        public void ShowOrderAssignment()
        {
            if (!OrderInquiry("rewrite", "제17호 · 재작성과 반환·출고", ShowOrderAssignment)) return;
            ReadFollowBundle(OrderFollowUpFact.Assignment, "한진의 작업 의뢰", RewriteAssignment);
            view.AddChoice("확보한 자료 비교", ShowOrderSubmission);
            view.AddChoice("대화 마치기", CloseModal);
        }
        public void ShowOrderRewrite()
        {
            if (!OrderInquiry("rewrite", "제17호 · 재작성과 반환·출고", ShowOrderRewrite)) return;
            ReadFollowBundle(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original | OrderFollowUpFact.Intake,
                "반환 묶음 · 의뢰와 원문", "[작업 의뢰] 제17호 / 담당 한진 / 내용 보존 재작성, 기존 묶음 반환.\n\n[반환 원문] 강 입구에 감시선을 유지하고 현지에서 보고. 끝부분 손상과 묶음 표시는 의뢰와 일치.\n\n[접수 목록] 변경 문의 답신 전달 뒤, 한진이 원문과 완성물을 반납. 완성물 지시: 본진 철수.");
            view.AddChoice("확보한 자료 비교", ShowOrderSubmission);
            view.AddChoice("세 문서 전문 펼치기", () => ShowFollowSource("반환 묶음 전문", RewriteAssignment + "\n\n" + ReturnedOriginal + "\n\n" + RewriteIntake, ShowOrderRewrite));
            view.AddChoice("조사 계속", CloseModal);
        }
        public void ShowOrderClerk()
        {
            OrderModal("발급 기록 담당자", "“보관 부본과 승인·회신 기록은 바로 옆 탁자에 있습니다. 제가 직접 받은 물건과 건넨 답신에 대해서는 말씀드리겠습니다.”");
            view.AddChoice("한진에게 직접 받은 물건을 묻는다", ShowOrderIntake);
            view.AddChoice("한진에게 답신을 직접 건넸는가?", ShowOrderReplyWitness);
            view.AddChoice("대화 마치기", CloseModal);
        }
        public void ShowOrderIntake()
        {
            if (!OrderInquiry("rewrite", "제17호 · 재작성과 반환·출고", ShowOrderIntake)) return;
            OrderState.TryRecordFollowUp(OrderFollowUpFact.ClerkConfirmation);
            UpdateObjective(); ShowOrderClerk();
            view.body.text = "“한진에게서 기존 묶음과 완성물을 직접 받았습니다. 같은 완성물을 출고 담당에게 넘겼습니다.”\n\n반납 접수 목록을 작성한 사람의 진술로 기록했다.";
            RedCliffMystery.VarcoIdleMotion.Speak("기록 담당자", view.body.text);
            view.SetFooterHint("진술 자동 기록 · 대화 마치기 또는 다음 질문");
        }
        public void ShowOrderReplyWitness()
        {
            if (!OrderInquiry("reply", "제17호 · 승인 회신과 도착 순서", ShowOrderReplyWitness)) return;
            OrderState.TryRecordFollowUp(OrderFollowUpFact.DeliveryWitness);
            UpdateObjective(); ShowOrderClerk(); view.body.text = ReplyWitness;
            RedCliffMystery.VarcoIdleMotion.Speak("기록 담당자", ReplyWitness);
            view.SetFooterHint("진술 자동 기록 · 대화 마치기 또는 다음 질문");
        }
        public void ShowOrderOutgoing()
        {
            if (!OrderInquiry("rewrite", "제17호 · 재작성과 반환·출고", ShowOrderOutgoing)) return;
            ReadFollowBundle(OrderFollowUpFact.Outgoing, "출고 인계 기록 · 제17호", "제출자: 한진 → 출고 담당 → 전달자\n지시: 감시선 본진 철수.\n번호·대상 부대·묶음 표시는 회수본과 일치한다. 출고 담당이 실제 넘긴 물건을 보고 남긴 기록이다.");
            view.AddChoice("확보한 자료 비교", ShowOrderSubmission);
            view.AddChoice("출고 기록 전문", () => ShowFollowSource("출고 기록 전문", OutgoingRecord, ShowOrderOutgoing));
            view.AddChoice("조사 계속", CloseModal);
        }
        public void ShowOrderReplies()
        {
            if (!OrderInquiry("reply", "제17호 · 승인 회신과 도착 순서", ShowOrderReplies)) return;
            ReadFollowBundle(OrderFollowUpFact.Rejection | OrderFollowUpFact.Delivery, "탁자 위 회신철 · 제17호", "[승인 측 보관 회신] 문의자 한진. ‘강 입구 유지를 바꾸지 않는다. 내용 보존 재작성만 진행하라.’\n\n[수령 기록] 발급 담당이 한진에게 답신을 건네 수령 표시를 받음. 그 뒤 한진의 완성물이 접수됨.\n\n[완성물 접수] 감시선 본진 철수.");
            view.AddChoice("확보한 자료 비교", ShowOrderReplyOrder);
            view.AddChoice("회신과 수령 기록 전문", () => ShowFollowSource("회신철 전문", RejectionReply + "\n\n" + ReplyDelivery, ShowOrderReplies));
            view.AddChoice("조사 계속", CloseModal);
        }
        void ShowFollowSource(string title, string text, Action back)
        {
            OrderModal(title, text);
            PlaytestObserver.SourceRead(title);
            view.AddChoice("간추린 자료로", () => back());
            view.AddChoice("닫기", CloseModal);
        }

        public void ShowOrderOriginalComparison()
        {
            bool ready = HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original) && HasOrder(OrderFact.RetainedCopy);
            OrderModal("반환 본문 · 보관 부본", ready
                ? ReturnedOriginal + "\n\n" + OrderCopy + "\n\n반환 위치와 손상 기록은 작업 의뢰와 연결된다. 이 자료로 어디까지 말할 수 있는가?"
                : "반환 본문·재작성 의뢰·발급 담당 보관 부본을 확인해야 출처와 내용을 비교할 수 있다.");
            if (ready)
            {
                view.AddChoice("재작성 전후 내용 차이, 변경 허가 확인이 필요", () => FollowRecord(OrderFollowUpFact.OriginalCompared, "원래 본문의 출처 확인", "반환 본문은 의뢰·반납 이력 및 독립 부본과 연결된다. 원래 지시는 강 입구 유지다. 원본 발견만으로 제출자나 고의가 입증되지는 않는다.", ShowOrderFollowUp));
                view.AddChoice("오래된 문서이므로 무조건 진본이다", () => FollowFeedback("오래됐다는 이유만으로 진본이 되지 않는다. 번호·보관 경위·부본과의 일치를 확인해야 한다.", ShowOrderOriginalComparison));
            }
            view.AddChoice("수첩으로", ShowOrderFollowUp);
        }
        string SubmissionEvidence()
        {
            var lines = new List<string>();
            if (HasOrder(OrderFact.Recovered)) lines.Add("[회수본] 제17호 / 감시선 본진 철수.");
            if (HasFollowUp(OrderFollowUpFact.Assignment)) lines.Add("[작업 의뢰] 한진 / 내용 보존 재작성 / 기존 묶음 반환.");
            if (HasFollowUp(OrderFollowUpFact.Original)) lines.Add("[반환 원문] 강 입구 유지. 의뢰의 손상·묶음 표시와 일치.");
            if (HasFollowUp(OrderFollowUpFact.Intake)) lines.Add("[접수 목록] 한진이 반납한 완성물: 본진 철수.");
            if (HasFollowUp(OrderFollowUpFact.Outgoing)) lines.Add("[독립 출고 기록] 한진의 완성물 → 전달자 / 본진 철수.");
            if (HasFollowUp(OrderFollowUpFact.ClerkConfirmation)) lines.Add("[발급 담당 진술] 한진에게 받은 묶음을 그대로 출고 담당에게 전달.");
            return lines.Count == 0 ? "아직 제출 관련 자료가 없다." : string.Join("\n", lines);
        }
        public void ShowOrderSubmission()
        {
            string question = !HasFollowUp(OrderFollowUpFact.BeforeCourier) ? "1 / 3 · 내용이 달라진 시점" : "2 / 3 · 바뀐 본문을 제출한 사람";
            OrderModal(question, SubmissionEvidence());
            view.SetFooterHint(FollowRouteHint());
            if (!HasFollowUp(OrderFollowUpFact.BeforeCourier))
            {
                // All options are always shown and look alike. Each wrong option cites a real fact but skips one record.
                bool ready = OrderState.CanLocateChange;
                view.AddChoice(JudgeCourierChanged, () => JudgeFollowUp(1, JudgeCourierChanged, ready, null,
                    "숨긴 사람이 곧 고친 사람이라는 근거는 없다. 전달자 손에 들어가기 전, 그 묶음을 실제로 보고 적은 기록이 무엇이라고 적었는지 다시 보자.", ShowOrderSubmission));
                view.AddChoice(JudgeBeforeCourier, () => JudgeFollowUp(1, JudgeBeforeCourier, ready,
                    () => FollowRecord(OrderFollowUpFact.BeforeCourier, "전달자 수령 전 변경", "", ShowOrderSubmission), null, ShowOrderSubmission));
                view.AddChoice(JudgeAlwaysWithdraw, () => JudgeFollowUp(1, JudgeAlwaysWithdraw, ready, null,
                    "재작성 의뢰에는 기존 묶음을 반환하라고 적혀 있다. 반환된 기존 묶음이나 인계 확인표에 적힌 원래 지시가 무엇이었는지 다시 보자.", ShowOrderSubmission));
            }
            else if (!HasFollowUp(OrderFollowUpFact.Submitted))
            {
                bool ready = OrderState.CanIdentifySubmission;
                view.AddChoice(JudgeHanByAssignment, () => JudgeFollowUp(2, JudgeHanByAssignment, ready, null,
                    "의뢰서는 누가 일을 맡았는지만 보여 준다. 일을 맡은 사람과 바뀐 본문을 실제로 넘긴 사람이 같다는 것을 어떤 기록이 이어 주는지 다시 보자.", ShowOrderSubmission));
                view.AddChoice(JudgeSwapAfterIntake, () => JudgeFollowUp(2, JudgeSwapAfterIntake, ready, null,
                    "중간에 바뀌었다면 받은 때와 넘긴 때의 지시가 달라야 한다. 한진에게서 받은 순간 그 완성물이 무엇을 지시했는지, 각 담당이 남긴 기록을 다시 보자.", ShowOrderSubmission));
                view.AddChoice(JudgeHanSubmitted, () => JudgeFollowUp(2, JudgeHanSubmitted, ready,
                    () => FollowRecord(OrderFollowUpFact.Submitted, "한진의 제출", "", ShowOrderReplyOrder), null, ShowOrderSubmission));
            }
            else view.AddChoice("3 / 3 · 착오인지 고의인지 비교", ShowOrderReplyOrder);
            view.AddChoice("수첩으로", ShowOrderFollowUp);
        }
        public void ShowOrderReplyOrder()
        {
            var lines = new List<string>();
            if (HasFollowUp(OrderFollowUpFact.Submitted)) lines.Add("[확인한 제출] 한진 / 철수 본문 완성물.");
            if (HasOrder(OrderFact.Verified)) lines.Add("[승인 내용 대조] 승인 지시는 강 입구 유지.");
            if (HasFollowUp(OrderFollowUpFact.Rejection)) lines.Add("[거절 회신] 한진의 철수 문의에 ‘강 입구 유지, 내용 보존 재작성’으로 답함.");
            if (HasFollowUp(OrderFollowUpFact.Delivery)) lines.Add("[수령 기록] 한진의 답신 수령 → 철수 본문 반납.");
            if (HasFollowUp(OrderFollowUpFact.DeliveryWitness)) lines.Add("[직접 전달 진술] 발급 담당이 답신을 한진에게 건넨 뒤 완성물을 받음.");
            OrderModal("3 / 3 · 착오인가, 알고 제출했는가", string.Join("\n", lines));
            view.SetFooterHint(FollowRouteHint());
            if (!HasFollowUp(OrderFollowUpFact.Knowing))
            {
                bool ready = OrderState.CanOrderReply && HasOrder(OrderFact.Verified);
                view.AddChoice(JudgeKnowingByReceipt, () => JudgeFollowUp(3, JudgeKnowingByReceipt, ready,
                    () => FollowRecord(OrderFollowUpFact.Sequence | OrderFollowUpFact.Knowing, "회신 순서와 고의 판단", "", ShowOrderFollowUp), null, ShowOrderReplyOrder));
                view.AddChoice(JudgeKnowingByDispatch, () => JudgeFollowUp(3, JudgeKnowingByDispatch, ready, null,
                    "회신이 나간 때와 한진이 그것을 손에 쥔 때는 다르다. 한진에게 실제로 건네졌다는 표시가 어디에 있고, 그것이 반납보다 앞서는지 다시 보자.", ShowOrderReplyOrder));
                view.AddChoice(JudgeMisreadReply, () => JudgeFollowUp(3, JudgeMisreadReply, ready, null,
                    "회신을 처음부터 다시 읽자. 한진은 무엇을 물었고, 회신은 그 물음에 무엇이라 답했는가.", ShowOrderReplyOrder));
            }
            view.AddChoicePair("제출 자료와 비교", ShowOrderSubmission, "수첩으로", ShowOrderFollowUp);
        }
        public void ShowOrderJudgment() => ShowOrderReplyOrder();
        string FollowRouteHint()
        {
            if (!HasFollowUp(OrderFollowUpFact.Assignment)) return "문서고: 한진 앞 반환 묶음에서 작업 의뢰를 확인할 수 있다.";
            if (!HasOrder(OrderFact.Recovered)) return "수군 진영: 수송 물품에서 제17호 꾸러미를 찾자.";
            if (!HasFollowUp(OrderFollowUpFact.Intake) && !HasFollowUp(OrderFollowUpFact.Outgoing)) return "수군 진영: 출고 기록 탁자를 확인하자. 전령은 보급 천막에 있다.";
            if (HasFollowUp(OrderFollowUpFact.BeforeCourier) && !HasFollowUp(OrderFollowUpFact.Submitted) && !OrderState.CanIdentifySubmission) return "보급 천막: 발급 담당에게 한진의 반납을 확인하고, 수군 진영의 출고 기록과 연결하자.";
            if (!HasOrder(OrderFact.Verified)) return "보급 천막: 발급 기록 탁자의 부본·승인 기록으로 회수본을 대조하자.";
            if (!HasFollowUp(OrderFollowUpFact.Rejection) || !(HasFollowUp(OrderFollowUpFact.Delivery) || HasFollowUp(OrderFollowUpFact.DeliveryWitness))) return "보급 천막: 발급 기록 탁자의 회신철을 읽자. 담당자의 직접 전달 진술도 확인할 수 있다.";
            if (!HasFollowUp(OrderFollowUpFact.Knowing)) return "확보한 자료만으로 수첩에서 비교를 이어갈 수 있다. 재방문은 필요 없다.";
            return HasOrder(OrderFact.Map) ? "조조의 지휘 천막에서 결론과 제출 근거를 골라 최종 보고하자." : "수군 배치도로 명령 변경의 영향을 확인할 수 있다. 준비되면 조조에게 최종 보고하자.";
        }

        public void ShowOrderFollowUp()
        {
            string status = "";
            status += "1. 변경 시점: " + (HasFollowUp(OrderFollowUpFact.BeforeCourier) ? "전달자 수령 전" : "미판단") + "\n";
            status += "2. 제출자: " + (HasFollowUp(OrderFollowUpFact.Submitted) ? "한진의 완성물 제출 확인" : "미판단") + "\n";
            status += "3. 인지 여부: " + (HasFollowUp(OrderFollowUpFact.Knowing) ? "거절 수령 뒤 제출 · 고의 근거 확보" : "미판단") + "\n";
            status += "군령 중지와 배치 요청은 최종 보고에서 선택한다.";
            OrderModal("제17호 후속 수사", status);
            view.SetFooterHint(FollowRouteHint());
            view.AddChoice(HasFollowUp(OrderFollowUpFact.Knowing) ? "판단 근거 다시 보기" : "확보한 자료로 추리 이어가기", HasFollowUp(OrderFollowUpFact.Submitted) ? ShowOrderReplyOrder : ShowOrderSubmission);
            view.AddChoice("자료 전문 · 선택 열람", ShowOrderFollowSources);
            view.AddChoice("군령·확인표·부본 다시 읽기", ShowOrderEvidence);
            view.AddChoice("수첩 닫기", CloseModal);
        }
        void ShowOrderFollowSources()
        {
            var lines = new List<string>();
            if (HasFollowUp(OrderFollowUpFact.Assignment)) lines.Add(RewriteAssignment);
            if (HasFollowUp(OrderFollowUpFact.Original)) lines.Add(ReturnedOriginal);
            if (HasFollowUp(OrderFollowUpFact.Intake)) lines.Add(RewriteIntake);
            if (HasFollowUp(OrderFollowUpFact.Outgoing)) lines.Add(OutgoingRecord);
            if (HasFollowUp(OrderFollowUpFact.ClerkConfirmation)) lines.Add("발급 담당 진술: 한진에게서 기존 묶음과 완성물을 받아 같은 완성물을 출고 담당에게 넘겼다.");
            if (HasFollowUp(OrderFollowUpFact.Rejection)) lines.Add(RejectionReply);
            if (HasFollowUp(OrderFollowUpFact.Delivery)) lines.Add(ReplyDelivery);
            if (HasFollowUp(OrderFollowUpFact.DeliveryWitness)) lines.Add(ReplyWitness);
            OrderModal("확보한 자료 전문", lines.Count == 0 ? "확보한 자료가 없다." : string.Join("\n\n", lines));
            PlaytestObserver.SourceRead("확보한 자료 전문");
            if (HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original) && HasOrder(OrderFact.RetainedCopy))
                view.AddChoice("원문 출처 추가 검토 · 선택", ShowOrderOriginalComparison);
            view.AddChoice("수첩으로", ShowOrderFollowUp);
        }
    }
}
