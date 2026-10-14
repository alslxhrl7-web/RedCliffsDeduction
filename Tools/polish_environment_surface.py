"""One exported static mesh, edited/baked in the existing Blender window."""
import bpy,bmesh,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];key=globals()['ASSET_KEY']
base=root/'ArtSource/ModelQuality20261014';data=json.loads((base/'Input'/(key+'.json')).read_text(encoding='utf-8-sig'))
assert data['environment']
if not any(s['triangles'] for s in data['surfaces']):raise RuntimeError('Source has no rendered triangles; preserve and exclude from baking')
out=root/'Assets/Art/ModelQuality20261014'/key;master=base/(key+'.blend')
if master.exists() or (out.exists() and any(out.iterdir())):raise RuntimeError('Existing revision protected')
if bpy.app.background or not bpy.context.window:raise RuntimeError('Visible existing Blender required')
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
out.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new(key+'_Quality');bpy.context.window.scene=scene
if hasattr(scene,'varco3d_props'):scene.varco3d_props.asset_dir=str(root/'ArtSource/VarcoDetail20261014/Source/BridgeIncoming');scene.varco3d_props.server_port=5324
matrix=np.array(data['toAuthoring']).reshape(4,4)
local=np.array([[p['x'],p['y'],p['z']] for p in data['vertices']]);author=(np.c_[local,np.ones(len(local))]@matrix.T)[:,:3]
raw=author[:,[0,2,1]].copy();raw[:,1]*=-1
faces=np.concatenate([np.array(s['triangles']).reshape(-1,3) for s in data['surfaces']])
mesh=bpy.data.meshes.new(key);mesh.from_pydata(raw.tolist(),[],faces.tolist());mesh.update()
obj=bpy.data.objects.new(key,mesh);scene.collection.objects.link(obj)
source_uv=mesh.uv_layers.new(name='SourceUV')
for loop in mesh.loops:
    if len(data['uv'])==len(raw):p=data['uv'][loop.vertex_index];source_uv.data[loop.index].uv=(p['x'],p['y'])
def texture_path(path):
    return base/'Textures'/(Path(path).stem+'.png') if path.endswith('.asset') else root/path
offset=0
for i,s in enumerate(data['surfaces']):
    mat=bpy.data.materials.new(key+'_Surface'+str(i));mat.use_nodes=True;nt=mat.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Metallic'].default_value=s['metallic'];bs.inputs['Roughness'].default_value=max(.55,s['roughness'])
    tint=s['tint'];color=tuple(tint[c] for c in ('r','g','b','a'));bs.inputs['Base Color'].default_value=color
    uvnode=nt.nodes.new('ShaderNodeUVMap');uvnode.uv_map='SourceUV'
    mapping=nt.nodes.new('ShaderNodeMapping');nt.links.new(uvnode.outputs[0],mapping.inputs[0]);mapping.inputs['Scale'].default_value=(s['tiling']['x'],s['tiling']['y'],1);mapping.inputs['Location'].default_value=(s['offset']['x'],s['offset']['y'],0)
    if s['color']:
        tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(texture_path(s['color'])),check_existing=True);nt.links.new(mapping.outputs[0],tex.inputs[0])
        mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=color;nt.links.new(tex.outputs['Color'],mix.inputs[1]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
    elif s['metallic']<.3 and any(word in key.lower() for word in ('record','order','slip','wood','case','parchment','boat','timber','desk','shaft')):
        coordinates=nt.nodes.new('ShaderNodeTexCoord');grainmap=nt.nodes.new('ShaderNodeVectorMath');grainmap.operation='MULTIPLY';grainmap.inputs[1].default_value=(35,35,2);nt.links.new(coordinates.outputs['Generated'],grainmap.inputs[0])
        noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=6;noise.inputs['Detail'].default_value=2;nt.links.new(grainmap.outputs[0],noise.inputs[0])
        ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.87,.87,.87,1);ramp.color_ramp.elements[1].color=(1,1,1,1);nt.links.new(noise.outputs['Fac'],ramp.inputs[0])
        mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=color;nt.links.new(ramp.outputs[0],mix.inputs[1]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
        bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.00015;nt.links.new(noise.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs[0],bs.inputs['Normal'])
    if s['normal']:
        tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(texture_path(s['normal'])),check_existing=True);tex.image.colorspace_settings.name='Non-Color';nt.links.new(mapping.outputs[0],tex.inputs[0])
        normal=nt.nodes.new('ShaderNodeNormalMap');normal.uv_map='SourceUV';normal.inputs['Strength'].default_value=min(.3,s['bump']);nt.links.new(tex.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs[0],bs.inputs['Normal'])
    mesh.materials.append(mat);count=len(s['triangles'])//3
    for p in list(mesh.polygons)[offset:offset+count]:p.material_index=i
    offset+=count
original=obj.copy();original.data=mesh.copy();scene.collection.objects.link(original);original.name=key+'_Original';original.hide_render=True;original.hide_set(True)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
extent=float(np.ptp(raw,axis=0).max())
# Keep ground/cloth boundaries and door clearances; soften rigid manufactured edges only.
rigid=any(word in key.lower() for word in ('chest','crate','case','desk','pot','lamp','ledger','slip','writing','boat','brass','timber','buttcap','socketcollar'))
if rigid:
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bm.to_mesh(mesh);bm.free()
    bevel=obj.modifiers.new('Small worn edges','BEVEL');bevel.width=min(.0025,extent*.003);bevel.segments=2;bevel.limit_method='ANGLE';bevel.angle_limit=math.radians(38)
    bpy.ops.object.modifier_apply(modifier=bevel.name)
