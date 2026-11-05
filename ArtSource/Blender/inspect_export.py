import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='D:/unityprojects/Zombarvr/Assets/Zombar/Art/Characters/Adapted/Zombie_Male.fbx')
a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for mode in ['REST','POSE']:
 a.data.pose_position=mode;bpy.context.view_layer.update()
 for o in bpy.context.scene.objects:
  if o.type!='MESH':continue
  eo=o.evaluated_get(bpy.context.evaluated_depsgraph_get());xyz=[eo.matrix_world@v.co for v in eo.data.vertices]
  print(mode,o.name,len(xyz),[[round(fn(v[i] for v in xyz),2) for i in range(3)] for fn in (min,max)])
