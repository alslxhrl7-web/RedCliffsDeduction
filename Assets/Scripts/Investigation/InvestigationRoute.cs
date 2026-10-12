using UnityEngine;
using System;
using System.Collections;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationRoute : MonoBehaviour
    {
        public FirstPersonInvestigator player;
        public Transform archiveArrival, navalArrival, supplyArrival;
        public ArchiveCurtain curtain;
        public AudioClip curtainRustle;
        public bool animateDeparture=true;
        public bool connectedWalkway;
        public bool connectedCampNetwork;
        public bool AtCommandTent => connectedCampNetwork && player != null && InArea(player.transform.position, 40, 0, 6.2f, 5);
        public bool OnConnectingRoad => connectedCampNetwork && player != null && !AtCommandTent && !AtSupplyTent && !AtNavalCamp && (Mathf.Abs(player.transform.position.x)>10 || player.transform.position.z<-6);
        public float campBoundaryZ=-15.2f;
        public bool IsTravelling {get;private set;}
        public bool AtNavalCamp {get;private set;}
        public bool AtSupplyTent {get;private set;}
        public bool IsReady => player!=null && archiveArrival!=null && navalArrival!=null;
        public bool SupplyTentReady => IsReady && supplyArrival!=null;
        public bool OnOutdoorGround => !AtCommandTent && (connectedCampNetwork && Mathf.Abs(player.transform.position.x)>10 || (connectedWalkway && curtain!=null ? player.transform.position.z<curtain.transform.position.z-.25f : AtNavalCamp));
        AudioSource fabricSource;
        Collider doorCollider;
        bool curtainOpen;
        bool outdoorWas;

        void Awake() { if(curtain!=null) doorCollider=curtain.transform.Find("ClosedDoor")?.GetComponent<Collider>(); }
        void Update()
        {
            if(!connectedWalkway || !IsReady || IsTravelling || !player.session.CanProcessInvestigationInput) return;
            var p=player.transform.position;
            if(connectedCampNetwork) RefreshPhysicalLocation();
            if(Mathf.Abs(p.x)>10 || p.z>1 || p.z<-40) return;
            if(!curtainOpen && Mathf.Abs(p.x-curtain.transform.position.x)<1.7f && p.z<curtain.transform.position.z-.25f && p.z>curtain.transform.position.z-2.5f) DepartThroughCurtain(null);
            if(connectedCampNetwork) return;
            bool naval=AtNavalCamp;
            if(p.z<campBoundaryZ-.25f) naval=true;
            else if(p.z>campBoundaryZ+.25f) naval=false;
            if(naval!=AtNavalCamp || outdoorWas!=OnOutdoorGround) { AtNavalCamp=naval; outdoorWas=OnOutdoorGround; player.session.RefreshWalkLocation(); }
        }
        static bool InArea(Vector3 p,float x,float z,float halfX,float halfZ) => Mathf.Abs(p.x-x)<halfX && Mathf.Abs(p.z-z)<halfZ;
        bool commandWas, roadWas;
        public void RefreshPhysicalLocation()
        {
            if(!connectedCampNetwork || player==null) return;
            var p=player.transform.position;
            bool supply=InArea(p,-40,-23.5f,9.5f,9.5f);
            bool naval=InArea(p,0,-23.5f,9.5f,8);
            bool changed=supply!=AtSupplyTent || naval!=AtNavalCamp || commandWas!=AtCommandTent;
            AtSupplyTent=supply; AtNavalCamp=naval;
            changed|=roadWas!=OnConnectingRoad || outdoorWas!=OnOutdoorGround;
            commandWas=AtCommandTent; roadWas=OnConnectingRoad; outdoorWas=OnOutdoorGround;
            if(changed && player.session!=null) player.session.RefreshWalkLocation();
        }
        public void EnterArchive() { if(!IsReady) return; player.TeleportTo(archiveArrival); AtNavalCamp=false; AtSupplyTent=false; }
        public void EnterNavalCamp() { if(!IsReady) return; player.TeleportTo(navalArrival); AtNavalCamp=true; AtSupplyTent=false; }
        // 보급 천막은 수군 진영과 별도 구역이다. 문서고 입구에서 직접 가지 않고 수군 진영 안에서만 이동한다.
        public void EnterSupplyTent() { if(!SupplyTentReady) return; player.TeleportTo(supplyArrival); AtNavalCamp=false; AtSupplyTent=true; }
        public void DepartThroughCurtain(Action complete)
        {
            if(!IsReady || IsTravelling) return;
            if(curtainOpen) { complete?.Invoke(); return; }
            IsTravelling=true; StartCoroutine(OpenCurtain(complete));
        }
        IEnumerator OpenCurtain(Action complete)
        {
            var go=new GameObject("CanvasRustleSource"); go.transform.SetParent(curtain.transform,false);
            fabricSource=go.AddComponent<AudioSource>(); fabricSource.playOnAwake=false; fabricSource.spatialBlend=.7f;
            fabricSource.minDistance=1; fabricSource.maxDistance=8; fabricSource.clip=curtainRustle;
            fabricSource.volume=.28f*InvestigationAudioSettings.Effects; fabricSource.Play();
            float elapsed=0;
            while(elapsed<.65f) {
                if(!Application.isFocused && !Application.isEditor) { yield return null; continue; }
                elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f); curtain.SetOpening(Mathf.SmoothStep(0,1,elapsed/.65f)); yield return null;
            }
            curtainOpen=true; curtain.SetOpening(1);
            if(doorCollider!=null) doorCollider.enabled=false;
            if(fabricSource!=null) Destroy(fabricSource.gameObject,.4f);
            IsTravelling=false; complete?.Invoke();
        }
        public void BeginReturnWalk()
        {
            if(!IsReady) return;
            Vector3 direction=curtain.transform.position-player.transform.position; direction.y=0;
            if(direction.sqrMagnitude>.01f) player.transform.rotation=Quaternion.LookRotation(direction);
            curtainOpen=true; if(curtain!=null) curtain.SetOpening(1);
            if(doorCollider!=null) doorCollider.enabled=false;
        }
        void OnDisable()
        {
            if(!IsTravelling) return;
            StopAllCoroutines(); IsTravelling=false;
            if(fabricSource!=null) Destroy(fabricSource.gameObject);
            if(curtain!=null) curtain.SetOpening(curtainOpen?1:0);
        }
    }
}
