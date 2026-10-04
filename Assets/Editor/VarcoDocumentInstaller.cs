using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class VarcoDocumentInstaller
    {
        const string Root = "Assets/Art/Evidence/Varco/";
        static readonly string[] Names = { "MaintenanceOrder", "PersonnelRecord", "DutyRecord", "HouseholdRegistry", "SecretLetter" };
        [MenuItem("적벽추리/바르코 증거 문서 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Open Investigation first.");
            var objects = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var view = objects.Select(t=>t.GetComponent<InvestigationView>()).First(v=>v!=null);
            var preview = view.GetComponent<EvidenceDocumentView>() ?? Undo.AddComponent<EvidenceDocumentView>(view.gameObject);
            Undo.RecordObject(preview,"Apply evidence illustrations");
            preview.maintenanceOrder = Reference(Names[0]); preview.personnelRecord = Reference(Names[1]);
            preview.dutyRecord = Reference(Names[2]); preview.householdRegistry = Reference(Names[3]); preview.secretLetter = Reference(Names[4]);
            foreach (var name in Names.Take(3))
            {
                var placeholder = objects.Single(t=>t!=null && t.name==name);
                var table = placeholder.parent;
                var old = table.Find(name+"Visual"); if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);
                foreach(Transform t in table)
                    if(t==placeholder || t.name.StartsWith("Writing") || t.name=="CommandSeal")
                    { var renderer=t.GetComponent<Renderer>(); if(renderer!=null) { Undo.RecordObject(renderer,"Restore tabletop document marker"); renderer.enabled=true; } }
            }
            foreach(var name in Names)
            {
                AssetDatabase.DeleteAsset(Root+name+"/"+name+".prefab");
                AssetDatabase.DeleteAsset(Root+name+"/NaturalMaterial.mat");
                var importer=AssetImporter.GetAtPath(Root+name+"/Reference.png") as TextureImporter;
                if(importer!=null) { importer.maxTextureSize=2048; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.CompressedHQ; importer.SaveAndReimport(); }
            }
            AssetDatabase.DeleteAsset(Root+"HempCord.mat");
            AddTokenWave(objects.Single(t=>t!=null && t.name=="WoodenToken"));
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/EvidenceDocuments");
            File.WriteAllText("Logs/EvidenceDocuments/import.txt",DateTime.Now+"\n"+string.Join("\n",Names.Select(n=>n+" 2D image="+(Reference(n)!=null))));
            Debug.Log("EVIDENCE_DOCUMENTS_APPLIED");
        }
        static Texture2D Reference(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root+name+"/Reference.png");
        static void AddTokenWave(Transform token)
        {
            if(token.Find("WavePattern")!=null) return;
            var go=new GameObject("WavePattern"); go.transform.SetParent(token,false); Undo.RegisterCreatedObjectUndo(go,"Add token wave");
            var line=go.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=41; line.widthMultiplier=.025f;
            for(int i=0;i<41;i++) { float z=-.34f+i*.017f; line.SetPosition(i,new Vector3(.15f*Mathf.Sin(z*24),.515f,z)); }
            line.alignment=LineAlignment.TransformZ; go.transform.localRotation=Quaternion.Euler(90,0,0);
            // Line points are supplied in the rotated object's local plane.
            for(int i=0;i<41;i++) { float z=-.34f+i*.017f; line.SetPosition(i,new Vector3(.15f*Mathf.Sin(z*24),z,-.515f)); }
            string path=Root+"TokenInk.mat"; var ink=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(ink==null) { ink=new Material(Shader.Find("Universal Render Pipeline/Unlit")); ink.SetColor("_BaseColor",new Color(.16f,.10f,.05f)); AssetDatabase.CreateAsset(ink,path); }
            line.sharedMaterial=ink; line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows=false;
        }
    }
}

