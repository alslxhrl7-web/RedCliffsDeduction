"""Clip a distal hand at the wrist without changing the body vertex count.

Unused vertices from removed fingers store edge intersections. Runtime interpolation
keeps these intersections on the animated source edges.
"""
import numpy as np

def clip_hand(raw,uv,faces,selected,cut):
    crossing=[]
    for face in faces:
        for j in range(3):
            a,b=int(face[j]),int(face[(j+1)%3])
            if bool(selected[a])!=bool(selected[b]):crossing.append(tuple(sorted((a,b))))
    edges=sorted(set(crossing))
    protected={i for edge in edges for i in edge}
    kept_ids=set(faces[~np.all(selected[faces],axis=1)].ravel().tolist())
    available=[int(i) for i in np.flatnonzero(selected) if int(i) not in protected and int(i) not in kept_ids]
    if len(available)<len(edges):raise RuntimeError('Insufficient unused hand vertices for clipping')
    result=raw.copy();new_uv=uv.copy();records=[];lookup={}
    for dest,(a,b) in zip(available,edges):
        dz=raw[b,2]-raw[a,2]
        t=float(np.clip((cut-raw[a,2])/dz,0,1)) if abs(dz)>1e-8 else .5
        result[dest]=raw[a]*(1-t)+raw[b]*t;new_uv[dest]=uv[a]*(1-t)+uv[b]*t
        lookup[(a,b)]=dest;records.append(dict(index=dest,a=a,b=b,t=t))
    triangles=[];border=[]
    for face in faces:
        polygon=[]
        for j in range(3):
            a,b=int(face[j]),int(face[(j+1)%3]);inside=not selected[a];next_inside=not selected[b]
            if inside:polygon.append(a)
            if inside!=next_inside:polygon.append(lookup[tuple(sorted((a,b)))])
        for j in range(1,len(polygon)-1):triangles.append((polygon[0],polygon[j],polygon[j+1]))
        corners=[i for i in polygon if i in lookup.values()]
        if len(corners)==2:border.append(tuple(corners))
    return result,new_uv,np.array(triangles),records,border
