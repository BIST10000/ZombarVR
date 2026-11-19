using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Zombar;

namespace ZombarEditor
{
    public static class PhysicalBarSceneBuilder
    {
        private const string Root = "Assets/Zombar/Resources/PhysicalBar";
        [MenuItem("НЕКРО-ПАБ/Построить физический бар")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Zombar/Art/Materials");
            AssetDatabase.Refresh();
            ConfigureModels();
            ExternalCharacterBuilder.BuildAll();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZombieBar.unity");
            // Remove the original oversized decorative drinks from the playable counter.
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
                if (t != null && (t.name.StartsWith("Бокал ·") || t.name.StartsWith("Бутылка ·") ||
                    t.name == "Пробка" || t.name == "Этикетка бутылки" || t.name == "Печать на этикетке"))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var target in UnityEngine.Object.FindObjectsByType<BarTarget>()) UnityEngine.Object.DestroyImmediate(target.gameObject);
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMesh>())
                if (text.text.Contains("СМЕШАТЬ") || text.text.Contains("МАГАЗИН") || text.text.Contains("ЖИВОЙ ПОМОЩНИК") || text.text.Contains("НЕ КОРМИТЬ ТИТАНА")) UnityEngine.Object.DestroyImmediate(text.gameObject);
            foreach (string old in new[] { "Помощник · регенерирует", "Physical Workbench" })
            { var obj = GameObject.Find(old); if (obj != null) UnityEngine.Object.DestroyImmediate(obj); }
            var rig = UnityEngine.Object.FindAnyObjectByType<BarPlayerInput>();
            if (rig != null)
            {
                rig.transform.position = new Vector3(0, 0, -0.95f);
                foreach (var behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour != null && (behaviour.GetType().Name.Contains("RayInteractor") || behaviour.GetType().Name.Contains("NearFarInteractor") || behaviour.GetType().Name.Contains("InteractorLineVisual"))) behaviour.enabled = false;
                foreach (var renderer in rig.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            }
            var workbench = new GameObject("Physical Workbench");
            var bar = workbench.AddComponent<PhysicalBar>();
            bar.workSurface = workbench.transform;
            AddProp(bar, PropKind.BloodBottle, new Vector3(-0.95f, 1.23f, -0.5f), "КРОВЬ · поднесите флягу к кольцу чаши");
            bar.shaker = AddProp(bar, PropKind.Shaker, new Vector3(-0.1f, 1.23f, -0.42f), "ШЕЙКЕР · ингредиенты + 5 встряхиваний");
            AddProp(bar, PropKind.Glass, new Vector3(0.35f, 1.23f, -0.45f), "СТАКАН · наполните и передайте клиенту");
            AddProp(bar, PropKind.Cleaver, new Vector3(0.7f, 1.24f, -0.5f), "ТЕСАК · разделать поверженного зомби");
            AddProp(bar, PropKind.Rag, new Vector3(1f, 1.23f, -0.5f), "ТРЯПКА · протрите стойку");
            AddProp(bar, PropKind.Syringe, new Vector3(-1.2f, 1.23f, -0.5f), "ШПРИЦ · приложите к груди · 2 крови");
            AddProp(bar, PropKind.Bell, new Vector3(0.05f, 1.23f, -0.7f), "КОЛОКОЛ · встряхнуть после поражения");
            AddProp(bar, PropKind.Pump, new Vector3(-1.5f, 1.05f, -0.6f), "НАСОС · качайте вверх-вниз", true);
            // A narrower worktop extension puts ingredients within arm's reach.
            Cube("Рабочая поверхность", workbench.transform, new Vector3(0, 1.16f, -0.45f), new Vector3(3.1f, 0.1f, 0.8f), Material("Worktop", new Color(.12f,.115f,.08f),.65f));
            Cube("Поднос ингредиентов", workbench.transform, new Vector3(-.6f,1.22f,-.55f), new Vector3(.52f,.015f,.48f),Material("Tray",new Color(.21f,.23f,.2f),.7f));
            Cube("Раковина · слить смесь", workbench.transform,new Vector3(1.35f,1.19f,-.45f),new Vector3(.4f,.04f,.4f),Material("Sink",new Color(.035f,.045f,.045f),.7f));
            CreateHelper(bar);
            CreateShop(bar);
            AddRecipeCard(workbench.transform);
            ApplyAtmosphere(workbench.transform);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
            AssetDatabase.SaveAssets();
            File.WriteAllText("Builds/Validation/physical-scene.txt", "Physical bar built. No BarTarget buttons remain.\n" +
                "Characters: Adobe Mixamo; environment: imported CC0 models.\nProps: " + UnityEngine.Object.FindObjectsByType<BarProp>().Length);
            if (File.Exists("Assets/Zombar/Art/External/Environment/material-map.json"))
                ExternalEnvironmentBuilder.Build();
        }

