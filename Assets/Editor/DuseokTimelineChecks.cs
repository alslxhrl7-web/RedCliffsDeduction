using System;
using System.Reflection;
using UnityEngine;
using RedCliffMystery.Investigation;

public static class DuseokTimelineChecks
{
    public static void Verify()
    {
        var temporary = new GameObject("DuseokTimelineCheck");
        try
        {
            var session = temporary.AddComponent<InvestigationSession>();
            var requirements = new[] { "DuseokTestimonyHeard", "DocumentReturnedHeard", "MissingDiscoveryHeard", "GuardDepartureHeard", "GuardReturnHeard", "GamblingAdmissionHeard", "GamblingWitnessHeard", "ArchivePresenceHeard" };
            Require(!session.CanCompleteDuseokTimeline, "Empty investigation completed");
            foreach (var name in requirements) Set(session, name, true);
            Require(session.CanCompleteDuseokTimeline, "Completed evidence not accepted");
            foreach (var name in requirements)
            {
                Set(session, name, false);
                Require(!session.CanCompleteDuseokTimeline, "Missing statement bypass: " + name);
                Set(session, name, true);
            }
            var clock = new InvestigationTime();
            for (int i = 0; i < 6; i++) clock.Complete("base" + i, "조사");
            Require(clock.Complete("duseok_timeline", "두석 조사"), "Time not charged");
            Require(!clock.Complete("duseok_timeline", "두석 조사") && clock.ElapsedSlots == 7 && clock.Label.StartsWith("둘째 날 · 밤"), "Duplicate or wrong time charge");
            Require(EndingRules.Resolve("설평", true, true) == InvestigationEnding.True && !session.DuseokTimelineComplete, "Optional investigation changed ending rules");
            foreach (var pair in new[] {
                ("InvestigationRoutes/ArchiveEntrance/ReliefGuard", InvestigationTarget.TargetKind.ReliefGuard),
                ("InvestigationRoutes/NavalCamp/DocumentCourier", InvestigationTarget.TargetKind.DocumentCourier),
                ("InvestigationRoutes/NavalCamp/GamblingSoldier", InvestigationTarget.TargetKind.GamblingSoldier) })
            {
                var go = GameObject.Find(pair.Item1);
                Require(go != null && go.GetComponent<InvestigationTarget>().kind == pair.Item2 && go.GetComponentInChildren<Collider>() != null, "Witness connection missing: " + pair.Item1);
            }
            Debug.Log("DUSEOK_TIMELINE_VERIFIED: eight prerequisites, repeat time charge, optional ending rules, three scene witnesses.");
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }
    static void Set(object target, string property, bool value) => typeof(InvestigationSession).GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
