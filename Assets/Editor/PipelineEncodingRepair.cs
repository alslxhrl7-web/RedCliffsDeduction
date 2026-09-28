using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

internal static class PipelineEncodingRepair
{
    static EmbedRequest request;

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        EditorApplication.delayCall += Begin;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Begin();
        };
    }

    static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || request != null
            || Directory.Exists(Path.Combine(Application.dataPath, "../Packages/com.unity.pipeline"))) return;
        request = Client.Embed("com.unity.pipeline");
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (!request.IsCompleted) return;
        EditorApplication.update -= Poll;
        if (request.Status == StatusCode.Failure) Debug.LogError(request.Error.message);
        else Debug.Log("Pipeline embedded for encoding repair.");
    }
}
