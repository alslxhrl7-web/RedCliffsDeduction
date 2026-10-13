"""Author role-specific shape keys and bake their animation for Unity. No UV/topology changes."""
import bpy, json, math, struct, sys, zipfile
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parent.parent
OUT=ROOT/'Output'; OUT.mkdir(exist_ok=True)
PREVIEW=ROOT/'Preview'; PREVIEW.mkdir(exist_ok=True)
if (ROOT/'RoleActivities.blend').exists() and '--replace-generated' not in sys.argv:
    raise RuntimeError('Preserve hand edits: explicit --replace-generated required.')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1500;scene.render.resolution_y=650;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.19,.21,.25,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
scene.view_settings.view_transform='AgX'
# Elbow lift (left/right), inward forearm angle, head pitch and head yaw.
CONFIG=[
 ('CaoCao','Consider the report, hands gathered',72,82,65,-7,0),
 ('Seolpyeong','Check a folded record',78,72,32,-12,-6),
 ('Duseok','Keep the spear steady, check the belt',0,46,15,-3,5),
 ('Caimao','Survey the camp, deliberate left and right glances',0,0,0,-2,-8),
 ('Zhangyun','Inspect the opposite forearm guard',85,60,68,-10,8),
 ('RecordsClerk','Review the hands and organise work',72,78,20,-14,0),
 ('DocumentCourier','Hold a dispatch close, look up',90,65,32,-8,-5),
 ('ReliefGuard','Check a sleeve, then watch the approach',56,68,24,-4,8),
 ('GamblingSoldier','Rub palms while waiting',87,83,70,-10,-6),
 ('HanJin','Straighten a cuff and glance aside',65,83,65,-7,7),
]

def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def rotate(p,pivot,axis,degrees):
    a=math.radians(degrees);c=math.cos(a);s=math.sin(a)
    mat=np.eye(3);j,k=((1,2),(2,0),(0,1))[axis]
    mat[j,j]=mat[k,k]=c;mat[j,k]=-s;mat[k,j]=s
    return (p-pivot)@mat.T+pivot
def unity(p):return np.column_stack((p[:,0],p[:,2],-p[:,1]))

