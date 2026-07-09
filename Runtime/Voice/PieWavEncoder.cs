using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Pie
{
    internal static class PieWavEncoder
    {
        internal static byte[] Encode(float[] samples, int channels, int sampleRate)
        {
            if (samples == null)
                samples = new float[0];
            if (channels <= 0)
                channels = 1;
            if (sampleRate <= 0)
                sampleRate = 16000;

            const int bitsPerSample = 16;
            var dataLength = samples.Length * sizeof(short);
            using (var stream = new MemoryStream(44 + dataLength))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * bitsPerSample / 8);
                writer.Write((short)(channels * bitsPerSample / 8));
                writer.Write((short)bitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);

                for (var i = 0; i < samples.Length; i++)
                {
                    var clamped = Mathf.Clamp(samples[i], -1.0f, 1.0f);
                    writer.Write((short)Mathf.RoundToInt(clamped * short.MaxValue));
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
