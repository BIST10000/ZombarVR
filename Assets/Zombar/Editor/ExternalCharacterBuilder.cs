using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ZombarEditor
{
    public static class ExternalCharacterBuilder
    {
        private const string Source = "Assets/Zombar/Art/External/Mixamo/";
        private const string Output = "Assets/Zombar/Resources/PhysicalBar/";
        [Serializable] private class Catalog { public Model[] models; }
        [Serializable] private class Model { public string file; public Surface[] materials; }
        [Serializable] private class Surface { public string name; public string albedo; public string normal; }

        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Source + "material-map.json"));
            var idle = ImportMotion("Zombie_Idle", true);
            var attack = ImportMotion("Zombie_Punching", true);
            var dead = ImportMotion("Zombie_Death", false);
            var controller = CreateController(idle, attack, dead);
            BuildModel(catalog, "Warzombie_F_Pedroso.fbx", "PatronMale", 1.82f, controller);
            BuildModel(catalog, "Zombiegirl_W_Kurniawan.fbx", "PatronFemale", 1.73f, controller);
            BuildModel(catalog, "Yaku_J_Ignite.fbx", "Helper", 1.83f, controller);
            BuildModel(catalog, "Mutant.fbx", "Titan", 3.05f, controller);
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip ImportMotion(string name, bool loop)
        {
            string path = Source + "Animations/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = name;
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__"));
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip attack, AnimationClip death)
        {
            string path = Output + "ExternalPatron.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                controller.AddParameter("Angry", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
                var machine = controller.layers[0].stateMachine;
                var rest = machine.AddState("Idle");
                var strike = machine.AddState("Attack");
                var dead = machine.AddState("Dead");
                machine.defaultState = rest;
                var a = rest.AddTransition(strike); a.hasExitTime = false; a.duration = .2f;
                a.AddCondition(AnimatorConditionMode.If, 0, "Angry");
                var b = strike.AddTransition(rest); b.hasExitTime = false; b.duration = .2f;
                b.AddCondition(AnimatorConditionMode.IfNot, 0, "Angry");
                var d = machine.AddAnyStateTransition(dead); d.hasExitTime = false; d.duration = .15f;
                d.canTransitionToSelf = false; d.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            }
            foreach (var state in controller.layers[0].stateMachine.states)
            {
                state.state.motion = state.state.name == "Dead" ? death : state.state.name == "Attack" ? attack : idle;
                EditorUtility.SetDirty(state.state);
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Texture2D Texture(string file, bool normal)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string path = Source + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            var platform = importer.GetPlatformTextureSettings("Android");
            platform.overridden = true; platform.maxTextureSize = 2048;
            platform.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void BuildModel(Catalog catalog, string file, string name, float height, AnimatorController controller)
        {
            string path = Source + file;
            var model = catalog.models.First(m => m.file == file);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            if (file == "Mutant.fbx") ConfigureMutantAvatar(importer, path);
            foreach (var surface in model.materials)
            {
                string matPath = Source + name + "_" + surface.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, matPath);
                }
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BaseMap", Texture(surface.albedo, false));
                var colorImporter = (TextureImporter)AssetImporter.GetAtPath(Source + surface.albedo);
                bool cutout = colorImporter.DoesSourceTextureHaveAlpha();
                material.SetFloat("_AlphaClip", cutout ? 1 : 0);
                material.SetFloat("_Cutoff", .45f);
                material.SetFloat("_Cull", cutout ? 0 : 2);
                if (cutout)
                {
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                }
                material.SetTexture("_BumpMap", Texture(surface.normal, true));
                material.SetFloat("_BumpScale", 1);
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_Smoothness", .24f);
                if (!string.IsNullOrEmpty(surface.normal)) material.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), surface.name), material);
            }
            importer.SaveAndReimport();
            var root = new GameObject(name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) throw new Exception(file + " has no skinned mesh.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            instance.transform.localScale *= height / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var animator = instance.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new Exception(file + " failed Humanoid avatar validation.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root, Output + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureMutantAvatar(ModelImporter importer, string path)
        {
            // The asymmetric claws confuse Unity's automatic hand detection.
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var transforms = source.GetComponentsInChildren<Transform>();
            var aliases = new Dictionary<string,string>
            {
                {"Hips","Hips"}, {"Spine","Spine"}, {"Chest","Spine1"}, {"UpperChest","Spine2"},
                {"Neck","Neck"}, {"Head","Head"},
                {"LeftShoulder","LeftShoulder"}, {"RightShoulder","RightShoulder"},
                {"LeftUpperArm","LeftArm"}, {"RightUpperArm","RightArm"},
                {"LeftLowerArm","LeftForeArm"}, {"RightLowerArm","RightForeArm"},
                {"LeftHand","LeftHand"}, {"RightHand","RightHand"},
                {"LeftUpperLeg","LeftUpLeg"}, {"RightUpperLeg","RightUpLeg"},
                {"LeftLowerLeg","LeftLeg"}, {"RightLowerLeg","RightLeg"},
                {"LeftFoot","LeftFoot"}, {"RightFoot","RightFoot"},
                {"LeftToes","LeftToeBase"}, {"RightToes","RightToeBase"}
            };
            var bones = new List<HumanBone>();
            foreach (var entry in aliases)
            {
                var bone = transforms.FirstOrDefault(t => t.name.EndsWith(":" + entry.Value) || t.name == entry.Value);
                if (bone == null) throw new Exception("Mutant skeleton missing " + entry.Value);
                bones.Add(new HumanBone { humanName = entry.Key, boneName = bone.name,
                    limit = new HumanLimit { useDefaultValues = true } });
            }
            var description = importer.humanDescription;
            description.human = bones.ToArray();
            description.skeleton = transforms.Select(t => new SkeletonBone
            {
                name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale
            }).ToArray();
            importer.humanDescription = description;
        }
    }
}
