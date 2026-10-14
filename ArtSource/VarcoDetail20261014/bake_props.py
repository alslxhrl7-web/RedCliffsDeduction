"""VARCO USD source -> editable master, conservative cleanup, four baked game maps.
Run in the existing Blender window using the supplied job wrapper.
Existing master/output is protected unless --replace-generated is explicitly passed.
"""
import bpy,bmesh,json,sys,math
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent; PROJECT=ROOT.parent.parent
args=globals().get('JOB_ARGS')
if args is None:args=sys.argv[sys.argv.index('--')+1:]
name=args[0]
master=ROOT/(name+('Quality' if '--quality-pass' in args else ('Crafted' if '--crafted' in args else ''))+'.blend'); OUT=PROJECT/'Assets/Art/Environment/VarcoDetailBaked';OUT.mkdir(parents=True,exist_ok=True)
if (master.exists() or (OUT/(name+'.fbx')).exists()) and '--replace-generated' not in args:raise RuntimeError('Existing authoring/output protected')
if '--keep-scene' in args:
 if bpy.app.background or bpy.context.window is None:raise RuntimeError('Use the existing visible Blender window')
 scene=bpy.data.scenes.new(name+'_Workbench')
 if hasattr(scene,'varco3d_props'):
  scene.varco3d_props.asset_dir=str(PROJECT/'ArtSource/VarcoDetail20261014/Source/BridgeIncoming')
  scene.varco3d_props.server_port=5324
 bpy.context.window.scene=scene
else:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
input_path=ROOT/'Source'/name/'model.usda'
if name=='CampFence':input_path=ROOT/'Source/BridgeIncoming/b93615db-9f72-4483-b5d1-3c1831b9a123/model.usda'
bpy.ops.wm.usd_import(filepath=str(input_path))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.bake.margin=12;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
source=bpy.data.collections.new('EDITABLE_VARCO_SOURCE');scene.collection.children.link(source)
objs=[o for o in scene.objects if o.type=='MESH']
for o in objs:
 # Bake parent transforms before detaching; USD Y-up becomes Blender Z-up.
 matrix=o.matrix_world.copy();o.parent=None;o.matrix_world=matrix
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 backup=o.copy();backup.data=o.data.copy();source.objects.link(backup);backup.name=name+'_Original';backup.hide_render=True;backup.hide_set(True)
bpy.ops.object.select_all(action='DESELECT')
for o in objs:o.select_set(True)
bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
before=len(o.data.vertices);bm=bmesh.new();bm.from_mesh(o.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000015)
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
# Keep folds and joinery; one low-strength pass removes small reconstruction spikes.
mod=o.modifiers.new('Restrained surface relaxation','SMOOTH');mod.factor=.12;mod.iterations=1;bpy.ops.object.modifier_apply(modifier=mod.name)
points=np.array([v.co[:] for v in o.data.vertices]);dims=points.max(0)-points.min(0)
if '--finish-props' in args and name in ('CanvasDetails','FieldTable','ArchiveShelf'):
 # Generated furniture can arrive diagonal to its axes. Align its width before fitting in Unity.
 center=points[:,:2].mean(0);xy=points[:,:2]-center
 values,vectors=np.linalg.eigh(np.cov(xy.T));axis=vectors[:,-1]
 if axis[0]<0:axis=-axis
 rotation=np.array([[axis[0],-axis[1]],[axis[1],axis[0]]])
 points[:,:2]=xy@rotation
 for v,p in zip(o.data.vertices,points):v.co=Vector(p)
 dims=points.max(0)-points.min(0)
target={'CampStandard':3.0,'CanvasDetails':2.1,'SupplyLoad':1.0,'FieldTable':.85,'ArchiveShelf':2.1,'CampFence':1.5}[name]
scale=target/dims[2];shift=np.array([(points[:,0].min()+points[:,0].max())/2,(points[:,1].min()+points[:,1].max())/2,points[:,2].min()])
for v in o.data.vertices:v.co=Vector((np.array(v.co)-shift)*scale)
if name=='CampStandard':
 # Relax the reconstructed cloth fringe without rounding the timber joinery.
 group=o.vertex_groups.new(name='ClothFringeRefinement')
 xmin=min(v.co.x for v in o.data.vertices)
 cloth=[v.index for v in o.data.vertices if v.co.x>xmin+.34 and v.co.z<2.58]
 group.add(cloth,1.0,'REPLACE')
 m=o.modifiers.new('Soften frayed reconstruction edges','SMOOTH');m.factor=.48;m.iterations=4;m.vertex_group=group.name
 bpy.ops.object.modifier_apply(modifier=m.name)
 # The apparently open post end is a closed concavity: flatten the tiny top recess.
 for v in o.data.vertices:
  if v.co.z>2.94:v.co.z=2.975
