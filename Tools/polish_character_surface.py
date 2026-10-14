"""Conservative per-mesh revision; preserve indices, wrist boundaries, UV and rig.
Runs in the existing visible Blender. Sources and prior Unity assets stay intact.
"""
import bpy, json, math
import numpy as np
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
key=globals()['ASSET_KEY']
folder=root/'ArtSource/ModelQuality20261014'
data=json.loads((folder/'Input'/(key+'.json')).read_text(encoding='utf-8-sig'))
output=root/'Assets/Art/ModelQuality20261014'/key
master=folder/(key+'.blend')
if master.exists() or (output.exists() and any(output.iterdir())):raise RuntimeError('Existing quality revision protected: '+key)
if bpy.app.background or not bpy.context.window:raise RuntimeError('Existing visible Blender required')
output.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new(key+'_Quality');bpy.context.window.scene=scene
if hasattr(scene,'varco3d_props'):
    scene.varco3d_props.asset_dir=str(root/'ArtSource/VarcoDetail20261014/Source/BridgeIncoming')
    scene.varco3d_props.server_port=5324
matrix=np.array(data['toAuthoring']).reshape(4,4)
local=np.array([[p['x'],p['y'],p['z']] for p in data['vertices']],dtype=float)
author=(np.c_[local,np.ones(len(local))]@matrix.T)[:,:3]
raw=author[:,[0,2,1]].copy();raw[:,1]*=-1
faces=np.concatenate([np.array(s['triangles']).reshape(-1,3) for s in data['surfaces']])
# This coordinate conversion is a rotation (positive determinant), not a reflection.
local_normals=np.array([[p['x'],p['y'],p['z']] for p in data['normals']])
author_normals=local_normals@np.linalg.inv(matrix[:3,:3])
source_normals=author_normals[:,[0,2,1]].copy();source_normals[:,1]*=-1
cross=np.cross(raw[faces[:,1]]-raw[faces[:,0]],raw[faces[:,2]]-raw[faces[:,0]])
area=np.linalg.norm(cross,axis=1)
opposed=np.sum(cross*source_normals[faces].mean(axis=1),axis=1)<0
checked=area>area.max()*.05 if 'HeldRecord' in key else np.ones(len(area),dtype=bool)
if area[opposed&checked].sum()/max(area[checked].sum(),1e-12)>.01:
    raise RuntimeError('Unexpected source winding; inspect before baking')
mesh=bpy.data.meshes.new(key);mesh.from_pydata(raw.tolist(),[],faces.tolist());mesh.update()
obj=bpy.data.objects.new(key,mesh);scene.collection.objects.link(obj)
uv=mesh.uv_layers.new(name='PreservedUV')
for loop in mesh.loops:
    p=data['uv'][loop.vertex_index];uv.data[loop.index].uv=(p['x'],p['y'])
for p in mesh.polygons:p.use_smooth=True
height=np.ptp(raw[:,2]);hand='Hand' in key
# Weld only the calculation graph, never actual vertices/skin indices.
unique,inv=np.unique(np.round(raw,6),axis=0,return_inverse=True)
edges=inv[np.concatenate([faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]])]
edges,counts=np.unique(np.sort(edges,axis=1),axis=0,return_counts=True)
boundary=np.unique(edges[counts==1]);edges=edges[edges[:,0]!=edges[:,1]]
z=(unique[:,2]-raw[:,2].min())/max(height,.001)
x=np.abs(unique[:,0])/max(height,.001)
weights=np.full(len(unique),.08) if hand else np.where((z>.15)&(z<.76),.10,0.)
if not hand:
    if key.split('_')[0] in ('CaoCao','Caimao','Zhangyun','Duseok','ReliefGuard'):
        weights[(z>.40)&(x<.14)]=0 # Preserve lamellar plates.
    weights[(z>.83)&(z<.90)&(x>.025)&(x<.06)]=.035 # Cheek ripple only.
weights[boundary]=0 # Body cuts and finger/wrist borders must not separate.
degree=np.maximum(np.bincount(edges.ravel(),minlength=len(unique)),1)
points=unique.copy()
for _ in range(2):
    total=np.zeros_like(points)
    np.add.at(total,edges[:,0],points[edges[:,1]]);np.add.at(total,edges[:,1],points[edges[:,0]])
    points+=(total/degree[:,None]-points)*weights[:,None]
