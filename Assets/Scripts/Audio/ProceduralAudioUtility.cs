using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Builds short looping AudioClips at runtime when baked assets are missing.
    /// </summary>
    public static class ProceduralAudioUtility
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateOspreyEngineLoop()
        {
            const float duration = 4f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float wobble = Mathf.Sin(t * 5.8f * Mathf.PI * 2f) * 0.1f;
                float rumble = Mathf.Sin(t * 54f * Mathf.PI * 2f) * (0.34f + wobble);
                float rotor = Mathf.Sin(t * 118f * Mathf.PI * 2f + Mathf.Sin(t * 3.2f) * 1.4f) * 0.2f;
                float whine = Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.04f;
                float noise = HashNoise(i, 11) * 0.05f;
                samples[i] = Mathf.Clamp(rumble + rotor + whine + noise, -1f, 1f) * 0.5f;
            }

            return CreateClip("OspreyEngineLoop", samples, loop: true);
        }

        public static AudioClip CreateOspreyBoostLayer()
        {
            const float duration = 2.5f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float core = Mathf.Sin(t * 280f * Mathf.PI * 2f) * 0.22f;
                float grit = HashNoise(i, 29) * 0.12f;
                float sweep = Mathf.Sin(t * 12f * Mathf.PI * 2f) * 0.08f;
                samples[i] = Mathf.Clamp(core + grit + sweep, -1f, 1f) * 0.45f;
            }

            return CreateClip("OspreyBoostLayer", samples, loop: true);
        }

        public static AudioClip CreateThrusterPuff()
        {
            const float duration = 0.35f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float env = (1f - t) * (1f - t);
                float burst = HashNoise(i, 7) * env;
                float tone = Mathf.Sin(t * 420f * Mathf.PI * 2f) * env * 0.18f;
                samples[i] = Mathf.Clamp(burst + tone, -1f, 1f) * 0.65f;
            }

            return CreateClip("ThrusterPuff", samples, loop: false);
        }

        public static AudioClip CreateSpaceHumLoop()
        {
            const float duration = 6f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float hum = Mathf.Sin(t * 38f * Mathf.PI * 2f) * 0.28f;
                float sub = Mathf.Sin(t * 19f * Mathf.PI * 2f) * 0.18f;
                float drift = Mathf.Sin(t * 0.08f * Mathf.PI * 2f) * 0.06f;
                samples[i] = Mathf.Clamp(hum + sub + drift, -1f, 1f) * 0.42f;
            }

            return CreateClip("SpaceHumLoop", samples, loop: true);
        }

        public static AudioClip CreateSpaceWindLoop()
        {
            const float duration = 5f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float env = 0.55f + Mathf.Sin(t * 0.35f * Mathf.PI * 2f) * 0.2f;
                float noise = HashNoise(i, 3) * env;
                float airy = Mathf.Sin(t * 900f * Mathf.PI * 2f + HashNoise(i, 5) * 2f) * 0.015f;
                samples[i] = Mathf.Clamp(noise + airy, -1f, 1f) * 0.35f;
            }

            return CreateClip("SpaceWindLoop", samples, loop: true);
        }

        public static AudioClip CreateSpaceChime()
        {
            const float duration = 2.2f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float env = Mathf.Exp(-4.5f * t);
                float tone = Mathf.Sin(t * 880f * Mathf.PI * 2f) * env * 0.12f;
                float shimmer = Mathf.Sin(t * 1320f * Mathf.PI * 2f) * env * 0.05f;
                samples[i] = tone + shimmer;
            }

            return CreateClip("SpaceChime", samples, loop: false);
        }

        private static float HashNoise(int index, int seed)
        {
            float x = Mathf.Sin((index + seed * 131) * 12.9898f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        private static AudioClip CreateClip(string name, float[] samples, bool loop)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
