"""Apply the paper contact correction to an already generated Blender source, once."""
import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'Hands.blend'),use_scripts=False)
for name in ['Seolpyeong','RecordsClerk','DocumentCourier']:
    paper=bpy.data.objects[name+'_HeldRecord'];delta=-.04-float(paper.get('hand_refinement_offset',0))
    for key in paper.data.shape_keys.key_blocks:
        for vertex in key.data:vertex.co.z+=delta
    paper['hand_refinement_offset']=-.04
    path=root/'Output'/(name+'.json');data=json.loads(path.read_text());data['paperOffset']=dict(x=0,y=-.04,z=0);path.write_text(json.dumps(data))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Hands.blend'))
