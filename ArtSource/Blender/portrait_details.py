# Detail construction functions used by the portrait exporter. All coordinates are
# shared with the facial sculpt, so lids and stitches follow the animated head.
import math
def patch_mesh(name, verts, faces, mat, uvface=False):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);mesh.materials.append(mat)
 uv=mesh.uv_layers.new(name='Dermis UV')
 for p in mesh.polygons:
  p.use_smooth=True
  for l in p.loop_indices:
   x,y,z=verts[mesh.loops[l].vertex_index];uv.data[l].uv=((x+.42)/.84,(z+.14)/1.16) if uvface else (x*2+.5,z*2)
 return obj
def make_lids(x):
 for upper in [-1,1]:
  verts=[];faces=[]
  for j in range(6):
   f=j/5
   for i in range(25):
    t=i/24*2-1
    dx=.068*t
    dz=upper*((.008+.021*(1-t*t))*(1-f)+(.060+.015*(1-t*t))*f)+.007*t
    depth=math.sqrt(max(.001,1-(dx/.067)**2-(dz/.062)**2))
    y=-.235-.060*depth-.004+.018*f**3
    verts.append((x+dx,y,.579+dz))
  for j in range(5):
   for i in range(24):
    a=j*25+i;face=(a,a+1,a+26,a+25);faces.append(face if upper>0 else tuple(reversed(face)))
  pieces.append(patch_mesh('Heavy eyelid',verts,faces,dermis,True))
def wardrobe():
 textile=material('Portrait_Twill',(.074,.091,.087),.88)
 n=64;verts=[];faces=[]
 # A peaked dockworker cap and a cowl give the silhouettes distinct profiles.
 if sex=='Male':
  for z,rx,ry,drift in [(.733,.282,.245,0),(.765,.287,.254,0),(.80,.305,.27,-.012),(.85,.322,.282,-.024),(.918,.305,.253,-.036),(.956,.245,.21,-.05),(.978,.128,.11,-.06),(.987,.002,.002,-.065)]:
   for i in range(n):
    t=i*math.tau/n;loss=.006*math.sin(8*t+z*11)
    verts.append((drift+(rx+loss)*math.cos(t),(.035+(ry+loss)*math.sin(t)),z+.018*math.cos(t+.2)))
  for j in range(7):
   for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
  pieces.append(patch_mesh('Salt-stained peaked cap',verts,faces,textile))
  verts=[];faces=[]
  for j in range(4):
   f=j/3
   for i in range(33):
    t=(i/32-.5)*2; xx=t*.267
    y=-.07-.182*math.sqrt(max(0,1-t*t))-.10*f*(1-t*t)
    verts.append((xx,y,.757+.025*t-.027*f+.015*t*t))
  for j in range(3):
   for i in range(32):
    a=j*33+i;faces.append((a,a+33,a+34,a+1))
  pieces.append(patch_mesh('Cracked leather peak',verts,faces,leather))
  for z in [.756,.772]:
   cord('Cap piping',[(.286*math.cos(t),.035+.246*math.sin(t),z+.018*math.cos(t+.2)) for t in [i*math.tau/32 for i in range(33)]],.004,thread)
 else:
  verts=[];faces=[]
  for j,(z,rx,ry) in enumerate([(.07,.364,.258),(.22,.337,.278),(.43,.322,.30),(.66,.31,.29),(.81,.312,.272),(.91,.254,.234),(.958,.142,.144),(.97,.008,.008)]):
   for i in range(45):
    t=-.93+i/44*(math.pi+1.86);fold=.015*math.sin(12*t+z*6)
    verts.append(((rx+fold)*math.cos(t),.03+(ry+fold)*math.sin(t),z+.012*math.sin(3*t)))
  for j in range(7):
   for i in range(44):
    a=j*45+i;faces.append((a,a+1,a+46,a+45))
  pieces.append(patch_mesh('Undertaker hood',verts,faces,textile))

def tooth_crown(x,y,z,index,row):
 # Incisors have a broad, chipped biting edge and a narrower root.
 sign=1 if row==0 else -1
 verts=[];faces=[];n=12
 taper=.72 if index in [1,8] else 1
 for h,w,d in [(-.018,.010,.012),(-.010,.014,.015),(.009,.013,.013),(.019,.013*taper,.008),(.022,.011*taper,.006)]:
  for i in range(n):
   t=math.tau*i/n;xx=math.copysign(abs(math.cos(t))**.68,math.cos(t))*w
   yy=math.copysign(abs(math.sin(t))**.7,math.sin(t))*d
   verts.append((x+xx+h*.08,y+yy,z+sign*(h+xx*.12+.0015*math.cos(i*3+index))))
 for j in range(4):
  for i in range(n):
   f=(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i)
   faces.append(f if sign>0 else tuple(reversed(f)))
 faces.append(tuple(range(4*n,5*n)) if sign>0 else tuple(reversed(range(4*n,5*n))))
 pieces.append(patch_mesh('Chipped enamel',verts,faces,bone))
