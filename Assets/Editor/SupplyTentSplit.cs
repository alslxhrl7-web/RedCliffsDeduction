using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    /// <summary>
    /// Moves the record clerk, document courier and resting soldier out of the naval camp into a separate
    /// supply tent zone. Caimao and Zhangyun stay in the naval camp.
    /// </summary>
    public static class SupplyTentSplit
    {
        const string ScenePath = "Assets/Scenes/Investigation.unity";
        const string UndoLabel = "Split supply tent from naval camp";
        const string MaterialFolder = "Assets/Art/PrototypeMaterials";
        static readonly string[] MovedNames = { "CareerRecordsStation", "DocumentCourier", "GamblingSoldier" };
        static readonly Vector3 ZoneOffset = new Vector3(-40f, 0f, 0f);
        static readonly Vector3 ZoneCenter = new Vector3(-40f, 0f, -23.5f);

        [MenuItem("적벽추리/보급 천막 분리")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save the Investigation scene first.");

            var routes = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "InvestigationRoutes");
            if (routes == null) throw new InvalidOperationException("InvestigationRoutes is required.");
            var camp = routes.transform.Find("NavalCamp");
            if (camp == null) throw new InvalidOperationException("NavalCamp is required.");
            if (routes.transform.Find("SupplyTent") != null)
                throw new InvalidOperationException("SupplyTent already exists; the split was already applied.");
            var route = routes.GetComponent<InvestigationRoute>();
            if (route == null) throw new InvalidOperationException("InvestigationRoute component is required.");

            var moved = MovedNames.Select(n => camp.Find(n)).ToArray();
            if (moved.Any(t => t == null)) throw new InvalidOperationException("Expected naval camp people were not found.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                var zone = new GameObject("SupplyTent").transform;
                Undo.RegisterCreatedObjectUndo(zone.gameObject, UndoLabel);
                zone.SetParent(routes.transform, false);

                var earth = MakeMaterial("SupplyTentEarth", new Color(.25f, .235f, .19f));
                var cloth = MakeMaterial("SupplyTentCloth", new Color(.42f, .27f, .18f));
                var timber = MakeMaterial("SupplyTentTimber", new Color(.32f, .24f, .15f));

                Primitive("SupplyGround", zone, ZoneCenter + Vector3.down * .15f, new Vector3(18, .3f, 18), earth);
                Primitive("SupplyCanopy", zone, ZoneCenter + Vector3.up * 2.7f, new Vector3(7, .12f, 5), cloth);
                foreach (int sx in new[] { -1, 1 })
                    foreach (int sz in new[] { -1, 1 })
                        Primitive("SupplyPost", zone,
                            ZoneCenter + new Vector3(sx * 3.3f, 1.35f, sz * 2.3f), new Vector3(.18f, 2.7f, .18f), timber);

                var arrival = new GameObject("SupplyArrival").transform;
                Undo.RegisterCreatedObjectUndo(arrival.gameObject, UndoLabel);
                arrival.SetParent(routes.transform, false);
                arrival.position = new Vector3(ZoneCenter.x, .08f, ZoneCenter.z + 4f);
                arrival.rotation = Quaternion.identity;

                foreach (var t in moved)
                {
                    Undo.SetTransformParent(t, zone, UndoLabel);
                    Undo.RecordObject(t, UndoLabel);
                    t.position += ZoneOffset;
                }

                Undo.RecordObject(route, UndoLabel);
                route.supplyArrival = arrival;
                EditorUtility.SetDirty(route);

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity could not save the Investigation scene.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("SUPPLY_TENT_SPLIT: clerk, courier and resting soldier moved to SupplyTent.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        static GameObject Primitive(string name, Transform parent, Vector3 center, Vector3 size, UnityEngine.Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static UnityEngine.Material MakeMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("The Universal Render Pipeline/Lit shader is required.");
            var material = new UnityEngine.Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
