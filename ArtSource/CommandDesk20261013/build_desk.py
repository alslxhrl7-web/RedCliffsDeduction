import bpy, math, random, json, sys
from pathlib import Path
from mathutils import Vector
import numpy as np
random.seed(208)
root=Path(__file__).resolve().parents[2]
out=root/'Assets/Art/Props/CommandDeskBaked'
src=Path(__file__).parent
if (src/'CommandDesk_Baked.blend').exists() and '--replace-generated' not in sys.argv:
    raise RuntimeError('Existing authoring file preserved. Back up manual edits, then explicitly pass --replace-generated to regenerate the desk.')
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.world=bpy.data.worlds.new('Review World');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.25,.3,1)
objects=[]
def mat(name,color,rough=.7,metal=0,grain=None):
    m=bpy.data.materials.new(name);m.use_nodes=True
    n=m.node_tree.nodes;p=n.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if grain:
        tc=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=grain
        m.node_tree.links.new(tc.outputs['Generated'],mapping.inputs[0]);noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=4;noise.inputs['Detail'].default_value=3
        m.node_tree.links.new(mapping.outputs[0],noise.inputs['Vector']);ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*[c*.68 for c in color],1);ramp.color_ramp.elements[1].color=(*[min(c*1.17,1) for c in color],1)
        m.node_tree.links.new(noise.outputs['Fac'],ramp.inputs[0]);m.node_tree.links.new(ramp.outputs[0],p.inputs['Base Color'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.002
        m.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs[0],p.inputs['Normal'])
    return m
wood=mat('Dark walnut',(.105,.048,.019),.69,grain=(1.2,35,12))
# Reuse the project's CC0 wood photograph rather than uniform procedural stripes.
nt=wood.node_tree;p=nt.nodes.get('Principled BSDF');uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='SurfaceUV'
photo=nt.nodes.new('ShaderNodeTexImage');photo.image=bpy.data.images.load(str(root/'Assets/Art/Environment/CommandTent/Textures/Wood049_1K-JPG_Color.jpg'));nt.links.new(uv.outputs[0],photo.inputs[0])
tint=nt.nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(.57,.43,.32,1);nt.links.new(photo.outputs[0],tint.inputs[1]);nt.links.new(tint.outputs[0],p.inputs['Base Color'])
edge=mat('End grain',(.12,.060,.026),.75,grain=(10,10,1))
silk=mat('Aged map cloth',(.53,.405,.23),.94,grain=(30,30,1))
# Author a new flat ink map. This is illustrative terrain, not an investigation clue.
W,H=1536,768
rng=np.random.default_rng(208);Y,X=np.mgrid[0:H,0:W];grain=rng.normal(0,.005,(H,W))
wash=.012*np.sin(X*.011+np.sin(Y*.016))+.009*np.cos(Y*.015+X*.002)
canvas=np.empty((H,W,4),np.float32);canvas[:,:,:3]=np.array([.58,.465,.30])[None,None,:]+(grain+wash)[:,:,None];canvas[:,:,3]=1
def stroke(points,width=1.2,color=(.095,.087,.063),opacity=.60):
    width*=1.65;opacity=min(opacity*1.8,.9)
    for a,b in zip(points,points[1:]):
        ax,ay=a;bx,by=b;pad=width*3+1;x0=max(0,int(min(ax,bx)-pad));x1=min(W,int(max(ax,bx)+pad+1));y0=max(0,int(min(ay,by)-pad));y1=min(H,int(max(ay,by)+pad+1))
        if x1<=x0 or y1<=y0:continue
        xx=X[y0:y1,x0:x1];yy=Y[y0:y1,x0:x1];dx=bx-ax;dy=by-ay;t=np.clip(((xx-ax)*dx+(yy-ay)*dy)/(dx*dx+dy*dy+1e-6),0,1)
        d2=(xx-ax-t*dx)**2+(yy-ay-t*dy)**2;alpha=(np.exp(-d2/(width*width))*opacity)[:,:,None]
        canvas[y0:y1,x0:x1,:3]*=1-alpha;canvas[y0:y1,x0:x1,:3]+=np.array(color)*alpha
riverpoints=[(70+t*1370,330+85*math.sin(t*5.4)+27*math.sin(t*15.7)+10*math.sin(t*27)) for t in np.linspace(0,1,150)]
stroke(riverpoints,9,(.22,.25,.21),.34)
for offset in [-6,7]:stroke([(x,y+offset) for x,y in riverpoints],1.4,(.16,.17,.13),.4)
for t,side in [(.20,-1),(.41,1),(.68,-1),(.82,1)]:
    x,y=riverpoints[int(t*149)];stroke([(x-s*140+12*math.sin(s*7),y+side*s*210) for s in np.linspace(0,1,60)],2.5,(.19,.22,.18),.38)
for cx,cy,spread,count in [(240,150,120,10),(580,610,150,13),(950,155,165,15),(1260,590,110,9)]:
    for j in range(count):
        x=cx+random.uniform(-spread,spread);y=cy+random.uniform(-60,60);w=random.uniform(16,45);h=random.uniform(17,48)
        pts=[(x-w,y),(x-w*.55,y-h*.32),(x-w*.22,y-h*.83),(x,y-h),(x+w*.33,y-h*.42),(x+w,y+3)]
        stroke(pts,random.uniform(.9,1.8),opacity=random.uniform(.25,.5))
        for k in range(3):stroke([(x+k*3,y-h+8+k*3),(x+w*.38+k*3,y-h*.28),(x+w*.64+k*3,y)],.8,opacity=.18)
for x,y in [(430,310),(785,472),(1190,315)]:
    stroke([(x-13,y+8),(x-13,y-12),(x+15,y-12),(x+15,y+8)],1.4,opacity=.6)
    stroke([(x-22,y-13),(x,y-28),(x+25,y-11)],1.4,opacity=.6)
for k in range(3):stroke([(160+t*1180,520+k*6+28*math.sin(t*8)) for t in np.linspace(0,1,100)],.8,opacity=.11)
# Subtle folds and worn edges, without imposing a regular grid on the map.
for x in [410,1015]:stroke([(x+3*math.sin(y*.01),y) for y in range(15,H-15,8)],2.5,(.44,.35,.22),.10)
mapim=bpy.data.images.new('Hand drawn terrain',W,H,alpha=True);mapim.colorspace_settings.name='Non-Color';mapim.pixels.foreach_set(np.clip(canvas,0,1).ravel());mapim.pack()
nt=silk.node_tree;uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='SurfaceUV';tex=nt.nodes.new('ShaderNodeTexImage');tex.image=mapim;nt.links.new(uv.outputs[0],tex.inputs[0]);nt.links.new(tex.outputs[0],nt.nodes.get('Principled BSDF').inputs['Base Color'])
ink=mat('Faded charcoal ink',(.060,.067,.047),.96)
river=mat('River ink wash',(.12,.18,.17),.94)
bamboo=mat('Bamboo slips',(.31,.19,.075),.79,grain=(30,1,1))
rope=mat('Hemp binding',(.105,.078,.046),.95)
stone=mat('Inkstone',(.025,.029,.028),.8,grain=(5,5,5))
wet=mat('Ink well',(.008,.009,.008),.3)
bronze=mat('Aged bronze',(.16,.106,.043),.48,.62,grain=(4,4,4))
red=mat('Red lacquer marker',(.21,.037,.020),.57)
hair=mat('Brush hair',(.12,.072,.036),.86)
def add(o,name,m):
    o.name=name;o.data.materials.append(m);objects.append(o);return o
def box(name,loc,scale,m,bevel=.005):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft worn edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
    return add(o,name,m)
def cyl(name,loc,r,depth,m,r2=None):
    bpy.ops.mesh.primitive_cone_add(vertices=32,radius1=r,radius2=r if r2 is None else r2,depth=depth,location=loc)
    o=add(bpy.context.object,name,m)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o
def line(name,points,r,m):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=1;sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,cu);scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;add(o,name,m);o.select_set(False);return o
# Separate planks with restrained seams and mortise-style stretchers.
for j in range(5):box('Table plank', (0,-.5+j*.25,.854),(2.9,.248,.092),wood,.004)
for x in [-1.22,1.22]:
    for y in [-.43,.43]:box('Table leg',(x,y,.405),(.115,.115,.81),wood,.007)
    box('End rail',(x,0,.72),(.095,1.06,.18),wood,.006)
