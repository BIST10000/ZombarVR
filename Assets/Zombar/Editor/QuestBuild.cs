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
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ZombieBar.unity");
            if (UnityEngine.Object.FindAnyObjectByType<PhysicalBar>() == null)
                throw new BuildFailedException("Build the physical bar scene first: НЕКРО-ПАБ/Построить физический бар.");
            EditorSceneManager.SaveScene(scene);
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

