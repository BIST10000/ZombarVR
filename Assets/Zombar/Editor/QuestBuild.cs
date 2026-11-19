using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEditor.SceneManagement;
using Zombar;

namespace ZombarEditor
{
    public static class QuestBuild
    {
        public static void PrepareScene()
        {
            // A Resources material retains the runtime-created stream/mask shader in player builds.
            const string streamPath="Assets/Zombar/Resources/PhysicalBar/BloodStream.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(streamPath)==null)
            {
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor",new Color(.42f,.015f,.025f));
                AssetDatabase.CreateAsset(material,streamPath);
                AssetDatabase.SaveAssets();
            }
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZombieBar.unity");
            if (UnityEngine.Object.FindAnyObjectByType<PhysicalBar>() == null)
                throw new BuildFailedException("Build the physical bar scene first: НЕКРО-ПАБ/Построить физический бар.");
            IngredientModels.Prepare();
            foreach (string recipe in new[] {"Кровавый флип", "Гнилой сауэр", "Глазной хайбол"})
            {
                var marker = GameObject.Find("Ингредиент · " + recipe);
                if (marker != null) UnityEngine.Object.DestroyImmediate(marker);
            }
            FinishWorkbench();
            CompleteRoom();
            InstallMixer();
            TaproomArt.Apply();
            PrepareMusic();
            EditorSceneManager.SaveScene(scene);
        }

        static void PrepareMusic()
        {
            const string path = "Assets/Zombar/Audio/Music/Джентльмены_удачи.mp3";
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .72f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            var root = GameObject.Find("Музыка бара");
            if (!root) root = new GameObject("Музыка бара");
            var music = root.GetComponent<AudioSource>();
            if (!music) music = root.AddComponent<AudioSource>();
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = .13f;
        }

        static void FinishWorkbench()
        {
            var room = GameObject.Find("CC0 Environment");
            if (room == null) return;
            foreach (Transform fixture in room.transform)
            {
                if (fixture.GetComponent<Collider>() != null) continue;
                foreach (var mesh in fixture.GetComponentsInChildren<MeshFilter>())
                {
                    if (mesh.sharedMesh == null || mesh.GetComponent<Collider>() != null) continue;
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                }
            }
            if (room.transform.Find("Памятки на стойке") != null) return;
            var signs = new GameObject("Памятки на стойке").transform;
            signs.SetParent(room.transform, false);
            PlaceSign(signs, -.67f, "ПОДНОС", "Возьми ингредиенты по рецепту");
            PlaceSign(signs, -.11f, "ШЕЙКЕР", "Положи внутрь и встряхни 5 раз");
            PlaceSign(signs, .41f, "БОКАЛ", "Перелей и поднеси клиенту");
            PlaceSign(signs, 1.35f, "СЛИВ", "Наклони шейкер здесь");
        }

