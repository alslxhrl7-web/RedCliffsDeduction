using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RedCliffMystery;
public static class CaoCaoMotionEquivalence
{
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Validate Cached Motion")]
    public static void Validate()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play required");
        var mo=UnityEngine.Object.FindObjectsByType<VarcoIdleMotion>().First(m=>m.Profile==0);
        if(!mo.DisableLipMotion)throw new InvalidOperationException("No-lip profile required");
        var surfaces=(Array)typeof(VarcoIdleMotion).GetField("surfaces",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mo);
        float error=0;int checkedVertices=0;
        foreach(float time in new[]{0f,.3f,1f,1.7f})
        {
            mo.Sample(time);float phase=mo.Profile*1.37f;float breath=Mathf.Sin(time*(1.35f+mo.Profile*.035f)+phase),sway=Mathf.Sin(time*.61f+phase);
            foreach(object surface in surfaces)
            {
                var type=surface.GetType();var f=(MeshFilter)type.GetField("Filter").GetValue(surface);var rest=(Vector3[])type.GetField("Rest").GetValue(surface);var weight=(float[])type.GetField("Weight").GetValue(surface);var actual=f.sharedMesh.vertices;
                var a=mo.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;var b=f.transform.worldToLocalMatrix*mo.transform.localToWorldMatrix;
                for(int i=0;i<rest.Length;i++)
                {
                    var p=a.MultiplyPoint3x4(rest[i]);float w=weight[i];p.x*=1+breath*mo.Amplitude*w;p.z*=1+breath*mo.Amplitude*1.5f*w;p.x+=sway*mo.Amplitude*.45f*w;p.y+=breath*mo.Amplitude*.2f*w;
                    error=Mathf.Max(error,f.transform.TransformVector(actual[i]-b.MultiplyPoint3x4(p)).magnitude);checkedVertices++;
                }
            }
        }
        mo.Sample(Time.time);File.WriteAllText("LocalArchive/CaoCaoHairEnemies20261016/Revision16/motion-equivalence.txt","Compared vertices="+checkedVertices+"\nMax world difference metres="+error+"\n"+(error<.000001f?"PASS":"FAIL"));
        if(error>=.000001f)throw new InvalidOperationException("Cached motion differs");
    }
}

