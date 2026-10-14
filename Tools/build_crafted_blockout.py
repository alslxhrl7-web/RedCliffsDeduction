"""Author one named replacement kit in the existing Blender UI, then bake it.
Each kit remains an editable collection and a joined game mesh. No source deletion.
"""
from pathlib import Path
import bpy, math, json, random
import numpy as np
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
family=globals()['FAMILY'];key='Kit_'+family
source=root/'ArtSource/BlockoutReplacement20261014'
out=root/'Assets/Art/Environment/BlockoutReplacement20261014'/family
master=source/(family+'.blend')
if master.exists() or (out.exists() and any(out.iterdir())):raise RuntimeError('Existing crafted revision protected')
if bpy.app.background or not bpy.context.window:raise RuntimeError('Reuse the visible project window')
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
scene=bpy.data.scenes.new(key);bpy.context.window.scene=scene
if hasattr(scene,'varco3d_props'):scene.varco3d_props.asset_dir=str(root/'ArtSource/VarcoDetail20261014/Source/BridgeIncoming');scene.varco3d_props.server_port=5324
parts=bpy.data.collections.new(key+'_EditableParts');scene.collection.children.link(parts)
out.mkdir(parents=True,exist_ok=True);source.mkdir(parents=True,exist_ok=True)
rng=random.Random(208)

def material(name,color,texture=None,rough=.8,metal=0,grain=False):
    m=bpy.data.materials.new(key+'_'+name);m.use_nodes=True;m['emission_mask']=name=='PaperGlow'
    nt=m.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
    co=nt.nodes.new('ShaderNodeTexCoord');mapping=nt.nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(12,12,1) if grain else (3,3,3);nt.links.new(co.outputs['Generated'],mapping.inputs[0])
    noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=7;noise.inputs['Detail'].default_value=3;nt.links.new(mapping.outputs[0],noise.inputs['Vector'])
    if texture:
        t=nt.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(root/'Assets/Art/Environment/CommandTent/Textures'/texture),check_existing=True);t.projection='BOX';t.projection_blend=.18;nt.links.new(co.outputs['Generated'],t.inputs['Vector'])
        mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.4;mix.inputs[2].default_value=(*color,1);nt.links.new(t.outputs['Color'],mix.inputs[1]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
    else:
        ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*(v*.78 for v in color),1);ramp.color_ramp.elements[1].color=(*color,1);nt.links.new(noise.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs[0],bs.inputs['Base Color'])
    bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.0008 if grain else .00025;nt.links.new(noise.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs[0],bs.inputs['Normal'])
    return m
wood=material('Aged timber',(.45,.28,.14),'Wood049_1K-JPG_Color.jpg',grain=True)
endwood=material('Cut timber',(.38,.22,.10),grain=True)
rope=material('Hemp cord',(.38,.29,.16),rough=.93)
paper=material('Mulberry paper',(.76,.70,.52),'Fabric030_1K-JPG_Color.jpg',rough=.94)
cloth=material('Undyed canvas',(.78,.74,.64),'Fabric030_1K-JPG_Color.jpg',rough=.94)
iron=material('Forged fittings',(.13,.12,.10),rough=.62,metal=.65)
ink=material('Soot ink',(.045,.037,.025),rough=.95)
earth=material('Packed earth',(.46,.35,.21),'Ground054_1K-JPG_Color.jpg',rough=.95)
stone=material('River stone',(.20,.22,.19),'Ground054_1K-JPG_Color.jpg',rough=.91)
red=material('Sealing wood',(.25,.055,.025),rough=.78)
glow=material('PaperGlow',(.9,.69,.33),'Fabric030_1K-JPG_Color.jpg',rough=.9)

def link(obj,name,mat):
    obj.name=name
    for c in list(obj.users_collection):c.objects.unlink(obj)
    parts.objects.link(obj);obj.data.materials.append(mat);return obj
def bevel(obj,width=.008):
    bpy.context.view_layer.objects.active=obj
    m=obj.modifiers.new('Worn hand finished edges','BEVEL');m.width=width;m.segments=2;m.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=m.name)
    for p in obj.data.polygons:p.use_smooth=True
    obj.data.set_sharp_from_angle(angle=math.radians(45))
