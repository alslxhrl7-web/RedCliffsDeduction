"""Editable anatomical replacement hands; retain body vertex IDs for existing motions."""
import bpy, json, math, sys
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from clip_surface import clip_hand
from hand_detail import sculpt_and_nails, skin_detail, bake_surface, flex
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parent.parent
OUT=ROOT/'Output'; PREVIEW=ROOT/'Preview'
OUT.mkdir(exist_ok=True); PREVIEW.mkdir(exist_ok=True)
if (ROOT/'Hands.blend').exists() and '--replace-generated' not in sys.argv:
    raise RuntimeError('Existing editable source: --replace-generated required')
bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'CharacterDetail20261013/CharacterDetails.blend'),use_scripts=False)
scene=bpy.context.scene;scene.cycles.samples=8
scene.render.resolution_x=900;scene.render.resolution_y=700
NAMES=['CaoCao','Seolpyeong','Duseok','Caimao','Zhangyun','RecordsClerk','DocumentCourier','ReliefGuard','GamblingSoldier','HanJin']
CUTS=[.516,.512,.501,.511,.510,.490,.516,.507,.525,.517]
def unit(v):return v/max(np.linalg.norm(v),1e-10)
def unity(v):return np.column_stack((v[:,0],v[:,2],-v[:,1]))
def vectors(v):return [dict(x=float(p[0]),y=float(p[1]),z=float(p[2])) for p in v]
def tube(name,centers,radii,flat=.78):
    verts=[];faces=[];n=12
    for i,(p,radius) in enumerate(zip(centers,radii)):
        tangent=unit(np.array(centers[min(i+1,len(centers)-1)])-np.array(centers[max(i-1,0)]))
        x=unit(np.cross(tangent,[0,0,1]));z=unit(np.cross(x,tangent))
        for k in range(n):
            a=2*math.pi*k/n;verts.append(np.array(p)+x*radius*math.cos(a)+z*radius*flat*math.sin(a))
    for i in range(len(centers)-1):
        for k in range(n):faces.append((i*n+k,i*n+(k+1)%n,(i+1)*n+(k+1)%n,(i+1)*n+k))
    faces.extend([tuple(range(n-1,-1,-1)),tuple((len(centers)-1)*n+k for k in range(n))])
    m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);scene.collection.objects.link(o);return o
def create_hand(side):
    # Metacarpal volume, tapered wrist, four different finger lengths, opposed thumb.
    parts=[];verts=[];faces=[];n=24
    for y,width,depth in [(-.025,.017,.010),(-.01,.022,.014),(0,.025,.014),(.028,.032,.016),(.057,.035,.016),(.080,.034,.013),(.091,.028,.009)]:
        for k in range(n):
            a=2*math.pi*k/n;verts.append((width*math.cos(a),y,depth*math.sin(a)))
    for j in range(6):
        for k in range(n):faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    faces.extend([tuple(range(n-1,-1,-1)),tuple(6*n+k for k in range(n))])
    m=bpy.data.meshes.new('Palm');m.from_pydata(verts,[],faces);m.update();p=bpy.data.objects.new('Palm',m);scene.collection.objects.link(p);parts.append(p)
    for digit,(x,length,radius,curl) in enumerate([(-.026,.080,.0092,35),(-.008,.090,.010,40),(.011,.084,.0094,46),(.029,.066,.0077,52)]):
        centers=[];radii=[]
        for i in range(13):
            t=i/12;angle=math.radians(curl)*t
            centers.append((x*side+(x*.10*side)*t,.076+length*math.sin(angle)/max(math.radians(curl),.001),-length*(1-math.cos(angle))/math.radians(curl)))
            radii.append(radius*(1-.27*t)*(1+.08*math.exp(-((t-.42)/.09)**2))* (max(.08,math.sqrt(max(0,1-((t-.88)/.12)**2))) if t>.88 else 1))
        parts.append(tube(['Index','Middle','Ring','Little'][digit],centers,radii))
    centers=[(-.023*side,.023,-.004),(-.037*side,.034,-.006),(-.049*side,.048,-.011),(-.052*side,.065,-.017),(-.050*side,.078,-.023),(-.046*side,.086,-.027)]
    parts.append(tube('Thumb',centers,[.017,.015,.012,.011,.009,.0015],.85))
    # Keep the independent source volumes for later sculpting.
    collection=bpy.data.collections.get('Finger construction') or bpy.data.collections.new('Finger construction')
    if collection.name not in scene.collection.children:scene.collection.children.link(collection)
    copies=[]
    for part in parts:
        cp=part.copy();cp.data=part.data.copy();collection.objects.link(cp);cp.hide_render=True;cp.hide_viewport=True;copies.append(cp)
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:part.select_set(True)
    bpy.context.view_layer.objects.active=p;bpy.ops.object.join()
    p.data.remesh_voxel_size=.0016;bpy.ops.object.voxel_remesh()
    smooth=p.modifiers.new('Soften webs and joints','SMOOTH');smooth.factor=.55;smooth.iterations=4;bpy.ops.object.modifier_apply(modifier=smooth.name)
    dec=p.modifiers.new('Game surface','DECIMATE');dec.ratio=.12;bpy.ops.object.modifier_apply(modifier=dec.name)
    sculpt_and_nails(p,side)
    for polygon in p.data.polygons:polygon.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
    return p
