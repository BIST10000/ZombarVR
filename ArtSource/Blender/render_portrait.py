"""Contact sheet uses the exported production FBX, its UVs and production maps."""
import bpy, math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(R/'Assets/Zombar/Art/Characters/Adapted/Zombie_Male.fbx'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.data.pose_position='POSE'
idle=next((action for action in bpy.data.actions if action.name.lower().endswith('idle')),None)
if idle:
 arm.animation_data_create().action=idle
 arm.animation_data.use_nla=False
 bpy.context.scene.frame_set(int(idle.frame_range[0]))
for m in bpy.data.materials:
 if not m.use_nodes:continue
 shader=m.node_tree.nodes.get('Principled BSDF')
 if shader and 'Portrait_' not in m.name:
  for link in list(shader.inputs['Alpha'].links):m.node_tree.links.remove(link)
  shader.inputs['Alpha'].default_value=1
 if any(token in m.name for token in ['Portrait_Dermis','Portrait_Twill','Portrait_Leather']):
  for node in list(m.node_tree.nodes):
   if node.type=='TEX_IMAGE':m.node_tree.nodes.remove(node)
  node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(R/'Assets/Zombar/Art/Characters/PortraitMaps'/('Twill.png' if 'Twill' in m.name else 'Hide.png' if 'Leather' in m.name else 'Dermis.png')))
  m.node_tree.links.new(node.outputs['Color'],m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
def aim(o,point):o.rotation_euler=(Vector(point)-o.location).to_track_quat('-Z','Y').to_euler()
def area(loc,target,colour,power,size):
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.color=colour;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,target)
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('Cellar');scene.world.color=(.08,.095,.105)
bpy.ops.object.camera_add(location=(.80,-3.5,2.79));cam=bpy.context.object;cam.data.lens=69;aim(cam,(0,0,2.4));scene.camera=cam
area((-.95,-1.25,3.6),(0,0,2.4),(1,.79,.53),115,1.1)
area((1.2,.6,3.35),(0,0,2.35),(.30,.65,1),165,.9)
area((1.1,-2.1,2.2),(0,0,2.4),(.58,.7,.75),31,1.4)
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.resolution_x=900;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.filepath=str(R/'Builds/Validation/cellar-portrait.png');bpy.ops.render.render(write_still=True)


