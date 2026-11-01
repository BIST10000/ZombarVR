using UnityEngine;

namespace Zombar
{
    // World-space UI is visible in both stereo eyes and in the desktop preview.
    public sealed class BarHud : MonoBehaviour
    {
        private UnityEngine.UI.Text status;
        private UnityEngine.UI.Text message;
        private UnityEngine.UI.Text hint;
        private float messageUntil;

        public void Initialize(Camera camera)
        {
            var root = new GameObject("Интерфейс бармена", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(camera.transform, false);
            root.transform.localPosition = new Vector3(0, 0, 1.6f);
            root.transform.localScale = Vector3.one * 0.0015f;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 650);
            status = Label(root.transform, "Смена", new Vector2(0, 240), new Vector2(960, 175), 23);
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 16;
            status.resizeTextMaxSize = 23;
            message = Label(root.transform, "Событие", new Vector2(0, -180), new Vector2(900, 70), 25);
            message.color = new Color(1f, 0.82f, 0.46f);
            hint = Label(root.transform, "Действие", new Vector2(0, -255), new Vector2(960, 65), 21);
        }

        private static UnityEngine.UI.Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.025f, 0.035f, 0.055f, 0.87f);
            background.raycastTarget = false;
            var child = new GameObject("Текст", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            child.transform.SetParent(panel.transform, false);
            var textRect = child.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 8);
            textRect.offsetMax = new Vector2(-12, -8);
            var text = child.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.8f, 1f, 0.92f);
            text.raycastTarget = false;
            return text;
        }

        public void SetStatus(string value) { if (status != null) status.text = value; }
        public void SetHint(string value) { if (hint != null) hint.text = value; }
        public void Notify(string value)
        {
            if (message == null) return;
            message.text = value;
            messageUntil = Time.unscaledTime + 5f;
        }
        private void Update()
        {
            if (message != null) message.transform.parent.gameObject.SetActive(Time.unscaledTime < messageUntil);
        }
    }
}
