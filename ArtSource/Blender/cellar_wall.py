"""Hand-laid cellar wall, tide marks, cracked glaze and a worn painted trade sign."""
from pathlib import Path
import numpy as np
from scipy.ndimage import gaussian_filter
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2];path=root/'Assets/Zombar/Art/Materials'
rng=np.random.default_rng(402);h,w=1024,2048;y,x=np.mgrid[:h,:w]
def noise(s):
 a=gaussian_filter(rng.normal(size=(h,w)),s);return a/a.std()
rough=noise(1.1);mottle=noise(26);damage=noise(7);fine=rng.normal(size=(h,w))
base=np.array([118,123,106])+18*mottle[:,:,None]+5*rough[:,:,None]+3*fine[:,:,None]
tide=540+9*np.sin(x/61)+3*damage
tiles=y>tide
colour=np.array([83,101,91])+6*rough[:,:,None]+13*mottle[:,:,None]
joint=((x//2%59<2)|(y%71<3))
colour[joint]*=.57;base[tiles]=colour[tiles]
flaking=np.clip((damage*.7+mottle*.8-1.3)*1.2,0,1)
base=base*(1-flaking[:,:,None])+np.array([71,62,51])*flaking[:,:,None]
drips=gaussian_filter(rng.normal(size=(h,w)),(59,1.7));drips/=drips.std()
base-=np.maximum(drips-1.4,0)[:,:,None]*np.array([7,13,12])*np.clip((y-190)/500,0,1)[:,:,None]
sign=Image.new('L',(w,h));g=ImageDraw.Draw(sign)
try:font=ImageFont.truetype('C:/Windows/Fonts/georgiab.ttf',97)
except OSError:font=ImageFont.load_default()
g.text((w*.5,h*.29),'МОРГ  IV',font=font,anchor='mm',fill=255,stroke_width=1)
g.line((835,346,1213,346),fill=180,width=3)
mask=np.asarray(sign)/255*np.clip(.90-.13*rough-.22*damage-.35*flaking,0,1)
base=base*(1-mask[:,:,None])+np.array([47,57,51])*mask[:,:,None]
Image.fromarray(np.uint8(np.clip(base,0,255))).transpose(Image.Transpose.FLIP_LEFT_RIGHT).save(path/'MortuaryWall.jpg',quality=93)

