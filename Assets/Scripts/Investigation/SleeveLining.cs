using UnityEngine;
namespace RedCliffMystery
{
    // The lining shares the existing animated sleeve and wrist boundary positions.
    public sealed class SleeveLining : MonoBehaviour
    {
        public MeshFilter Source;
        public int[] Bindings;
        public Transform OffsetFrame;
        public Vector3[] Offsets;
        Mesh original, animated;
        Vector3[] vertices;
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            var r=GetComponent<SkinnedMeshRenderer>();original=r.sharedMesh;
            animated=Instantiate(original);animated.MarkDynamic();vertices=animated.vertices;r.sharedMesh=animated;
        }
        public void Follow(MeshFilter source,Vector3[] points)
        {
            if(source!=Source||animated==null||Bindings.Length!=vertices.Length)return;
            for(int i=0;i<vertices.Length;i++)vertices[i]=transform.InverseTransformPoint(source.transform.TransformPoint(points[Bindings[i]])+OffsetFrame.TransformVector(Offsets[i]));
            animated.vertices=vertices;animated.RecalculateNormals();
        }
        void OnDisable()
        {
            if(animated==null)return;
            GetComponent<SkinnedMeshRenderer>().sharedMesh=original;Destroy(animated);animated=null;
        }
    }
}
