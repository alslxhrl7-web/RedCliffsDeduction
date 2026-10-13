using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    public sealed partial class InvestigationSession
    {
        string saveResumePoint = "field";
        string lastAutoSaveSignature, failedAutoSaveSignature;
        bool saveMenuOpen;
        int selectedSaveSlot = 1;
        InvestigationSaveData pendingManualSave;

        Dictionary<string, Action> SavePoints => new Dictionary<string, Action> {
            { "order_journal", ShowOrderJournal }, { "order_final", ResumeOrderFinal },
            { "field", CloseModal }, { "travel", ShowStageSelection }, { "tent", VisitCommandTent }
        };
        public static bool IsKnownSavePoint(string point) => string.IsNullOrEmpty(point)
            || point == "field" || point == "order_journal" || point == "order_final"
            || point == "travel" || point == "tent";

        void SetSavePoint(string point) { if (!saveMenuOpen) saveResumePoint = point; }
        void ResumeSavedPoint() {
            saveMenuOpen = false;
            pendingManualSave = null;
            if (SavePoints.TryGetValue(saveResumePoint, out var resume)) resume();
            else CloseModal();
        }
        void ReturnFromSaveMenu() => ResumeSavedPoint();

        bool HandleSaveShortcuts() {
            if (!Application.isFocused || Keyboard.current == null || !introductionFinished || dayTransitionActive || enteringName
                || FinalReportCompleted || Time.frameCount <= modalOpenedFrame) return false;
            if (Keyboard.current.f5Key.wasPressedThisFrame) { OpenManualSaveSlots(); return true; }
            if (Keyboard.current.f9Key.wasPressedThisFrame) {
                if (sequence != null) { view.SetFooterHint("대화를 마친 선택 화면에서 불러올 수 있습니다."); return true; }
                saveMenuOpen = true;
                ShowLoadSlots(ReturnFromSaveMenu);
                return true;
            }
            return false;
        }

        void OpenManualSaveSlots() {
            if (!introductionFinished || FinalReportCompleted || dayTransitionActive) return;
            if (sequence != null) { view.SetFooterHint("대화를 마친 선택 화면에서 저장할 수 있습니다. 조사 기록은 자동저장됩니다."); return; }
            if (pendingManualSave == null) pendingManualSave = CaptureSave();
            saveMenuOpen = true;
            SetModal("진행 저장", "저장 / 불러오기", "선택하기 전 현재 분기를 수동 슬롯에 보관할 수 있습니다.\n조사 기록과 진행은 별도 자동저장에 보관됩니다.\n자동저장은 수동 슬롯을 덮어쓰지 않습니다.");
            for (int slot = 1; slot <= 3; slot++) {
                int selected = slot;
                string path = InvestigationSaveStore.ManualPath(slot);
                ReadSavePreview(out var data, out var preview, path);
                view.AddChoice("슬롯 " + slot + "에 저장" + (data == null ? " · 빈 슬롯 또는 읽기 실패" : " · " + data.savedAt), () => {
                    selectedSaveSlot = selected;
                    if (File.Exists(path)) {
                        SetModal("덮어쓰기 확인", "슬롯 " + selected + "을 바꿀까요?", preview);
                        onEscape = OpenManualSaveSlots;
                        view.AddChoice("덮어쓰기", SaveInvestigation);
                        view.AddChoice("취소", OpenManualSaveSlots);
                    } else SaveInvestigation();
                });
            }
            view.AddChoice("불러올 기록 선택", () => ShowLoadSlots(OpenManualSaveSlots));
            view.AddChoice("원래 선택 화면으로", ReturnFromSaveMenu);
            onEscape = ReturnFromSaveMenu;
        }

        void ShowLoadSlots(Action back) {
            SetModal("진행 불러오기", "어느 기록으로 돌아갈까요?", "수동 저장 3개와 최근 자동저장 중에서 선택하세요.\n불러오기 전에 저장 내용과 날짜를 확인합니다.");
            for (int slot = 1; slot <= 3; slot++) {
                string path = InvestigationSaveStore.ManualPath(slot);
                bool readable = ReadSavePreview(out var data, out var preview, path);
                view.AddChoice("수동 슬롯 " + slot + (readable ? " · " + data.savedAt : " · 기록 없음 / 읽기 실패"),
                    () => ConfirmLoadPath(path, () => ShowLoadSlots(back)), readable || File.Exists(path));
            }
            bool hasAuto = ReadSavePreview(out var automatic, out var autoPreview, InvestigationSaveStore.AutoSavePath);
            view.AddChoice("최근 자동저장" + (hasAuto ? " · " + automatic.savedAt : " · 기록 없음 / 읽기 실패"),
                () => ConfirmLoadPath(InvestigationSaveStore.AutoSavePath, () => ShowLoadSlots(back)), hasAuto || File.Exists(InvestigationSaveStore.AutoSavePath));
            view.AddChoice("돌아가기", () => back());
            onEscape = back;
        }

        static string SaveSignature(InvestigationSaveData data) {
            string timestamp = data.savedAt;
            data.savedAt = null;
            string signature = JsonUtility.ToJson(data);
            data.savedAt = timestamp;
            return signature;
        }
        void LateUpdate() => AutoSaveProgress();
        void OnApplicationQuit() => AutoSaveProgress();
        void OnApplicationPause(bool paused) { if (paused) AutoSaveProgress(); }
        void AutoSaveProgress() {
            if (!Application.isPlaying || !introductionFinished || FinalReportCompleted || route == null || !route.IsReady) return;
            try {
                var data = CaptureSave();
                string signature = SaveSignature(data);
                if (signature == lastAutoSaveSignature || signature == failedAutoSaveSignature) return;
                try {
                    InvestigationSaveStore.Write(InvestigationSaveStore.AutoSavePath, data);
                    lastAutoSaveSignature = signature;
                    failedAutoSaveSignature = null;
                } catch (Exception e) {
                    failedAutoSaveSignature = signature;
                    Debug.LogWarning("자동저장 실패: " + e.Message, this);
                    view.SetFooterHint("자동저장에 실패했습니다. F5 수동 저장을 확인해 주세요.");
                }
            } catch (Exception e) { Debug.LogWarning("자동저장 상태 확인 실패: " + e.Message, this); }
        }

    }
}
