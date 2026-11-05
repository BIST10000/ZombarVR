using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Zombar
{
    public sealed class BarHand : MonoBehaviour
    {
        public XRNode node;
        public Transform trackingSpace;
        public Camera view;
        public Vector3 Velocity { get; private set; }
        public BarProp Held { get; private set; }
        private Vector3 previous;
        private bool gripWasDown;
        private bool trackedLastFrame;
        private float strikeAt;
        private Renderer[] handRenderers;
        private readonly Collider[] overlaps = new Collider[32];
        private float desktopDistance = 0.6f;
        private Quaternion desktopTurn = Quaternion.identity;

        public void Initialize(XRNode handNode, Transform space, Camera camera)
        {
            node = handNode; trackingSpace = space; view = camera;
            var mesh = PhysicalBar.MakeVisual("Hand", transform, new Vector3(0.085f, 0.055f, 0.13f), new Color(0.32f, 0.27f, 0.22f));
            handRenderers = mesh.GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            bool grip = false;
            bool tracked = false;
            bool disabled = ZombieBarGame.Instance != null && !ZombieBarGame.Instance.GameOver &&
                (node == XRNode.LeftHand ? ZombieBarGame.Instance.LeftArmDisabled : ZombieBarGame.Instance.RightArmDisabled);
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                tracked = !device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) || isTracked;
                transform.SetPositionAndRotation(trackingSpace.TransformPoint(pos), trackingSpace.rotation * rot);
                grip = device.TryGetFeatureValue(CommonUsages.gripButton, out bool down) && down;
            }
            else if (!XRSettings.isDeviceActive && node == XRNode.RightHand && view != null)
            {
                tracked = true;
                var mouse = Mouse.current;
                var keyboard = Keyboard.current;
                if (mouse != null) desktopDistance = Mathf.Clamp(desktopDistance + mouse.scroll.ReadValue().y * 0.001f, 0.25f, 1.5f);
                desktopTurn = Quaternion.Slerp(desktopTurn, keyboard != null && keyboard.fKey.isPressed ? Quaternion.Euler(0, 0, 115) : Quaternion.identity, Time.deltaTime * 10);
                Vector3 shake = keyboard != null && keyboard.spaceKey.isPressed ? Vector3.up * (Mathf.Sin(Time.time * 20) * 0.14f) : Vector3.zero;
                transform.SetPositionAndRotation(view.transform.position + view.transform.forward * desktopDistance + shake,
                    view.transform.rotation * desktopTurn);
                grip = mouse != null && mouse.leftButton.isPressed;
            }
            if (handRenderers != null)
                foreach (var renderer in handRenderers) renderer.enabled = tracked && !disabled;
            if (!tracked || disabled)
            {
                if (Held != null) Held.Release(Vector3.zero);
                trackedLastFrame = false; gripWasDown = false; return;
            }
            Velocity = trackedLastFrame ? Vector3.ClampMagnitude((transform.position - previous) / Mathf.Max(Time.unscaledDeltaTime, 0.001f), 8f) : Vector3.zero;
            Vector3 from = trackedLastFrame ? previous : transform.position;
            previous = transform.position;
            trackedLastFrame = true;
            if (grip && !gripWasDown) TryGrab();
            if (!grip && gripWasDown && Held != null) Held.Release(Velocity);
            if (grip && Held == null && Velocity.magnitude >= 1.1f && Time.time >= strikeAt)
            {
                foreach (var patron in PhysicalPatron.All)
                    if (patron != null && !patron.Defeated && patron.IntersectsStrike(from, transform.position))
                    { patron.Punch(); strikeAt = Time.time + 0.45f; Pulse(0.7f); break; }
            }
            gripWasDown = grip;
        }

        public bool TryGrab()
        {
            BarProp nearest = null;
            float distance = 0.19f;
            int count = Physics.OverlapSphereNonAlloc(transform.position, distance, overlaps, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var prop = overlaps[i].GetComponentInParent<BarProp>();
                if (prop == null || prop.IsHeld || Time.time < prop.readyAt) continue;
                if (ZombieBarGame.Instance != null && ZombieBarGame.Instance.GameOver && prop.kind != PropKind.Bell) continue;
                float candidate = Vector3.Distance(transform.position, overlaps[i].ClosestPoint(transform.position));
                if (candidate < distance) { nearest = prop; distance = candidate; }
            }
            if (nearest == null || !nearest.Grab(this)) return false;
            Held = nearest;
            Pulse(0.2f);
            return true;
        }
        public void Forget(BarProp prop) { if (Held == prop) Held = null; }
        public void Pulse(float strength)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) device.SendHapticImpulse(0, strength, 0.07f);
        }
    }
}
