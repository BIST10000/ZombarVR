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
        private CharacterController walker;
        public static XRNode DesktopHand { get; private set; } = XRNode.RightHand;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDesktopHand() { DesktopHand = XRNode.RightHand; }
        private void Awake() { view = GetComponentInChildren<Camera>(); pitch = view != null ? view.transform.localEulerAngles.x : 0; }
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
            if (keys.tabKey.wasPressedThisFrame)
                DesktopHand = DesktopHand == XRNode.RightHand ? XRNode.LeftHand : XRNode.RightHand;
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
            if (direction.sqrMagnitude > 0)
            {
                if (walker == null)
                {
                    walker = GetComponent<CharacterController>() ?? gameObject.AddComponent<CharacterController>();
                    walker.height = 1.6f;
                    walker.radius = .11f;
                    walker.center = Vector3.up * .83f;
                    walker.skinWidth = .012f;
                    walker.stepOffset = .04f;
                }
                walker.Move(direction.normalized * (Time.deltaTime * 1.4f));
            }
            var game = ZombieBarGame.Instance;
            if (game != null && game.Hud != null)
                game.Hud.SetHint((DesktopHand == XRNode.RightHand ? "Правая рука" : "Левая рука") +
                    " · Tab — сменить · ЛКМ — хват · колесо — дальность · F — наклон · пробел — трясти");
        }
    }
}

