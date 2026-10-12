using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    public sealed class RecordArchiveScreen : MonoBehaviour
    {
        static RecordArchiveScreen current;
        public static bool IsOpen=>current!=null&&current.gameObject.activeInHierarchy;
        public string CurrentTab{get;private set;}="결말";
        public string SelectedId{get;private set;}
        Font font; Action closed; RectTransform list,detail; UnityEngine.UI.Text count; int page;
        ArchivePortrait stage; AudioSource wind, cue; AudioClip unlockTone; float opened;
        static readonly Color Gold=new Color(.83f,.68f,.43f),Ivory=new Color(.95f,.91f,.81f);
        public static RecordArchiveScreen Show(Font font,Action closed,string tab="결말")
        {
            if(IsOpen)return FindAnyObjectByType<RecordArchiveScreen>();
            var go=new GameObject("RedCliffsArchive",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=150;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var screen=go.AddComponent<RecordArchiveScreen>();screen.font=Resources.Load<Font>("Title/RedCliffsSerif-SemiBold")??font;screen.closed=closed;screen.opened=Time.unscaledTime;current=screen;
            screen.Build();screen.SelectTab(tab);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return screen;
        }
        void Build()
        {
            var background=Rect("Ink",transform,0,0,1,1);background.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.035f,.043f);
            Text(transform,"Title","적벽 기록관",38,Gold,.05f,.86f,.5f,.96f);
            Text(transform,"Chapter","제17호 군령 사건 · 회차를 넘어 남기는 기록",18,Ivory,.05f,.80f,.80f,.86f);
            count=Text(transform,"Collection","",18,Gold,.52f,.89f,.93f,.95f);
            string[] tabs={"인물","증거","결말","연표"};for(int i=0;i<4;i++){string t=tabs[i];Button(transform,t,()=>SelectTab(t),.05f+i*.16f,.71f,.20f+i*.16f,.78f);}
            Button(transform,"돌아가기",Close,.79f,.71f,.95f,.78f);
            list=Rect("Cards",transform,.05f,.13f,.34f,.68f);detail=Rect("Detail",transform,.37f,.13f,.95f,.68f);
            Text(transform,"Footer","잠긴 기록은 조사 중 확인한 뒤 열립니다. 열람은 조사 횟수를 쓰지 않습니다.  ·  Esc 돌아가기",17,Ivory,.05f,.025f,.95f,.075f);
            wind=gameObject.AddComponent<AudioSource>();wind.clip=Resources.Load<AudioClip>("Audio/CampWindAmbience");wind.loop=true;wind.spatialBlend=0;wind.volume=.12f*InvestigationAudioSettings.Background;if(wind.clip)wind.Play();
            cue=gameObject.AddComponent<AudioSource>();cue.playOnAwake=false;cue.spatialBlend=0;
            var samples=new float[22050];for(int i=0;i<samples.Length;i++){float t=i/44100f;samples[i]=.2f*Mathf.Exp(-t*12)*(Mathf.Sin(t*2*Mathf.PI*440)+.3f*Mathf.Sin(t*2*Mathf.PI*880))*Mathf.Clamp01(t*1000);}
            unlockTone=AudioClip.Create("ArchivePluckedNote",samples.Length,1,44100,false);unlockTone.SetData(samples,0);
        }
        public void SelectTab(string tab){CurrentTab=tab;page=0;Draw();}
        void Clear(Transform t){foreach(Transform child in t){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void Draw()
        {
            Clear(list);Clear(detail);SelectedId=null;if(stage){Destroy(stage.gameObject);stage=null;}
            count.text="인물 "+Total("인물")+"   증거 "+Total("증거")+"   결말 "+Total("결말");
            var entries=RecordArchive.Entries.Where(e=>e.tab==CurrentTab).ToArray();
            for(int i=page*5;i<Mathf.Min(entries.Length,page*5+5);i++){
                var e=entries[i];int row=i-page*5;bool owned=RecordArchive.Has(e.id);
                Button(list,(owned?(RecordArchive.IsNew(e.id)?"새 기록 · ":"")+e.title:"봉인된 기록  ???"),()=>OpenEntry(e.id),0, .82f-row*.17f,1,.97f-row*.17f);
            }
            int pages=Mathf.CeilToInt(entries.Length/5f);
            if(pages>1){Button(list,"〈",()=>{page=(page+pages-1)%pages;Draw();},0,0,.2f,.1f);Text(list,"Page",(page+1)+" / "+pages,18,Ivory,.3f,0,.7f,.1f);Button(list,"〉",()=>{page=(page+1)%pages;Draw();},.8f,0,1,.1f);}
            Text(detail,"Instruction",RecordArchive.Error??"기록을 선택해 내용을 확인하세요.\n\n이번 기록관은 한진의 군령 사건만 수록합니다.\n잠긴 카드에는 이름과 정답을 표시하지 않습니다.",24,Ivory,0,.2f,1,.9f);
        }
        string Total(string tab){var e=RecordArchive.Entries.Where(x=>x.tab==tab);return e.Count(x=>RecordArchive.Has(x.id))+" / "+e.Count();}
        public void OpenEntry(string id)
        {
            var entry=RecordArchive.Entries.Single(e=>e.id==id);Clear(detail);if(stage){Destroy(stage.gameObject);stage=null;}SelectedId=id;
            bool owned=RecordArchive.Has(id);
            Text(detail,"Heading",owned?entry.title:"아직 밝혀지지 않은 기록",29,Gold,0,.81f,1,1);
            if(!owned){Text(detail,"Locked",entry.hint,23,Ivory,0,.25f,1,.76f);return;}
            bool fresh=RecordArchive.IsNew(id);RecordArchive.Viewed(id);
            if(fresh&&cue)cue.PlayOneShot(unlockTone,.35f*InvestigationAudioSettings.Effects);
            if(fresh)Text(detail,"New","새로 채워진 기록",16,Gold,.60f,.94f,1,1);
            Text(detail,"Body",entry.body,22,Ivory,0,entry.profile>=0?.36f:.12f,entry.profile>=0?.55f:1,.78f);
            if(entry.profile>=0){
                var portrait=Rect("Portrait",detail,.58f,.1f,1,.78f);var picture=Rect("View",portrait,0,0,1,1);var raw=picture.gameObject.AddComponent<RawImage>();stage=picture.gameObject.AddComponent<ArchivePortrait>();stage.Show(entry.profile,raw);
                Text(detail,"RotateHint","인물상을 드래그해 돌려볼 수 있습니다.",16,Ivory,0,.05f,1,.16f);
                string extra=entry.profile==9&&RecordArchive.Has("time-5")?"확인된 이면: 거절 답신을 읽은 뒤에도 철수 본문을 제출했다.":entry.profile==6&&RecordArchive.Has("recovered")?"문서 은닉과 내용 변경의 책임은 구분해서 살펴야 한다.":"진술의 사실 여부는 증거 탭에서 확인하세요.";
                Text(detail,"Details",extra,19,Gold,0,.17f,.55f,.35f);
            }
            if(id=="ending-True")Button(detail,"후일담 · 한진에게 다시 묻다",()=>Bonus(0),0,0,1,.11f);
            if(entry.tab=="증거")Button(detail,"문서 외형 참고 보기",()=>EvidenceImage(id),0,0,1,.11f);
        }
        void EvidenceImage(string id)
        {
            if(!RecordArchive.Has(id))return;Clear(detail);
            var evidence=FindObjectsByType<EvidenceDocumentView>(FindObjectsInactive.Include).FirstOrDefault();
            if(evidence&&evidence.dutyRecord){var r=Rect("DocumentImage",detail,.12f,.19f,.88f,.90f);var picture=Rect("View",r,0,0,1,1);var image=picture.gameObject.AddComponent<RawImage>();image.texture=evidence.dutyRecord;image.raycastTarget=false;var aspect=picture.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=(float)image.texture.width/image.texture.height;}
            Text(detail,"Caption","공통 문서 외형 참고 · 실제 증거 내용은 본문에서 확인",17,Gold,0,.10f,1,.18f);
            Button(detail,"본문으로 돌아가기",()=>OpenEntry(id),0,0,1,.1f);
        }
        public void Bonus(int question)
        {
            if(!RecordArchive.Has("ending-True"))return;
            Clear(detail);if(stage){Destroy(stage.gameObject);stage=null;}
            string[] answers={"보고가 끝난 뒤, 한진에게 확인된 기록을 다시 묻는다.\n\n이 장면은 군령 사건의 후일담이다. 새로운 범행이나 역사적 결말을 확정하지 않는다.","참모: 맡은 일은 무엇이었소?\n\n한진: 끝부분이 갈라진 제17호를 다시 쓰는 일이었소. 본문을 그대로 옮기고 기존 묶음은 반환하라는 의뢰였소. 내용을 바꾸라는 허가는 아니었소.","참모: 거절 답신은 읽었소?\n\n한진: …읽었소. 강 입구 유지를 바꾸지 말라는 뜻이었지. 그래도 내 판단대로 철수 본문을 넘겼소.\n\n참모: 수령 기록과 반납 순서가 그 사실을 보여 주었소.","참모: 전달자가 숨긴 일도 당신의 행위인가?\n\n한진: 내가 제출한 완성물에는 이미 철수 지시가 있었소. 그 뒤의 일을 내 재작성과 같은 일로 말할 수는 없소.\n\n은닉 책임과 변경 책임은 별도로 기록된다. 진실을 밝힌 뒤에도, 잘못된 명령을 멈추는 조치가 필요했다."};
            Text(detail,"Heading","후일담 · 한진의 진술",28,Gold,0,.85f,1,1);Text(detail,"Body",answers[question],18,Ivory,0,.31f,1,.83f);
            string[] questions={"재작성 의뢰","거절 답신","전달자의 은닉"};for(int i=0;i<3;i++){int q=i+1;Button(detail,questions[i],()=>Bonus(q),i*.335f,.18f,i*.335f+.32f,.30f);}
            Button(detail,"기록으로 돌아가기",()=>OpenEntry("ending-True"),0,0,1,.12f);
        }
        void Update(){if(wind)wind.volume=.12f*InvestigationAudioSettings.Background;if(Time.unscaledTime-opened>.25f&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
        public void Close(){if(current!=this)return;current=null;gameObject.SetActive(false);closed?.Invoke();Destroy(gameObject);}
        void OnDestroy(){if(current==this)current=null;if(unlockTone)Destroy(unlockTone);}
        UnityEngine.UI.Text Text(Transform p,string name,string text,int size,Color color,float x,float y,float xx,float yy){var t=Rect(name,p,x,y,xx,yy).gameObject.AddComponent<UnityEngine.UI.Text>();t.font=font;t.fontSize=size;t.color=color;t.text=text;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        void Button(Transform p,string label,UnityEngine.Events.UnityAction click,float x,float y,float xx,float yy){var rect=Rect(label,p,x,y,xx,yy);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.13f,.17f,.18f);var b=rect.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(click);var text=Text(rect,"Label",label,20,Ivory,.04f,.05f,.96f,.95f);text.alignment=TextAnchor.MiddleLeft;}
        static RectTransform Rect(string name,Transform p,float x,float y,float xx,float yy){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    }
    public sealed class ArchivePortrait : MonoBehaviour,IDragHandler
    {
        GameObject root,figure;RenderTexture texture;Camera camera;readonly System.Collections.Generic.List<Mesh> meshes=new System.Collections.Generic.List<Mesh>();
        public void Show(int profile,RawImage image)
        {
            var source=FindObjectsByType<RedCliffMystery.VarcoIdleMotion>(FindObjectsInactive.Include).FirstOrDefault(m=>m.Profile==profile);if(!source)return;
            root=new GameObject("ArchiveStage");root.transform.position=new Vector3(10000,-10000,10000);figure=new GameObject("Figure");figure.transform.SetParent(root.transform,false);
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true)){
                var g=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));g.layer=31;g.transform.SetParent(figure.transform,false);
                var mesh=Instantiate(filter.sharedMesh);meshes.Add(mesh);var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]=source.transform.InverseTransformPoint(filter.transform.TransformPoint(v[i]));mesh.vertices=v;mesh.RecalculateBounds();g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;
            }
            var bounds=figure.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            float h=bounds.size.y;figure.transform.localPosition-=new Vector3(bounds.center.x-root.transform.position.x,bounds.min.y-root.transform.position.y,bounds.center.z-root.transform.position.z);
            var cam=new GameObject("PortraitCamera",typeof(Camera));cam.transform.SetParent(root.transform,false);camera=cam.GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.05f);camera.nearClipPlane=.05f;camera.farClipPlane=15;camera.fieldOfView=32;camera.transform.localPosition=new Vector3(0,h*.58f,-h*2.1f);camera.transform.LookAt(root.transform.position+Vector3.up*h*.52f);
            texture=new RenderTexture(480,640,24);texture.Create();camera.targetTexture=texture;image.texture=texture;
            var aspect=image.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=.75f;
            for(int i=0;i<3;i++){var g=new GameObject("PortraitLight"+i,typeof(Light));g.transform.SetParent(root.transform,false);g.transform.localPosition=new Vector3(i==0?-h:h,h*.85f,i==1?h:-h);var l=g.GetComponent<Light>();l.type=LightType.Point;l.range=h*4;l.intensity=i==0?8:4f;l.color=i==1?new Color(.6f,.75f,1):new Color(1,.85f,.66f);l.cullingMask=1<<31;l.shadows=LightShadows.None;}
        }
        void Update(){if(figure)figure.transform.Rotate(0,Time.unscaledDeltaTime*8,0);}
        public void OnDrag(PointerEventData data){if(figure)figure.transform.Rotate(0,-data.delta.x*.5f,0);}
        void OnDestroy(){if(root)Destroy(root);if(texture){texture.Release();Destroy(texture);}foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
    }
}