def box(name,loc,scale,mat=wood,edge=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=link(bpy.context.object,name,mat);o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if edge:bevel(o,edge)
    return o
def cylinder(name,loc,radius,depth,mat=wood,vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc);o=link(bpy.context.object,name,mat);bevel(o,min(radius*.05,depth*.05,.004));return o
def curve(name,points,radius,mat=rope,cyclic=False):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=2;cu.bevel_depth=radius;cu.bevel_resolution=1
    sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for a,p in zip(sp.points,points):a.co=(*p,1)
    sp.use_cyclic_u=cyclic;o=bpy.data.objects.new(name,cu);parts.objects.link(o);cu.materials.append(mat);return o
def ring(name,z,radius,minor=.006,mat=rope):
    return curve(name,[(radius*math.cos(a*math.tau/48),radius*math.sin(a*math.tau/48),z) for a in range(48)],minor,mat,True)
def mesh(name,vertices,faces,mat):
    me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.update();o=bpy.data.objects.new(name,me);parts.objects.link(o);me.materials.append(mat);return o
def grid(name,w,h,z,mat,vertical=False,wave=.015,nx=16,ny=12):
    v=[];f=[]
    for j in range(ny+1):
        for i in range(nx+1):
            x=(i/nx-.5)*w;y=(j/ny-.5)*h;delta=wave*math.sin(i/nx*math.pi*5+.6)*math.sin(j/ny*math.pi)
            v.append((x,delta,y+z) if vertical else (x,y,z+delta))
    for j in range(ny):
        for i in range(nx):
            a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
    o=mesh(name,v,f,mat)
    if vertical:
        # Author both visible sides; canvas remains a thin real surface.
        mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.006;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

if family=='TimberPost':
    vertices=[];faces=[];n=12
    for j,z in enumerate((0,.18,.7,1.4,2.15,2.4)):
        for i in range(n):
            a=i*math.tau/n;r=.105*(1+rng.uniform(-.085,.085))*(.93 if j==5 else 1);vertices.append((r*math.cos(a),r*math.sin(a),z))
    for j in range(5):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    faces.extend([tuple(reversed(range(n))),tuple(range(5*n,6*n))]);o=mesh('Hewn post with uneven arrises',vertices,faces,wood);bevel(o,.003)
    for z in (.38,1.92):
        for k in range(4):ring('Bound hemp turn',z+k*.013,.11,.006)
        curve('Binding tail',[(.1,0,z),(.13,.015,z-.08),(.125,.02,z-.13)],.005)
    cylinder('End grain plug',(0,0,2.4),.087,.008,endwood)
elif family in ('TimberBeam','TrayRim','CarvedInlay','BannerTrim'):
    length=2.4 if family=='TimberBeam' else .55;height=.16 if family=='TimberBeam' else .035;depth=.13 if family=='TimberBeam' else .025
    mat=wood if family in ('TimberBeam','TrayRim') else cloth
    box('Hand shaped rail',(0,0,0),(length,depth,height),mat,min(.009,height*.12))
    if family=='TimberBeam':
        for x in (-length*.38,length*.38):
            for k in range(3):
                curve('Rail lashing',[(x+k*.011,-depth*.54,-height*.54),(x+k*.011,depth*.54,-height*.54),(x+k*.011,depth*.54,height*.54),(x+k*.011,-depth*.54,height*.54)],.004,rope,True)
            o=cylinder('Joinery peg',(x,-depth*.54,0),.012,.018,endwood);o.rotation_euler.x=math.pi/2
elif family in ('PlankWall','RoofLining','FloorDeck','SignBoard','TrayBase'):
    count=9 if family=='PlankWall' else 7 if family in ('FloorDeck','RoofLining') else 3
    w=2.6 if family=='PlankWall' else 2.2 if family in ('FloorDeck','RoofLining') else .7
    h=2.8 if family=='PlankWall' else 2.4 if family in ('FloorDeck','RoofLining') else .36
    vertical=family in ('PlankWall','SignBoard')
    for i in range(count):
        if vertical:box('Individually fitted board '+str(i),((i+.5)*w/count-w/2,0,h/2+rng.uniform(-.006,.006)),(w/count-.004,.042,h),wood,.005)
        else:box('Individual deck plank '+str(i),((i+.5)*w/count-w/2,0,0),(w/count-.004,h,.045),wood,.004)
    for level in (.14,.86):
        if vertical:box('Back brace',(0,.037,h*level),(w,.038,.075),endwood,.006)
        else:box('Underside brace',(0,(level-.5)*h,-.035),(w,.075,.032),endwood,.003)
    if family=='SignBoard':
        for x in (-w*.38,w*.38):curve('Hanging cord',[(x,0,h),(x,-.025,h+.07),(x+.03,.03,h+.1),(x+.02,.04,h*.6)],.007)
    if family=='PlankWall':
        for x in (-w/2,w/2):box('Frame stile',(x,.045,h/2),(.105,.11,h+.04),endwood,.006)
        for x in (-w*.43,w*.43):
            for z in (.14*h,.86*h):o=cylinder('Wooden dowel',(x,-.024,z),.013,.008,endwood);o.rotation_euler.x=math.pi/2
elif family in ('Banner','WovenMat'):
    vertical=family=='Banner';w=.85 if vertical else 1.6;h=1.4 if vertical else 2.8
    grid('Woven cloth',w,h,h/2 if vertical else 0,cloth,vertical,.028 if vertical else .008)
    for side in (-1,1):
        points=[(side*w*.48,.005,(j/24)*h) if vertical else (side*w*.48,(j/24-.5)*h,.006) for j in range(25)];curve('Stitched border',points,.006,rope)
    if vertical:
        for x in (-.3,.3):curve('Suspension loop',[(x,0,h-.04),(x,-.02,h+.04),(x+.03,.025,h+.08),(x+.045,0,h-.04)],.007,rope,True)
    else:
        for y in (-h/2,h/2):
            for i in range(30):x=(i/29-.5)*w;curve('Mat fringe',[(x,y,0),(x+.006,y+math.copysign(.04,y),-.004)],.0025)
elif family=='Lantern':
    cylinder('Paper light chamber',(0,0,.26),.13,.34,glow,32)
    for z in (.08,.12,.4,.44):ring('Bronze rim',z,.151,.012,iron)
    for i in range(12):
        a=i*math.tau/12;curve('Bowed protective rib',[(math.cos(a)*r,math.sin(a)*r,z) for r,z in ((.145,.08),(.168,.2),(.168,.32),(.145,.44))],.008,wood)
    bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=.19,radius2=.05,depth=.09,location=(0,0,.495));o=link(bpy.context.object,'Vented weather cap',iron);bevel(o,.003)
    cylinder('Lower foot',(0,0,.055),.105,.03,iron)
    curve('Carry handle',[(-.045,0,.54),(-.06,0,.62),(0,0,.66),(.06,0,.62),(.045,0,.54)],.006,iron)
