using System;
using UnityEngine;

namespace RedCliffMystery
{
    // Reuses hidden finger vertices for clean wrist intersections, keeping motion vertex IDs stable.
    public sealed class WristSurface : MonoBehaviour
    {
        [Serializable] public struct Cut {public int index,a,b;public float t;}
        public Cut[] Cuts;
        public void Apply(Vector3[] positions,Vector3[] normals)
        {
            if(Cuts==null)return;
            foreach(var c in Cuts)
            {
                positions[c.index]=Vector3.LerpUnclamped(positions[c.a],positions[c.b],c.t);
                normals[c.index]=Vector3.LerpUnclamped(normals[c.a],normals[c.b],c.t).normalized;
            }
        }
    }
}
