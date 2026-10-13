using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedCliffMystery.Investigation;

namespace RedCliffMystery.EditorTools
{
    public static class DemoInteractionChecks
    {
        public static void RepairLayout()
        {
            if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != FirstInvestigationBuilder.ScenePath)
                throw new InvalidOperationException("Open Investigation in Edit Mode first.");
            var tray = GameObject.Find("ArchiveRoom/MissingDocumentTray").transform;
            Undo.RecordObjects(tray.GetComponentsInChildren<Transform>(), "Fix investigation props");
            tray.position = new Vector3(0, 1.08f, 2.4f);
            tray.Find("TrayBack").position = new Vector3(0, 1.24f, 2.82f);
            foreach (var side in new[] { tray.Find("TrayLeft"), tray.Find("TrayRight") })
            {
                side.position = new Vector3(side.position.x, 1.24f, 2.37f);
                var scale = side.localScale;
                scale.z = .84f / tray.lossyScale.z;
                side.localScale = scale;
            }
            var token = GameObject.Find("ArchiveRoom/PersonalEvidence/PersonalToken").transform;
            Undo.RecordObject(token, "Move token to front of desk");
            token.localPosition = new Vector3(3.95f, 1.08f, 4.12f);
            Physics.SyncTransforms();
            var desk = GameObject.Find("ArchiveRoom").GetComponentsInChildren<BoxCollider>()
                .Single(c => c.name == "WritingDesk" && Mathf.Abs(c.transform.position.x) < .1f);
            Require(tray.GetComponent<Collider>().bounds.min.y > desk.bounds.max.y, "Tray intersects desk");
            EditorSceneManager.MarkSceneDirty(tray.gameObject.scene);
            EditorSceneManager.SaveScene(tray.gameObject.scene);
            Debug.Log("DEMO_LAYOUT_FIXED: tray clears desk; rim joints aligned; token moved forward");
        }

        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        public static void CheckAim()
        {
            var mover = UnityEngine.Object.FindAnyObjectByType<FirstPersonInvestigator>();
            var camera = mover.viewCamera.transform;
            Vector3 position = camera.position;
            Quaternion rotation = camera.rotation;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Temporary interaction test wall";
            blocker.transform.localScale = new Vector3(2, 2, .15f);
            blocker.SetActive(false);
            int count = 0;
            try
            {
                foreach (var kind in new[] { InvestigationTarget.TargetKind.MissingDocument, InvestigationTarget.TargetKind.PersonalToken, InvestigationTarget.TargetKind.MaintenanceOrder, InvestigationTarget.TargetKind.PersonalPapers })
                {
                    var target = UnityEngine.Object.FindObjectsByType<InvestigationTarget>().Single(t => t.kind == kind);
                    var collider = target.GetComponentsInChildren<Collider>().First(c => c.enabled && (c.name == "WoodenToken" || c.name == "MaintenanceOrder" || c.name == "Case" || c.name == "MissingDocumentTray"));
                    var point = collider.bounds.center;
                    foreach (float x in new[] { -.6f, 0, .6f })
                    {
                        camera.position = point + new Vector3(x, .6f, -1.6f);
                        camera.LookAt(point);
                        Physics.SyncTransforms();
                        Require(mover.FindTarget() == target, "Prompt missing: " + kind + " at " + x);
                        count++;
                    }
                    blocker.transform.position = (camera.position + point) * .5f;
                    blocker.transform.rotation = Quaternion.LookRotation(point - camera.position);
                    blocker.SetActive(true);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == null, "Interaction through wall: " + kind);
                    blocker.SetActive(false);
                    camera.position = point + new Vector3(0, .6f, -4.5f);
                    camera.LookAt(point);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == null, "Interaction exceeds range: " + kind);
                    count += 2;
                }
                // An isolated thin clue proves aim forgiveness and occlusion independently of the table.
                var clue = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    clue.transform.position = new Vector3(0, 20, 0);
                    clue.transform.localScale = new Vector3(.2f, .05f, .3f);
                    var target = clue.AddComponent<InvestigationTarget>();
                    camera.position = new Vector3(0, 20.08f, -2);
                    camera.rotation = Quaternion.identity;
                    Physics.SyncTransforms();
                    Require(!Physics.Raycast(camera.position, camera.forward, 3), "Test must narrowly miss the clue");
                    Require(mover.FindTarget() == target, "Small aim margin failed");
                    blocker.transform.position = new Vector3(0, 20, -1);
                    blocker.transform.rotation = Quaternion.identity;
                    blocker.SetActive(true);
                    Physics.SyncTransforms();
                    Require(mover.FindTarget() == null, "Aim margin bypassed wall");
                    count += 2;
                }
                finally { UnityEngine.Object.DestroyImmediate(clue); }
                Debug.Log("DEMO_AIM_PASS cases=" + count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(blocker);
                camera.SetPositionAndRotation(position, rotation);
                Physics.SyncTransforms();
            }
        }

        public static void Inspect()
        {
            var text = new StringBuilder();
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
            {
                string path = c.name;
                for (var p = c.transform.parent; p != null; p = p.parent) path = p.name + "/" + path;
                if (path.Contains("Personal") || path.Contains("Maintenance") || path.Contains("Tray") || path.Contains("Desk"))
                    text.AppendLine(path + " active=" + c.gameObject.activeInHierarchy + " enabled=" + c.enabled + " trigger=" + c.isTrigger + " bounds=" + c.bounds + " target=" + c.GetComponentInParent<InvestigationTarget>());
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/DemoInteractionInspection.txt", text.ToString());
            Debug.Log("DEMO_INSPECTION_COMPLETE");
        }
    }
}
