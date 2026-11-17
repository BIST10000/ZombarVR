import bpy
from pathlib import Path
R=Path(__file__).resolve().parents[2]
for filename in ['Yaku_J_Ignite','Zombiegirl_W_Kurniawan']:
 path=R/'ArtSource/Blender/ExternalCharacters'/(filename+'.blend')
 bpy.ops.wm.open_mainfile(filepath=str(path))
 for mat in bpy.data.materials:
  if not (mat.name=='yifu' or mat.name.startswith('ZombieGirl_Material')):continue
  shader=mat.node_tree.nodes.get('Principled BSDF')
  if shader and shader.inputs['Base Color'].links:
   tex=shader.inputs['Base Color'].links[0].from_node
   if tex.type=='TEX_IMAGE':mat.node_tree.links.new(tex.outputs['Alpha'],shader.inputs['Alpha'])
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
