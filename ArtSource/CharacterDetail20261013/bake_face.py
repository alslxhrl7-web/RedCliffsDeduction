import bpy,numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'CharacterDetails.blend'),use_scripts=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=4;s.render.threads_mode='FIXED';s.render.threads=6;s.frame_set(1)
s.render.bake.use_pass_direct=False;s.render.bake.use_pass_indirect=False;s.render.bake.use_pass_color=True;s.render.bake.margin=8
for obj in [o for o in bpy.data.objects if o.type=='MESH' and o.vertex_groups.get('Face_Detail_Sculpt')]:
 bpy.ops.object.select_all(action='DESELECT');obj.hide_set(False);obj.hide_render=False;obj.select_set(True);bpy.context.view_layer.objects.active=obj
 mat=obj.data.materials[0];nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');source=bs.inputs['Base Color'].links[0].from_socket;image=source.node.image
 if image.name.endswith('_DetailBaseColor'):raise RuntimeError('Face bake already applied; rebuild master before rebaking')
 pixels=np.array(image.pixels[:],dtype=np.float32).reshape(image.size[1],image.size[0],4)
 attr=obj.data.color_attributes.get('FaceToneMask') or obj.data.color_attributes.new(name='FaceToneMask',type='FLOAT_COLOR',domain='POINT')
 group=obj.vertex_groups['Face_Detail_Sculpt'];weights=np.zeros(len(obj.data.vertices));uvs=np.zeros((len(weights),2))
 for loop in obj.data.loops:uvs[loop.vertex_index]=obj.data.uv_layers.active.data[loop.index].uv[:]
 colors=pixels[(uvs[:,1]*(image.size[1]-1)).astype(int).clip(0,image.size[1]-1),(uvs[:,0]*(image.size[0]-1)).astype(int).clip(0,image.size[0]-1),:3]
 for v in obj.data.vertices:
  try:weights[v.index]=group.weight(v.index)
  except RuntimeError:pass
 skin=(colors[:,0]>colors[:,2]*1.15)&(colors[:,0]>.12);weights*=skin
 tone=np.median(colors[weights>.3],axis=0)
 for i,d in enumerate(attr.data):d.color=(weights[i]*.28,)*3+(1,)
 node=nt.nodes.new('ShaderNodeVertexColor');node.layer_name='FaceToneMask';mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[2].default_value=(*tone,1)
 nt.links.new(node.outputs['Color'],mix.inputs[0]);nt.links.new(source,mix.inputs[1]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
 baked=bpy.data.images.new(obj.name+'_DetailBaseColor',2048,2048,alpha=True);target=nt.nodes.new('ShaderNodeTexImage');target.image=baked;nt.nodes.active=target
 bpy.ops.object.bake(type='DIFFUSE');baked.filepath_raw=str(root/'Output'/(obj.name+'_BaseColor.png'));baked.file_format='PNG';baked.save()
 nt.links.new(target.outputs['Color'],bs.inputs['Base Color']);print('BAKED FACE',obj.name,flush=True)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(root/'CharacterDetails.blend'));print('COMPLETE face tone bake',flush=True)

