import bpy, math, json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
report=[]
for i,name in enumerate(['zombie','crawler','brute']):
 before=set(bpy.data.objects)
 bpy.ops.import_scene.gltf(filepath=str(R/'ArtSource/ExternalReview'/(name+'.glb')))
 objs=list(set(bpy.data.objects)-before);roots=[o for o in objs if not o.parent];meshes=[o for o in objs if o.type=='MESH']
 arm=next(o for o in objs if o.type=='ARMATURE');arm.animation_data_clear();arm.data.pose_position='REST';bpy.context.view_layer.update()
 xyz=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 height=max(p.z for p in xyz)-min(p.z for p in xyz)
 parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent)
 for o in roots:o.parent=parent
 parent.scale=(1.95/height,)*3;parent.location.x=(i-1)*1.8;parent.rotation_euler.z=0
 for m in [m for o in meshes for m in o.data.materials]:
  b=m.node_tree.nodes.get('Principled BSDF')
  if b:b.inputs['Metallic'].default_value=0;b.inputs['Roughness'].default_value=.7
 report.append({'name':name,'height':height,'tris':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),'actions':[a.name for a in bpy.data.actions]})
def area(loc,power,col):
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.color=col;o.data.size=4;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area((-3,-4,5),650,(1,.82,.65));area((3,1,3),900,(.4,.65,1))
bpy.ops.object.camera_add(location=(0,-8,2.8));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=5.8;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler()
s=bpy.context.scene;s.camera=cam;s.world=bpy.data.worlds.new('World');s.world.color=(.12,.12,.12);s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=True;s.render.resolution_x=1500;s.render.resolution_y=850;s.render.resolution_percentage=100;s.render.filepath=str(R/'ArtSource/ExternalReview/lineup.png')
(R/'ArtSource/ExternalReview/models-report.json').write_text(json.dumps(report,indent=2))
bpy.ops.render.render(write_still=True)
