using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;
using Object=UnityEngine.Object;
namespace RedCliffMystery.EditorTools
{
    public static class FeedbackArchiveInstaller
    {
        const string Folder="Assets/Art/Characters/Feedback20261012";
        const string Log="Logs/FeedbackArchive";
        [MenuItem("적벽추리/피드백/인물 현재 상태 확인")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Log);var report=new StringBuilder();
            foreach(var m in Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include))
                foreach(var f in m.GetComponentsInChildren<MeshFilter>(true))report.AppendLine(m.Profile+" "+m.transform.parent.name+" "+AssetDatabase.GetAssetPath(f.sharedMesh));
            File.WriteAllText(Log+"/before.txt",report.ToString());
            Focus();
        }
        [MenuItem("적벽추리/피드백/장윤 자세 보기")]
        public static void Focus(){var m=Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include).Single(x=>x.Profile==4);var b=m.GetComponentInChildren<Renderer>().bounds;EditorWindow.GetWindow<SceneView>().LookAt(b.center,Quaternion.LookRotation(m.transform.forward+Vector3.down*.1f),1.45f,false,true);}
        [MenuItem("적벽추리/피드백/포즈와 대화 연출 적용")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.path!="Assets/Scenes/Investigation.unity"||scene.isDirty)throw new InvalidOperationException("Saved Investigation scene required");
            Directory.CreateDirectory(Log);if(!File.Exists(Log+"/before-feedback.unity"))File.Copy(scene.path,Log+"/before-feedback.unity");
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/Characters","Feedback20261012");
            var report=new StringBuilder();
            foreach(var motion in Object.FindObjectsByType<VarcoIdleMotion>(FindObjectsInactive.Include)){
                var visual=motion.transform;int index=0;
                foreach(var filter in motion.GetComponentsInChildren<MeshFilter>(true)){
                    string path=Folder+"/"+visual.parent.name+"_Rest"+index+++".asset";
                    var corrected=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(corrected==null){
                        var source=filter.sharedMesh;var mesh=Object.Instantiate(source);var vertices=mesh.vertices;
                        var points=vertices.Select(v=>visual.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
                        float floor=points.Min(p=>p.y),h=points.Max(p=>p.y)-floor,width=points.Max(p=>p.x)-points.Min(p=>p.x);int changed=0;
                        for(int i=0;i<points.Length;i++){
                            var p=points[i];float y=(p.y-floor)/h;float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.13f,.23f,Mathf.Abs(p.x)/h));
                            w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.47f,y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,.84f,y)));
                            float side=Mathf.Sign(p.x);var shoulder=new Vector3(side*h*.115f,floor+h*.77f,0);
                            if(w>0&&width/h>.43f){var q=shoulder+Quaternion.AngleAxis(-side*9,Vector3.forward)*(p-shoulder);p=Vector3.Lerp(p,q,w);changed++;}
                            vertices[i]=filter.transform.InverseTransformPoint(visual.TransformPoint(p));
                        }
                        mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(mesh,path);corrected=mesh;
                        report.AppendLine("PASS "+visual.parent.name+" RolePose source preserved; upper-arm adjustment vertices="+changed+" widthRatio="+width/h);
                    }
                    Undo.RecordObject(filter,"Apply resting arms");filter.sharedMesh=corrected;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                }
            }
            var session=Object.FindAnyObjectByType<InvestigationSession>();var presentation=session.GetComponent<ConversationPresentation>()??Undo.AddComponent<ConversationPresentation>(session.gameObject);Undo.RecordObject(presentation,"Bind conversation presentation");presentation.session=session;presentation.viewCamera=session.introductionStage.player.viewCamera;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Log+"/apply.txt",report+"COMPLETE failures=0\n");Focus();
        }
    }
}


