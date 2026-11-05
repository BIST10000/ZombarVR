import bpy, math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
models=sorted((R/'Assets/Zombar/Resources/PhysicalBar/Models').glob('*.fbx'))
for i,path in enumerate(models):
 old=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path));parts=list(set(bpy.data.objects)-old)
 group=bpy.data.collections.new(path.stem);bpy.context.scene.collection.children.link(group);group['prop_id']=path.stem
 anchor=bpy.data.objects.new(path.stem+' • origin',None);group.objects.link(anchor);anchor.empty_display_size=.03
 for part in parts:
  for c in list(part.users_collection):c.objects.unlink(part)
  group.objects.link(part)
  if part.parent is None:
   world=part.matrix_world.copy();part.parent=anchor;part.matrix_world=world
 anchor.location=((i%5-2)*.40,(i//5-1)*.43,0)
bpy.ops.object.select_all(action='DESELECT')
sample=next(o for o in bpy.data.collections['Shaker'].objects if o.type=='MESH')
sample.select_set(True);bpy.context.view_layer.objects.active=sample
exec((R/'ArtSource/Blender/native_setup.py').read_text(encoding='utf-8-sig'),globals())
save_native('BarProps',(0,0,.13),2.65)