delta=points-unique;distance=np.linalg.norm(delta,axis=1)
limit=.00012 if hand else .00045
delta*=np.minimum(1,limit/np.maximum(distance,1e-12))[:,None]
result=raw+delta[inv]
original=obj.copy();original.data=mesh.copy();scene.collection.objects.link(original)
original.name=key+'_Original';original.hide_render=True;original.hide_set(True)
for v,p in zip(mesh.vertices,result):v.co=p
mesh.update()
source_normals/=np.maximum(np.linalg.norm(source_normals,axis=1)[:,None],1e-12)
mesh.normals_split_custom_set_from_vertices(source_normals.tolist())
offset=0
for i,surface in enumerate(data['surfaces']):
    mat=bpy.data.materials.new(key+'_Surface'+str(i));mat.use_nodes=True
    nt=mat.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Roughness'].default_value=max(.72,surface['roughness'])
    bs.inputs['Metallic'].default_value=0
    if surface['color']:
        tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/surface['color']),check_existing=True)
        nt.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    else:
        tint=surface['tint'];color=tuple(tint[c] for c in ('r','g','b','a'));bs.inputs['Base Color'].default_value=color
        noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=180;noise.inputs['Detail'].default_value=2
        ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=tuple(c*.9 for c in color[:3])+(1,);ramp.color_ramp.elements[1].color=color;nt.links.new(noise.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs[0],bs.inputs['Base Color'])
    if surface['normal']:
        normaltex=nt.nodes.new('ShaderNodeTexImage');normaltex.image=bpy.data.images.load(str(root/surface['normal']),check_existing=True)
        normaltex.image.colorspace_settings.name='Non-Color'
        normal=nt.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=min(surface['bump'],.16 if hand else .18)
        nt.links.new(normaltex.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs[0],bs.inputs['Normal'])
    mesh.materials.append(mat)
    count=len(surface['triangles'])//3
    for p in list(mesh.polygons)[offset:offset+count]:p.material_index=i
    offset+=count
scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.bake.margin=12;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
size=512 if 'HeldRecord' in key else (1024 if hand else 2048)
for suffix,kind in [('BaseColor','DIFFUSE'),('Normal','NORMAL'),('AO','AO'),('MetalSmooth','EMIT')]:
    targets=[];restore=[];images=[]
    for material_index,mat in enumerate(mesh.materials):
        prefix='Surface'+str(material_index)+'_' if len(mesh.materials)>1 else ''
        image=bpy.data.images.new(key+'_'+prefix+suffix,size,size,alpha=True);images.append((prefix,image))
        if suffix!='BaseColor':image.colorspace_settings.name='Non-Color'
        nt=mat.node_tree;n=nt.nodes.new('ShaderNodeTexImage');n.image=image;nt.nodes.active=n;targets.append((nt,n))
        if kind=='EMIT':
            bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');out=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL');previous=out.inputs[0].links[0].from_socket
            em=nt.nodes.new('ShaderNodeEmission');rough=bs.inputs['Roughness'].default_value;em.inputs[0].default_value=(rough,rough,rough,1)
            nt.links.new(em.outputs[0],out.inputs[0]);restore.append((nt,out,previous,em))
    bpy.ops.object.bake(type=kind)
    for nt,out,previous,em in restore:nt.links.new(previous,out.inputs[0]);nt.nodes.remove(em)
    for nt,n in targets:nt.nodes.remove(n)
    for prefix,image in images:
        if suffix=='MetalSmooth':
            pixels=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4);rough=pixels[:,0].copy();pixels[:,:3]=0;pixels[:,3]=np.clip(1-rough,0,.28);image.pixels.foreach_set(pixels.ravel())
        image.filepath_raw=str(output/(prefix+suffix+'.png'));image.file_format='PNG';image.save()
back=result[:,[0,2,1]].copy();back[:,2]*=-1
back=(np.c_[back,np.ones(len(back))]@np.linalg.inv(matrix).T)[:,:3]
blender_normals=np.array([list(v.normal) for v in mesh.vertices]);back_normals=blender_normals[:,[0,2,1]].copy();back_normals[:,2]*=-1
back_normals=local_normals # Preserve authored split normals at UV and material seams.
vertices=[{'x':float(p[0]),'y':float(p[1]),'z':float(p[2])} for p in back]
normals=[{'x':float(p[0]),'y':float(p[1]),'z':float(p[2])} for p in back_normals]
report={'key':key,'path':data['path'],'vertices':vertices,'normals':normals,'vertex_count':len(raw),'surface_count':len(mesh.materials),
    'max_displacement_mm':float(np.linalg.norm(result-raw,axis=1).max()*1000),
    'boundary_displacement':float(np.linalg.norm(delta[boundary],axis=1).max()) if len(boundary) else 0,
    'status':'BAKED; Unity review pending','size':size}
(output/'geometry.json').write_text(json.dumps(report,separators=(',',':')),encoding='utf-8')
scene.world=bpy.data.worlds.new('QualityReview');scene.world.use_nodes=True
next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.4
center=Vector((raw.min(0)+raw.max(0))*.5)
bpy.ops.object.camera_add(location=center+Vector((height*.65,height*2,height*.25)))
camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=max(np.ptp(raw,axis=0))*1.2;scene.camera=camera
if 'HeldRecord' in key:
    height=float(np.ptp(raw,axis=0).max());camera.location=center+Vector(source_normals[0])*height*2+Vector((height*.25,0,height*.2));camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=height*1.7
for loc,power in [(center+Vector((height,height*2,height*2)),250),(center+Vector((-height,0,height)),120)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power*(height/1.8)**2;light.data.size=height;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=700;scene.render.resolution_y=900;scene.render.resolution_percentage=100
review=root/'Logs/ModelQuality';review.mkdir(exist_ok=True);scene.render.filepath=str(review/(key+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.file.pack_all();bpy.data.libraries.write(str(master),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
if master.stat().st_size>50*1024*1024:raise RuntimeError('Master exceeds ordinary Git file budget')
print('QUALITY COMPLETE',key,flush=True)
