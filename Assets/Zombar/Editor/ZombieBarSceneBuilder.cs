using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Zombar;

namespace ZombarEditor
{
    public static class ZombieBarSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ZombieBar.unity";
        private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        [MenuItem("НЕКРО-ПАБ/Собрать игровую сцену")]
        public static void BuildDemo()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.ClearLightingDataAsset();
            ConfigureLighting();
            BuildRoom();
            BuildCounter();
            BuildAssistant();
            BuildRecipeStations();
            BuildShop();
            BuildDecor();
            BuildPlayer();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Игровая сцена НЕКРО-ПАБ готова: " + ScenePath);
        }

        private static void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.14f, 0.24f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.095f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.022f, 0.04f);
            RenderSettings.ambientIntensity = 0.85f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.028f, 0.075f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.009f;
            Camera.main?.gameObject.SetActive(false);

            GameObject lightObject = new GameObject("Луна · верхний свет");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.61f, 0.69f, 1f);
            light.intensity = 0.58f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;

            AddPointLight("Неон · мятный контур", new Vector3(-3.4f, 2.65f, 1.2f), new Color(0.12f, 1f, 0.68f), 4.6f, 1.25f);
            AddPointLight("Неон · фиолетовый контур", new Vector3(3.3f, 2.6f, 2f), new Color(0.63f, 0.17f, 1f), 4.8f, 1.35f);
            AddPointLight("Свет · янтарное стекло", new Vector3(0f, 2.5f, -1.15f), new Color(1f, 0.39f, 0.11f), 4.3f, 1.4f);
            AddPointLight("Фонарь · над стойкой", new Vector3(0f, 3.5f, 2.45f), new Color(1f, 0.68f, 0.34f), 3.6f, 1.55f);
            AddPointLight("Фонарь · восточная стена", new Vector3(5.3f, 2.9f, 4.4f), new Color(0.56f, 0.32f, 1f), 3.2f, 1.1f);
            AddPointLight("Фонарь · западная стена", new Vector3(-5.3f, 2.9f, 4.4f), new Color(0.26f, 0.72f, 0.68f), 3.2f, 1.1f);
            AddGlobalVolume();
        }

        private static void BuildRoom()
        {
            Material floor = MaterialFor(new Color(0.15f, 0.075f, 0.07f), 0.16f, 0.38f);
            Material wall = MaterialFor(new Color(0.085f, 0.075f, 0.13f), 0.08f, 0.55f);
            Material trim = MaterialFor(new Color(0.28f, 0.15f, 0.095f), 0.42f, 0.7f);
            Cube("Пол · потёртая плитка", new Vector3(0f, -0.2f, 1f), new Vector3(13f, 0.4f, 11f), floor);
            Cube("Задняя стена", new Vector3(0f, 2.2f, 6.45f), new Vector3(13f, 4.8f, 0.35f), wall);
            Cube("Левая стена", new Vector3(-6.5f, 2.2f, 1f), new Vector3(0.35f, 4.8f, 11f), wall);
            Cube("Правая стена", new Vector3(6.5f, 2.2f, 1f), new Vector3(0.35f, 4.8f, 11f), wall);
            Cube("Плинтус задней стены", new Vector3(0f, 0.1f, 6.19f), new Vector3(13f, 0.22f, 0.12f), trim);
            Cube("Потолок · тёмный дуб", new Vector3(0f, 4.72f, 1f), new Vector3(13f, 0.28f, 11f), MaterialFor(new Color(0.055f, 0.035f, 0.06f), 0.12f, 0.3f));
            for (int z = -4; z <= 6; z += 2)
                Cube("Балка потолка", new Vector3(0f, 4.53f, z), new Vector3(13f, 0.18f, 0.16f), trim);

            for (int z = 0; z <= 5; z += 1)
                Cube("Шов плитки", new Vector3(0f, 0.004f, z), new Vector3(12.8f, 0.004f, 0.018f), MaterialFor(new Color(0.22f, 0.12f, 0.11f), 0.12f));

            for (int x = -5; x <= 5; x += 2)
            {
                Cube("Шов плитки", new Vector3(x, 0.004f, 0.5f), new Vector3(0.018f, 0.004f, 10.4f), MaterialFor(new Color(0.22f, 0.12f, 0.11f), 0.12f));
            }

            Cube("Коврик за стойкой", new Vector3(0f, 0.02f, -1.45f), new Vector3(7.6f, 0.045f, 1.2f), MaterialFor(new Color(0.17f, 0.09f, 0.15f)));
            BuildStainedGlassWindows();
        }

        private static void BuildStainedGlassWindows()
        {
            Material frame = MaterialFor(new Color(0.43f, 0.22f, 0.1f), 0.62f, 0.72f);
            Material tealGlass = EmissiveMaterialFor(new Color(0.08f, 0.64f, 0.52f));
            Material violetGlass = EmissiveMaterialFor(new Color(0.37f, 0.15f, 0.72f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 4.45f;
                Cube("Витраж · тёмный проём", new Vector3(x, 3.15f, 6.22f), new Vector3(1.36f, 1.72f, 0.08f), MaterialFor(new Color(0.025f, 0.035f, 0.075f), 0.18f));
                Cube("Витраж · стекло цвета морской волны", new Vector3(x - 0.29f, 3.17f, 6.16f), new Vector3(0.48f, 1.38f, 0.035f), tealGlass);
                Cube("Витраж · фиолетовое стекло", new Vector3(x + 0.29f, 3.17f, 6.16f), new Vector3(0.48f, 1.38f, 0.035f), violetGlass);
                Cube("Витраж · верхняя перемычка", new Vector3(x, 3.94f, 6.1f), new Vector3(1.48f, 0.12f, 0.16f), frame);
                Cube("Витраж · нижняя перемычка", new Vector3(x, 2.4f, 6.1f), new Vector3(1.48f, 0.12f, 0.16f), frame);
                Cube("Витраж · левая стойка", new Vector3(x - 0.68f, 3.17f, 6.1f), new Vector3(0.12f, 1.65f, 0.16f), frame);
                Cube("Витраж · правая стойка", new Vector3(x + 0.68f, 3.17f, 6.1f), new Vector3(0.12f, 1.65f, 0.16f), frame);
                Cube("Витраж · крестовина", new Vector3(x, 3.17f, 6.08f), new Vector3(0.1f, 1.48f, 0.18f), frame);
                Cube("Витраж · горизонтальная вязь", new Vector3(x, 3.17f, 6.08f), new Vector3(1.22f, 0.08f, 0.18f), frame);
                Primitive("Витраж · шип", PrimitiveType.Sphere, null, new Vector3(x, 4.03f, 6.13f), new Vector3(0.18f, 0.22f, 0.08f), frame);
            }
        }

        private static void AddGlobalVolume()
        {
            const string folder = "Assets/Zombar/Settings";
            const string profilePath = folder + "/NecroBarVolume.asset";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Zombar")) AssetDatabase.CreateFolder("Assets", "Zombar");
                AssetDatabase.CreateFolder("Assets/Zombar", "Settings");
            }

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(0.78f);
            bloom.intensity.Override(0.48f);
            bloom.scatter.Override(0.62f);

            ColorAdjustments color = GetOrAddVolumeComponent<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.Override(0f);
            color.contrast.Override(10f);
            color.saturation.Override(7f);

            Vignette vignette = GetOrAddVolumeComponent<Vignette>(profile);
            vignette.active = true;
            vignette.color.Override(new Color(0.035f, 0.018f, 0.07f));
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.55f);

            Volume volume = new GameObject("Атмосфера · URP volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.components.Find(item => item is T) as T;
            if (component != null) return component;
            component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void BuildCounter()
        {
            Material wood = MaterialFor(new Color(0.22f, 0.105f, 0.075f));
            Material top = MaterialFor(new Color(0.12f, 0.24f, 0.23f), 0.24f);
            Material brass = MaterialFor(new Color(0.87f, 0.48f, 0.15f), 0.3f);
            Cube("Стойка · корпус", new Vector3(0f, 0.55f, 0.16f), new Vector3(9.5f, 1.05f, 0.62f), wood);
            Cube("Стойка · столешница", new Vector3(0f, 1.13f, 0.13f), new Vector3(9.75f, 0.16f, 0.86f), top);
            Cube("Медная окантовка", new Vector3(0f, 1.07f, -0.307f), new Vector3(9.55f, 0.055f, 0.035f), brass);
            Cube("Передняя панель", new Vector3(0f, 0.53f, -0.165f), new Vector3(9.05f, 0.72f, 0.04f), MaterialFor(new Color(0.36f, 0.17f, 0.12f)));
            for (int i = 0; i < 5; i++)
            {
                float x = -3.6f + i * 1.8f;
                Cube("Филёнка стойки", new Vector3(x, 0.53f, -0.193f), new Vector3(1.55f, 0.48f, 0.025f), MaterialFor(new Color(0.21f, 0.08f, 0.075f), 0.14f, 0.42f));
                Cube("Латунная рамка панели", new Vector3(x, 0.53f, -0.21f), new Vector3(1.42f, 0.018f, 0.015f), brass);
                Primitive("Заклёпка стойки", PrimitiveType.Sphere, null, new Vector3(x - 0.68f, 0.53f, -0.225f), new Vector3(0.055f, 0.055f, 0.035f), brass);
                Primitive("Заклёпка стойки", PrimitiveType.Sphere, null, new Vector3(x + 0.68f, 0.53f, -0.225f), new Vector3(0.055f, 0.055f, 0.035f), brass);
            }
            Cube("Знак · подложка", new Vector3(0f, 0.59f, -0.25f), new Vector3(2.6f, 0.42f, 0.08f), MaterialFor(new Color(0.055f, 0.045f, 0.09f), 0.36f, 0.62f));
            Label("МЕРТВЕЦКИ ВКУСНО", new Vector3(0f, 0.62f, -0.31f), 0.18f, new Color(0.42f, 1f, 0.74f), 180f);

            for (int i = 0; i < 5; i++)
            {
                float x = -3.7f + i * 1.85f;
                Cylinder("Барный табурет · ножка", new Vector3(x, 0.28f, 1.25f), new Vector3(0.12f, 0.28f, 0.12f), brass);
                Cylinder("Барный табурет · сиденье", new Vector3(x, 0.63f, 1.25f), new Vector3(0.52f, 0.09f, 0.52f), MaterialFor(new Color(0.29f, 0.13f, 0.17f)));
                Cube("Подножка табурета", new Vector3(x, 0.34f, 1.25f), new Vector3(0.52f, 0.055f, 0.52f), brass);
            }
        }

        private static void BuildAssistant()
        {
            Material skin = MaterialFor(new Color(0.38f, 0.63f, 0.42f));
            GameObject helper = new GameObject("Помощник · регенерирует");
            helper.transform.position = new Vector3(-5.12f, 0f, 0.62f);
            Primitive("Туловище", PrimitiveType.Capsule, helper.transform, new Vector3(0f, 0.83f, 0f), new Vector3(0.52f, 0.56f, 0.42f), MaterialFor(new Color(0.21f, 0.28f, 0.3f)));
            Primitive("Голова", PrimitiveType.Sphere, helper.transform, new Vector3(0f, 1.61f, 0f), new Vector3(0.53f, 0.53f, 0.52f), skin);
            Primitive("Глаз", PrimitiveType.Sphere, helper.transform, new Vector3(-0.12f, 1.66f, -0.24f), new Vector3(0.095f, 0.095f, 0.065f), MaterialFor(new Color(1f, 0.25f, 0.12f), 0.05f));
            Primitive("Левая рука", PrimitiveType.Capsule, helper.transform, new Vector3(-0.45f, 0.82f, 0f), new Vector3(0.19f, 0.48f, 0.19f), skin);
            Primitive("Правая рука", PrimitiveType.Capsule, helper.transform, new Vector3(0.45f, 0.82f, 0f), new Vector3(0.19f, 0.48f, 0.19f), skin);
            Label("ЖИВОЙ ПОМОЩНИК", new Vector3(-5.1f, 2.0f, 0.55f), 0.12f, new Color(0.55f, 1f, 0.73f));

            Target("СНЯТЬ РУКУ", BarAction.HarvestArm, new Vector3(-5.05f, 0.32f, -0.52f), new Color(0.53f, 0.28f, 0.18f));
            Target("ВЗЯТЬ ГЛАЗА", BarAction.HarvestEye, new Vector3(-3.95f, 0.32f, -0.52f), new Color(0.31f, 0.3f, 0.56f));
            Target("ВЗЯТЬ КРОВЬ", BarAction.DrawBlood, new Vector3(-2.85f, 0.32f, -0.52f), new Color(0.67f, 0.12f, 0.2f));
            Target("ВОССТАНОВИТЬСЯ", BarAction.RestoreBody, new Vector3(-5.05f, 0.75f, -0.64f), new Color(0.13f, 0.5f, 0.37f), new Vector3(0.98f, 0.2f, 0.12f));
        }

        private static void BuildRecipeStations()
        {
            Material tray = MaterialFor(new Color(0.19f, 0.24f, 0.28f));
            string[] names = { "Кровавый флип", "Гнилой сауэр", "Глазной хайбол" };
            string[] parts = { "КРОВЬ + ГЛАЗ", "ПЛОТЬ + КРОВЬ", "ПЛОТЬ + ГЛАЗ" };
            BarAction[] actions = { BarAction.BrewBloodFlip, BarAction.BrewRotSour, BarAction.BrewEyeHighball };
            Color[] colors = { new Color(0.64f, 0.12f, 0.25f), new Color(0.41f, 0.34f, 0.14f), new Color(0.22f, 0.47f, 0.43f) };
            for (int i = 0; i < names.Length; i++)
            {
                float x = -1.65f + i * 1.65f;
                Cube("Поднос · " + names[i], new Vector3(x, 1.25f, 0.1f), new Vector3(1.25f, 0.08f, 0.57f), tray);
                Primitive("Бокал · " + names[i], PrimitiveType.Cylinder, null, new Vector3(x, 1.55f, 0.11f), new Vector3(0.18f, 0.22f, 0.18f), MaterialFor(colors[i], 0.22f));
                Target(names[i] + "\n" + parts[i], actions[i], new Vector3(x, 0.35f, -0.5f), colors[i], new Vector3(1.45f, 0.25f, 0.13f));
            }
            Label("СМЕШАТЬ КОКТЕЙЛЬ", new Vector3(0f, 1.72f, -0.1f), 0.11f, new Color(0.75f, 0.95f, 0.88f), 0f);
            Target("УБРАТЬ СТОЙКУ", BarAction.CleanBar, new Vector3(0f, 0.32f, -1.03f), new Color(0.32f, 0.32f, 0.36f), new Vector3(1.45f, 0.2f, 0.12f));
        }

        private static void BuildShop()
        {
            Material shelf = MaterialFor(new Color(0.12f, 0.16f, 0.22f));
            Cube("Полка · улучшения", new Vector3(4.35f, 1.65f, 0.56f), new Vector3(3.55f, 0.12f, 0.48f), shelf);
            Label("МАГАЗИН И МАСТЕРСКАЯ", new Vector3(4.3f, 2.65f, 0.64f), 0.14f, new Color(0.91f, 0.66f, 1f));
            Target("КАЧЕСТВО · 12", BarAction.UpgradeQuality, new Vector3(3.05f, 1.31f, -0.48f), new Color(0.42f, 0.2f, 0.58f), new Vector3(1.08f, 0.23f, 0.13f));
            Target("СИЛА · 12", BarAction.UpgradeStrength, new Vector3(4.25f, 1.31f, -0.48f), new Color(0.68f, 0.24f, 0.16f), new Vector3(1.08f, 0.23f, 0.13f));
            Target("ХОЛОДИЛЬНИК · 16", BarAction.BuyIceBox, new Vector3(5.45f, 1.31f, -0.48f), new Color(0.12f, 0.43f, 0.56f), new Vector3(1.15f, 0.23f, 0.13f));
            Target("СПРИНКЛЕР · 16", BarAction.BuySprinkler, new Vector3(3.65f, 0.37f, -0.52f), new Color(0.15f, 0.47f, 0.51f), new Vector3(1.26f, 0.22f, 0.13f));
            Target("НАСОС · 16", BarAction.BuyDrain, new Vector3(5.0f, 0.37f, -0.52f), new Color(0.12f, 0.32f, 0.65f), new Vector3(1.15f, 0.22f, 0.13f));
        }

        private static void BuildDecor()
        {
            Material bottleA = MaterialFor(new Color(0.08f, 0.42f, 0.26f), 0.19f);
            Material bottleB = MaterialFor(new Color(0.44f, 0.12f, 0.31f), 0.17f);
            Material cork = MaterialFor(new Color(0.72f, 0.48f, 0.2f), 0.12f, 0.62f);
            Material label = MaterialFor(new Color(0.82f, 0.68f, 0.43f), 0.12f, 0.48f);
            for (int i = 0; i < 8; i++)
            {
                float x = -3.75f + i * 1.07f;
                Material glass = i % 2 == 0 ? bottleA : bottleB;
                Cylinder("Бутылка · корпус", new Vector3(x, 1.43f, 0.46f), new Vector3(0.14f, 0.23f, 0.14f), glass);
                Primitive("Бутылка · плечики", PrimitiveType.Sphere, null, new Vector3(x, 1.64f, 0.46f), new Vector3(0.14f, 0.13f, 0.14f), glass);
                Cylinder("Бутылка · горлышко", new Vector3(x, 1.78f, 0.46f), new Vector3(0.065f, 0.1f, 0.065f), glass);
                Cylinder("Пробка", new Vector3(x, 1.89f, 0.46f), new Vector3(0.055f, 0.055f, 0.055f), cork);
                Cube("Этикетка бутылки", new Vector3(x, 1.45f, 0.318f), new Vector3(0.17f, 0.16f, 0.012f), label);
                Primitive("Печать на этикетке", PrimitiveType.Sphere, null, new Vector3(x, 1.45f, 0.307f), new Vector3(0.045f, 0.06f, 0.018f), EmissiveMaterialFor(i % 2 == 0 ? new Color(0.12f, 0.7f, 0.44f) : new Color(0.68f, 0.2f, 0.48f)));
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -4f + i * 2f;
                Cube("Неоновая планка", new Vector3(x, 2.75f, 6.18f), new Vector3(0.55f, 0.055f, 0.04f), MaterialFor(new Color(0.2f, 0.72f, 0.64f), 0.1f));
            }
            Label("СМЕНА НАЧАЛАСЬ · НЕ КОРМИТЬ ТИТАНА", new Vector3(0f, 3.2f, 6.15f), 0.22f, new Color(0.54f, 1f, 0.76f));

            // A few tables and candles make the patron area read as a bar, while keeping a clear aisle.
            for (int i = 0; i < 3; i++)
            {
                float x = -4.6f + i * 4.6f;
                if (Mathf.Abs(x) < 2f) continue;
                Cube("Столик", new Vector3(x, 0.62f, 3.55f), new Vector3(1.15f, 0.12f, 0.78f), MaterialFor(new Color(0.22f, 0.12f, 0.1f)));
                Cylinder("Ножка столика", new Vector3(x, 0.27f, 3.55f), new Vector3(0.1f, 0.32f, 0.1f), MaterialFor(new Color(0.53f, 0.3f, 0.13f)));
                Cylinder("Свеча на столике", new Vector3(x, 0.79f, 3.55f), new Vector3(0.055f, 0.1f, 0.055f), MaterialFor(new Color(0.82f, 0.72f, 0.48f), 0.02f, 0.72f));
                Primitive("Пламя свечи", PrimitiveType.Sphere, null, new Vector3(x, 0.91f, 3.55f), new Vector3(0.045f, 0.085f, 0.045f), EmissiveMaterialFor(new Color(1f, 0.42f, 0.08f)));
            }
        }

        private static void BuildPlayer()
        {
            GameObject xrOrigin = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
            if (prefab != null)
            {
                xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                xrOrigin.name = "XR игрок · OpenXR";
                xrOrigin.transform.position = new Vector3(0f, 0f, -3.25f);
                xrOrigin.transform.rotation = Quaternion.identity;
                Camera camera = xrOrigin.GetComponentInChildren<Camera>(true);
                if (camera != null)
                {
                    camera.tag = "MainCamera";
                    camera.nearClipPlane = 0.06f;
                    ConfigureCameraPostProcessing(camera);
                }
                if (xrOrigin.GetComponent<BarPlayerInput>() == null)
                    xrOrigin.AddComponent<BarPlayerInput>();
            }

            if (xrOrigin == null)
            {
                xrOrigin = new GameObject("XR игрок · резервный вид");
                xrOrigin.transform.position = new Vector3(0f, 0f, -3.25f);
                GameObject cameraObject = new GameObject("Камера бармена");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(xrOrigin.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
                Camera fallbackCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                ConfigureCameraPostProcessing(fallbackCamera);
                xrOrigin.AddComponent<BarPlayerInput>();
            }

            if (Object.FindAnyObjectByType<XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            GameObject manager = new GameObject("Системы бара");
            manager.AddComponent<ZombieBarGame>();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            string guid = AssetDatabase.AssetPathToGUID(path);
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].guid.ToString() == guid || existing[i].path == path) continue;
                scenes.Add(existing[i]);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddPointLight(string name, Vector3 position, Color color, float range, float intensity)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            Light light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static void ConfigureCameraPostProcessing(Camera camera)
        {
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask |= 1;
        }

        private static void Target(string label, BarAction action, Vector3 position, Color color, Vector3? size = null)
        {
            Vector3 scale = size ?? new Vector3(0.9f, 0.3f, 0.16f);
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Действие · " + label.Replace('\n', ' ');
            button.transform.position = position;
            button.transform.localScale = scale;
            button.GetComponent<Renderer>().sharedMaterial = MaterialFor(color, 0.28f);
            BarTarget target = button.AddComponent<BarTarget>();
            target.action = action;
            target.displayLabel = label;

            GameObject textObject = new GameObject("Подпись · " + label.Replace('\n', ' '));
            textObject.transform.SetParent(button.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            textObject.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.characterSize = 0.055f;
            text.fontSize = 42;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            var layout = textObject.AddComponent<BarLabelLayout>();
            layout.width = scale.x * 0.88f;
            layout.height = scale.y * 0.8f;
        }

        private static void Label(string text, Vector3 position, float characterSize, Color color, float yaw = 180f)
        {
            GameObject label = new GameObject("Вывеска · " + text);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = color;
            var layout = label.AddComponent<BarLabelLayout>();
            layout.width = text.Contains("СМЕНА") ? 7f : 2.8f;
            layout.height = 0.32f;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            return Primitive(name, PrimitiveType.Cylinder, null, position, scale, material);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
                obj.transform.localPosition = position;
                obj.transform.localScale = scale;
            }
            else
            {
                obj.transform.position = position;
                obj.transform.localScale = scale;
            }
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static Material MaterialFor(Color color, float metallic = 0.05f, float smoothness = 0.45f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private static Material EmissiveMaterialFor(Color color)
        {
            Material material = MaterialFor(color, 0.08f, 0.68f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.65f);
            }
            return material;
        }
    }
}
