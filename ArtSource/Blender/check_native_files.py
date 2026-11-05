import bpy,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
target=R/'ArtSource/Blender/Review'
rows=[]
for name in ('Zombie_Male','Zombie_Female','BarProps'):
 bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/Blender/Editable'/(name+'.blend')))
 script=bpy.data.texts['Export to Unity.py'];scope={'__name__':'validate_export'};exec(script.as_string(),scope)
 if name=='BarProps':
  group=bpy.data.collections['Shaker'];bpy.context.view_layer.objects.active=next(o for o in group.objects if o.type=='MESH')
  result=target/'Check_Shaker.fbx'
 else:
  face=bpy.data.collections['Face details'];assert len(face.objects)>26
  assert len(next(o for o in face.objects if o.get('portrait_sculpt')).data.vertices)>20000
  assert all(im.packed_file is not None for im in bpy.data.images if im.source=='FILE' and im.users)
  result=target/(name+'.fbx')
 scope['export_asset'](result)
 assert result.stat().st_size>3000
 rows.append({'blend':name,'fbxBytes':result.stat().st_size,'meshes':sum(o.type=='MESH' for o in bpy.context.scene.objects)})
print('NATIVE ROUNDTRIP',rows)
(R/'Builds/Validation/editable-assets.json').write_text(json.dumps(rows,indent=2))
