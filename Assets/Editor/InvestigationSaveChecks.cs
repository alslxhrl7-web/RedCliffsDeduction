using System;
using System.IO;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class InvestigationSaveChecks
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static string Verify()
    {
        var state = new OrderInvestigationState();
        state.TryRecord(OrderFact.Dispatch);
        var data = new InvestigationSaveData {
            version = 2, playerName = "검증 참모", order = state, orderReport = new OrderFinalReport(),
            resumePoint = "order_journal", supplyTent = true,
            inquiryIds = new[] { "order_archive" }, inquiryTitles = new[] { "반출 기록" }
        };
        string json = JsonUtility.ToJson(data);
        var restored = JsonUtility.FromJson<InvestigationSaveData>(json);
        restored.Validate();
        Require(restored.order.Has(OrderFact.Dispatch) && restored.supplyTent, "Current progress lost");
        Require(restored.PreviewSummary().Contains("남은 조사 11회"), "Budget preview changed");
        var olderV2 = JsonUtility.FromJson<InvestigationSaveData>(json.Substring(0, json.Length - 1)
            + ",\"HasWuLetterEvidence\":true,\"target\":\"설평\",\"report\":null}");
        olderV2.Validate();
        Require(olderV2.order.facts == data.order.facts && olderV2.resumePoint == data.resumePoint, "Existing v2 incompatible");
        foreach (string point in new[] { "field", "order_journal", "travel", "tent", null }) {
            var copy = JsonUtility.FromJson<InvestigationSaveData>(json); copy.resumePoint = point; copy.Validate();
        }
        foreach (string point in new[] { "registry", "report_confirm", "deadline", "unknown" }) {
            var copy = JsonUtility.FromJson<InvestigationSaveData>(json); copy.resumePoint = point; Reject(copy, false);
        }
        var duplicate = JsonUtility.FromJson<InvestigationSaveData>(json);
        duplicate.inquiryIds = new[] { "order_archive", "order_archive" };
        duplicate.inquiryTitles = new[] { "반출", "반출" }; Reject(duplicate, false);
        var legacy = JsonUtility.FromJson<InvestigationSaveData>(json); legacy.version = 1; Reject(legacy, true);
        var unknown = JsonUtility.FromJson<InvestigationSaveData>(json); unknown.version = 99; Reject(unknown, false);
        var draft = JsonUtility.FromJson<InvestigationSaveData>(json);
        draft.resumePoint = "order_final"; draft.orderReport.stage = 1; draft.Validate();
        draft.orderReport.proofs = 63; Reject(draft, false);
        string folder = Path.GetFullPath("Temp/SaveChecks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "current.json"), oldPath = Path.Combine(folder, "legacy.json");
        InvestigationSaveStore.Write(path, data);
        Require(InvestigationSaveStore.Read(path).order.facts == state.facts, "File round trip failed");
        data.playerName = "두 번째 참모"; InvestigationSaveStore.Write(path, data);
        Require(File.Exists(path + ".bak") && InvestigationSaveStore.Read(path).playerName == data.playerName, "Atomic replacement failed");
        string legacyJson = JsonUtility.ToJson(legacy); File.WriteAllText(oldPath, legacyJson);
        try { InvestigationSaveStore.Read(oldPath); throw new Exception("Legacy file accepted"); }
        catch (NotSupportedException) { }
        Require(File.ReadAllText(oldPath) == legacyJson, "Legacy file altered");
        return "PASS current/v2 compatibility, legacy rejection, validation, atomic write and file preservation";
    }
    static void Reject(InvestigationSaveData data, bool legacy)
    {
        try { data.Validate(); }
        catch (NotSupportedException e) when (legacy) { Require(e.Message.Contains("구버전"), "Explanation missing"); return; }
        catch (InvalidDataException) when (!legacy) { return; }
        throw new Exception("Invalid save accepted");
    }
}
