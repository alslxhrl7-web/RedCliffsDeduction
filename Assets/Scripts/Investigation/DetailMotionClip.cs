using System;
using System.IO;
using UnityEngine;

namespace RedCliffMystery
{
    // Relative head/forearm rotations sampled in Blender from CC0 actions.
    // This deliberately leaves the NPC root and lower body under existing control.
    public sealed class DetailMotionClip
    {
        struct Influence { public int Index; public float Weight; }
        struct Group { public Vector3 Pivot; public Influence[] Vertices; }
        Group[] groups;
        Quaternion[,] frames;
        int fps;
        public int Profile { get; private set; }
        public int VertexCount { get; private set; }
        public static DetailMotionClip Read(TextAsset asset)
        {
            using(var stream=new MemoryStream(asset.bytes))
            using(var r=new BinaryReader(stream))
            {
                if(r.ReadInt32()!=0x54444352 || r.ReadInt32()!=1)throw new InvalidDataException("Invalid detail motion");
                var clip=new DetailMotionClip {Profile=r.ReadInt32(),VertexCount=r.ReadInt32(),fps=r.ReadInt32()};
                int count=r.ReadInt32(),groupCount=r.ReadInt32();
                if(clip.VertexCount<1 || clip.fps<1 || count<2 || count>10000 || groupCount!=3)throw new InvalidDataException("Invalid detail dimensions");
                clip.frames=new Quaternion[count,groupCount];clip.groups=new Group[groupCount];
                for(int i=0;i<count;i++)for(int g=0;g<groupCount;g++)
                {
                    var q=new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
                    float length=Quaternion.Dot(q,q);
                    if(float.IsNaN(length)||Mathf.Abs(length-1)>.01f)throw new InvalidDataException("Invalid detail rotation");
                    clip.frames[i,g]=q;
                }
                for(int g=0;g<groupCount;g++)
                {
                    var pivot=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());int n=r.ReadInt32();
                    if(n<0||n>clip.VertexCount)throw new InvalidDataException("Invalid influence count");
                    var indices=new Influence[n];int previous=-1;
                    for(int i=0;i<n;i++)
                    {
                        var v=new Influence {Index=r.ReadInt32(),Weight=r.ReadSingle()};
                        if(v.Index<=previous||v.Index>=clip.VertexCount||float.IsNaN(v.Weight)||v.Weight<0||v.Weight>1)throw new InvalidDataException("Invalid detail influence");
                        indices[i]=v;previous=v.Index;
                    }
                    clip.groups[g]=new Group {Pivot=pivot,Vertices=indices};
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing detail data");
                return clip;
            }
        }
        public void Apply(Vector3[] positions,Vector3[] normals,Matrix4x4 toVisual,Matrix4x4 toMesh,float time)
        {
            if(positions.Length!=VertexCount)return;
            float frame=Mathf.Repeat(time+Profile*.71f,(frames.GetLength(0)-1)/(float)fps)*fps;
            int a=Mathf.Min((int)frame,frames.GetLength(0)-2);float blend=frame-a;
            var normalToVisual=toMesh.transpose;var normalToMesh=toVisual.transpose;
            for(int g=0;g<groups.Length;g++)
            {
                var group=groups[g];var q=Quaternion.Slerp(frames[a,g],frames[a+1,g],blend);
                foreach(var v in group.Vertices)
                {
                    var p=toVisual.MultiplyPoint3x4(positions[v.Index]);
                    positions[v.Index]=toMesh.MultiplyPoint3x4(Vector3.Lerp(p,group.Pivot+q*(p-group.Pivot),v.Weight));
                    var n=normalToVisual.MultiplyVector(normals[v.Index]);
                    normals[v.Index]=normalToMesh.MultiplyVector(Vector3.Lerp(n,q*n,v.Weight)).normalized;
                }
            }
        }
    }
}
