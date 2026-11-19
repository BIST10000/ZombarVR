using UnityEditor;
using UnityEngine;
using Zombar;

namespace ZombarEditor
{
    public static class IngredientModels
    {
        public static void Prepare()
        {
            foreach (string name in new[] { "Eye", "Flesh" })
            {
                string path = "Assets/Zombar/Art/Ingredients/" + name;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".obj");
                var material = AssetDatabase.LoadAssetAtPath<Material>(path + ".mat");
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, path + ".mat");
                }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path + ".png"));
                var normalPath = path + "Normal.png";
                var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                }
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_Smoothness", name == "Eye" ? .84f : .52f);
                if (name == "Eye")
                {
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", .55f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType","TransparentCutout");
                    material.renderQueue = 2450;
                }
                EditorUtility.SetDirty(material);
                var root = new GameObject(name);
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);
                var renderers = mesh.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    bounds.Encapsulate(renderer.bounds);
                }
                mesh.transform.localScale *= (name == "Eye" ? .030f : .065f) / bounds.size.x;
                string prefab = "Assets/Zombar/Resources/PhysicalBar/Props/" + name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
                Object.DestroyImmediate(root);
                if (name != "Eye") continue;
                foreach (var prop in Object.FindObjectsByType<BarProp>())
                {
                    if (prop.kind != PropKind.HelperEye || prop.transform.Find("Donor eye") != null) continue;
                    for (int i = prop.transform.childCount-1; i >= 0; i--)
                        Object.DestroyImmediate(prop.transform.GetChild(i).gameObject);
                    var eye = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),prop.transform);
                    eye.name = "Donor eye";
                }
            }
        }
    }
}

