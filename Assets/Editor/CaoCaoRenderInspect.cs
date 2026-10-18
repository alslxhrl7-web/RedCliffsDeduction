using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
public static class CaoCaoRenderInspect {
[MenuItem("Tools/RedCliffs/CaoCao Full Rework/Inspect Rendering")]
public static void Inspect(){var c=Camera.main;var u=c?c.GetComponent<UniversalAdditionalCameraData>():null;var a=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;File.WriteAllText("LocalArchive/CaoCaoHairEnemies20261016/Revision7/render-settings.txt","Asset="+(a?AssetDatabase.GetAssetPath(a):"none")+"\nMSAA="+(a?a.msaaSampleCount:0)+"\nCamera="+(c?c.name:"none")+"\nCameraAA="+(u?u.antialiasing.ToString():"none")+"\nAllowMSAA="+(c&&c.allowMSAA));}
}
