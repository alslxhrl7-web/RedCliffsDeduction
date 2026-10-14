"""Readable supply tent, metres, Z up; entrance on +Y. No external downloads.
Preserve named construction parts in the master; export a joined baked copy.
"""
import math
import bpy
from mathutils import Vector
from craft_standard import material


def build():
    parts = []
    cloth = material('Tent - undyed hemp', (.29,.23,.15), (.43,.36,.25), 110, rough=.9)
    trim = material('Tent - doubled hems', (.22,.17,.10), (.32,.25,.16), 100, rough=.9)
    wood = material('Tent - timber', (.12,.075,.034), (.24,.16,.075), 6, (6,6,.2))
    rope = material('Tent - hemp ties', (.25,.19,.11), (.38,.30,.19), 60)

    def finish(obj, name, mat):
        obj.name = name
        obj.data.materials.append(mat)
        parts.append(obj)
        return obj

    def rod(name, a, b, radius, mat):
        bpy.ops.object.select_all(action='DESELECT')
        delta = Vector(b)-Vector(a)
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=radius,
            depth=delta.length, location=(Vector(a)+Vector(b))*.5)
        obj=bpy.context.object
        obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
        return finish(obj,name,mat)

    def seam(name, points, radius=.012, mat=None):
        for i in range(len(points)-1):
            rod(name+' '+str(i),points[i],points[i+1],radius,mat or trim)

    def panel(name, fn, nx, ny, mat):
        verts=[fn(x/nx,y/ny) for y in range(ny+1) for x in range(nx+1)]
        faces=[]
        for y in range(ny):
            for x in range(nx):
                i=y*(nx+1)+x
                faces.append((i,i+1,i+nx+2,i+nx+1))
        mesh=bpy.data.meshes.new(name)
        mesh.from_pydata(verts,[],faces);mesh.update()
        obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
        finish(obj,name,mat)
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        mod=obj.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.006
        bpy.ops.object.modifier_apply(modifier=mod.name)
        return obj

    # Structure supports the roof, leaving a wide unobstructed entrance.
    for y in (-2.05,2.05):
        for x in (-2.9,2.9):
            rod('Corner upright',(x,y,0),(x,y,2.14),.065,wood)
        rod('Gable tie beam',(-2.9,y,2.1),(2.9,y,2.1),.045,wood)
        for side in (-1,1):
            rod('Roof rafter',(0,y,3.04),(side*2.95,y,2.1),.047,wood)
    rod('Ridge pole',(0,-2.25,3.04),(0,2.25,3.04),.065,wood)
    for side in (-1,1):
        rod('Eave rail',(side*2.9,-2.2,2.1),(side*2.9,2.2,2.1),.045,wood)

        def roof(u,v,s=side):
            return (s*3.06*u, -2.28+4.56*v,
                3.12-1.02*u-.10*math.sin(math.pi*u)*math.sin(math.pi*v)
                +.012*math.sin(v*math.pi*16)*math.sin(u*math.pi))
        panel('Sloping canvas roof '+str(side),roof,16,24,cloth)
        for v in (0,.25,.5,.75,1):
            seam('Roof seam',[roof(i/16,v) for i in range(17)],.008)
        seam('Eave hem',[roof(1,i/24) for i in range(25)],.015)
        panel('Side curtain '+str(side),
            lambda u,v,s=side:(s*(2.91+.035*math.sin(u*math.pi*18)*math.sin(v*math.pi)),
                -2.16+4.32*u,.055+2.04*v),24,12,cloth)
    panel('Closed rear wall',lambda u,v:(-2.91+5.82*u,
        -2.15-.025*math.sin(u*math.pi*20)*math.sin(v*math.pi),
        .055+v*(3.03-1.0*abs(2*u-1))),30,12,cloth)
    # Two curtains pulled to the posts at waist height; opening remains obvious.
    for side in (-1,1):
        def flap(u,v,s=side):
            inner=1.15+1.23*math.sin(math.pi*v)
            x=s*(inner+(2.91-inner)*u)
            return (x,2.16+.055*math.sin(u*math.pi*8)*math.sin(v*math.pi),
                    .055+2.04*v)
        panel('Tied entrance curtain '+str(side),flap,12,20,cloth)
        seam('Entrance hem',[flap(0,i/20) for i in range(21)],.014)
        seam('Curtain tie',[(side*2.37,2.12,1.05),(side*2.65,2.27,1.03),
            (side*2.95,2.13,1.04)],.018,rope)
        for y in (-2.04,2.04):
            stake=(side*3.50,y+(.40 if y>0 else -.40),.08)
            seam('Tension rope',[(side*2.96,y,2.10),
                (side*3.23,(y+stake[1])*.5,1.04),stake],.015,rope)
            rod('Ground peg',(stake[0],stake[1],0),
                (stake[0]-.06*side,stake[1],.28),.026,wood)

    edit=bpy.data.collections.new('EDITABLE_TENT_PARTS')
    bpy.context.scene.collection.children.link(edit)
    for obj in parts:
        copy=obj.copy();copy.data=obj.data.copy();edit.objects.link(copy)
        copy.hide_render=True;copy.hide_set(True)
    edit.hide_render=True
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
    result=bpy.context.object;result.name='CanvasDetails'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if not result.data.uv_layers:result.data.uv_layers.new(name='SourceUV')
    return result
