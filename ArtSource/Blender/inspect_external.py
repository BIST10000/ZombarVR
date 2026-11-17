import bpy,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];report=[]
for p in (R/'ArtSource/ExternalReview/FullResolution').glob('*.fbx'):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p))
 mats=[]
 for m in bpy.data.materials:
  mats.append({'name':m.name,'textures':[{'node':n.name,'image':n.image.name,'size':list(n.image.size),'path':n.image.filepath,'packed':bool(n.image.packed_file)} for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if m.use_nodes else []})
 report.append({'file':p.name,'materials':mats,'meshes':[{'name':o.name,'triangles':sum(len(f.vertices)-2 for f in o.data.polygons)} for o in bpy.data.objects if o.type=='MESH'],'bones':[b.name for o in bpy.data.objects if o.type=='ARMATURE' for b in o.data.bones][:12]})
(R/'ArtSource/ExternalReview/full-inspection.json').write_text(json.dumps(report,indent=2))
