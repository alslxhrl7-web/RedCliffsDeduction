using UnityEngine;

namespace RedCliffMystery.Investigation
{
    // Visual time follows the investigation clock, including loaded saves; it never advances it.
    public sealed class NavalRiverAtmosphere : MonoBehaviour
    {
        public InvestigationSession session;
        public Camera playerCamera;
        public Material skyTemplate;
        public Transform[] boats;
        Material sky;
        Vector3[] boatPositions;
        Material oldSky;
        bool oldFog;
        FogMode oldMode;
        Color oldFogColor;
        float oldStart, oldEnd;
        CameraClearFlags oldClear;
        int slot = -1;

        void OnEnable()
        {
            oldSky=RenderSettings.skybox; oldFog=RenderSettings.fog; oldMode=RenderSettings.fogMode;
            oldFogColor=RenderSettings.fogColor;oldStart=RenderSettings.fogStartDistance;oldEnd=RenderSettings.fogEndDistance;
            if(playerCamera){oldClear=playerCamera.clearFlags;playerCamera.clearFlags=CameraClearFlags.Skybox;}
            if(skyTemplate){sky=new Material(skyTemplate);RenderSettings.skybox=sky;}
            boatPositions=new Vector3[boats.Length];
            for(int i=0;i<boats.Length;i++)if(boats[i])boatPositions[i]=boats[i].localPosition;
            slot=-1;ApplyTime(session?session.InvestigationTimeSlot:0);
        }
        void Update()
        {
            ApplyTime(session?session.InvestigationTimeSlot:0);
            for(int i=0;i<boats.Length;i++)if(boats[i]){
                boats[i].localPosition=boatPositions[i]+Vector3.up*(Mathf.Sin(Time.time*.55f+i*1.7f)*.035f);
            }
        }
        public void ApplyTime(int value)
        {
            value=Mathf.Clamp(value,0,3);if(slot==value)return;slot=value;
            Color haze=value==3?new Color(.065f,.085f,.12f):value==2?new Color(.44f,.32f,.29f):new Color(.44f,.54f,.58f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=28;RenderSettings.fogEndDistance=155;RenderSettings.fogColor=haze;
            if(sky){sky.SetColor("_SkyTint",value==2?new Color(.62f,.39f,.31f):new Color(.42f,.53f,.68f));
                sky.SetColor("_GroundColor",haze);sky.SetFloat("_Exposure",value==3?.12f:value==2?.65f:1.05f);}
        }
        void OnDisable()
        {
            RenderSettings.skybox=oldSky;RenderSettings.fog=oldFog;RenderSettings.fogMode=oldMode;
            RenderSettings.fogColor=oldFogColor;RenderSettings.fogStartDistance=oldStart;RenderSettings.fogEndDistance=oldEnd;
            if(playerCamera)playerCamera.clearFlags=oldClear;
            if(boatPositions!=null)for(int i=0;i<boats.Length;i++)if(boats[i])boats[i].localPosition=boatPositions[i];
            if(sky)Destroy(sky);
        }
    }
}