        static void InstallMixer()
        {
            var bar = UnityEngine.Object.FindAnyObjectByType<PhysicalBar>();
            if (GameObject.Find("Миксер · основание") != null) return;
            var cup = bar.shaker;
            for (int i = cup.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(cup.transform.GetChild(i).gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Zombar/Resources/PhysicalBar/Props/Glass.prefab"),cup.transform);
            visual.transform.localScale = Vector3.one * 1.4f;
            cup.transform.position = new Vector3(-.1f,1.25f,-.42f);
            cup.home = cup.transform.position;
            cup.gripOffset = Vector3.down * .1f;
            var bounds = new Bounds(cup.transform.position,Vector3.zero);
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var box = cup.GetComponent<BoxCollider>();
            box.center = cup.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Art/External/Environment/CocktailShaker__lambert1.mat");
            RoomPart(bar.transform,"Миксер · основание",new Vector3(-.02f,1.23f,-.46f),new Vector3(.4f,.04f,.35f),metal);
            var button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = "Нажать · смешать";
            button.transform.SetParent(bar.transform,true);
            button.transform.position = BarMixer.ButtonPosition;
            button.transform.localScale = new Vector3(.085f,.016f,.085f);
            button.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Resources/PhysicalBar/BloodStream.mat");
            foreach (var label in UnityEngine.Object.FindObjectsByType<TextMesh>())
                if (label.text.Contains("Положи внутрь и встряхни"))
                    label.text = "МИКСЕР\nЗагрузи → кнопка → перелей";
        }

        static void CompleteRoom()
        {
            // Keep the enclosure authored in the saved scene for both Editor play and builds.
            if (GameObject.Find("Bar enclosure") == null)
            {
                var enclosure = new GameObject("Bar enclosure").transform;
                var wall = GameObject.Find("Левая стена");
                var material = wall != null ? wall.GetComponent<Renderer>().sharedMaterial : null;
                RoomPart(enclosure, "Задняя стена за барменом", new Vector3(0,2.2f,-4.4f), new Vector3(13.2f,4.4f,.3f), material);
                RoomPart(enclosure, "Левая граница", new Vector3(-6.5f,2.2f,1), new Vector3(.3f,4.4f,11), material);
                RoomPart(enclosure, "Правая граница", new Vector3(6.5f,2.2f,1), new Vector3(.3f,4.4f,11), material);
                RoomPart(enclosure, "Стена за гостями", new Vector3(0,2.2f,6.3f), new Vector3(13.2f,4.4f,.3f), material);
                var floor = RoomPart(enclosure, "Сплошное основание", new Vector3(0,-.11f,1), new Vector3(13,.2f,11), material);
                floor.GetComponent<Renderer>().enabled = false;
            }
            var rear = GameObject.Find("Задняя стена за барменом");
            if (rear != null) rear.transform.rotation = Quaternion.Euler(0,0,180);
            if (rear != null && GameObject.Find("Отделка задней стены") == null)
            {
                var trim = new GameObject("Отделка задней стены").transform;
                rear.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Art/Materials/Mortuary plaster.mat");
                var oak = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Art/Materials/WeatheredWood.mat");
                RoomPart(trim,"Деревянная обшивка",new Vector3(0,.52f,-4.19f),new Vector3(12.9f,1.04f,.10f),oak);
                RoomPart(trim,"Карниз",new Vector3(0,1.04f,-4.12f),new Vector3(12.9f,.08f,.14f),oak);
                for (int i=-2;i<=2;i++)
                    RoomPart(trim,"Стойка обшивки",new Vector3(i*2.6f,2.24f,-4.18f),new Vector3(.12f,4.48f,.14f),oak);
                foreach (float x in new[] {-2.6f,2.6f})
                {
                    var lamp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Zombar/Resources/PhysicalBar/Environment/Lantern.prefab"),trim);
                    lamp.transform.position = new Vector3(x,1.14f,-3.96f);
                    RoomPart(trim,"Полка под фонарь",new Vector3(x,1.1f,-3.98f),new Vector3(.56f,.08f,.48f),oak);
                    var glow = new GameObject("Свет фонаря").AddComponent<Light>();
                    glow.transform.SetParent(trim,false);
                    glow.transform.position = lamp.transform.position + new Vector3(0,.28f,.22f);
                    glow.type = LightType.Point; glow.color = new Color(1,.66f,.37f); glow.range = 3.7f; glow.intensity = 2.2f;
                }
            }
            var sink = GameObject.Find("Раковина · слить смесь");
            if (sink != null) sink.transform.position = new Vector3(1.35f,1.225f,-.45f);
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMesh>())
            {
                if (!text.text.StartsWith("РЕЦЕПТЫ") && !text.text.StartsWith("МЕНЮ\n")) continue;
                var menu = new System.Text.StringBuilder("МЕНЮ\n");
                for (int i=0; i<CocktailRecipes.Names.Length; i++)
                    menu.AppendLine(CocktailRecipes.Names[i] + " — " + CocktailRecipes.Formula(i));
                text.text = menu.ToString();
                text.transform.position = new Vector3(-2.95f,2.12f,5.97f);
                text.transform.localScale = Vector3.one;
                var layout = text.GetComponent<BarLabelLayout>();
                if (layout != null) { layout.width = 1.65f; layout.height = .92f; }
            }
            var rig = UnityEngine.Object.FindAnyObjectByType<BarPlayerInput>();
            if (rig != null)
            {
                rig.transform.position = new Vector3(0,0,-1.45f);
                var camera = rig.GetComponentInChildren<Camera>();
                if (camera != null) camera.transform.localRotation = Quaternion.Euler(20,0,0);
            }
            foreach (var prop in UnityEngine.Object.FindObjectsByType<BarProp>())
            {
                foreach (var collider in prop.GetComponentsInChildren<Collider>()) collider.isTrigger = false;
                var body = prop.GetComponent<Rigidbody>();
                body.isKinematic = prop.anchored;
                body.useGravity = !prop.anchored;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        static GameObject RoomPart(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = size;
            if (material != null) obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static void PlaceSign(Transform parent, float x, string title, string instruction)
        {
            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = title;
            plaque.transform.SetParent(parent, false);
            plaque.transform.position = new Vector3(x, 1.228f, -.764f);
            plaque.transform.rotation = Quaternion.Euler(66, 0, 0);
            plaque.transform.localScale = new Vector3(.39f, .082f, .008f);
            plaque.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombar/Resources/PhysicalBar/BloodStream.mat");
            var obj = new GameObject(title + " · надпись");
            obj.transform.SetParent(parent, false);
            obj.transform.SetPositionAndRotation(plaque.transform.position - plaque.transform.forward * .005f, plaque.transform.rotation);
            var text = obj.AddComponent<TextMesh>();
            text.text = title + "\n" + instruction;
            text.fontSize = 64;
            text.characterSize = .0024f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, .91f, .71f);
        }

        [MenuItem("НЕКРО-ПАБ/Настроить Meta Quest")]
        public static void Configure()
        {
            PlayerSettings.productName = "Necro Pub";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zombar.necropub");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new BuildFailedException("Android OpenXR settings missing.");
            // This is an immersive Quest controller game, not an Android XR / AR application.
            foreach (var feature in settings.GetFeatures<OpenXRFeature>())
            {
                string name = feature.GetType().Name;
                feature.enabled = name == "MetaQuestFeature" || name == "OculusTouchControllerProfile" ||
                                  name == "MetaQuestTouchPlusControllerProfile" || name == "MetaQuestTouchProControllerProfile";
                EditorUtility.SetDirty(feature);
            }
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(settings);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/Project Configuration/Performance URP Config.asset");
            if (pipeline != null)
            {
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = false;
                pipeline.shadowDistance = 12f;
                var serialized = new SerializedObject(pipeline);
                serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
                serialized.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Quest: ARM64 / IL2CPP / Vulkan / OpenXR stereo configured.");
        }

        [MenuItem("НЕКРО-ПАБ/Собрать APK для Meta Quest")]
        public static void BuildApk()
        {
            Configure();
            PrepareScene();
            Directory.CreateDirectory("Builds/Quest");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/ZombieBar.unity" },
                locationPathName = "Builds/Quest/NecroPub.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            File.WriteAllText("Builds/Quest/build-result.txt", report.summary.result + "\n" +
                report.summary.totalErrors + " errors\n" + report.summary.totalSize + " bytes");
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Quest build failed. See Editor console and Builds/Quest/build-result.txt.");
        }
    }
}







