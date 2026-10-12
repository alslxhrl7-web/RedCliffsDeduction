"""Original camp props. Blender background authoring; does not touch an open .blend."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
EXPORT=OUT.parents[2]/'Assets/Art/Props/CampRefresh20261012'
EXPORT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
materials={}
for name,color,metal,rough in [
 ('CampWood',(.24,.105,.039,1),0,.83),('CutWood',(.39,.22,.09,1),0,.8),
 ('Iron',(.09,.095,.087,1),.55,.65),('Bronze',(.24,.15,.06,1),.6,.56),
 ('Hemp',(.47,.35,.18,1),0,.95),('Canvas',(.56,.43,.27,1),0,.95),
 ('ClothBorder',(.23,.065,.036,1),0,.91),('Clay',(.23,.085,.034,1),0,.88),
 ('LampGlow',(1,.42,.08,1),0,.5)]:
 m=bpy.data.materials.new(name); m.diffuse_color=color; m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=color
 p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
 if name=='LampGlow': p.inputs['Emission Color'].default_value=color; p.inputs['Emission Strength'].default_value=2
 materials[name]=m

def finish(o,name,mat,bevel=0):
 o.name=name; o.data.materials.append(materials[mat])
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Soft tool-cut edges','BEVEL'); mod.width=bevel; mod.segments=2
  bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in o.data.polygons:p.use_smooth=False
 return o
def box(name,loc,size,mat='CampWood',bevel=.012):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.dimensions=size
 return finish(o,name,mat,bevel)
def cylinder(name,loc,radius,depth,mat='CampWood',vertices=16):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc)
 return finish(bpy.context.object,name,mat,.004)
def bar(name,a,b,r=.014,mat='Hemp',vertices=8):
 a,b=Vector(a),Vector(b); o=cylinder(name,(a+b)/2,r,(b-a).length,mat,vertices)
 o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); return o
def ring(name,loc,major,minor,mat='Iron',rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=20,minor_segments=6,location=loc,rotation=rot)
 return finish(bpy.context.object,name,mat)
def export(name,objs):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join(); o=bpy.context.object; o.name=name
 bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # Planar UVs on every timber face; Unity supplies shared wood/canvas maps.
 bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.01); bpy.ops.object.mode_set(mode='OBJECT')
 bpy.ops.export_scene.fbx(filepath=str(EXPORT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False)
 count=sum(len(p.vertices)-2 for p in o.data.polygons)
 return o,count
stats={}; models=[]
def build(name,fn):
 before=set(bpy.data.objects); fn(); o,n=export(name,[o for o in bpy.data.objects if o not in before]); stats[name]=n; models.append(o)

def crate():
 # One metre transport chest, separate lid boards, inset panels and iron straps.
 for z in range(5):
  h=.09+z*.175
  for y in [-.465,.465]:box('Side plank',(0,y,h),(.98,.07,.164),bevel=.009)
  for x in [-.465,.465]:box('End plank',(x,0,h),(.07,.86,.164),bevel=.009)
 for x in [-.37,.37]:
  for y in [-.506,.506]:box('Iron strap',(x,y,.44),(.058,.019,.83),'Iron',.005)
  for z in [.1,.42,.77]:
   for y in [-.522,.522]:
    o=cylinder('Rivet',(x,y,z),.017,.012,'Bronze',10);o.rotation_euler[0]=math.pi/2
 for i in range(6):box('Lid board',(-.416+i*.166,0,.934),(.158,1.03,.09),'CutWood',.01)
 for x in [-.37,.37]:box('Lid iron band',(x,0,.989),(.058,1.04,.018),'Iron',.004)
 box('Lock plate',(0,-.517,.77),(.16,.024,.23),'Iron',.008)
 ring('Carry ring',(0,-.565,.47),.075,.012,rot=(math.pi/2,0,0))
 for x in [-.38,.38]:box('Foot runner',(x,0,.018),(.16,.92,.036))

def desk():
 for i in range(5):box('Desktop plank',(-.48+i*.24,0,.96),(.233,.8,.08),'CutWood',.008)
 for x in [-.48,.48]:
  for y in [-.27,.27]:
   box('Mortised leg',(x,y,.46),(.115,.115,.92),bevel=.013)
   box('Leg collar',(x,y,.79),(.14,.14,.045),'CutWood',.007)
  box('Side stretcher',(x,0,.25),(.075,.59,.065))
 for y in [-.28,.28]:
  box('Apron',(0,y,.84),(1.03,.075,.14))
  for x in [-.47,.47]:
   o=cylinder('Wood peg',(x,y-.043 if y<0 else y+.043,.85),.016,.01,'CutWood',10);o.rotation_euler[0]=math.pi/2
 box('Lower cross rail',(0,0,.25),(1.02,.065,.065))

def lamp():
 # Wood stand and protected bronze oil lamp instead of an opaque glowing cylinder.
 box('Splayed base',(0,0,.04),(.48,.4,.08))
 cylinder('Round post',(0,0,1.02),.053,1.97)
 bar('Bracket',(0,0,2.02),(.43,0,2.02),.044,'CampWood',12)
 bar('Brace',(0,0,1.69),(.34,0,2.01),.03,'CampWood',10)
 for z in [1.90,1.94,1.98]:ring('Post lashing',(0,0,z),.057,.01,'Hemp')
 x=.36
 ring('Hanging loop',(x,0,1.94),.065,.009,'Iron',(math.pi/2,0,0))
 cylinder('Lamp foot',(x,0,1.38),.19,.045,'Bronze',24)
 cylinder('Clay oil dish',(x,0,1.43),.128,.07,'Clay',24)
 ring('Bowl lip',(x,0,1.465),.125,.017,'Bronze')
 for a in range(4):
  t=a*math.pi/2; bar('Protective rib',(x+.155*math.cos(t),.155*math.sin(t),1.40),(x+.12*math.cos(t),.12*math.sin(t),1.83),.012,'Bronze')
 for z in [1.52,1.66]:ring('Guard hoop',(x,0,z),.147,.009,'Bronze')
 bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=.21,radius2=.06,depth=.11,location=(x,0,1.85));finish(bpy.context.object,'Smoke hood','Bronze',.006)
 cylinder('Hood knob',(x,0,1.925),.032,.05,'Bronze')
 bar('Wick',(x,0,1.45),(x,0,1.51),.012,'CampWood')
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=(x,0,1.565));o=bpy.context.object;o.scale=(.026,.026,.065);finish(o,'Flame','LampGlow')

def canopy():
 # Slightly sagging gabled canvas, stitched hem, timber poles and tied guy ropes.
 verts=[];faces=[];nx,ny=48,32
 for j in range(ny+1):
  y=-4.5+9*j/ny
  for i in range(nx+1):
   x=-5.5+11*i/nx
   z=2.72+1.1*(1-abs(x)/5.5)-.10*math.sin(math.pi*abs(x)/5.5)+.027*math.cos(j*.9+i*.65)
   verts.append((x,y,z))
 for j in range(ny):
  for i in range(nx):
   a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
 mesh=bpy.data.meshes.new('Woven sagging canvas');mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new('Gabled canopy',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(materials['Canvas'])
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 mod=o.modifiers.new('Canvas thickness','SOLIDIFY');mod.thickness=.015;bpy.ops.object.modifier_apply(modifier=mod.name)
 for y in [-4.5,4.5]:
  bar('Ridge support',(0,y,0),(0,y,3.84),.065,'CampWood',12)
  for x in [-5.5,5.5]:
   bar('Corner pole',(x,y,0),(x,y,2.78),.065,'CampWood',12)
   bar('Gable hem',(x,y,2.72),(0,y,3.82),.035,'ClothBorder')
   end=(x*1.25,y*1.22,.08);bar('Guy rope',(x,y,2.65),end,.014,'Hemp')
   bar('Tent stake',(end[0],end[1],-.05),(end[0]+.08,end[1],.3),.034,'CampWood')
 for x in [-5.5,5.5]:bar('Side hem',(x,-4.5,2.72),(x,4.5,2.72),.033,'ClothBorder')
 bar('Ridge beam',(0,-4.5,3.79),(0,4.5,3.79),.052,'CampWood',12)
 for x in [-3.3,3.3]:
  for y in [-2.3,2.3]:
   top=2.72+1.1*(1-abs(x)/5.5)-.1*math.sin(math.pi*abs(x)/5.5)
   bar('Inner support',(x,y,0),(x,y,top),.09,'CampWood',12)
   for z in [2.45,2.48,2.51]:ring('Pole tie',(x,y,z),.094,.009,'Hemp')

build('TransportChest',crate);build('FieldDesk',desk);build('CampOilLamp',lamp);build('SupplyCanopy',canopy)
for i,o in enumerate(models):o.location.x=i*18
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CampProps_20261012.blend'))
(OUT/'mesh-stats.json').write_text(json.dumps(stats,indent=2))
print('PROP_EXPORT_COMPLETE',stats)
