using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class NoiseNode : AudioNode
    {
        private uint x, y, z, w;
        private NoiseType currentNoiseType;
        private float amplitude = 1.0f;
        private float dcOffset = 0.0f;
        private const int seed = 123;

        private float targetFrequencySlope;
        private float currentFrequencySlope;
        private const float SlopeSmoothing = 0.1f;

        private float previousOutput = 0.0f;
        private float previousInput = 0.0f;
        private float filterCoeff = 0.0f;

        private float[] pinkNoiseState;
        private float brownNoiseState;

        public NoiseNode() : base()
        {
            currentNoiseType = NoiseType.White;
            SetSeed(seed);
            targetFrequencySlope = 0.0f;
            currentFrequencySlope = 0.0f;
            UpdateFilterCoefficient();
            pinkNoiseState = new float[7];
            brownNoiseState = 0f;
        }

        public float FrequencySlope
        {
            get => targetFrequencySlope;
            set => targetFrequencySlope = Math.Clamp(value, -1f, 1f);
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
            currentFrequencySlope += (targetFrequencySlope - currentFrequencySlope) * SlopeSmoothing;

            if (Math.Abs(currentFrequencySlope) < 1e-6f)
            {
                filterCoeff = 1.0f;
                return;
            }

            float minFrequency = 20f;
            float maxFrequency = SampleRate / 2f;

            float cutoffFrequency;
            if (currentFrequencySlope < 0)
            {
                cutoffFrequency = maxFrequency * (float)Math.Pow(minFrequency / maxFrequency, -currentFrequencySlope);
            }
            else
            {
                cutoffFrequency = minFrequency * (float)Math.Pow(maxFrequency / minFrequency, currentFrequencySlope);
            }
            cutoffFrequency = Math.Clamp(cutoffFrequency, minFrequency, maxFrequency);

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
        private Vector<float> GetWhiteNoiseVector()
        {
            var noiseValues = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                noiseValues[i] = GetWhiteNoise();
            }
            return new Vector<float>(noiseValues);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetPinkNoise()
        {
            float pink = 0f;
            pinkNoiseState[0] = 0.99886f * pinkNoiseState[0] + GetWhiteNoise() * 0.0555179f;
            pinkNoiseState[1] = 0.99332f * pinkNoiseState[1] + GetWhiteNoise() * 0.0750759f;
            pinkNoiseState[2] = 0.96900f * pinkNoiseState[2] + GetWhiteNoise() * 0.1538520f;
            pinkNoiseState[3] = 0.86650f * pinkNoiseState[3] + GetWhiteNoise() * 0.3104856f;
            pinkNoiseState[4] = 0.55000f * pinkNoiseState[4] + GetWhiteNoise() * 0.5329522f;
            pinkNoiseState[5] = -0.7616f * pinkNoiseState[5] - GetWhiteNoise() * 0.0168980f;
            pink = pinkNoiseState[0] + pinkNoiseState[1] + pinkNoiseState[2] + pinkNoiseState[3] + pinkNoiseState[4] + pinkNoiseState[5] + pinkNoiseState[6] + GetWhiteNoise() * 0.5362f;
            pinkNoiseState[6] = GetWhiteNoise() * 0.115926f;
            return pink * 0.11f; // Scale to roughly the same range as white noise
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<float> GetPinkNoiseVector()
        {
            var noiseValues = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                noiseValues[i] = GetPinkNoise();
            }
            return new Vector<float>(noiseValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetBrownianNoise()
        {
            float white = GetWhiteNoise();
            brownNoiseState = (brownNoiseState + (0.02f * white)) / 1.02f;
            return brownNoiseState * 3.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<float> GetBrownianNoiseVector()
        {
            var noiseValues = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                noiseValues[i] = GetBrownianNoise();
            }
            return new Vector<float>(noiseValues);
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
                output = filterCoeff * previousOutput + (1 - filterCoeff) * inputNoise;
            }
            else if (currentFrequencySlope > 0)
            {
                output = filterCoeff * (previousOutput + inputNoise - previousInput);
            }
            else
            {
                output = inputNoise;
            }

            previousInput = inputNoise;
            previousOutput = output;
            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<float> ApplyFilterVector(Vector<float> inputNoise)
        {
            Vector<float> output;
            if (currentFrequencySlope < 0)
            {
                output = Vector<float>.One * filterCoeff * previousOutput +
                         (Vector<float>.One - Vector<float>.One * filterCoeff) * inputNoise;
            }
            else if (currentFrequencySlope > 0)
            {
                output = Vector<float>.One * filterCoeff *
                         (Vector<float>.One * previousOutput + inputNoise - Vector<float>.One * previousInput);
            }
            else
            {
                output = inputNoise;
            }

            previousInput = inputNoise[Vector<float>.Count - 1];
            previousOutput = output[Vector<float>.Count - 1];
            return output;
        }

        public override void Process(double increment)
        {
            UpdateFilterCoefficient();

            int bufferSize = buffer.Length;
            int vectorSize = Vector<float>.Count;
            int i = 0;

            Vector<float> amplitudeVector = new Vector<float>(amplitude);
            Vector<float> dcOffsetVector = new Vector<float>(dcOffset);

            for (; i <= bufferSize - vectorSize; i += vectorSize)
            {
                Vector<float> gainVector = new Vector<float>(GetParameter(AudioParam.Gain, i).Item2);
                Vector<float> noiseVector = currentNoiseType switch
                {
                    NoiseType.White => GetWhiteNoiseVector(),
                    NoiseType.Pink => GetPinkNoiseVector(),
                    NoiseType.Brownian => GetBrownianNoiseVector(),
                    _ => GetWhiteNoiseVector(),
                };

                Vector<float> filteredNoise = ApplyFilterVector(noiseVector);
                Vector<float> result = (filteredNoise * amplitudeVector * gainVector) + dcOffsetVector +
                                       new Vector<float>(GetParameter(AudioParam.Gain, i).Item1);

                result.CopyTo(buffer, i);
            }

            for (; i < bufferSize; i++)
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