using UnityEngine;
using System;
using System.Collections;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationRoute : MonoBehaviour
    {
        public FirstPersonInvestigator player;
        public Transform archiveArrival, navalArrival;
        public ArchiveCurtain curtain;
        public AudioClip curtainRustle;
        public bool animateDeparture=true;
        public bool connectedWalkway;
        public float campBoundaryZ=-15.2f;
        public bool IsTravelling {get;private set;}
        public bool AtNavalCamp {get;private set;}
        public bool IsReady => player!=null && archiveArrival!=null && navalArrival!=null;
        public bool OnOutdoorGround => connectedWalkway && curtain!=null ? player.transform.position.z<curtain.transform.position.z-.25f : AtNavalCamp;
        AudioSource fabricSource;
        Collider doorCollider;
        bool curtainOpen;
        bool outdoorWas;

        void Awake() { if(curtain!=null) doorCollider=curtain.transform.Find("ClosedDoor")?.GetComponent<Collider>(); }
        void Update()
        {
            if(!connectedWalkway || !IsReady || IsTravelling || !player.session.CanProcessInvestigationInput) return;
            var p=player.transform.position;
            if(Mathf.Abs(p.x)>10 || p.z>1 || p.z<-40) return;
            if(!curtainOpen && Mathf.Abs(p.x-curtain.transform.position.x)<1.7f && p.z<curtain.transform.position.z-.25f && p.z>curtain.transform.position.z-2.5f) DepartThroughCurtain(null);
            bool naval=AtNavalCamp;
            if(p.z<campBoundaryZ-.25f) naval=true;
            else if(p.z>campBoundaryZ+.25f) naval=false;
            if(naval!=AtNavalCamp || outdoorWas!=OnOutdoorGround) { AtNavalCamp=naval; outdoorWas=OnOutdoorGround; player.session.RefreshWalkLocation(); }
        }
        public void EnterArchive() { if(!IsReady) return; player.TeleportTo(archiveArrival); AtNavalCamp=false; }
        public void EnterNavalCamp() { if(!IsReady) return; player.TeleportTo(navalArrival); AtNavalCamp=true; }
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
