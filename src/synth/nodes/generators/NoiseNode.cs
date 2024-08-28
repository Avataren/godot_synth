using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class NoiseNode : AudioNode
    {
        private uint x, y, z, w;
        private NoiseType currentNoiseType;
        private SynthType amplitude = 1.0f;
        private SynthType dcOffset = 0.0f;
        private const int seed = 123;

        private SynthType targetCutoff;
        private SynthType currentCutoff;
        private const SynthType CutoffSmoothing = 0.1f;

        private SynthType previousOutput = 0.0f;
        private SynthType filterCoeff = 0.0f;

        private SynthType[] pinkNoiseState;
        private SynthType brownNoiseState;

        public NoiseNode() : base()
        {
            currentNoiseType = NoiseType.White;
            SetSeed(seed);
            targetCutoff = 1.0f;
            currentCutoff = 1.0f;
            UpdateFilterCoefficient();
            pinkNoiseState = new SynthType[7];
            brownNoiseState = 0f;
        }

        public SynthType Cutoff
        {
            get => targetCutoff;
            set => targetCutoff = SynthTypeHelper.Clamp(value, 0f, 1f);
        }

        public void SetSeed(int seed)
        {
            x = (uint)seed;
            y = 362436069;
            z = 521288629;
            w = 88675123;
        }

        public void SetAmplitude(SynthType newAmplitude)
        {
            amplitude = SynthTypeHelper.Clamp(newAmplitude, 0f, 1f);
        }

        public void SetDCOffset(SynthType newOffset)
        {
            dcOffset = SynthTypeHelper.Clamp(newOffset, -1f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateFilterCoefficient(SynthType cutoffMod = 1.0f)
        {
            currentCutoff += (targetCutoff * cutoffMod - currentCutoff) * CutoffSmoothing;

            SynthType minFrequency = 20f;
            SynthType maxFrequency = SampleRate / 2f;

            // Exponential curve for cutoff frequency
            SynthType cutoffFrequency = minFrequency * SynthTypeHelper.Pow(maxFrequency / minFrequency, currentCutoff);
            cutoffFrequency = SynthTypeHelper.Clamp(cutoffFrequency, minFrequency, maxFrequency);

            SynthType rc = SynthTypeHelper.One / (2.0f * MathF.PI * cutoffFrequency);
            SynthType dt = SynthTypeHelper.One / SampleRate;
            filterCoeff = dt / (rc + dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Next()
        {
            uint t = x ^ (x << 11);
            x = y; y = z; z = w;
            return w = w ^ (w >> 19) ^ (t ^ (t >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType GetWhiteNoise()
        {
            return Next() / (SynthType)uint.MaxValue * 2 - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<SynthType> GetWhiteNoiseVector()
        {
            var noiseValues = new SynthType[Vector<SynthType>.Count];
            for (int i = 0; i < Vector<SynthType>.Count; i++)
            {
                noiseValues[i] = GetWhiteNoise();
            }
            return new Vector<SynthType>(noiseValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType GetPinkNoise()
        {
            SynthType pink = 0f;
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
        private Vector<SynthType> GetPinkNoiseVector()
        {
            var noiseValues = new SynthType[Vector<SynthType>.Count];
            for (int i = 0; i < Vector<SynthType>.Count; i++)
            {
                noiseValues[i] = GetPinkNoise();
            }
            return new Vector<SynthType>(noiseValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType GetBrownianNoise()
        {
            var white = GetWhiteNoise();
            brownNoiseState = (brownNoiseState + (0.02f * white)) / 1.02f;
            return brownNoiseState * 3.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<SynthType> GetBrownianNoiseVector()
        {
            var noiseValues = new SynthType[Vector<SynthType>.Count];
            for (int i = 0; i < Vector<SynthType>.Count; i++)
            {
                noiseValues[i] = GetBrownianNoise();
            }
            return new Vector<SynthType>(noiseValues);
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            currentNoiseType = noiseType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType ApplyFilter(SynthType inputNoise)
        {
            SynthType output = filterCoeff * inputNoise + (SynthTypeHelper.One - filterCoeff) * previousOutput;
            previousOutput = output;
            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector<SynthType> ApplyFilterVector(Vector<SynthType> inputNoise)
        {
            Vector<SynthType> output = Vector<SynthType>.One * filterCoeff * inputNoise +
                                   (Vector<SynthType>.One - Vector<SynthType>.One * filterCoeff) * new Vector<SynthType>(previousOutput);
            previousOutput = output[Vector<SynthType>.Count - 1];
            return output;
        }

        public override void Process(double increment)
        {
            int bufferSize = buffer.Length;
            int vectorSize = Vector<SynthType>.Count;
            int i = 0;

            Vector<SynthType> amplitudeVector = new Vector<SynthType>(amplitude);
            Vector<SynthType> dcOffsetVector = new Vector<SynthType>(dcOffset);

            for (; i <= bufferSize - vectorSize; i += vectorSize)
            {
                Vector<SynthType> gainVector = new Vector<SynthType>(GetParameter(AudioParam.Gain, i).Item2);
                UpdateFilterCoefficient(GetParameter(AudioParam.CutOffMod, i).Item2);

                Vector<SynthType> noiseVector = currentNoiseType switch
                {
                    NoiseType.White => GetWhiteNoiseVector(),
                    NoiseType.Pink => GetPinkNoiseVector(),
                    NoiseType.Brownian => GetBrownianNoiseVector(),
                    _ => GetWhiteNoiseVector(),
                };

                Vector<SynthType> filteredNoise = ApplyFilterVector(noiseVector);
                Vector<SynthType> result = (filteredNoise * amplitudeVector * gainVector) + dcOffsetVector +
                                       new Vector<SynthType>(GetParameter(AudioParam.Gain, i).Item1);

                result.CopyTo(buffer, i);
            }

            for (; i < bufferSize; i++)
            {
                var gainParam = GetParameter(AudioParam.Gain, i);
                UpdateFilterCoefficient(GetParameter(AudioParam.CutOffMod, i).Item2);
                var noiseValue = currentNoiseType switch
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