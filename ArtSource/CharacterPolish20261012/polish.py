"""Blender sculpt cleanup of current Unity poses, preserving vertex order and UVs."""
import bpy, json, numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parent.parent
OUT=ROOT/'Output';OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1600;scene.render.resolution_y=650;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral review');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.16,.19,.23,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
scene.view_settings.view_transform='AgX'
originals=bpy.data.collections.new('ORIGINAL_CURRENT_GAME_DO_NOT_EXPORT');scene.collection.children.link(originals)
finished=bpy.data.collections.new('BLENDER_POLISHED');scene.collection.children.link(finished)
items=[];audit=[]

def smoothstep(a,b,x):
    q=np.clip((x-a)/(b-a),0,1);return q*q*(3-2*q)

for file in sorted((ROOT/'Input').glob('*.json')):
    s=json.loads(file.read_text(encoding='utf-8-sig'));profile=s['profile']
    # Unity visual coordinates -> Blender, handedness preserved.
    raw=np.array([[v['x'],-v['z'],v['y']] for v in s['vertices']],dtype=np.float64)
    mesh=bpy.data.meshes.new(s['name']+'_Current');tri=np.array(s['triangles']).reshape(-1,3)[:,::-1]
    mesh.from_pydata(raw.tolist(),[],tri.tolist());mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        p=s['uv'][loop.vertex_index];uv.data[loop.index].uv=(p['x'],p['y'])
    for p in mesh.polygons:p.use_smooth=True
    obj=bpy.data.objects.new(s['name']+'_Before',mesh);originals.objects.link(obj)
    mat=bpy.data.materials.new(s['name']+'_BeforeMaterial');mat.use_nodes=True
    nt=mat.node_tree;bsdf=nt.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.78;bsdf.inputs['Specular IOR Level'].default_value=.26
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(PROJECT/s['colorPath']),check_existing=True);nt.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    if s['normalPath']:
        texn=nt.nodes.new('ShaderNodeTexImage');texn.image=bpy.data.images.load(str(PROJECT/s['normalPath']),check_existing=True);texn.image.colorspace_settings.name='Non-Color'
        normal=nt.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35;nt.links.new(texn.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
    mesh.materials.append(mat)
    new=obj.copy();new.data=mesh.copy();new.name=s['name']+'_Polished';finished.objects.link(new)
    # Weld adjacency only for the smoothing calculation: UV seams and actual topology stay intact.
    unique,inv=np.unique(np.round(raw,6),axis=0,return_inverse=True)
    edges=np.array([[inv[e.vertices[0]],inv[e.vertices[1]]] for e in mesh.edges]);edges=np.unique(np.sort(edges,axis=1),axis=0)
    edges=edges[edges[:,0]!=edges[:,1]]
    h=raw[:,2].max()-raw[:,2].min();y=(unique[:,2]-raw[:,2].min())/h;x=np.abs(unique[:,0])/h
    armored=profile in (0,2,3,4,7)
    cloth=smoothstep(.12,.22,y)*(1-smoothstep(.78,.83,y))
    if armored:cloth*=np.maximum(smoothstep(.10,.18,x),1-smoothstep(.38,.44,y))
    hands=smoothstep(.13,.18,x)*smoothstep(.35,.40,y)*(1-smoothstep(.56,.62,y))
    # Protect eyes, lips, nose, ears, hat silhouette and feet; only cheek/jaw ripple is softened.
    cheeks=smoothstep(.82,.84,y)*(1-smoothstep(.90,.92,y))*smoothstep(.020,.033,x)*(1-smoothstep(.055,.065,x))
    weights=np.maximum(cloth*.22,np.maximum(hands*.30,cheeks*.10))
    points=unique.copy()
    degree=np.bincount(edges.flatten(),minlength=len(points));degree=np.maximum(degree,1)
    for _ in range(4):
        total=np.zeros_like(points);np.add.at(total,edges[:,0],points[edges[:,1]]);np.add.at(total,edges[:,1],points[edges[:,0]])
        points+=(total/degree[:,None]-points)*weights[:,None]
    delta=points-unique;length=np.linalg.norm(delta,axis=1)
    limit=np.where(cheeks>0,.00065,np.where(hands>.1,.0025,.003))
    delta*=np.minimum(1,limit/np.maximum(length,1e-12))[:,None]
    result=raw+delta[inv]
    for v,p in zip(new.data.vertices,result):v.co=p
    new.data.update()
    newmat=mat.copy();newmat.name=s['name']+'_PolishedMaterial';new.data.materials.clear();new.data.materials.append(newmat)
    newmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.84
    for n in newmat.node_tree.nodes:
        if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.22 if armored else .16
    new['method']='Localized seam-aware relaxation; cheeks <=0.65mm, hands <=2.5mm, cloth <=3mm. Eyes/lips/UVs/topology retained.'
    for i,p in enumerate(result):s['vertices'][i]={'x':float(p[0]),'y':float(p[2]),'z':float(-p[1])}
    (OUT/file.name).write_text(json.dumps(s,separators=(',',':')),encoding='utf-8')
    dist=np.linalg.norm(result-raw,axis=1)
    audit.append({'name':s['name'],'profile':profile,'vertices':len(raw),'triangles':len(tri),'changed_vertices':int((dist>1e-6).sum()),'max_displacement_mm':float(dist.max()*1000),'normal_strength':.22 if armored else .16})
    items.append((profile,obj,new));print('POLISHED',audit[-1],flush=True)

items.sort(key=lambda t:t[0])
def aim(obj,p):obj.rotation_euler=(Vector(p)-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.4,7,1.0));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=6;aim(cam,(2.4,0,1));scene.camera=cam
for loc,power in [((0,3,4),500),((5,1,3),300)]:
    bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=4;aim(l,(2.4,0,1))
for row in range(2):
    for phase in ('before','after'):
        for i,(profile,old,new) in enumerate(items):
            old.location=new.location=(i%5*1.2,0,0);old.hide_render=i//5!=row or phase!='before';new.hide_render=i//5!=row or phase!='after'
        scene.render.filepath=str(ROOT/f'{phase}-{row}.png');bpy.ops.render.render(write_still=True)
# Editable master opens with corrected actors arranged in two rows; originals are preserved hidden.
for i,(profile,old,new) in enumerate(items):
    old.hide_render=True;old.hide_set(True);new.hide_render=False;new.location=(i%5*1.2,i//5*2.2,0)
originals.hide_render=True
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Characters_Polished.blend'))
(ROOT/'polish-report.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('COMPLETE 10; original vertices and texture files preserved',flush=True)