templates={s:create_hand(s) for s in (-1,1)}
for s,o in templates.items():o.name='HandTemplate_'+str(s);o.hide_render=True
reports=[]
for profile,name in enumerate(NAMES):
    if '--prototype' in sys.argv and profile>0:continue
    body=bpy.data.objects[name];body.location=(0,0,0)
    raw=np.array([v.co[:] for v in body.data.shape_keys.key_blocks[0].data]);h=np.ptp(raw[:,2]);floor=raw[:,2].min();z=(raw[:,2]-floor)/h
    faces=np.array([list(p.vertices) for p in body.data.polygons]);remove=np.zeros(len(raw),bool);hands=[];cut_records=[];original_face_count=len(faces)
    image=next(n.image for n in body.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and n.image)
    pixels=np.array(image.pixels[:]).reshape(image.size[1],image.size[0],4)
    uv=np.zeros((len(raw),2))
    for loop in body.data.loops:uv[loop.vertex_index]=body.data.uv_layers.active.data[loop.index].uv
    texcoords=(uv*np.array([image.size[0]-1,image.size[1]-1])).astype(int)
    vertex_colors=pixels[texcoords[:,1],texcoords[:,0],:3]
    skin_mask=(vertex_colors[:,0]-vertex_colors[:,1]>.035)&(vertex_colors[:,0]-vertex_colors[:,2]>.075)&(vertex_colors[:,0]>.28)
    for side in (-1,1):
        cut=floor+CUTS[profile]*h
        group=body.vertex_groups.get(('Left' if side<0 else 'Right')+'_Forearm')
        arm=np.array([sum(g.weight for g in v.groups if g.group==group.index) for v in body.data.vertices])
        if profile==3:arm=((raw[:,0]*side>.16*h)&(z<.55)).astype(float)
        candidates=np.flatnonzero((arm>.95)&(abs(raw[:,2]-cut)<.008))
        # Outer forearm, excluding the robe; restrict to the compact wrist section.
        center=np.median(raw[candidates],axis=0);center[2]=cut
        near=np.linalg.norm(raw[:,:2]-center[:2],axis=1)<.16
        # Cut a connected distal section, not individual skin-colored faces (which leaves islands).
        unique,inv=np.unique(np.round(raw,5),axis=0,return_inverse=True)
        allowed=(unique[:,2]<cut)&(unique[:,2]>cut-.25)&(unique[:,0]*side>h*.14)
        edges=inv[np.concatenate((faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]]))]
        adjacency=[[] for _ in unique]
        for u,v in edges:
            if allowed[u] and allowed[v]:adjacency[u].append(int(v));adjacency[v].append(int(u))
        seeds=np.flatnonzero(allowed&(abs(unique[:,2]-(cut-.07))<.025))
        seed=int(seeds[np.argmax(unique[seeds,0]*side)])
        visited={seed};todo=[seed]
        while todo:
            for v in adjacency[todo.pop()]:
                if v not in visited:visited.add(v);todo.append(v)
        mask=np.zeros(len(unique),bool);mask[list(visited)]=True;selected=mask[inv]
        remove|=selected
        raw,uv,faces,records,border=clip_hand(raw,uv,faces,selected,cut);cut_records.extend(records)
        # Original shape keys retain their count and IDs. Only previously hidden finger vertices change.
        for key in body.data.shape_keys.key_blocks:
            for record in records:key.data[record['index']].co=key.data[record['a']].co.lerp(key.data[record['b']].co,record['t'])
        # Anchors above the old deformed palm avoid inheriting malformed finger curl.
        ring=np.flatnonzero(near&(arm>.95)&(abs(z-.54)<.008))
        a=int(ring[np.argmax(raw[ring,1])]);b=int(ring[np.argmin(raw[ring,1])]);c=int(ring[np.argmax(raw[ring,2])])
        if c in (a,b):c=int(ring[np.argmax(abs(raw[ring,0]-raw[a,0]))])
        origin=raw[a];x=unit(raw[b]-origin);normal=unit(np.cross(x,raw[c]-origin));y=np.cross(normal,x);basis=np.column_stack((x,y,normal))
        if np.linalg.norm(np.cross(raw[b]-origin,raw[c]-origin))<1e-5:raise RuntimeError('Degenerate wrist anchors '+name)
        hand=templates[side].copy();hand.data=templates[side].data.copy();scene.collection.objects.link(hand);hand.hide_render=False;hand.name=name+('_LeftHand' if side<0 else '_RightHand')
        # Stitch the exact old cut boundary to the new wrist, instead of hiding a jagged cut.
        # Wide robe openings stay hollow; only the narrow wrist skin is bridged.
        border=[(u,v) for u,v in border if max(np.linalg.norm(raw[u,:2]-center[:2]),np.linalg.norm(raw[v,:2]-center[:2]))<.052]
        border_ids=sorted({v for e in border for v in e});lookup={v:i for i,v in enumerate(border_ids)}
        outer=raw[border_ids]-center
        outer=np.column_stack((-side*outer[:,1],-outer[:,2],side*outer[:,0]))/(h/1.78)
        inner=outer.copy();theta=np.arctan2(outer[:,2]/.016,outer[:,0]/.028)
        inner[:,0]=.028*np.cos(theta);inner[:,2]=.016*np.sin(theta);inner[:,1]=.006
        bandverts=np.vstack((outer,inner));n=len(outer)
        bandfaces=[(lookup[v],lookup[u],lookup[u]+n,lookup[v]+n) for u,v in border]
        bmsh=bpy.data.meshes.new('WristTransition');bmsh.from_pydata(bandverts.tolist(),[],bandfaces);bmsh.update()
        band=bpy.data.objects.new('WristTransition',bmsh);scene.collection.objects.link(band)
        for polygon in bmsh.polygons:polygon.use_smooth=True
        cuff_start=len(hand.data.vertices)
        bpy.ops.object.select_all(action='DESELECT');hand.select_set(True);band.select_set(True);bpy.context.view_layer.objects.active=hand;bpy.ops.object.join()
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
        local=np.array([v.co[:] for v in hand.data.vertices]);scale=h/1.78
        shaped=local.copy();angle=math.radians([10,0,0,0,35,0,0,0,45,40][profile])
        holding=profile in (1,5,6)
        # Keep the paper contact pose; empty hands have less uniform, more relaxed fingers.
        for digit in range(4):
            extra=0 if holding else [8,10,13,16][digit]
            shaped=flex(shaped,side,digit,extra)
        posed=shaped.copy()
        # Extend the wrist away from the torso in folded-arm roles, preserving the cuff origin.
        blend=np.clip(local[:,1]/.04,0,1);blend[cuff_start:]=0;wrist_angle=angle*blend
        shaped[:,1]=posed[:,1]*np.cos(wrist_angle)-posed[:,2]*np.sin(wrist_angle)
        shaped[:,2]=posed[:,1]*np.sin(wrist_angle)+posed[:,2]*np.cos(wrist_angle)
        # Local hand +Y runs from wrist to fingertips, +Z is the back of the hand.
        points=np.column_stack((side*shaped[:,2],-side*shaped[:,0],-shaped[:,1]))*scale+center
        hand.data.vertices.foreach_set('co',points.ravel());hand.data.update()
        palmids=np.flatnonzero(selected&(z>CUTS[profile]-.055)&(z<CUTS[profile]-.020))
        xy=(uv[palmids]*[image.size[0]-1,image.size[1]-1]).astype(int)
        colors=pixels[xy[:,1],xy[:,0],:3];skin=colors[(colors[:,0]>colors[:,2]*1.14)&(colors[:,0]>.18)]
        color=np.median(skin if len(skin)>10 else colors,axis=0)
        # Image pixels are sRGB values; shader color constants must be linear.
        color=np.where(color<=.04045,color/12.92,((color+.055)/1.055)**2.4)
        mat=bpy.data.materials.new(hand.name+'_Skin');mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.7
        nails=np.array([a.value for a in hand.data.attributes['NailMask'].data])>.5
        detail,roughness=skin_detail(local,side,nails,cuff_start)
        hand.data.materials.clear();hand.data.materials.append(mat)
        bake_surface(hand,mat,color,detail,roughness,OUT)
        # Export per-loop vertices: UV seams preserved, triangles supplied in Unity winding.
        hand.data.calc_loop_triangles();positions=[];normals=[];uvs=[];triangles=[];bindings=[];exported_indices={}
        for tri in hand.data.loop_triangles:
            for li in tri.loops:
                loop=hand.data.loops[li];v=hand.data.vertices[loop.vertex_index];uv_value=hand.data.uv_layers.active.data[li].uv
                identifier=(loop.vertex_index,round(uv_value.x,6),round(uv_value.y,6))
                if identifier not in exported_indices:
                    exported_indices[identifier]=len(positions);positions.append(v.co[:]);normals.append(v.normal[:]);uvs.append(dict(x=float(uv_value.x),y=float(uv_value.y)))
                    bindings.append(int(border_ids[loop.vertex_index-cuff_start]) if cuff_start<=loop.vertex_index<cuff_start+len(border_ids) else -1)
                triangles.append(exported_indices[identifier])
        # Four independently weighted finger relaxations. Paper contact fingers stay fixed.
        source_indices=np.array([key[0] for key in exported_indices])
        finger_shapes=[];finger_targets=[]
        for digit in range(4):
            degrees=([0,0,.6,.9] if holding else [4,5,6,7])[digit]
            variant=flex(posed,side,digit,degrees)
            variant[:,1],variant[:,2]=variant[:,1]*np.cos(wrist_angle)-variant[:,2]*np.sin(wrist_angle),variant[:,1]*np.sin(wrist_angle)+variant[:,2]*np.cos(wrist_angle)
            target=np.column_stack((side*variant[:,2],-side*variant[:,0],-variant[:,1]))*scale+center
            target[cuff_start:]=points[cuff_start:]
            finger_targets.append(target)
            hand.data.vertices.foreach_set('co',target.ravel());hand.data.update()
            target_normals=np.array([v.normal[:] for v in hand.data.vertices])[source_indices]
            finger_shapes.append(dict(name=['IndexRelax','MiddleRelax','RingRelax','LittleRelax'][digit],delta=vectors(unity((target-points)[source_indices])),normalDelta=vectors(unity(target_normals-np.array(normals)))))
        hand.data.vertices.foreach_set('co',points.ravel());hand.data.update()
        hands.append(dict(name=hand.name,holding=holding,anchors=[a,b,c],bindings=bindings,vertices=vectors(unity(np.array(positions))),normals=vectors(unity(np.array(normals))),uv=uvs,triangles=triangles,fingers=finger_shapes))
        hand['source']='Original anatomical construction with sculpted joints and separate nail plates';hand['bake']='512 color, tangent normal and metallic/smoothness; no fixed AO'
        # Preview the three authored role poses with exactly the same rigid wrist attachment.
        hand.shape_key_add(name='Original')
        for key in body.data.shape_keys.key_blocks[1:]:
            pose=np.array([v.co[:] for v in key.data]);ox=pose[a];bx=unit(pose[b]-ox);bn=unit(np.cross(bx,pose[c]-ox));by=np.cross(bn,bx);target=(points-origin)@basis@np.column_stack((bx,by,bn)).T+ox
            target[cuff_start:cuff_start+len(border_ids)]=pose[border_ids]
            k=hand.shape_key_add(name=key.name);k.data.foreach_set('co',target.ravel());k.value=key.value
        for f in range(1,scene.frame_end+1):
            scene.frame_set(f)
            for k,bk in zip(hand.data.shape_keys.key_blocks[1:],body.data.shape_keys.key_blocks[1:]):k.value=bk.value;k.keyframe_insert('value',frame=f)
        for digit,target in enumerate(finger_targets):
            k=hand.shape_key_add(name=finger_shapes[digit]['name']);k.data.foreach_set('co',target.ravel())
            for frame,value in [(1,0),(60+digit*9,.65),(125+digit*9,0),(scene.frame_end,0)]:k.value=value;k.keyframe_insert('value',frame=frame)
    kept=faces
    paper_offset=-.04 if profile in (1,5,6) else 0
    paper=bpy.data.objects.get(name+'_HeldRecord')
    if paper:
        for key in paper.data.shape_keys.key_blocks:
            for vertex in key.data:vertex.co.z+=paper_offset
        paper['hand_refinement_offset']=paper_offset
    (OUT/(name+'.json')).write_text(json.dumps(dict(name=name,vertexCount=len(raw),paperOffset=dict(x=0,y=paper_offset,z=0),vertices=vectors(unity(raw)),uv=[dict(x=float(v[0]),y=float(v[1])) for v in uv],cuts=cut_records,triangles=kept[:,::-1].ravel().tolist(),hands=hands)),encoding='utf-8')
    keys=[(k.name,np.array([v.co[:] for v in k.data]),k.value) for k in body.data.shape_keys.key_blocks]
    old_animation=body.data.shape_keys.animation_data
    action=old_animation.action if old_animation else None;slot=old_animation.action_slot if old_animation else None
    material=body.data.materials[0];new_body=bpy.data.meshes.new(name+'_ClippedBody');new_body.from_pydata(raw.tolist(),[],kept.tolist());new_body.update();new_body.materials.append(material)
    new_uv=new_body.uv_layers.new(name='PreservedUV')
    for loop in new_body.loops:new_uv.data[loop.index].uv=uv[loop.vertex_index]
    for polygon in new_body.polygons:polygon.use_smooth=True
    body.data=new_body
    for label,points,value in keys:
        key=body.shape_key_add(name=label);key.data.foreach_set('co',points.ravel());key.value=value
    if action:
        body.data.shape_keys.animation_data_create();body.data.shape_keys.animation_data.action=action
        if slot:body.data.shape_keys.animation_data.action_slot=slot
    scene.frame_set(1)
    for o in bpy.data.objects:
        if o.type=='MESH':o.hide_render=not(o==body or o.name.startswith(name+'_') and 'Hand' in o.name and o not in templates.values())
    target=Vector((0,0,h*.59));cam=scene.camera;cam.location=target+Vector((0,3,.08));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.80
    scene.render.filepath=str(PREVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)
    reports.append(dict(name=name,removedTriangles=original_face_count-len(kept),hands=2,triangles=sum(len(v['triangles'])//3 for v in hands),clippedBoundaryVertices=len(cut_records)))
    print('HANDS',name,reports[-1],flush=True)
for i,name in enumerate(NAMES):
    for o in bpy.data.objects:
        if o.name==name or o.name.startswith(name+'_'):o.location=(i%5*1.6,i//5*2.5,0)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Hands.blend'))
(ROOT/'report.json').write_text(json.dumps(reports,indent=2))
