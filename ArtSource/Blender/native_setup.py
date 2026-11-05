import bpy,math
from mathutils import Euler
from pathlib import Path
def save_native(name,center,distance):
 R=Path(__file__).resolve().parents[2];folder=R/'ArtSource/Blender/Editable';folder.mkdir(exist_ok=True)
 for m in bpy.data.materials:
  if m.use_nodes:
   shader=m.node_tree.nodes.get('Principled BSDF')
   if shader and 'Portrait_' not in m.name:
    for link in list(shader.inputs['Alpha'].links):m.node_tree.links.remove(link)
    shader.inputs['Alpha'].default_value=1
   kind=next((k for k in ('Dermis','Leather','Twill') if 'Portrait_'+k in m.name),None)
   if kind and shader:
    maps=R/'Assets/Zombar/Art/Characters/PortraitMaps'
    texname={'Dermis':'Dermis','Leather':'Hide','Twill':'Twill'}[kind]
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(maps/(texname+'.png')),check_existing=True)
    m.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
    n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(maps/('DermisNormal.png' if kind=='Dermis' else 'HideNormal.png')),check_existing=True);n.image.colorspace_settings.name='Non-Color'
    node=m.node_tree.nodes.new('ShaderNodeNormalMap');node.inputs['Strength'].default_value=.65
    m.node_tree.links.new(n.outputs['Color'],node.inputs['Color']);m.node_tree.links.new(node.outputs['Normal'],shader.inputs['Normal'])
 for area in bpy.context.screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.region_3d.view_distance=distance;space.region_3d.view_location=center
   space.region_3d.view_rotation=Euler((math.radians(82),0,math.radians(11))).to_quaternion()
   space.overlay.show_extras=False;space.overlay.show_bones=False;space.shading.color_type='TEXTURE'
 text=bpy.data.texts.new('Export to Unity.py');text.write((R/'ArtSource/Blender/export_editable.py').read_text(encoding='utf-8-sig'))
 bpy.ops.file.pack_all()
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/(name+'.blend')),compress=True)
