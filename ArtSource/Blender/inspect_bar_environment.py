import bpy, json, pathlib
from mathutils import Vector
ROOT=pathlib.Path('D:/unityprojects/Zombarvr')
files=list((ROOT/'ArtSource/EnvironmentReview').glob('*/*.gltf')) + list((ROOT/'ArtSource/EnvironmentReview').glob('*.glb'))
report=[]
for path in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objects=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH': continue
        pts=[obj.matrix_world@Vector(p) for p in obj.bound_box]
        objects.append(dict(name=obj.name, tris=sum(len(p.vertices)-2 for p in obj.data.polygons),
                            min=[min(v[i] for v in pts) for i in range(3)],
                            max=[max(v[i] for v in pts) for i in range(3)], materials=[m.name for m in obj.data.materials]))
    report.append(dict(file=str(path),objects=objects))
(ROOT/'ArtSource/EnvironmentReview/mesh-inspection.json').write_text(json.dumps(report,indent=2))
print('INSPECTION COMPLETE')
