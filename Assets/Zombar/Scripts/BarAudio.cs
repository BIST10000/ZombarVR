using System.Collections.Generic;
using UnityEngine;

namespace Zombar
{
    public sealed class BarAudio : MonoBehaviour
    {
        public static BarAudio Instance { get; private set; }
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource source;

        private void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.volume = 0.28f;
            clips["coin"] = Tone(880f, 1320f, 0.12f, false);
            clips["brew"] = Tone(240f, 620f, 0.24f, false);
            clips["hit"] = Tone(105f, 48f, 0.18f, true);
            clips["hurt"] = Tone(180f, 55f, 0.38f, true);
            clips["harvest"] = Tone(150f, 90f, 0.2f, true);
            clips["alarm"] = Tone(520f, 390f, 0.45f, false);
            clips["buy"] = Tone(420f, 980f, 0.25f, false);
        }

        public static void Play(string cue)
        {
            if (Instance == null || !Instance.clips.ContainsKey(cue)) return;
            Instance.source.pitch = Random.Range(0.96f, 1.04f);
            Instance.source.PlayOneShot(Instance.clips[cue]);
        }

        private static AudioClip Tone(float from, float to, float seconds, bool noisy)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * seconds);
            float[] samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(from, to, t);
                phase += frequency * Mathf.PI * 2f / sampleRate;
                float envelope = Mathf.Sin(t * Mathf.PI) * (1f - t);
                float wave = Mathf.Sin(phase) * 0.72f + Mathf.Sin(phase * 2.01f) * 0.18f;
                if (noisy) wave = wave * 0.55f + Random.Range(-1f, 1f) * 0.45f;
                samples[i] = wave * envelope * 0.5f;
            }
            AudioClip clip = AudioClip.Create("Zombar cue", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
