using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Keyboard = UnityEngine.InputSystem.Keyboard;
using Mouse = UnityEngine.InputSystem.Mouse;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace Zombar
{
    public sealed class BarPlayerInput : MonoBehaviour
    {
        private readonly List<XRInputDevice> devices = new List<XRInputDevice>();
        private bool leftTriggerWasDown;
        private bool rightTriggerWasDown;
        private Camera playerCamera;
        private Transform rigRoot;
        private LineRenderer leftRay;
        private LineRenderer rightRay;
        private Material rayMaterial;
        private Transform trackingSpace;
        private bool restartWasDown;
        private BarTarget focusedTarget;
        private string interactionHint = "Наведитесь на предмет и нажмите E / курок контроллера";
        private float flatLookPitch;

        private void Awake()
        {
            rigRoot = transform;
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
            trackingSpace = playerCamera != null ? playerCamera.transform.parent : rigRoot;
            if (trackingSpace == null) trackingSpace = rigRoot;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            rayMaterial = new Material(shader);
            leftRay = CreateRayRenderer("Луч контроллера · левый");
            rightRay = CreateRayRenderer("Луч контроллера · правый");
        }

        private void Update()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerCamera == null)
                return;

            if (Keyboard.current != null)
            {
                if (Mouse.current != null && !XRSettings.isDeviceActive && Mouse.current.rightButton.isPressed)
                {
                    Vector2 look = Mouse.current.delta.ReadValue();
                    rigRoot.Rotate(0f, look.x * 0.12f, 0f, Space.World);
                    flatLookPitch = Mathf.Clamp(flatLookPitch - look.y * 0.12f, -78f, 78f);
                    playerCamera.transform.localRotation = Quaternion.Euler(flatLookPitch, 0f, 0f);
                }

                float moveSpeed = 2.1f * Time.deltaTime;
                Vector3 move = Vector3.zero;
                if (Keyboard.current.wKey.isPressed) move += playerCamera.transform.forward;
                if (Keyboard.current.sKey.isPressed) move -= playerCamera.transform.forward;
                if (Keyboard.current.aKey.isPressed) move -= playerCamera.transform.right;
                if (Keyboard.current.dKey.isPressed) move += playerCamera.transform.right;
                move.y = 0f;
                if (move.sqrMagnitude > 0.001f)
                    rigRoot.position += move.normalized * moveSpeed;

                if (Keyboard.current.eKey.wasPressedThisFrame ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                    InteractFrom(playerCamera.transform.position, playerCamera.transform.forward);

                if (Keyboard.current.qKey.wasPressedThisFrame)
                    InteractWithNearestCounterTarget();
            }

            ZombieBarGame game = ZombieBarGame.Instance;
            // Restart must work even after both gameplay hands have been disabled.
            bool restartDown = false;
            foreach (XRNode node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var device = InputDevices.GetDeviceAtXRNode(node);
                restartDown |= device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool primary) && primary;
            }
            if (game != null && game.GameOver && restartDown && !restartWasDown) game.RestartRound();
            restartWasDown = restartDown;
            if (!XRSettings.isDeviceActive)
                FocusFrom(playerCamera.transform.position, playerCamera.transform.forward);
            if (game == null || !game.LeftArmDisabled)
                PollController(XRNode.LeftHand, ref leftTriggerWasDown, leftRay);
            else if (leftRay != null)
                leftRay.enabled = false;
            if (game == null || !game.RightArmDisabled)
                PollController(XRNode.RightHand, ref rightTriggerWasDown, rightRay);
            else if (rightRay != null)
                rightRay.enabled = false;
            if (game != null && game.Hud != null) game.Hud.SetHint(interactionHint);
        }

        private LineRenderer CreateRayRenderer(string objectName)
        {
            GameObject rayObject = new GameObject(objectName);
            rayObject.transform.SetParent(rigRoot, false);
            LineRenderer ray = rayObject.AddComponent<LineRenderer>();
            ray.useWorldSpace = true;
            ray.positionCount = 2;
            ray.startWidth = 0.008f;
            ray.endWidth = 0.003f;
            ray.numCapVertices = 2;
            ray.sharedMaterial = rayMaterial;
            ray.startColor = new Color(0.55f, 1f, 0.82f, 0.92f);
            ray.endColor = new Color(0.55f, 1f, 0.82f, 0.18f);
            ray.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ray.receiveShadows = false;
            ray.enabled = false;
            return ray;
        }

        private void PollController(XRNode node, ref bool wasDown, LineRenderer ray)
        {
            InputDevices.GetDevicesAtXRNode(node, devices);
            bool triggerDown = false;
            bool hasController = false;
            for (int i = 0; i < devices.Count; i++)
            {
                XRInputDevice device = devices[i];
                bool down = device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool trigger) && trigger;
                triggerDown |= down;
                if (device.isValid &&
                    device.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 localPosition) &&
                    device.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion localRotation))
                {
                    hasController = true;
                    Vector3 worldPosition = trackingSpace.TransformPoint(localPosition);
                    Quaternion worldRotation = trackingSpace.rotation * localRotation;
                    Vector3 direction = worldRotation * Vector3.forward;
                    RaycastHit hit;
                    bool didHit = Physics.Raycast(worldPosition, direction, out hit, 10f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                    if (ray != null)
                    {
                        ray.enabled = true;
                        ray.SetPosition(0, worldPosition);
                        ray.SetPosition(1, didHit ? hit.point : worldPosition + direction * 10f);
                    }

                    if (down && !wasDown)
                    {
                        InteractFrom(worldPosition, direction);
                        if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                            device.SendHapticImpulse(0, 0.25f, 0.06f);
                    }
                    if (node == XRNode.RightHand || (ZombieBarGame.Instance != null && ZombieBarGame.Instance.RightArmDisabled))
                        FocusFrom(worldPosition, direction);
                }
            }

            if (!hasController && ray != null) ray.enabled = false;
            wasDown = hasController && triggerDown;
        }

        private void InteractFrom(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                BarTarget target = hit.collider.GetComponentInParent<BarTarget>();
                if (target != null)
                {
                    target.Interact();
                    return;
                }
            }

            interactionHint = "Здесь нет кнопки. Посмотрите на стойку или клиента.";
        }

        private void FocusFrom(Vector3 origin, Vector3 direction)
        {
            BarTarget target = null;
            if (Physics.Raycast(origin, direction, out var hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<BarTarget>();
            if (focusedTarget != target)
            {
                if (focusedTarget != null) focusedTarget.SetFocused(false);
                focusedTarget = target;
                if (focusedTarget != null) focusedTarget.SetFocused(true);
            }
            interactionHint = target != null ? target.displayLabel + " · курок / E" : "Наведите луч на станцию или клиента · курок / E";
        }

        private void InteractWithNearestCounterTarget()
        {
            BarTarget[] targets = FindObjectsByType<BarTarget>();
            BarTarget nearest = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].action != BarAction.BrewBloodFlip &&
                    targets[i].action != BarAction.BrewRotSour &&
                    targets[i].action != BarAction.BrewEyeHighball &&
                    targets[i].action != BarAction.CleanBar &&
                    targets[i].action != BarAction.HarvestArm &&
                    targets[i].action != BarAction.HarvestEye &&
                    targets[i].action != BarAction.DrawBlood)
                    continue;

                float distance = Vector3.Distance(playerCamera.transform.position, targets[i].transform.position);
                if (distance < bestDistance)
                {
                    nearest = targets[i];
                    bestDistance = distance;
                }
            }

            if (nearest != null)
                nearest.Interact();
        }

        private void OnDestroy()
        {
            if (rayMaterial != null) Destroy(rayMaterial);
        }
    }
}
