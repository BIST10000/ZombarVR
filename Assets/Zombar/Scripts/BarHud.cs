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
        private UnityEngine.UI.Text ticketTitle, ticketRecipe, ticketAction, ticketStation;
        private RectTransform ticket;
        private UnityEngine.UI.Text ticketStages;

        public void Initialize(Camera camera)
        {
            var root = new GameObject("Интерфейс бармена", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(camera.transform, false);
            root.transform.localPosition = new Vector3(0, 0, .85f);
            root.transform.localScale = Vector3.one * .00080f;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 650);
            status = Label(root.transform, "Смена", new Vector2(0, 240), new Vector2(960, 175), 23);
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 16;
            status.resizeTextMaxSize = 23;
            var ledger = new GameObject("Счёт и запасы на стене", typeof(RectTransform), typeof(Canvas));
            ledger.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ledger.GetComponent<Canvas>().worldCamera = camera;
            ledger.transform.position = new Vector3(0, 2.5f, 1.6f);
            ledger.transform.localScale = Vector3.one * 0.0015f;
            var panel = status.transform.parent.GetComponent<RectTransform>();
            panel.SetParent(ledger.transform, false);
            panel.anchoredPosition = Vector2.zero;
            var slip = new GameObject("Чек заказа", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            ticket = slip.GetComponent<RectTransform>();
            ticket.SetParent(root.transform, false);
            ticket.anchoredPosition = new Vector2(-555, 376);
            ticket.sizeDelta = new Vector2(500, 246);
            var paper = slip.GetComponent<UnityEngine.UI.RawImage>();
            paper.texture = Resources.Load<Texture2D>("PhysicalBar/OrderSlip");
            paper.raycastTarget = false;
            var ink = new Color32(246, 237, 215, 255);
            var stamp = new Color32(236, 192, 122, 255);
            ticketStation = SlipLine("Место подачи", 8, 27, 19, stamp, FontStyle.Bold);
            ticketTitle = SlipLine("Коктейль", 45, 49, 40, ink, FontStyle.Bold);
            ticketRecipe = SlipLine("Состав", 100, 34, 27, ink, FontStyle.Normal);
            ticketAction = SlipLine("Следующее действие", 176, 59, 24, stamp, FontStyle.Bold);
            ticketStages = SlipLine("Стадия заказа", 146, 26, 18, ink, FontStyle.Bold);
            message = Label(root.transform, "Событие", new Vector2(0, -360), new Vector2(770, 83), 25);
            message.color = new Color(1f, 0.82f, 0.46f);
            hint = Label(root.transform, "Действие", new Vector2(0, -444), new Vector2(870, 62), 21);
        }

        private UnityEngine.UI.Text SlipLine(string name, int top, int height, int size, Color tint, FontStyle style)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(ticket, false);
            rect.anchorMin = new Vector2(0,1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f,1);
            rect.sizeDelta = new Vector2(-42,height);
            rect.anchoredPosition = new Vector2(0,-top);
            var line = obj.GetComponent<UnityEngine.UI.Text>();
            line.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            line.fontSize = size;
            line.fontStyle = style;
            line.color = tint;
            line.alignment = TextAnchor.MiddleLeft;
            line.raycastTarget = false;
            line.resizeTextForBestFit = true;
            line.resizeTextMinSize = size - 5;
            line.resizeTextMaxSize = size;
            return line;
        }

        private static UnityEngine.UI.Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(.065f,.057f,.049f,.94f);
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

        public void SetWorkflow(string station, string title, string recipe, string action, int stage)
        {
            if (ticket == null) return;
            ticketStation.text = station;
            ticketTitle.text = title;
            ticketRecipe.text = recipe;
            ticketAction.text = action;
            const string muted = "#A8B4B0";
            const string active = "#FFE0A1";
            ticketStages.text = $"<color={(stage==0?active:muted)}>Состав</color>   ›   <color={(stage==1?active:muted)}>Миксер</color>   ›   <color={(stage==2?active:muted)}>Подача</color>";
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
            if (hint != null) hint.transform.parent.gameObject.SetActive(Time.timeSinceLevelLoad < 12f);
            if (message != null) message.transform.parent.gameObject.SetActive(Time.unscaledTime < messageUntil);
        }
    }
}

