using System.Collections.Generic;
using UnityEngine;

namespace Zombar
{
    public sealed class PhysicalPatron : MonoBehaviour
    {
        public static readonly List<PhysicalPatron> All = new List<PhysicalPatron>();
        public string id;
        public bool giant;
        public bool Defeated { get; private set; }
        private Animator animator;
        private float harvest;
        private bool harvested;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }
        public Vector3 ReceivePosition => new Vector3(transform.position.x, 1.3f, 0.05f);
        public bool IntersectsStrike(Vector3 from, Vector3 to)
        {
            Vector3 head = transform.position + Vector3.up * (giant ? 1.95f : 1.55f) + Vector3.back * 0.18f;
            Vector3 segment = to - from;
            float t = segment.sqrMagnitude < 0.00001f ? 0 : Mathf.Clamp01(Vector3.Dot(head - from, segment) / segment.sqrMagnitude);
            return Vector3.Distance(from + segment * t, head) < 0.32f;
        }
        public void Punch() { ZombieBarGame.Instance?.PhysicalPunch(id); }
        public void SetAngry(bool angry) { if (animator != null) animator.SetBool("Angry", angry); }
        public void Defeat()
        {
            Defeated = true;
            if (animator != null) animator.SetBool("Dead", true);
        }
        public void Cut(float distance)
        {
            if (!Defeated || harvested) return;
            harvest += distance;
            if (harvest < 0.45f) return;
            harvested = true;
            var bar = PhysicalBar.Instance;
            ZombieBarGame.Instance?.PhysicalHarvest(id);
            if (bar != null) { bar.SpawnIngredient(PropKind.Flesh, ReceivePosition); bar.SpawnIngredient(PropKind.Eye, ReceivePosition + Vector3.right * 0.12f); }
        }
        private void Update()
        {
            if (Defeated)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation * Quaternion.Euler(-65, 0, 10), Time.deltaTime * 3f);
                transform.position = Vector3.Lerp(transform.position, new Vector3(originalPosition.x, 1.05f, 0.3f), Time.deltaTime * 2f);
            }
        }
    }
}
