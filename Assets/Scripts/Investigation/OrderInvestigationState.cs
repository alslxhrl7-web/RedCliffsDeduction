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

    [Serializable]
    public sealed class OrderInvestigationState
    {
        public int facts;
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
