"""Execute once in the current Blender console. Keeps the original scenes intact."""
from pathlib import Path
import datetime
import json
import runpy
import traceback
import bpy

root = Path(__file__).resolve().parents[1]
log = root / 'Logs/VarcoFenceWork'
log.mkdir(parents=True, exist_ok=True)
report = log / 'status.json'
def status(stage, **fields):
    report.write_text(json.dumps({'time': datetime.datetime.now().isoformat(), 'stage': stage, **fields}, ensure_ascii=False, indent=2), encoding='utf-8')

source = root / 'ArtSource/VarcoDetail20261014/Source/BridgeIncoming/b93615db-9f72-4483-b5d1-3c1831b9a123/model.usda'
master = root / 'ArtSource/VarcoDetail20261014/CampFence.blend'
if not source.exists():
    raise RuntimeError('Received fence source is missing.')
if master.exists() or (root / 'Assets/Art/Environment/VarcoDetailBaked/CampFence.fbx').exists():
    raise RuntimeError('Existing fence output protected. Do not repeat the job without checking the result.')

try:
    status('backing_up')
    backup = log / ('before-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S') + '.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(backup), copy=True, compress=True)
    status('refining_and_baking')
    runpy.run_path(str(root / 'ArtSource/VarcoDetail20261014/bake_props.py'), init_globals={'JOB_ARGS': ['CampFence', '--keep-scene']})
    status('complete', master=str(master.relative_to(root)), source_bytes=sum(p.stat().st_size for p in source.parent.iterdir() if p.is_file()), game_bytes=sum(p.stat().st_size for p in (root / 'Assets/Art/Environment/VarcoDetailBaked').glob('CampFence*') if p.suffix in ('.png','.fbx')))
    print('FENCE COMPLETE. Existing Blender scenes preserved. Unity application is next.')
except Exception as error:
    status('failed', message=str(error), traceback=traceback.format_exc())
    raise
