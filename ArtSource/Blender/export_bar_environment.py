import bpy,json,pathlib,hashlib,sys
from mathutils import Vector,Matrix
ROOT=pathlib.Path('D:/unityprojects/Zombarvr')
SOURCE=ROOT/'ArtSource/EnvironmentReview'
OUT=ROOT/'ArtSource/EnvironmentPackage'
BLENDS=ROOT/'ArtSource/Blender/ExternalEnvironment';BLENDS.mkdir(exist_ok=True)
catalog=json.loads((OUT/'material-map.json').read_text())
only=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
report=json.loads((OUT/'validation.json').read_text()) if only else []
for model in catalog['models']:
    if only and model['name'] not in only:continue
    report=[r for r in report if r['name']!=model['name']]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE/model['source']))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and (not model['select'] or model['select'] in o.name)]
    for o in objects:
        world=o.matrix_world.copy(); o.parent=None; o.matrix_world=world
    bpy.context.view_layer.update()
    for o in list(bpy.context.scene.objects):
        if o not in objects:bpy.data.objects.remove(o,do_unlink=True)
    points=[o.matrix_world@Vector(v) for o in objects for v in o.bound_box]
    lo=Vector([min(p[i] for p in points) for i in range(3)])
    hi=Vector([max(p[i] for p in points) for i in range(3)])
    center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    size=model['size']; uniform=size[2]/(hi.z-lo.z)
    scale=Vector([size[i]/(hi[i]-lo[i]) if size[i] else uniform for i in range(3)])
    total=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
    for o in objects:
        matrix=o.matrix_world.copy();o.parent=None
        o.data.transform(matrix);o.matrix_world=Matrix.Identity(4)
        for v in o.data.vertices:
            v.co=Vector([(v.co[i]-center[i])*scale[i] for i in range(3)])
        if total>model['budget']:
            bpy.context.view_layer.objects.active=o
            mod=o.modifiers.new('Quest triangle budget','DECIMATE');mod.ratio=model['budget']/total
            bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(True)
    for material in bpy.data.materials:
        match=next((m for m in model['materials'] if m['original']==material.name),None)
        if match: material.name=match['name']
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(model['name']+'.fbx')),use_selection=True,
        object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDS/(model['name']+'.blend')),compress=True)
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
    report.append(dict(name=model['name'],triangles=tris,source=model['source'],size_xyz_blender=list(scale[i]*(hi[i]-lo[i]) for i in range(3))))
    assert tris <= model['budget']+50,(model['name'],tris)
    if any(m['albedo'] for m in model['materials']):
        assert all(len(o.data.uv_layers)>0 for o in objects),'Missing UVs for textured mesh'
    print('EXPORTED',model['name'],tris,flush=True)
(OUT/'validation.json').write_text(json.dumps(report,indent=2))
roster={str(p.relative_to(OUT)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
        for p in OUT.rglob('*') if p.is_file() and p.name!='sha256.json'}
(OUT/'sha256.json').write_text(json.dumps(roster,indent=2))
print('PACKAGE COMPLETE')
