import bpy, json, shutil, hashlib
from pathlib import Path
R=Path(__file__).resolve().parents[2];review=R/'ArtSource/ExternalReview';dest=R/'Assets/Zombar/Art/External/Mixamo'
dest.mkdir(parents=True,exist_ok=True);(dest/'Animations').mkdir(exist_ok=True)
blenddir=R/'ArtSource/Blender/ExternalCharacters';blenddir.mkdir(exist_ok=True)
rows=[]
for p in (review/'FullResolution').glob('*.fbx'):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p))
 shutil.copy2(p,dest/p.name)
 model={'file':p.name,'materials':[]}
 for m in bpy.data.materials:
  entry={'name':m.name,'albedo':'','normal':''}
  if not m.use_nodes:continue
  shader=m.node_tree.nodes.get('Principled BSDF')
  if shader:
   shader.inputs['Metallic'].default_value=0
   shader.inputs['Roughness'].default_value=.67
   for l in list(shader.inputs['Alpha'].links):m.node_tree.links.remove(l)
   shader.inputs['Alpha'].default_value=1
  for node in m.node_tree.nodes:
   if node.type!='TEX_IMAGE' or not node.image:continue
   im=node.image;original=im.filepath.lower()
   role='normal' if 'normal' in original else 'albedo' if 'diffuse' in original or 'albedo' in original or 'basecolor' in original else None
   if shader and any(link.from_node==node for link in shader.inputs['Base Color'].links):role='albedo'
   if role is None:continue
   if not all(im.size):raise RuntimeError('Missing texture '+im.name)
   file=p.stem+'_'+m.name+'_'+role+'.png'
   im.filepath_raw=str(dest/file);im.file_format='PNG';im.save();entry[role]=file
  if not entry['albedo']:raise RuntimeError('Missing albedo: '+m.name)
  model['materials'].append(entry)
 rows.append(model)
 bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(blenddir/(p.stem+'.blend')),compress=True)
for p in (review/'Animations').glob('*.fbx'):shutil.copy2(p,dest/'Animations'/p.name)
(dest/'material-map.json').write_text(json.dumps({'models':rows},indent=2))
manifest={str(p.relative_to(dest)):hashlib.sha256(p.read_bytes()).hexdigest() for p in dest.rglob('*') if p.is_file() and p.suffix!='.meta'}
(review/'admitted-sha256.json').write_text(json.dumps(manifest,indent=2))
print('PREPARED',len(rows),'characters,',len(manifest),'files')

