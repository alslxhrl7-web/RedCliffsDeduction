using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    /// <summary>Adds travel destinations without rebuilding the existing investigation.</summary>
    public static class InvestigationRouteBuilder
    {
        const string MaterialFolder = "Assets/Art/PrototypeMaterials";
        const string UndoLabel = "Connect archive and naval camp";
        static Material wood, darkWood, paper, brass, canvas, skin, hair, armour;
        static Material earth, path, guardCloth, caimaoCloth, zhangyunCloth, banner;

        public static void BuildDuseokWitnesses()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != FirstInvestigationBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save Investigation scene first.");
            var routes = FindRoot(scene, "InvestigationRoutes");
            var camp = routes == null ? null : routes.transform.Find("NavalCamp");
            var entrance = routes == null ? null : routes.transform.Find("ArchiveEntrance");
            if (camp == null || entrance == null) throw new InvalidOperationException("Existing routes required.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                LoadMaterials();
                var guard = Person("ReliefGuard", entrance, new Vector3(-2.4f, 0, -4), guardCloth, InvestigationTarget.TargetKind.ReliefGuard, false);
                Undo.RecordObject(guard, UndoLabel);
                guard.localRotation = Quaternion.Euler(0, 180, 0);
                Person("DocumentCourier", camp, new Vector3(-2.3f, 0, -2.6f), zhangyunCloth, InvestigationTarget.TargetKind.DocumentCourier, false);
                Person("GamblingSoldier", camp, new Vector3(4.5f, 0, -3.8f), caimaoCloth, InvestigationTarget.TargetKind.GamblingSoldier, false);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("DUSEOK_WITNESSES_CONNECTED");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void BuildPersonalEvidence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != FirstInvestigationBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save Investigation scene first.");
            var room = FindRoot(scene, "ArchiveRoom");
            if (room == null) throw new InvalidOperationException("ArchiveRoom is required.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                LoadMaterials();
                var root = Group("PersonalEvidence", room.transform, Vector3.zero);
                var token = Group("PersonalToken", root, new Vector3(3.95f, 1.08f, 4.12f));
                var target = Component<InvestigationTarget>(token.gameObject);
                Undo.RecordObject(target, UndoLabel);
                target.kind = InvestigationTarget.TargetKind.PersonalToken;
                Box("PaperBundle", token, Vector3.zero, new Vector3(.55f, .12f, .48f), paper);
                Box("WoodenToken", token, new Vector3(0, .09f, 0), new Vector3(.2f, .05f, .34f), wood);
                for (int i = 0; i < 3; i++)
                    Box("Notch" + i, token, new Vector3(.085f, .119f, -.12f + i * i * .052f), new Vector3(.055f, .008f, .022f), hair, false);
                var papers = Group("PersonalPapers", root, new Vector3(4.8f, 1.2f, 4.5f));
                var paperTarget = Component<InvestigationTarget>(papers.gameObject);
                Undo.RecordObject(paperTarget, UndoLabel);
                paperTarget.kind = InvestigationTarget.TargetKind.PersonalPapers;
                Box("Case", papers, Vector3.zero, new Vector3(.68f, .35f, .6f), darkWood);
                Box("Lid", papers, new Vector3(0, .2f, 0), new Vector3(.73f, .06f, .65f), wood);
                Box("Clasp", papers, new Vector3(0, .12f, -.31f), new Vector3(.1f, .15f, .025f), brass, false);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("PERSONAL_EVIDENCE_CONNECTED");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void BuildPersonalEvidenceBatch()
        {
            EditorSceneManager.OpenScene(FirstInvestigationBuilder.ScenePath);
            BuildPersonalEvidence();
        }

        public static void BuildCareerRecords()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != FirstInvestigationBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save Investigation scene first.");
            var routes = FindRoot(scene, "InvestigationRoutes");
            var camp = routes == null ? null : routes.transform.Find("NavalCamp");
            if (camp == null) throw new InvalidOperationException("NavalCamp is required.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                LoadMaterials();
                var station = Group("CareerRecordsStation", camp, new Vector3(-4.4f, 0, -3.7f));
                Person("RecordsClerk", station, new Vector3(0, 0, 1.05f), guardCloth, InvestigationTarget.TargetKind.RecordsClerk, false);
                var table = Group("CareerRecordsTable", station, Vector3.zero);
                var target = Component<InvestigationTarget>(table.gameObject);
                Undo.RecordObject(target, UndoLabel);
                target.kind = InvestigationTarget.TargetKind.CareerRecords;
                Box("Tabletop", table, new Vector3(0, .94f, 0), new Vector3(2.1f, .12f, 1), wood);
                foreach (int x in new[] { -1, 1 })
                    foreach (int z in new[] { -1, 1 })
                        Box("Leg" + x + z, table, new Vector3(x * .85f, .44f, z * .35f), new Vector3(.12f, .88f, .12f), darkWood);
                foreach (int side in new[] { -1, 1 })
                {
                    Box(side < 0 ? "PersonnelRecord" : "DutyRecord", table, new Vector3(side * .48f, 1.025f, 0), new Vector3(.75f, .05f, .65f), paper);
                    for (int i = 0; i < 4; i++)
                        Box("Writing" + side + i, table, new Vector3(side * .48f, 1.053f, -.18f + i * .11f), new Vector3(.5f, .004f, .02f), hair, false);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("CAREER_RECORDS_INSTALLED: clerk and two records at naval entrance left.");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building investigation routes.");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != FirstInvestigationBuilder.ScenePath)
                throw new InvalidOperationException("Open the existing Assets/Scenes/Investigation.unity scene first.");
            if (scene.isDirty)
                throw new InvalidOperationException("Save the current Investigation scene changes before building routes.");

            var session = SingleComponent<InvestigationSession>(scene);
            var player = SingleComponent<FirstPersonInvestigator>(scene);
            if (FindRoot(scene, "ArchiveRoom") == null)
                throw new InvalidOperationException("The existing ArchiveRoom is required; the scene will not be rebuilt.");
            if (Shader.Find("Universal Render Pipeline/Lit") == null)
                throw new InvalidOperationException("The Universal Render Pipeline/Lit shader is required.");

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                LoadMaterials();
                var root = FindRoot(scene, "InvestigationRoutes");
                if (root == null)
                {
                    root = new GameObject("InvestigationRoutes");
                    SceneManager.MoveGameObjectToScene(root, scene);
                    Undo.RegisterCreatedObjectUndo(root, UndoLabel);
                }
                else if (root.transform.parent != null || root.transform.lossyScale != Vector3.one)
                    throw new InvalidOperationException("InvestigationRoutes must remain a root object with unit scale.");

                var route = Component<InvestigationRoute>(root);
                var archiveArrival = Group("ArchiveArrival", root.transform, new Vector3(0, .08f, -3.5f));
                var navalArrival = Group("NavalArrival", root.transform, new Vector3(-40, .08f, -5));
                // These are world-space destinations even if the route root was moved in the Editor.
                archiveArrival.SetPositionAndRotation(new Vector3(0, .08f, -3.5f), Quaternion.identity);
                navalArrival.SetPositionAndRotation(new Vector3(-40, .08f, -5), Quaternion.identity);
                var entrance = Group("ArchiveEntrance", root.transform, Vector3.zero);
                entrance.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                BuildArchiveEntrance(entrance);
                var camp = Group("NavalCamp", root.transform, new Vector3(-40, 0, 0));
                camp.SetPositionAndRotation(new Vector3(-40, 0, 0), Quaternion.identity);
                BuildNavalCamp(camp);

                Undo.RecordObject(route, UndoLabel);
                route.player = player;
                route.archiveArrival = archiveArrival;
                route.navalArrival = navalArrival;
                EditorUtility.SetDirty(route);
                Undo.RecordObject(session, UndoLabel);
                session.route = route;
                EditorUtility.SetDirty(session);

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity could not save the Investigation scene.");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("Investigation routes connected: archive entrance, Duseok, and naval camp with Caimao, Zhangyun, and the maintenance order.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        static void BuildArchiveEntrance(Transform root)
        {
            var doorway = Group("SouthDoorway", root, new Vector3(0, 0, -5.68f));
            var door = Box("ClosedDoor", doorway, new Vector3(0, 1.25f, 0), new Vector3(2.22f, 2.5f, .16f), wood);
            var exit = Component<InvestigationTarget>(door);
            Undo.RecordObject(exit, UndoLabel);
            exit.kind = InvestigationTarget.TargetKind.NavalCampExit;
            Box("LeftFrame", doorway, new Vector3(-1.23f, 1.4f, .09f), new Vector3(.2f, 2.8f, .25f), darkWood);
            Box("RightFrame", doorway, new Vector3(1.23f, 1.4f, .09f), new Vector3(.2f, 2.8f, .25f), darkWood);
            Box("Lintel", doorway, new Vector3(0, 2.77f, .09f), new Vector3(2.67f, .25f, .25f), darkWood);
            Box("DoorSeam", doorway, new Vector3(0, 1.25f, .10f), new Vector3(.035f, 2.45f, .045f), darkWood, false);
            foreach (int side in new[] { -1, 1 })
            {
                Box("DoorHandle" + side, doorway, new Vector3(side * .16f, 1.24f, .14f), new Vector3(.06f, .23f, .07f), brass, false);
                Box("DoorBrace" + side, doorway, new Vector3(side * .57f, .65f, .11f), new Vector3(.95f, .10f, .06f), darkWood, false);
            }
            var guard = Person("Duseok", root, new Vector3(2.4f, 0, -4), guardCloth, InvestigationTarget.TargetKind.Duseok, false);
            Undo.RecordObject(guard, UndoLabel);
            guard.localRotation = Quaternion.Euler(0, 180, 0);
            Box("SpearShaft", guard, new Vector3(-.75f, 1.22f, 0), new Vector3(.065f, 2.44f, .065f), wood, false);
            Shape("SpearHead", PrimitiveType.Cube, guard, new Vector3(-.75f, 2.58f, 0), new Vector3(.15f, .32f, .075f), armour, false,
                Quaternion.Euler(0, 0, 45));
        }

        static void BuildNavalCamp(Transform root)
        {
            Box("Ground", root, new Vector3(0, -.15f, 0), new Vector3(18, .3f, 18), earth);
            Box("ApproachPath", root, new Vector3(0, .011f, -1), new Vector3(3, .018f, 13), path, false);
            Box("CrossPath", root, new Vector3(0, .012f, .45f), new Vector3(13, .018f, 2), path, false);
            Fence("NorthFence", root, new Vector3(0, 0, 8.7f), 0);
            Fence("SouthFence", root, new Vector3(0, 0, -8.7f), 0);
            Fence("WestFence", root, new Vector3(-8.7f, 0, 0), 90);
            Fence("EastFence", root, new Vector3(8.7f, 0, 0), 90);

            Tent("CaimaoTent", root, new Vector3(-4, 0, 4.65f), caimaoCloth);
            Tent("ZhangyunTent", root, new Vector3(4, 0, 4.65f), zhangyunCloth);
            Person("Caimao", root, new Vector3(-4, 0, 2), caimaoCloth, InvestigationTarget.TargetKind.Caimao, true);
            Person("Zhangyun", root, new Vector3(4, 0, 2), zhangyunCloth, InvestigationTarget.TargetKind.Zhangyun, true);
            BuildMaintenanceOrder(root);
            Standard("CampStandard", root, new Vector3(0, 0, 6.7f), banner);
            Standard("CaimaoStandard", root, new Vector3(-6.6f, 0, 2.2f), caimaoCloth);
            Standard("ZhangyunStandard", root, new Vector3(6.6f, 0, 2.2f), zhangyunCloth);
            Lantern("LeftEntranceLantern", root, new Vector3(-3.3f, 0, -6.8f));
            Lantern("RightEntranceLantern", root, new Vector3(3.3f, 0, -6.8f));
            Lantern("LeftTentLantern", root, new Vector3(-6.8f, 0, .2f));
            Lantern("RightTentLantern", root, new Vector3(6.8f, 0, .2f));
            var fillRoot = Group("CampFill", root, new Vector3(0, 6, -1));
            var fill = Component<Light>(fillRoot.gameObject);
            Undo.RecordObject(fill, UndoLabel);
            fill.type = LightType.Point;
            fill.color = new Color(.72f, .82f, 1f);
            fill.intensity = 28;
            fill.range = 25;
            fill.shadows = LightShadows.None;

            // Distinct entrance posts and a high lintel frame the arrival without blocking its path.
            var gate = Group("CampEntrance", root, new Vector3(0, 0, -7.8f));
            Box("LeftPost", gate, new Vector3(-2, 1.6f, 0), new Vector3(.28f, 3.2f, .28f), darkWood);
            Box("RightPost", gate, new Vector3(2, 1.6f, 0), new Vector3(.28f, 3.2f, .28f), darkWood);
            Box("Crossbeam", gate, new Vector3(0, 3.15f, 0), new Vector3(4.65f, .3f, .32f), wood);
            Box("EntranceBanner", gate, new Vector3(0, 2.68f, -.02f), new Vector3(2.25f, .58f, .055f), banner, false);
            // A reachable sign on the camp side of the fence provides the return interaction.
            var sign = Group("ArchiveReturnSign", root, new Vector3(1.45f, 0, -6.3f));
            Box("Post", sign, new Vector3(0, .65f, 0), new Vector3(.14f, 1.3f, .14f), darkWood);
            Box("Board", sign, new Vector3(0, 1.45f, 0), new Vector3(1.15f, .6f, .15f), wood);
            Box("ArrowShaft", sign, new Vector3(0, 1.45f, .09f), new Vector3(.65f, .065f, .03f), paper, false);
            Shape("ArrowUpper", PrimitiveType.Cube, sign, new Vector3(-.23f, 1.54f, .09f), new Vector3(.28f, .065f, .03f), paper, false,
                Quaternion.Euler(0, 0, 40));
            Shape("ArrowLower", PrimitiveType.Cube, sign, new Vector3(-.23f, 1.36f, .09f), new Vector3(.28f, .065f, .03f), paper, false,
                Quaternion.Euler(0, 0, -40));
            var returnTarget = Component<InvestigationTarget>(sign.gameObject);
            Undo.RecordObject(returnTarget, UndoLabel);
            returnTarget.kind = InvestigationTarget.TargetKind.ArchiveReturn;
        }

        static void BuildMaintenanceOrder(Transform root)
        {
            // Keep the cross-path and both commanders' approaches clear.
            var table = Group("MaintenanceOrderTable", root, new Vector3(0, 0, 2.4f));
            var target = Component<InvestigationTarget>(table.gameObject);
            Undo.RecordObject(target, UndoLabel);
            target.kind = InvestigationTarget.TargetKind.MaintenanceOrder;
            // The tabletop also resolves to this target, so the thin page is easy to inspect.
            Box("Tabletop", table, new Vector3(0, .94f, 0), new Vector3(1.8f, .12f, 1), wood);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box("Leg" + x + z, table, new Vector3(x * .72f, .44f, z * .35f), new Vector3(.12f, .88f, .12f), darkWood);
            Box("MaintenanceOrder", table, new Vector3(-.13f, 1.023f, .02f), new Vector3(1.1f, .045f, .69f), paper);
            for (int i = 0; i < 4; i++)
                Box("Writing" + i, table, new Vector3(-.20f, 1.048f, -.16f + i * .105f), new Vector3(.68f, .004f, .025f), hair, false);
            Box("CommandSeal", table, new Vector3(.30f, 1.048f, -.13f), new Vector3(.12f, .004f, .12f), banner, false);
        }

        static void Fence(string name, Transform root, Vector3 position, float yaw)
        {
            var fence = Group(name, root, position);
            fence.localRotation = Quaternion.Euler(0, yaw, 0);
            // A continuous low solid wall keeps the player on the raised prototype ground.
            Box("Base", fence, new Vector3(0, .57f, 0), new Vector3(17.5f, 1.14f, .18f), wood);
            Box("UpperRail", fence, new Vector3(0, 1.5f, 0), new Vector3(17.7f, .14f, .20f), darkWood);
            for (int i = 0; i < 13; i++)
            {
                float x = -8.4f + i * 1.4f;
                Box("Post" + i, fence, new Vector3(x, .96f, 0), new Vector3(.20f, 1.92f, .26f), darkWood);
            }
        }

        static void Tent(string name, Transform root, Vector3 position, Material trim)
        {
            var tent = Group(name, root, position);
            Box("FloorMat", tent, new Vector3(0, .016f, .2f), new Vector3(4.8f, .025f, 4.4f), path, false);
            Box("BackCanvas", tent, new Vector3(0, 1.13f, 2.3f), new Vector3(5, 2.26f, .09f), canvas);
            Box("LeftCanvas", tent, new Vector3(-2.45f, 1.13f, .3f), new Vector3(.09f, 2.26f, 4), canvas);
            Box("RightCanvas", tent, new Vector3(2.45f, 1.13f, .3f), new Vector3(.09f, 2.26f, 4), canvas);
            Shape("LeftRoof", PrimitiveType.Cube, tent, new Vector3(-1.23f, 2.83f, .2f), new Vector3(2.78f, .09f, 4.55f), canvas, true,
                Quaternion.Euler(0, 0, 25));
            Shape("RightRoof", PrimitiveType.Cube, tent, new Vector3(1.23f, 2.83f, .2f), new Vector3(2.78f, .09f, 4.55f), canvas, true,
                Quaternion.Euler(0, 0, -25));
            Box("Ridge", tent, new Vector3(0, 3.39f, .2f), new Vector3(.12f, .14f, 4.7f), darkWood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("FrontPost" + side, tent, new Vector3(side * 2.35f, 1.18f, -1.8f), new Vector3(.14f, 2.36f, .14f), darkWood);
                Box("FrontTrim" + side, tent, new Vector3(side * 1.97f, 1.7f, -1.83f), new Vector3(.5f, 1.25f, .06f), trim, false);
            }
            var table = Group("WritingTable", tent, new Vector3(0, 0, .55f));
            Box("Tabletop", table, new Vector3(0, .87f, 0), new Vector3(2.05f, .13f, 1.04f), wood);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box("Leg" + x + z, table, new Vector3(x * .85f, .42f, z * .36f), new Vector3(.11f, .84f, .11f), darkWood);
            Box("Records", table, new Vector3(-.27f, .96f, .08f), new Vector3(.83f, .045f, .58f), paper, false);
            for (int i = 0; i < 3; i++)
                Box("RolledRecord" + i, table, new Vector3(.62f, .99f + i * .058f, .13f), new Vector3(.23f, .05f, .43f), paper, false);
        }

        static Transform Person(string name, Transform root, Vector3 position, Material robe, InvestigationTarget.TargetKind kind, bool commander)
        {
            var person = Group(name, root, position);
            var target = Component<InvestigationTarget>(person.gameObject);
            Undo.RecordObject(target, UndoLabel);
            target.kind = kind;
            var collider = Component<CapsuleCollider>(person.gameObject);
            Undo.RecordObject(collider, UndoLabel);
            collider.center = new Vector3(0, 1.08f, 0);
            collider.height = 2.16f;
            collider.radius = .49f;
            collider.direction = 1;
            collider.isTrigger = false;
            collider.enabled = true;
            Shape("Robe", PrimitiveType.Capsule, person, new Vector3(0, .8f, 0), new Vector3(.62f, .71f, .54f), robe, false);
            Box("ArmourChest", person, new Vector3(0, 1.18f, -.26f), new Vector3(.65f, .53f, .12f), armour, false);
            Box("Belt", person, new Vector3(0, .88f, -.29f), new Vector3(.67f, .11f, .10f), brass, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Sleeve" + side, person, new Vector3(side * .44f, 1.03f, 0), new Vector3(.24f, .61f, .35f), robe, false);
                Box("Shoulder" + side, person, new Vector3(side * .40f, 1.34f, -.02f), new Vector3(.31f, .12f, .4f), armour, false);
                Shape("Hand" + side, PrimitiveType.Sphere, person, new Vector3(side * .44f, .73f, -.045f), new Vector3(.17f, .2f, .17f), skin, false);
                Box("Boot" + side, person, new Vector3(side * .18f, .11f, -.07f), new Vector3(.22f, .22f, .36f), darkWood, false);
                Box("Eye" + side, person, new Vector3(side * .09f, 1.74f, -.201f), new Vector3(.061f, .025f, .022f), hair, false);
            }
            Shape("Head", PrimitiveType.Sphere, person, new Vector3(0, 1.7f, -.005f), new Vector3(.41f, .48f, .4f), skin, false);
            Box("Headgear", person, new Vector3(0, 1.98f, .015f), new Vector3(.43f, .21f, .4f), commander ? hair : armour, false);
            Box("Headband", person, new Vector3(0, 1.88f, -.198f), new Vector3(.43f, .055f, .038f), brass, false);
            if (commander)
                Box("Beard", person, new Vector3(0, 1.51f, -.215f), new Vector3(.18f, .22f, .057f), hair, false);
            return person;
        }

        static void Standard(string name, Transform root, Vector3 position, Material cloth)
        {
            var standard = Group(name, root, position);
            Box("Foot", standard, new Vector3(0, .09f, 0), new Vector3(.57f, .18f, .57f), darkWood);
            Box("Pole", standard, new Vector3(0, 1.8f, 0), new Vector3(.09f, 3.6f, .09f), wood);
            Box("Crossbar", standard, new Vector3(0, 3.45f, 0), new Vector3(1.43f, .075f, .075f), brass, false);
            Box("Cloth", standard, new Vector3(0, 2.71f, -.07f), new Vector3(1.18f, 1.44f, .04f), cloth, false);
            Box("Stripe", standard, new Vector3(0, 2.73f, -.10f), new Vector3(.085f, 1.17f, .022f), brass, false);
        }

        static void Lantern(string name, Transform root, Vector3 position)
        {
            var lantern = Group(name, root, position);
            Box("Post", lantern, new Vector3(0, 1.42f, 0), new Vector3(.12f, 2.84f, .12f), darkWood);
            Box("Arm", lantern, new Vector3(.28f, 2.8f, 0), new Vector3(.67f, .12f, .12f), wood);
            Shape("Shade", PrimitiveType.Cylinder, lantern, new Vector3(.53f, 2.40f, 0), new Vector3(.33f, .31f, .33f), paper, false);
            Box("Top", lantern, new Vector3(.53f, 2.73f, 0), new Vector3(.42f, .075f, .42f), darkWood, false);
            Box("Bottom", lantern, new Vector3(.53f, 2.07f, 0), new Vector3(.42f, .075f, .42f), darkWood, false);
            var lightRoot = Group("Light", lantern, new Vector3(.53f, 2.36f, -.13f));
            var light = Component<Light>(lightRoot.gameObject);
            Undo.RecordObject(light, UndoLabel);
            light.type = LightType.Point;
            light.color = new Color(1, .79f, .5f);
            light.intensity = 4;
            light.range = 12;
            light.shadows = LightShadows.None;
        }

        static void LoadMaterials()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "PrototypeMaterials");
            wood = Mat("WarmWood", new Color(.30f, .17f, .09f));
            darkWood = Mat("DarkWood", new Color(.12f, .075f, .046f));
            paper = Mat("Paper", new Color(.81f, .70f, .48f));
            brass = Mat("Brass", new Color(.65f, .42f, .15f));
            canvas = Mat("TentCanvas", new Color(.46f, .36f, .24f));
            skin = Mat("CaoCaoFace", new Color(.62f, .43f, .29f));
            hair = Mat("CaoCaoHair", new Color(.035f, .028f, .025f));
            armour = Mat("CaoCaoArmour", new Color(.075f, .11f, .14f));
            earth = Mat("NavalCampEarth", new Color(.25f, .235f, .19f));
            path = Mat("NavalCampPath", new Color(.38f, .34f, .245f));
            guardCloth = Mat("DuseokUniform", new Color(.22f, .28f, .32f));
            caimaoCloth = Mat("CaimaoUniform", new Color(.11f, .29f, .31f));
            zhangyunCloth = Mat("ZhangyunUniform", new Color(.38f, .12f, .105f));
            banner = Mat("CommandCrimson", new Color(.34f, .045f, .055f));
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        static Material Mat(string name, Color color)
        {
            string assetPath = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            GameObject found = null;
            foreach (var candidate in scene.GetRootGameObjects())
            {
                if (candidate.name != name) continue;
                if (found != null) throw new InvalidOperationException("Duplicate scene root: " + name);
                found = candidate;
            }
            return found;
        }

        static T SingleComponent<T>(Scene scene) where T : UnityEngine.Component
        {
            T found = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (found != null) throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " in the scene.");
                    found = candidate;
                }
            if (found == null) throw new InvalidOperationException("Missing " + typeof(T).Name + " in the scene.");
            return found;
        }

        static T Component<T>(GameObject go) where T : UnityEngine.Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        static Transform Child(Transform parent, string name)
        {
            Transform found = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name != name) continue;
                if (found != null) throw new InvalidOperationException("Duplicate generated object: " + parent.name + "/" + name);
                found = child;
            }
            return found;
        }

        static Transform Group(string name, Transform parent, Vector3 position)
        {
            var child = Child(parent, name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(child.gameObject, UndoLabel);
            }
            Undo.RecordObject(child, UndoLabel);
            child.localPosition = position;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool solid = true)
            => Shape(name, PrimitiveType.Cube, parent, position, scale, mat, solid, Quaternion.identity);

        static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material mat,
            bool solid, Quaternion rotation = default(Quaternion))
        {
            var child = Child(parent, name);
            GameObject go;
            if (child == null)
            {
                go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            }
            else go = child.gameObject;
            var renderer = go.GetComponent<Renderer>();
            var collider = go.GetComponent<Collider>();
            if (renderer == null || collider == null)
                throw new InvalidOperationException("Generated primitive was replaced by an incompatible object: " + name);
            Undo.RecordObject(go.transform, UndoLabel);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = rotation == default(Quaternion) ? Quaternion.identity : rotation;
            Undo.RecordObject(renderer, UndoLabel);
            renderer.sharedMaterial = mat;
            Undo.RecordObject(collider, UndoLabel);
            collider.enabled = solid;
            collider.isTrigger = false;
            return go;
        }
    }
}
