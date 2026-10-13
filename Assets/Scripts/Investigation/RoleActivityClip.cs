using System.IO;
using UnityEngine;

namespace RedCliffMystery
{
    // Blender-authored poses + sampled action weights. Original mesh and UVs remain immutable.
    public sealed class RoleActivityClip
    {
        public struct Vertex
        {
            public int Index;
            public Vector3 Rest, A, B, NormalRest, NormalA, NormalB;
        }
        public int Profile { get; private set; }
        public int VertexCount { get; private set; }
        int fps;
        Vector3[] frames;
        Vertex[] vertices;
        static Vector3 ReadVector(BinaryReader r) => new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
        public static RoleActivityClip Read(TextAsset source)
        {
            using(var stream=new MemoryStream(source.bytes))
            using(var r=new BinaryReader(stream))
            {
                if(r.ReadInt32()!=0x49444352 || r.ReadInt32()!=1)throw new InvalidDataException("Invalid role activity: "+source.name);
                var clip=new RoleActivityClip {Profile=r.ReadInt32(),VertexCount=r.ReadInt32(),fps=r.ReadInt32()};
                int frameCount=r.ReadInt32(),count=r.ReadInt32();
                if(clip.VertexCount<1 || clip.fps<1 || frameCount<2 || frameCount>10000 || count<0 || count>clip.VertexCount
                    || stream.Length!=28L+frameCount*12L+count*76L)throw new InvalidDataException("Invalid activity dimensions");
                clip.frames=new Vector3[frameCount];
                for(int i=0;i<frameCount;i++)clip.frames[i]=ReadVector(r);
                clip.vertices=new Vertex[count];int previous=-1;
                for(int i=0;i<count;i++)
                {
                    var v=new Vertex {Index=r.ReadInt32(),Rest=ReadVector(r),A=ReadVector(r),B=ReadVector(r),
                        NormalRest=ReadVector(r),NormalA=ReadVector(r),NormalB=ReadVector(r)};
                    if(v.Index<=previous || v.Index>=clip.VertexCount)throw new InvalidDataException("Invalid activity index");
                    clip.vertices[i]=v;previous=v.Index;
                }
                return clip;
            }
        }
        public Vertex[] Bind(Matrix4x4 toMesh)
        {
            var result=new Vertex[vertices.Length];
            // Normal transform is inverse-transpose, including non-uniform scale.
            var normalMatrix=toMesh.inverse.transpose;
            for(int i=0;i<result.Length;i++)
            {
                var v=vertices[i];v.Rest=toMesh.MultiplyVector(v.Rest);v.A=toMesh.MultiplyVector(v.A);v.B=toMesh.MultiplyVector(v.B);
                v.NormalRest=normalMatrix.MultiplyVector(v.NormalRest).normalized;
                v.NormalA=normalMatrix.MultiplyVector(v.NormalA).normalized;v.NormalB=normalMatrix.MultiplyVector(v.NormalB).normalized;
                result[i]=v;
            }
            return result;
        }
        public Vector3 Sample(float time)
        {
            float frame=Mathf.Repeat(time+Profile*.83f,(frames.Length-1)/(float)fps)*fps;
            int a=Mathf.Min((int)frame,frames.Length-2);
            var weights=Vector3.Lerp(frames[a],frames[a+1],frame-a);
            weights=Vector3.Max(weights,Vector3.zero);
            return weights/Mathf.Max(.00001f,weights.x+weights.y+weights.z);
        }
        public static void Apply(Vector3[] positions,Vector3[] normals,Vertex[] offsets,Vector3 weights)
        {
            if(offsets==null)return;
            foreach(var v in offsets)
            {
                positions[v.Index]+=v.Rest*weights.x+v.A*weights.y+v.B*weights.z;
                normals[v.Index]=(v.NormalRest*weights.x+v.NormalA*weights.y+v.NormalB*weights.z).normalized;
            }
        }
    }
}
