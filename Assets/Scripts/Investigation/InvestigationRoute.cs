using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class InvestigationRoute : MonoBehaviour
    {
        public FirstPersonInvestigator player;
        public Transform archiveArrival;
        public Transform navalArrival;
        public bool AtNavalCamp { get; private set; }
        public bool IsReady => player != null && archiveArrival != null && navalArrival != null;

        public void EnterArchive()
        {
            if (!IsReady) return;
            player.TeleportTo(archiveArrival);
            AtNavalCamp = false;
        }

        public void EnterNavalCamp()
        {
            if (!IsReady) return;
            player.TeleportTo(navalArrival);
            AtNavalCamp = true;
        }
    }
}
