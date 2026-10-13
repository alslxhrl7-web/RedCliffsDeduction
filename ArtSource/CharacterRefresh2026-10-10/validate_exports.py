import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).parent/'BlenderRefined'
result=[]
for file in sorted((root.parents[2]/'Assets/Art/Characters/VarcoV3').glob('*/*.fbx')):
    if file.suffix not in ['.fbx','.glb']:continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if file.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(file))
    else:bpy.ops.import_scene.gltf(filepath=str(file))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    textures=[{'name':im.name,'size':list(im.size),'packed':bool(im.packed_file)} for im in bpy.data.images if im.type=='IMAGE']
    entry={'file':file.name,'meshes':len(meshes),'faces':sum(len(o.data.polygons) for o in meshes),'height':max(p.z for p in points)-min(p.z for p in points),'feet_z':min(p.z for p in points),'uv_layers':sum(len(o.data.uv_layers) for o in meshes),'textures':textures}
    entry['pass']=len(meshes)==1 and entry['uv_layers']>0 and 1.65<entry['height']<1.85 and abs(entry['feet_z'])<.0001 and len(textures)>=2 and all(t['size'][0]>0 for t in textures)
    result.append(entry)
(root/'export-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
print('EXPORT_VALIDATION',sum(r['pass'] for r in result),'/',len(result),flush=True)
assert len(result)==10 and all(r['pass'] for r in result)
