using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zombar
{
    public sealed class BarHazardVisual : MonoBehaviour
    {
        private readonly List<Transform> animated = new List<Transform>();
        private bool isFire;
        private Light glow;
        private float seed;

        public void Initialize(bool fire)
        {
            isFire = fire;
            seed = Random.Range(0f, 10f);
            if (isFire) BuildFire(); else BuildFlood();
        }

        private void BuildFire()
        {
            for (int i = 0; i < 9; i++)
            {
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Язык пламени";
                flame.transform.SetParent(transform, false);
                flame.transform.localPosition = new Vector3(Random.Range(-0.48f, 0.48f), Random.Range(0.15f, 0.65f), Random.Range(-0.35f, 0.35f));
                flame.transform.localScale = new Vector3(0.25f, Random.Range(0.5f, 1.15f), 0.25f);
                RemoveCollider(flame);
                flame.GetComponent<Renderer>().sharedMaterial = Material(new Color(1f, i % 2 == 0 ? 0.16f : 0.55f, 0.025f), true);
                animated.Add(flame.transform);
            }
            GameObject lightObject = new GameObject("Свет пожара");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up;
            glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.2f, 0.035f);
            glow.range = 5f;
            glow.intensity = 3.2f;
            CreateParticles(new Color(1f, 0.3f, 0.02f, 0.85f), 0.11f, 1.3f, 22f, true);
        }

        private void BuildFlood()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Вода";
            water.transform.SetParent(transform, false);
            water.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            water.transform.localScale = new Vector3(5.4f, 0.06f, 4.2f);
            RemoveCollider(water);
            water.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.035f, 0.28f, 0.38f, 0.62f), false, true);
            animated.Add(water.transform);
            for (int i = 0; i < 7; i++)
            {
                GameObject pipeDrop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pipeDrop.name = "Брызги";
                pipeDrop.transform.SetParent(transform, false);
                pipeDrop.transform.localPosition = new Vector3(Random.Range(-2f, 2f), 0.09f, Random.Range(-1.5f, 1.5f));
                pipeDrop.transform.localScale = new Vector3(0.28f, 0.025f, 0.28f);
                RemoveCollider(pipeDrop);
                pipeDrop.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.25f, 0.75f, 0.88f, 0.7f), true, true);
                animated.Add(pipeDrop.transform);
            }
            CreateParticles(new Color(0.25f, 0.75f, 0.9f, 0.6f), 0.06f, 0.35f, 12f, false);
        }

        private void CreateParticles(Color color, float size, float speed, float rate, bool up)
        {
            GameObject particleObject = new GameObject(isFire ? "Искры" : "Пена");
            particleObject.transform.SetParent(transform, false);
            ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = up ? 1.1f : 1.8f;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = up ? 16f : 75f;
            shape.radius = 0.45f;
            particleObject.transform.localRotation = Quaternion.Euler(up ? -90f : 0f, 0f, 0f);
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material(color, true, color.a < 1f);
        }

        private void Update()
        {
            float t = Time.time + seed;
            for (int i = 0; i < animated.Count; i++)
            {
                if (isFire)
                {
                    Vector3 scale = animated[i].localScale;
                    scale.y = 0.65f + Mathf.Abs(Mathf.Sin(t * 5f + i * 1.7f)) * 0.65f;
                    animated[i].localScale = scale;
                    animated[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 12f);
                }
                else if (i == 0)
                {
                    Vector3 position = animated[i].localPosition;
                    position.y = 0.04f + Mathf.Sin(t * 1.4f) * 0.018f;
                    animated[i].localPosition = position;
                }
                else
                {
                    float pulse = 0.75f + Mathf.Sin(t * 2f + i) * 0.25f;
                    animated[i].localScale = new Vector3(0.35f * pulse, 0.025f, 0.35f * pulse);
                }
            }
            if (glow != null) glow.intensity = 2.6f + Mathf.Sin(t * 11f) * 0.55f;
        }

        private static void RemoveCollider(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private static Material Material(Color color, bool emission, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.color = color;
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            return material;
        }
    }
}
