using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// One-shot, targeted repair. Never enters Play Mode or builds a player.
[InitializeOnLoad]
public static class UrpShaderRepair
{
    const string Request = "Library/UrpShaderRepair.request";
    const string Report = "Docs/URP 셰이더 복구 결과 2026-10-04.md";
    static double verifyAfter;

    static UrpShaderRepair() { EditorApplication.delayCall += RunRequestedRepair; }

    static void RunRequestedRepair()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) {
            EditorApplication.delayCall += RunRequestedRepair;
            return;
        }
        File.Move(Request, Request + ".handled-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"));
        try {
            var flags = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;
            AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal-config/Runtime/ShaderConfig.cs.hlsl", flags);
            AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl", flags);
            AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", flags);
            verifyAfter = EditorApplication.timeSinceStartup + 3;
            EditorApplication.update += Verify;
        } catch (Exception e) { WriteReport("복구 실패", e.ToString()); }
    }

    static void Verify()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < verifyAfter) return;
        EditorApplication.update -= Verify;
        try {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
            if (shader == null) { WriteReport("확인 실패", "URP/Lit 셰이더를 로드하지 못했습니다."); return; }
            var messages = ShaderUtil.GetShaderMessages(shader);
            var details = new StringBuilder();
            int errors = 0;
            foreach (var message in messages) {
                if (message.severity.ToString() == "Error") errors++;
                details.AppendLine(message.severity + ": " + message.message);
            }
            WriteReport(errors == 0 ? "재가져오기 후 현재 셰이더 오류 0건" : "셰이더 오류 " + errors + "건 남음",
                details.Length == 0 ? "현재 셰이더 메시지가 없습니다." : details.ToString());
        } catch (Exception e) { WriteReport("확인 실패", e.ToString()); }
    }

    static void WriteReport(string result, string detail)
    {
        File.WriteAllText(Report, "# URP 셰이더 복구 결과\n\n"
            + "편집기 기록 시각: " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "\n\n"
            + "프로젝트: " + Directory.GetParent(Application.dataPath).FullName + "\n\n"
            + "결과: " + result + "\n\n"
            + "ShaderConfig.cs.hlsl, Fog.hlsl, Lit.shader만 강제 재가져오기했습니다. 전체 Library 삭제, Play, 빌드는 수행하지 않았습니다.\n\n"
            + "```text\n" + detail + "\n```\n\n"
            + "이 결과는 편집기 재가져오기 직후의 ShaderUtil 메시지 확인입니다. 플레이 화면과 모든 셰이더 변형 검증을 의미하지 않습니다.\n", new UTF8Encoding(false));
        Debug.Log("URP_SHADER_REPAIR: " + result + " · " + Report);
    }
}
