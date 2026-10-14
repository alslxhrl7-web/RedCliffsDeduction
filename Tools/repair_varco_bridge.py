"""Run inside the existing Blender window; never launches or closes Blender.
Keeps models intact and logs the real download/import exception without URLs.
"""
from pathlib import Path
import datetime
import json
import os
import re
import bpy

PROJECT = Path(__file__).resolve().parents[1]
LOG = PROJECT / 'Logs/VarcoBridgeRepair'
LOG.mkdir(parents=True, exist_ok=True)

def record(event, **fields):
    entry = {'time': datetime.datetime.now().isoformat(), 'event': event, **fields}
    with (LOG / 'events.jsonl').open('a', encoding='utf-8') as stream:
        stream.write(json.dumps(entry, ensure_ascii=False) + '\n')

def safe_error(error):
    return re.sub(r'https?://\S+', '[URL omitted]', str(error))

if bpy.app.background or bpy.context.window is None:
    raise RuntimeError('Use the existing visible project window; no background Blender.')
if not bpy.data.filepath or not Path(bpy.data.filepath).resolve().is_relative_to(PROJECT.resolve()):
    raise RuntimeError('The current Blender document is outside this project; no server changed.')
bpy.ops.preferences.addon_enable(module='bl_ext.user_default.varco3d_bridge_for_blender')
from bl_ext.user_default.varco3d_bridge_for_blender import server, operators

# Re-running this script does not accumulate wrapper functions.
if not hasattr(server, '_redcliffs_original_download'):
    server._redcliffs_original_download = server._download
if not hasattr(operators, '_redcliffs_original_import'):
    operators._redcliffs_original_import = operators._import_asset

def download(url):
    record('download_started')
    try:
        path, fmt = server._redcliffs_original_download(url)
        record('download_complete', bytes=path.stat().st_size, format=fmt)
        return path, fmt
    except Exception as error:
        record('download_failed', type=type(error).__name__, message=safe_error(error), http_status=getattr(error, 'code', None))
        raise

def import_asset(path, name, fmt):
    before = set(bpy.data.objects)
    try:
        result = operators._redcliffs_original_import(path, name, fmt)
        imported = [o for o in bpy.data.objects if o not in before]
        record('import_complete', meshes=sum(o.type == 'MESH' for o in imported), objects=len(imported))
        return result
    except Exception as error:
        record('import_failed', type=type(error).__name__, message=safe_error(error))
        raise

server._download = download
operators._import_asset = import_asset
props = bpy.context.scene.varco3d_props
props.asset_dir = str(PROJECT / 'ArtSource/VarcoDetail20261014/Source/BridgeIncoming')
props.server_port = 5324
try:
    if server.is_running():
        record('reused_existing_server', pid=os.getpid(), file=bpy.data.filepath)
    else:
        outcome = bpy.ops.varco3d.server_start()
        if 'FINISHED' not in outcome or not server.is_running():
            raise RuntimeError('The bridge did not start. Existing windows and servers were left intact.')
    record('ready', pid=os.getpid(), file=bpy.data.filepath, mesh_count=sum(o.type == 'MESH' for o in bpy.data.objects))
    print('REDCLIFFS VARCO BRIDGE READY. Models preserved. Export one prop from VARCO now.')
except Exception as error:
    record('start_failed', type=type(error).__name__, message=safe_error(error))
    raise
