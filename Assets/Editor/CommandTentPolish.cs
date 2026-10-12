using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class CommandTentPolish
    {
        const string Root = "Assets/Art/Environment/CommandTent";
        const string Log = "Logs/CommandTentPolish";
        static GameObject Tent => UnityEngine.Object.FindAnyObjectByType<IntroductionStage>().tent;

        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.isDirty || scene.path != "Assets/Scenes/Investigation.unity")
                throw new InvalidOperationException("Open the saved Investigation scene outside Play Mode.");
            Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Meshes");
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Root+"/Textures", "*.jpg")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                bool normal=path.Contains("NormalGL");
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.maxTextureSize=1024; importer.mipmapEnabled=true; importer.isReadable=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=4; importer.SaveAndReimport();
            }
            var cloth=Material("WovenCanvas",new Color(.88f,.78f,.62f),"Fabric030",new Vector2(1,1),.04f,.25f);
            var wood=Material("DarkTimber",new Color(.42f,.28f,.17f),"Wood049",new Vector2(1,2),.18f,.18f);
            var dirt=Material("PackedEarth",new Color(.61f,.54f,.42f),"Ground054",new Vector2(4,4),.025f,.35f);
            var crimson=Material("CrimsonCloth",new Color(.43f,.075f,.043f),"Fabric030",Vector2.one,.035f,.22f);
            var rug=Material("CrimsonRug",new Color(.25f,.065f,.04f),"Fabric030",new Vector2(3,7),.035f,.15f);
            var brass=Material("AgedBrass",new Color(.43f,.28f,.11f),null,Vector2.one,.32f,0); brass.SetFloat("_Metallic",.65f);
            var rope=Material("HempRope",new Color(.41f,.31f,.17f),"Fabric030",Vector2.one,.04f,.1f);
            var paper=Material("Parchment",new Color(.64f,.52f,.33f),null,Vector2.one,.02f,0);
            var ink=Material("MapInk",new Color(.12f,.20f,.18f),null,Vector2.one,.02f,0);
            var flame=Material("LampGlow",new Color(1,.54f,.12f),null,Vector2.one,.1f,0);
            flame.EnableKeyword("_EMISSION"); flame.SetColor("_EmissionColor",new Color(3,1.05f,.18f));
            var tent=Tent.transform;
            var previous=tent.Find("TentPolish"); if(previous!=null) Undo.DestroyObjectImmediate(previous.gameObject);
            var details=new GameObject("TentPolish").transform; Undo.RegisterCreatedObjectUndo(details.gameObject,"Improve command tent");
            details.SetParent(tent,false); details.position=new Vector3(40,0,0);
            string[] replaced={"BackCanvas","LeftCanvas","RightCanvas","RearEntrance","SlopingRoofLeft","SlopingRoofRight","CrimsonStandard","StandardStripe"};
            foreach(Transform child in tent) {
                var renderer=child.GetComponent<Renderer>(); if(renderer==null)continue;
                Undo.RecordObject(renderer,"Improve tent surface");
                if(replaced.Contains(child.name))renderer.enabled=false;
                else if(child.name=="TentFloor")renderer.sharedMaterial=dirt;
                else if(child.name=="AudienceCarpet")renderer.sharedMaterial=rug;
                else if(child.name=="CampaignMap")renderer.sharedMaterial=paper;
                else if(child.name=="MapMarker")renderer.sharedMaterial=crimson;
                else if(child.name=="StandardPole")renderer.sharedMaterial=brass;
                else if(child.name=="CommandDesk" || child.name=="DeskLeg" || child.name=="TentPost" || child.name=="RidgeBeam")renderer.sharedMaterial=wood;
            }
            foreach(var light in tent.GetComponentsInChildren<Light>(true)) {
                if(light.name!="TentLantern")continue;
                Undo.RecordObject(light,"Replace flat tent lighting"); light.enabled=false;
            }

            // Static low-poly fabric replaces planar panels; old collision walls stay in place.
            Grid("BackFabric",details,64,6,(u,v)=>new Vector3(-5.8f+11.6f*u,3.8f*v,4.36f-Fold(u,v)),cloth,new Vector2(6,2));
            Grid("LeftFabric",details,48,6,(u,v)=>new Vector3(-5.76f+Fold(u,v),3.8f*v,-4.4f+8.8f*u),cloth,new Vector2(5,2));
            Grid("RightFabric",details,48,6,(u,v)=>new Vector3(5.76f-Fold(u,v),3.8f*v,4.4f-8.8f*u),cloth,new Vector2(5,2));
            Grid("FrontFabric",details,64,6,(u,v)=>new Vector3(5.8f-11.6f*u,3.8f*v,-4.36f+Fold(u,v)),cloth,new Vector2(6,2));
            Grid("CanvasRoof",details,48,12,(u,v)=>new Vector3(-5.8f+11.6f*u,
                3.8f+1.3f*(1-Mathf.Abs(u*2-1))-.12f*Mathf.Sin(u*Mathf.PI*2)*Mathf.Sin(u*Mathf.PI*2)+.03f*Mathf.Cos(v*70),4.4f-8.8f*v),cloth,new Vector2(6,5));
            Grid("RearGable",details,32,4,(u,v)=>new Vector3(-5.8f+11.6f*u,3.8f+v*1.3f*(1-Mathf.Abs(u*2-1)),4.36f),cloth,new Vector2(6,1));
            Grid("CommandDrape",details,48,6,(u,v)=>new Vector3(-.8f+4.4f*u,.25f+3.25f*v,4.02f-.045f*Mathf.Sin(u*55)),crimson,new Vector2(2.2f,1.7f));
            foreach(float x in new[]{-.75f,3.55f}) Box("DrapeBorder",details,new Vector3(x,1.89f,3.90f),new Vector3(.055f,3.2f,.025f),brass);
            Bar("DrapeRod",details,new Vector3(-.95f,3.52f,3.95f),new Vector3(3.75f,3.52f,3.95f),.038f,brass);
            foreach(float z in new[]{-3.8f,0f,3.8f}) {
                Bar("RoofRib",details,new Vector3(-5.65f,3.65f,z),new Vector3(0,5.05f,z),.055f,wood);
                Bar("RoofRib",details,new Vector3(0,5.05f,z),new Vector3(5.65f,3.65f,z),.055f,wood);
            }
            foreach(float x in new[]{-5f,0f,5f}) {
                for(int i=0;i<5;i++) Box("PostBinding",details,new Vector3(x,2.94f+i*.03f,4.2f),new Vector3(.215f,.018f,.215f),rope);
            }
            // Borders give the carpet a readable edge without new collision geometry.
            foreach(float x in new[]{-.10f,2.90f})Box("RugEdge",details,new Vector3(x,.038f,-.5f),new Vector3(.028f,.003f,7.85f),brass);
            foreach(float z in new[]{-4.4f,3.4f})Box("RugEdge",details,new Vector3(1.4f,.039f,z),new Vector3(3,.003f,.025f),brass);
            foreach(float x in new[]{-.9f,3.25f}) {
                Grid("Standard",details,12,12,(u,v)=>new Vector3(x+.75f*u,1.66f+1.25f*v,2.75f-.05f*Mathf.Sin(u*18+v*2)),crimson,new Vector2(.4f,.7f));
                Box("StandardTrim",details,new Vector3(x+.375f,2.91f,2.73f),new Vector3(.8f,.025f,.035f),brass);
            }
            // Map relief is decoration, never an additional investigation target.
            Grid("RiverOnMap",details,36,1,(u,v)=>new Vector3(.59f+1.61f*u,.932f,-.35f+.20f*Mathf.Sin(u*8)+(v-.5f)*.035f),ink,Vector2.one);
            for(int i=0;i<12;i++) {
                float x=.68f+i*.13f, z=-.67f+.035f*Mathf.Sin(i*1.7f);
                Bar("MapContour",details,new Vector3(x,.933f,z),new Vector3(x+.06f,.933f,z+.09f),.003f,ink);
                Bar("MapContour",details,new Vector3(x+.06f,.933f,z+.09f),new Vector3(x+.10f,.933f,z+.015f),.003f,ink);
            }
            foreach(float x in new[]{.48f,2.32f}) Bar("MapRoll",details,new Vector3(x,.95f,-.81f),new Vector3(x,.95f,.12f),.028f,paper);
            Box("Inkstone",details,new Vector3(2.63f,.947f,-.67f),new Vector3(.23f,.048f,.15f),ink);
            Bar("Brush",details,new Vector3(2.49f,.984f,-.48f),new Vector3(2.78f,.984f,-.62f),.009f,wood);
            Scrolls(details,new Vector3(.09f,.945f,-.25f),paper,rope);
            Lamp(details,new Vector3(2.67f,.925f,.09f),brass,flame);
            Lamp(details,new Vector3(-2.65f,1.07f,2.9f),brass,flame);
            Box("SideTable",details,new Vector3(-2.75f,1,2.95f),new Vector3(1.7f,.12f,.8f),wood);
            foreach(float x in new[]{-3.43f,-2.07f})foreach(float z in new[]{2.66f,3.24f})Box("SideTableLeg",details,new Vector3(x,.5f,z),new Vector3(.09f,1,.09f),wood);
            Scrolls(details,new Vector3(-3.13f,1.09f,2.8f),paper,rope);
            foreach(float y in new[]{.26f,.92f,1.57f})Box("Shelf",details,new Vector3(4.50f,y,2.6f),new Vector3(1.25f,.065f,.72f),wood);
            foreach(float x in new[]{3.93f,5.07f})foreach(float z in new[]{2.27f,2.93f})Box("ShelfPost",details,new Vector3(x,.93f,z),new Vector3(.065f,1.86f,.065f),wood);
            Scrolls(details,new Vector3(4.5f,1.64f,2.5f),paper,rope);
            Scrolls(details,new Vector3(4.3f,.99f,2.5f),paper,rope);
            // One shadow map for the key, fill lamps do not allocate shadow maps.
            LightAt(details,"TentKey",new Vector3(.1f,3.15f,-1.8f),new Vector3(1.4f,1.0f,.6f),new Color(1,.78f,.51f),13,LightType.Spot,true);
            LightAt(details,"TentCoolFill",new Vector3(-1,3.2f,.1f),Vector3.zero,new Color(.68f,.75f,.88f),6,LightType.Point,false);
            LightAt(details,"TentRim",new Vector3(3.8f,2.8f,3.3f),Vector3.zero,new Color(1,.64f,.30f),3,LightType.Point,false);
            CombineDecoration(details);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Capture("after");
        }

        static float Fold(float u,float v) => .055f*(1.2f-v*.5f)*(Mathf.Sin(u*115)+.35f*Mathf.Sin(u*49))+.09f*Mathf.Sin(v*Mathf.PI);
        static Material Material(string name,Color tint,string surface,Vector2 tiling,float smooth,float normal)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",tint); material.SetFloat("_Smoothness",smooth); material.SetFloat("_Cull",0);
            material.enableInstancing=true;
            if(surface!=null){
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+surface+"_1K-JPG_Color.jpg"));
                material.SetTextureScale("_BaseMap",tiling);
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+surface+"_1K-JPG_NormalGL.jpg"));
                material.SetFloat("_BumpScale",normal); material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);return material;
        }
        static void Box(string name,Transform root,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.localPosition=p;go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Bar(string name,Transform root,Vector3 a,Vector3 b,float radius,Material material)
        {
            // Eight-sided cylinder, instead of Unity's dense sphere/cylinder primitives.
            var mesh=new Mesh(); var v=new Vector3[18]; var uv=new Vector2[18];var triangles=new int[96];
            for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;for(int j=0;j<2;j++){int k=i*2+j;v[k]=new Vector3(Mathf.Cos(angle)*radius,j-.5f,Mathf.Sin(angle)*radius);uv[k]=new Vector2(i/8f,j);}}
            v[16]=new Vector3(0,-.5f,0);v[17]=new Vector3(0,.5f,0);int n=0;
            for(int i=0;i<8;i++){int a0=i*2,b0=((i+1)%8)*2; foreach(int t in new[]{a0,a0+1,b0,b0,a0+1,b0+1,16,a0,b0,17,b0+1,a0+1})triangles[n++]=t;}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.transform.localPosition=(a+b)/2;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            go.transform.localScale=new Vector3(1,Vector3.Distance(a,b),1);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Grid(string name,Transform root,int nx,int ny,Func<float,float,Vector3> position,Material material,Vector2 tile)
        {
            var mesh=new Mesh();var vertices=new Vector3[(nx+1)*(ny+1)];var uv=new Vector2[vertices.Length];var indices=new int[nx*ny*6];int k=0;
            for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){int i=y*(nx+1)+x;float u=x/(float)nx,v=y/(float)ny;vertices[i]=position(u,v);uv[i]=new Vector2(u*tile.x,v*tile.y);}
            for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=y*(nx+1)+x;foreach(int t in new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2})indices[k++]=t;}
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Scrolls(Transform root,Vector3 p,Material paper,Material rope)
        {
            for(int i=0;i<4;i++){var c=p+new Vector3(i*.067f,.045f,0);Bar("Scroll",root,c+new Vector3(0,0,-.18f),c+new Vector3(0,0,.18f),.029f,paper);
                Box("ScrollTie",root,c,new Vector3(.06f,.062f,.016f),rope);}
        }
        static void Lamp(Transform root,Vector3 p,Material brass,Material flame)
        {
            Bar("LampFoot",root,p,p+Vector3.up*.04f,.10f,brass);
            Bar("LampStem",root,p,p+Vector3.up*.29f,.025f,brass);
            Bar("LampBowl",root,p+Vector3.up*.28f,p+Vector3.up*.33f,.12f,brass);
            Bar("Flame",root,p+Vector3.up*.33f,p+Vector3.up*.41f,.017f,flame);
        }
        static void LightAt(Transform root,string name,Vector3 position,Vector3 target,Color color,float intensity,LightType type,bool shadow)
        {
            var light=new GameObject(name,typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);light.transform.localPosition=position;
            light.type=type;light.color=color;light.intensity=intensity;light.range=10;light.spotAngle=92;light.innerSpotAngle=56;
            if(type==LightType.Spot)light.transform.localRotation=Quaternion.LookRotation(target-position);
            light.shadows=shadow?LightShadows.Soft:LightShadows.None;light.shadowBias=.04f;light.shadowNormalBias=.18f;
            if(shadow){var data=light.gameObject.AddComponent<UniversalAdditionalLightData>();var serialized=new SerializedObject(data);
                serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();}
        }
        static void CombineDecoration(Transform root)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>();
            var transient=filters.Select(f=>f.sharedMesh).Where(m=>!AssetDatabase.Contains(m)).Distinct().ToArray();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial)){
                var combines=group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh {name="Tent_"+group.Key.name};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
                string path=Root+"/Meshes/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{
                    saved.Clear(); saved.indexFormat=mesh.indexFormat;
                    saved.vertices=mesh.vertices;saved.normals=mesh.normals;saved.uv=mesh.uv;
                    saved.triangles=mesh.triangles;saved.RecalculateTangents();saved.RecalculateBounds();
                    UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);
                }
                var go=new GameObject(saved.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
                go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<Renderer>().sharedMaterial=group.Key;
            }
            foreach(var filter in filters)UnityEngine.Object.DestroyImmediate(filter.gameObject);
            foreach(var mesh in transient)UnityEngine.Object.DestroyImmediate(mesh);
        }

        public static void Before() => Capture("before");
        public static void After() => Capture("after");

        static void Capture(string name)
        {
            Directory.CreateDirectory(Log);
            var stage = UnityEngine.Object.FindAnyObjectByType<IntroductionStage>();
            var source = stage.player.viewCamera;
            var go = new GameObject("TentReviewCamera", typeof(Camera));
            var camera = go.GetComponent<Camera>(); camera.CopyFrom(source); camera.enabled = false;
            var target = new RenderTexture(1280,720,24);
            var pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
            var old = RenderTexture.active; bool active = Tent.activeSelf;
            try {
                Tent.SetActive(true);
                camera.transform.SetPositionAndRotation(stage.tentSpawn.position + new Vector3(0,1.65f,0), stage.tentSpawn.rotation);
                camera.targetTexture = target; camera.aspect = 1280f/720;
                for(int i=0;i<4;i++) camera.Render();
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                File.WriteAllBytes(Log+"/"+name+".png",pixels.EncodeToPNG());
                var renderers = Tent.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                var meshFilters = renderers.Select(r=>r.GetComponent<MeshFilter>()).Where(f=>f!=null && f.sharedMesh!=null).ToArray();
                var report = new StringBuilder();
                report.AppendLine("Active tent renderers: "+renderers.Length);
                report.AppendLine("Tent triangles: "+meshFilters.Sum(f=>f.sharedMesh.triangles.Length/3));
                report.AppendLine("Unique materials: "+renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count());
                foreach(var light in Tent.GetComponentsInChildren<Light>().Where(l=>l.enabled)) report.AppendLine("Light "+light.name+" "+light.type+" intensity="+light.intensity+" shadows="+light.shadows);
                report.AppendLine("Render snapshot (includes scene): drawcalls="+UnityStats.drawCalls+" triangles="+UnityStats.triangles+" setpass="+UnityStats.setPassCalls);
                report.AppendLine("Camera="+camera.transform.position+" rotation="+camera.transform.eulerAngles);
                report.AppendLine("Snapshot is NOT an averaged player FPS measurement.");
                File.WriteAllText(Log+"/"+name+".txt",report.ToString());
            } finally {
                Tent.SetActive(active); RenderTexture.active=old; camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(go); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
