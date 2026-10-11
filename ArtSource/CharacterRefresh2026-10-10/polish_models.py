"""Preserve source FBX; prepare editable, meter-scale static character masters."""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).parent
OUT=ROOT/'BlenderRefined'
OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
scene.render.engine='CYCLES'
scene.cycles.samples=16
scene.cycles.use_denoising=True
scene.render.resolution_x=1800
scene.render.resolution_y=720
scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED'
scene.render.threads=6
scene.world=bpy.data.worlds.new('Neutral studio')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.18,.20,.23,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
scene.view_settings.view_transform='AgX'
roles=[('CaoCao',1.78),('Seolpyeong',1.70),('Duseok',1.78),('Caimao',1.80),('Zhangyun',1.77),('RecordsClerk',1.68),('DocumentCourier',1.72),('ReliefGuard',1.76),('GamblingSoldier',1.70),('HanJin',1.74)]
characters=[]
report=[]
def mesh_stats(mesh):
    bm=bmesh.new();bm.from_mesh(mesh)
    stats={'vertices':len(bm.verts),'faces':len(bm.faces),'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'loose_vertices':sum(not v.link_faces for v in bm.verts),'degenerate_faces':sum(f.calc_area()<1e-12 for f in bm.faces)}
    bm.free();return stats

for idx,(name,height) in enumerate(roles):
    path=ROOT/'ModelsOriginal'/f'{name}_Han208_v3.fbx'
    if not path.exists():
        report.append({'name':name,'missing':True});continue
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes=[o for o in set(bpy.data.objects)-before if o.type=='MESH']
    assert len(meshes)==1,(name,'unexpected object structure')
    obj=meshes[0];obj.name=f'{name}_Han208_v3_refined'
    obj.data.name=obj.name+'_Mesh'
    coords=[obj.matrix_world@v.co for v in obj.data.vertices]
    low=Vector([min(v[i] for v in coords) for i in range(3)])
    high=Vector([max(v[i] for v in coords) for i in range(3)])
    factor=height/(high.z-low.z)
    center=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
    for v,p in zip(obj.data.vertices,coords):v.co=(p-center)*factor
    obj.matrix_world.identity()
    collection=bpy.data.collections.new(f'{idx+1:02d} {name}')
    scene.collection.children.link(collection)
    for old in list(obj.users_collection):old.objects.unlink(obj)
    collection.objects.link(obj)
    audit={'name':name,'height_m':height,'source_height':high.z-low.z,'before':mesh_stats(obj.data),'material_links_before':[]}
    for mat in obj.data.materials:
        mat.name=name+'_PBR'
        audit['material_links_before'] += [f'{l.from_node.name}:{l.from_socket.name}->{l.to_node.name}:{l.to_socket.name}' for l in mat.node_tree.links]
    obj['source_file']=path.name
    obj['notes']='Static mesh. Feet at Z=0; meter scale. No skeleton or facial rig.'
    obj.location=(idx%5*1.3,0,0)
    characters.append((obj,idx,collection))
    report.append(audit)

def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.6,-12,1.05))
camera=bpy.context.object;camera.name='Review Camera';camera.data.type='ORTHO';camera.data.ortho_scale=6.5
aim(camera,(2.6,0,1.05));scene.camera=camera
for loc,power,size in [((1,-4,5),1500,5),((5,1,4),1000,4),((-3,-2,2.5),500,3)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;aim(light,(2.6,0,1))

def render_rows(prefix):
    for row in range(2):
        for obj,idx,col in characters:obj.hide_render=idx//5!=row
        scene.render.filepath=str(OUT/f'{prefix}_{row+1}.png')
        bpy.ops.render.render(write_still=True)
    for obj,idx,col in characters:obj.hide_render=False

render_rows('before')
for (obj,idx,col),audit in zip(characters,[r for r in report if not r.get('missing')]):
    mesh=obj.data
    bm=bmesh.new();bm.from_mesh(mesh)
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.000001)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=0.0000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    for face in bm.faces:face.smooth=True
    for edge in bm.edges:
        if edge.is_manifold:edge.smooth=edge.calc_face_angle()<math.radians(55)
    bm.to_mesh(mesh);bm.free();mesh.update()
    audit['after']=mesh_stats(mesh)
    for mat in mesh.materials:
        tree=mat.node_tree
        principled=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
        tex=[n for n in tree.nodes if n.type=='TEX_IMAGE' and n.image]
        orm=next((n for n in tex if 'orm' in n.image.name.lower()),None)
        for n in tex:
            if 'normal' in n.image.name.lower() or 'orm' in n.image.name.lower():n.image.colorspace_settings.name='Non-Color'
        # Review preset: AI-generated ORM metal masks incorrectly metallicize cloth.
        # Preserve source maps for later manually segmented armor/cloth materials.
        for socket in ['Metallic','Roughness']:
            for link in list(principled.inputs[socket].links):tree.links.remove(link)
        principled.inputs['Metallic'].default_value=0
        principled.inputs['Roughness'].default_value=.82
        principled.inputs['Specular IOR Level'].default_value=.26
        for n in tree.nodes:
            if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.75
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    location=obj.location.copy();obj.location=(0,0,0)
    bpy.ops.export_scene.gltf(filepath=str(OUT/f'{obj.name}.glb'),export_format='GLB',use_selection=True,export_apply=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'{obj.name}.fbx'),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,bake_anim=False)
    obj.location=location
    print('REFINED',audit['name'],audit['after'],flush=True)
render_rows('after')
# Editable review scene: five columns, two rows; all original FBX remain untouched.
for obj,idx,col in characters:obj.location=(idx%5*1.3,idx//5*2.3,0)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=9
            area.spaces.active.region_3d.view_location=(2.6,1.1,.9)
            area.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RedCliffs_Characters_v3_refined.blend'))
(OUT/'cleanup-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('POLISH_COMPLETE',len(characters),flush=True)
