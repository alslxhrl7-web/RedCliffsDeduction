using System;
using System.Linq;
using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        // A chosen destination is a player preference, never an automatically assigned lead.
        public string SelectedLead { get; private set; } = "";
        GameObject guidancePanel;
        UnityEngine.UI.Text guidanceText;
        InvestigationTarget[] guidanceTargets;
        float nextGuidanceRefresh, noticeSeconds;
        string guidanceNotice = "";
        int guidancePage;
        sealed class Lead
        {
            public string id, label, area, inquiry;
            public InvestigationTarget.TargetKind kind;
            public Lead(string id, string label, string area, string inquiry, InvestigationTarget.TargetKind kind)
            { this.id=id; this.label=label; this.area=area; this.inquiry=inquiry; this.kind=kind; }
        }
        static readonly Lead[] Leads = {
            new Lead("archive","보관 자리·반출 기록","문서고","archive",InvestigationTarget.TargetKind.MissingDocument),
            new Lead("caimao","채모 · 수령 여부","수군 진영","receipt",InvestigationTarget.TargetKind.Caimao),
            new Lead("zhangyun","장윤 · 인계 절차","수군 진영","procedure",InvestigationTarget.TargetKind.Zhangyun),
            new Lead("courier","전달자 · 인계 경위","보급 천막","courier",InvestigationTarget.TargetKind.DocumentCourier),
            new Lead("duseok","두석 · 근무 중 목격","수군 진영","movement",InvestigationTarget.TargetKind.Duseok),
            new Lead("seol","설평 · 서류 취급","문서고","",InvestigationTarget.TargetKind.Seolpyeong),
            new Lead("records","발급 기록 탁자","보급 천막","records",InvestigationTarget.TargetKind.CareerRecords),
            new Lead("han","한진 · 담당 업무","문서고","",InvestigationTarget.TargetKind.HanJin),
            new Lead("returned","반환 묶음","문서고","rewrite",InvestigationTarget.TargetKind.ReturnedOrder),
            new Lead("outgoing","출고 인계 기록","수군 진영","rewrite",InvestigationTarget.TargetKind.OutgoingOrder),
            new Lead("storage","수송 물품·반입 기록","수군 진영","storage",InvestigationTarget.TargetKind.OrderStorage),
            new Lead("map","수군 배치도","수군 진영","",InvestigationTarget.TargetKind.MaintenanceOrder)
        };
        bool LeadKnown(Lead l)
        {
            switch(l.id) {
                case "storage": return HasOrder(OrderFact.Movement) || HasOrder(OrderFact.NoMessenger) || HasOrder(OrderFact.StorageTrail);
                case "records": return HasOrder(OrderFact.Recovered) || HasOrder(OrderFact.Procedure);
                case "han": case "returned": return HasOrder(OrderFact.Dispatch) || HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original) || HasFollowUp(OrderFollowUpFact.MetHan);
                case "outgoing": return HasFollowUp(OrderFollowUpFact.Assignment) || HasOrder(OrderFact.Recovered);
                case "map": return HasOrder(OrderFact.Recovered);
                default: return true;
            }
        }
        public bool IsLeadAvailable(string id) => Leads.Any(l=>l.id==id && LeadKnown(l));
        public void SelectInvestigationLead(string id)
        {
            if (!string.IsNullOrEmpty(id) && !IsLeadAvailable(id)) return;
            SelectedLead=id ?? ""; CompleteTutorialSelection(SelectedLead); CloseModal(); nextGuidanceRefresh=0;
        }
        string ShortCost(Lead l) => string.IsNullOrEmpty(l.inquiry) ? "기본 대화 무료"
            : investigationTime.HasStarted("order_"+l.inquiry) ? "재확인 무료"
            : investigationTime.DeadlineReached ? "새 조사 마감" : "신규 1회";
        public void ShowInvestigationDirections() { guidancePage=0; ShowGuidancePage(); }
        void ShowGuidancePage()
        {
            if (!introductionFinished || FinalReportCompleted) return;
            var choices=Leads.Where(LeadKnown).ToArray(); int pages=Math.Max(1,(choices.Length+3)/4);
            guidancePage=Mathf.Clamp(guidancePage,0,pages-1);
            SetModal("조사 방향 선택 · "+(guidancePage+1)+"/"+pages,"어느 쪽을 확인할까?",
                CaseSummary()+"\n\n대상을 고르면 위치를 안내합니다. 선택은 무료입니다.");
            foreach(var l in choices.Skip(guidancePage*4).Take(4)) {
                var captured=l;
                view.AddChoice(l.label+" · "+l.area+" · "+ShortCost(l),()=>SelectInvestigationLead(captured.id));
            }
            if(pages>1) view.AddChoicePair("이전 목록",()=>{guidancePage=(guidancePage+pages-1)%pages;ShowGuidancePage();},"다음 목록",()=>{guidancePage=(guidancePage+1)%pages;ShowGuidancePage();});
            view.AddChoicePair("대상 지정 해제",()=>SelectInvestigationLead(""),"조사 계속",CloseModal);
            view.SetFooterHint("Q / Esc 닫기");
        }
        public string CaseSummary()
        {
            if(FinalReportCompleted) return "현재 수사 · 보고 완료";
            if(HasFollowUp(OrderFollowUpFact.Knowing)) return "현재 수사 · 조사 결과 정리\n확보한 근거와 제출할 결론을 검토 중이다.";
            if(HasOrder(OrderFact.Verified)) return "현재 수사 · 군령 변경 경위\n회수본이 승인 내용과 다르다는 점을 확인했다.";
            if(HasOrder(OrderFact.Compared)) return "현재 수사 · 군령 내용 대조\n군령과 확인표의 지시가 서로 다르다.";
            if(HasOrder(OrderFact.Recovered)) return "현재 수사 · 회수한 군령 확인\n문서를 찾았다. 내용과 인계 경위를 살펴보는 중이다.";
            return "현재 수사 · 군령의 전달 경위\n문서고는 반출했다고 하고, 수령 부대는 받지 못했다고 한다.";
        }
        void RecordGuidanceNotice(string category,string title)
        {
            guidanceNotice=category+" · "+title; noticeSeconds=7; nextGuidanceRefresh=0;
        }
        void RefreshGuidance()
        {
            if(guidancePanel==null) {
                view.objective.fontSize=20;
                guidancePanel=new GameObject("InvestigationGuidance",typeof(RectTransform),typeof(UnityEngine.UI.Image));
                guidancePanel.transform.SetParent(view.transform,false);
                var rect=(RectTransform)guidancePanel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(24,-112);rect.sizeDelta=new Vector2(470,166);
                var bg=guidancePanel.GetComponent<UnityEngine.UI.Image>();bg.color=new Color(.025f,.05f,.055f,.92f);bg.raycastTarget=false;
                var go=new GameObject("Direction",typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(rect,false);
                var tr=(RectTransform)go.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(16,12);tr.offsetMax=new Vector2(-16,-12);
                guidanceText=go.GetComponent<UnityEngine.UI.Text>();guidanceText.font=view.font;guidanceText.fontSize=20;guidanceText.color=new Color(.95f,.9f,.77f);guidanceText.raycastTarget=false;guidanceText.supportRichText=false;
                guidanceText.horizontalOverflow=HorizontalWrapMode.Wrap;guidanceText.verticalOverflow=VerticalWrapMode.Truncate;
                guidanceTargets=UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            }
            bool visible=introductionFinished && !titleScreenActive && !BlocksMovement && !FinalReportCompleted && !RecordArchiveScreen.IsOpen;
            guidancePanel.SetActive(visible);if(!visible)return;
            noticeSeconds=Mathf.Max(0,noticeSeconds-Time.unscaledDeltaTime);
            if(Time.unscaledTime<nextGuidanceRefresh)return;nextGuidanceRefresh=Time.unscaledTime+.15f;
            var lead=Leads.FirstOrDefault(l=>l.id==SelectedLead && LeadKnown(l));
            string lesson=TutorialHint();
            guidanceText.text=!string.IsNullOrEmpty(lesson) ? lesson : lead==null
                ? "Q 조사 대상 선택 · J 기록"
                : lead.label+"\n"+DirectionTo(lead);
            if(noticeSeconds>0 && string.IsNullOrEmpty(lesson))guidanceText.text+="\n"+guidanceNotice;
            var panelRect=(RectTransform)guidancePanel.transform;
            panelRect.sizeDelta=new Vector2(470,Mathf.Max(56,guidanceText.preferredHeight+28));
        }
        string DirectionTo(Lead lead)
        {
            var t=guidanceTargets.FirstOrDefault(x=>x!=null && x.kind==lead.kind && x.gameObject.activeInHierarchy);
            if(t==null || route==null || route.player==null)return lead.area+" · Esc 장소 이동";
            // Direction points to a known destination; it does not reveal unseen evidence or draw a route through walls.
            bool same=lead.area=="보급 천막" ? route.AtSupplyTent : lead.area=="수군 진영" ? route.AtNavalCamp
                : !route.AtSupplyTent && !route.AtNavalCamp && !route.AtCommandTent && !route.OnConnectingRoad;
            
            var delta=t.transform.position-route.player.transform.position;delta.y=0;
            if(same && delta.magnitude<=route.player.interactionDistance)return "대상을 바라보고 E 조사/대화";
            float angle=Vector3.SignedAngle(route.player.transform.forward,delta,Vector3.up);
            string bearing=Mathf.Abs(angle)>135?"↓ 뒤쪽":Mathf.Abs(angle)<25?"↑ 앞쪽":angle<0?"← 왼쪽":"→ 오른쪽";
            return lead.area+" · "+bearing+" "+Mathf.CeilToInt(delta.magnitude)+"m"+(same ? "" : "\n연결 길로 이동 · Esc 장소 이동");
        }
    }
}
