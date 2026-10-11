using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class CommandTentBuilder
    {
        [MenuItem("적벽추리/지휘 천막 도입부 추가")]
        public static void AddToInvestigation()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save current scene changes first.");
            var scene = EditorSceneManager.OpenScene(FirstInvestigationBuilder.ScenePath);
            var session = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            if (session.introductionStage != null) return;
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonInvestigator>();
            var stageGo = new GameObject("IntroductionStage");
            var stage = stageGo.AddComponent<IntroductionStage>();
            stage.player = player;
            stage.archiveSpawn = new GameObject("ArchiveArrival").transform;
            stage.archiveSpawn.SetParent(stageGo.transform);
            stage.archiveSpawn.SetPositionAndRotation(player.transform.position,player.transform.rotation);
            stage.tentSpawn = new GameObject("AudiencePosition").transform;
            stage.tentSpawn.SetParent(stageGo.transform);
            stage.tentSpawn.position = new Vector3(40,.08f,-3.4f);
            stage.tent = new GameObject("CommandTent");
            var root = stage.tent.transform;
            root.SetParent(stageGo.transform);
            var canvas = Mat("TentCanvas",new Color(.46f,.36f,.24f));
            var beam = Mat("TentTimber",new Color(.16f,.09f,.045f));
            var red = Mat("CommandCrimson",new Color(.34f,.045f,.055f));
            var armour = Mat("CaoCaoArmour",new Color(.075f,.11f,.14f));
            var gold = Mat("CommandGold",new Color(.64f,.44f,.16f));
            var skin = Mat("CaoCaoFace",new Color(.62f,.43f,.29f));
            var black = Mat("CaoCaoHair",new Color(.035f,.028f,.025f));
            var map = Mat("StrategyMap",new Color(.68f,.57f,.37f));
            Box("TentFloor",new Vector3(40,-.15f,0),new Vector3(12,.3f,12),beam,root);
            Box("AudienceCarpet",new Vector3(41.4f,.02f,-.5f),new Vector3(3.1f,.025f,8),red,root);
            Box("BackCanvas",new Vector3(40,1.9f,4.5f),new Vector3(12,3.8f,.15f),canvas,root);
            Box("LeftCanvas",new Vector3(34.1f,1.9f,0),new Vector3(.15f,3.8f,9),canvas,root);
            Box("RightCanvas",new Vector3(45.9f,1.9f,0),new Vector3(.15f,3.8f,9),canvas,root);
            Box("RearEntrance",new Vector3(40,1.9f,-4.5f),new Vector3(12,3.8f,.15f),canvas,root);
            var roofLeft=Box("SlopingRoofLeft",new Vector3(37,4.2f,0),new Vector3(6.4f,.15f,9),canvas,root);
            roofLeft.transform.rotation=Quaternion.Euler(0,0,17);
            var roofRight=Box("SlopingRoofRight",new Vector3(43,4.2f,0),new Vector3(6.4f,.15f,9),canvas,root);
            roofRight.transform.rotation=Quaternion.Euler(0,0,-17);
            Box("RidgeBeam",new Vector3(40,5.05f,0),new Vector3(.18f,.18f,9),beam,root);
            foreach(float x in new[]{35f,40f,45f})
                Box("TentPost",new Vector3(x,1.9f,4.2f),new Vector3(.18f,3.8f,.18f),beam,root);
            foreach(float x in new[]{39.4f,43.3f})
            {
                Box("StandardPole",new Vector3(x,1.5f,2.8f),new Vector3(.07f,3,.07f),gold,root);
                Box("CrimsonStandard",new Vector3(x,2.28f,2.8f),new Vector3(.9f,1.3f,.06f),red,root);
                Box("StandardStripe",new Vector3(x,2.28f,2.75f),new Vector3(.1f,1.1f,.025f),gold,root);
            }
            Box("CommandDesk",new Vector3(41.4f,.83f,-.35f),new Vector3(2.9f,.14f,1.25f),beam,root);
            foreach(float x in new[]{40.15f,42.65f}) foreach(float z in new[]{-.83f,.13f})
                Box("DeskLeg",new Vector3(x,.4f,z),new Vector3(.14f,.8f,.14f),beam,root);
            Box("CampaignMap",new Vector3(41.4f,.915f,-.35f),new Vector3(1.8f,.025f,.9f),map,root);
            foreach(float x in new[]{40.9f,41.5f,41.9f})
                Box("MapMarker",new Vector3(x,.97f,-.35f),new Vector3(.09f,.1f,.09f),red,root);
            var cao = new GameObject("CaoCao").transform;
            cao.SetParent(root);
            Box("CommanderRobe",new Vector3(41.4f,.9f,.95f),new Vector3(.72f,1.4f,.48f),red,cao);
            Box("ArmourChest",new Vector3(41.4f,1.33f,.67f),new Vector3(.76f,.58f,.16f),armour,cao);
            Box("Belt",new Vector3(41.4f,.98f,.64f),new Vector3(.77f,.10f,.07f),gold,cao);
            foreach(float x in new[]{40.92f,41.88f})
            {
                Box("WideSleeve",new Vector3(x,1.14f,.92f),new Vector3(.28f,.68f,.44f),red,cao);
                Box("ShoulderArmour",new Vector3(x,1.48f,.9f),new Vector3(.35f,.15f,.52f),armour,cao);
            }
            Shape("Head",PrimitiveType.Sphere,new Vector3(41.4f,1.84f,.87f),new Vector3(.43f,.50f,.42f),skin,cao);
            Box("Crown",new Vector3(41.4f,2.14f,.92f),new Vector3(.43f,.24f,.38f),black,cao);
            Box("CrownBand",new Vector3(41.4f,2.035f,.70f),new Vector3(.44f,.065f,.025f),gold,cao);
            Box("Beard",new Vector3(41.4f,1.59f,.64f),new Vector3(.22f,.38f,.075f),black,cao);
            foreach(float x in new[]{41.30f,41.50f})
                Box("Eye",new Vector3(x,1.88f,.671f),new Vector3(.075f,.027f,.025f),black,cao);
            foreach(var p in new[]{new Vector3(39,2.7f,-1.3f),new Vector3(43.3f,2.7f,.2f)})
            {
                var lamp = new GameObject("TentLantern",typeof(Light)); lamp.transform.SetParent(root); lamp.transform.position=p;
                var light=lamp.GetComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.77f,.48f); light.intensity=4; light.range=9;
            }
            session.introductionStage=stage;
            player.transform.SetPositionAndRotation(stage.tentSpawn.position,stage.tentSpawn.rotation);
            session.view.SetPrologueLayout(true);
            session.view.SetPlayerIdentity("알현 대기","조조군 지휘 천막");
            session.view.SetObjective("조조의 부름에 응하라");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static Material Mat(string name,Color color)
        {
            string path="Assets/Art/PrototypeMaterials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat!=null)return mat;
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=color; mat.SetFloat("_Smoothness",.12f);
            AssetDatabase.CreateAsset(mat,path); return mat;
        }
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material mat,Transform root)=>Shape(name,PrimitiveType.Cube,position,scale,mat,root);
        static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material mat,Transform root)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(root);
            go.transform.position=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat; return go;
        }
    }
}
