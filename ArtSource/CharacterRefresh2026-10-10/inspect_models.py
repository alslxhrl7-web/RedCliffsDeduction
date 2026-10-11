import bpy, json
from pathlib import Path
from mathutils import Vector
root = Path(__file__).parent
bpy.ops.wm.read_factory_settings(use_empty=True)
reports = []
for path in sorted((root / 'ModelsOriginal').glob('*.fbx')):
    bpy.ops.import_scene.fbx(filepath=str(path))
    objs = list(bpy.context.selected_objects)
    entry = {'name':path.stem, 'objects':[]}
    for obj in objs:
        item = {'name':obj.name,'type':obj.type,'dimensions':list(obj.dimensions),'rotation':list(obj.rotation_euler),'scale':list(obj.scale)}
        if obj.type == 'MESH':
            points=[obj.matrix_world @ Vector(c) for c in obj.bound_box]
            item.update(vertices=len(obj.data.vertices),faces=len(obj.data.polygons),bounds=[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)],materials=[])
            for mat in obj.data.materials:
                item['materials'].append({'name':mat.name,'nodes':[{'type':n.type,'name':n.name,'image':n.image.name if n.type=='TEX_IMAGE' and n.image else None} for n in mat.node_tree.nodes] if mat.use_nodes else []})
        entry['objects'].append(item)
    reports.append(entry)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
(root / 'model-import-audit.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding='utf-8')
print('AUDIT_COMPLETE',len(reports),flush=True)
