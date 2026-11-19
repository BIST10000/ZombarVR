"""Convert the admitted Sketchfab GLBs to Unity OBJ meshes and PBR textures."""
from pathlib import Path
import io, json, struct
import numpy as np
from PIL import Image
from scipy.spatial.transform import Rotation

root = Path(__file__).resolve().parents[2]
out = root / "Assets/Zombar/Art/Ingredients"
models = {"Eye": "18915d2f17a2431fbefeb3eaf4411f16", "Flesh": "7239ceff0bbf4510afe08656553978e0"}
for name, uid in models.items():
    data = (root / "ArtSource/EnvironmentReview" / (uid+".glb")).read_bytes()
    end = 20 + struct.unpack_from("<I", data, 12)[0]
    doc, binary = json.loads(data[20:end]), data[end+8:]
    def accessor(index):
        a = doc["accessors"][index]
        view = doc["bufferViews"][a["bufferView"]]
        dtype = np.dtype({5123:"<u2",5125:"<u4",5126:"<f4"}[a["componentType"]])
        cols = {"SCALAR":1,"VEC3":3,"VEC2":2}[a["type"]]
        return np.ndarray((a["count"],cols),dtype,buffer=binary,offset=view.get("byteOffset",0)+a.get("byteOffset",0),strides=(view.get("byteStride",cols*dtype.itemsize),dtype.itemsize)).copy()
    material = doc["materials"][0]
    def texture(info):
        image = doc["images"][doc["textures"][info["index"]]["source"]]
        view = doc["bufferViews"][image["bufferView"]]
        start = view.get("byteOffset",0)
        im = Image.open(io.BytesIO(binary[start:start+view["byteLength"]])).convert("RGBA" if name == "Eye" and info == material["pbrMetallicRoughness"]["baseColorTexture"] else "RGB")
        im.thumbnail((1024,1024),Image.Resampling.LANCZOS)
        return im
    color = texture(material["pbrMetallicRoughness"]["baseColorTexture"])
    color.save(out/(name+".png"))
    texture(material["normalTexture"]).save(out/(name+"Normal.png"))
    primitive = doc["meshes"][0]["primitives"][0]
    attrs = primitive["attributes"]
    v = accessor(attrs["POSITION"]).astype(float)
    uv = accessor(attrs["TEXCOORD_0"])
    n = accessor(attrs["NORMAL"]).astype(float)
    faces = accessor(primitive["indices"]).reshape(-1,3)+1
    world = np.eye(4)
    for node in doc["nodes"]:
        if "matrix" in node: local = np.asarray(node["matrix"]).reshape(4,4).T
        else:
            local=np.eye(4)
            local[:3,:3]=Rotation.from_quat(node.get("rotation",[0,0,0,1])).as_matrix() @ np.diag(node.get("scale",[1,1,1]))
            local[:3,3]=node.get("translation",[0,0,0])
        world=world@local
    v=v@world[:3,:3].T
    n=n@np.linalg.inv(world[:3,:3])
    if np.linalg.det(world[:3,:3])<0: faces=faces[:,::-1]
    v-=(v.max(axis=0)+v.min(axis=0))/2
    if name=="Eye":
        rgb=np.asarray(color)[(uv[:,1]*color.height).astype(int)%color.height,(uv[:,0]*color.width).astype(int)%color.width].astype(float)
        iris=(rgb[:,2]>rgb[:,0]*1.18)&(rgb[:,0]<160)
        direction=v[iris].mean(axis=0)
        direction/=np.linalg.norm(direction)
        rot=Rotation.align_vectors([[0,.48,-.877268]], [direction])[0]
        v,n=rot.apply(v),rot.apply(n)
    else:
        _,_,basis=np.linalg.svd(v,full_matrices=False)
        rot=basis[[0,2,1]]
        if np.linalg.det(rot)<0: rot[2]*=-1
        v,n=v@rot.T,n@rot.T
    v*= (.030 if name=="Eye" else .065)/np.ptp(v[:,0])
    v[:,1]-=v[:,1].min()
    n/=np.maximum(1e-10,np.linalg.norm(n,axis=1,keepdims=True))
    with (out/(name+".obj")).open("w") as f:
        f.write("o "+name+"\n")
        np.savetxt(f,v,fmt="v %.8f %.8f %.8f")
        np.savetxt(f,np.column_stack([uv[:,0],1-uv[:,1]]),fmt="vt %.7f %.7f")
        np.savetxt(f,n,fmt="vn %.7f %.7f %.7f")
        for face in faces: f.write("f "+" ".join(f"{i}/{i}/{i}" for i in face)+"\n")
    print(name,len(faces),np.ptp(v,axis=0))

