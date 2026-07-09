using UnityEngine;

namespace Pie
{
    internal static class PieVoiceRecorder
    {
        internal static float[] ExtractSamples(AudioClip clip, int frameCount)
        {
            if (clip == null || frameCount <= 0)
                return new float[0];

            var clampedFrames = Mathf.Clamp(frameCount, 0, clip.samples);
            var samples = new float[clampedFrames * clip.channels];
            if (samples.Length == 0)
                return samples;

            clip.GetData(samples, 0);
            return samples;
        }

        internal static bool IsSilent(float[] samples, float rmsThreshold)
        {
            return CalculateRms(samples) < Mathf.Max(0.0f, rmsThreshold);
        }

        internal static float CalculateRms(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0.0f;

            double sum = 0.0;
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                sum += sample * sample;
            }

            return Mathf.Sqrt((float)(sum / samples.Length));
        }
    }
}
