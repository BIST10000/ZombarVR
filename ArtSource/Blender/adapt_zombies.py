import bpy, os, json, random
from mathutils import Vector
ROOT=r'D:/unityprojects/Zombarvr'
out=ROOT+'/Assets/Zombar/Art/Characters/Adapted'
os.makedirs(out,exist_ok=True)
report=[]
for sex in ['Male','Female']:
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=ROOT+'/Assets/Zombar/Art/Characters/Zombie_'+sex+'.fbx')
    arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    head=arm.data.bones['Head']; pivot=head.head_local.copy()
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        group=obj.vertex_groups.get('Head')
        if not group:continue
        to_arm=arm.matrix_world.inverted()@obj.matrix_world
        to_obj=to_arm.inverted()
        for v in obj.data.vertices:
            w=next((g.weight for g in v.groups if g.group==group.index),0)
            if w<=0:continue
            p=to_arm@v.co; delta=p-pivot
            # Keep a broad jaw, but remove the toy-like oversized cranium.
            adjusted=pivot+Vector((delta.x*.62,delta.y*.65,delta.z*.77))
            adjusted.x += .025*w*(1 if sex=='Male' else -1)
            v.co=to_obj@(p.lerp(adjusted,w))
        obj.data.update()
        report.append({'variant':sex,'vertices':len(obj.data.vertices),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'headScale':[.62,.65,.77]})
    # Bake retained skeletal animations into a separate derived FBX.
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out+'/Zombie_'+sex+'.fbx',use_selection=True,add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_simplify_factor=.2,axis_forward='-Z',axis_up='Y')
with open(ROOT+'/ArtSource/Blender/adaptation-report.json','w') as f:json.dump(report,f,indent=2)
print('ADAPTED',report)
