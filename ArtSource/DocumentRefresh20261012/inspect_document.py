import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
bpy.ops.wm.read_factory_settings(use_empty=True)
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
source_id=args[0] if args else 'e4c5e753-9208-4ae4-a335-4df7fce3e96f'
label=args[1] if len(args)>1 else 'ImportedInspection'
bpy.ops.wm.usd_import(filepath=str(OUT/'Source'/source_id/'model.usda'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)));center=(lo+hi)/2;size=max(hi-lo)
stats={'boundsMin':list(lo),'boundsMax':list(hi),'meshes':[{'name':o.name,'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'materials':[m.name for m in o.data.materials]}for o in meshes]}
(OUT/(label+'-inspection.json')).write_text(json.dumps(stats,indent=2),encoding='utf-8')
bpy.ops.object.camera_add(location=center+Vector((.8,-1.05,1.45))*size);cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=size*1.45;bpy.context.scene.camera=cam
for loc,power in [((1,-1,2),800),((-1,.2,1),450)]:
 bpy.ops.object.light_add(type='AREA',location=center+Vector(loc)*size);l=bpy.context.object;l.data.energy=power*size*size;l.data.shape='DISK';l.data.size=size*2;l.rotation_euler=(center-l.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.16,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
scene.render.filepath=str(OUT/'Preview'/(label+'-source.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(label+'.blend')))
print('DOCUMENT_INSPECT_COMPLETE',stats)
