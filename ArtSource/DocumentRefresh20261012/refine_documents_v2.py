import bpy, bmesh, math, random, json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'V2'; OUT.mkdir(exist_ok=True)
PROJECT=ROOT.parents[1]
ASSETS=PROJECT/'Assets/Art/Evidence/VarcoRefined20261012V2'; ASSETS.mkdir(parents=True,exist_ok=True)
random.seed(208)

def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.91;p.inputs['Metallic'].default_value=0
 return m

def apply_modifiers(o):
 bpy.context.view_layer.objects.active=o
 for m in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)

def box(name,loc,size,mat,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 if bevel:
  m=o.modifiers.new('Soft worn edges','BEVEL');m.width=bevel;m.segments=2;apply_modifiers(o)
 return o

def cord(name,pts,mat,r=.0016):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=3;c.bevel_depth=r;c.bevel_resolution=2
 s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,co in zip(s.points,pts):p.co=(*co,1)
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
 return bpy.context.object

def setup(source):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.usd_import(filepath=str(ROOT/'Source'/source/'model.usda'))
 imported=[o for o in bpy.context.scene.objects if o.type=='MESH']
 points=[o.matrix_world@v.co for o in imported for v in o.data.vertices]
 lo=Vector(tuple(min(p[i] for p in points)for i in range(3)));hi=Vector(tuple(max(p[i]for p in points)for i in range(3)));size=hi-lo
 verts=[];faces=[]
 for o in imported:
  offset=len(verts);verts.extend([o.matrix_world@v.co for v in o.data.vertices]);faces.extend([tuple(offset+i for i in f.vertices)for f in o.data.polygons])
 tree=BVHTree.FromPolygons(verts,faces)
 def sample(x,y):
  result=tree.ray_cast(Vector((lo.x+(x+.5)*size.x,lo.y+(y+.5)*size.y,hi.z+1)),Vector((0,0,-1)))
  return 0 if result[0] is None else (result[0].z-lo.z)/size.z-.5
 # Keep the untouched imported source inside the editable blend, excluded from export/render.
 originals=bpy.data.collections.new('VARCO_ORIGINAL_DO_NOT_EXPORT');bpy.context.scene.collection.children.link(originals)
 for o in imported:
  for collection in list(o.users_collection):collection.objects.unlink(o)
  originals.objects.link(o)
 originals.hide_render=True;originals.hide_viewport=True
 return sample, sum(len(o.data.vertices)for o in imported), sum(len(f.vertices)-2 for o in imported for f in o.data.polygons)

def ink_strokes(z,mat,width,height,columns,rows):
 # Abstract brush marks, not new readable evidence. Exact wording remains in the evidence UI.
 verts=[];faces=[]
 for c in range(columns):
  for row in range(rows):
   if random.random()<.08:continue
   x=-width/2+(c+.5)*width/columns;y=-height/2+(row+.5)*height/rows
   for k in range(random.randint(2,4)):
    a=x+random.uniform(-.0025,.0025);b=y+random.uniform(-.003,.003)
    length=random.uniform(.002,.006);thick=random.uniform(.0008,.0012);idx=len(verts)
    verts.extend([(a,b,z),(a+length,b+.0005,z),(a+length,b+thick+.0005,z),(a,b+thick,z)]);faces.append((idx,idx+1,idx+2,idx+3))
 mesh=bpy.data.meshes.new('Subtle ink mesh');mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new('Decorative ink - read evidence in UI',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
 return o

def heading(text,loc,mat):
 bpy.ops.object.text_add(location=loc);o=bpy.context.object;o.name='Document heading'
 o.data.body=text;o.data.size=.021;o.data.align_x='CENTER';o.data.resolution_u=2
 o.data.font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
 o.data.materials.append(mat);bpy.ops.object.convert(target='MESH');return bpy.context.object

def paper(sample,layers=4,label='人員簿'):
 papers=[material('PaperIvory',(.64,.50,.31)),material('PaperEdge',(.53,.40,.23))]
 ink=material('InkFaded',(.075,.065,.047));hemp=material('HempCord',(.31,.22,.12))
 models=[];nx=24;ny=20
 heights=[[sample(i/nx-.5,j/ny-.5)for i in range(nx+1)]for j in range(ny+1)]
 for repeat in range(8):
  heights=[[sum(heights[yy][xx] for yy in range(max(0,j-1),min(ny,j+1)+1)for xx in range(max(0,i-1),min(nx,i+1)+1))/((min(ny,j+1)-max(0,j-1)+1)*(min(nx,i+1)-max(0,i-1)+1)) for i in range(nx+1)]for j in range(ny+1)]
 for layer in range(layers):
  verts=[];faces=[];base=.0015+layer*.0016
  for j in range(ny+1):
   v=j/ny
   for i in range(nx+1):
    u=i/nx;x=(u-.5)*.50+(layer%2)*.0025;y=(v-.5)*.38+(layer-1.5)*.002
    x+=.00055*math.sin(v*29+layer);y+=.0006*math.sin(u*24+layer)
    warp=heights[j][i]*.002+.0009*math.sin(u*math.pi)*math.sin(v*math.pi)
    edge_curl=.0015*(abs(u-.5)*2)**8*(abs(v-.5)*2)**4
    verts.append((x,y,base+warp+edge_curl))
  for j in range(ny):
   for i in range(nx):
    q=j*(nx+1)+i;faces.append((q,q+1,q+nx+2,q+nx+1))
  mesh=bpy.data.meshes.new('Retopologized paper');mesh.from_pydata(verts,[],faces);mesh.update()
  o=bpy.data.objects.new('Record page '+str(layer+1),mesh);bpy.context.collection.objects.link(o);o.data.materials.append(papers[0 if layer==layers-1 else 1])
  bpy.context.view_layer.objects.active=o;s=o.modifiers.new('Paper thickness','SOLIDIFY');s.thickness=.00065;s.offset=-1;apply_modifiers(o)
  models.append(o)
 # Markings follow a simple smooth reading plane without reproducing hallucinated lettering.
 models.append(ink_strokes(.0085,ink,.38,.26,16,14))
 if label:models.append(heading(label,(0,.148,.009),ink))
 # A folded fastening tab identifies a filed record, without adding a plot clue or seal.
 models.append(box('Filing tab',(-.215,0,.0085),(.018,.11,.0012),papers[1],.0005))
 models.append(cord('Filing thread',[(-.22,-.055,.010),(-.225,0,.011),(-.22,.055,.010)],hemp,.0011))
 return models

def bamboo(sample,label='軍令'):
 woods=[material('BambooHoney'+str(i),(.30+i*.018,.17+i*.012,.065+i*.007))for i in range(4)]
 ink=material('InkFaded',(.048,.042,.029));hemp=material('HempCord',(.27,.18,.085));models=[]
 for i in range(27):
  x=-.25+(i+.5)*.5/27;dy=.0009*math.sin(i*2.17)
  length=.34+sample((i+.5)/27-.5,0)*.002
  models.append(box('Bamboo slip '+str(i+1),(x,dy,.0035),(.5/27-.0006,length,.0065+sample(i/27-.5,0)*.0008),woods[i%4],.0010))
 models.append(ink_strokes(.0077,ink,.46,.21,27,14))
 models.append(heading(label,(0,.141,.008),ink))
 for y in [-.126,.126]:
  models.append(cord('Hemp binding',[(-.252,y,.0005),(-.255,y,.005),(-.251,y,.010),(0,y,.010),(.251,y,.010),(.255,y,.005),(.252,y,.0005),(-.252,y,.0005)],hemp))
  models.append(cord('Binding knot',[(.248,y,.010),(.258,y+.006,.010),(.266,y,.011),(.258,y-.005,.012),(.25,y,.010),(.276,y+.008,.008)],hemp,.0014))
 return models

def finish(name,source,creator):
 sample,before_v,before_t=setup(source);models=creator(sample)
 # Seat ink on the actual surface to avoid floating strokes and their detached shadows.
 solids=[o for o in models if not (o.name.startswith('Decorative ink') or o.name.startswith('Document heading'))]
 verts=[];faces=[]
 for obj in solids:
  offset=len(verts);verts.extend([obj.matrix_world@v.co for v in obj.data.vertices]);faces.extend([tuple(offset+i for i in f.vertices)for f in obj.data.polygons])
 surface=BVHTree.FromPolygons(verts,faces)
 for obj in models:
  if obj in solids:continue
  inverse=obj.matrix_world.inverted()
  for v in obj.data.vertices:
   p=obj.matrix_world@v.co;hit=surface.ray_cast(Vector((p.x,p.y,1)),Vector((0,0,-1)))[0]
   if hit is not None:p.z=hit.z+.00008;v.co=inverse@p
 bpy.ops.object.select_all(action='DESELECT')
 for o in models:o.select_set(True)
 bpy.context.view_layer.objects.active=models[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 bpy.ops.export_scene.fbx(filepath=str(ASSETS/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
 bpy.ops.export_scene.gltf(filepath=str(OUT/(name+'.glb')),export_format='GLB',use_selection=True)
 stats={'source':source,'beforeVertices':before_v,'beforeTriangles':before_t,'afterVertices':len(o.data.vertices),'afterTriangles':sum(len(p.vertices)-2 for p in o.data.polygons),'dimensionsMetres':list(o.dimensions),'method':'Retopology using sampled VARCO surface; bounded shallow deformation; rebuilt clean edges and fastening; original kept hidden in blend','materials':{m.name:list(m.diffuse_color)for m in o.data.materials},'readableEvidence':'Existing Unity evidence text unchanged'}
 (OUT/(name+'.json')).write_text(json.dumps(stats,indent=2),encoding='utf-8')
 center=Vector((0,0,.005));bpy.ops.object.camera_add(location=(.38,-.51,.72));cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.64;bpy.context.scene.camera=cam
 for loc,power in [((.5,-.6,1),28),((-.4,.3,.6),16)]:
  bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.size=.65;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1100;scene.render.resolution_y=850;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.13,.15,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
 scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 for screen in bpy.data.screens:
  for area in screen.areas:
   if area.type=='VIEW_3D':area.spaces.active.region_3d.view_distance=.8;area.spaces.active.region_3d.view_location=center
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
 print('REFINED',name,stats)

if __name__=='__main__':
 finish('PersonnelRecordRefined','6bff62a5-3ae1-43fb-9b11-387596a46f80',paper)
 finish('MaintenanceOrderRefinedV2','9dabdfc6-2394-4298-a106-0916228f31a4',bamboo)
