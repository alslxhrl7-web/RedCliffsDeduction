using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class ConversationPresentation : MonoBehaviour
    {
        public InvestigationSession session; public Camera viewCamera;
        RedCliffMystery.VarcoIdleMotion subject;
        Light[] lights; float originalFov; Quaternion originalRotation; bool engaged;
        RectTransform panel; Vector2 min,max; Color shadeColor; UnityEngine.UI.Image shade;
        public bool Active=>engaged;
        public static void Begin(RedCliffMystery.VarcoIdleMotion actor)
        {
            var presentation=FindAnyObjectByType<ConversationPresentation>();
            if(presentation==null)return;
            presentation.subject=actor;
            RecordArchive.RecordPerson(actor.Profile);
        }
        public static void End(){var p=FindAnyObjectByType<ConversationPresentation>();if(p)p.subject=null;}
        public static void BeforeTravel(){var p=FindAnyObjectByType<ConversationPresentation>();if(p){p.subject=null;p.Restore();}}
        void LateUpdate()
        {
            if(!viewCamera||!session)return;
            bool wanted=subject!=null&&session.view.gameObject.activeInHierarchy&&session.view.modal.activeInHierarchy
                &&!RecordArchiveScreen.IsOpen&&Vector3.Distance(subject.transform.position,viewCamera.transform.position)<5f;
            if(!wanted){Restore();return;}
            if(!engaged){
                engaged=true;originalFov=viewCamera.fieldOfView;originalRotation=viewCamera.transform.localRotation;
                panel=session.view.modal.transform.Find("Panel").GetComponent<RectTransform>();min=panel.anchorMin;max=panel.anchorMax;
                shade=session.view.modal.GetComponent<UnityEngine.UI.Image>();shadeColor=shade.color;
                panel.anchorMin=new Vector2(.035f,.08f);panel.anchorMax=new Vector2(.52f,.86f);shade.color=new Color(.015f,.025f,.03f,.12f);
            }
            if(lights==null){lights=new Light[3];for(int i=0;i<3;i++){var go=new GameObject("ConversationLight"+i);go.transform.SetParent(transform);lights[i]=go.AddComponent<Light>();lights[i].type=i==2?LightType.Point:LightType.Spot;lights[i].shadows=LightShadows.None;lights[i].range=i==2?1.15f:3;lights[i].spotAngle=75;lights[i].intensity=i==0?.85f:i==1?.8f:.08f;lights[i].color=i==1?new Color(.65f,.78f,1):new Color(1,.84f,.65f);}}
            var rr=subject.GetComponentsInChildren<Renderer>();if(rr.Length==0){Restore();return;}
            Bounds b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
            Vector3 face=new Vector3(b.center.x,b.min.y+b.size.y*.88f,b.center.z);
            Vector3 toward=(viewCamera.transform.position-face).normalized;Vector3 right=Vector3.Cross(Vector3.up,toward).normalized;
            Vector3[] positions={face+toward*.8f+right*.65f+Vector3.up*.35f,face-toward*.6f-right*.5f+Vector3.up*.3f,face+toward*.45f};
            for(int i=0;i<3;i++){lights[i].enabled=true;lights[i].transform.position=positions[i];lights[i].transform.LookAt(face);}
            var aim=Quaternion.LookRotation(face-Vector3.up*.18f-viewCamera.transform.position)*Quaternion.Euler(0,-12,0);
            viewCamera.transform.rotation=Quaternion.Slerp(viewCamera.transform.rotation,aim,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
            float distance=Vector3.Distance(face,viewCamera.transform.position);
            viewCamera.fieldOfView=Mathf.Lerp(viewCamera.fieldOfView,distance<1.3f?55:45,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
        }
        void Restore()
        {
            if(lights!=null)foreach(var light in lights)if(light)light.enabled=false;
            if(!engaged)return;
            if(viewCamera){viewCamera.fieldOfView=originalFov;viewCamera.transform.localRotation=originalRotation;}
            if(panel){panel.anchorMin=min;panel.anchorMax=max;}if(shade)shade.color=shadeColor;engaged=false;
        }
        void OnDisable(){subject=null;Restore();}
    }
}
