import bpy, json
from mathutils import Vector
for sex in ('Male','Female'):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath='D:/unityprojects/Zombarvr/Assets/Zombar/Art/Characters/Zombie_'+sex+'.fbx')
 a=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 print(sex,'head pivot',list(a.matrix_world@a.data.bones['Head'].head_local),'neck',list(a.matrix_world@a.data.bones['Neck'].head_local))
 for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
  for m in o.data.materials:
   verts={v for p in o.data.polygons if o.data.materials[p.material_index]==m for v in p.vertices}
   xyz=[o.matrix_world@o.data.vertices[v].co for v in verts]
   if xyz: print(m.name, [tuple(round(fn(p[i] for p in xyz),3) for i in range(3)) for fn in (min,max)])
