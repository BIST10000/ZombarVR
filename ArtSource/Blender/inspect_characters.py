import bpy, json, os
from mathutils import Vector
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=r'D:/unityprojects/Zombarvr/Assets/Zombar/Art/Characters/Zombie_Male.fbx')
report={'objects':[], 'actions':[a.name for a in bpy.data.actions]}
for obj in bpy.context.scene.objects:
    row={'name':obj.name,'type':obj.type,'dimensions':list(obj.dimensions)}
    if obj.type=='ARMATURE':row['bones']=[b.name for b in obj.data.bones]
    if obj.type=='MESH':row.update(vertices=len(obj.data.vertices),polygons=len(obj.data.polygons),materials=[m.name for m in obj.data.materials])
    report['objects'].append(row)
with open(r'D:/unityprojects/Zombarvr/ArtSource/Blender/character-inspection.json','w') as f:json.dump(report,f,indent=2)
print(json.dumps(report))
