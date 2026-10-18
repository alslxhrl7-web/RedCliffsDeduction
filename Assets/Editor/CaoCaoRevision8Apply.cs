using System;using System.IO;using UnityEditor;using UnityEngine;
public static class CaoCaoRevision8Apply {
 [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Queue Apply R8")]
 public static void Queue(){string p=Path.Combine(Application.dataPath,"../LocalArchive/CaoCaoHairEnemies20261016/Revision8/apply-status.txt");File.WriteAllText(p,"QUEUED");EditorApplication.delayCall+=()=>{try{File.WriteAllText(p,"STARTED");CaoCaoFullReworkSetup.Apply();File.WriteAllText(p,"COMPLETE");}catch(Exception e){File.WriteAllText(p,"FAILED\n"+e);Debug.LogException(e);}};}
}




