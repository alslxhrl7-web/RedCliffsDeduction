"""Small anatomical features and reusable finger poses in hand-local metres."""
import math
import bpy
import numpy as np

DIGITS=[(-.026,.080,.0092,35),(-.008,.090,.010,40),(.011,.084,.0094,46),(.029,.066,.0077,52)]

def finger_fields(points,side):
    fields=[]
    for x,length,radius,curl in DIGITS:
        a=math.radians(curl)
        t=np.arcsin(np.clip((points[:,1]-.076)*a/length,-1,1))/a
        midz=-length*(1-np.cos(a*t))/a
        dx=points[:,0]-(x*side+x*.1*side*t)
        distance=np.sqrt(dx*dx+(points[:,2]-midz)**2)/radius
        fields.append((t,midz,dx,distance))
    return fields

def flex(points,side,digit,degrees):
    fields=finger_fields(points,side)
    nearest=np.argmin([f[3] for f in fields],axis=0)
    t,_,_,distance=fields[digit]
    weight=np.clip((t-.08)/.22,0,1)
    weight=weight*weight*(3-2*weight)
    weight*=((nearest==digit)&(distance<1.6)&(t>0))
    angle=np.radians(degrees)*weight
    result=points.copy();y=points[:,1]-.076;z=points[:,2]
    result[:,1]=.076+y*np.cos(angle)+z*np.sin(angle)
    result[:,2]=-y*np.sin(angle)+z*np.cos(angle)
    return result

def sculpt_and_nails(obj,side):
    points=np.array([v.co[:] for v in obj.data.vertices])
    fields=finger_fields(points,side)
    nearest=np.argmin([f[3] for f in fields],axis=0)
    for digit,(t,midz,dx,distance) in enumerate(fields):
        radius=DIGITS[digit][2]
        back=np.clip((points[:,2]-midz)/radius,0,1)
        mask=(nearest==digit)&(distance<1.5)&(t>.05)&(t<.94)
        folds=np.exp(-((t-.39)/.035)**2)+.7*np.exp(-((t-.68)/.03)**2)
        joints=np.exp(-((t-.34)/.07)**2)+.7*np.exp(-((t-.63)/.06)**2)
        points[mask,2]+=(.0008*joints-.0006*folds)[mask]*back[mask]
    # Subtle extensor tendons on the back, thenar pad on the palm.
    palm=(points[:,1]>.003)&(points[:,1]<.076)&(points[:,2]>.008)
    for x,_,_,_ in DIGITS[:3]:
        tendon=np.exp(-((points[:,0]-x*side*.65)/.0035)**2)*np.sin(np.clip(points[:,1]/.08,0,1)*math.pi)
        points[palm,2]+=.00065*tendon[palm]
    obj.data.vertices.foreach_set('co',points.ravel());obj.data.update()
    plates=[]
    for digit,(x,length,radius,curl) in enumerate(DIGITS):
        verts=[];faces=[];rows=9;cols=9;a=math.radians(curl)
        for row in range(rows):
            u=row/(rows-1);t=.73+.235*u;angle=a*t
            taper=radius*(1-.27*t)
            width=taper*.63*(.24+.76*math.sin(math.pi*u)**.4)
            for column in range(cols):
                q=column/(cols-1)*2-1;dx=width*q
                radial=taper*.78*math.sqrt(max(0,1-(dx/taper)**2))
                radial+=.00035*math.sin(math.pi*u)*(1-q*q)
                verts.append((x*side+x*.1*side*t+dx,.076+length*math.sin(angle)/a+radial*math.sin(angle),-length*(1-math.cos(angle))/a+radial*math.cos(angle)))
        for row in range(rows-1):
            for col in range(cols-1):
                i=row*cols+col;faces.append((i,i+1,i+cols+1,i+cols))
        mesh=bpy.data.meshes.new('Nail_'+str(digit));mesh.from_pydata(verts,[],faces);mesh.update()
        plate=bpy.data.objects.new('Nail_'+str(digit),mesh);bpy.context.scene.collection.objects.link(plate)
        mask=mesh.attributes.new('NailMask','FLOAT','POINT')
        for v in mask.data:v.value=1
        for p in mesh.polygons:p.use_smooth=True
        # Keep independent nail surfaces available in the source collection.
        source=bpy.data.collections['Finger construction'];copy=plate.copy();copy.data=plate.data.copy();source.objects.link(copy);copy.hide_render=True;copy.hide_viewport=True
        plates.append(plate)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
    for plate in plates:plate.select_set(True)
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
    obj.data.update()

