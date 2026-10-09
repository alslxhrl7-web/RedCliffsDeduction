using System;
using System.IO;

namespace RedCliffMystery.Investigation
{
    [Flags]
    public enum OrderFact
    {
        None = 0, Dispatch = 1, NoReceipt = 2, Procedure = 4, CourierClaim = 8,
        NoMessenger = 16, Movement = 32, StorageTrail = 64, Recovered = 128,
        Explanation = 256, Receipt = 512, Compared = 1024, RetainedCopy = 2048,
        Approval = 4096, Verified = 8192, Motive = 16384, Map = 32768,
        OptionalCareer = 65536, OptionalProtection = 131072, Reported = 262144,
        ClaimChallenged = 524288
    }

    [Flags]
    public enum OrderFollowUpFact
    {
        None = 0, Assignment = 1, Original = 2, Intake = 4, Outgoing = 8,
        Rejection = 16, Delivery = 32, DeliveryWitness = 64, BeforeCourier = 128,
        Submitted = 256, Knowing = 512, Held = 1024, DeploymentRequested = 2048,
        Reported = 4096, MetHan = 8192, ClerkConfirmation = 16384, Sequence = 32768,
        OriginalCompared = 65536
    }

    [Serializable]
    public sealed class OrderInvestigationState
    {
        public int facts;
        // Missing in older version-2 saves: defaults to no follow-up progress.
        public int followUp;
        public bool HasFollowUp(OrderFollowUpFact value) => ((OrderFollowUpFact)followUp & value) == value;
        public bool CanLocateChange => Has(OrderFact.Recovered) &&
            (HasFollowUp(OrderFollowUpFact.Intake) || HasFollowUp(OrderFollowUpFact.Outgoing));
        public bool CanIdentifySubmission => HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.BeforeCourier)
            && (HasFollowUp(OrderFollowUpFact.Original | OrderFollowUpFact.Intake)
                || HasFollowUp(OrderFollowUpFact.Outgoing | OrderFollowUpFact.ClerkConfirmation));
        public bool CanOrderReply => HasFollowUp(OrderFollowUpFact.Submitted | OrderFollowUpFact.Rejection)
            && (HasFollowUp(OrderFollowUpFact.Delivery) || HasFollowUp(OrderFollowUpFact.DeliveryWitness));
        public bool CanProveKnowledge => Has(OrderFact.Verified) && HasFollowUp(OrderFollowUpFact.Submitted | OrderFollowUpFact.Sequence);
        public bool TryRecordFollowUp(OrderFollowUpFact value)
        {
            int previous = followUp;
            followUp |= (int)value;
            try { Validate(); return true; }
            catch (InvalidDataException) { followUp = previous; return false; }
        }
        public OrderInvestigationState Copy() => new OrderInvestigationState { facts = facts, followUp = followUp };
        public bool Has(OrderFact value) => ((OrderFact)facts & value) == value;
        public bool CanRecover => Has(OrderFact.Dispatch | OrderFact.StorageTrail)
            && (Has(OrderFact.NoMessenger | OrderFact.CourierClaim) || Has(OrderFact.Movement));
        public bool CanChallenge => Has(OrderFact.CourierClaim)
            && (Has(OrderFact.NoMessenger) || Has(OrderFact.Movement));
        public bool CanCompare => Has(OrderFact.Recovered | OrderFact.Receipt);
        public bool CanVerify => Has(OrderFact.Compared | OrderFact.RetainedCopy | OrderFact.Approval);
        public bool CanExplainMotive => Has(OrderFact.Recovered | OrderFact.Procedure | OrderFact.Explanation);
        public bool ReadyToReport => Has(OrderFact.Verified | OrderFact.Map);
        public bool TryRecord(OrderFact value)
        {
            if (value == OrderFact.Recovered && !CanRecover
                || value == OrderFact.Compared && !CanCompare
                || value == OrderFact.Verified && !CanVerify
                || value == OrderFact.Motive && !CanExplainMotive
                || value == OrderFact.Map && !Has(OrderFact.Verified)
                || value == OrderFact.Reported && !ReadyToReport
                || value == OrderFact.ClaimChallenged && !CanChallenge
                || value == OrderFact.Explanation && !Has(OrderFact.Recovered)) return false;
            facts |= (int)value;
            return true;
        }
        public void Validate()
        {
            if (followUp < 0 || followUp > 131071
                || HasFollowUp(OrderFollowUpFact.BeforeCourier) && !CanLocateChange
                || HasFollowUp(OrderFollowUpFact.Submitted) && !CanIdentifySubmission
                || HasFollowUp(OrderFollowUpFact.Sequence) && !CanOrderReply
                || HasFollowUp(OrderFollowUpFact.Knowing) && !CanProveKnowledge
                || HasFollowUp(OrderFollowUpFact.Held) && !Has(OrderFact.Reported)
                || HasFollowUp(OrderFollowUpFact.DeploymentRequested) && !(HasFollowUp(OrderFollowUpFact.Held) && Has(OrderFact.Verified))
                || HasFollowUp(OrderFollowUpFact.Reported) && !HasFollowUp(OrderFollowUpFact.Knowing)
                || HasFollowUp(OrderFollowUpFact.OriginalCompared) && !(HasFollowUp(OrderFollowUpFact.Assignment | OrderFollowUpFact.Original) && Has(OrderFact.RetainedCopy)))
                throw new InvalidDataException("군령 후속 조사 기록의 선행 근거가 없습니다.");
            if (facts < 0 || facts > 1048575
                || Has(OrderFact.Recovered) && !CanRecover
                || Has(OrderFact.Compared) && !CanCompare
                || Has(OrderFact.Verified) && !CanVerify
                || Has(OrderFact.Motive) && !CanExplainMotive
                || Has(OrderFact.Map) && !Has(OrderFact.Verified)
                || Has(OrderFact.Reported) && !ReadyToReport
                || Has(OrderFact.Explanation) && !Has(OrderFact.Recovered)
                || Has(OrderFact.ClaimChallenged) && !CanChallenge)
                throw new InvalidDataException("군령 조사 기록의 선행 근거가 없습니다.");
        }
    }
}
