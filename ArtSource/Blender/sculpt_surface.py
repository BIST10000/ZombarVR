"""Build the sculpt and the UV-matched skin maps for the two bar regulars."""
from pathlib import Path
import numpy as np
from scipy.ndimage import gaussian_filter
from PIL import Image
R = Path(__file__).resolve().parents[2]
out = R/'Assets/Zombar/Art/Characters/PortraitMaps'
out.mkdir(exist_ok=True)
rng = np.random.default_rng(94817)
step = .010
axes = [np.arange(a,b,step) for a,b in [(-.42,.42),(-.52,.42),(-.14,1.02)]]
X,Y,Z = np.meshgrid(*axes, indexing='ij', sparse=True)
def ell(cx,cy,cz,rx,ry,rz,tilt=0):
    xx=X-cx; zz=Z-cz-tilt*xx
    return (np.sqrt((xx/rx)**2+((Y-cy)/ry)**2+(zz/rz)**2)-1)*min(rx,ry,rz)
def blend(a,b,k):
    return np.minimum(a,b) - np.maximum(k-np.abs(a-b),0)**2/(4*k)
def subtract(a,b,k):
    return -blend(-a,b,k)
# Cranium, jaw, masseters, superciliary ridges, malar shelves and nose.
d=ell(0,.045,.65,.295,.255,.295)
for spec in [(0,.055,.2,.13,.13,.30),(0,-.095,.39,.224,.205,.225),(0,-.154,.185,.14,.145,.095),(.012,-.275,.525,.046,.088,.136),(.019,-.322,.422,.06,.073,.044)]:
    d=blend(d,ell(*spec),.043)
for side in [-1,1]:
    for spec in [(side*.19,-.115,.34,.073,.105,.145,side*.65),
                 (side*.166,-.197,.477,.101,.092,.063,side*.32),
                 (side*.123,-.202,.65,.126,.084,.058,side*.19),
                 (side*.053,-.286,.405,.046,.061,.037,0),
                 (side*.277,.015,.491,.037,.046,.083,side*.22)]:
        d=blend(d,ell(*spec),.028)
    d=subtract(d,ell(side*.121,-.283,.578,.098,.117,.074,side*.15),.012)
    d=subtract(d,ell(side*.044,-.34,.391,.029,.047,.028,side*.44),.006)
# Crooked opening through the lips; one cheek has sloughed away.
d=subtract(d,ell(.022,-.299,.294,.164,.119,.054,-.10),.013)
d=subtract(d,ell(.198,-.218,.369,.066,.09,.083,.65),.011)
d+=.0028*np.sin(31*X+16*Z)*np.cos(27*Y-12*Z)+.0014*np.sin(73*Z+13*Y)*np.cos(61*X)
# Surface nets, averaged crossing positions inside each active cube.
corners=np.array([[i,j,k] for i in (0,1) for j in (0,1) for k in (0,1)])
shape=np.array(d.shape)-1
s=[d[tuple(slice(t,t+n) for t,n in zip(c,shape))] for c in corners]
active=np.logical_and(np.min(s,axis=0)<0,np.max(s,axis=0)>=0)
idx=np.argwhere(active); v=np.zeros((len(idx),3)); counts=np.zeros(len(idx))
for i,a in enumerate(corners):
 for j,b in enumerate(corners[i+1:],i+1):
    if np.abs(a-b).sum()!=1: continue
    f=s[i][active];g=s[j][active]; hit=(f<0)!=(g<0)
    v[hit]+=a+(b-a)*(f[hit]/(f[hit]-g[hit]))[:,None];counts[hit]+=1
v=(idx+v/counts[:,None])*step+np.array([a[0] for a in axes])
ids=np.full(tuple(shape),-1);ids[active]=np.arange(len(v))
faces=[]
for axis in range(3):
 u=np.eye(3,dtype=int)[(axis+1)%3];w=np.eye(3,dtype=int)[(axis+2)%3]
 cross=np.argwhere(np.diff(d<0,axis=axis)); q=np.stack([ids[tuple((cross-e).T)] for e in [u+w,w,np.zeros(3,int),u]],1)
 flip=d[tuple(cross.T)]>=0;q[flip]=q[flip,::-1]; faces.extend(q.tolist())
np.savez_compressed(R/'ArtSource/Blender/sculpt.npz',vertices=v.astype('f4'),faces=np.array(faces,dtype='i4'))
# Planar UV maps share anatomical coordinates with the carved geometry.
N=1024
x,z=np.meshgrid(np.linspace(-.42,.42,N),np.linspace(1.02,-.14,N))
def noise(s):
 t=gaussian_filter(rng.standard_normal((N,N)),s);return (t-t.mean())/max(t.std(),1e-5)
coarse=noise(31); mid=noise(7);grain=noise(.75)
def patch(cx,cz,rx,rz):
 return np.exp(-((x-cx)/rx)**2-((z-cz)/rz)**2)
bruise=np.clip(.85*(patch(-.12,.58,.13,.10)+patch(.12,.58,.11,.09))+.34*patch(.12,.31,.13,.13)+.13*coarse,0,1)
wound=np.clip((patch(.207,.37,.078,.094)+.19*mid+.09*coarse-.49)*1.7,0,1)
scalp=np.clip((z-.70)*4,0,.55)
skin=np.array([159,145,110])+np.array([12,11,9])*coarse[:,:,None]+6*mid[:,:,None]+4*grain[:,:,None]
for mask,col in [(bruise,[67,50,58]),(wound,[111,43,38]),(scalp,[104,105,90])]:
 skin=skin*(1-mask[:,:,None])+np.array(col)*mask[:,:,None]
# Broken capillaries and fine transverse wrinkles.
vein=np.exp(-((np.sin(25*x+6*z+1.9*np.sin(13*z)+.5*mid))/.055)**2)*np.clip(.3+.2*coarse,0,.7)
skin-=vein[:,:,None]*np.array([12,31,22])
fold=np.exp(-(np.sin(76*z+1.1*np.cos(12*x)+.12*mid)/.22)**2)*patch(0,.735,.23,.07)
skin-=fold[:,:,None]*18
height=.40*grain+.6*mid+1.3*fold
dy,dx=np.gradient(height); n=np.stack([-dx*.20,dy*.20,np.ones_like(dx)],2);n/=np.linalg.norm(n,axis=2)[:,:,None]
for name,pix in [('Dermis',skin),('DermisNormal',(n*.5+.5)*255)]:
 Image.fromarray(np.clip(pix,0,255).astype('uint8')).save(out/(name+'.png'))
print('Sculpt:',len(v),'vertices;',len(faces)*2,'triangles before retopology')
