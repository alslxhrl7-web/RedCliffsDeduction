using System;
using System.IO;

namespace RedCliffMystery.Investigation
{
    // AccessRecord is a decoy: available once dispatch is known, but it proves neither alteration nor intent.
    // The true ending requires exactly the six evidence proofs (63); including the decoy breaks it.
    [Flags] public enum OrderReportProof { None = 0, Difference = 1, Original = 2, Submission = 4, Intent = 8, Impact = 16, Concealment = 32, AccessRecord = 64 }
    public enum OrderReportEnding { Bad, Normal, True }
    [Serializable]
    public sealed class OrderFinalReport
    {
        public const int EvidenceSetMask = 63;
        public int stage, suspect, proofs, action;
        public bool fromNaval;
        public OrderFinalReport Copy() => new OrderFinalReport { stage = stage, suspect = suspect, proofs = proofs, action = action, fromNaval = fromNaval };
        public bool Has(OrderReportProof proof) => ((OrderReportProof)proofs & proof) == proof;
        public static OrderReportProof Available(OrderInvestigationState state)
        {
            var result = OrderReportProof.None;
            if (state.Has(OrderFact.Compared)) result |= OrderReportProof.Difference;
            if (state.Has(OrderFact.Verified)) result |= OrderReportProof.Original;
            if (state.HasFollowUp(OrderFollowUpFact.Submitted)) result |= OrderReportProof.Submission;
            if (state.HasFollowUp(OrderFollowUpFact.Knowing)) result |= OrderReportProof.Intent;
            if (state.Has(OrderFact.Map)) result |= OrderReportProof.Impact;
            if (state.Has(OrderFact.Recovered | OrderFact.StorageTrail)) result |= OrderReportProof.Concealment;
            if (state.Has(OrderFact.Dispatch)) result |= OrderReportProof.AccessRecord;
            return result;
        }
        public void Validate(OrderInvestigationState state)
        {
            if (stage < 0 || stage > 2 || suspect < 0 || suspect > 6 || action < 0 || action > 3 || proofs < 0 || proofs > 127
                || (proofs & ~(int)Available(state)) != 0)
                throw new InvalidDataException("최종 보고에 확보하지 않은 근거나 잘못된 선택이 있습니다.");
        }
        // All six evidence proofs submitted and nothing else (the decoy excluded).
        public bool EvidenceComplete => proofs == EvidenceSetMask;
        public bool RestoresDeployment => action == 1 && Has(OrderReportProof.Original | OrderReportProof.Impact);
        public bool HoldsOrder => (action == 1 || action == 2) && Has(OrderReportProof.Difference | OrderReportProof.Original);
        public OrderReportEnding Resolve(OrderInvestigationState state)
        {
            Validate(state);
            // Only explicitly submitted proof counts. Old interim-report flags never count.
            if (suspect >= 2 || (suspect == 1 && !Has(OrderReportProof.Submission | OrderReportProof.Intent))) return OrderReportEnding.Bad;
            bool alteration = Has(OrderReportProof.Difference | OrderReportProof.Original) || Has(OrderReportProof.Submission | OrderReportProof.Intent);
            if (!alteration) return OrderReportEnding.Bad;
            return suspect == 1 && EvidenceComplete && HoldsOrder && RestoresDeployment ? OrderReportEnding.True : OrderReportEnding.Normal;
        }
    }
}
