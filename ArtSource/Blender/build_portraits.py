"""Fit two hand-authored portrait variants to the licensed animated bodies."""
import bpy, bmesh, numpy as np, math, random, sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R=Path(__file__).resolve().parents[2]
data=np.load(R/'ArtSource/Blender/sculpt.npz')
out=R/('ArtSource/Blender/Review' if '--stage' in sys.argv else 'Assets/Zombar/Art/Characters/Adapted')
out.mkdir(exist_ok=True)
for sex in ['Male','Female']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(R/'Assets/Zombar/Art/Characters'/('Zombie_'+sex+'.fbx')))
 arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
 bm=bmesh.new(); bm.from_mesh(body.data)
 obsolete=[f for f in bm.faces if (body.matrix_world@f.calc_center_median()).z>2.12 or body.data.materials[f.material_index].name in ['Face','Hair','Brain']]
 bmesh.ops.delete(bm,geom=obsolete,context='FACES')
 bm.to_mesh(body.data);bm.free();body.data.update()
 materials={}
 def material(name,color,rough=.6,metal=0):
  m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
  b=m.node_tree.nodes['Principled BSDF'];b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=rough;b.inputs['Metallic'].default_value=metal
  materials[name]=m;return m
 dermis=material('Portrait_Dermis',(.34,.28,.17))
 leather=material('Portrait_Leather',(.085,.042,.025));thread=material('Portrait_Thread',(.35,.23,.13))
 dark=material('Portrait_Cavity',(.016,.009,.014));gum=material('Portrait_Gum',(.21,.033,.028),.4)
 bone=material('Portrait_Enamel',(.46,.39,.23),.32);eye=material('Portrait_Cornea',(.40,.45,.33),.18)
 iris=material('Portrait_Iris',(.16,.265,.16),.18);iron=material('Portrait_Suture',(.31,.27,.21),.29,.69)
 atlas=bpy.data.images.load(str(R/'Assets/Zombar/Art/Characters/PortraitMaps/Dermis.png'))
 tex=dermis.node_tree.nodes.new('ShaderNodeTexImage');tex.image=atlas;dermis.node_tree.links.new(tex.outputs['Color'],dermis.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
 V=data['vertices']; me=bpy.data.meshes.new('Sculpted facial planes')
 me.from_pydata(V.tolist(),[],data['faces'].tolist());me.update()
 obj=bpy.data.objects.new('Portrait',me);bpy.context.collection.objects.link(obj);me.materials.append(dermis)
 uv=me.uv_layers.new(name='Dermis UV')
 for poly in me.polygons:
  poly.use_smooth=True
  for li in poly.loop_indices:
   x,_,z=V[me.loops[li].vertex_index];uv.data[li].uv=((x+.42)/.84,(z+.14)/1.16)
 bpy.context.view_layer.objects.active=obj
 source_sculpt=me.copy() if '--editable' in sys.argv else None
 dec=obj.modifiers.new('Preserve silhouette','DECIMATE');dec.ratio=.23;bpy.ops.object.modifier_apply(modifier=dec.name)
 skin_tree=BVHTree.FromPolygons(V.tolist(),data['faces'].tolist())
 def on_skin(x,z,offset=0):
  hit=skin_tree.ray_cast(Vector((x,-1,z)),Vector((0,1,0)),2)[0]
  return (x,hit.y-offset if hit else -.20,z)
 pieces=[obj]
 def ball(name,loc,scale,mat,roll=0):
  bpy.ops.mesh.primitive_uv_sphere_add(segments=18,ring_count=12,location=loc)
  o=bpy.context.object;o.name=name;o.scale=scale;o.rotation_euler.y=roll;o.data.materials.append(mat)
  for p in o.data.polygons:p.use_smooth=True
  pieces.append(o);return o
 def cord(name,pts,radius,mat):
  c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=4;c.bevel_depth=radius;c.bevel_resolution=2;c.use_fill_caps=True
  s=c.splines.new('BEZIER');s.bezier_points.add(len(pts)-1)
  for p,v in zip(s.bezier_points,pts):
   p.co=v;p.handle_left_type=p.handle_right_type='AUTO'
  o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);c.materials.append(mat)
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');pieces.append(o)
 rnd=random.Random(135 if sex=='Male' else 826)
 exec((R/'ArtSource/Blender/portrait_details.py').read_text(),globals())
 wardrobe()
 living=-1 if sex=='Male' else 1
 for side in [-1,1]:
  x=side*.12
  ball('Orbital shadow',(x,-.196,.578),(.087,.028,.063),dark)
  if side==living:
   ball('Clouded eye',(x,-.235,.579),(.063,.054,.060),eye)
   make_lids(x)
   ball('Washed iris',(x+.009,-.285,.579),(.022,.006,.022),iris)
   ball('Pupil',(x+.010,-.294,.579),(.009,.003,.010),dark)
  cord('Lower lid',[(x-.078,-.245,.572),(x,-.264,.529),(x+.077,-.247,.582)],.008,gum)
  if side!=living:
   for j in range(4):
    px=x-.049+j*.032
    cord('Orbital staple',[(px,-.252,.554),(px+.004,-.274,.574),(px+.009,-.25,.598)],.004,iron)
 ball('Recess of mouth',(.018,-.195,.294),(.161,.044,.056),dark,-.1)
 for row in [0,1]:
  for j in range(10):
   if j in ([2,8] if row==0 else [1,4,9]):continue
   x=(j-4.5)*.027; z=.294-.10*x+([-.031,.033][row])
   tooth_crown(x,-.264+.049*(x/.15)**2,z,j,row)
 for j in range(4):
  z=.349+j*.020
  cord('Exposed cheek tendon',[(.136,-.26,z),(.189,-.245,z+.008),(.239,-.170,z+.044)],.007,gum)
 # A raised surgical seam arcs from temple to pate.
 pts=[on_skin(x,z,.002) for x,z in [(-.245,.60),(-.229,.636),(-.195,.673),(-.162,.695),(-.134,.717)]]
 cord('Incision',pts,.005,gum)
 for a,b in zip(pts,pts[1:]):
  for t in [.20,.73]:
   p=Vector(a).lerp(Vector(b),t)
   cord('Stitch',[on_skin(p.x-.007,p.z-.010,.001),on_skin(p.x,p.z,.005),on_skin(p.x+.007,p.z+.010,.001)],.003,thread)
 source_parts=[]
 if '--editable' in sys.argv:
  bpy.context.view_layer.update()
  for part in pieces:
   geo=source_sculpt if part==obj else part.data.copy()
   geo.transform(part.matrix_world)
   for vertex in geo.vertices:
    vertex.co*=.85;vertex.co.z+=2.105
    if sex=='Female':vertex.co.x*=.92
   source_parts.append((part.name,geo,part==obj))
 # Join the detailed parts into one rigidly skinned mesh with one skeleton.
 bpy.ops.object.select_all(action='DESELECT')
 for o in pieces:o.select_set(True)
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
 for v in obj.data.vertices:
  v.co*=.85;v.co.z+=2.105
  if sex=='Female':v.co.x*=.92
 def bind(o,bone_name):
  group=o.vertex_groups.new(name=bone_name);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
  world=o.matrix_world.copy();o.parent=arm;o.matrix_world=world
  mod=o.modifiers.new('Existing character skeleton','ARMATURE');mod.object=arm
 bind(obj,'Head')
 # Broad folded leather gorget at the neckline; the two shoulders differ in height.
 n=60; verts=[];faces=[]
 for z,rx,ry in [(1.84,.448,.28),(1.91,.39,.253),(1.98,.33,.236),(2.11,.258,.211),(2.17,.23,.19)]:
  for i in range(n):
   t=i*math.tau/n;rad=.012*math.sin(7*t+z*26)+.007*math.cos(11*t-z*8)
   verts.append(((rx+rad)*math.cos(t),(ry+rad)*math.sin(t),z+.045*math.cos(t-.3)+.018*math.cos(3*t)))
 for j in range(4):
  for i in range(n): faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 mesh=bpy.data.meshes.new('Folded collar');mesh.from_pydata(verts,[],faces);mesh.update()
 collar=bpy.data.objects.new('Waxed mortuary collar',mesh);bpy.context.collection.objects.link(collar);mesh.materials.append(leather)
 uv=mesh.uv_layers.new(name="Hide UV")
 for p in mesh.polygons:
  p.use_smooth=True
  for l in p.loop_indices:
   vi=mesh.loops[l].vertex_index;uv.data[l].uv=((vi%n)/n*2,vi//n/4)
 bind(collar,'Neck')
 bibverts=[];bibfaces=[]
 for j in range(17):
  f=j/16
  for i in range(25):
   u=i/12-1
   width=.49-.17*f+.06*math.sin(f*math.pi)
   x=u*width; z=1.09+f*.84+.018*math.cos(5*u+4*f)
   y=-.345+.10*u*u+.018*math.sin(14*u+6*f)+.008*math.sin(31*u-5*f)
   bibverts.append((x,y,z))
 for j in range(16):
  for i in range(24):
   a=j*25+i;bibfaces.append((a,a+1,a+26,a+25))
 bib=patch_mesh('Mortician apron',bibverts,bibfaces,leather);bind(bib,'Torso')
 # Attach a tarnished, riveted identification plate to the bib.
 pieces=[]
 ball('Copper ID plate',(-.18,-.354,1.76),(.10,.009,.053),iron,.11)
 for i in range(3):
  x=-.20+i*.018
  cord('Stamped tally',[(x-.009,-.366,1.736),(x+.002,-.366,1.783)],.003,dark)
 for o in pieces:
  bpy.context.view_layer.update()
  bind(o,'Torso')
 # Tailored outer jacket and separate sleeves follow the original arm animation.
 # Sampling along each rest bone preserves the shoulder and elbow articulation.
 textile=materials['Portrait_Twill']
 def garment(name,rings,mat,bonename):
  n=len(rings[0]);vs=[pt for ring in rings for pt in ring];fs=[]
  for k in range(len(rings)-1):
   for i in range(n): fs.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
  garment=patch_mesh(name,vs,fs,mat)
  uv=garment.data.uv_layers.active
  for poly in garment.data.polygons:
   col=poly.index%n;row=poly.index//n
   for li,(du,dv) in zip(poly.loop_indices,[(0,0),(1,0),(1,1),(0,1)]):
    uv.data[li].uv=((col+du)*2/n,(row+dv)/(len(rings)-1))
  bind(garment,bonename)
 jacket=[]
 for j,(z,rx,ry) in enumerate([(1.04,.425,.267),(1.11,.43,.27),(1.19,.41,.269),(1.39,.42,.29),(1.60,.46,.325),(1.80,.505,.344),(1.90,.53,.314),(2.01,.35,.234),(2.055,.22,.198)]):
  ring=[]
  for i in range(52):
   t=i*math.tau/52;pleat=.015*math.cos(9*t+1.5*z)+.006*math.cos(17*t-.8*z)
   ring.append(((rx+pleat)*math.cos(t),(ry+pleat)*math.sin(t),z+.023*math.cos(3*t+z)))
  jacket.append(ring)
 garment('Funeral coat',jacket,textile,'Torso')
 for side in ['L','R']:
  for part in ['UpperArm','LowerArm']:
   name=part+'.'+side;b=arm.data.bones[name]
   start=arm.matrix_world@b.head_local;end=arm.matrix_world@b.tail_local
   axis=(end-start).normalized();u=axis.cross(Vector((0,0,1))).normalized();v=axis.cross(u)
   upper=part=='UpperArm';rings=[]
   for j in range(17):
    f=j/16;t=f*(1.04 if upper else (.78 if sex=='Male' else .45))-.035
    center=start.lerp(end,t)
    rad=(.20-.045*f+.026*math.sin(f*math.pi)) if upper else (.166-.038*f)
    ring=[]
    for i in range(32):
     a=i*math.tau/32
     wrinkle=.012*math.sin(26*f+.85*math.sin(a*3))+.006*math.cos(7*a+5*f)
     cuff=.013*math.exp(-((f-.90)/.08)**2)
     rr=rad+wrinkle+cuff
     ring.append(tuple(center+rr*(math.cos(a)*u+math.sin(a)*v)))
    rings.append(ring)
   garment('Tailored '+name,rings,textile,name)
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/('Zombie_'+sex+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_simplify_factor=.2,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 print('PORTRAIT',sex,len(obj.data.vertices),len(obj.data.polygons))
 if '--editable' in sys.argv:
  bpy.data.objects.remove(obj,do_unlink=True)
  face=bpy.data.collections.new('Face details');bpy.context.scene.collection.children.link(face)
  for label,geo,is_sculpt in source_parts:
   part=bpy.data.objects.new('Face sculpt' if is_sculpt else label,geo);face.objects.link(part);bind(part,'Head')
   if is_sculpt:part['portrait_sculpt']=True;primary=part
  bpy.ops.object.select_all(action='DESELECT');primary.select_set(True);bpy.context.view_layer.objects.active=primary
  bpy.context.scene.frame_set(1)
  exec((R/'ArtSource/Blender/native_setup.py').read_text(encoding='utf-8-sig'),globals())
  save_native('Zombie_'+sex,(0,0,2.28),2.3)





