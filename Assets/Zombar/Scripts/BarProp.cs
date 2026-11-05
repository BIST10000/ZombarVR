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
            Holder = hand;
            Body.isKinematic = true;
            return true;
        }
        public void Release(Vector3 velocity)
        {
            if (Holder != null) Holder.Forget(this);
            Holder = null;
            if (Body == null) return;
            Body.isKinematic = anchored;
            if (!anchored) Body.linearVelocity = Vector3.ClampMagnitude(velocity, 5f);
        }
        public void ResetHome()
        {
            Release(Vector3.zero);
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
            Body.MovePosition(desired);
            if (!anchored) Body.MoveRotation(Holder.transform.rotation);
        }
        private void Update()
        {
            if (transform.position.y < -0.4f || Vector3.Distance(home, transform.position) > 7f) ResetHome();
            if (IsHeld) SampleShake(Holder.Velocity, Time.time);
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
