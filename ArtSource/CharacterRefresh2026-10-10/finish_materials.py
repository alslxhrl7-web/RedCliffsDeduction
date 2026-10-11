import bpy,json
from pathlib import Path
root=Path(__file__).parent/'BlenderRefined'
bpy.ops.wm.open_mainfile(filepath=str(root/'RedCliffs_Characters_v3_refined.blend'))
scene=bpy.context.scene
order=['CaoCao','Seolpyeong','Duseok','Caimao','Zhangyun','RecordsClerk','DocumentCourier','ReliefGuard','GamblingSoldier','HanJin']
chars=[bpy.data.objects[name+'_Han208_v3_refined'] for name in order]
for obj in chars:
    for mat in obj.data.materials:
        tree=mat.node_tree
        p=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
        # AI ORM blue is unreliable on cloth/skin; do not interpret it as a metal mask.
        for socket in ['Metallic','Roughness']:
            for link in list(p.inputs[socket].links):tree.links.remove(link)
        p.inputs['Metallic'].default_value=0
        p.inputs['Roughness'].default_value=.82
        p.inputs['Specular IOR Level'].default_value=.26
        for node in tree.nodes:
            if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.75
        mat['surface_review']='Matte review preset; source ORM retained but metal mask requires artist segmentation for physically accurate iron vs cloth.'
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    loc=obj.location.copy();obj.location=(0,0,0)
    bpy.ops.export_scene.gltf(filepath=str(root/f'{obj.name}.glb'),export_format='GLB',use_selection=True,export_apply=True)
    bpy.ops.export_scene.fbx(filepath=str(root/f'{obj.name}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,bake_anim=False)
    obj.location=loc
for row in range(2):
    for i,obj in enumerate(chars):obj.hide_render=i//5!=row;obj.location=(i%5*1.3,0,0)
    scene.render.filepath=str(root/f'after_{row+1}.png');bpy.ops.render.render(write_still=True)
for i,obj in enumerate(chars):obj.hide_render=False;obj.location=(i%5*1.3,i//5*2.3,0)
bpy.ops.object.select_all(action='DESELECT')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            from mathutils import Vector
            view=area.spaces.active.region_3d
            view.view_location=(2.6,1.1,.9);view.view_distance=8
            view.view_rotation=Vector((0,8,-5)).to_track_quat('-Z','Y')
            area.spaces.active.shading.type='MATERIAL'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(root/'RedCliffs_Characters_v3_refined.blend'))
print('MATTE_REVIEW_COMPLETE',flush=True)
