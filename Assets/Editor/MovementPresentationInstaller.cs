using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class MovementPresentationInstaller
    {
        const string Root="Assets/Audio/Movement";
        public static void RefineFootsteps()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for(int i=0;i<3;i++) { MakeWave(Root+"/WoodStep"+i+".wav",false,false,30+i); MakeWave(Root+"/EarthStep"+i+".wav",false,true,50+i); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("FOOTSTEP_REFINEMENT_APPLIED");
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply outside Play Mode.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/Investigation.unity") throw new InvalidOperationException("Open Investigation scene.");
            Directory.CreateDirectory(Root);
            for(int i=0;i<3;i++) { MakeWave(Root+"/WoodStep"+i+".wav",false,false,30+i); MakeWave(Root+"/EarthStep"+i+".wav",false,true,50+i); }
            MakeWave(Root+"/CanvasRustle.wav",true,false,70);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(Root,"*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono=true;
                var settings=importer.defaultSampleSettings; settings.preloadAudioData=true; settings.loadType=AudioClipLoadType.DecompressOnLoad; settings.compressionFormat=AudioCompressionFormat.PCM;
                importer.defaultSampleSettings=settings; importer.SaveAndReimport();
            }
            var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonInvestigator>();
            var route=UnityEngine.Object.FindAnyObjectByType<InvestigationRoute>();
            var foot=player.GetComponent<DistanceFootsteps>() ?? Undo.AddComponent<DistanceFootsteps>(player.gameObject);
            Undo.RecordObject(foot,"Add distance footsteps");
            foot.woodenSteps=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/WoodStep"+i+".wav")).ToArray();
            foot.earthSteps=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/EarthStep"+i+".wav")).ToArray();
            foot.metresPerStep=1.2f;
            var doorway=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="SouthDoorway");
            var wall=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="SouthWall" && t.parent.name=="ArchiveRoom");
            var wallRenderer=wall.GetComponent<Renderer>(); var bounds=wallRenderer.bounds;
            var wallMaterial=wallRenderer.sharedMaterial;
            Undo.RecordObject(wallRenderer,"Open actual curtain doorway"); wallRenderer.enabled=false;
            var wallCollider=wall.GetComponent<Collider>(); Undo.RecordObject(wallCollider,"Open actual curtain doorway"); wallCollider.enabled=false;
            foreach(var support in wall.parent.GetComponentsInChildren<Transform>().Where(t=>t.name=="Pillar" && Mathf.Abs(t.position.x-doorway.position.x)<1.11f && Mathf.Abs(t.position.z-doorway.position.z)<.5f))
            { Undo.RecordObject(support,"Move support beside curtain passage"); support.position=new Vector3(doorway.position.x-1.45f,support.position.y,support.position.z); }
            float left=doorway.position.x-1.11f, right=doorway.position.x+1.11f, top=doorway.position.y+2.7f;
            Piece("SouthWallLeftOfCurtain",wall.parent,new Vector3((bounds.min.x+left)*.5f,bounds.center.y,bounds.center.z),new Vector3(left-bounds.min.x,bounds.size.y,bounds.size.z),wallMaterial);
            Piece("SouthWallRightOfCurtain",wall.parent,new Vector3((bounds.max.x+right)*.5f,bounds.center.y,bounds.center.z),new Vector3(bounds.max.x-right,bounds.size.y,bounds.size.z),wallMaterial);
            Piece("SouthWallAboveCurtain",wall.parent,new Vector3(doorway.position.x,(top+bounds.max.y)*.5f,bounds.center.z),new Vector3(2.22f,bounds.max.y-top,bounds.size.z),wallMaterial);
            var earth=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Ground" && t.parent.name=="NavalCamp").GetComponent<Renderer>().sharedMaterial;
            Piece("ArchiveExteriorApron",wall.parent,new Vector3(doorway.position.x,-.08f,doorway.position.z-5),new Vector3(8,.16f,10),earth);
            var timber=doorway.Find("LeftFrame").GetComponent<Renderer>().sharedMaterial;
            foreach(int side in new[]{-1,1}) Piece("ArchiveExteriorPost"+side,wall.parent,new Vector3(doorway.position.x+side*2.4f,1.1f,doorway.position.z-3.4f),new Vector3(.13f,2.2f,.13f),timber);
            string lanternPath=Root+"/ExitLanternGlow.mat";
            var glow=AssetDatabase.LoadAssetAtPath<Material>(lanternPath);
            if(glow==null) { glow=new Material(Shader.Find("Universal Render Pipeline/Unlit")); glow.SetColor("_BaseColor",new Color(1,.61f,.25f)); AssetDatabase.CreateAsset(glow,lanternPath); }
            foreach(int side in new[]{-1,1})
            {
                Vector3 position=new Vector3(doorway.position.x+side*2.4f,2.16f,doorway.position.z-3.4f);
                Piece("ArchiveExitLantern"+side,wall.parent,position,new Vector3(.2f,.28f,.2f),glow);
                var lightTransform=wall.parent.Find("ArchiveExitWarmLight"+side);
                if(lightTransform==null) { var lightObject=new GameObject("ArchiveExitWarmLight"+side,typeof(Light)); lightObject.transform.SetParent(wall.parent,false); lightTransform=lightObject.transform; Undo.RegisterCreatedObjectUndo(lightObject,"Light curtain exterior"); }
                lightTransform.position=position+new Vector3(-side*.15f,0,.1f);
                var light=lightTransform.GetComponent<Light>(); Undo.RecordObject(light,"Set exterior lamp");
                light.type=LightType.Point; light.color=new Color(1,.75f,.45f); light.intensity=1.6f; light.range=7; light.shadows=LightShadows.None;
            }
            foreach(Transform child in doorway)
                if(child.name=="ClosedDoor" || child.name=="DoorSeam" || child.name.StartsWith("DoorHandle") || child.name.StartsWith("DoorBrace"))
                { var renderer=child.GetComponent<Renderer>(); if(renderer!=null) { Undo.RecordObject(renderer,"Replace wooden door with canvas"); renderer.enabled=false; } }
            var curtain=doorway.GetComponent<ArchiveCurtain>() ?? Undo.AddComponent<ArchiveCurtain>(doorway.gameObject);
            string materialPath=Root+"/ArchiveCanvas.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,materialPath); }
            material.SetColor("_BaseColor",new Color(.48f,.39f,.27f)); material.SetFloat("_Smoothness",0); material.SetFloat("_Metallic",0); material.SetFloat("_Cull",0);
            string texturePath=Root+"/HempWeave.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(texture==null)
            {
                texture=new Texture2D(32,32,TextureFormat.RGB24,true) {name="Hemp weave",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
                var pixels=new Color[1024]; for(int y=0;y<32;y++) for(int x=0;x<32;x++) { float v=.8f+.15f*((x%4==0 || y%4==0)?0:1)+.025f*Mathf.Sin(x*12+y*9); pixels[y*32+x]=new Color(v,v,v); }
                texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture,texturePath);
            }
            material.SetTexture("_BaseMap",texture); material.SetTextureScale("_BaseMap",new Vector2(10,20)); EditorUtility.SetDirty(material);
            Undo.RecordObject(curtain,"Set curtain material"); curtain.cloth=material;
            Undo.RecordObject(route,"Connect curtain travel"); route.curtain=curtain;
            route.curtainRustle=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/CanvasRustle.wav"); route.animateDeparture=true;
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Debug.Log("MOVEMENT_PRESENTATION_APPLIED: 6 distance-step clips and canvas departure");
        }

        static void Piece(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var existing=parent.Find(name); GameObject go;
            if(existing==null) { go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,true); Undo.RegisterCreatedObjectUndo(go,"Open curtain exit"); }
            else go=existing.gameObject;
            Undo.RecordObject(go.transform,"Place exterior exit"); go.transform.position=position;
            go.transform.localScale=new Vector3(size.x/parent.lossyScale.x,size.y/parent.lossyScale.y,size.z/parent.lossyScale.z);
            go.GetComponent<Renderer>().sharedMaterial=material;
        }

        static void MakeWave(string path,bool cloth,bool earth,int seed)
        {
            const int rate=22050; float length=cloth?.95f:.22f; int count=(int)(rate*length);
            var random=new System.Random(seed); double low=0,slow=0,body=0; var samples=new short[count];
            double toeTime=.052+random.NextDouble()*.014, weight=.9+random.NextDouble()*.18;
            for(int i=0;i<count;i++)
            {
                double t=(double)i/rate,noise=random.NextDouble()*2-1;
                low+=.24*(noise-low); slow+=.035*(noise-slow); double value;
                if(cloth)
                {
                    double envelope=Math.Pow(Math.Sin(Math.PI*t/length),2)*(.35+.65*Math.Pow(Math.Sin(t*15),2));
                    value=(low-slow)*envelope*.8;
                }
                else
                {
                    // Broad, damped contact instead of a pitched drum-like strike.
                    body+=.055*(noise-body);
                    double heel=(1-Math.Exp(-t/ .005))*Math.Exp(-t/.025);
                    double toe=t<toeTime?0:(1-Math.Exp(-(t-toeTime)/.008))*Math.Exp(-(t-toeTime)/.033);
                    double rub=Math.Exp(-Math.Pow((t-.095)/.033,2));
                    value=weight*(body*(earth?.72:1.05)*(heel+.42*toe)
                        +(low-slow)*(earth?.20:.10)*(heel+.55*toe)
                        +(low-slow)*(earth?.15:.045)*rub);
                    value*=Math.Min(1,(length-t)/.018);
                }
                samples[i]=(short)(Math.Clamp(value,-.9,.9)*32767);
            }
            using(var stream=File.Create(path)) using(var writer=new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+count*2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count*2); foreach(short sample in samples) writer.Write(sample);
            }
        }
    }
}