if name=='CampFence':
 # Remove reconstruction chatter while preserving the rope silhouette and source UV.
 m=o.modifiers.new('Reduced game mesh','DECIMATE');m.ratio=.50;bpy.ops.object.modifier_apply(modifier=m.name)
 for v in o.data.vertices:
  if v.co.z>target-.012:v.co.z=target-.006
if '--finish-props' in args:
 # Limit scan chatter without erasing seams or changing load-bearing proportions.
 textile=name in ('CanvasDetails','SupplyLoad')
 m=o.modifiers.new('Surface cleanup for close viewing','SMOOTH');m.factor=.22 if textile else .16;m.iterations=2
 bpy.ops.object.modifier_apply(modifier=m.name)
 if name=='FieldTable':
  # The supporting top must be level so investigation props do not float.
  top=max(v.co.z for v in o.data.vertices)
  for v in o.data.vertices:
   if v.co.z>top-.012:v.co.z=top-.004
 if name in ('FieldTable','ArchiveShelf','SupplyLoad'):
  floor=min(v.co.z for v in o.data.vertices)
  for v in o.data.vertices:
   if v.co.z<floor+.006:v.co.z=floor
 m=o.modifiers.new('Controlled game mesh','DECIMATE');m.ratio=.72 if textile else .85
 bpy.ops.object.modifier_apply(modifier=m.name)
if '--crafted' in args:
 sys.path.insert(0,str(ROOT));import craft_standard
 bpy.data.objects.remove(o,do_unlink=True);o=craft_standard.build()
if name=='CanvasDetails' and '--finish-props' in args:
 # The received source is a hanging sheet, not a tent. Preserve it as reference,
 # then build the roof, walls, tied entrance and supporting structure explicitly.
 import runpy
 sys.path.insert(0,str(ROOT))
 bpy.data.objects.remove(o,do_unlink=True)
 o=runpy.run_path(str(ROOT/'craft_supply_tent.py'))['build']()
 target=3.12
for p in o.data.polygons:p.use_smooth=True
# Pin imported map sampling before making the non-overlapping target UV.
o.data.uv_layers.active.name='SourceUV'
for mat in o.data.materials:
 nt=mat.node_tree;uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='SourceUV'
 for n in list(nt.nodes):
  if n.type=='TEX_IMAGE' and '--crafted' not in args:nt.links.new(uv.outputs['UV'],n.inputs['Vector'])
 bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
 # Cloth and wood must not inherit random metallic glints from generated PBR.
 for l in list(bs.inputs['Metallic'].links):nt.links.remove(l)
 bs.inputs['Metallic'].default_value=0
 clamp=nt.nodes.new('ShaderNodeMath');clamp.operation='MAXIMUM';clamp.inputs[1].default_value=.72
 if bs.inputs['Roughness'].is_linked:nt.links.new(bs.inputs['Roughness'].links[0].from_socket,clamp.inputs[0])
 else:clamp.inputs[0].default_value=.84
 nt.links.new(clamp.outputs[0],bs.inputs['Roughness'])
 for n in nt.nodes:
  if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.28 if '--finish-props' in args else .55
 if '--finish-props' in args:
  # Restrain orange cast and coarse generated weave; retain original texture files.
  color=bs.inputs['Base Color']
  if color.is_linked:
   old=color.links[0].from_socket
   hsv=nt.nodes.new('ShaderNodeHueSaturation');hsv.inputs['Saturation'].default_value=.78
   hsv.inputs['Value'].default_value=.93;nt.links.new(old,hsv.inputs['Color']);nt.links.new(hsv.outputs['Color'],color)