elif family=='PaperStack':
    for k in range(8):o=grid('Separate paper sheet '+str(k),.48,.33,k*.003,paper,False,.0015,8,6);o.rotation_euler.z=rng.uniform(-.025,.025)
    for y in (-.1,.1):curve('Bundle cord',[(-.25,y,-.004),(.25,y,-.004),(.25,y,.027),(-.25,y,.027)],.0025,rope,True)
    for j in range(10):
        y=-.11+j*.023
        for i in range(7):
            x=-.16+i*.046;curve('Brush mark',[(x,y,.024),(x+.014,y+.005,.024),(x+.009,y+.012,.024)],.0008,ink)
elif family=='ScrollRoll':
    cylinder('Rolled paper core',(0,0,0),.06,.38,paper,48)
    for z in (-.191,.191):
        pts=[]
        for i in range(240):a=i/239*math.tau*4;r=.008+.05*i/239;pts.append((r*math.cos(a),r*math.sin(a),z))
        curve('Visible wound paper edge',pts,.0012,rope)
    for z in (-.085,.085):ring('Scroll tie',z,.063,.003)
    curve('Loose tie ends',[(.06,0,.085),(.085,0,.035),(.075,.008,-.015)],.002)
elif family=='WoodToken':
    o=box('Carved identity tablet',(0,0,0),(.12,.21,.014),wood,.009)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=.009,depth=.05,location=(0,.076,0));cutter=bpy.context.object
    bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Cord eye','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
    curve('Threaded tablet cord',[(0,.076,.018),(-.02,.125,.025),(.015,.15,.018),(.03,.115,-.01),(0,.076,-.018)],.0025,rope,True)
    for y in (-.045,-.02,.005):curve('Incised tally', [(-.027,y,.0075),(.027,y+.002,.0075)],.0013,ink)
