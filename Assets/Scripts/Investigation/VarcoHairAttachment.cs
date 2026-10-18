using UnityEngine;

namespace RedCliffMystery
{
    // Hair follows the head's full idle displacement and never receives lip deformation.
    public sealed class VarcoHairAttachment : MonoBehaviour
    {
        // Keep facial deformation at its original height after the old topknot is reduced.
        public float BodyReferenceHeight = 1.78f;
    }
}
