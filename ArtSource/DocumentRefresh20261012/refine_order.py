import bpy, bmesh, json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[1]
ASSETS=PROJECT/'Assets/Art/Evidence/VarcoRefined20261012'
ASSETS.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.usd_import(filepath=str(OUT/'Source/9dabdfc6-2394-4298-a106-0916228f31a4/model.usda'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
lo=Vector(tuple(min(v[i] for v in pts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in pts) for i in range(3)))
size=hi-lo; center=(hi+lo)/2
before=sum(len(o.data.vertices)for o in meshes)
mat=bpy.data.materials.new('OrderBambooMatte');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.34,.19,.07,1);bs.inputs['Roughness'].default_value=.9;bs.inputs['Metallic'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(PROJECT/'Assets/Art/Environment/CommandTent/Textures/Wood049_1K-JPG_Color.jpg'))
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.35;mix.inputs[2].default_value=(.67,.45,.20,1);mat.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
total_boundary=0
for o in meshes:
 world=o.matrix_world.copy()
 for v in o.data.vertices:
  p=world@v.co; v.co=((p.x-center.x)/size.x*.5,(p.y-center.y)/size.y*.34,(p.z-lo.z)/size.z*.016)
 o.parent=None;o.matrix_world.identity();o.name='MaintenanceOrderRefined'
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00003);bmesh.ops.dissolve_degenerate(bm,dist=.000001,edges=list(bm.edges));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));total_boundary+=sum(e.is_boundary for e in bm.edges);bm.to_mesh(o.data);bm.free()
 o.data.materials.clear();o.data.materials.append(mat)
 for poly in o.data.polygons:poly.material_index=0;poly.use_smooth=True
 # Replace generated pseudo-Latin lettering with project-owned CC0 grain.
 # The authoritative document wording remains the existing Unity evidence UI.
 uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
 for poly in o.data.polygons:
  for li in poly.loop_indices:
   p=o.data.vertices[o.data.loops[li].vertex_index].co
   uv.data[li].uv=(p.x*3.5+.5,p.y*2+.5)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.export_scene.fbx(filepath=str(ASSETS/'MaintenanceOrderRefined.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'MaintenanceOrderRefined.glb'),export_format='GLB',use_selection=True)
stats={'source':'VARCO bridge 9dabdfc6-2394-4298-a106-0916228f31a4','beforeVertices':before,'afterVertices':sum(len(o.data.vertices)for o in meshes),'triangles':sum(len(f.vertices)-2 for o in meshes for f in o.data.polygons),'sourceDimensions':list(size),'refinedDimensions':[.5,.34,.016],'boundaryEdges':total_boundary,'material':'CC0 Wood049; no generated text; no metallic/normal map','evidenceText':'Unity existing overlays and evidence UI retained'}
(OUT/'refinement.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
center=Vector((0,0,.008));bpy.ops.object.camera_add(location=(.45,-.60,.85));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.69;bpy.context.scene.camera=cam
for loc,power in [((.6,-.5,1),110),((-.5,.2,.6),70)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.size=1;l.rotation_euler=(center-l.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.16,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
scene.render.filepath=str(OUT/'Preview/MaintenanceOrder-refined.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MaintenanceOrderRefined.blend'))
print('DOCUMENT_REFINED',stats)
