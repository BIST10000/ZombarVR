using UnityEngine;

namespace Zombar
{
    public sealed class BarMixer : MonoBehaviour
    {
        public static readonly Vector3 ButtonPosition = new Vector3(.14f,1.26f,-.61f);
        private float finishesAt;
        private bool pressed;
        private TextMesh display;
        private GameObject eyeVisual, fleshVisual, liquid;
        private int lastCount;
        private string added;
        private float addedUntil;
        private BarHand[] hands;
        public bool Running => finishesAt > 0;

        private void Start()
        {
            hands = FindObjectsByType<BarHand>();
            var label = new GameObject("Состав в миксере");
            label.transform.SetParent(transform,false);
            label.transform.position = new Vector3(-.1f,1.65f,-.37f);
            display = label.AddComponent<TextMesh>();
            display.fontSize = 64;
            display.characterSize = .004f;
            display.anchor = TextAnchor.MiddleCenter;
            display.alignment = TextAlignment.Center;
            display.color = new Color(1,.9f,.65f);
            var cup = PhysicalBar.Instance.shaker.transform;
            eyeVisual = PhysicalBar.MakeVisual("Eye",cup,Vector3.one,Color.white);
            eyeVisual.transform.localPosition = new Vector3(-.018f,.217f,-.01f);

            fleshVisual = PhysicalBar.MakeVisual("Flesh",cup,Vector3.one,Color.white);
            fleshVisual.transform.localPosition = new Vector3(.006f,.205f,.009f);

            liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(liquid.GetComponent<Collider>());
            liquid.transform.SetParent(cup,false);
            liquid.transform.localPosition = Vector3.up * .2f;
            liquid.transform.localScale = new Vector3(.08f,.003f,.08f);
            liquid.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("PhysicalBar/BloodStream");
        }

        public bool Press()
        {
            var cup = PhysicalBar.Instance.shaker;
            if (ZombieBarGame.Instance.GameOver || Running || cup.IsHeld || Vector3.Distance(cup.transform.position,cup.home) > .16f || cup.recipe >= 0) return false;
            if (cup.blood + cup.eyes + cup.flesh < 2)
            {
                ZombieBarGame.Instance.Tell("Сначала добавьте ингредиенты по рецепту.");
                return false;
            }
            finishesAt = Time.time + 2.5f;
            cup.Body.isKinematic = true;
            cup.Body.position = cup.home;
            cup.Body.rotation = cup.homeRotation;
            cup.transform.SetPositionAndRotation(cup.home,cup.homeRotation);
            BarAudio.Play("brew");
            return true;
        }

        private void Update()
        {
            var cup = PhysicalBar.Instance.shaker;
            bool contact = false;
            foreach (var hand in hands)
                if (hand.Held == null && Vector3.Distance(hand.transform.position,ButtonPosition) < .075f)
                    contact = true;
            if (contact && !pressed) Press();
            pressed = contact;
            if (Running && (Time.time >= finishesAt || ZombieBarGame.Instance.GameOver || cup.blood + cup.eyes + cup.flesh == 0))
            {
                finishesAt = 0;
                cup.Body.isKinematic = false;
                if (!ZombieBarGame.Instance.GameOver && cup.blood + cup.eyes + cup.flesh > 0) PhysicalBar.Instance.FinishMix(cup);
            }
            display.text = Running ? "СМЕШИВАЕТСЯ…" : cup.recipe >= 0 ? "ГОТОВО · ПОДНЕСИТЕ БОКАЛ" :
                $"Кровь {cup.blood} · глаз {cup.eyes} · плоть {cup.flesh}\nКнопка справа — смешать";
            int count = cup.blood + cup.eyes + cup.flesh;
            if (count > lastCount) { added = "ИНГРЕДИЕНТ ДОБАВЛЕН"; addedUntil = Time.time + 1.5f; }
            lastCount = count;
            if (count == 0) { addedUntil = 0; display.text = ""; }
            if (Time.time < addedUntil) display.text = added + "\n" + display.text;
            eyeVisual.SetActive(cup.eyes > 0 && cup.recipe < 0);
            fleshVisual.SetActive(cup.flesh > 0 && cup.recipe < 0);
            liquid.SetActive(cup.blood > 0 || cup.recipe >= 0);
        }

        private void OnDisable()
        {
            finishesAt = 0;
            if (PhysicalBar.Instance != null && PhysicalBar.Instance.shaker != null && PhysicalBar.Instance.shaker.Body != null)
                PhysicalBar.Instance.shaker.Body.isKinematic = false;
        }
    }
}

