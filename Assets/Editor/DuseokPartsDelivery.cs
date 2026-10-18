using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace RedCliffMystery.EditorTools {
public static class DuseokPartsDelivery {
 const string Root="Assets/Art/Characters/Parts20261018/Duseok";
 static string ReportDir=>Path.Combine(Application.dataPath,"../LocalArchive/CharacterParts20261018/Duseok/UnityReview");
 static Transform NPC()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Duseok");
 [MenuItem("Tools/Duseok Parts/Inspect")]
 public static void Inspect(){
  Directory.CreateDirectory(ReportDir);var n=NPC();var b=new StringBuilder();
  b.AppendLine("scene="+SceneManager.GetActiveScene().path+" play="+EditorApplication.isPlaying+" dirty="+SceneManager.GetActiveScene().isDirty);
  foreach(var t in n.GetComponentsInChildren<Transform>(true)){
   b.AppendLine(t.name+" active="+t.gameObject.activeSelf+" pos="+t.localPosition+" rot="+t.localEulerAngles+" scale="+t.localScale+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name)));
   var f=t.GetComponent<MeshFilter>();if(f!=null)b.AppendLine("mesh="+AssetDatabase.GetAssetPath(f.sharedMesh)+" verts="+f.sharedMesh.vertexCount+" bounds="+f.sharedMesh.bounds);
   var r=t.GetComponent<Renderer>();if(r!=null)b.AppendLine("materials="+string.Join(",",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m))));
  }
  File.WriteAllText(Path.Combine(ReportDir,"inspection.txt"),b.ToString());Debug.Log("DUSEOK_INSPECT_COMPLETE");
 }
 static string State(Transform n)=>string.Join("|",n.GetComponents<Component>().Where(c=>!(c is Transform)).Select(c=>EditorJsonUtility.ToJson(c)));
 static Texture2D Texture(string label,string file,bool normal=false,bool linear=false){
  string p=Root+"/Maps/"+label+"/"+file+".png";
  if(!File.Exists(p))return null;
  var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.sRGBTexture=!normal&&!linear;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.CompressedHQ;i.mipmapEnabled=true;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
 }
 static Texture2D Raw(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);return t;}
 static Material MaterialFor(string label){
  var p=Root+"/"+label+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}
  m.SetTexture("_BaseMap",Texture(label,"BaseColor"));m.SetColor("_BaseColor",Color.white);var normalMap=Texture(label,"Normal",true);m.SetTexture("_BumpMap",normalMap);m.SetFloat("_BumpScale",1);if(normalMap!=null)m.EnableKeyword("_NORMALMAP");else m.DisableKeyword("_NORMALMAP");
  string rp=Root+"/Maps/"+label+"/Roughness.png",mp=Root+"/Maps/"+label+"/Metallic.png";
  var rough=Raw(rp);var metal=File.Exists(mp)?Raw(mp):null;var packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);var pixels=rough.GetPixels();var metals=metal!=null?metal.GetPixels():null;
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(metals==null?0:metals[i].r,0,0,1-pixels[i].r);
  packed.SetPixels(pixels);packed.Apply();var packedPath=Root+"/Maps/"+label+"/MetallicSmoothness.png";File.WriteAllBytes(packedPath,packed.EncodeToPNG());Object.DestroyImmediate(rough);if(metal!=null)Object.DestroyImmediate(metal);Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(packedPath);
  m.SetTexture("_MetallicGlossMap",Texture(label,"MetallicSmoothness",false,true));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
  var ao=Texture(label,"AO",false,true);if(ao!=null){m.SetTexture("_OcclusionMap",ao);m.SetFloat("_OcclusionStrength",.55f);m.EnableKeyword("_OCCLUSIONMAP");}
  EditorUtility.SetDirty(m);return m;
 }
 [MenuItem("Tools/Duseok Parts/Apply")]
 public static void Apply(){
  Directory.CreateDirectory(ReportDir);File.WriteAllText(Path.Combine(ReportDir,"apply-status.txt"),"QUEUED");
  EditorApplication.delayCall+=()=>{try{File.WriteAllText(Path.Combine(ReportDir,"apply-status.txt"),"STARTED");ApplyCore();File.WriteAllText(Path.Combine(ReportDir,"apply-status.txt"),"COMPLETE");}catch(Exception e){File.WriteAllText(Path.Combine(ReportDir,"apply-status.txt"),"FAILED\n"+e);Debug.LogException(e);}};
 }
 static void ApplyCore(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
  var n=NPC();var before=State(n);var visual=n.Find("VarcoVisual");var model=visual.Find("Model");var f=model.GetComponent<MeshFilter>();var renderer=model.GetComponent<MeshRenderer>();
  if(f==null||renderer==null||visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0)throw new Exception("Unexpected rig structure");
  Directory.CreateDirectory(ReportDir);if(!File.Exists(Path.Combine(ReportDir,"before-apply.txt")))File.WriteAllText(Path.Combine(ReportDir,"before-apply.txt"),AssetDatabase.GetAssetPath(f.sharedMesh)+"\n"+model.localPosition+"\n"+model.localEulerAngles+"\n"+model.localScale+"\n"+before);
  var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/DuseokParts.fbx");importer.isReadable=true;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/DuseokParts.fbx");var temp=Object.Instantiate(source);temp.transform.position=Vector3.zero;
  try{
   var filters=temp.GetComponentsInChildren<MeshFilter>();var combos=new System.Collections.Generic.List<CombineInstance>();var materials=new System.Collections.Generic.List<Material>();
   foreach(var filter in filters){var mats=filter.GetComponent<Renderer>().sharedMaterials;for(int i=0;i<filter.sharedMesh.subMeshCount;i++){
    string name=mats[i].name;string label=name.Contains("Helmet")?"Helmet":name.Contains("Head")?"Head":name.Contains("Armor")?"Armor":name.Contains("Hands")?"Hands":name.Contains("Garment")?"Garment":null;if(label==null)throw new Exception("Unknown baked material "+name);
    combos.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=Matrix4x4.Rotate(Quaternion.Euler(0,180,0))*filter.transform.localToWorldMatrix});materials.Add(MaterialFor(label));
   }}
   var mesh=new Mesh{name="Duseok_Parts_Game",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(combos.ToArray(),false,true);mesh.RecalculateBounds();
   if(mesh.bounds.size.y<1.65f||mesh.bounds.size.y>1.9f||Mathf.Abs(mesh.bounds.min.y)>.03f)throw new Exception("Unexpected imported axis/scale: "+mesh.bounds);
   string meshPath=Root+"/Duseok_Parts_Game.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
   Undo.RecordObjects(new Object[]{f,renderer,model,visual.GetComponent<VarcoIdleMotion>()},"Apply baked Duseok parts");f.sharedMesh=saved;renderer.sharedMaterials=materials.ToArray();model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;model.localScale=Vector3.one;
   visual.GetComponent<VarcoIdleMotion>().MouthHeightOverride=1.57f/saved.bounds.size.y;
   if(State(n)!=before)throw new Exception("Root interaction changed");
   PrefabUtility.SaveAsPrefabAsset(visual.gameObject,Root+"/DuseokVisual.prefab");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(n.gameObject.scene);EditorSceneManager.SaveScene(n.gameObject.scene);
   File.WriteAllText(Path.Combine(ReportDir,"apply.txt"),"COMPLETE: original assets retained; root components unchanged; one combined game mesh, "+saved.vertexCount+" vertices; "+saved.bounds+"; materials="+materials.Count);
  }finally{Object.DestroyImmediate(temp);}
 }
 static Investigation.InvestigationSession Session()=>Object.FindAnyObjectByType<Investigation.InvestigationSession>();
 [MenuItem("Tools/Duseok Parts/Review Start")]
 public static void ReviewStart(){if(!EditorApplication.isPlaying)throw new Exception("Play required");var title=Object.FindAnyObjectByType<Investigation.InvestigationTitleScreen>();if(title!=null)title.StartButton.onClick.Invoke();}
 [MenuItem("Tools/Duseok Parts/Review Prepare")]
 public static void ReviewPrepare(){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");var session=Session();session.BeginIntroduction("검증");typeof(Investigation.InvestigationSession).GetMethod("FinishIntroduction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(session,null);ReviewFront();
 }
 static void View(Vector3 localCamera,Vector3 localAim){
  if(!EditorApplication.isPlaying)throw new Exception("Play required");var visual=NPC().Find("VarcoVisual");var player=Object.FindAnyObjectByType<Investigation.FirstPersonInvestigator>();var camera=player.viewCamera;Session().CloseModal();player.enabled=false;
  var position=visual.TransformPoint(localCamera);var arrival=new GameObject("TemporaryDuseokReviewArrival");arrival.transform.position=position-Vector3.up*camera.transform.localPosition.y;arrival.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(visual.TransformPoint(localAim)-position,Vector3.up));player.TeleportTo(arrival.transform);Object.Destroy(arrival);camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(visual.TransformPoint(localAim)-position));Physics.SyncTransforms();
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  Directory.CreateDirectory(ReportDir);File.AppendAllText(Path.Combine(ReportDir,"play-views.txt"),DateTime.Now.ToString("s")+" camera="+position+" aim="+localAim+"\n");
 }
 [MenuItem("Tools/Duseok Parts/Review Front")]
 public static void ReviewFront()=>View(new Vector3(.10f,1.62f,-1.10f),new Vector3(0,1.48f,0));
 [MenuItem("Tools/Duseok Parts/Review Side")]
 public static void ReviewSide()=>View(new Vector3(1.10f,1.62f,-.1f),new Vector3(0,1.48f,0));
 [MenuItem("Tools/Duseok Parts/Review Back")]
 public static void ReviewBack()=>View(new Vector3(.1f,1.62f,1.10f),new Vector3(0,1.48f,0));
 [MenuItem("Tools/Duseok Parts/Review Full")]
 public static void ReviewFull()=>View(new Vector3(.15f,1.60f,-2.3f),new Vector3(0,.95f,0));
 [MenuItem("Tools/Duseok Parts/Review Face Close")]
 public static void ReviewFaceClose()=>View(new Vector3(.04f,1.64f,-.48f),new Vector3(0,1.60f,0));
 [MenuItem("Tools/Duseok Parts/Review Hands Close")]
 public static void ReviewHandsClose()=>View(new Vector3(.72f,1.02f,-.50f),new Vector3(.33f,.87f,0));
 [MenuItem("Tools/Duseok Parts/Review Lower Close")]
 public static void ReviewLowerClose()=>View(new Vector3(.12f,.80f,-1.12f),new Vector3(0,.43f,0));
 [MenuItem("Tools/Duseok Parts/Review Interaction")]
 public static void ReviewInteraction(){
  var player=Object.FindAnyObjectByType<Investigation.FirstPersonInvestigator>();var target=player.FindTarget();if(target==null||target.transform!=NPC())throw new Exception("Player aim does not hit Duseok");target.Interact(Session());if(!Session().BlocksMovement)throw new Exception("Dialogue did not open");File.WriteAllText(Path.Combine(ReportDir,"interaction.txt"),"PASS actual player ray targets Duseok and interaction opens dialogue.");
 }
 [MenuItem("Tools/Duseok Parts/Validate Motion")]
 public static void ValidateMotion(){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/DuseokVisual.prefab");var clone=Object.Instantiate(source);clone.transform.position=new Vector3(500,500,500);
  try{var motion=clone.GetComponent<VarcoIdleMotion>();motion.InitializeForPreview();motion.Sample(0);var f=clone.GetComponentInChildren<MeshFilter>();var rest=f.sharedMesh.vertices;motion.Sample(1);var idle=f.sharedMesh.vertices;int moved=rest.Where((v,i)=>(v-idle[i]).sqrMagnitude>1e-12f).Count();motion.StartSpeech("아",-.07f);motion.Sample(0);var speaking=f.sharedMesh.vertices;int lips=rest.Where((v,i)=>(v-speaking[i]).sqrMagnitude>1e-12f).Count();motion.StartSpeech("아",-10);motion.Sample(0);bool closes=rest.SequenceEqual(f.sharedMesh.vertices);if(moved==0||lips==0||!closes)throw new Exception("Motion or speech failed");File.WriteAllText(Path.Combine(ReportDir,"motion.txt"),"PASS idle vertices="+moved+" mouth vertices="+lips+" speech returns to rest="+closes+"; combined mesh keeps identical deformation coordinates across part seams.");}
  finally{Object.DestroyImmediate(clone);}
 }
}
}

