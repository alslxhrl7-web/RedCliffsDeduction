using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class IntroductionStage : MonoBehaviour
    {
        public FirstPersonInvestigator player;
        public Transform tentSpawn;
        public Transform archiveSpawn;
        public GameObject tent;

        public void Begin()
        {
            tent.SetActive(true);
            player.TeleportTo(tentSpawn);
        }

        public void EnterArchive()
        {
            player.TeleportTo(archiveSpawn);
            tent.SetActive(false);
        }
    }
}
