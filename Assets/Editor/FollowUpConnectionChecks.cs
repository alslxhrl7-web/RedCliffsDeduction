using System;
using System.Reflection;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class FollowUpConnectionChecks
{
    public static void Verify()
    {
        var temporary = new GameObject("FollowUpGuardCheck");
        try
        {
            var session = temporary.AddComponent<InvestigationSession>();
            const string registry = "clue_household_registry_no_record";
            const string letter = "clue_wu_secret_letter_true_name";
            session.RecordVerifiedKeyEvidence(registry);
            session.RecordVerifiedKeyEvidence(letter);
            Require(!session.HasHouseholdRegistryEvidence && !session.HasWuLetterEvidence, "Premature clue award");
            Set(session, "registryScopeChecked", true);
            Set(session, "registryNamesChecked", true);
            foreach (var property in new[] { "ContradictionFound", "PersonnelRecordRead", "DutyRecordRead" }) SetProperty(session, property);
            session.RecordVerifiedKeyEvidence(registry);
            Require(session.HasHouseholdRegistryEvidence, "Registry path disconnected");
            foreach (var property in new[] { "SearchAuthorized", "LetterFound", "TokenMatched", "TokenOwnershipConfirmed" }) SetProperty(session, property);
            session.RecordVerifiedKeyEvidence(letter);
            Require(!session.HasWuLetterEvidence, "Letter awarded before interrogation");
            SetProperty(session, "InterrogationCompleted");
            session.RecordVerifiedKeyEvidence(letter);
            Require(session.HasWuLetterEvidence, "Interrogation path disconnected");
            Require(EndingRules.Resolve("설평", session.HasHouseholdRegistryEvidence, session.HasWuLetterEvidence) == InvestigationEnding.True, "True ending disconnected");
            foreach (var required in new[] { "SearchAuthorized", "LetterFound", "TokenMatched", "TokenOwnershipConfirmed", "InterrogationCompleted", "HasHouseholdRegistryEvidence" })
            {
                Set(session, "<HasWuLetterEvidence>k__BackingField", false);
                Set(session, "<" + required + ">k__BackingField", false);
                session.RecordVerifiedKeyEvidence(letter);
                Require(!session.HasWuLetterEvidence, "Missing requirement bypass: " + required);
                SetProperty(session, required);
            }
            session.RecordVerifiedKeyEvidence(letter);
            Require(session.HasWuLetterEvidence, "Repeat verification failed");
            var token = GameObject.Find("ArchiveRoom/PersonalEvidence/PersonalToken");
            var papers = GameObject.Find("ArchiveRoom/PersonalEvidence/PersonalPapers");
            Require(token != null && token.GetComponent<InvestigationTarget>().kind == InvestigationTarget.TargetKind.PersonalToken, "Token target missing");
            Require(papers != null && papers.GetComponent<InvestigationTarget>().kind == InvestigationTarget.TargetKind.PersonalPapers, "Papers target missing");
            Require(token.GetComponentInChildren<Collider>() != null && papers.GetComponentInChildren<Collider>() != null, "Interaction colliders missing");
            Debug.Log("FOLLOWUP_CONNECTIONS_VERIFIED: clue guards, true ending, scene targets; no Play Mode used.");
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }
    static void SetProperty(object target, string name) => Set(target, "<" + name + ">k__BackingField", true);
    static void Set(object target, string name, bool value) => typeof(InvestigationSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