for y in [-.45,.45]:box('Long rail',(0,y,.73),(2.49,.065,.16),wood,.005)
box('Lower stretcher',(0,.29,.26),(2.5,.065,.075),wood,.006)
# Thin slightly undulating cloth map, resting on the tabletop rather than floating.
nx,ny=48,28;verts=[];faces=[]
for j in range(ny+1):
    for i in range(nx+1):
        x=-.94+1.88*i/nx;y=-.44+.88*j/ny
        if i in (0,nx):x+=.0017*math.sin(j*1.4)
        if j in (0,ny):y+=.0015*math.sin(i*1.3)
        z=.902+.002*math.sin(x*9+y*5)+.006*(abs(x)/.94)**14
        verts.append((x,y,z))
for j in range(ny):
    for i in range(nx):a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
mesh=bpy.data.meshes.new('Cloth map');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Campaign map',mesh);scene.collection.objects.link(o);add(o,o.name,silk)
for x,y in [(-.82,-.28),(.78,.30)]:
    weight=box('Map weight',(x,y,.919),(.12,.025,.024),wood,.006);weight.rotation_euler[2]=random.uniform(-.18,.18)
for x,y in [(-.52,.04),(.12,-.09),(.66,.14)]:
    cyl('Marker foot',(x,y,.915),.020,.017,wood,r2=.018)
    marker=box('Carved marker',(x,y,.935),(.022,.024,.026),red,.004);marker.rotation_euler[2]=random.uniform(-.3,.3)
