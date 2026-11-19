using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Zombar;

namespace ZombarEditor
{
    internal static class TaproomArt
    {
        const string Folder = "Assets/Zombar/Art/Taproom/";
        static Texture2D Texture(string file, bool normal = false)
        {
            var path = Folder + file;
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            if (normal && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Surface(string name, string color, string normal, float smooth)
        {
            var path = Folder + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat,path);
            }
            mat.SetTexture("_BaseMap",Texture(color));
            var bumps = string.IsNullOrEmpty(normal) ? null : Texture(normal,true);
            if (bumps) { mat.SetTexture("_BumpMap",bumps); mat.EnableKeyword("_NORMALMAP"); }
            mat.SetFloat("_Smoothness",smooth);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Dress(BarProp prop, string name, int count)
        {
            if (prop.transform.Find("Workshop " + name) != null) return;
            for (int i=prop.transform.childCount-1;i>=0;i--)
                Object.DestroyImmediate(prop.transform.GetChild(i).gameObject);
            var root = new GameObject("Workshop " + name).transform;
            root.SetParent(prop.transform,false);
            for (int i=0;i<count;i++)
            {
                string stem = name + i;
                var part = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+stem+".obj"),root);
                var mat = Surface(stem,stem+"Color.png",stem+"Normal.png",name=="Bell"?.72f:.24f);
                if (name=="Bell") mat.SetFloat("_Metallic",.56f);
                else mat.SetFloat("_Cull",0);
                foreach (var renderer in part.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial=mat;
            }
            var meshes = root.GetComponentsInChildren<MeshFilter>();
            var bounds = new Bounds(Vector3.zero,Vector3.zero);
            foreach(var mesh in meshes)
            {
                Bounds local = mesh.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var point=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    bounds.Encapsulate(prop.transform.InverseTransformPoint(mesh.transform.TransformPoint(point)));
                }
            }
            var box=prop.GetComponent<BoxCollider>();
            box.center=bounds.center; box.size=Vector3.Max(bounds.size,Vector3.one*.014f);
            prop.gripOffset=Vector3.down*bounds.size.y*.45f;
        }

        static void DressBackWall()
        {
            var brick=Surface("Cellar masonry","Brick_color.jpg","Brick_normal.jpg",.22f);
            brick.SetTextureScale("_BaseMap",new Vector2(4.5f,1.6f));
            brick.SetColor("_BaseColor",new Color(.83f,.79f,.70f));
            foreach (var name in new[] {"Стена за гостями","Задняя стена"})
                GameObject.Find(name).GetComponent<Renderer>().sharedMaterial=brick;
            if(GameObject.Find("Обшивка за гостями")) return;
            var root=new GameObject("Обшивка за гостями").transform;
            var oak=AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Art/Materials/WeatheredWood.mat");
            void Timber(string name,Vector3 position,Vector3 size)
            {
                var piece=GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name=name;
                piece.transform.SetParent(root,false);
                piece.transform.position=position;
                piece.transform.localScale=size;
                piece.GetComponent<Renderer>().sharedMaterial=oak;
            }
            Timber("Дубовые панели",new Vector3(0,.55f,6.05f),new Vector3(12.7f,1.1f,.11f));
            Timber("Верхняя планка",new Vector3(0,1.12f,5.98f),new Vector3(12.7f,.09f,.19f));
            for(int i=-7;i<=7;i++)
                Timber("Филёнка",new Vector3(i*.84f,.54f,5.975f),new Vector3(.055f,1.05f,.045f));
            foreach(var x in new[]{-4.25f,4.25f})
            {
                Timber("Подоконник",new Vector3(x,2.13f,5.98f),new Vector3(1.98f,.11f,.31f));
                Timber("Карниз окна",new Vector3(x,4.13f,5.98f),new Vector3(2.05f,.13f,.26f));
            }
            var center=new Vector3(0,3.20f,5.99f);
            Timber("Рама вывески",center,new Vector3(3.14f,1.02f,.14f));
            var sign=GameObject.CreatePrimitive(PrimitiveType.Quad);
            sign.name="Вывеска дома";sign.transform.SetParent(root,false);
            sign.transform.position=center+Vector3.back*.075f;
            sign.transform.localScale=new Vector3(3f,.94f,1);
            Object.DestroyImmediate(sign.GetComponent<Collider>());
            var enamel=Surface("House enamel","HouseSign.png",null,.31f);
            enamel.EnableKeyword("_EMISSION");
            enamel.SetTexture("_EmissionMap",Texture("HouseSign.png"));
            enamel.SetColor("_EmissionColor",new Color(.23f,.21f,.18f));
            sign.GetComponent<Renderer>().sharedMaterial=enamel;
            foreach(float x in new[]{-2.12f,2.12f})
            {
                var lantern=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Zombar/Resources/PhysicalBar/Environment/Lantern.prefab"),root);
                lantern.transform.position=new Vector3(x,2.6f,5.73f);
                Timber("Кронштейн фонаря",new Vector3(x,2.56f,5.83f),new Vector3(.31f,.07f,.53f));
                var glow=new GameObject("Свет на кладке").AddComponent<Light>();
                glow.transform.SetParent(root,false);glow.transform.position=lantern.transform.position+new Vector3(0,.24f,-.22f);
                glow.type=LightType.Point;glow.range=3.6f;glow.intensity=4.2f;glow.color=new Color(1,.65f,.36f);
            }
        }

