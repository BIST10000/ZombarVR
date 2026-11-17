using System.Collections.Generic;
using UnityEngine;

namespace Zombar
{
    public enum PropKind { BloodBottle, Eye, Flesh, Shaker, Glass, Cleaver, Rag, Pump, Syringe, Coin, Offer, Bell, HelperArm, HelperEye }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class BarProp : MonoBehaviour
    {
        public static readonly List<BarProp> All = new List<BarProp>();
        public PropKind kind;
        public BarAction offer;
        public string label;
        public bool anchored;
        public Transform attachedLimb;
        public Vector3 gripOffset;
        public Vector3 home;
        public Quaternion homeRotation;
        public BarHand Holder { get; private set; }
        public Rigidbody Body { get; private set; }
        public int blood, eyes, flesh, recipe = -1, quality;
        public float mixed, readyAt;
        public bool poured;
        public bool IsHeld => Holder != null;
        private Vector3 previousVelocity;
        private float shakeGate;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            home = transform.position;
            homeRotation = transform.rotation;
        }
        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { Release(Vector3.zero); All.Remove(this); }

        public bool Grab(BarHand hand)
        {
            if (IsHeld || Time.time < readyAt) return false;
            if (kind == PropKind.Shaker && PhysicalBar.Instance.GetComponent<BarMixer>()?.Running == true) return false;
            Holder = hand;
            Body.isKinematic = anchored;
            if (!anchored) Body.useGravity = false;
            return true;
        }
        public void Release(Vector3 velocity)
        {
            if (Holder != null) Holder.Forget(this);
            Holder = null;
            if (Body == null) return;
            Body.isKinematic = anchored;
            Body.useGravity = !anchored;
            if (!anchored) Body.linearVelocity = Vector3.ClampMagnitude(velocity, 5f);
        }
        public void ResetHome()
        {
            Release(Vector3.zero);
            // Keep the Transform and interpolated body in agreement after a handoff/teleport.
            // A pending Transform write must not restore the served position on the next physics sync.
            transform.SetPositionAndRotation(home, homeRotation);
            Body.position = home;
            Body.rotation = homeRotation;
            if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
            readyAt = 0;
            blood = eyes = flesh = 0;
            recipe = -1; mixed = 0; poured = false;
            previousVelocity = Vector3.zero; shakeGate = 0;
        }
        private void FixedUpdate()
        {
            if (!IsHeld) return;
            Vector3 desired = Holder.transform.TransformPoint(gripOffset);
            if (kind == PropKind.Pump) desired = new Vector3(home.x, Mathf.Clamp(desired.y, home.y - 0.18f, home.y + 0.18f), home.z);
            if (anchored) { Body.MovePosition(desired); return; }
            // Drive a dynamic body so the solver can stop the grip against solid surfaces.
            Body.linearVelocity = Vector3.ClampMagnitude((desired - Body.position) * 28f, 5f);
            Quaternion delta = Holder.transform.rotation * Quaternion.Inverse(Body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Body.angularVelocity = Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * 24f), 12f);
        }
        private void Update()
        {
            if (transform.position.y < -0.4f || Vector3.Distance(home, transform.position) > 7f) ResetHome();
        }
        public void SampleShake(Vector3 velocity, float time)
        {
            if (kind != PropKind.Shaker || !IsHeld || blood + eyes + flesh < 2 || recipe >= 0) return;
            if (velocity.magnitude > 0.65f && previousVelocity.magnitude > 0.65f &&
                Vector3.Dot(velocity.normalized, previousVelocity.normalized) < -0.35f && time > shakeGate)
            {
                mixed += 1;
                shakeGate = time + 0.14f;
                Holder.Pulse(0.15f);
                BarAudio.Play("brew");
                if (mixed >= 5) PhysicalBar.Instance?.FinishMix(this);
            }
            if (velocity.magnitude > 0.65f) previousVelocity = velocity;
        }
    }
}