# Two bound slip bundles, with visible slats and cords.
for k in range(2):
    start=len(objects)
    for i in range(8):box('Bamboo slip',(-1.21+i*.018,-.10+k*.21+random.uniform(-.006,.006),.913),(.016,.20+random.uniform(-.006,.006),.018),bamboo,.002)
    for y in [-.16+k*.21,-.035+k*.21]:
        line('Binding cord',[(-1.23,y,.92),(-1.222,y,.931),(-1.069,y,.931),(-1.059,y,.92)],.0018,rope)
    angle=[-.13,.09][k];cx=-1.145;cy=-.10+k*.21
    for ob in objects[start:]:
        for v in ob.data.vertices:
            co=ob.matrix_world@v.co;x,y=co.x-cx,co.y-cy;co.x=cx+x*math.cos(angle)-y*math.sin(angle);co.y=cy+x*math.sin(angle)+y*math.cos(angle);v.co=ob.matrix_world.inverted()@co
# Rounded inkstone: shallow recessed well is assembled from a base and four rims.
box('Inkstone base',(1.16,-.28,.915),(.24,.18,.027),stone,.012)
for y in [-.365,-.195]:box('Stone rim',(1.16,y,.935),(.24,.020,.028),stone,.008)
for x in [1.05,1.27]:box('Stone rim',(x,-.28,.935),(.021,.16,.028),stone,.008)
box('Recessed ink',(1.16,-.28,.930),(.18,.115,.002),wet,.009)
box('Brush rest',(1.18,-.03,.922),(.14,.027,.044),wood,.009)
line('Bamboo brush',[(1.035,.11,.953),(1.33,-.11,.953)],.005,bamboo)
line('Brush ferrule',[(1.018,.123,.953),(1.042,.105,.953)],.006,wood)
o=cyl('Tapered brush hair',(1.001,.137,.953),.006,.044,hair,r2=.001);o.rotation_euler=Vector((-.8,.6,0)).to_track_quat('Z','Y').to_euler()
# Small bronze oil lamp, positioned away from the map and paper.
profile=[(0,.901),(.055,.901),(.059,.908),(.055,.916),(.025,.923),(.013,.94),(.012,1.06),(.026,1.073),(.058,1.080),(.080,1.098),(.080,1.105),(.074,1.108),(.066,1.097),(.045,1.089),(0,1.086)]
verts=[];faces=[]
for r,z in profile:
    for i in range(48):a=i*math.tau/48;verts.append((1.18+r*math.cos(a),.37+r*math.sin(a),z))
