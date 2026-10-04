using UnityEngine;

namespace RedCliffMystery.Investigation
{
    public sealed class ArchiveCurtain : MonoBehaviour
    {
        public Material cloth;
        public float Opening { get; private set; }
        Mesh[] meshes;
        Vector3[][] points;
        const int Columns=12, Rows=16;
        void Awake()
        {
            meshes=new Mesh[2]; points=new Vector3[2][];
            for(int side=0;side<2;side++)
            {
                var go=new GameObject(side==0?"LeftCanvas":"RightCanvas",typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(transform,false); go.GetComponent<MeshRenderer>().sharedMaterial=cloth;
                var mesh=new Mesh {name="Hemp curtain panel"}; mesh.MarkDynamic(); meshes[side]=mesh;
                points[side]=new Vector3[(Columns+1)*(Rows+1)];
                var uv=new Vector2[points[side].Length]; var triangles=new int[Columns*Rows*6]; int index=0;
                for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++) uv[y*(Columns+1)+x]=new Vector2((float)x/Columns,(float)y/Rows);
                for(int y=0;y<Rows;y++) for(int x=0;x<Columns;x++)
                { int a=y*(Columns+1)+x; triangles[index++]=a; triangles[index++]=a+Columns+1; triangles[index++]=a+1; triangles[index++]=a+1; triangles[index++]=a+Columns+1; triangles[index++]=a+Columns+2; }
                mesh.vertices=points[side]; mesh.uv=uv; mesh.triangles=triangles; go.GetComponent<MeshFilter>().sharedMesh=mesh;
            }
            SetOpening(0);
        }
        public void SetOpening(float value)
        {
            Opening=Mathf.Clamp01(value); if(meshes==null) return;
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++)
                {
                    float u=(float)x/Columns, v=(float)y/Rows;
                    // The upper seam stays attached; the loose fabric gathers below it.
                    float pull=Mathf.SmoothStep(0,1,Mathf.Clamp01((Opening-(side==0?0:.09f))/(side==0?1:.91f)));
                    float drape=Mathf.Sin(Mathf.Pow(1-v,.55f)*Mathf.PI*.5f);
                    float spread=pull*.92f*drape;
                    float px=sign*(1.11f-u*1.115f*(1-spread));
                    float py=.04f+v*2.48f+pull*.19f*(1-v)*u-.035f*Mathf.Sin(u*Mathf.PI)*(1-v);
                    float fold=Mathf.Sin(u*Mathf.PI*7+v*.65f+side*.8f);
                    float settle=Mathf.Sin(pull*Mathf.PI*3)*Mathf.Sin(pull*Mathf.PI);
                    float pz=.14f+fold*(.014f+.065f*spread)+pull*.22f*drape*Mathf.Sin(u*Mathf.PI*.8f)+settle*.028f*drape*u;
                    points[side][y*(Columns+1)+x]=new Vector3(px,py,pz);
                }
                meshes[side].vertices=points[side]; meshes[side].RecalculateNormals(); meshes[side].RecalculateBounds();
            }
        }
        void OnDestroy() { if(meshes!=null) foreach(var mesh in meshes) Destroy(mesh); }
    }
}