actors=[];reports=[];props=[]
paper=bpy.data.materials.new('Baked paper');paper.use_nodes=True
nt=paper.node_tree;bs=nt.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9
noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=95
ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.37,.26,.12,1);ramp.color_ramp.elements[1].color=(.75,.66,.43,1)
nt.links.new(noise.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs[0],bs.inputs['Base Color'])
paper_bake=bpy.data.images.new('HeldPaper_BaseColor',512,512)
bake_node=nt.nodes.new('ShaderNodeTexImage');bake_node.image=paper_bake;nt.nodes.active=bake_node
for profile,(name,description,left,right,inward,pitch,yaw) in enumerate(CONFIG):
    with zipfile.ZipFile(ROOT/'SourceInputs.zip') as archive:
        data=json.loads(archive.read(name+'.json').decode('utf-8-sig'))
    raw=np.array([[v['x'],-v['z'],v['y']] for v in data['vertices']])
    floor=raw[:,2].min();h=np.ptp(raw[:,2]);z=(raw[:,2]-floor)/h
    faces=np.array(data['triangles']).reshape(-1,3)[:,::-1]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(raw.tolist(),[],faces.tolist());mesh.update()
    uv=mesh.uv_layers.new(name='PreservedUV')
    uvdata=np.array([[v['x'],v['y']] for v in data['uv']])
    for loop in mesh.loops:uv.data[loop.index].uv=uvdata[loop.vertex_index]
    for polygon in mesh.polygons:polygon.use_smooth=True
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
    mat=bpy.data.materials.new(name+'_ExistingSurface');mat.use_nodes=True
    nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.84
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(PROJECT/data['colorPath']),check_existing=True)
    nt.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mesh.materials.append(mat)
    obj.shape_key_add(name='Original_Game_Mesh')
    unique,inv=np.unique(np.round(raw,5),axis=0,return_inverse=True)
    edges=np.concatenate((faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]))
    edges=inv[edges]
    uz=(unique[:,2]-floor)/h
    arm_masks=[]
    for side in (-1,1):
        # Isolate each forearm by connectivity below the elbow, excluding robe panels.
        allowed=(unique[:,0]*side/h>.105)&(uz>.32)&(uz<.665)
        adjacency=[[] for _ in unique]
        for u,v in edges:
            if allowed[u] and allowed[v]:adjacency[u].append(v);adjacency[v].append(u)
        candidates=np.flatnonzero(allowed&(uz>.40)&(uz<.56))
        seed=candidates[np.argmax(unique[candidates,0]*side)]
        visited={int(seed)};todo=[int(seed)]
        while todo:
            for v in adjacency[todo.pop()]:
                if int(v) not in visited:visited.add(int(v));todo.append(int(v))
        selected=np.zeros(len(unique));selected[list(visited)]=1
        arm=selected[inv]*(1-smooth(.55,.665,z))
        if profile==3:
            # Sleeves and apron are fused in this source. Keep them intact; animate the lookout.
            arm*=0
        arm_masks.append(arm)
        group=obj.vertex_groups.new(name=('Left' if side<0 else 'Right')+'_Forearm')
        for i in np.flatnonzero(arm>.001):group.add([int(i)],float(arm[i]),'REPLACE')
    neck=smooth(.77,.84,z)
    group=obj.vertex_groups.new(name='Head_Neck')
    for i in np.flatnonzero(neck>.001):group.add([int(i)],float(neck[i]),'REPLACE')
    def pose(a,b,turn,extra_pitch=0):
        result=raw.copy()
        for side,angle,mask in zip((-1,1),(a,b),arm_masks):
            pivot=np.array([side*.175*h,0,floor+.60*h])
            target=rotate(raw,pivot,0,angle)
            target=rotate(target,pivot,2,side*inward) if angle else raw
            result+=(target-raw)*mask[:,None]
        pivot=np.array([0,0,floor+.79*h])
        head=rotate(raw,pivot,0,pitch+extra_pitch)
        head=rotate(head,pivot,2,turn)
        result+=(head-raw)*neck[:,None]
        return result
    poses=[pose(left,right,yaw),pose(left+(0 if profile==2 else 8),right-7,yaw+7,-2),pose(left,right+6,yaw-8,2)]
    normals=[]
    for label,points in zip(('WorkRest','ActivityA','ActivityB'),poses):
        key=obj.shape_key_add(name=label)
        key.data.foreach_set('co',points.astype(np.float32).ravel())
        temp=bpy.data.meshes.new(name+'_Normals');temp.from_pydata(points.tolist(),[],faces.tolist());temp.update()
        # Unity's clockwise surface convention is opposite to the Blender preview faces.
        normals.append(-unity(np.array([v.normal[:] for v in temp.vertices])))
        bpy.data.meshes.remove(temp)
    # Animate pose keys with long pauses and smooth starts/stops; bake exactly this action.
    duration=10+profile*.43;fps=20;frames=round(duration*fps)+1
    scene.render.fps=fps
    for frame,wa,wb in [(1,0,0),(30,0,0),(65,1,0),(95,1,0),(125,0,0),(150,0,0),(180,0,1),(frames,0,0)]:
        for key,value in zip(obj.data.shape_keys.key_blocks[1:],(1-wa-wb,wa,wb)):
            key.value=value;key.keyframe_insert('value',frame=frame)
    samples=[]
    for frame in range(1,frames+1):
        scene.frame_set(frame)
        samples.append([key.value for key in obj.data.shape_keys.key_blocks[1:]])
    deltas=[unity(p-raw) for p in poses]
    changed=np.max(np.linalg.norm(np.stack(deltas),axis=2),axis=0)>1e-7
    indices=np.flatnonzero(changed)
    with (OUT/f'{name}.bytes').open('wb') as f:
        f.write(struct.pack('<7i',0x49444352,1,profile,len(raw),fps,frames,len(indices)))
        f.write(np.array(samples,dtype='<f4').tobytes())
        for i in indices:
            f.write(struct.pack('<i',int(i)))
            for values in deltas+normals:f.write(np.array(values[i],dtype='<f4').tobytes())
    obj['activity']=description;obj['profile']=profile
    obj['source']='Current Unity mesh; vertex order and UVs preserved'
    obj['bake']='20 Hz shape-key action weights; positions and pose normals, no fixed lighting'
    actors.append(obj)
    if profile in (1,5,6):
        # Match each paper edge to the corresponding posed palm; shape keys keep it attached.
        hand_indices=[]
        for side in (-1,1):
            candidates=np.flatnonzero((raw[:,0]*side/h>.16)&(z>.40)&(z<.49))
            if len(candidates)==0:raise RuntimeError('No palm anchor for '+name)
            hand_indices.append(candidates)
        prop_poses=[]
        for points in poses:
            l,r=[points[ids].mean(axis=0) for ids in hand_indices]
            direction=np.array([0,.12,.11])
            # A folded sheet rests across the two palms; front edge hangs slightly downward.
            front=np.array([l-direction*.6,r-direction*.6,r+direction*.6,l+direction*.6])
            prop_poses.append(np.vstack((front,front+np.array([0,0,.003]))))
        triangles=np.array([[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[1,2,6],[1,6,5],[2,3,7],[2,7,6],[3,0,4],[3,4,7]])
        pm=bpy.data.meshes.new(name+'_Paper');pm.from_pydata(prop_poses[0].tolist(),[],triangles.tolist());pm.update()
        puv=pm.uv_layers.new(name='BakeUV');puvs=[[0,0],[1,0],[1,1],[0,1]]*2
        for loop in pm.loops:puv.data[loop.index].uv=puvs[loop.vertex_index]
        pm.materials.append(paper);prop=bpy.data.objects.new(name+'_HeldRecord',pm);scene.collection.objects.link(prop)
        prop.shape_key_add(name='WorkRest')
        for label,points in zip(('ActivityA','ActivityB'),prop_poses[1:]):
            key=prop.shape_key_add(name=label);key.data.foreach_set('co',points.astype(np.float32).ravel())
        for frame,weights in enumerate(samples,1):
            for key,value in zip(prop.data.shape_keys.key_blocks[1:],weights[1:]):key.value=value;key.keyframe_insert('value',frame=frame)
        exported=dict(vertices=[dict(x=float(p[0]),y=float(p[1]),z=float(p[2])) for p in unity(prop_poses[0])],
            a=[dict(x=float(p[0]),y=float(p[1]),z=float(p[2])) for p in unity(prop_poses[1]-prop_poses[0])],
            b=[dict(x=float(p[0]),y=float(p[1]),z=float(p[2])) for p in unity(prop_poses[2]-prop_poses[0])],
            uv=[dict(x=p[0],y=p[1]) for p in puvs],triangles=triangles[:,::-1].ravel().tolist())
        (OUT/f'{name}-prop.json').write_text(json.dumps(exported),encoding='utf-8')
        props.append((profile,prop))
    reports.append(dict(name=name,activity=description,vertices=len(raw),affected=len(indices),frames=frames,seconds=(frames-1)/fps,feet_displacement=float(max(np.linalg.norm(d[z<.3],axis=1).max() for d in deltas)),uv_unchanged=True))
    print('BAKED',name,flush=True)

def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
if props:
    bpy.ops.object.select_all(action='DESELECT');props[0][1].select_set(True);bpy.context.view_layer.objects.active=props[0][1]
    scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
    scene.render.bake.margin=4;bpy.ops.object.bake(type='DIFFUSE')
    paper_bake.filepath_raw=str(OUT/'HeldPaper_BaseColor.png');paper_bake.file_format='PNG';paper_bake.save()
bpy.ops.object.camera_add(location=(3.2,8,1.9));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=8;aim(cam,(3.2,0,1.0));scene.camera=cam
for loc,power in [((1,4,5),650),((5,1,4),400)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=4;aim(light,(3,0,1))
scene.frame_end=max(round((10+i*.43)*20)+1 for i in range(10))
for row in range(2):
    for i,obj in enumerate(actors):obj.location=(i%5*1.6,0,0);obj.hide_render=i//5!=row
    for i,prop in props:prop.location=actors[i].location;prop.hide_render=i//5!=row
    scene.frame_set(65);scene.render.filepath=str(PREVIEW/f'activities-{row}.png');bpy.ops.render.render(write_still=True)
for i,obj in enumerate(actors):obj.hide_render=False;obj.location=(i%5*1.6,i//5*2.5,0)
for i,prop in props:prop.hide_render=False;prop.location=actors[i].location
scene.frame_set(1)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'RoleActivities.blend'))
(ROOT/'bake-report.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('COMPLETE role activities 10',flush=True)