for j in range(len(profile)-1):
    for i in range(48):a=j*48+i;b=j*48+(i+1)%48;faces.append((a,b,b+48,a+48))
mesh=bpy.data.meshes.new('Hollow lamp');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Hollow bronze oil lamp',mesh);scene.collection.objects.link(o);add(o,o.name,bronze)
for p in mesh.polygons:p.use_smooth=True
cyl('Lamp oil',(1.18,.37,1.090),.046,.001,wet)
line('Lamp wick',[(1.217,.37,1.092),(1.244,.37,1.101)],.002,rope)
# Flatten the ink to the cloth surface; raised tubes would read as ropes on a map.
for o in objects:
    if o.name.startswith(('River wash','Ink mountain','Dotted route')):
        for v in o.data.vertices:
            x,y=v.co.x,v.co.y
            v.co.z=.903+.002*math.sin(x*9+y*5)+.006*(abs(x)/.94)**14+(v.co.z-.908)*.025
# Preserve material UVs separately from the packed bake atlas.
for ob in objects:
    layer=ob.data.uv_layers.new(name='SurfaceUV')
    for poly in ob.data.polygons:
        for li in poly.loop_indices:
            co=ob.matrix_world@ob.data.vertices[ob.data.loops[li].vertex_index].co
            if ob.name=='Campaign map':u=(co.x+.94)/1.88;v=(co.y+.44)/.88
            elif 'leg' in ob.name:u=co.z/1.4;v=co.x*2+co.y*2
            elif abs(poly.normal.z)>.5:u=co.x/2.3+.18;v=co.y/.85+.37
            else:u=co.x/2.3+.18;v=(co.y+co.z)/.85+.37
            layer.data[li].uv=(u,v)
# Give the map a slight working angle, while keeping markers and weights grounded.
for ob in objects:
    if ob.name=='Campaign map' or ob.name.startswith(('Map weight','Marker foot','Carved marker')):
        for v in ob.data.vertices:
            co=ob.matrix_world@v.co;x,y=co.x*.96,co.y*.96;a=.045;co.x=x*math.cos(a)-y*math.sin(a)-.035;co.y=x*math.sin(a)+y*math.cos(a);v.co=ob.matrix_world.inverted()@co
# Join and unwrap once; bake procedural surface colour, normal and packed metal/smoothness.
# Preserve editable, separately named components before destructive join/UV packing.
editable=bpy.data.collections.new('EditableDeskParts');scene.collection.children.link(editable)
for part in objects:
    copy=part.copy();copy.data=part.data.copy();copy.name='Source - '+part.name
    editable.objects.link(copy)
editable.hide_render=True;editable.hide_viewport=True
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();model=bpy.context.object;model.name='CommandDeskBaked'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
model.data.uv_layers.new(name='BakeUV');model.data.uv_layers.active=model.data.uv_layers['BakeUV'];model.data.uv_layers['BakeUV'].active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT')
scene.render.bake.margin=8;scene.render.bake.use_clear=True
def target(name,colorspace='Non-Color'):
    im=bpy.data.images.new(name,2048,2048,alpha=True);im.colorspace_settings.name=colorspace
    for m in model.data.materials:
        n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;m.node_tree.nodes.active=n
    return im
