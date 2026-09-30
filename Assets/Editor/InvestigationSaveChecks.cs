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
            loaded.inquiryIds = new[] { "career", "career", "registry" };
            bool rejected = false;
            try { InvestigationSaveStore.Write(path, loaded); } catch (InvalidDataException) { rejected = true; }
            if (!rejected || InvestigationSaveStore.Read(path).playerName != "두 번째 저장") throw new Exception("Invalid save replaced good file");
            File.WriteAllText(path, "{}");
            rejected = false;
            try { InvestigationSaveStore.Read(path); } catch (InvalidDataException) { rejected = true; }
            if (!rejected || session.PlayerName != "검증 참모") throw new Exception("Invalid load changed session");
            return "PASS: progress flags, Korean name, paid inquiries, overwrite, backup, invalid data rejection";
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (string file in new[] { path, path + ".tmp", path + ".bak" }) if (File.Exists(file)) File.Delete(file);
        }
    }
}
