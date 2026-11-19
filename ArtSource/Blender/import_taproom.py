from pathlib import Path
import numpy as np, json,struct,io
from PIL import Image,ImageOps
from scipy.spatial.transform import Rotation
out=Path('Assets/Zombar/Art/Taproom')
for name,uid,width in [('Bell','cc21b304c4bb4750bdde7a8fb538b116',.135),('Rag','0aeaa109e7bd49e19a6bd4963cd08aad',.17)]:
 b=(Path('ArtSource/EnvironmentReview')/(uid+'.glb')).read_bytes(); end=20+struct.unpack_from('<I',b,12)[0]; d=json.loads(b[20:end]); data=b[end+8:]
 def acc(idx):
  a=d['accessors'][idx];v=d['bufferViews'][a['bufferView']];dt=np.dtype({5126:'<f4',5123:'<u2',5125:'<u4'}[a['componentType']]);w={'VEC3':3,'VEC2':2,'SCALAR':1}[a['type']]
  return np.ndarray((a['count'],w),dt,data,v.get('byteOffset',0)+a.get('byteOffset',0),(v.get('byteStride',w*dt.itemsize),dt.itemsize)).copy()
 parts=[]
 def walk(idx,parent):
  n=d['nodes'][idx];local=np.array(n.get('matrix',np.eye(4).flatten())).reshape(4,4).T.copy()
  if 'matrix' not in n:
   local[:3,:3]=Rotation.from_quat(n.get('rotation',[0,0,0,1])).as_matrix()@np.diag(n.get('scale',[1,1,1]));local[:3,3]=n.get('translation',[0,0,0])
  world=parent@local
  if 'mesh' in n:
   for prim in d['meshes'][n['mesh']]['primitives']:
    a=prim['attributes'];v=acc(a['POSITION'])@world[:3,:3].T+world[:3,3];norm=acc(a['NORMAL'])@np.linalg.inv(world[:3,:3]);norm/=np.maximum(1e-9,np.linalg.norm(norm,axis=1,keepdims=True));uv=acc(a['TEXCOORD_0']);idxs=acc(prim['indices']).reshape(-1,3)+1;parts.append((v,norm,uv,idxs,prim['material']))
  for child in n.get('children',[]):walk(child,world)
 for idx in d['scenes'][0]['nodes']:walk(idx,np.eye(4))
 allv=np.vstack([s[0] for s in parts]);center=(allv.max(0)+allv.min(0))*.5;center[1]=allv.min(0)[1];scale=width/max(np.ptp(allv,axis=0));print(name,np.ptp(allv,axis=0)*scale)
 for i,(v,n,uv,faces,mat) in enumerate(parts):
  stem=name+str(i);m=d['materials'][mat]
  with (out/(stem+'.obj')).open('w') as f:
   f.write('o '+stem+'\n');np.savetxt(f,(v-center)*scale,fmt='v %.7f %.7f %.7f');np.savetxt(f,np.column_stack((uv[:,0],1-uv[:,1])),fmt='vt %.7f %.7f');np.savetxt(f,n,fmt='vn %.7f %.7f %.7f')
   for face in faces:f.write('f '+' '.join(f'{j}/{j}/{j}' for j in face)+'\n')
  for suffix,info in [('Color',m['pbrMetallicRoughness']['baseColorTexture']),('Normal',m.get('normalTexture'))]:
   if not info:continue
   image=d['images'][d['textures'][info['index']]['source']];v=d['bufferViews'][image['bufferView']];p=v.get('byteOffset',0)
   im=Image.open(io.BytesIO(data[p:p+v['byteLength']])).convert('RGB');im.thumbnail((1024,1024));im.save(out/(stem+suffix+'.png'))
rough=Image.open(out/'Floor_rough.jpg').convert('L')
mask=Image.new('RGBA',rough.size,(0,0,0,255));mask.putalpha(ImageOps.invert(rough));mask.save(out/'FloorMask.png')
