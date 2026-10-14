"""Finish ONE prop in the existing visible Blender; review before running another.
Default: CanvasDetails. Pass PROP_NAME through runpy.init_globals for another prop.
Never starts or closes Blender, and never automatically advances to the next prop.
"""
from pathlib import Path
import datetime
import json
import runpy
import traceback
import os
import shutil
import bpy

root=Path(__file__).resolve().parents[1]
art=root/'ArtSource/VarcoDetail20261014'
out=root/'Assets/Art/Environment/VarcoDetailBaked'
allowed=('CanvasDetails','SupplyLoad','FieldTable','ArchiveShelf')
prop_name=globals().get('PROP_NAME','CanvasDetails')
if prop_name not in allowed:raise ValueError('Unknown prop: '+str(prop_name))
log=root/'Logs/VarcoPropsWork'/prop_name
log.mkdir(parents=True,exist_ok=True)
names=(prop_name,)
completed=[]
quality_revision='quality-pass-20261014'
def status(stage,**data):
    entry=dict(time=datetime.datetime.now().isoformat(),stage=stage,pid=os.getpid(),file=bpy.data.filepath,completed=completed,**data)
    temporary=log/'status.tmp'
    temporary.write_text(json.dumps(entry,ensure_ascii=False,indent=2),encoding='utf-8')
    temporary.replace(log/'status.json')
    with (log/'events.jsonl').open('a',encoding='utf-8') as stream:stream.write(json.dumps(entry,ensure_ascii=False)+'\n')

lock='RedCliffsDeduction.ReceivedProps.Running'
if bpy.app.driver_namespace.get(lock):raise RuntimeError('This job is already running in this Blender; wait for its log.')
bpy.app.driver_namespace[lock]=True
original_window=bpy.context.window
original_scene=bpy.context.scene
try:
    status('preflight')
    if bpy.app.background or original_window is None:raise RuntimeError('Run in the existing visible Blender window; no new/background process.')
    if not bpy.data.filepath or not Path(bpy.data.filepath).resolve().is_relative_to(root.resolve()):
        raise RuntimeError('The current Blender document is not in this project. No scene was changed.')
    for name in names:
        if not (art/'Source'/name/'model.usda').is_file():raise RuntimeError('Missing source: '+name)
        if (art/(name+'Quality.blend')).exists():
            raise RuntimeError('Quality revision already exists; inspect it before another revision.')
        backup=log/quality_revision
        backup.mkdir(exist_ok=True)
        for file in out.glob(name+'*'):
            if file.suffix in ('.fbx','.png'):
                destination=backup/file.name
                if destination.exists():raise RuntimeError('Revision backup already exists; inspect partial output')
                shutil.copy2(file,destination)
    status('backing_up')
    bpy.ops.wm.save_as_mainfile(filepath=str(log/('before-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S')+'.blend')),copy=True,compress=True)
    for name in names:
        status('refining_and_baking',asset=name)
        runpy.run_path(str(art/'bake_props.py'),init_globals={'JOB_ARGS':[name,'--keep-scene','--finish-props','--quality-pass','--replace-generated']})
        files=[p for p in out.glob(name+'*') if p.suffix in ('.png','.fbx')]
        total=sum(p.stat().st_size for p in files)
        master=art/(name+'Quality.blend')
        if total>10*1024*1024:raise RuntimeError(name+' exceeds the 10 MiB game-asset budget; review required.')
        if master.stat().st_size>50*1024*1024:raise RuntimeError(name+' editable source exceeds the ordinary Git budget; separate handoff required.')
        completed.append(dict(name=name,game_bytes=total,master_bytes=master.stat().st_size))
    # Preserve the repaired destination when new workbench scenes become active.
    for scene in bpy.data.scenes:
        if scene.name.endswith('_Workbench') and hasattr(scene,'varco3d_props'):
            scene.varco3d_props.asset_dir=str(art/'Source/BridgeIncoming')
    status('complete')
    if prop_name=='ArchiveShelf':
        # Keep an incoming transfer away from refined/baked workbench geometry.
        inbox=bpy.data.scenes.get('VARCO_TransferInbox') or bpy.data.scenes.new('VARCO_TransferInbox')
        if hasattr(inbox,'varco3d_props'):
            inbox.varco3d_props.asset_dir=str(art/'Source/BridgeIncoming')
            inbox.varco3d_props.server_port=5324
        bpy.context.window.scene=inbox
    print(prop_name+' COMPLETE. Review this prop before starting the next. Originals preserved.')
except Exception as error:
    status('failed',message=str(error),traceback=traceback.format_exc())
    if original_window is not None:original_window.scene=original_scene
    raise
finally:
    bpy.app.driver_namespace.pop(lock,None)
