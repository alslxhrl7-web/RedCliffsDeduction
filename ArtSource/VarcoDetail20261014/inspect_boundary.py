import bpy,bmesh,json
from pathlib import Path
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'CampStandard.blend'))
o=bpy.data.objects['CampStandard'];bm=bmesh.new();bm.from_mesh(o.data)
unseen={e for e in bm.edges if e.is_boundary};groups=[]
while unseen:
 todo=[unseen.pop()];edges=[]
 while todo:
  e=todo.pop();edges.append(e)
  for v in e.verts:
   for x in v.link_edges:
    if x in unseen:unseen.remove(x);todo.append(x)
 verts={v for e in edges for v in e.verts};groups.append({'edges':len(edges),'min':[min(v.co[i] for v in verts) for i in range(3)],'max':[max(v.co[i] for v in verts) for i in range(3)]})
print('BOUNDARIES',json.dumps(groups))