elif family=='CommandSeal':
    cylinder('Pressed sealing wax',(0,0,0),.05,.004,red,40)
    ring('Impressed seal rim',.002,.038,.001,red)
    for x in (-.015,0,.015):curve('Seal impression',[(x,-.018,.0021),(x,.018,.0021)],.00065,ink)
    curve('Seal cross impression',[(-.022,0,.0021),(.022,0,.0021)],.00065,ink)
elif family=='StoneFoot':
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=1,location=(0,0,0));o=link(bpy.context.object,'Rough cut anchor stone',stone);o.scale=(.24,.24,.11);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for v in o.data.vertices:
        v.co*=1+.09*math.sin(v.co.x*45+v.co.y*31)+rng.uniform(-.035,.035);v.co.z=max(-.07,v.co.z)
    for p in o.data.polygons:p.use_smooth=True
    cylinder('Pole seat',(0,0,.12),.065,.02,iron)
elif family in ('EarthPath','RiverSurface','RockyBank','DistantBank'):
    w=6.;h=2.5;nx=32;ny=12;v=[];f=[]
    for j in range(ny+1):
        for i in range(nx+1):
            x=(i/nx-.5)*w;y=(j/ny-.5)*h
            if family=='RiverSurface':z=.005*math.sin(x*4+y*3)+.003*math.cos(y*7)
            elif family=='DistantBank':z=(.22+.65*math.sin(i/nx*math.pi)**2)*math.sin(j/ny*math.pi)**.7+rng.uniform(-.035,.035)
            elif family=='RockyBank':z=.12*math.sin(j/ny*math.pi)+.035*math.sin(x*5+y*3)
            else:z=.012*math.sin(x*3+y*4)*(math.sin(i/nx*math.pi)*math.sin(j/ny*math.pi))
            if family!='RiverSurface' and i in (0,nx):x+=.04*math.sin(j*1.7)
            v.append((x,y,z))
    for j in range(ny):
        for i in range(nx):a=j*(nx+1)+i;f.append((a,a+1,a+nx+2,a+nx+1))
    o=mesh('Shaped surface',v,f,earth)
    for p in o.data.polygons:p.use_smooth=True
    if family in ('RockyBank','DistantBank'):
        for i in range(12):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rng.uniform(.07,.17),location=(rng.uniform(-2.7,2.7),rng.uniform(-1,1),.09));o=link(bpy.context.object,'Weathered bank stone',stone);o.scale=(1.4,.85,.6)
else:raise RuntimeError('Unknown kit: '+family)

# Preserve editable components; only join game copies.
game=bpy.data.collections.new(key+'_Game');scene.collection.children.link(game)
bpy.ops.object.select_all(action='DESELECT');copies=[]
for o in list(parts.objects):
    c=o.copy();c.data=o.data.copy();game.objects.link(c);c.select_set(True);copies.append(c)
parts.hide_render=True;parts.hide_viewport=True
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();obj=bpy.context.object;obj.name=key+'_Baked'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);me=obj.data
for i,m in enumerate(me.materials):
    if m is None:me.materials[i]=wood
