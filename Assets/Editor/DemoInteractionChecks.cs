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
        [MenuItem("적벽추리/데모 수정/보관함과 목패 배치 수정")]
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

        [MenuItem("적벽추리/데모 수정/조사 판정 검증")]
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
                    var target = UnityEngine.Object.FindObjectsByType<InvestigationTarget>(FindObjectsSortMode.None).Single(t => t.kind == kind);
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

        static object SessionCall(InvestigationSession session, string method, params object[] args) =>
            typeof(InvestigationSession).GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(session, args);
        static void Click(InvestigationSession session, string label)
        {
            var button = session.view.choices.GetComponentsInChildren<UnityEngine.UI.Button>()
                .Single(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text == label);
            Require(button.interactable, "Disabled choice: " + label);
            button.onClick.Invoke();
        }

        [MenuItem("적벽추리/데모 수정/미완료 조사 보고 검증")]
        public static void CheckEarlyReport()
        {
            Require(Application.isPlaying, "Enter Play Mode first");
            var session = UnityEngine.Object.FindAnyObjectByType<InvestigationSession>();
            var snapshot = (InvestigationSaveData)SessionCall(session, "CaptureSave");
            try
            {
                for (int heard = 0; heard < 3; heard++)
                {
                    var data = new InvestigationSaveData { version = 1, playerName = "검증 참모", inquiryIds = Array.Empty<string>(), inquiryTitles = Array.Empty<string>(), CaimaoTestimonyHeard = heard == 1, ZhangyunTestimonyHeard = heard == 2 };
                    SessionCall(session, "RestoreSave", data);
                    session.introductionStage.EnterArchive();
                    session.view.SetPrologueLayout(false);
                    session.ShowStageSelection();
                    Click(session, "현재 기록으로 조사 정리");
                    Require(session.view.body.text.Contains("미확인"), "Missing evidence presented as acquired");
                    Click(session, "조조에게 최종 보고");
                    Click(session, "특정 인물을 지목한다");
                    Click(session, "설평");
                    if (heard == 0)
                    {
                        Require(session.view.title.text == "제출할 기록 없음", "Invented report evidence");
                        Click(session, "후속 조치 선택");
                    }
                    else Click(session, "제출 선택 완료 (0건)");
                    Click(session, "현재 결과로 보고를 마친다");
                    Click(session, "이 내용으로 보고 완료");
                    Require(session.FinalReportCompleted && session.CurrentEnding == InvestigationEnding.Normal, "Early report did not reach normal ending");
                    Require(!session.ContradictionFound && !session.HasHouseholdRegistryEvidence && !session.HasWuLetterEvidence, "Unconfirmed evidence awarded");
                }
                Debug.Log("DEMO_EARLY_REPORT_PASS: zero clues, only Caimao, only Zhangyun; evidence and true-ending gates preserved");
            }
            finally
            {
                SessionCall(session, "RestoreSave", snapshot);
                session.introductionStage.EnterArchive();
                if (snapshot.navalCamp) session.route.EnterNavalCamp(); else session.route.EnterArchive();
                session.CloseModal();
            }
        }
        [MenuItem("적벽추리/데모 수정/배치 진단")]
        public static void Inspect()
        {
            var text = new StringBuilder();
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
