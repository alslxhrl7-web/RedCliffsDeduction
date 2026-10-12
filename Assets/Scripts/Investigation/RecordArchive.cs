using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RedCliffMystery.Investigation
{
    [Serializable] public sealed class RecordArchiveData { public int version=1; public List<string> unlocked=new List<string>(); public List<string> viewed=new List<string>(); }
    public sealed class ArchiveEntry
    {
        public string id, tab, title, body, hint; public int profile=-1;
        public OrderFact fact; public OrderFollowUpFact follow;
        public ArchiveEntry(string id,string tab,string title,string body,string hint="조사에서 관련 기록을 확인하면 열립니다.",OrderFact fact=0,OrderFollowUpFact follow=0,int profile=-1)
        {this.id=id;this.tab=tab;this.title=title;this.body=body;this.hint=hint;this.fact=fact;this.follow=follow;this.profile=profile;}
    }
    public static class RecordArchive
    {
        public static readonly string[] SpeakerNames={"조조","설평","두석","채모","장윤","기록 담당자","전령","교대 병사","동석 병사","한진"};
        public static readonly ArchiveEntry[] Entries=Build();
        static RecordArchiveData data; static string loadedPath; static bool blocked;
        public static string Error {get;private set;}
        public static string PathName=>Path.Combine(Path.GetDirectoryName(InvestigationSaveStore.SavePath),"redcliffs-order-archive-v1.json");
        static ArchiveEntry[] Build()
        {
            var list=new List<ArchiveEntry>();
            void Evidence(string id,string title,string body,OrderFact fact=0,OrderFollowUpFact follow=0)
                =>list.Add(new ArchiveEntry(id,"증거",title,body,fact:fact,follow:follow));
            string[] roles={"조조군 지휘관. 제17호의 인계 경위와 책임을 조사하도록 명했다.","문서고 서기. 과거 소속에 관한 선택 조사는 이번 군령 변경과 구분해야 한다.","문서고 경비. 보았던 동선과 보지 못했던 문서 내용을 구분해서 진술한다.","수군 지휘관. 강 입구 부대의 수령 여부를 확인한다.","수군 지휘관. 본문과 인계 확인표의 대조 절차를 설명한다.","발급 기록 담당자. 직접 받은 묶음과 직접 건넨 답신을 확인한다.","문서 전달자. 제17호를 받아 수령 부대로 운반하는 임무를 맡았다.","문서고 근무 병사. 개인 사정과 군령 변경의 증거는 구분해야 한다.","보급 천막의 병사. 군령의 실제 인계는 자료와 대조해야 한다.","군령 정서·인계 연락관. 손상된 제17호를 내용 보존 조건으로 다시 쓰는 일을 맡았다."};
            for(int i=0;i<10;i++)list.Add(new ArchiveEntry("person-"+i,"인물",SpeakerNames[i],roles[i],"해당 인물과 처음 대화하면 열립니다.",profile:i));
            Evidence("dispatch","제17호 반출 대장","수군 제17호 · 강 입구 감시선 부대 앞으로 발급. 전달자 수령과 대조 완료 표시가 있다. 출고 기록은 목적지 도착을 증명하지 않는다.",fact:OrderFact.Dispatch);
            Evidence("receipt","인계 확인표",InvestigationSession.OrderReceipt,fact:OrderFact.Receipt);
            Evidence("recovered","회수한 군령",InvestigationSession.OrderBody,fact:OrderFact.Recovered);
            Evidence("copy","보관 부본",InvestigationSession.OrderCopy,fact:OrderFact.RetainedCopy);
            Evidence("approval","변경 승인 대장","제17호 승인 내용은 강 입구 유지. 본진 철수로 변경한 승인 항목은 없다.",fact:OrderFact.Approval);
            Evidence("assignment","재작성 의뢰",InvestigationSession.RewriteAssignment,follow:OrderFollowUpFact.Assignment);
            Evidence("original","반환된 원래 본문",InvestigationSession.ReturnedOriginal,follow:OrderFollowUpFact.Original);
            Evidence("intake","반납 접수 목록",InvestigationSession.RewriteIntake,follow:OrderFollowUpFact.Intake);
            Evidence("outgoing","출고 인계 기록",InvestigationSession.OutgoingRecord,follow:OrderFollowUpFact.Outgoing);
            Evidence("reply","변경 문의 회신",InvestigationSession.RejectionReply,follow:OrderFollowUpFact.Rejection);
            Evidence("delivery","답신 수령 기록",InvestigationSession.ReplyDelivery,follow:OrderFollowUpFact.Delivery);
            Evidence("witness","발급 담당의 대면 확인",InvestigationSession.ReplyWitness,follow:OrderFollowUpFact.DeliveryWitness);
            list.Add(new ArchiveEntry("ending-True","결말","지켜낸 강 입구","한진의 제출과 거절 회신 수령 순서가 입증됐다. 전달자의 은닉 책임은 별도로 기록됐다. 잘못된 군령과 대체 재발행이 멈추고 강 입구 배치가 복원됐다.","책임을 입증하는 일과 현장의 명령을 바로잡는 일."));
            list.Add(new ArchiveEntry("ending-Normal","결말","남겨진 공백","군령 변경의 일부를 밝혔지만 책임 입증과 명령 복원·저지를 모두 완성하지 못했다. 세부 결과는 그 회차에 제출한 조치에 따라 다르다.","보고한 사실 뒤에 남은 문제를 돌아본다."));
            list.Add(new ArchiveEntry("ending-Bad","결말","빗나간 보고","군령 변경의 책임을 뒷받침하는 보고를 완성하지 못했다. 은닉과 변경은 서로 다른 행위이며, 이름만 맞히는 것으로는 입증되지 않는다.","지목과 입증 사이의 거리를 돌아본다."));
            list.Add(new ArchiveEntry("time-1","연표","출고 · 반출 기록","제17호가 전달자에게 넘어갔다. 문서고의 반출 대장에 수령 표시가 있다.",fact:OrderFact.Dispatch));
            list.Add(new ArchiveEntry("time-2","연표","인계 단절 · 부대 미수령","문서고의 반출 기록과 달리 강 입구 부대는 제17호를 받지 못했다.",fact:OrderFact.NoReceipt));
            list.Add(new ArchiveEntry("time-3","연표","보관소 · 문서 회수","보관소에서 제17호를 회수했다. 은닉과 본문 변경은 구분해서 확인해야 한다.",fact:OrderFact.Recovered));
            list.Add(new ArchiveEntry("time-4","연표","출고 전 · 변경된 본문","전달자 수령 전 기록에 이미 본진 철수 지시가 있었다.",follow:OrderFollowUpFact.BeforeCourier));
            list.Add(new ArchiveEntry("time-5","연표","답신 수령 → 완성물 반납","한진이 변경 거절 답신을 직접 받은 뒤 철수 본문을 제출했다는 순서를 확인했다.",follow:OrderFollowUpFact.Knowing));
            list.Add(new ArchiveEntry("time-6","연표","최종 명령 · 강 입구 복원","잘못된 군령과 대체 재발행을 막고 강 입구 배치를 복원했다.","현장의 감시 공백을 막은 결말에서 열립니다."));
            return list.ToArray();
        }
        static void Load()
        {
            string path=PathName;if(data!=null&&loadedPath==path)return;
            loadedPath=path;data=new RecordArchiveData();blocked=false;Error=null;
            if(!File.Exists(path))return;
            try{data=Read(path);}catch(Exception e)when(e is IOException||e is ArgumentException||e is UnauthorizedAccessException){
                try{data=Read(path+".bak");Error="기록관의 이전 백업을 복원했습니다.";}
                catch{blocked=true;Error="기록관 파일을 읽지 못했습니다. 원본은 보존했으며 이번 기록은 저장하지 않습니다.";}
            }
        }
        static RecordArchiveData Read(string path)
        {
            var d=JsonUtility.FromJson<RecordArchiveData>(File.ReadAllText(path));
            if(d==null||d.version!=1||d.unlocked==null||d.viewed==null||d.unlocked.Any(id=>!Entries.Any(e=>e.id==id))||d.viewed.Any(id=>!d.unlocked.Contains(id)))throw new InvalidDataException("Invalid archive");return d;
        }
        static void Save()
        {
            if(blocked)return;
            try{Directory.CreateDirectory(Path.GetDirectoryName(loadedPath));string temp=loadedPath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));Read(temp);if(File.Exists(loadedPath))File.Replace(temp,loadedPath,loadedPath+".bak");else File.Move(temp,loadedPath);}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Error="기록관 저장에 실패했습니다. 이번 실행 중의 기록은 유지됩니다.";}
        }
        public static bool Has(string id){Load();return data.unlocked.Contains(id);}
        public static bool IsNew(string id)=>Has(id)&&!data.viewed.Contains(id);
        public static void Viewed(string id){if(IsNew(id)){data.viewed.Add(id);Save();}}
        static bool Add(string id){if(data.unlocked.Contains(id))return false;data.unlocked.Add(id);return true;}
        public static void RecordPerson(int profile){if(profile<0||profile>=10)return;Load();if(Add("person-"+profile))Save();}
        public static void Capture(InvestigationSession session)
        {
            if(session==null||!session.OrderChapterActive)return;Load();bool changed=false;
            foreach(var entry in Entries)if((entry.fact!=0&&session.OrderState.Has(entry.fact))||(entry.follow!=0&&session.OrderState.HasFollowUp(entry.follow)))changed|=Add(entry.id);
            if(changed)Save();
        }
        public static void Ending(OrderReportEnding ending){Load();bool changed=Add("ending-"+ending);if(ending==OrderReportEnding.True)changed|=Add("time-6");if(changed)Save();}
        public static void Reload(){data=null;loadedPath=null;}
    }
}

