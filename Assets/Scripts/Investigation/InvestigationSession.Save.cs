using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace RedCliffMystery.Investigation
{
    [Serializable]
    public sealed class InvestigationSaveData
    {
        public int version;
        public OrderInvestigationState order;
        public OrderFinalReport orderReport;
        public string playerName, savedAt;
        public string resumePoint;
        public int tutorialVersion, tutorialStep;
        public bool comparisonTipSeen;
        public string selectedLead; // Optional in existing version-2 saves.
        public bool navalCamp, supplyTent, commandTent;
        public string[] inquiryIds, inquiryTitles;
        public string PreviewSummary() {
            Validate();
            var clock = new InvestigationTime();
            clock.Restore(inquiryIds, inquiryTitles);
            return "이야기: " + "군령 분실 조사" + "\n참모: " + playerName + "\n조사 일정: " + clock.Label
                + "\n저장 장소: " + (commandTent ? "조조군 지휘 천막" : supplyTent ? "조조군 보급 천막" : navalCamp ? "조조군 수군 진영" : "조조군 문서고")
                + "\n저장 지점: " + ((orderReport != null && orderReport.stage > 0) ? "보고 선택" : string.IsNullOrEmpty(resumePoint) || resumePoint == "field" ? "현장 조사" : "조사 선택 화면")
                + "\n저장 시각: " + (string.IsNullOrWhiteSpace(savedAt) ? "시각 기록 없음" : savedAt)
                + (clock.DeadlineReached ? "\n기한 종료: 이미 시작한 조사 확인 또는 최종 보고로 이어집니다." : "");
        }
        public void Validate() {
            if (version == 1)
                throw new NotSupportedException("구버전 설평 사건 저장은 현재 군령 사건에서 불러올 수 없습니다. 원본 파일은 유지됩니다. 구버전 실행본에서 이어 주세요.");
            if (version != 2 || string.IsNullOrWhiteSpace(playerName) || playerName.Length > 16
                || inquiryIds == null || inquiryTitles == null || inquiryIds.Length != inquiryTitles.Length
                || inquiryIds.Length > InvestigationTime.Budget)
                throw new InvalidDataException("지원하지 않거나 손상된 저장 기록입니다.");
            if (order == null || !InvestigationSession.IsKnownSavePoint(resumePoint))
                throw new InvalidDataException("군령 조사 저장의 이야기 상태가 올바르지 않습니다.");
            order.Validate();
            if (orderReport != null) orderReport.Validate(order);
            if (resumePoint == "order_final" && (orderReport == null || orderReport.stage == 0))
                throw new InvalidDataException("최종 보고 초안이 없습니다.");
            var known = new HashSet<string> { "order_archive", "order_receipt", "order_procedure", "order_courier",
                "order_movement", "order_storage", "order_records", "order_rewrite", "order_reply" };
            foreach (var id in inquiryIds)
                if (id == null || !known.Remove(id)) throw new InvalidDataException("조사 기록이 손상되었습니다.");
            foreach (var title in inquiryTitles)
                if (string.IsNullOrWhiteSpace(title)) throw new InvalidDataException("조사 제목이 없습니다.");
        }
    }
    public static class InvestigationSaveStore
    {
        static string DirectoryPath {
            get {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetString("RedCliffMystery.TestSaveDirectory", Application.persistentDataPath);
#else
                return Application.persistentDataPath;
#endif
            }
        }
        public static string SavePath => Path.Combine(DirectoryPath, "order-investigation-save.json");
        public static string AutoSavePath => Path.Combine(DirectoryPath, "order-investigation-autosave.json");
        public static string ManualPath(int slot) {
            if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return slot == 1 ? SavePath : Path.Combine(DirectoryPath, "order-investigation-save-" + slot + ".json");
        }
        public static InvestigationSaveData Read(string path) {
            var data = JsonUtility.FromJson<InvestigationSaveData>(File.ReadAllText(path));
            if (data == null) throw new InvalidDataException("저장 기록이 비어 있습니다.");
            data.Validate();
            return data;
        }
        public static void Write(string path, InvestigationSaveData data) {
            data.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            try {
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                Read(temporary);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
    public sealed partial class InvestigationSession
    {
        InvestigationSaveData CaptureSave() => new InvestigationSaveData {
            version = 2, tutorialVersion = 1, tutorialStep = TutorialStep, comparisonTipSeen = comparisonTipSeen, order = OrderState.Copy(), orderReport = OrderReport.Copy(),
            playerName = PlayerName, resumePoint = saveResumePoint, selectedLead = SelectedLead,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), navalCamp = route.AtNavalCamp || route.OnConnectingRoad, supplyTent = route.AtSupplyTent, commandTent = !visitingCommandTent && !reportActive && route.AtCommandTent,
            inquiryIds = investigationTime.SavedIds, inquiryTitles = investigationTime.SavedTitles,
        };
        void ShowSaveMenu() {
            OpenManualSaveSlots();
        }
        void SaveInvestigation() {
            if (pendingManualSave == null) return;
            try {
                InvestigationSaveStore.Write(InvestigationSaveStore.ManualPath(selectedSaveSlot), pendingManualSave);
                SetModal("저장 완료", "슬롯 " + selectedSaveSlot + "에 저장했습니다", pendingManualSave.PreviewSummary());
            } catch (Exception e) {
                Debug.LogWarning("조사 저장 실패: " + e.Message);
                SetModal("저장 실패", "기록을 저장하지 못했습니다", "저장 공간과 폴더 접근 권한을 확인해 주세요.");
            }
            onEscape = ReturnFromSaveMenu;
            view.AddChoice("저장한 분기에서 계속", ReturnFromSaveMenu);
        }
        void ConfirmLoad(Action back) {
            ShowLoadSlots(back);
        }
        void ConfirmLoadPath(string path, Action back) {
            if (!ReadSavePreview(out var data, out var preview, path)) {
                SetModal("불러오기 안내", "저장 기록을 확인해 주세요", preview + "\n현재 진행은 유지됩니다.");
                onEscape = back;
                view.AddChoice("돌아가기", () => back());
                return;
            }
            SetModal("불러오기 확인", "이 저장 지점으로 돌아갈까요?", preview + "\n\n현재 진행을 이 기록으로 바꾸고 저장한 선택 지점에서 이어갑니다.");
            onEscape = back;
            view.AddChoice("불러오기", () => LoadInvestigation(back, data));
            view.AddChoice("취소", () => back());
        }
        static bool ReadSavePreview(out InvestigationSaveData data, out string preview, string path = null) {
            data = null;
            try {
                data = InvestigationSaveStore.Read(path ?? InvestigationSaveStore.SavePath);
                preview = data.PreviewSummary();
                return true;
            } catch (FileNotFoundException) { preview = "아직 저장한 조사 기록이 없습니다."; }
              catch (DirectoryNotFoundException) { preview = "아직 저장한 조사 기록이 없습니다."; }
              catch (NotSupportedException e) { preview = e.Message; }
              catch (Exception) { preview = "저장 기록을 읽을 수 없습니다. 파일 손상·버전 또는 접근 권한을 확인해 주세요."; }
            data = null;
            return false;
        }
        void LoadInvestigation(Action back, InvestigationSaveData data) {
            try {
                // Restore exactly the validated snapshot shown in the confirmation.
                data.Validate();
                if (route == null || !route.IsReady || introductionStage == null)
                    throw new InvalidOperationException("장소 연결이 준비되지 않았습니다.");
            } catch (Exception e) {
                Debug.LogWarning("조사 불러오기 실패: " + e.Message);
                SetModal("불러오기 실패", "저장 기록을 불러오지 못했습니다", "저장 기록이 없거나 손상되었거나 현재 버전과 맞지 않습니다. 현재 진행은 유지됩니다.");
                onEscape = back;
                view.AddChoice("돌아가기", () => back());
                return;
            }
            RestoreSave(data);
            introductionStage.EnterArchive();
            if(data.commandTent) { introductionStage.Begin(); route.RefreshPhysicalLocation(); }
            else if (data.supplyTent && route.SupplyTentReady) route.EnterSupplyTent();
            else if (data.navalCamp) route.EnterNavalCamp(); else route.EnterArchive();
            ResetTutorialMovement();
            view.SetPrologueLayout(false);
            view.SetPlayerIdentity(PlayerName, LocationName);
            RefreshInvestigationTime();
            UpdateObjective();
            ResumeSavedPoint();
            lastAutoSaveSignature = SaveSignature(CaptureSave());
        }
        void RestoreSave(InvestigationSaveData data) {
            data.Validate();
            OrderState = data.order == null ? new OrderInvestigationState() : data.order.Copy();
            OrderReport = data.orderReport == null ? new OrderFinalReport() : data.orderReport.Copy();
            if (dayTransition != null) dayTransition.Cancel();
            dayTransitionActive = false;
            PlayerName = data.playerName;
            TutorialStep = data.tutorialVersion == 1 ? Mathf.Clamp(data.tutorialStep,0,5) : 0;
            comparisonTipSeen = data.comparisonTipSeen;
            ResetTutorialMovement();
            SelectedLead = IsLeadAvailable(data.selectedLead) ? data.selectedLead : "";
            guidanceNotice = ""; noticeSeconds = 0; nextGuidanceRefresh = 0;
            investigationTime.Restore(data.inquiryIds, data.inquiryTitles);
            justStartedInquiry = null;
            pendingInquiryNotice = null;
            enteringName = false; introductionFinished = true;
            reportActive = false; visitingCommandTent = false;
            deadlineReportLocked = false;
            FinalReportCompleted = false; FinalReportSummary = null;
            CurrentEnding = InvestigationEnding.None;
            sequence = null; onSequenceComplete = null; onAdvance = null; onEscape = null;
            displayedTimeSlots = -1;
            saveResumePoint = string.IsNullOrEmpty(data.resumePoint) ? "field" : data.resumePoint;
            saveMenuOpen = false;
            pendingManualSave = null;
            failedAutoSaveSignature = null;
        }
    }
}
