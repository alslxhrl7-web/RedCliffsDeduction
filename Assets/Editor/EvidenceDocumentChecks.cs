using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class EvidenceDocumentChecks
    {
        static InvestigationView view;
        static int step;
        static double next;
        static readonly string[] Headings={"동쪽 수채의 정비 닷새","형주 인력 인계 명부","이전 근무 기록","형주 남군 강릉의 신고 신원 대조","목연에게 내린 지시","업무 서신 안쪽의 접지","개인 서류 조사에는 허가가 필요하다"};
        [MenuItem("적벽추리/플레이 검증/증거 문서 화면 확인")]
        static void Begin()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter isolated play test first.");
            view=UnityEngine.Object.FindAnyObjectByType<InvestigationView>();
            UnityEngine.Object.FindAnyObjectByType<InvestigationSession>().enabled=false;
            view.SetPrologueLayout(false); step=0; next=EditorApplication.timeSinceStartup;
            File.WriteAllText("Logs/EvidenceDocuments/ui-check.txt",""); EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next) return;
            next=EditorApplication.timeSinceStartup+1;
            try
            {
                if(step<Headings.Length*2)
                {
                    int index=step/2;
                    if(step%2==0)
                    {
                        string[] fields={"MaintenanceOrder","PersonnelRecord","DutyRecord",null,"SecretLetter"};
                        string content=index<5 && fields[index]!=null
                            ? (string)typeof(InvestigationSession).GetField(fields[index],System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).GetRawConstantValue()
                            : index==3 ? "형주 인력 편입 때 함께 이관된 호적 등본이다. 이름이 보이지 않는다는 이유만으로 결론 내리지 말고 자료 범위와 표기를 확인하자."
                            : "허가와 문서 발견 조건을 충족하기 전에는 밀서 이미지를 표시하지 않는다.";
                        view.ShowModal("증거 문서 확인",Headings[index],content,"조사를 계속한다");
                        view.AddChoice("조사를 계속한다",()=>{}); Canvas.ForceUpdateCanvases();
                    }
                    else
                    {
                        var preview=view.GetComponent<EvidenceDocumentView>();
                        if(preview.Visible!=(index<5)) throw new InvalidOperationException("Preview disclosure: "+Headings[index]);
                        var scroll=view.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
                        if(scroll.viewport.rect.width<200 || scroll.viewport.rect.height<50) throw new InvalidOperationException("Document text area too small");
                        var panel=view.modal.transform.Find("Panel/EvidenceDocumentPreview");
                        if(index<5 && panel.GetComponentsInChildren<UnityEngine.UI.Graphic>().Any(g=>g.raycastTarget)) throw new InvalidOperationException("Preview blocks choices");
                        File.AppendAllText("Logs/EvidenceDocuments/ui-check.txt","PASS "+Headings[index]+" textArea="+scroll.viewport.rect.size+"\n");
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/EvidenceDocuments/preview-"+index+".png"));
                    }
                    step++; return;
                }
                view.HideModal(); EditorApplication.update-=Tick; EditorApplication.isPlaying=false;
            }
            catch(Exception e) { File.AppendAllText("Logs/EvidenceDocuments/ui-check.txt","FAIL "+e); EditorApplication.update-=Tick; EditorApplication.isPlaying=false; Debug.LogException(e); }
        }
    }
}
