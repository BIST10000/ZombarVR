using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Zombar
{
    public sealed class PhysicalBar : MonoBehaviour
    {
        public static PhysicalBar Instance { get; private set; }
        public BarProp shaker;
        public Transform checkout;
        public Transform helper;
        public Transform workSurface;
        private readonly Dictionary<BarProp, Vector3> lastPositions = new Dictionary<BarProp, Vector3>();
        private bool pumpDown;
        private float wipeDistance;
        private float injection;
        private float bellCooldown;
        private float refillAt;
        private readonly List<BarProp> frameProps = new List<BarProp>();
        private LineRenderer stream;
        private BarProp selectedOffer;
        private ZombieBarGame Game => ZombieBarGame.Instance;
        public static readonly string[] Recipes = { "Кровавый флип", "Гнилой сауэр", "Глазной хайбол" };

        private void Awake() { Instance = this; }
        private void Start()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                Transform space = camera.transform.parent;
                foreach (XRNode node in new[] { XRNode.LeftHand, XRNode.RightHand })
                {
                    var obj = new GameObject(node == XRNode.LeftHand ? "Левая физическая рука" : "Правая физическая рука");
                    obj.AddComponent<BarHand>().Initialize(node, space, camera);
                }
            }
            var lineObject = new GameObject("Струя крови");
            stream = lineObject.AddComponent<LineRenderer>();
            stream.positionCount = 2;
            stream.startWidth = 0.012f; stream.endWidth = 0.008f;
            stream.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            stream.sharedMaterial.color = new Color(0.42f, 0.015f, 0.025f);
            stream.enabled = false;
            RefillTray();
            Game?.Hud?.SetHint("Хват — взять и удерживать · наклон — налить · движение рукой — удар или работа инструментом");
        }

        public static GameObject MakeVisual(string model, Transform parent, Vector3 size, Color tint)
        {
            var prefab = Resources.Load<GameObject>("PhysicalBar/Models/" + model);
            if (prefab == null) throw new System.InvalidOperationException("Missing authored model: " + model);
            var obj = Instantiate(prefab, parent, false);
            obj.name = model;
            return obj;
        }

        public static BarProp MakeProp(PropKind kind, Vector3 position, bool anchored = false)
        {
            var obj = new GameObject(kind.ToString());
            obj.transform.position = position;
            var visual = MakeVisual(kind == PropKind.HelperArm ? "Arm" : kind == PropKind.HelperEye ? "Eye" : kind.ToString(), obj.transform, Vector3.one, Color.white);
            var bounds = new Bounds(position, Vector3.zero);
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var collider = obj.AddComponent<BoxCollider>();
            collider.center = obj.transform.InverseTransformPoint(bounds.center);
            collider.size = Vector3.Max(bounds.size, Vector3.one * 0.035f);
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = kind == PropKind.Cleaver ? 0.65f : 0.18f;
            rb.isKinematic = anchored;
            var prop = obj.AddComponent<BarProp>();
            prop.kind = kind; prop.anchored = anchored;
            prop.home = position; prop.homeRotation = Quaternion.identity;
            return prop;
        }

        public void SpawnIngredient(PropKind kind, Vector3 position)
        {
            var prop = MakeProp(kind, position);
            prop.label = kind == PropKind.Eye ? "Глаз — бросьте в шейкер" : "Плоть — бросьте в шейкер";
        }

        private void RefillTray()
        {
            if (Game == null) return;
            int eyes = 0, flesh = 0;
            foreach (var item in BarProp.All) { if (item.kind == PropKind.Eye) eyes++; if (item.kind == PropKind.Flesh) flesh++; }
            if (eyes < 2 && Game.TakePhysicalStock(PropKind.Eye)) SpawnIngredient(PropKind.Eye, new Vector3(-0.65f + eyes * 0.13f, 1.28f, -0.42f));
            if (flesh < 2 && Game.TakePhysicalStock(PropKind.Flesh)) SpawnIngredient(PropKind.Flesh, new Vector3(-0.65f + flesh * 0.14f, 1.28f, -0.68f));
        }

        public bool AddIngredient(BarProp container, PropKind ingredient)
        {
            if (container == null || container.kind != PropKind.Shaker || container.recipe >= 0 || container.blood + container.eyes + container.flesh >= 3) return false;
            if (ingredient == PropKind.Eye) container.eyes++;
            else if (ingredient == PropKind.Flesh) container.flesh++;
            else if (ingredient == PropKind.BloodBottle) container.blood++;
            else return false;
            container.mixed = 0;
            Game?.Tell("В шейкере: кровь " + container.blood + ", глаза " + container.eyes + ", плоть " + container.flesh + ". Закройте хватом и встряхните.");
            return true;
        }

        public bool FinishMix(BarProp container)
        {
            int recipe = container.blood == 1 && container.eyes == 1 && container.flesh == 0 ? 0 :
                         container.blood == 1 && container.flesh == 1 && container.eyes == 0 ? 1 :
                         container.eyes == 1 && container.flesh == 1 && container.blood == 0 ? 2 : -1;
            if (recipe < 0) { Game?.Tell("Смесь испорчена. Переверните шейкер над раковиной справа."); return false; }
            container.recipe = recipe;
            container.quality = Game != null ? Game.PhysicalQuality : 50;
            Game?.Tell(Recipes[recipe] + " готов. Наклоните шейкер над стаканом.");
            return true;
        }

        public bool TransferDrink(BarProp source, BarProp glass)
        {
            if (source.recipe < 0 || glass.kind != PropKind.Glass || glass.recipe >= 0) return false;
            glass.recipe = source.recipe; glass.quality = source.quality;
            source.recipe = -1; source.blood = source.eyes = source.flesh = 0; source.mixed = 0;
            SetLiquid(glass, true);
            Game?.Tell("Поднесите стакан клиенту. Его заказ на карточке.");
            return true;
        }

        private void Update()
        {
            if (Game == null) return;
            stream.enabled = false;
            if (Time.time >= refillAt && !Game.GameOver) { refillAt = Time.time + 3; RefillTray(); }
            frameProps.Clear(); frameProps.AddRange(BarProp.All);
            selectedOffer = null;
            if (checkout != null)
                foreach (var candidate in frameProps)
                    if (candidate != null && candidate.kind == PropKind.Offer && !candidate.IsHeld &&
                        Vector3.Distance(candidate.transform.position, checkout.position) < 0.23f)
                    { selectedOffer = candidate; break; }
            foreach (var prop in frameProps)
            {
                if (prop == null || Time.time < prop.readyAt) continue;
                Vector3 previous = lastPositions.TryGetValue(prop, out var old) ? old : prop.transform.position;
                float distance = Vector3.Distance(previous, prop.transform.position);
                lastPositions[prop] = prop.transform.position;
                if (prop.kind == PropKind.Bell && prop.IsHeld && distance > 0.015f && Time.time >= bellCooldown)
                {
                    bellCooldown = Time.time + 2;
                    if (Game.GameOver) { Game.RestartRound(); ResetProps(); }
                    continue;
                }
                if (Game.GameOver) continue;
                if ((prop.kind == PropKind.Eye || prop.kind == PropKind.Flesh) && !prop.IsHeld && shaker != null &&
                    Vector3.Distance(prop.transform.position, shaker.transform.position + shaker.transform.up * 0.14f) < 0.12f && AddIngredient(shaker, prop.kind))
                { lastPositions.Remove(prop); Destroy(prop.gameObject); continue; }
                if (prop.kind == PropKind.BloodBottle || prop.kind == PropKind.Shaker) ProcessPour(prop);
                if (prop.kind == PropKind.Glass && prop.recipe >= 0)
                {
                    foreach (var patron in PhysicalPatron.All.ToArray())
                    {
                        if (patron == null || patron.Defeated || Vector3.Distance(prop.transform.position, patron.ReceivePosition) > 0.23f) continue;
                        Game.PhysicalServe(patron.id, prop.recipe, prop.quality);
                        prop.recipe = -1; SetLiquid(prop, false); prop.Release(Vector3.zero); prop.ResetHome(); break;
                    }
                }
                if (prop.kind == PropKind.Pump && prop.IsHeld)
                {
                    if (prop.transform.position.y < prop.home.y - 0.09f) pumpDown = true;
                    if (pumpDown && prop.transform.position.y > prop.home.y + 0.09f) { pumpDown = false; Game.PhysicalPump(); prop.Holder.Pulse(0.3f); }
                }
                if (prop.kind == PropKind.Rag && prop.IsHeld && Mathf.Abs(prop.transform.position.y - 1.23f) < 0.15f)
                { wipeDistance += distance; if (wipeDistance > 0.8f) { Game.PhysicalWipe(); wipeDistance = 0; } }
                if (prop.kind == PropKind.Cleaver && prop.IsHeld && distance > 0.01f)
                    foreach (var patron in PhysicalPatron.All.ToArray())
                        if (patron != null && patron.Defeated && Vector3.Distance(prop.transform.position, patron.ReceivePosition) < 0.45f) patron.Cut(distance);
                if ((prop.kind == PropKind.HelperArm || prop.kind == PropKind.HelperEye) && prop.IsHeld && Vector3.Distance(prop.transform.position, prop.home) > 0.2f)
                    TearHelper(prop);
                if (prop.kind == PropKind.Syringe && prop.IsHeld && Camera.main != null)
                {
                    bool touching = Vector3.Distance(prop.transform.position, Camera.main.transform.position - Vector3.up * 0.32f) < 0.23f;
                    injection = touching ? injection + Time.deltaTime : 0;
                    if (injection > 0.7f) { injection = 0; Game.PhysicalHeal(); prop.readyAt = Time.time + 2; }
                }
                if (checkout != null && !prop.IsHeld && Vector3.Distance(prop.transform.position, checkout.position) < 0.23f)
                {
                    if (prop.kind == PropKind.Coin && selectedOffer != null)
                    { Game.PhysicalPurchase(selectedOffer.offer); selectedOffer.ResetHome(); selectedOffer = null; prop.ResetHome(); }
                }
            }
        }

        private void ProcessPour(BarProp prop)
        {
            bool tilted = Vector3.Dot(prop.transform.up, Vector3.up) < 0.2f;
            if (!tilted) { prop.poured = false; return; }
            if (!prop.IsHeld || prop.poured) return;
            Vector3 mouth = prop.transform.position + prop.transform.up * (prop.kind == PropKind.BloodBottle ? 0.28f : 0.175f);
            stream.enabled = true; stream.SetPosition(0, mouth); stream.SetPosition(1, mouth + Vector3.down * 0.28f);
            foreach (var candidate in frameProps)
            {
                if (candidate == null || candidate == prop) continue;
                Vector3 delta = mouth - (candidate.transform.position + Vector3.up * 0.08f);
                if (new Vector2(delta.x, delta.z).magnitude > 0.13f || delta.y < 0 || delta.y > 0.4f) continue;
                if (prop.kind == PropKind.BloodBottle && candidate.kind == PropKind.Shaker && candidate.recipe < 0 &&
                    candidate.blood + candidate.eyes + candidate.flesh < 3 && Game.TakePhysicalStock(PropKind.BloodBottle))
                { AddIngredient(candidate, PropKind.BloodBottle); prop.poured = true; return; }
                if (prop.kind == PropKind.Shaker && TransferDrink(prop, candidate)) { prop.poured = true; return; }
            }
            if (prop.kind == PropKind.BloodBottle && Game.PhysicalExtinguish(mouth)) { prop.poured = true; return; }
            // A sink discards a bad mixture without creating free ingredients.
            if (prop.kind == PropKind.Shaker && Vector3.Distance(mouth, new Vector3(1.35f, 1.15f, -0.45f)) < 0.35f)
            { prop.blood = prop.eyes = prop.flesh = 0; prop.recipe = -1; prop.mixed = 0; prop.poured = true; Game.Tell("Шейкер промыт."); }
            if (prop.kind == PropKind.BloodBottle && helper != null && Vector3.Distance(mouth, helper.position + Vector3.up * 1.1f) < 0.35f)
            { Game.PhysicalDrawBlood(); prop.poured = true; }
        }

        private void TearHelper(BarProp prop)
        {
            bool eye = prop.kind == PropKind.HelperEye;
            SpawnIngredient(eye ? PropKind.Eye : PropKind.Flesh, prop.transform.position);
            prop.Release(Vector3.zero); prop.transform.position = prop.home;
            prop.readyAt = Time.time + (eye ? 12 : 16);
            if (prop.attachedLimb != null) prop.attachedLimb.localScale = Vector3.zero;
            StartCoroutine(Regrow(prop));
            BarAudio.Play("harvest"); Game.Tell(eye ? "Глаз вырван. Новый вырастет через 12 секунд." : "Конечность отделена. Регенерация — 16 секунд.");
        }
        private System.Collections.IEnumerator Regrow(BarProp prop)
        {
            var renderers = prop.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            while (prop != null && Time.time < prop.readyAt) yield return null;
            if (prop != null)
            {
                foreach (var r in renderers) r.enabled = true;
                if (prop.attachedLimb != null) prop.attachedLimb.localScale = Vector3.one;
            }
        }
        private static void SetLiquid(BarProp prop, bool on)
        {
            foreach (var renderer in prop.GetComponentsInChildren<Renderer>())
                if (renderer.name.ToLowerInvariant().Contains("liquid")) renderer.enabled = on;
        }
        public void ResetProps()
        {
            StopAllCoroutines();
            foreach (var prop in BarProp.All.ToArray())
            {
                if (prop.kind == PropKind.Eye || prop.kind == PropKind.Flesh) { Destroy(prop.gameObject); continue; }
                prop.ResetHome();
                if (prop.attachedLimb != null) prop.attachedLimb.localScale = Vector3.one;
                foreach (var renderer in prop.GetComponentsInChildren<Renderer>()) renderer.enabled = true;
                if (prop.kind == PropKind.Glass) SetLiquid(prop, false);
            }
            lastPositions.Clear(); selectedOffer = null; injection = wipeDistance = 0; pumpDown = false;
            refillAt = Time.time + 0.5f;
        }
        private void OnDestroy() { if (stream != null) { Destroy(stream.sharedMaterial); Destroy(stream.gameObject); } }
    }
}
