using UnityEngine;

namespace Zombar
{
    public sealed class BarActorVisual : MonoBehaviour
    {
        public bool angry;
        public bool defeated;
        public float motion = 1f;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float phase;
        private Transform leftArm;
        private Transform rightArm;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            phase = Random.Range(0f, 8f);
            Transform[] children = GetComponentsInChildren<Transform>();
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == "Левая рука") leftArm = children[i];
                if (children[i].name == "Правая рука") rightArm = children[i];
            }
        }

        private void Update()
        {
            if (defeated)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation * Quaternion.Euler(74f, 0f, 12f), Time.deltaTime * 4f);
                return;
            }

            float t = Time.time * (angry ? 5.5f : 1.6f) + phase;
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(t) * 0.018f * motion);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(t * 0.43f) * 2.5f, angry ? Mathf.Sin(t) * 4f : Mathf.Sin(t * 0.7f) * 1.2f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(angry ? Mathf.Sin(t) * 35f : 0f, 0f, 18f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(angry ? -Mathf.Sin(t) * 35f : 0f, 0f, -18f);
        }
    }
}