bakeuv=me.uv_layers.new(name='BakeUV');me.uv_layers.active_index=len(me.uv_layers)-1
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.threads_mode='FIXED';scene.render.threads=6;scene.render.bake.margin=10;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
maps=[('BaseColor','DIFFUSE'),('Normal','NORMAL'),('AO','AO'),('MetalSmooth','EMIT')]+([('Emission','EMIT')] if family=='Lantern' else [])
for suffix,kind in maps:
    im=bpy.data.images.new(key+'_'+suffix,1024,1024,alpha=True)
    if suffix not in ('BaseColor','Emission'):im.colorspace_settings.name='Non-Color'
    targets=[];restores=[]
    for m in me.materials:
        nt=m.node_tree;node=nt.nodes.new('ShaderNodeTexImage');node.image=im;nt.nodes.active=node;targets.append((nt,node))
        if kind=='EMIT':
            bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL');previous=output.inputs[0].links[0].from_socket;em=nt.nodes.new('ShaderNodeEmission')
            value=float(m.get('emission_mask',False));em.inputs[0].default_value=(value,value,value,1) if suffix=='Emission' else (bs.inputs['Metallic'].default_value,bs.inputs['Roughness'].default_value,0,1);nt.links.new(em.outputs[0],output.inputs[0]);restores.append((nt,output,previous,em))
    bpy.ops.object.bake(type=kind)
    for nt,o,p,e in restores:nt.links.new(p,o.inputs[0]);nt.nodes.remove(e)
    for nt,n in targets:nt.nodes.remove(n)
    if suffix=='MetalSmooth':
        pixels=np.array(im.pixels[:],dtype=np.float32).reshape(-1,4);rough=pixels[:,1].copy();pixels[:,1:3]=0;pixels[:,3]=np.where(rough>.01,np.clip(1-rough,0,.45),.12);im.pixels.foreach_set(pixels.ravel())
    im.filepath_raw=str(out/(suffix+'.png'));im.file_format='PNG';im.save()
me.calc_loop_triangles();vertices=[];normals=[];uv=[];triangles=[];lookup={}
for tri in me.loop_triangles:
    for li in tri.loops:
        p=me.vertices[me.loops[li].vertex_index].co;n=me.corner_normals[li].vector;u=me.uv_layers['BakeUV'].data[li].uv
        vertex=(p.x,p.z,-p.y,n.x,n.z,-n.y,u.x,u.y);signature=tuple(round(v,7) for v in vertex)
        if signature not in lookup:
            lookup[signature]=len(vertices);vertices.append(dict(zip(('x','y','z'),vertex[:3])));normals.append(dict(zip(('x','y','z'),vertex[3:6])));uv.append(dict(zip(('x','y'),vertex[6:])))
        triangles.append(lookup[signature])
(out/'geometry.json').write_text(json.dumps(dict(family=family,vertices=vertices,normals=normals,uv=uv,triangles=triangles),separators=(',',':')),encoding='utf-8')
scene.world=bpy.data.worlds.new(key+'_World');scene.world.use_nodes=True;next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.35
coords=np.array([v.co[:] for v in me.vertices]);center=Vector((coords.min(0)+coords.max(0))*.5);size=max(float(np.ptp(coords,axis=0).max()),.1)
bpy.ops.object.camera_add(location=center+Vector((1.1,-1.8,1.15))*size);cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=size*1.6;cam.data.clip_start=.001;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam
for vector,power in [((1,-2,3),400),((-2,-1,1),180)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(vector)*size);light=bpy.context.object;light.data.energy=power*size*size;light.data.size=size*2;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=900;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.filepath=str(root/'Logs/BlockoutReplacement'/(family+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.file.pack_all();bpy.data.libraries.write(str(master),{scene},path_remap='RELATIVE',fake_user=True,compress=True)
report=dict(family=family,parts=len(parts.objects),vertices=len(vertices),triangles=len(triangles)//3,maps=[m[0] for m in maps],size=1024,source=str(master.relative_to(root)),unity=str(out.relative_to(root)),review='Blender render produced; Unity review pending')
(source/(family+'-report.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
if any(p.stat().st_size>50*1048576 for p in [master,*out.iterdir()]):raise RuntimeError('50 MiB cap exceeded')
print('CRAFTED KIT COMPLETE',family,flush=True)