mesh=obj.data
curved=any(word in key for word in ('StoragePot','RiverRock','HempRope','SpearShaft','Cord','ButtCap','SocketCollar'))
for p in mesh.polygons:p.use_smooth=curved
if curved:mesh.set_sharp_from_angle(angle=math.radians(50))
tiled=any(word in key for word in ('Ground_','SurroundingEarth','CommandCanvas','CrimsonCloth','DarkTimber','HempRope'))
geometry_mesh=mesh
if tiled:
    # Bake one repeat, not the entire large surface: keep original UV density.
    if len(mesh.materials)!=1:raise RuntimeError('Tiled workflow requires one surface')
    bpy.ops.object.select_all(action='DESELECT');obj.hide_render=True
    bpy.ops.mesh.primitive_plane_add(size=2)
    tile=bpy.context.object;tile.name=key+'_MaterialBakeTile';tile.data.materials.append(mesh.materials[0]);mesh=tile.data
    mesh.uv_layers[0].name='SourceUV'
bake_uv=mesh.uv_layers.new(name='BakeUV');mesh.uv_layers.active_index=len(mesh.uv_layers)-1;bake_uv.active_render=True
if tiled:
    for a,b in zip(bake_uv.data,mesh.uv_layers['SourceUV'].data):a.uv=b.uv
else:
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.bake.margin=12;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
size=1024
for suffix,kind in [('BaseColor','DIFFUSE'),('Normal','NORMAL'),('AO','AO'),('MetalSmooth','EMIT')]:
    image=bpy.data.images.new(key+'_'+suffix,size,size,alpha=True)
    if suffix!='BaseColor':image.colorspace_settings.name='Non-Color'
    targets=[];restore=[]
    for mat in mesh.materials:
        nt=mat.node_tree;n=nt.nodes.new('ShaderNodeTexImage');n.image=image;nt.nodes.active=n;targets.append((nt,n))
        if kind=='EMIT':
            bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL');previous=output.inputs[0].links[0].from_socket
            em=nt.nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(bs.inputs['Metallic'].default_value,bs.inputs['Roughness'].default_value,0,1);nt.links.new(em.outputs[0],output.inputs[0]);restore.append((nt,output,previous,em))
    bpy.ops.object.bake(type=kind)
    for nt,output,previous,em in restore:nt.links.new(previous,output.inputs[0]);nt.nodes.remove(em)
    for nt,n in targets:nt.nodes.remove(n)
    if suffix=='MetalSmooth':
        pixels=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4);rough=pixels[:,1].copy();pixels[:,1:3]=0;pixels[:,3]=np.where(rough>.01,np.clip(1-rough,0,.45),.15);image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw=str(out/(suffix+'.png'));image.file_format='PNG';image.save()
if tiled:
    tile.hide_render=True;tile.hide_set(True);obj.hide_render=False;mesh=geometry_mesh
mesh.calc_loop_triangles();inv=np.linalg.inv(matrix)
vertices=[];normals=[];uvs=[];triangles=[]
for tri in mesh.loop_triangles:
    for li in tri.loops:
        loop=mesh.loops[li];p=mesh.vertices[loop.vertex_index].co;v=inv@np.array([p.x,p.z,-p.y,1]);n=mesh.corner_normals[li].vector;normal=np.array([n.x,n.z,-n.y])@matrix[:3,:3];normal/=max(np.linalg.norm(normal),1e-12)
        vertices.append(dict(zip(('x','y','z'),map(float,v[:3]))));normals.append(dict(zip(('x','y','z'),map(float,normal))));u=mesh.uv_layers['SourceUV' if tiled else 'BakeUV'].data[li].uv;uvs.append({'x':float(u.x),'y':float(u.y)});triangles.append(len(triangles))
report={'key':key,'path':data['path'],'meshName':data['meshName'],'environment':True,'vertices':vertices,'normals':normals,'uv':uvs,'triangles':triangles,'size':size,'status':'BAKED; Unity review pending'}
report['uv_strategy']='preserve repeat density; material tile baked, AO neutral' if tiled else 'individual atlas'
(out/'geometry.json').write_text(json.dumps(report,separators=(',',':')),encoding='utf-8')
scene.world=bpy.data.worlds.new('QualityReview');scene.world.use_nodes=True;next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.4
visible_raw=raw[np.unique(faces)]
center=Vector((visible_raw.min(0)+visible_raw.max(0))*.5);distance=max(float(np.ptp(visible_raw,axis=0).max()),.1)
bpy.ops.object.camera_add(location=center+Vector((distance*.8,distance*1.5,distance*.7)));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=distance*2;cam.data.clip_start=.001;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
for v,power in [((1,2,2),250),((-1,0,1),120)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(v)*distance);light=bpy.context.object;light.data.energy=power*(distance/1.8)**2;light.data.size=distance;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.filepath=str(root/'Logs/ModelQuality'/(key+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.file.pack_all();bpy.data.libraries.write(str(master),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
if any(p.stat().st_size>50*1024*1024 for p in [master,*out.iterdir()]):raise RuntimeError('Ordinary Git budget exceeded')
print('QUALITY COMPLETE',key,flush=True)