        private static void ConfigureModels()
        {
            foreach (var path in Directory.GetFiles(Root + "/Models", "*.fbx"))
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\','/')) as ModelImporter;
                if (importer == null) continue;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importAnimation = false;
                importer.SaveAndReimport();
                foreach (var source in AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\','/')).OfType<Material>())
                {
                    string matPath = "Assets/Zombar/Art/Materials/" + source.name.Replace('/','_') + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.SetFloat("_Smoothness", source.name.Contains("steel") ? .5f : .22f);
                        AssetDatabase.CreateAsset(mat,matPath);
                    }
                    mat.SetColor("_BaseColor", source.HasProperty("_Color") ? source.color.gamma : new Color(.25f,.22f,.16f));
                    if (source.name.Contains("lantern glass"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", new Color(1,.49f,.14f)*2.4f);
                    }
                    EditorUtility.SetDirty(mat);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(source), mat);
                }
                importer.SaveAndReimport();
            }
        }

        private static BarProp AddProp(PhysicalBar bar, PropKind kind, Vector3 pos, string label, bool anchored=false)
        {
            var prop=PhysicalBar.MakeProp(kind,pos,anchored); prop.transform.SetParent(bar.transform,true); prop.label=label;
            return prop;
        }
        private static void CreateHelper(PhysicalBar bar)
        {
            var helper=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Helper.prefab"));
            helper.name="Помощник · донор"; helper.transform.SetParent(bar.transform,false);
            helper.transform.localPosition=new Vector3(-1.85f,0,-.2f); helper.transform.localRotation=Quaternion.Euler(0,35,0);
            bar.helper=helper.transform;
            var arm = AddProp(bar,PropKind.HelperArm,new Vector3(-1.57f,1.12f,-.42f),"Схватите и потяните конечность",true);
            arm.attachedLimb = helper.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("LeftForeArm"));
            AddProp(bar,PropKind.HelperEye,new Vector3(-1.75f,1.61f,-.42f),"Схватите и потяните глаз",true);
            Label("ДОНОР № 04\nКровь: наклоните бутылку к крану\nКонечность / глаз: схватить и потянуть",bar.transform,new Vector3(-1.85f,2,-.1f),1f,.28f);
        }
        private static void CreateShop(PhysicalBar bar)
        {
            Cube("Полка улучшений",bar.transform,new Vector3(1.65f,.9f,-1.1f),new Vector3(.7f,.08f,1.1f),Material("Shelf",new Color(.08f,.045f,.025f)));
            var checkout=new GameObject("Кассовый лоток"); checkout.transform.SetParent(bar.transform,false); checkout.transform.localPosition=new Vector3(1.5f,.98f,-1.25f); bar.checkout=checkout.transform;
            Cube("Касса",bar.transform,checkout.transform.position,new Vector3(.3f,.025f,.3f),Material("Checkout",new Color(.28f,.2f,.09f),.7f));
            var actions=new[]{BarAction.UpgradeQuality,BarAction.UpgradeStrength,BarAction.BuyIceBox,BarAction.BuySprinkler,BarAction.BuyDrain};
            var names=new[]{"Рецепты · 12+","Сила · 12+","Холодильник · 16","Спринклер · 16","Насос · 16"};
            for(int i=0;i<actions.Length;i++)
            {
                var offer=AddProp(bar,PropKind.Offer,new Vector3(1.86f,.98f,-.7f-i*.16f),names[i]); offer.offer=actions[i];
                Label(names[i],offer.transform,new Vector3(0,.09f,0),.16f,.04f);
            }
            AddProp(bar,PropKind.Coin,new Vector3(1.35f,.99f,-1.05f),"Жетон кошелька · положите в кассу после товара");
            Label("ЛАВКА\nПоложите товар в лоток, затем жетон",bar.transform,new Vector3(1.65f,1.55f,-1.15f),.7f,.22f);
        }
        private static void AddRecipeCard(Transform parent)
        {
            Cube("Меню на доске",parent,new Vector3(-1.25f,1.6f,.05f),new Vector3(.48f,.42f,.025f),Material("Paper",new Color(.15f,.14f,.1f)));
            Label("РЕЦЕПТЫ\nКровь + глаз = Флип\nКровь + плоть = Сауэр\nГлаз + плоть = Хайбол\n5 встряхиваний → перелить",parent,new Vector3(-1.25f,1.6f,.025f),.43f,.34f);
        }
        private static void ApplyAtmosphere(Transform parent)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.32f,.40f,.46f);
            RenderSettings.ambientEquatorColor=new Color(.23f,.22f,.20f);
            RenderSettings.ambientGroundColor=new Color(.12f,.09f,.08f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.085f; RenderSettings.fogColor=new Color(.075f,.115f,.13f);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                if (light.type==LightType.Directional)
                {
                    light.color=new Color(.96f,.83f,.69f); light.intensity=1.25f;
                    light.transform.rotation=Quaternion.Euler(37,-24,0);
                }
                else light.enabled=false;
            }
            PortraitSpot(parent,"Свет латунного фонаря",new Vector3(-1.25f,2.52f,-1.25f),new Vector3(.1f,1.35f,.4f),new Color(1,.82f,.62f),8,96);
            PortraitSpot(parent,"Отблеск из погреба",new Vector3(1.55f,2.65f,1.85f),new Vector3(0,1.4f,.2f),new Color(.30f,.69f,.85f),17,92);
            PortraitSpot(parent,"Кровавое стекло",new Vector3(-2.5f,2.16f,2.75f),new Vector3(-.9f,1.25f,.55f),new Color(.78f,.22f,.12f),8,80);
            var lampAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/CellarLamp.fbx");
            foreach (float x in new[]{-1.65f,1.65f})
            {
                var lantern=(GameObject)PrefabUtility.InstantiatePrefab(lampAsset,parent);
                lantern.transform.position=new Vector3(x,2.65f,3.45f);
                PortraitSpot(parent,"Свет под плафоном",lantern.transform.position-Vector3.up*.21f,
                    new Vector3(x,1.25f,6.2f),new Color(1,.70f,.38f),14,102);
            }
            var rear=GameObject.Find("Задняя стена");
            if(rear!=null)
            {
                var plaster=Material("Mortuary plaster",Color.white);
                plaster.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Zombar/Art/Materials/MortuaryWall.jpg"));
                plaster.SetTextureScale("_BaseMap",new Vector2(1,-1));
                rear.GetComponent<MeshRenderer>().sharedMaterial=plaster;
            }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
                if (t!=null && t.name=="Неоновая планка") UnityEngine.Object.DestroyImmediate(t.gameObject);
            var grime=Material("Grime",new Color(.025f,.025f,.014f));
            var rust=Material("Rust",new Color(.2f,.065f,.025f));
            var random=new System.Random(19);
            for(int i=0;i<36;i++)
            {
                float x=(float)random.NextDouble()*8-4;
                Cube("Потёк на стойке",parent,new Vector3(x,.6f,-.191f),new Vector3(.015f+(float)random.NextDouble()*.08f,.1f+(float)random.NextDouble()*.45f,.003f),i%3==0?rust:grime);
            }
            // Author-made texture detail rather than a flat, pristine countertop.
            var texture=new Texture2D(128,128,TextureFormat.RGB24,false);
            var pixels=new Color[128*128];
            for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            { float noise=(float)random.NextDouble()*.09f; float grain=Mathf.Sin(x*.7f+y*.035f)*.025f; pixels[y*128+x]=new Color(.11f+noise+grain,.065f+noise*.5f,.028f+noise*.3f); }
            texture.SetPixels(pixels); texture.Apply();
            const string texturePath="Assets/Zombar/Art/Materials/WeatheredWood.png";
            File.WriteAllBytes(texturePath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(texturePath);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
                if(renderer.name.Contains("Филёнка") || renderer.name.Contains("корпус") || renderer.name.Contains("столешница"))
                { var mat=Material("WeatheredWood",Color.white); mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); renderer.sharedMaterial=mat; }
        }
        private static void PortraitSpot(Transform parent,string name,Vector3 source,Vector3 aim,Color colour,float power,float cone)
        {
            var lamp=new GameObject(name);
            lamp.transform.SetParent(parent,false);
            lamp.transform.position=source;
            lamp.transform.rotation=Quaternion.LookRotation(aim-source);
            var light=lamp.AddComponent<Light>();
            light.type=LightType.Spot; light.color=colour; light.intensity=power;
            light.range=7; light.spotAngle=cone; light.innerSpotAngle=cone*.62f;
        }
        private static Material Material(string name,Color color,float metal=0)
        {
            string path="Assets/Zombar/Art/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor",color); mat.SetFloat("_Metallic",metal); mat.SetFloat("_Smoothness",.2f); return mat;
        }
        private static void Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(parent,true);
            obj.transform.position=pos; obj.transform.localScale=size; obj.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private static void Label(string value,Transform parent,Vector3 pos,float width,float height)
        {
            var obj=new GameObject("Карточка"); obj.transform.SetParent(parent,false); obj.transform.localPosition=pos;
            var text=obj.AddComponent<TextMesh>(); text.text=value; text.fontSize=64; text.characterSize=.012f;
            text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; text.color=new Color(.78f,.72f,.49f);
            var layout=obj.AddComponent<BarLabelLayout>(); layout.width=width; layout.height=height;
        }
    }
}