def skin_detail(local,side,nails,cuff_start):
    detail=np.ones((len(local),3));roughness=np.full(len(local),.72)
    fields=finger_fields(local,side);nearest=np.argmin([f[3] for f in fields],axis=0)
    for digit,(t,midz,dx,distance) in enumerate(fields):
        back=(local[:,2]>midz);mask=(nearest==digit)&(distance<1.6)&(t>0)&(t<1)
        joints=np.exp(-((t-.36)/.09)**2)+.5*np.exp(-((t-.67)/.07)**2)
        crease=np.exp(-((t-.39)/.025)**2)+.65*np.exp(-((t-.68)/.025)**2)
        for channel,factor in enumerate([.045,.085,.095]):detail[mask,channel]-=(joints*factor+crease*.055)[mask]
        roughness[mask]+=joints[mask]*.05
    # Palm creases follow oblique arcs, not straight rings around the entire hand.
    palm=(local[:,2]<-.007)&(local[:,1]>.01)&(local[:,1]<.077)
    crease=np.exp(-((local[:,1]-(.052+side*local[:,0]*.35))/.0013)**2)
    crease+=.7*np.exp(-((local[:,1]-(.027-side*local[:,0]*.4))/.0015)**2)
    detail[palm]*=(1-.12*crease[palm,None])
    detail[local[:,1]<.003]*=.91;detail[cuff_start:]*=.91
    detail[nails]=[1.14,1.08,1.04];roughness[nails]=.48
    return np.clip(detail,.65,1.2),np.clip(roughness,.4,.85)

def bake_surface(hand,mat,base_color,detail,roughness,out):
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
    color=hand.data.color_attributes.new(name='AnatomicalDetail',type='FLOAT_COLOR',domain='POINT')
    rough=hand.data.color_attributes.new(name='SkinRoughness',type='FLOAT_COLOR',domain='POINT')
    for i,value in enumerate(detail):color.data[i].color=(*value,1);rough.data[i].color=(roughness[i],)*3+(1,)
    attr=nodes.new('ShaderNodeVertexColor');attr.layer_name=color.name
    ra=nodes.new('ShaderNodeVertexColor');ra.layer_name=rough.name
    links.new(ra.outputs['Color'],bs.inputs['Roughness'])
    noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=95;noise.inputs['Detail'].default_value=3
    ramp=nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color=(*np.clip(base_color*np.array([.88,.85,.84]),0,1),1)
    ramp.color_ramp.elements[1].color=(*np.clip(base_color*1.055,0,1),1)
    links.new(noise.outputs['Fac'],ramp.inputs[0])
    mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
    links.new(ramp.outputs[0],mix.inputs[1]);links.new(attr.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],bs.inputs['Base Color'])
    pores=nodes.new('ShaderNodeTexNoise');pores.inputs['Scale'].default_value=240;pores.inputs['Detail'].default_value=2
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.00012
    links.new(pores.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    scene=bpy.context.scene;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True;scene.render.bake.margin=8;scene.render.bake.normal_space='TANGENT'
    bpy.ops.object.select_all(action='DESELECT');hand.select_set(True);bpy.context.view_layer.objects.active=hand
    images={}
    for suffix,kind in [('', 'DIFFUSE'),('_Normal','NORMAL'),('_Roughness','ROUGHNESS')]:
        image=bpy.data.images.new(hand.name+suffix,512,512,alpha=True)
        if suffix:image.colorspace_settings.name='Non-Color'
        tex=nodes.new('ShaderNodeTexImage');tex.image=image;nodes.active=tex
        bpy.ops.object.bake(type=kind)
        if suffix=='_Roughness':
            pixels=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4);r=np.clip(pixels[:,0],0,1)
            pixels[:,:3]=0;pixels[:,3]=1-r;image.pixels.foreach_set(pixels.ravel());suffix='_Mask'
        image.filepath_raw=str(out/(hand.name+suffix+'.png'));image.file_format='PNG';image.save();image.pack();images[suffix]=image
    # Preview uses the same baked maps as the game.
    tex=nodes.new('ShaderNodeTexImage');tex.image=images[''];links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    normal=nodes.new('ShaderNodeNormalMap');nt=nodes.new('ShaderNodeTexImage');nt.image=images['_Normal'];links.new(nt.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bs.inputs['Normal'])
    mask=nodes.new('ShaderNodeTexImage');mask.image=images['_Mask'];invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(mask.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],bs.inputs['Roughness'])
