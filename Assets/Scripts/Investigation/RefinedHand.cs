using UnityEngine;

namespace RedCliffMystery
{
    // Wrist follows the animated body; authored finger shapes relax independently.
    public sealed class RefinedHand : MonoBehaviour
    {
        public MeshFilter Source;
        public int A, B, C;
        public int[] BoundaryBindings;
        public float MotionPhase;
        Mesh original,animated;
        Vector3[] buffer;
        SkinnedMeshRenderer surface;
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            surface=GetComponent<SkinnedMeshRenderer>();
            if(surface==null||surface.sharedMesh==null)return;
            original=surface.sharedMesh;animated=Instantiate(original);animated.MarkDynamic();buffer=animated.vertices;surface.sharedMesh=animated;
        }
        void OnDisable()
        {
            if(surface!=null&&original!=null)surface.sharedMesh=original;
            if(animated!=null)Destroy(animated);
            animated=null;buffer=null;
        }
        public static Quaternion Frame(Vector3 a, Vector3 b, Vector3 c)
        {
            var x=(b-a).normalized;
            var z=Vector3.Cross(x,c-a).normalized;
            return Quaternion.LookRotation(z,Vector3.Cross(z,x));
        }
        public void Follow(MeshFilter source,Vector3[] vertices)
        {
            if(source!=Source || A<0 || B<0 || C<0 || A>=vertices.Length || B>=vertices.Length || C>=vertices.Length)return;
            var t=source.transform;
            var a=t.TransformPoint(vertices[A]);var b=t.TransformPoint(vertices[B]);var c=t.TransformPoint(vertices[C]);
            if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-12f)return;
            transform.SetPositionAndRotation(a,Frame(a,b,c));
            if(animated!=null&&BoundaryBindings!=null&&BoundaryBindings.Length==buffer.Length)
            {
                for(int i=0;i<buffer.Length;i++)
                    if(BoundaryBindings[i]>=0&&BoundaryBindings[i]<vertices.Length)
                        buffer[i]=transform.InverseTransformPoint(t.TransformPoint(vertices[BoundaryBindings[i]]));
                animated.vertices=buffer;
                for(int finger=0;finger<animated.blendShapeCount;finger++)
                {
                    // Long rests between slight flexes; paper contact digits have zero authored delta.
                    float phase=Time.time*(.55f+finger*.047f)+MotionPhase+finger*.83f;
                    float pulse=Mathf.Max(0,Mathf.Sin(phase));
                    surface.SetBlendShapeWeight(finger,65*pulse*pulse);
                }
            }
        }
        public float BoundaryError(Vector3[] sourceVertices)
        {
            if(buffer==null||BoundaryBindings==null||BoundaryBindings.Length!=buffer.Length)return float.PositiveInfinity;
            float error=0;
            for(int i=0;i<buffer.Length;i++)
                if(BoundaryBindings[i]>=0)
                    error=Mathf.Max(error,Vector3.Distance(transform.TransformPoint(buffer[i]),Source.transform.TransformPoint(sourceVertices[BoundaryBindings[i]])));
            return error;
        }
    }
}
