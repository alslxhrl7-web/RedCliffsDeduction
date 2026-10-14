"""Hand-built construction details replacing the damaged generated silhouette.
VARCO reference is retained separately by bake_props.py. No external textures.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector

def material(name, dark, light, scale, stretch=(1,1,1), rough=.85):
    m=bpy.data.materials.new(name);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links;b=next(x for x in n if x.type=='BSDF_PRINCIPLED')
    tex=n.new('ShaderNodeTexCoord');v=n.new('ShaderNodeVectorMath');v.operation='MULTIPLY';v.inputs[1].default_value=stretch;l.new(tex.outputs['Generated'],v.inputs[0])
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=2;l.new(v.outputs[0],noise.inputs[0])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*dark,1);ramp.color_ramp.elements[1].color=(*light,1);l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],b.inputs['Base Color'])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.0015;l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],b.inputs['Normal']);b.inputs['Roughness'].default_value=rough
    return m

def build():
    parts=[]
    wood=material('Oiled ash - long grain',(.13,.085,.042),(.31,.23,.13),7,(5,5,.15))
    woodtex=Path(__file__).resolve().parents[2]/'Assets/Art/Environment/CommandTent/Textures/Wood049_1K-JPG_Color.jpg'
    nt=wood.node_tree;img=nt.nodes.new('ShaderNodeTexImage');img.image=bpy.data.images.load(str(woodtex),check_existing=True);img.projection='FLAT'
    coord=nt.nodes.new('ShaderNodeUVMap');coord.uv_map='SourceUV';nt.links.new(coord.outputs['UV'],img.inputs['Vector']);nt.links.new(img.outputs['Color'],next(x for x in nt.nodes if x.type=='BSDF_PRINCIPLED').inputs['Base Color'])
    cloth=material('Oxide-dyed hemp',(.15,.027,.017),(.29,.067,.032),95,(1,1,.8))
    hem=material('Folded double hem',(.12,.023,.015),(.23,.053,.027),75)
    rope=material('Undyed hemp cord',(.22,.16,.078),(.43,.33,.18),65)
    def finish(o,name,mat):
        o.name=name;o.data.materials.append(mat);parts.append(o);return o
    def rod(name,a,b,r,mat,verts=12):
        d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=d.length,location=(Vector(a)+Vector(b))/2)
        o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();finish(o,name,mat)
        bevel=o.modifiers.new('Tool-rounded edge','BEVEL');bevel.width=min(.006,r*.18);bevel.segments=2
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        uv=o.data.uv_layers.active;uv.name='SourceUV'
        for loop in uv.data:
            a,b=loop.uv;loop.uv=(b,a*.4)
        return o
    def cord(name,pts,r,mat):
        c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=0;c.resolution_u=1
        s=c.splines.new('POLY');s.points.add(len(pts)-1)
        for p,v in zip(s.points,pts):p.co=(*v,1)
        o=bpy.data.objects.new(name,c);bpy.context.scene.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');finish(bpy.context.object,name,mat)
    rod('Solid upright',(-.61,0,0),(-.61,0,3),.063,wood,20)
    rod('Crossbar',(-.84,0,2.78),(.79,0,2.78),.048,wood,20)
    # Interlocking base, rather than an unsupported pole planted through the floor.
    for i,(a,b) in enumerate([((-.99,0,.065),(-.23,0,.065)),((-.61,-.30,.075),(-.61,.30,.075))]):rod('Ground shoe '+str(i),a,b,.06,wood)
    rod('Crossbar fixing peg',(-.61,-.095,2.78),(-.61,.095,2.78),.014,wood)
    def point(u,v,front=0):
        return (-.49+1.19*u, -.036+.044*math.sin(u*math.pi*6+.4)*(v*.6+.4)+.042*math.sin(v*math.pi)*math.sin(u*math.pi)+front, 2.64-1.87*v-.022*math.sin(u*math.pi)*v)
    nx,ny=24,36;vs=[point(x/nx,y/ny) for y in range(ny+1) for x in range(nx+1)];faces=[]
    for y in range(ny):
        for x in range(nx):
            i=y*(nx+1)+x;faces.append((i,i+1,i+nx+2,i+nx+1))
    me=bpy.data.meshes.new('Woven field topology');me.from_pydata(vs,[],faces);me.update();o=bpy.data.objects.new('Continuous cloth - repaired silhouette',me);bpy.context.scene.collection.objects.link(o);finish(o,o.name,cloth)
    bpy.context.view_layer.objects.active=o
    so=o.modifiers.new('Woven thickness','SOLIDIFY');so.thickness=.003;bpy.ops.object.modifier_apply(modifier=so.name)
    for label,pts in [('Left',[point(.006,i/60) for i in range(61)]),('Right',[point(.994,i/60) for i in range(61)]),('Bottom',[point(i/50,.994) for i in range(51)]),('Top',[point(i/50,.006) for i in range(51)])]:cord('Folded '+label+' edge',pts,.008,hem)
    # Deliberate stitch spacing and cloth attachment loops, not random surface noise.
    for edge in (0.012,.988):
        for j in range(42):cord('Edge stitch', [point(edge,j/42+.003,-.005),point(edge,j/42+.012,-.005)],.0016,rope)
    for i in range(6):
        x=-.45+i*.22
        cord('Suspension loop '+str(i),[(x,.067*math.cos(t),2.715+.087*math.sin(t)) for t in [j*math.tau/20 for j in range(21)]],.009,hem)
    for side in (-1,1):
        for turn in range(4):
            offset=(turn-1.5)*.018
            cord('Diagonal square lashing', [(-.61+.14*math.cos(t)+offset,.093*math.sin(t),2.78+side*.14*math.cos(t)-offset) for t in [j*math.tau/48 for j in range(49)]],.008,rope)
    cord('Tucked cord end',[(-.66,-.065,2.7),(-.69,-.07,2.63),(-.67,-.06,2.59)],.008,rope)
    # A small lower-corner repair uses the same weave, not a conspicuous decorative badge.
    verts=[point(u,v,-.006) for u,v in [(.77,.83),(.94,.83),(.94,.94),(.77,.94)]]
    me=bpy.data.meshes.new('Repair panel');me.from_pydata(verts,[],[(0,1,2,3)]);o=bpy.data.objects.new('Lower corner repair',me);bpy.context.scene.collection.objects.link(o);finish(o,o.name,hem)
    edit=bpy.data.collections.new('EDITABLE_CRAFT_PARTS');bpy.context.scene.collection.children.link(edit)
    for o in parts:
        dup=o.copy();dup.data=o.data.copy();edit.objects.link(dup);dup.hide_render=True;dup.hide_set(True)
    edit.hide_render=True
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='CampStandard';bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if not o.data.uv_layers.get('SourceUV'):o.data.uv_layers.new(name='SourceUV')
    o.data.uv_layers.active=o.data.uv_layers['SourceUV']
    return o