uv=o.data.uv_layers.new(name='BakeUV');o.data.uv_layers.active=uv;uv.active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
baked={};size=1024 if '--crafted' in args else (2048 if name in ('CampStandard','CanvasDetails','ArchiveShelf') else 1024)
if '--finish-props' in args:size=1024
for suffix,kind in [('BaseColor','DIFFUSE'),('Normal','NORMAL'),('AO','AO'),('MetalSmooth','EMIT')]:
 im=bpy.data.images.new(name+'_'+suffix,size,size,alpha=True)
 if suffix!='BaseColor':im.colorspace_settings.name='Non-Color'
 nodes=[];restore=[]
 for mat in o.data.materials:
  nt=mat.node_tree;n=nt.nodes.new('ShaderNodeTexImage');n.image=im;nt.nodes.active=n;nodes.append((nt,n))
  if kind=='EMIT':
   bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');out=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL');old=out.inputs['Surface'].links[0].from_socket
   e=nt.nodes.new('ShaderNodeEmission');nt.links.new(bs.inputs['Roughness'].links[0].from_socket,e.inputs[0]);nt.links.new(e.outputs[0],out.inputs[0]);restore.append((nt,out,old,e))
 bpy.ops.object.bake(type=kind)
 for nt,out,old,e in restore:nt.links.new(old,out.inputs[0]);nt.nodes.remove(e)
 for nt,n in nodes:nt.nodes.remove(n)
 if suffix=='MetalSmooth':
  # Uncovered texels bake as zero roughness. Clamp their smoothness too, otherwise
  # mip filtering produces glossy blue seams on matte timber and canvas.
  px=np.array(im.pixels[:],dtype=np.float32).reshape(-1,4);r=px[:,0].copy();px[:,:3]=0;px[:,3]=np.clip(1-r,0,.28);im.pixels.foreach_set(px.ravel())
 im.filepath_raw=str(OUT/(name+'_'+suffix+'.png'));im.file_format='PNG';im.save();baked[suffix]=im;print('BAKED',name,suffix,flush=True)
mat=bpy.data.materials.new(name+'_Baked');mat.use_nodes=True;nt=mat.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.82
n=nt.nodes.new('ShaderNodeTexImage');n.image=baked['BaseColor'];nt.links.new(n.outputs['Color'],bs.inputs['Base Color'])
n=nt.nodes.new('ShaderNodeTexImage');n.image=baked['Normal'];normal=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(n.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs[0],bs.inputs['Normal'])
o.data.materials.clear();o.data.materials.append(mat)
for p in o.data.polygons:p.material_index=0
for u in list(o.data.uv_layers):
 if u.name!='BakeUV':o.data.uv_layers.remove(u)
bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
source.hide_render=True
scene.world=bpy.data.worlds.new('ReviewWorld');scene.world.use_nodes=True;next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.4
bpy.ops.object.camera_add(location=(target*1.6,-target*2.2,target*1.1));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,target*.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(o.dimensions)*1.45;scene.camera=cam
for loc,power in [((target,-target*1.7,target*2),500),((-target,target,target*1.8),300)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.size=target*2;l.rotation_euler=(Vector((0,0,target*.5))-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
review=PROJECT/'Logs/VarcoDetailReview20261014';review.mkdir(parents=True,exist_ok=True);scene.render.filepath=str(review/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.file.pack_all()
if '--keep-scene' in args:
 bpy.data.libraries.write(str(master),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
else:bpy.ops.wm.save_as_mainfile(filepath=str(master),compress=True)
(ROOT/(name+'-bake-report.json')).write_text(json.dumps({'name':name,'source':'Hand-built construction using retained VARCO reference and existing ambientCG Wood049 CC0' if '--crafted' in args else 'VARCO varco-3d-2.0; private generation','vertices_before':before,'vertices_after':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'dimensions':list(o.dimensions),'resolution':size,'maps':list(baked),'UV':'BakeUV only in FBX; SourceUV in editable source','padding':12,'engine':'Cycles 12 samples','metallic':0,'minimum_roughness':.72,'smoothness':'1 - roughness','status':'BAKED; Unity verification pending'},indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.shading.type='MATERIAL';area.spaces.active.region_3d.view_distance=5;area.spaces.active.region_3d.view_location=(0,0,1.5)
if name=='CanvasDetails' and '--finish-props' in args:
 report_path=ROOT/(name+'-bake-report.json')
 report=json.loads(report_path.read_text(encoding='utf-8'))
 report['source']='Constructed supply tent; received VARCO cloth preserved as reference; procedural materials'
 report['editable_parts']='EDITABLE_TENT_PARTS; roof, curtains, timber, ropes and pegs'
 report['entrance']='Blender +Y; verify Unity orientation before placement'
 report_path.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('COMPLETE',name,flush=True)
