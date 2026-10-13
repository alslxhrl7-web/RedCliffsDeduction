import bpy
from pathlib import Path
root=Path.cwd()/'ArtSource/EnvironmentRefresh20261013/Downloads'
for name in ['wooden_crate_01','ceramic_pot','rock_07']:
    bpy.ops.wm.open_mainfile(filepath=str(root/name/(name+'.blend')))
    print('ASSET',name,flush=True)
    for o in bpy.data.objects:
        if o.type=='MESH':print(o.name,len(o.data.vertices),tuple(o.dimensions),[(m.name,m.type) for m in o.modifiers],flush=True)
