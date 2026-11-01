using System.Collections;
using UnityEngine;

namespace Zombar
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class BarLabelLayout : MonoBehaviour
    {
        public float width = 2f;
        public float height = 0.28f;

        private IEnumerator Start()
        {
            // TextMesh has generated its glyph mesh after the first rendered frame.
            transform.rotation = Quaternion.identity;
            yield return null;
            yield return null;
            Fit();
        }

        public void Fit()
        {
            var size = GetComponent<Renderer>().bounds.size;
            if (size.x < 0.0001f || size.y < 0.0001f) return;
            transform.localScale *= Mathf.Min(width / size.x, height / size.y);
        }
    }
}