def save(im,name):im.filepath_raw=str(out/name);im.file_format='PNG';im.save()
albedo=target('Desk BaseColor','sRGB');scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
bpy.ops.object.bake(type='DIFFUSE');save(albedo,'Desk_BaseColor.png');print('BAKED_COLOR',flush=True)
normal=target('Desk Normal');bpy.ops.object.bake(type='NORMAL');save(normal,'Desk_Normal.png');print('BAKED_NORMAL',flush=True)
ao=target('Desk AO');bpy.ops.object.bake(type='AO');save(ao,'Desk_AO.png');print('BAKED_AO',flush=True)
packed=target('Desk Metal Smoothness');saved=[]
for m in model.data.materials:
    nt=m.node_tree;p=nt.nodes.get('Principled BSDF');output=nt.nodes.get('Material Output');em=nt.nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(p.inputs['Metallic'].default_value,1-p.inputs['Roughness'].default_value,0,1);nt.links.new(em.outputs[0],output.inputs['Surface']);saved.append((nt,p,output,em))
bpy.ops.object.bake(type='EMIT');pixels=np.array(packed.pixels[:],dtype=np.float32).reshape(-1,4);pixels[:,3]=pixels[:,1];pixels[:,1:3]=0;packed.pixels.foreach_set(pixels.ravel());save(packed,'Desk_MetalSmooth.png')
for nt,p,output,em in saved:nt.links.new(p.outputs[0],output.inputs['Surface']);nt.nodes.remove(em)
# Export one material and one static mesh for Unity.
m=bpy.data.materials.new('Desk baked surface');m.use_nodes=True;nt=m.node_tree;p=nt.nodes.get('Principled BSDF')
tex=nt.nodes.new('ShaderNodeTexImage');tex.image=albedo;nt.links.new(tex.outputs['Color'],p.inputs['Base Color'])
tex=nt.nodes.new('ShaderNodeTexImage');tex.image=normal;nm=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(tex.outputs['Color'],nm.inputs[1]);nt.links.new(nm.outputs[0],p.inputs['Normal']);p.inputs['Roughness'].default_value=.72
model.data.materials.clear();model.data.materials.append(m)
for layer in list(model.data.uv_layers):
    if layer.name!='BakeUV':model.data.uv_layers.remove(layer)
for poly in model.data.polygons:poly.material_index=0
bpy.ops.export_scene.fbx(filepath=str(out/'CommandDeskBaked.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='AUTO',bake_anim=False)
# Workbench review uses the same baked material as the export.
for loc,power,size in [((0,-2.5,4),400,4),((2,1.5,3),250,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.6))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-3.6,3.1));scene.camera=bpy.context.object;scene.camera.rotation_euler=(Vector((0,0,.55))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.type='ORTHO';scene.camera.data.ortho_scale=3.8
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.filepath=str(src/'desk-baked-preview.png')
scene.view_settings.view_transform='AgX';bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT');model.select_set(True);bpy.context.view_layer.objects.active=model
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=4;area.spaces.active.region_3d.view_location=Vector((0,0,.6));area.spaces.active.shading.type='MATERIAL'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(src/'CommandDesk_Baked.blend'))
report={'triangles':sum(len(p.vertices)-2 for p in model.data.polygons),'vertices':len(model.data.vertices),'atlas':2048,'bakes':['DIFFUSE color only','tangent NORMAL','AO','EMIT metallic/smoothness'],'materials':1,'uv_channels':len(model.data.uv_layers),'editable_collection':'EditableDeskParts','editable_parts':len(editable.objects),'bake_margin':8,'blender_version':bpy.app.version_string,'source':'Authored mesh and map; reused ambientCG Wood049 CC0 from project','size':list(model.dimensions)}
(src/'bake-report.json').write_text(json.dumps(report,indent=2));print('DESK_BAKE_COMPLETE',report,flush=True)
