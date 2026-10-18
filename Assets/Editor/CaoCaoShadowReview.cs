using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class CaoCaoShadowReview {
 [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Inspect Shadow Lights")]
 public static void Inspect(){
 var lines=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.shadows!=LightShadows.None).Select(l=>l.name+" | "+l.type+" | "+l.transform.position+" | bias="+l.shadowBias+" normal="+l.shadowNormalBias+" range="+l.range+" active="+l.gameObject.activeInHierarchy+" | "+EditorJsonUtility.ToJson(l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>()));
 File.WriteAllLines("LocalArchive/CaoCaoHairEnemies20261016/Revision11/shadow-lights.txt",lines);
 }
 [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Fix Tent Shadow Bias")]
 public static void Fix(){
 if(EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
 var light=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(l=>l.name=="TentKey");
 var data=light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
 var backup="LocalArchive/CaoCaoHairEnemies20261016/Revision11/tent-light-before.json";
 if(!File.Exists(backup))File.WriteAllText(backup,EditorJsonUtility.ToJson(light,true)+"\n"+EditorJsonUtility.ToJson(data,true));
 Undo.RecordObjects(new Object[]{light,data},"Fix CaoCao tent self shadow acne");
 light.shadowBias=.5f;light.shadowNormalBias=.6f;data.usePipelineSettings=false;data.additionalLightsShadowResolutionTier=2;
 EditorUtility.SetDirty(light);EditorUtility.SetDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(light.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(light.gameObject.scene);
 }
}
