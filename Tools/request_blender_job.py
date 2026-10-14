"""Send one allowlisted job to the already-running project Blender session."""
from pathlib import Path
import argparse
import datetime
import json
import uuid

root=Path(__file__).resolve().parents[1]
queue=root/'Logs/BlenderWorkSession'
parser=argparse.ArgumentParser()
parser.add_argument('operation',choices=['inspect','review_front','refine_prop'])
characters=[p.stem for p in (root/'ArtSource/ModelQuality20261014/Input').glob('*.json')]
characters+=['Kit_'+p.stem for p in (root/'ArtSource/BlockoutReplacement20261014/Recipes').glob('*.json')]
parser.add_argument('--prop',choices=['CanvasDetails','SupplyLoad','FieldTable','ArchiveShelf']+characters)
args=parser.parse_args()
state=json.loads((queue/'status.json').read_text(encoding='utf-8'))
age=(datetime.datetime.now()-datetime.datetime.fromisoformat(state['time'])).total_seconds()
if age>10 or age<0 or state['stage']!='ready' or not state['project_ready']:
    raise SystemExit('Blender is not ready in this project. Do not launch another instance or retry blindly.')
if args.operation=='refine_prop' and not args.prop:raise SystemExit('--prop is required')
request=queue/'request.json'
if request.exists():raise SystemExit('A request is already pending')
job={'id':uuid.uuid4().hex,'session':state['session'],'operation':args.operation,'prop':args.prop}
temporary=queue/(job['id']+'.tmp')
temporary.write_text(json.dumps(job),encoding='utf-8')
# Windows rename refuses an existing destination; concurrent submissions cannot overwrite.
temporary.rename(request)
print(json.dumps({'submitted':job['id'],'result':str(queue/(job['id']+'.result.json'))}))
