using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class InvestigationSaveChecks
{
    public static string Verify()
    {
        if (Application.isPlaying) throw new Exception("Edit mode only");
        var root = new GameObject("Save checks");
        string path = Path.Combine(Path.GetTempPath(), "redcliff-save-check-" + Guid.NewGuid() + ".json");
        try {
            var session = root.AddComponent<InvestigationSession>();
            session.route = root.AddComponent<InvestigationRoute>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = (InvestigationSaveData)typeof(InvestigationSession).GetMethod("CaptureSave", flags).Invoke(session, null);
            foreach (var field in typeof(InvestigationSaveData).GetFields())
                if (field.FieldType == typeof(bool)) field.SetValue(data, true);
            data.playerName = "검증 참모";
            data.inquiryIds = new[] { "seol_initial", "career", "registry" };
            data.inquiryTitles = new[] { "설평", "경력", "호적" };
            string preview = data.PreviewSummary();
            if (!preview.Contains("검증 참모") || !preview.Contains("남은 조사 9회") || !preview.Contains("수군 진영") || !preview.Contains("첫째 날"))
                throw new Exception("Preview does not describe saved state");
            var expired = JsonUtility.FromJson<InvestigationSaveData>(JsonUtility.ToJson(data));
            expired.inquiryIds = new[] { "seol_initial", "duseok_initial", "caimao", "zhangyun", "career", "seol_reconfirm", "registry", "papers", "letter_interrogation", "relief_guard", "courier", "duseok_requestion" };
            expired.inquiryTitles = expired.inquiryIds;
            expired.navalCamp = false; expired.savedAt = null;
            preview = expired.PreviewSummary();
            if (!preview.Contains("남은 조사 0회") || !preview.Contains("열나흗날") || !preview.Contains("문서고") || !preview.Contains("기한 종료") || !preview.Contains("시각 기록 없음"))
                throw new Exception("Deadline/legacy preview failed");
            InvestigationSaveStore.Write(path, data);
            var loaded = InvestigationSaveStore.Read(path);
            typeof(InvestigationSession).GetMethod("RestoreSave", flags).Invoke(session, new object[] { loaded });
            var captured = (InvestigationSaveData)typeof(InvestigationSession).GetMethod("CaptureSave", flags).Invoke(session, null);
            foreach (var field in typeof(InvestigationSaveData).GetFields())
                if (field.FieldType == typeof(bool) && field.Name != "navalCamp" && !(bool)field.GetValue(captured))
                    throw new Exception("Flag lost: " + field.Name);
            var clock = (InvestigationTime)typeof(InvestigationSession).GetField("investigationTime", flags).GetValue(session);
            if (clock.Remaining != 9 || clock.Complete("career", "경력") || session.PlayerName != "검증 참모")
                throw new Exception("Clock/name restore failed");
            loaded.playerName = "두 번째 저장";
            InvestigationSaveStore.Write(path, loaded);
            if (InvestigationSaveStore.Read(path).playerName != "두 번째 저장" || InvestigationSaveStore.Read(path + ".bak").playerName != "검증 참모")
                throw new Exception("Replacement/backup failed");
            loaded.resumePoint = "report_confirm";
            loaded.report = new InvestigationReportSave {
                uncertain = true, deadlineLocked = true, fromNaval = true,
                suspect = "설평", request = "현재 결과로 보고를 마친다", page = 2, evidence = new[] { 0, 2 }
            };
            InvestigationSaveStore.Write(path, loaded);
            var branch = InvestigationSaveStore.Read(path);
            if (branch.resumePoint != "report_confirm" || branch.report.suspect != "설평"
                || !branch.report.deadlineLocked || branch.report.evidence.Length != 2)
                throw new Exception("Report branch round trip failed");
            branch.resumePoint = "unknown_branch";
            bool invalidBranchRejected = false;
            try { branch.Validate(); } catch (InvalidDataException) { invalidBranchRejected = true; }
            if (!invalidBranchRejected) throw new Exception("Unknown branch accepted");
            branch.resumePoint = "report_confirm"; branch.report = null;
            invalidBranchRejected = false;
            try { branch.Validate(); } catch (InvalidDataException) { invalidBranchRejected = true; }
            if (!invalidBranchRejected) throw new Exception("Report branch without report state accepted");
            if (InvestigationSaveStore.ManualPath(1) != InvestigationSaveStore.SavePath
                || InvestigationSaveStore.ManualPath(2) == InvestigationSaveStore.AutoSavePath
                || InvestigationSaveStore.ManualPath(3) == InvestigationSaveStore.ManualPath(2))
                throw new Exception("Save slot paths overlap or legacy path changed");
            loaded.inquiryIds = new[] { "career", "career", "registry" };
            bool rejected = false;
            try { InvestigationSaveStore.Write(path, loaded); } catch (InvalidDataException) { rejected = true; }
            if (!rejected || InvestigationSaveStore.Read(path).playerName != "두 번째 저장") throw new Exception("Invalid save replaced good file");
            File.WriteAllText(path, "{}");
            rejected = false;
            try { InvestigationSaveStore.Read(path); } catch (InvalidDataException) { rejected = true; }
            if (!rejected || session.PlayerName != "검증 참모") throw new Exception("Invalid load changed session");
            return "PASS: progress flags, Korean name, paid inquiries, overwrite, backup, report branch, slot separation, invalid data rejection";
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (string file in new[] { path, path + ".tmp", path + ".bak" }) if (File.Exists(file)) File.Delete(file);
        }
    }
}
