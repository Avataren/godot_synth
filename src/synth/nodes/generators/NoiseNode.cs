using System;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class NoiseNode : AudioNode
    {
        private uint x, y, z, w;
        private NoiseType currentNoiseType;
        private const int PinkNoiseNumRows = 16;
        private float[] pinkRows;
        private float pinkRunningSum;
        private int pinkIndexMask;
        private uint pinkCount;
        private float amplitude = 1.0f;
        private float dcOffset = 0.0f;
        private const int seed = 123;

        private float targetFrequencySlope;
        private float currentFrequencySlope;
        private const float SlopeSmoothing = 0.1f;  // Adjust this value to change smoothing speed

        private float previousOutput = 0.0f;
        private float previousInput = 0.0f;
        private float filterCoeff = 0.0f;

        public NoiseNode() : base()
        {
            currentNoiseType = NoiseType.White;
            SetSeed(seed);
            pinkRows = new float[PinkNoiseNumRows];
            pinkIndexMask = (1 << PinkNoiseNumRows) - 1; // mask for the index
            pinkCount = 0;
            pinkRunningSum = 0;
            targetFrequencySlope = 0.0f;
            currentFrequencySlope = 0.0f;
            UpdateFilterCoefficient();
        }

        public float FrequencySlope
        {
            get => targetFrequencySlope;
            set
            {
                targetFrequencySlope = Math.Clamp(value, -1f, 1f);
            }
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
        private void UpdateFilterCoefficient()
        {
            // Smoothly update the current frequency slope
            currentFrequencySlope += (targetFrequencySlope - currentFrequencySlope) * SlopeSmoothing;

            if (Math.Abs(currentFrequencySlope) < 1e-6f)
            {
                filterCoeff = 1.0f;  // No filtering
                return;
            }

            float minFrequency = 20f;
            float maxFrequency = SampleRate / 2f;
            
            // Exponential mapping of frequencySlope to cutoff frequency
            float cutoffFrequency;
            if (currentFrequencySlope < 0)
            {
                // For low-pass, we want the cutoff to decrease as the slope goes from 0 to -1
                cutoffFrequency = maxFrequency * (float)Math.Pow(minFrequency / maxFrequency, -currentFrequencySlope);
            }
            else
            {
                // For high-pass, we want the cutoff to increase as the slope goes from 0 to 1
                cutoffFrequency = minFrequency * (float)Math.Pow(maxFrequency / minFrequency, currentFrequencySlope);
            }
            cutoffFrequency = Math.Clamp(cutoffFrequency, minFrequency, maxFrequency);

            // Calculate the filter coefficient
            float rc = 1.0f / (2.0f * (float)Math.PI * cutoffFrequency);
            float dt = 1.0f / SampleRate;
            filterCoeff = rc / (rc + dt);
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
            // Voss-McCartney algorithm implementation
            pinkCount++;
            int lastZeroBit = (int)(pinkCount & -pinkCount); // isolate the rightmost 1-bit and cast to int
            int rowIndex = 0;

            while ((lastZeroBit >>= 1) != 0)
            {
                rowIndex++;
            }

            if (rowIndex < PinkNoiseNumRows)
            {
                pinkRunningSum -= pinkRows[rowIndex];
                pinkRows[rowIndex] = GetWhiteNoise();
                pinkRunningSum += pinkRows[rowIndex];
            }

            return pinkRunningSum / PinkNoiseNumRows;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetBrownianNoise()
        {
            previousOutput += GetWhiteNoise() * 0.1f; // Brownian noise is cumulative white noise
            return Math.Clamp(previousOutput, -1f, 1f);
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            currentNoiseType = noiseType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ApplyFilter(float inputNoise)
        {
            float output;
            if (currentFrequencySlope < 0)
            {
                // Low-pass filter
                output = filterCoeff * previousOutput + (1 - filterCoeff) * inputNoise;
            }
            else if (currentFrequencySlope > 0)
            {
                // High-pass filter
                output = filterCoeff * (previousOutput + inputNoise - previousInput);
            }
            else
            {
                // No filtering
                output = inputNoise;
            }

            previousInput = inputNoise;
            previousOutput = output;
            return output;
        }

        public override void Process(double increment)
        {
            UpdateFilterCoefficient();  // Update the filter coefficient for each buffer

            int bufferSize = buffer.Length;
            for (int i = 0; i < bufferSize; i++)
            {
                var gainParam = GetParameter(AudioParam.Gain, i);
                float noiseValue = currentNoiseType switch
                {
                    NoiseType.White => GetWhiteNoise(),
                    NoiseType.Pink => GetPinkNoise(),
                    NoiseType.Brownian => GetBrownianNoise(),
                    _ => GetWhiteNoise(),
                };

                buffer[i] = (ApplyFilter(noiseValue) * amplitude * gainParam.Item2) + dcOffset + gainParam.Item1;
            }
        }
    }

    public enum NoiseType
    {
        White,
        Pink,
        Brownian
    }
}