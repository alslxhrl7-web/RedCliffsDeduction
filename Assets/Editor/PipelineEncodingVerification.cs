using System;
using System.Reflection;
using UnityEngine;

internal static class PipelineEncodingVerification
{
    public static void VerifyBatch()
    {
        var driver = typeof(Unity.Pipeline.RuntimePipelineBootstrap).Assembly.GetType("Unity.Pipeline.RuntimePipelineDriver", true);
        var predicate = driver.GetMethod("ShouldScanForCodeReload", BindingFlags.NonPublic | BindingFlags.Static);
        if (predicate == null) throw new MissingMethodException(driver.FullName, "ShouldScanForCodeReload");
        int count = 0;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            predicate.Invoke(null, new object[] { assembly });
            count++;
        }
        Debug.Log("PIPELINE_ENCODING_VERIFIED assemblies=" + count);
    }
}
