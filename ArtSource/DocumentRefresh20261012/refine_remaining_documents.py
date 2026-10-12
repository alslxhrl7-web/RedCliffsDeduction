import bpy, sys, math, json
from pathlib import Path
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
import refine_documents_v2 as base
base.OUT=ROOT/'Remaining';base.OUT.mkdir(exist_ok=True)
base.ASSETS=ROOT.parents[1]/'Assets/Art/Evidence/VarcoRemaining20261012';base.ASSETS.mkdir(parents=True,exist_ok=True)

def scale_models(models,sx,sy,sz=1):
 for o in models:
  o.location.x*=sx;o.location.y*=sy;o.location.z*=sz
  for v in o.data.vertices:v.co.x*=sx;v.co.y*=sy;v.co.z*=sz
 return models

def duty(sample):
 models=base.paper(sample,layers=2,label='勤務簿')
 ink=bpy.data.materials['InkFaded']
 # Ledger rules distinguish the work log without inserting fabricated evidence text.
 for x in [-.19,-.085,.035,.19]:
  o=base.box('Decorative ink rule',(x,0,.006),(.00055,.27,.00008),ink)
  models.append(o)
 return models

def registry(sample):
 models=base.bamboo(sample,label='戶籍')
 wood=bpy.data.materials['BambooHoney0']
 models.append(base.box('Register backing',(0,0,-.003),(.51,.343,.005),wood,.001))
 return scale_models(models,.78,1)

def letter(sample):
 models=base.paper(sample,layers=1,label='')
 # A single opened, three-fold letter; subtle folds remain without holes or thick sides.
 for o in models:
  for v in o.data.vertices:
   if o.name.startswith('Record page'):
    crease=sum(.0012*math.exp(-((v.co.y-y)/.009)**2)for y in [-.063,.063])
    v.co.z+=crease
 models=scale_models(models,.60,.68)
 return models

sources={
 'DutyRecordRefined':('9dabdfc6-2394-4298-a106-0916228f31a4',duty),
 'HouseholdRegistryRefined':('8ae33142-18df-43e4-be34-895a6e47fe46',registry),
 'SecretLetterRefined':('6bff62a5-3ae1-43fb-9b11-387596a46f80',letter)}
for name,(source,creator)in sources.items():
 base.finish(name,source,creator)
 # Produce an alpha-background render for the existing evidence UI, not a second live camera.
 scene=bpy.context.scene;scene.render.film_transparent=True;scene.render.image_settings.color_mode='RGBA'
 scene.render.filepath=str(base.ASSETS/(name+'Preview.png'))
 bpy.ops.render.render(write_still=True)

(base.OUT/'source-map.json').write_text(json.dumps({name:{'studio':'https://3d.varco.ai/studio/'+source,'verified':'Workflow node > View Larger > Open in Studio, 2026-10-12','note':'Source reused where workflow points to the same generation; distinct refined document models'}for name,(source,_)in sources.items()},indent=2),encoding='utf-8')