        public static void Apply()
        {
            DressBackWall();
            var floor=GameObject.Find("Пол · потёртая плитка");
            var oldMat=GameObject.Find("Коврик за стойкой");
            if(oldMat) Object.DestroyImmediate(oldMat);
            var board=GameObject.Find("Меню на доске");
            if(board)
            {
                board.transform.position=new Vector3(-2.95f,2.12f,6.005f);
                board.transform.localScale=new Vector3(1.75f,1.04f,.035f);
            }
            var wood=Surface("Cellar boards","Floor_diff.jpg","Floor_nor_gl.jpg",.65f);
            wood.SetTexture("_MetallicGlossMap",Texture("FloorMask.png"));
            wood.EnableKeyword("_METALLICSPECGLOSSMAP");
            wood.SetColor("_BaseColor",new Color(.78f,.67f,.57f));
            wood.SetTextureScale("_BaseMap",new Vector2(5,4));
            floor.GetComponent<Renderer>().sharedMaterial=wood;
            foreach(var item in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Шов плитки"))
                Object.DestroyImmediate(item.gameObject);
            foreach(var prop in Object.FindObjectsByType<BarProp>())
            {
                if(prop.kind==PropKind.Bell) Dress(prop,"Bell",2);
                if(prop.kind==PropKind.Rag) Dress(prop,"Rag",1);
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.28f,.32f,.38f);
            RenderSettings.ambientEquatorColor=new Color(.20f,.17f,.15f);
            RenderSettings.ambientGroundColor=new Color(.10f,.069f,.052f);
            RenderSettings.fogColor=new Color(.067f,.084f,.094f);
            RenderSettings.fogDensity=.055f;
            foreach(var light in Object.FindObjectsByType<Light>())
            {
                if(light.type==LightType.Directional)
                {
                    light.color=new Color(.72f,.82f,1f);light.intensity=.52f;
                    light.transform.rotation=Quaternion.Euler(33,-42,0);
                    light.shadowStrength=.82f;
                }
                if(light.name=="Свет латунного фонаря") { light.color=new Color(1,.75f,.48f);light.intensity=10f; }
                if(light.name=="Отблеск из погреба") { light.color=new Color(.31f,.63f,.75f);light.intensity=9f; }
                if(light.name=="Кровавое стекло") light.intensity=3f;
            }
            if(GameObject.Find("Лунный свет на досках")==null)
            {
                var beam=new GameObject("Лунный свет на досках").AddComponent<Light>();
                beam.transform.position=new Vector3(-4.2f,2.55f,5.85f);
                beam.transform.LookAt(new Vector3(-1.4f,0,2.8f));
                beam.type=LightType.Spot;beam.range=8;beam.spotAngle=62;beam.innerSpotAngle=24;
                beam.color=new Color(.34f,.62f,.85f);beam.intensity=14;
            }
            if(GameObject.Find("Свет на полу за стойкой")==null)
            {
                var lamp=new GameObject("Свет на полу за стойкой").AddComponent<Light>();
                lamp.transform.position=new Vector3(-1.7f,2.7f,-3.6f);
                lamp.transform.LookAt(new Vector3(-.4f,0,-1.9f));
                lamp.type=LightType.Spot;lamp.range=6;lamp.spotAngle=92;lamp.innerSpotAngle=48;
                lamp.color=new Color(1,.72f,.47f);lamp.intensity=6;
            }
        }
    }
}
