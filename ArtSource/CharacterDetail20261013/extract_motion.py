import bpy,json,math
from pathlib import Path
from mathutils import Quaternion
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'Downloads/CC0-packs/Quaternius/Quaternius-human-packs.blend'),use_scripts=False)
s=bpy.context.scene;rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');rig.animation_data_create()
result={}
for name in ['Idle_A','Idle_Talking','Idle_ShakeOff','Idle_FoldArms','Yes']:
 action=bpy.data.actions[name];rig.animation_data.action=action
 if len(action.slots):rig.animation_data.action_slot=action.slots[0]
 start,end=action.frame_range;fps=s.render.fps/s.render.fps_base;duration=(end-start)/fps*2.3;count=max(2,round(duration*20)+1)
 samples=[];first=None
 for i in range(count):
  f=start+(end-start)*i/(count-1);s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
  q=[rig.pose.bones[b].matrix.to_quaternion() for b in ['head','lowerarm_l','lowerarm_r']]
  if first is None:first=[v.copy() for v in q]
  values=[]
  for j,(v,b) in enumerate(zip(q,first)):
   d=v@b.inverted();axis,angle=d.to_axis_angle()
   if angle>math.pi:angle-=2*math.pi
   angle=max(-.16,min(.16,angle*.28))
   edge=min(1,i/5,(count-1-i)/5);d=Quaternion(axis,angle*edge)
   # Blender to Unity reflection x,y,z -> x,z,-y; rotation mapped as proper basis.
   values.extend([d.x,d.z,-d.y,d.w])
  samples.append(values)
 result[name]=dict(fps=20,samples=samples,seconds=(count-1)/20)
(root/'external-motion.json').write_text(json.dumps(result),encoding='utf-8')
print('COMPLETE external animations',[(n,v['seconds']) for n,v in result.items()])
