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
            MoveTo(tentSpawn);
        }

        public void EnterArchive()
        {
            MoveTo(archiveSpawn);
            tent.SetActive(false);
        }

        void MoveTo(Transform spawn)
        {
            player.TeleportTo(spawn);
        }
    }
}
