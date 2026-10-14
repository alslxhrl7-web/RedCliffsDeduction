"""One named asset per request; dispatch only project-owned authoring recipes."""
from pathlib import Path
import runpy
import bpy
root=Path(__file__).resolve().parents[1]
name=globals().get('PROP_NAME','CanvasDetails')
props=('CanvasDetails','SupplyLoad','FieldTable','ArchiveShelf')
inputs=root/'ArtSource/ModelQuality20261014/Input'
characters=tuple(p.stem for p in inputs.glob('*.json')) if inputs.exists() else ()
kits=tuple('Kit_'+p.stem for p in (root/'ArtSource/BlockoutReplacement20261014/Recipes').glob('*.json'))
if name in kits:
    runpy.run_path(str(root/'Tools/build_crafted_blockout.py'),init_globals={'FAMILY':name[4:]})
elif name in props:
    runpy.run_path(str(root/'Tools/finish_received_varco.py'),init_globals={'PROP_NAME':name})
elif name in characters:
    recipe='polish_environment_surface.py' if name.startswith('Env_') else 'polish_character_surface.py'
    revision={'Env_Duseok_Spear_v1_SpearShaft':'Env_Duseok_Spear_v1_SpearShaftWoodFix'}.get(name,name)
    runpy.run_path(str(root/'Tools'/recipe),init_globals={'ASSET_KEY':revision})
else:raise ValueError('Unknown project asset: '+str(name))
# Extend the same live callback to the explicit exported character catalog.
# No new process, port or general-purpose code execution endpoint is introduced.
session=bpy.app.driver_namespace.get('RedCliffsDeduction.WorkSession')
if session:session['tick'].__globals__['PROPS']=props+characters+kits
if name=='Env_SurroundingEarth_SurroundingEarth':
    # Preserve the original main document when the user presses Save afterwards.
    import datetime
    target=root/'Logs/ModelQuality'/('WorkingSession-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(target),compress=True)
