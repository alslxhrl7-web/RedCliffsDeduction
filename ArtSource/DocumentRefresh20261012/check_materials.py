import bpy, collections
from pathlib import Path
p=Path(r'C:\Users\mbc\Documents\ChatGPT\적벽프로젝트\RedCliffsDeduction-GitHub-20261012\ArtSource\DocumentRefresh20261012\V2\PersonnelRecordRefined.blend')
bpy.ops.wm.open_mainfile(filepath=str(p))
o=bpy.data.objects['PersonnelRecordRefined']
print('COUNTS',collections.Counter(f.material_index for f in o.data.polygons))
for i,m in enumerate(o.data.materials): print(i,m.name,tuple(m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value))
