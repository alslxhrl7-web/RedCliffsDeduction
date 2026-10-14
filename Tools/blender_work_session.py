"""Project-local job queue in the existing Blender UI; no network or startup hook.
Requests require the current session ID and one explicitly supported operation.
Run once per Blender session; never starts, closes, or switches Blender files.
"""
from pathlib import Path
import datetime
import json
import os
import runpy
import traceback
import uuid
import bpy

ROOT=Path(__file__).resolve().parents[1]
QUEUE=ROOT/'Logs/BlenderWorkSession'
KEY='RedCliffsDeduction.WorkSession'
PROPS=('CanvasDetails','SupplyLoad','FieldTable','ArchiveShelf')+tuple(p.stem for p in (ROOT/'ArtSource/ModelQuality20261014/Input').glob('*.json'))
PROPS+=tuple('Kit_'+p.stem for p in (ROOT/'ArtSource/BlockoutReplacement20261014/Recipes').glob('*.json'))


def write(path, value):
    temporary=path.with_suffix('.tmp')
    temporary.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
    temporary.replace(path)


def project_ready():
    return (not bpy.app.background and bool(bpy.context.window_manager.windows)
        and bool(bpy.data.filepath)
        and Path(bpy.data.filepath).resolve().is_relative_to(ROOT.resolve()))


def install():
    if not project_ready():raise RuntimeError('Open this project in the existing visible Blender first.')
    old=bpy.app.driver_namespace.get(KEY)
    if old and bpy.app.timers.is_registered(old['tick']):
        print('REDCLIFFS WORK SESSION ALREADY READY');return
    QUEUE.mkdir(parents=True,exist_ok=True)
    state={'session':uuid.uuid4().hex,'pid':os.getpid(),'stage':'ready'}

    def heartbeat():
        write(QUEUE/'status.json',{**state,'file':bpy.data.filepath,
            'time':datetime.datetime.now().isoformat(),'project_ready':project_ready()})

    def tick():
        try:
            heartbeat()
            request=QUEUE/'request.json'
            if not request.exists():return 1.0
            job=json.loads(request.read_text(encoding='utf-8'))
            job_id=job.get('id','')
            if not isinstance(job_id,str) or not job_id.isalnum() or len(job_id)>64:
                raise ValueError('Job ID must be 1-64 alphanumeric characters')
            archive=QUEUE/(job_id+'.request.json')
            if archive.exists():raise RuntimeError('Duplicate job ID; refusing replay')
            request.rename(archive)
            state.update(stage='running',job=job_id)
            heartbeat()
            try:
                if job.get('session')!=state['session']:raise RuntimeError('Stale session request')
                if not project_ready():raise RuntimeError('Current document is outside this project')
                window=bpy.context.window_manager.windows[0]
                with bpy.context.temp_override(window=window):
                    operation=job.get('operation')
                    if operation=='inspect':
                        result={'scene':window.scene.name,'objects':[
                            {'name':o.name,'type':o.type,'dimensions':list(o.dimensions)}
                            for o in window.scene.objects if not o.hide_render]}
                    elif operation=='refine_prop':
                        prop=job.get('prop')
                        if prop not in PROPS:raise ValueError('Unknown prop')
                        runpy.run_path(str(ROOT/'Tools/finish_received_props.py'),
                            init_globals={'PROP_NAME':prop})
                        result={'prop':prop}
                    elif operation=='review_front':
                        scene=window.scene
                        if not scene.name.startswith('CanvasDetails_Workbench'):
                            raise RuntimeError('Select the completed tent workbench first')
                        camera=scene.camera
                        if camera is None:raise RuntimeError('Missing review camera')
                        from mathutils import Vector
                        previous=(camera.location.copy(),camera.rotation_euler.copy(),scene.render.filepath)
                        try:
                            camera.location=(5,7,4)
                            camera.rotation_euler=(Vector((0,0,1.5))-camera.location).to_track_quat('-Z','Y').to_euler()
                            scene.render.filepath=str(QUEUE/'CanvasDetails-front.png')
                            bpy.ops.render.render(write_still=True)
                            result={'render':scene.render.filepath}
                        finally:
                            camera.location,camera.rotation_euler,scene.render.filepath=previous
                    else:raise ValueError('Unsupported operation')
                write(QUEUE/(job_id+'.result.json'),{'status':'complete','result':result})
                state.update(stage='ready',last_completed=job_id)
            except Exception:
                write(QUEUE/(job_id+'.result.json'),{'status':'failed','traceback':traceback.format_exc()})
                state.update(stage='ready',last_failed=job_id)
            heartbeat()
        except Exception:
            state.update(stage='blocked',error=traceback.format_exc())
            heartbeat()
            # Malformed requests are left intact for inspection, without repeated retries.
            return None
        return 1.0

    bpy.app.driver_namespace[KEY]={'tick':tick,'session':state['session']}
    bpy.app.timers.register(tick,first_interval=.2)
    heartbeat()
    print('REDCLIFFS WORK SESSION READY. Keep this Blender window open.')


if __name__ in ('__main__','<run_path>'):install()
