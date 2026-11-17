using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Zombar;

namespace ZombarEditor
{
    public static class ExternalEnvironmentBuilder
    {
        const string Source = "Assets/Zombar/Art/External/Environment/";
        const string Prefabs = "Assets/Zombar/Resources/PhysicalBar/Environment/";
        const string Props = "Assets/Zombar/Resources/PhysicalBar/Props/";
        [Serializable] class Catalog { public Model[] models; }
        [Serializable] class Model { public string name; public Surface[] materials; }
        [Serializable] class Surface
        {
            public string name, original, albedo, normal, metal, ao, emission;
            public float[] color;
            public float metallic, smoothness;
            public bool cutout, transparent, transmission;
        }

        [MenuItem("НЕКРО-ПАБ/Обновить модели окружения CC0")]
        public static void Build()
        {
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Props);
            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Source + "material-map.json"));
            foreach (var model in catalog.models) Import(model);
            MakeHandPropPrefabs();
            MakePropPrefab("Glass", "Glass");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZombieBar.unity");
            var oldRoot = GameObject.Find("CC0 Environment");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
            string[] prefixes = { "Стойка ·", "Медная окантовка", "Передняя панель", "Филёнка стойки",
                "Латунная рамка панели", "Заклёпка стойки", "Знак · подложка", "Потёк на стойке", "Витраж ·",
                "Барный табурет ·", "Подножка табурета", "Поднос ·", "Неон ·", "CellarLamp" };
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
                if (t != null && prefixes.Any(p => t.name.StartsWith(p, StringComparison.Ordinal)))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var t in UnityEngine.Object.FindObjectsByType<TextMesh>())
                if (t.text.Contains("МЕРТВЕЦКИ ВКУСНО")) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (string n in new[] { "Рабочая поверхность", "Поднос ингредиентов", "Полка улучшений" })
            {
                var obj = GameObject.Find(n);
                if (obj != null && obj.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
            }
            var root = new GameObject("CC0 Environment").transform;
            var counter = Place(root,"Counter",new Vector3(0,0,-.22f),180);
            var collision = counter.AddComponent<BoxCollider>();
            collision.center = new Vector3(0,.60f,0); collision.size = new Vector3(5.3f,1.20f,1.20f);
            Place(root,"Tray",new Vector3(-.6f,1.212f,-.55f));
            Place(root,"Cabinet",new Vector3(1.65f,0,-1.1f),180,new Vector3(.47f,.47f,.90f));
            Place(root,"Backbar",new Vector3(0,.55f,5.95f),180);
            foreach (float x in new[] {-4.25f,4.25f})
                Place(root,"Window",new Vector3(x,2.15f,6.02f),0);
            foreach (float x in new[] {-3.8f,3.8f})
            {
                Place(root,"Barrel",new Vector3(x,0,4.65f),180);
                Place(root,"Lantern",new Vector3(x,.96f,4.65f));
                AddLight(root,"Barrel lantern",new Vector3(x,1.12f,4.4f),new Color(1,.54f,.22f),2.2f,3);
            }
            foreach (float x in new[] {-2.05f,2.15f})
            {
                Place(root,"Pendant",new Vector3(x,2.9f,1.6f));
                AddLight(root,"Pendant pool",new Vector3(x,2.88f,1.6f),new Color(1,.67f,.37f),3.8f,4.8f);
            }
            foreach (float x in new[] {-2.15f,0f,2.15f}) Place(root,"Stool",new Vector3(x,0,1.75f),180);
            Place(root,"Cabinet",new Vector3(-5.4f,0,5.8f),180);
            for (int i=0;i<5;i++)
                Place(root,i%2==0?"Bottle":"BottleAlt",new Vector3(-2.35f+i*.16f,1.22f,-.1f),i*38);
            Place(root,"Lantern",new Vector3(2.35f,1.22f,-.18f));
            AddLight(root,"Work lantern",new Vector3(2.35f,1.43f,-.35f),new Color(1,.57f,.25f),1.4f,2.5f);
            ReplaceInteractiveVisuals();
            var bar = UnityEngine.Object.FindAnyObjectByType<PhysicalBar>();
            // The wider real counter needs the donor beside its end, outside its solid collision.
            if (bar.helper != null)
            {
                Vector3 offset = new Vector3(-3.1f,0,-.45f) - bar.helper.position;
                bar.helper.position += offset;
                foreach (var prop in UnityEngine.Object.FindObjectsByType<BarProp>())
                    if (prop.kind == PropKind.HelperArm || prop.kind == PropKind.HelperEye)
                    { prop.transform.position += offset; prop.home += offset; }
                foreach (var label in bar.GetComponentsInChildren<TextMesh>())
                    if (label.text.StartsWith("ДОНОР")) label.transform.position = bar.helper.position+Vector3.up*2;
            }
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
                if(light.type==LightType.Directional)
                { light.intensity=.8f; light.shadows=LightShadows.Soft; light.shadowStrength=.75f; }
            var probeObject=new GameObject("Baked bar reflections");probeObject.transform.SetParent(root,false);
            probeObject.transform.position=new Vector3(0,1.75f,-.5f);
            var probe=probeObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Baked;
            probe.size=new Vector3(13,6,12);probe.resolution=128;probe.boxProjection=true;
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.09f,.08f,.07f);
            probe.renderDynamicObjects=true;probe.intensity=.8f;
            const string reflection="Assets/Zombar/Art/Materials/BarReflection.exr";
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)|StaticEditorFlags.ReflectionProbeStatic);
            if(!Lightmapping.BakeReflectionProbe(probe,reflection)) throw new InvalidOperationException("Reflection bake failed");
            AssetDatabase.ImportAsset(reflection);
            probe.bakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(reflection);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CC0 environment imported: 12 models, textured props and reachable work surface.");
        }

        static void Import(Model model)
        {
            string path=Source+model.name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false; importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals=ModelImporterNormals.Import; importer.importTangents=ModelImporterTangents.CalculateMikk;
            foreach(var surface in model.materials)
            {
                var mat=Material(surface,model.name);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),surface.name),mat);
            }
            importer.SaveAndReimport();
            var root=new GameObject(model.name);
            var mesh=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
            mesh.transform.localPosition=Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root,Prefabs+model.name+".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        static Texture2D Texture(string relative,bool normal=false,bool linear=false)
        {
            if(string.IsNullOrEmpty(relative)) return null;
            string path=Source+relative;
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            var android=imp.GetPlatformTextureSettings("Android");
            if(imp.textureType==(normal?TextureImporterType.NormalMap:TextureImporterType.Default)&&
                imp.sRGBTexture==(!normal&&!linear)&&imp.mipmapEnabled&&imp.maxTextureSize==2048&&
                android.overridden&&android.format==TextureImporterFormat.ASTC_6x6&&android.maxTextureSize==2048)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            imp.sRGBTexture=!normal&&!linear; imp.mipmapEnabled=true; imp.maxTextureSize=2048;
            imp.alphaSource=TextureImporterAlphaSource.FromInput;
            imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android", overridden=true, maxTextureSize=2048, format=TextureImporterFormat.ASTC_6x6 });
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Material(Surface s,string model)
        {
            string path=Source+s.name.Replace('/','_')+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path); }
            Color color=s.color.Length>=4?new Color(s.color[0],s.color[1],s.color[2],s.color[3]):Color.white;
            if(model=="Window" && !s.name.Contains("glass")) color*=new Color(.26f,.21f,.16f,1);
            // Opaque coloured glass avoids expensive transmission and sorting in mobile VR.
            if(s.transmission && model.StartsWith("Bottle")) color=new Color(.30f,.13f,.065f,1);
            if(model=="Window" && s.name.Contains("glass")) color=new Color(.035f,.13f,.15f,1);
            color.a=1; mat.SetColor("_BaseColor",color);
            mat.SetTexture("_BaseMap",Texture(s.albedo));
            mat.SetTexture("_BumpMap",Texture(s.normal,true)); if(!string.IsNullOrEmpty(s.normal))mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",Texture(s.metal,false,true));
            mat.SetFloat("_Metallic",string.IsNullOrEmpty(s.metal)?s.metallic:1);
            mat.SetFloat("_Smoothness",string.IsNullOrEmpty(s.metal)?s.smoothness:1);
            if(!string.IsNullOrEmpty(s.metal))mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_OcclusionMap",Texture(s.ao,false,true));
            if(!string.IsNullOrEmpty(s.ao))mat.EnableKeyword("_OCCLUSIONMAP");
            mat.SetFloat("_Cull",s.cutout?0:2); mat.SetFloat("_AlphaClip",s.cutout?1:0); mat.SetFloat("_Cutoff",.45f);
            if(s.cutout) {mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;}
            else {mat.DisableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","Opaque");mat.renderQueue=-1;}
            if(!string.IsNullOrEmpty(s.emission))
            {
                mat.EnableKeyword("_EMISSION");mat.SetTexture("_EmissionMap",Texture(s.emission));
                mat.SetColor("_EmissionColor",new Color(1,.63f,.28f)*2);
            }
            if(s.name.ToLowerInvariant().Contains("glass") && (model=="Window"||model=="Lantern"||model=="Pendant"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor",model=="Window"?new Color(.055f,.18f,.22f):new Color(1,.42f,.12f)*1.5f);
            }
            EditorUtility.SetDirty(mat); return mat;
        }

        static void MakePropPrefab(string visual,string name)
        {
            var root=new GameObject(name);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+visual+".prefab"),root.transform);
            if(name=="Glass")
            {
                var liquid=GameObject.CreatePrimitive(PrimitiveType.Cylinder); liquid.name="Liquid";
                UnityEngine.Object.DestroyImmediate(liquid.GetComponent<Collider>());
                liquid.transform.SetParent(root.transform,false); liquid.transform.localPosition=new Vector3(0,.154f,0);
                liquid.transform.localScale=new Vector3(.067f,.0015f,.067f);
                string path=Source+"CocktailLiquid.mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.25f,.015f,.025f);AssetDatabase.CreateAsset(mat,path);}
                liquid.GetComponent<Renderer>().sharedMaterial=mat; liquid.GetComponent<Renderer>().enabled=false;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Props+name+".prefab"); UnityEngine.Object.DestroyImmediate(root);
        }

        static void MakeHandPropPrefabs()
        {
            MakePropPrefab("BloodFlask", "BloodBottle");
            MakePropPrefab("Hatchet", "Cleaver");
            MakePropPrefab("CocktailShaker", "Shaker");
        }

        [MenuItem("НЕКРО-ПАБ/Обновить предметы бармена")]
        public static void UpdateHandProps()
        {
            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Source + "material-map.json"));
            foreach (var model in catalog.models.Where(m => m.name == "BloodFlask" || m.name == "Hatchet" || m.name == "CocktailShaker"))
                Import(model);
            MakeHandPropPrefabs();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZombieBar.unity");
            ReplaceInteractiveVisuals();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static void ReplaceInteractiveVisuals()
        {
            foreach(var prop in UnityEngine.Object.FindObjectsByType<BarProp>())
            {
                if (prop.kind != PropKind.BloodBottle && prop.kind != PropKind.Shaker &&
                    prop.kind != PropKind.Cleaver && prop.kind != PropKind.Glass) continue;
                string name=prop.kind.ToString();
                foreach(Transform child in prop.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                prop.transform.rotation=Quaternion.identity;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Props+name+".prefab"),prop.transform);
                var bounds=new Bounds(prop.transform.position,Vector3.zero);
                foreach(var r in visual.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                var box=prop.GetComponent<BoxCollider>();box.center=prop.transform.InverseTransformPoint(bounds.center);box.size=bounds.size;
                prop.gripOffset=Vector3.down*(prop.kind==PropKind.BloodBottle?.115f:prop.kind==PropKind.Shaker?.075f:prop.kind==PropKind.Cleaver?.09f:.055f);
                if (prop.kind==PropKind.Cleaver)
                {
                    // Lay the broad face on the counter. The model stands upright in the hand.
                    prop.transform.rotation=Quaternion.Euler(0,18,90);
                    prop.transform.position=new Vector3(.79f,1.212f+box.size.x*.5f,-.54f);
                    prop.label="ТОПОРИК · разделать поверженного зомби";
                }
                prop.home=prop.transform.position;
                prop.homeRotation=prop.transform.rotation;
            }
        }

        static GameObject Place(Transform parent,string name,Vector3 pos,float yaw=0,Vector3 scale=default)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab"),parent);
            obj.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
            if(scale!=default)obj.transform.localScale=scale;
            return obj;
        }
        static void AddLight(Transform parent,string name,Vector3 pos,Color color,float intensity,float range)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=pos;
            var light=obj.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;
        }
    }
}
