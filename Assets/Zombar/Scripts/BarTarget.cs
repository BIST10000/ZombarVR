using UnityEngine;

namespace Zombar
{
    public enum BarAction
    {
        BrewBloodFlip,
        BrewRotSour,
        BrewEyeHighball,
        HarvestArm,
        HarvestEye,
        DrawBlood,
        RestoreBody,
        ServePatron,
        PunchPatron,
        HarvestCorpse,
        ExtinguishFire,
        PumpFlood,
        UpgradeQuality,
        UpgradeStrength,
        BuyIceBox,
        BuySprinkler,
        BuyDrain,
        CleanBar,
        TradeOffer,
        DismissTrader
    }

    public sealed class BarTarget : MonoBehaviour
    {
        public BarAction action;
        public string subjectId;
        public string displayLabel;

        private Renderer targetRenderer;
        private Material targetMaterial;
        private Vector3 restingScale;
        private bool focused;

        private void Awake()
        {
            restingScale = transform.localScale;
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
                targetMaterial = targetRenderer.material;
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            transform.localScale = restingScale * (focused ? 1.07f : 1f);
            if (targetMaterial == null) return;
            Color color = focused ? new Color(0.35f, 1f, 0.72f, 1f) : Color.black;
            if (targetMaterial.HasProperty("_EmissionColor"))
            {
                targetMaterial.EnableKeyword("_EMISSION");
                targetMaterial.SetColor("_EmissionColor", color * (focused ? 0.35f : 0f));
            }
        }

        public void Interact()
        {
            ZombieBarGame game = ZombieBarGame.Instance;
            if (game != null)
                game.Interact(this);
        }
    }
}
