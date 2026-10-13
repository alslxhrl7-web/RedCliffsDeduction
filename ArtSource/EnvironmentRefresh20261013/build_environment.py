"""CC0 source adaptation and baked camp fabric. Run from project root in Blender."""
import bpy, math, json, sys
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parent.parent
OUT=PROJECT/'Assets/Art/Environment/CampRefreshBaked';OUT.mkdir(parents=True,exist_ok=True)
if (ROOT/'Environment_Set.blend').exists() and '--replace-generated' not in sys.argv:raise RuntimeError('Preserve edits; --replace-generated required')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.threads_mode='FIXED';scene.render.threads=6;scene.cycles.use_denoising=True
scene.render.bake.margin=8;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
originals=bpy.data.collections.new('EDITABLE_SOURCE_PARTS');scene.collection.children.link(originals)
finished=bpy.data.collections.new('BAKED_GAME_ASSETS');scene.collection.children.link(finished)
report=[];models=[]

def select(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
def preserve(objs):
    for o in objs:
        backup=o.copy();backup.data=o.data.copy();originals.objects.link(backup);backup.hide_render=True;backup.hide_set(True)
def image_node(mat,image):
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.nodes.active=node
    return node
def bake_model(name,objs,size=1024):
    preserve(objs);select(objs)
    for o in objs:
        for mod in list(o.modifiers):
            try:bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
            except Exception:o.modifiers.remove(mod)
    select(objs);bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # Ground the bottom and centre X/Y to make Unity placement predictable.
    points=np.array([v.co[:] for v in obj.data.vertices]);shift=np.array([(points[:,0].min()+points[:,0].max())/2,(points[:,1].min()+points[:,1].max())/2,points[:,2].min()])
    if name in ('FieldTent','SupplyCanopy'):shift=np.zeros(3)
    for v in obj.data.vertices:v.co-=Vector(shift)
    if len(obj.data.uv_layers)==0:
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
    obj.data.uv_layers.active.name='SurfaceUV'
    for mat in obj.data.materials:
        nt=mat.node_tree;uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='SurfaceUV'
        for n in list(nt.nodes):
            if n.type=='TEX_IMAGE':nt.links.new(uv.outputs['UV'],n.inputs['Vector'])
    target=obj.data.uv_layers.new(name='BakeUV');obj.data.uv_layers.active=target;target.active_render=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
    baked={}
    for suffix,kind in [('BaseColor','DIFFUSE'),('Normal','NORMAL'),('AO','AO'),('Roughness','EMIT')]:
        image=bpy.data.images.new(name+'_'+suffix,size,size,alpha=True)
        if suffix!='BaseColor':image.colorspace_settings.name='Non-Color'
        nodes=[];restore=[]
        for mat in obj.data.materials:
            nt=mat.node_tree;nodes.append((nt,image_node(mat,image)))
            if kind=='EMIT':
                bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL')
                old=output.inputs['Surface'].links[0].from_socket
                emission=nt.nodes.new('ShaderNodeEmission')
                if bs.inputs['Roughness'].is_linked:nt.links.new(bs.inputs['Roughness'].links[0].from_socket,emission.inputs['Color'])
                else:emission.inputs['Color'].default_value=(bs.inputs['Roughness'].default_value,)*3+(1,)
                nt.links.new(emission.outputs[0],output.inputs['Surface']);restore.append((nt,output,old,emission))
        bpy.ops.object.bake(type=kind)
        for nt,output,old,emission in restore:nt.links.new(old,output.inputs['Surface']);nt.nodes.remove(emission)
        for nt,node in nodes:nt.nodes.remove(node)
        if suffix=='Roughness':
            pixels=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4);rough=pixels[:,0].copy()
            pixels[:,:3]=0;pixels[:,3]=np.clip((1-rough)*.65,0,.45);image.pixels.foreach_set(pixels.ravel());suffix='MetalSmooth'
        image.filepath_raw=str(OUT/(name+'_'+suffix+'.png'));image.file_format='PNG';image.save();baked[suffix]=image
        print('BAKE',name,suffix,flush=True)
    mat=bpy.data.materials.new(name+'_Baked');mat.use_nodes=True;nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.84
    c=nt.nodes.new('ShaderNodeTexImage');c.image=baked['BaseColor'];nt.links.new(c.outputs['Color'],bs.inputs['Base Color'])
    n=nt.nodes.new('ShaderNodeTexImage');n.image=baked['Normal'];normal=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(n.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs[0],bs.inputs['Normal'])
    obj.data.materials.clear();obj.data.materials.append(mat)
    for poly in obj.data.polygons:poly.material_index=0
    for layer in list(obj.data.uv_layers):
        if layer.name!='BakeUV':obj.data.uv_layers.remove(layer)
    obj.data.uv_layers.active=obj.data.uv_layers['BakeUV']
    select([obj]);bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    for col in list(obj.users_collection):col.objects.unlink(obj)
    finished.objects.link(obj);models.append(obj)
    tris=sum(len(p.vertices)-2 for p in obj.data.polygons);report.append(dict(name=name,triangles=tris,materials=1,texture_size=size))
    obj.hide_render=True;obj.hide_set(True)

