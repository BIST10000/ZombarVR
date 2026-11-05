from pathlib import Path
import numpy as np
from scipy.ndimage import gaussian_filter
from scipy.spatial import cKDTree
from PIL import Image
out=Path(__file__).resolve().parents[2]/'Assets/Zombar/Art/Characters/PortraitMaps'
n=512;rng=np.random.default_rng(31);y,x=np.mgrid[:n,:n]
def octave(s):
 v=gaussian_filter(rng.normal(size=(n,n)),s,mode='wrap');return v/v.std()
points=rng.uniform(0,n,(2200,2));tree=cKDTree(points,boxsize=n)
p=np.stack([(x+octave(5)*1.6)%n,(y+octave(6)*1.6)%n],-1)
dist,_=tree.query(p,k=2);seams=np.exp(-((dist[:,:,1]-dist[:,:,0])/1.15)**2)
broad=octave(30);fine=octave(.8)
h=.42*fine+.34*octave(3)-1.4*seams
colour=np.array([100,65,44])+broad[:,:,None]*np.array([11,9,7])+h[:,:,None]*5.5
dy,dx=np.gradient(h);normal=np.stack([-dx*.40,dy*.40,np.ones_like(dx)],-1);normal/=np.linalg.norm(normal,axis=-1)[:,:,None]
warp=np.sin((x+y)*np.pi/2)+.65*np.cos((x-y)*np.pi/3)
twill=np.array([70,78,71])+broad[:,:,None]*6+warp[:,:,None]*7+fine[:,:,None]*3
for name,value in [('Hide',colour),('HideNormal',(normal*.5+.5)*255),('Twill',twill)]:
 Image.fromarray(np.clip(value,0,255).astype('uint8')).save(out/(name+'.png'))
