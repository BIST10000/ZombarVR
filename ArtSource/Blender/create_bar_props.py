import bpy, math, random, json, os
from mathutils import Vector
random.seed(17)
OUT = r'D:/unityprojects/Zombarvr/Assets/Zombar/Resources/PhysicalBar/Models'
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def material(name, color, metallic=0, rough=.6):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF'); b.inputs['Base Color'].default_value=(*color,1)
    b.inputs['Metallic'].default_value=metallic; b.inputs['Roughness'].default_value=rough
    return m
steel=material('Tarnished steel',(.22,.26,.25),.8,.33)
brass=material('Oxidised brass',(.32,.20,.065),.7,.48)
wood=material('Blackened wood',(.07,.032,.018),0,.83)
skin=material('Necrotic skin',(.24,.30,.14),0,.8)
red=material('Clotted blood',(.26,.012,.019),0,.28)
ivory=material('Yellowed sclera',(.6,.58,.34))
black=material('Socket',(.009,.007,.005))
green=material('Bottle green',(.035,.12,.075),.15,.25)
cloth=material('Soiled cloth',(.22,.19,.11),0,.96)
bone=material('Exposed bone',(.57,.49,.28))

def mesh(name,verts,faces,mat):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.data.materials.append(mat); return obj
def lathe(name,rings,mat,n=24):
    verts=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for z,r in rings for i in range(n)]
    faces=[]
    for j in range(len(rings)-1):
        for i in range(n): faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces.extend([tuple(range(n-1,-1,-1)),tuple((len(rings)-1)*n+i for i in range(n))])
    return mesh(name,verts,faces,mat)
def box(name,loc,size,mat,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.name=name; o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new('Worn edges','BEVEL'); mod.width=bevel; mod.segments=2
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o
def sphere(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mat); return o
def limb(name,points,radii,mat,n=10):
    verts=[]
    for (x,y,z),r in zip(points,radii):
        verts.extend([(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),z) for i in range(n)])
    faces=[]
    for j in range(len(points)-1):
        for i in range(n): faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces += [tuple(range(n-1,-1,-1)),tuple((len(points)-1)*n+i for i in range(n))]
    return mesh(name,verts,faces,mat)
def export(name):
    objs=list(bpy.context.scene.objects)
    for o in objs: o.select_set(True)
    # Blender Z-up converts to Unity Y-up through FBX.
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,object_types={'MESH'},
        add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
    bpy.ops.object.delete(use_global=False)

lathe('bottle',[(0,.045),(.02,.055),(.16,.055),(.19,.025),(.27,.022),(.28,.027)],green)
lathe('neck ring',[(.245,.024),(.255,.027)],brass)
box('blood label',(0,-.055,.1),(.07,.004,.075),cloth,.002)
box('red cross',(0,-.059,.1),(.012,.003,.05),red,.001)
box('cross bar',(0,-.06,.1),(.04,.003,.012),red,.001); export('BloodBottle')

lathe('shaker shell',[(0,.05),(.015,.052),(.17,.07),(.175,.07),(.175,.064),(.025,.048)],steel)
lathe('base ring',[(0,.052),(.012,.055)],brass); export('Shaker')
lathe('tumbler',[(0,.035),(.015,.043),(.13,.052),(.135,.052),(.135,.047),(.02,.038)],steel)
lathe('Liquid',[(.10,.047),(.105,.047)],red); export('Glass')

sphere('sclera',(0,0,.032),(.032,.029,.032),ivory)
sphere('iris',(0,-.027,.032),(.015,.005,.015),skin)
sphere('pupil',(0,-.032,.032),(.007,.003,.008),black)
limb('optic nerve',[(0,.02,.025),(.008,.045,.018),(.003,.065,.01)],[.008,.005,.002],red); export('Eye')
limb('muscle',[(0,0,0),(.007,0,.03),(-.008,.005,.08),(0,0,.10)],[.032,.045,.032,.012],red)
limb('bone',[(0,0,0),(0,0,.12)],[.009,.008],bone); export('Flesh')

box('grip',(0,0,.045),(.035,.035,.09),wood)
mesh('cleaver blade',[(-.015,0,.08),(.11,0,.10),(.11,0,.24),(-.015,0,.22),(-.015,.008,.08),(.11,.008,.10),(.11,.008,.24),(-.015,.008,.22)],
     [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],steel)
for z in [.025,.06]: sphere('rivet',(0,-.02,z),(.004,.002,.004),brass)
export('Cleaver')

verts=[((i-3)*.025,(j-3)*.025,.012*math.sin(i*1.8+j)) for j in range(7) for i in range(7)]
faces=[(j*7+i,j*7+i+1,(j+1)*7+i+1,(j+1)*7+i) for j in range(6) for i in range(6)]
mesh('folded rag',verts,faces,cloth); export('Rag')
box('pump grip',(0,0,0),(.22,.045,.045),wood)
lathe('pump stem',[(-.20,.013),(0,.013)],brass); export('Pump')
lathe('syringe barrel',[(-.06,.018),(.06,.018)],steel)
lathe('needle',[(.06,.004),(.15,.001)],steel)
box('plunger',(0,0,-.08),(.06,.02,.012),brass); export('Syringe')
lathe('coin',[(0,.026),(.006,.026)],brass)
box('coin scar',(0,-.002,.007),(.025,.007,.002),black,.001); export('Coin')
box('wooden offer',(0,0,.018),(.14,.1,.036),wood)
box('brass emblem',(0,-.01,.04),(.07,.05,.008),brass); export('Offer')
lathe('bell',[(0,.065),(.03,.05),(.075,.025),(.08,.018)],brass)
lathe('bell handle',[(.08,.012),(.16,.016)],wood); export('Bell')

for name in ['Hand','Arm']:
    if name=='Arm':
        limb('forearm',[(0,0,0),(.006,0,.12),(0,0,.26)],[.043,.038,.026],skin)
        offset=.26
    else: offset=0
    box('palm',(0,0,offset+.035),(.075,.035,.085),skin,.014)
    for i in range(4):
        x=(i-1.5)*.018
        limb('finger',[(x,0,offset+.06),(x,-.012,offset+.10),(x,-.035,offset+.115)],[.009,.008,.006],skin,8)
    limb('thumb',[(-.035,0,offset+.025),(-.058,-.01,offset+.05),(-.056,-.028,offset+.07)],[.012,.01,.007],skin,8)
    if name=='Arm': lathe('torn end',[(0,.042),(.01,.04)],red)
    export(name)

print('BAR_PROPS_EXPORTED',OUT)
