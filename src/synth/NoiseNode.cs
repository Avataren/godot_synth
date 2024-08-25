using System;
using System.Runtime.CompilerServices;
namespace Synth
{
    public class NoiseNode : AudioNode
    {
        private uint x, y, z, w;
        private NoiseType currentNoiseType;
        private const int PinkNoiseMaxOctaves = 5;
        private float[] pinkNoiseValues;
        private int pinkNoiseIndex;
        private float amplitude = 1.0f;
        private float dcOffset = 0.0f;
        private const int seed = 123;

        public NoiseNode() : base()
        {
            currentNoiseType = NoiseType.White;
            SetSeed(seed);
            pinkNoiseValues = new float[PinkNoiseMaxOctaves];
            pinkNoiseIndex = 0;
        }

        public void SetSeed(int seed)
        {
            x = (uint)seed;
            y = 362436069;
            z = 521288629;
            w = 88675123;
        }

        public void SetAmplitude(float newAmplitude)
        {
            amplitude = Math.Clamp(newAmplitude, 0f, 1f);
        }

        public void SetDCOffset(float newOffset)
        {
            dcOffset = Math.Clamp(newOffset, -1f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Next()
        {
            uint t = x ^ (x << 11);
            x = y; y = z; z = w;
            return w = w ^ (w >> 19) ^ (t ^ (t >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetWhiteNoise()
        {
            return Next() / (float)uint.MaxValue * 2 - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetPinkNoise()
        {
            float white = GetWhiteNoise();
            float pink = 0;

            pinkNoiseIndex = (pinkNoiseIndex + 1) % PinkNoiseMaxOctaves;
            pinkNoiseValues[pinkNoiseIndex] = white;

            for (int i = 0; i < PinkNoiseMaxOctaves; i++)
            {
                pink += pinkNoiseValues[(pinkNoiseIndex - i + PinkNoiseMaxOctaves) % PinkNoiseMaxOctaves];
            }

            return pink / PinkNoiseMaxOctaves;
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            currentNoiseType = noiseType;
        }

        public override void Process(double increment)
        {
            int bufferSize = buffer.Length;

            if (currentNoiseType == NoiseType.White)
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = GetWhiteNoise() * amplitude + dcOffset;
                }
            }
            else // Pink noise
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = GetPinkNoise() * amplitude + dcOffset;
                }
            }

            // ApplyEnvelope(buffer);
        }

        private void ApplyEnvelope(Span<float> samples)
        {
            // Simple linear fade in/out to avoid clicks
            int fadeLength = Math.Min(100, samples.Length / 2);
            for (int i = 0; i < fadeLength; i++)
            {
                float fadeIn = i / (float)fadeLength;
                float fadeOut = 1 - fadeIn;
                samples[i] *= fadeIn;
                samples[samples.Length - 1 - i] *= fadeOut;
            }
        }

        public ReadOnlySpan<float> GetBuffer() => buffer;
    }

    public enum NoiseType
    {
        White,
        Pink
    }
}