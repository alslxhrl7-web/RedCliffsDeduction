import bpy,json
from pathlib import Path
p=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.usd_import(filepath=str(p/'ArtSource/VarcoDetail20261014/Source/CampStandard/model.usda'))
r=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH':r.append({'name':o.name,'dims':list(o.dimensions),'verts':len(o.data.vertices),'polys':len(o.data.polygons),'uv':[u.name for u in o.data.uv_layers], 'materials':[{'name':m.name,'nodes':[(n.name,n.type) for n in m.node_tree.nodes]} for m in o.data.materials]})
print('INSPECTION',json.dumps(r))
