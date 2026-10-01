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
        public string playerName, target, savedAt;
        public bool navalCamp;
        public bool WorkRouteHeard, AccessAuthorityHeard;
        public string[] inquiryIds, inquiryTitles;
        public bool DocumentInspected;
        public bool TestimonyHeard;
        public bool DuseokTestimonyHeard;
        public bool CaimaoTestimonyHeard;
        public bool ZhangyunTestimonyHeard;
        public bool MaintenanceOrderRead;
        public bool TestimonyReconfirmed;
        public bool ContradictionFound;
        public bool InvestigationReviewStarted;
        public bool DocumentReturnedHeard;
        public bool MissingDiscoveryHeard;
        public bool GuardDepartureHeard;
        public bool GuardReturnHeard;
        public bool GamblingAdmissionHeard;
        public bool GamblingWitnessHeard;
        public bool ArchivePresenceHeard;
        public bool DuseokTimelineComplete;
        public bool HasHouseholdRegistryEvidence;
        public bool HasWuLetterEvidence;
        public bool TokenObserved;
        public bool SearchAuthorized;
        public bool LetterFound;
        public bool TokenMatched;
        public bool LetterDenialHeard;
        public bool TokenOwnershipConfirmed;
        public bool InterrogationCompleted;
        public bool PersonnelRecordRead;
        public bool DutyRecordRead;
        public bool registryScopeChecked;
        public bool registryNamesChecked;
        public string PreviewSummary() {
            Validate();
            var clock = new InvestigationTime();
            clock.Restore(inquiryIds, inquiryTitles);
            return "참모: " + playerName + "\n조사 일정: " + clock.Label
                + "\n저장 장소: " + (navalCamp ? "조조군 수군 진영" : "조조군 문서고")
                + "\n저장 시각: " + (string.IsNullOrWhiteSpace(savedAt) ? "시각 기록 없음" : savedAt)
                + (clock.DeadlineReached ? "\n기한 종료: 이미 시작한 조사 확인 또는 최종 보고로 이어집니다." : "");
        }
        public void Validate() {
            if (version != 1 || string.IsNullOrWhiteSpace(playerName) || playerName.Length > 16
                || inquiryIds == null || inquiryTitles == null || inquiryIds.Length != inquiryTitles.Length
                || inquiryIds.Length > InvestigationTime.Budget)
                throw new InvalidDataException("지원하지 않거나 손상된 저장 기록입니다.");
            var known = new HashSet<string> { "seol_initial", "duseok_initial", "caimao", "zhangyun", "career", "seol_reconfirm", "registry", "papers", "letter_interrogation", "relief_guard", "courier", "duseok_requestion", "gambling_witness" };
            foreach (var id in inquiryIds)
                if (id == null || !known.Remove(id)) throw new InvalidDataException("조사 기록이 손상되었습니다.");
            foreach (var title in inquiryTitles)
                if (string.IsNullOrWhiteSpace(title)) throw new InvalidDataException("조사 제목이 없습니다.");
        }
    }
    public static class InvestigationSaveStore
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "investigation-save.json");
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
            version = 1, playerName = PlayerName, target = selectedInvestigationTarget,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), navalCamp = route.AtNavalCamp,
            inquiryIds = investigationTime.SavedIds, inquiryTitles = investigationTime.SavedTitles,
            WorkRouteHeard = WorkRouteHeard, AccessAuthorityHeard = AccessAuthorityHeard,
            DocumentInspected = DocumentInspected,
            TestimonyHeard = TestimonyHeard,
            DuseokTestimonyHeard = DuseokTestimonyHeard,
            CaimaoTestimonyHeard = CaimaoTestimonyHeard,
            ZhangyunTestimonyHeard = ZhangyunTestimonyHeard,
            MaintenanceOrderRead = MaintenanceOrderRead,
            TestimonyReconfirmed = TestimonyReconfirmed,
            ContradictionFound = ContradictionFound,
            InvestigationReviewStarted = InvestigationReviewStarted,
            DocumentReturnedHeard = DocumentReturnedHeard,
            MissingDiscoveryHeard = MissingDiscoveryHeard,
            GuardDepartureHeard = GuardDepartureHeard,
            GuardReturnHeard = GuardReturnHeard,
            GamblingAdmissionHeard = GamblingAdmissionHeard,
            GamblingWitnessHeard = GamblingWitnessHeard,
            ArchivePresenceHeard = ArchivePresenceHeard,
            DuseokTimelineComplete = DuseokTimelineComplete,
            HasHouseholdRegistryEvidence = HasHouseholdRegistryEvidence,
            HasWuLetterEvidence = HasWuLetterEvidence,
            TokenObserved = TokenObserved,
            SearchAuthorized = SearchAuthorized,
            LetterFound = LetterFound,
            TokenMatched = TokenMatched,
            LetterDenialHeard = LetterDenialHeard,
            TokenOwnershipConfirmed = TokenOwnershipConfirmed,
            InterrogationCompleted = InterrogationCompleted,
            PersonnelRecordRead = PersonnelRecordRead,
            DutyRecordRead = DutyRecordRead,
            registryScopeChecked = registryScopeChecked,
            registryNamesChecked = registryNamesChecked,
        };
        void ShowSaveMenu() {
            ReadSavePreview(out var stored, out var preview);
            SetModal("조사 기록 보관", "저장 / 불러오기", "수동 저장 슬롯 1\n\n" + preview + "\n\n불러오면 저장한 장소의 입구에서 이어갑니다.");
            onEscape = ShowPause;
            view.AddChoice("현재 조사 저장", () => {
                if (File.Exists(InvestigationSaveStore.SavePath)) {
                    ReadSavePreview(out var previous, out var previousPreview);
                    SetModal("저장 확인", "기존 기록을 덮어쓸까요?", "덮어쓸 저장 기록\n" + previousPreview + "\n\n이전 저장 기록이 현재 조사로 바뀝니다.");
                    onEscape = ShowSaveMenu;
                    view.AddChoice("덮어쓰기", SaveInvestigation);
                    view.AddChoice("취소", ShowSaveMenu);
                } else SaveInvestigation();
            });
            view.AddChoice("저장한 조사 불러오기", () => ConfirmLoad(ShowSaveMenu));
            view.AddChoice("돌아가기", ShowPause);
        }
        void SaveInvestigation() {
            if (!introductionFinished || reportActive || FinalReportCompleted || deadlineReportLocked) return;
            try {
                InvestigationSaveStore.Write(InvestigationSaveStore.SavePath, CaptureSave());
                SetModal("저장 완료", "조사 기록을 보관했습니다", PlayerName + " · " + InvestigationDate + "\n" + LocationName);
            } catch (Exception e) {
                Debug.LogWarning("조사 저장 실패: " + e.Message);
                SetModal("저장 실패", "기록을 저장하지 못했습니다", "저장 공간과 폴더 접근 권한을 확인해 주세요.");
            }
            onEscape = ShowSaveMenu;
            view.AddChoice("돌아가기", ShowSaveMenu);
        }
        void ConfirmLoad(Action back) {
            if (!ReadSavePreview(out var data, out var preview)) {
                SetModal("불러오기 안내", "저장 기록을 확인해 주세요", preview + "\n현재 진행은 유지됩니다.");
                onEscape = back;
                view.AddChoice("돌아가기", () => back());
                return;
            }
            SetModal("불러오기 확인", "저장한 조사로 돌아갈까요?", preview + "\n\n저장하지 않은 진행은 사라집니다. 저장 당시 장소의 입구에서 이어집니다.");
            onEscape = back;
            view.AddChoice("불러오기", () => LoadInvestigation(back, data));
            view.AddChoice("취소", () => back());
        }
        static bool ReadSavePreview(out InvestigationSaveData data, out string preview) {
            data = null;
            try {
                data = InvestigationSaveStore.Read(InvestigationSaveStore.SavePath);
                preview = data.PreviewSummary();
                return true;
            } catch (FileNotFoundException) { preview = "아직 저장한 조사 기록이 없습니다."; }
              catch (DirectoryNotFoundException) { preview = "아직 저장한 조사 기록이 없습니다."; }
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
            if (data.navalCamp) route.EnterNavalCamp(); else route.EnterArchive();
            view.SetPrologueLayout(false);
            view.SetPlayerIdentity(PlayerName, LocationName);
            RefreshInvestigationTime();
            UpdateObjective();
            CloseModal();
        }
        void RestoreSave(InvestigationSaveData data) {
            data.Validate();
            WorkRouteHeard = data.WorkRouteHeard; AccessAuthorityHeard = data.AccessAuthorityHeard;
            DocumentInspected = data.DocumentInspected;
            TestimonyHeard = data.TestimonyHeard;
            DuseokTestimonyHeard = data.DuseokTestimonyHeard;
            CaimaoTestimonyHeard = data.CaimaoTestimonyHeard;
            ZhangyunTestimonyHeard = data.ZhangyunTestimonyHeard;
            MaintenanceOrderRead = data.MaintenanceOrderRead;
            TestimonyReconfirmed = data.TestimonyReconfirmed;
            ContradictionFound = data.ContradictionFound;
            InvestigationReviewStarted = data.InvestigationReviewStarted;
            DocumentReturnedHeard = data.DocumentReturnedHeard;
            MissingDiscoveryHeard = data.MissingDiscoveryHeard;
            GuardDepartureHeard = data.GuardDepartureHeard;
            GuardReturnHeard = data.GuardReturnHeard;
            GamblingAdmissionHeard = data.GamblingAdmissionHeard;
            GamblingWitnessHeard = data.GamblingWitnessHeard;
            ArchivePresenceHeard = data.ArchivePresenceHeard;
            DuseokTimelineComplete = data.DuseokTimelineComplete;
            HasHouseholdRegistryEvidence = data.HasHouseholdRegistryEvidence;
            HasWuLetterEvidence = data.HasWuLetterEvidence;
            TokenObserved = data.TokenObserved;
            SearchAuthorized = data.SearchAuthorized;
            LetterFound = data.LetterFound;
            TokenMatched = data.TokenMatched;
            LetterDenialHeard = data.LetterDenialHeard;
            TokenOwnershipConfirmed = data.TokenOwnershipConfirmed;
            InterrogationCompleted = data.InterrogationCompleted;
            PersonnelRecordRead = data.PersonnelRecordRead;
            DutyRecordRead = data.DutyRecordRead;
            registryScopeChecked = data.registryScopeChecked;
            registryNamesChecked = data.registryNamesChecked;
            PlayerName = data.playerName;
            selectedInvestigationTarget = data.target;
            investigationTime.Restore(data.inquiryIds, data.inquiryTitles);
            enteringName = false; introductionFinished = true;
            reportActive = false; visitingCommandTent = false;
            deadlineReportLocked = false; skipDeadlinePrompt = false;
            FinalReportCompleted = false; FinalReportSummary = null;
            CurrentEnding = InvestigationEnding.None;
            executionTransition = false; normalEndingTransition = false;
            reportSuspect = null; reportRequest = null; reportUncertain = false;
            submittedEvidence.Clear(); reportEvidence.Clear();
            sequence = null; onSequenceComplete = null; onAdvance = null; onEscape = null;
            displayedTimeSlots = -1;
        }
    }
}
