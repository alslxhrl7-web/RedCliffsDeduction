using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class FirstInvestigationBuilder
    {
        public const string ScenePath = "Assets/Scenes/Investigation.unity";
        static Material wood, darkWood, plaster, paper, cloth, gold, stone;
        static Font editorFont;

        [MenuItem("적벽추리/첫 조사 장면 생성 또는 열기")]
        public static void CreateOrOpen()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before opening the scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            BuildNew();
        }

        public static void BuildNew()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Cannot build in Play Mode.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Investigation already exists; open it instead of overwriting.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene changes first.");
            Directory.CreateDirectory("Assets/Art/PrototypeMaterials");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            wood = Material("WarmWood", new Color(.30f, .17f, .09f));
            darkWood = Material("DarkWood", new Color(.12f, .075f, .046f));
            plaster = Material("Plaster", new Color(.52f, .44f, .32f));
            paper = Material("Paper", new Color(.81f, .70f, .48f));
            cloth = Material("ClerkRobe", new Color(.16f, .26f, .27f));
            gold = Material("Brass", new Color(.65f, .42f, .15f));
            stone = Material("Floor", new Color(.23f, .22f, .19f));

            var room = new GameObject("ArchiveRoom").transform;
            Box("Floor", new Vector3(0,-.15f,0), new Vector3(12,.3f,12), stone, room);
            Box("NorthWall", new Vector3(0,2,6), new Vector3(12,4,.25f), plaster, room);
            Box("SouthWall", new Vector3(0,2,-6), new Vector3(12,4,.25f), plaster, room);
            Box("WestWall", new Vector3(-6,2,0), new Vector3(.25f,4,12), plaster, room);
            Box("EastWall", new Vector3(6,2,0), new Vector3(.25f,4,12), plaster, room);
            Box("Ceiling", new Vector3(0,4.1f,0), new Vector3(12,.2f,12), darkWood, room);
            for (int x=-5; x<=5; x+=5)
            {
                Box("RoofBeam", new Vector3(x,3.75f,0), new Vector3(.24f,.3f,12), wood, room);
                foreach (float z in new[] {-5.8f,5.8f}) Box("Pillar", new Vector3(x,1.9f,z), new Vector3(.28f,3.8f,.28f), wood, room);
            }
            for (int i=0; i<3; i++) Shelf(new Vector3(-5.5f,0,-2.5f+i*3), room);
            Table(new Vector3(0,0,2.4f), room);
            var tray = Box("MissingDocumentTray", new Vector3(0,1.06f,2.4f), new Vector3(1.45f,.12f,.9f), darkWood, room);
            tray.AddComponent<InvestigationTarget>().kind = InvestigationTarget.TargetKind.MissingDocument;
            Box("TrayBack", new Vector3(0,1.18f,2.82f), new Vector3(1.45f,.2f,.06f), gold, tray.transform);
            Box("TrayLeft", new Vector3(-.7f,1.18f,2.4f), new Vector3(.05f,.2f,.9f), gold, tray.transform);
            Box("TrayRight", new Vector3(.7f,1.18f,2.4f), new Vector3(.05f,.2f,.9f), gold, tray.transform);
            // Children are positioned in world space; preserve their scale under a scaled tray.
            for (int i=0;i<3;i++) Box("AdjacentRecords", new Vector3(1.08f,1.09f+i*.07f,2.4f), new Vector3(.4f,.055f,.65f), paper, room);

            var npc = new GameObject("Seolpyeong");
            npc.transform.position = new Vector3(2.8f,0,2.7f);
            npc.AddComponent<InvestigationTarget>().kind = InvestigationTarget.TargetKind.Seolpyeong;
            Shape("Robe", PrimitiveType.Capsule, new Vector3(2.8f,.84f,2.7f), new Vector3(.58f,.75f,.58f), cloth, npc.transform);
            Shape("Head", PrimitiveType.Sphere, new Vector3(2.8f,1.65f,2.7f), new Vector3(.4f,.46f,.4f), paper, npc.transform);
            Shape("Cap", PrimitiveType.Cylinder, new Vector3(2.8f,1.9f,2.7f), new Vector3(.45f,.07f,.45f), darkWood, npc.transform);
            Box("Belt", new Vector3(2.8f,.88f,2.7f), new Vector3(.60f,.10f,.60f), darkWood, npc.transform);
            Box("SleeveLeft", new Vector3(2.39f,1.02f,2.7f), new Vector3(.25f,.62f,.34f), cloth, npc.transform);
            Box("SleeveRight", new Vector3(3.21f,1.02f,2.7f), new Vector3(.25f,.62f,.34f), cloth, npc.transform);
            Table(new Vector3(4.5f,0,4.5f), room, .75f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f,.39f,.32f);
            RenderSettings.fog = false;
            foreach (var p in new[] {new Vector3(-2,3,1),new Vector3(3.5f,3,2),new Vector3(0,3,-3)})
            {
                var lamp = new GameObject("WarmLantern", typeof(Light)); lamp.transform.position=p;
                var light=lamp.GetComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.77f,.46f);
                light.intensity=3.0f; light.range=9; light.shadows=LightShadows.Soft;
                Shape("LanternBody",PrimitiveType.Cylinder,p+Vector3.up*.35f,new Vector3(.3f,.25f,.3f),paper,room);
            }

            var player = new GameObject("Player", typeof(CharacterController), typeof(FirstPersonInvestigator));
            player.transform.position = new Vector3(0,.08f,-3.5f);
            var cc=player.GetComponent<CharacterController>(); cc.height=1.8f; cc.radius=.3f; cc.center=new Vector3(0,.9f,0); cc.stepOffset=.25f;
            var cameraGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraGo.tag="MainCamera"; cameraGo.transform.SetParent(player.transform,false); cameraGo.transform.localPosition=new Vector3(0,1.65f,0);
            var camera=cameraGo.GetComponent<Camera>(); camera.fieldOfView=65; camera.nearClipPlane=.06f; camera.farClipPlane=60;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.08f,.10f,.11f);
            var sessionGo=new GameObject("InvestigationSession",typeof(InvestigationSession));
            var session=sessionGo.GetComponent<InvestigationSession>();
            session.view=BuildUI();
            var mover=player.GetComponent<FirstPersonInvestigator>(); mover.viewCamera=camera; mover.session=session;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[] {new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("First investigation scene created: " + ScenePath);
        }

        static Material Material(string name, Color color)
        {
            string path="Assets/Art/PrototypeMaterials/"+name+".mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result!=null) return result;
            result=new Material(Shader.Find("Universal Render Pipeline/Lit")); result.color=color;
            result.SetFloat("_Smoothness",.15f); AssetDatabase.CreateAsset(result,path); return result;
        }
        static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material mat,Transform parent)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,true);
            go.transform.position=position; go.transform.localScale=scale;
            // All authored parents except the tray have unit scale.
            if(parent!=null && parent.lossyScale!=Vector3.one)
                go.transform.localScale=new Vector3(scale.x/parent.lossyScale.x,scale.y/parent.lossyScale.y,scale.z/parent.lossyScale.z);
            go.GetComponent<Renderer>().sharedMaterial=mat; return go;
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent)=>Shape(n,PrimitiveType.Cube,p,s,m,parent);
        static void Table(Vector3 p,Transform root,float size=1)
        {
            Box("WritingDesk",p+Vector3.up*.94f,new Vector3(3*size,.14f,1.3f),wood,root);
            foreach(float x in new[]{-1.3f,1.3f}) foreach(float z in new[]{-.45f,.45f})
                Box("DeskLeg",p+new Vector3(x*size,.45f,z),new Vector3(.14f,.9f,.14f),darkWood,root);
        }
        static void Shelf(Vector3 p,Transform root)
        {
            Box("ShelfBack",p+new Vector3(0,1.3f,0),new Vector3(.18f,2.6f,2.3f),darkWood,root);
            foreach(float y in new[]{.3f,1.1f,1.9f,2.6f})
            {
                Box("ShelfBoard",p+new Vector3(.35f,y,0),new Vector3(.9f,.1f,2.3f),wood,root);
                if(y>2.5f) continue;
                for(int z=0;z<4;z++) Box("BambooRecords",p+new Vector3(.35f,y+.2f,-.85f+z*.55f),new Vector3(.62f,.25f,.39f),paper,root);
            }
        }
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=min; r.anchorMax=max; r.offsetMin=low; r.offsetMax=high; return r;
        }
        static UnityEngine.UI.Text Label(string name,Transform parent,string content,int size,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {
            var r=Rect(name,parent,min,max,low,high); var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();
            t.font=editorFont; t.fontSize=size; t.color=new Color(.94f,.89f,.78f); t.text=content; t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        static InvestigationView BuildUI()
        {
            editorFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go=new GameObject("InvestigationUI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(InvestigationView));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            var view=go.GetComponent<InvestigationView>();
            var banner=Rect("ObjectiveBackdrop",go.transform,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-112),Vector2.zero);
            banner.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.04f,.07f,.08f,.88f);
            Label("Location",banner,"적벽추리  /  조조군 문서고",18,Vector2.zero,Vector2.one,new Vector2(36,61),new Vector2(-30,-18));
            view.objective=Label("Objective",banner,"",29,Vector2.zero,Vector2.one,new Vector2(36,16),new Vector2(-30,-49));
            var dot=Label("Crosshair",go.transform,"·",40,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-15,-23),new Vector2(15,23)); dot.alignment=TextAnchor.MiddleCenter; view.crosshair=dot.gameObject;
            view.prompt=Label("Prompt",go.transform,"",26,new Vector2(.15f,.25f),new Vector2(.85f,.25f),new Vector2(0,-25),new Vector2(0,25)); view.prompt.alignment=TextAnchor.MiddleCenter;
            var controls=Label("Controls",go.transform,"WASD 이동    마우스 시점    E 조사/대화    J 수첩    Esc 일시정지",20,new Vector2(0,0),new Vector2(1,0),new Vector2(30,20),new Vector2(-30,60)); controls.alignment=TextAnchor.MiddleCenter;
            var shade=Rect("Modal",go.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.015f,.025f,.03f,.68f); view.modal=shade.gameObject;
            var panel=Rect("Panel",shade,new Vector2(.12f,.12f),new Vector2(.88f,.86f),Vector2.zero,Vector2.zero);
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.065f,.095f,.105f,.99f);
            var accent=Rect("Accent",panel,new Vector2(0,1),Vector2.one,new Vector2(0,-4),Vector2.zero);
            accent.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.74f,.56f,.28f);
            view.eyebrow=Label("Eyebrow",panel,"",20,new Vector2(0,1),Vector2.one,new Vector2(40,-62),new Vector2(-40,-30));
            view.title=Label("Title",panel,"",38,new Vector2(0,1),Vector2.one,new Vector2(40,-123),new Vector2(-40,-67));
            view.body=Label("Body",panel,"",25,new Vector2(0,.38f),new Vector2(1,1),new Vector2(40,0),new Vector2(-40,-145));
            view.body.lineSpacing=1.2f;
            view.choices=Rect("Choices",panel,Vector2.zero,new Vector2(1,0),new Vector2(40,62),new Vector2(-40,234));
            var layout=view.choices.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing=8; layout.childControlHeight=true; layout.childControlWidth=true; layout.childForceExpandHeight=false; layout.childForceExpandWidth=true; layout.childAlignment=TextAnchor.LowerLeft;
            view.footer=Label("Footer",panel,"",18,Vector2.zero,new Vector2(1,0),new Vector2(40,16),new Vector2(-40,48));
            return view;
        }
    }
}
