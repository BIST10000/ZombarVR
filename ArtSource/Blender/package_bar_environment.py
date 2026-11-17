"""Build a portable, hash-rostered CC0 package; originals are never modified."""
import hashlib, io, json, pathlib, struct, sys
from PIL import Image, ImageOps

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtSource/EnvironmentReview'
OUTPUT = ROOT / 'ArtSource/EnvironmentPackage'
OUTPUT.mkdir(exist_ok=True)
SPECS = [
    ('Counter', 'tavern_counter.glb', '', [5.4,1.25,1.21], 14000),
    ('Backbar', 'backbar.glb', '', [3.1,.62,1.7], 19000),
    ('Window', 'casement_window.glb', '', [1.75,.29,1.95], 12000),
    ('Bottle', 'wine_bottles_01/wine_bottles_01.gltf', 'bordeaux', [0,0,.28], 6500),
    ('BottleAlt', 'wine_bottles_01/wine_bottles_01.gltf', 'champagne', [0,0,.31], 6500),
    ('Glass', 'brass_goblets/brass_goblets.gltf', 'brass_goblet_01', [0,0,.18], 3500),
    ('Pendant', 'hanging_industrial_lamp/hanging_industrial_lamp.gltf', '', [0,0,1.35], 10000),
    ('Lantern', 'Lantern_01/Lantern_01.gltf', '', [0,0,.36], 6500),
    ('Stool', 'bar_chair_round_01/bar_chair_round_01.gltf', '', [0,0,.78], 6500),
    ('Barrel', 'wine_barrel_01/wine_barrel_01.gltf', '', [0,0,.95], 6500),
    ('Cabinet', 'GothicCabinet_01/GothicCabinet_01.gltf', '', [0,0,1.9], 13000),
    ('Tray', 'tray.glb', '', [.52,.40,.045], 3000),
    ('Hatchet', 'hatchet/hatchet.gltf', '', [0,0,.32], 9000),
    ('BloodFlask', 'leather.glb', '', [0,0,.28], 12000),
    ('CocktailShaker', '379336c0082e4c42ab2ae009ed92f563.glb', '', [0,0,.18], 9000),
]

def document(path):
    data=path.read_bytes()
    if path.suffix == '.gltf': return json.loads(data), None
    assert data[:4] == b'glTF', path
    length,kind=struct.unpack_from('<II',data,12)
    doc=json.loads(data[20:20+length])
    offset=20+length
    binary=data[offset+8:] if offset<len(data) else None
    return doc,binary

only=sys.argv[1:]
models=json.loads((OUTPUT/'material-map.json').read_text())['models'] if only else []
for name,relative,select,size,budget in SPECS:
    if only and name not in only: continue
    models=[m for m in models if m['name']!=name]
    path=SOURCE/relative; doc,binary=document(path)
    folder=OUTPUT/name; folder.mkdir(exist_ok=True)
    def texture(info, role):
        if not info:return ''
        tex=doc['textures'][info['index']]
        index=tex.get('source', next((e['source'] for e in tex.get('extensions',{}).values() if 'source' in e),None))
        item=doc['images'][index]
        if 'uri' in item:
            im=Image.open(path.parent/item['uri'])
        else:
            view=doc['bufferViews'][item['bufferView']]; start=view.get('byteOffset',0)
            im=Image.open(io.BytesIO(binary[start:start+view['byteLength']]))
        im=im.convert('RGBA'); im.thumbnail((2048,2048))
        if name=='BloodFlask' and role=='albedo':
            # The source uses one atlas for straps, leather, stopper and tinted glass.
            # Composite the translucent texels over dark blood for a single opaque Quest material.
            im=Image.alpha_composite(Image.new('RGBA',im.size,(94,12,19,255)),im)
        if role=='metal':
            r,g,b,a=im.split()
            if name == 'CocktailShaker':
                # Leave a diffuse contribution so the brushed finish reads under the dim bar lights.
                b = b.point(lambda value: round(value * .70))
            im=Image.merge('RGBA',(b,b,b,ImageOps.invert(g)))
        if role=='ao':
            r,g,b,a=im.split(); im=Image.merge('RGB',(r,r,r))
        dest=folder/(str(index)+'_'+role+'.png'); im.save(dest)
        return name+'/'+dest.name
    materials=[]
    for material_index,m in enumerate(doc.get('materials',[])):
        p=m.get('pbrMetallicRoughness',{})
        materials.append(dict(name=name+'__'+m.get('name','Material_'+str(material_index)),original=m.get('name','Material_'+str(material_index)),
            albedo=texture(p.get('baseColorTexture'),'albedo'), normal=texture(m.get('normalTexture'),'normal'),
            metal=texture(p.get('metallicRoughnessTexture'),'metal'), ao=texture(m.get('occlusionTexture'),'ao'),
            emission=texture(m.get('emissiveTexture'),'emission'), color=p.get('baseColorFactor',[1,1,1,1]),
            metallic=p.get('metallicFactor',1), smoothness=1-p.get('roughnessFactor',1),
            cutout=m.get('alphaMode')=='MASK', transparent=m.get('alphaMode')=='BLEND',
            transmission='KHR_materials_transmission' in m.get('extensions',{})))
    models.append(dict(name=name,source=relative,select=select,size=size,budget=budget,materials=materials))
(OUTPUT/'material-map.json').write_text(json.dumps({'models':models},indent=2))
print('Texture package prepared:', len(models),'models')
