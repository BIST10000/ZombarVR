using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Zombar
{
    // Gameplay uses physical hands; this component only supplies a desktop rehearsal mode.
    public sealed class BarPlayerInput : MonoBehaviour
    {
        private Camera view;
        private float pitch;
        private void Awake() { view = GetComponentInChildren<Camera>(); }
        private void Update()
        {
            if (XRSettings.isDeviceActive || view == null) return;
            if (view.transform.parent != null && view.transform.parent != transform)
            {
                var offset = view.transform.parent;
                offset.position += Vector3.up * (transform.position.y + 1.64f - view.transform.position.y);
            }
            if (Keyboard.current == null) return;
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 look = mouse.delta.ReadValue();
                transform.Rotate(0, look.x * 0.12f, 0);
                pitch = Mathf.Clamp(pitch - look.y * 0.12f, -80, 80);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            Vector3 direction = Vector3.zero;
            if (keys.wKey.isPressed) direction += view.transform.forward;
            if (keys.sKey.isPressed) direction -= view.transform.forward;
            if (keys.dKey.isPressed) direction += view.transform.right;
            if (keys.aKey.isPressed) direction -= view.transform.right;
            direction.y = 0;
            if (direction.sqrMagnitude > 0) transform.position += direction.normalized * (Time.deltaTime * 1.4f);
            var game = ZombieBarGame.Instance;
            if (game != null && game.Hud != null)
                game.Hud.SetHint("ЛКМ — удерживать предмет · колесо — рука ближе/дальше · F — наклон · пробел — встряхнуть");
        }
    }
}