for source,name in [('wooden_crate_01','SupplyCrate'),('ceramic_pot','StoragePot'),('rock_07','RiverRock')]:
    path=ROOT/'Downloads'/source/(source+'.blend')
    with bpy.data.libraries.load(str(path),link=False) as (a,b):
        b.objects=[n for n in a.objects if (source!='rock_07' or n.endswith('LOD2')) and 'latch' not in n.lower()]
    objs=[]
    for o in b.objects:
        if o is None or o.type!='MESH':continue
        scene.collection.objects.link(o);objs.append(o)
    for image in bpy.data.images:
        if image.source=='FILE' and not image.packed_file:
            candidates=list((ROOT/'Downloads').rglob(Path(image.filepath).name))
            if candidates:image.filepath=str(candidates[0]);image.reload()
    bake_model(name,objs)

def material(name,color,texture=None,rough=.88):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=rough
    if texture:
        n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(PROJECT/'Assets/Art/Environment/CommandTent/Textures'/texture),check_existing=True)
        mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*color,1)
        m.node_tree.links.new(n.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
    return m
canvas=material('Weathered hemp canvas',(.95,.87,.70),'Fabric030_1K-JPG_Color.jpg')
wood=material('Worn timber',(.48,.32,.19),'Wood049_1K-JPG_Color.jpg')
rope=material('Hemp cord',(.38,.29,.16))
def grid(name,nu,nv,fn,mat):
    points=[fn(u/nu,v/nv) for v in range(nv+1) for u in range(nu+1)]
    faces=[]
    for v in range(nv):
        for u in range(nu):a=v*(nu+1)+u;faces.append((a,a+1,a+nu+2,a+nu+1))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);scene.collection.objects.link(o);mesh.materials.append(mat)
    uv=mesh.uv_layers.new(name='SurfaceUV')
    for loop in mesh.loops:
        i=loop.vertex_index;uv.data[loop.index].uv=((i%(nu+1))/nu*4,(i//(nu+1))/nv*2)
    for p in mesh.polygons:p.use_smooth=True
    solid=o.modifiers.new('Canvas hem thickness','SOLIDIFY');solid.thickness=.012
    return o
def line(name,a,b,r,mat):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=r,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(mat);return o
def fold(u,v):return (.009*math.sin(u*27)+.004*math.sin(u*43+.8))*(.25+.75*math.sin(math.pi*v))
# Coordinates are Blender X/right,Y/depth,Z/up; exported +Y in Unity is height.
parts=[]
parts.append(grid('Back canvas',48,12,lambda u,v:(-2.45+4.9*u,-2.28+fold(u,v),.025+2.25*v),canvas))
for side in (-1,1):
    parts.append(grid('Side canvas',40,12,lambda u,v,s=side:(s*(2.43-fold(u,v)),1.72-4*u,.025+2.25*v),canvas))
    parts.append(grid('Roof cloth',30,32,lambda u,v,s=side:(s*2.65*u,2.08-4.58*v,3.44-1.18*u-.11*math.sin(math.pi*u)*math.sin(math.pi*v)+.007*math.sin(v*32)*u),canvas))
    parts.append(grid('Tied entrance flap',12,22,lambda u,v,s=side:(s*(2.40-.48*u*(.55+.45*math.cos(v*math.pi))),1.82+.035*math.sin(v*30+u*5),.08+2.2*v),canvas))
    for y in (-2.1,1.7):
        parts.append(line('Timber pole',(side*2.35,y,0),(side*2.35,y,2.35),.065,wood))
        parts.append(line('Guy rope',(side*2.58,y,2.29),(side*2.95,y,.12),.013,rope))
        parts.append(line('Ground peg',(side*2.95,y,-.02),(side*2.91,y,.26),.032,wood))
    for y in (-2.25,-1.15,0,1.1,2.02):
        parts.append(line('Roof seam',(0,y,3.455),(side*2.64,y,2.28),.008,rope))
parts.append(line('Ridge support',(0,-2.4,3.39),(0,2.2,3.39),.06,wood))
bake_model('FieldTent',parts,2048)
# Larger open work canopy for the supply clerks; maintain the full entrance opening.
parts=[grid('Canopy cloth',50,36,lambda u,v:(-3.6+7.2*u,-2.6+5.2*v,2.84+.28*(1-abs(u*2-1))-.12*math.sin(math.pi*u)*math.sin(math.pi*v)),canvas)]
for x in (-3.35,3.35):
    for y in (-2.32,2.32):parts.append(line('Canopy support',(x,y,0),(x,y,2.94),.06,wood))
for x in (-3.55,3.55):parts.append(line('Hem cord',(x,-2.58,2.84),(x,2.58,2.84),.014,rope))
bake_model('SupplyCanopy',parts,2048)

scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
bpy.ops.object.camera_add(location=(10,14,9));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=17
for loc in [(1,3,8),(-4,-2,6)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=1400;o.data.size=8;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
for i,o in enumerate(models):o.hide_set(False);o.hide_render=False;o.location=((i%3-1)*5,(i//3)*6,0)
scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'preview.png');bpy.ops.render.render(write_still=True)
originals.hide_render=True
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Environment_Set.blend'))
(ROOT/'bake-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('COMPLETE environment assets',flush=True)


