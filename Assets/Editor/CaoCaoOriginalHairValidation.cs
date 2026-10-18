using System;
using System.IO;
using UnityEditor;
public static class CaoCaoOriginalHairValidation
{
    [MenuItem("Tools/RedCliffs/CaoCao Full Rework/Validate Original Hair Dependencies")]
    public static void Validate()
    {
        const string root="LocalArchive/CaoCaoHairEnemies20261016/Revision16/";
        var paths=AssetDatabase.GetDependencies(new[]{"Assets/Art/Characters/VarcoV3/CaoCao/CaoCaoVisual.prefab","Assets/Scenes/Investigation.unity"},true);
        File.WriteAllLines(root+"prefab-scene-dependencies.txt",paths);
        var bad=Array.FindAll(paths,p=>p.Contains("EnemiesHair")||p.Contains("HairCards_")||p.Contains("V3_GroomCards")||p.Contains("V3_HairUnderlay")||p.Contains("V3_BindingSilk"));
        File.WriteAllText(root+"dependency-check.txt",DateTime.UtcNow.ToString("O")+"\n"+(bad.Length==0?"PASS: prefab and scene have no retired Enemies hair dependencies":"FAIL\n"+string.Join("\n",bad)));
        if(bad.Length>0)throw new InvalidOperationException("Retired hair dependencies remain");
    }
}


