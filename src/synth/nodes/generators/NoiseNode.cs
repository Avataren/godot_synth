using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class NoiseNode : AudioNode
    {
        // Constants
        private const int SEED = 123;
        private const SynthType CUTOFF_SMOOTHING = 0.1f;
        private const SynthType MIN_FREQUENCY = 20f;
        private const SynthType PINK_NOISE_SCALE = 0.25f;
        private const SynthType BROWNIAN_NOISE_SCALE = 3.5f;

        // Random number generator state
        private uint state0, state1, state2, state3;

        // Noise parameters
        private NoiseType currentNoiseType;
        private SynthType dcOffset = 0f;

        // Filter parameters
        private SynthType targetCutoff;
        private SynthType currentCutoff;
        private SynthType previousOutput = 0f;
        private SynthType filterCoeff = 0f;

        // Pink and Brownian noise state
        private SynthType[] pinkNoiseState;
        private SynthType brownNoiseState;

        public NoiseNode() : base()
        {
            currentNoiseType = NoiseType.White;
            SetSeed(SEED);
            targetCutoff = 1f;
            currentCutoff = 1f;
            UpdateFilterCoefficient();
            pinkNoiseState = new SynthType[7];
            brownNoiseState = 0f;
        }

        public SynthType Cutoff
        {
            get => targetCutoff;
            set => targetCutoff = SynthType.Clamp(value, 0f, 1f);
        }

        public SynthType DCOffset
        {
            get => dcOffset;
            set => dcOffset = SynthType.Clamp(value, -1f, 1f);
        }

        public void SetSeed(int seed)
        {
            state0 = (uint)seed;
            state1 = 362436069;
            state2 = 521288629;
            state3 = 88675123;
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            currentNoiseType = noiseType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateFilterCoefficient(SynthType cutoffMod = 1f)
        {
            currentCutoff += (targetCutoff * cutoffMod - currentCutoff) * CUTOFF_SMOOTHING;

            SynthType maxFrequency = SampleRate / 2f;

            // Exponential curve for cutoff frequency
            SynthType cutoffFrequency = MIN_FREQUENCY * (SynthType)Math.Exp(Math.Log(maxFrequency / MIN_FREQUENCY) * currentCutoff);
            cutoffFrequency = SynthType.Clamp(cutoffFrequency, MIN_FREQUENCY, maxFrequency);

            SynthType rc = 1f / ((SynthType)(2.0 * Math.PI) * cutoffFrequency);
            SynthType dt = 1f / SampleRate;
            filterCoeff = dt / (rc + dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GenerateRandomUInt()
        {
            // xoshiro128** algorithm
            uint result = BitOperations.RotateLeft(state1 * 5, 7) * 9;
            uint t = state1 << 9;
            state2 ^= state0;
            state3 ^= state1;
            state1 ^= state2;
            state0 ^= state3;
            state2 ^= t;
            state3 = BitOperations.RotateLeft(state3, 11);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType GetWhiteNoise()
        {
            return (SynthType)GenerateRandomUInt() / uint.MaxValue * 2f - 1f;
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
            SynthType white = GetWhiteNoise();

            pinkNoiseState[0] = 0.99886f * pinkNoiseState[0] + white * 0.0555179f;
            pinkNoiseState[1] = 0.99332f * pinkNoiseState[1] + white * 0.0750759f;
            pinkNoiseState[2] = 0.96900f * pinkNoiseState[2] + white * 0.1538520f;
            pinkNoiseState[3] = 0.86650f * pinkNoiseState[3] + white * 0.3104856f;
            pinkNoiseState[4] = 0.55000f * pinkNoiseState[4] + white * 0.5329522f;
            pinkNoiseState[5] = -0.7616f * pinkNoiseState[5] - white * 0.0168980f;

            pink = pinkNoiseState[0] + pinkNoiseState[1] + pinkNoiseState[2] + pinkNoiseState[3] +
                   pinkNoiseState[4] + pinkNoiseState[5] + pinkNoiseState[6] + white * 0.5362f;
            pinkNoiseState[6] = white * 0.115926f;

            return pink * PINK_NOISE_SCALE;
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
            return brownNoiseState * BROWNIAN_NOISE_SCALE;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SynthType ApplyFilter(SynthType inputNoise)
        {
            SynthType output = filterCoeff * inputNoise + (1f - filterCoeff) * previousOutput;
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

            Vector<SynthType> amplitudeVector = new Vector<SynthType>(Amplitude);
            Vector<SynthType> dcOffsetVector = new Vector<SynthType>(dcOffset);

            // Pre-calculate values that don't change per sample
            var gainParam = GetParameter(AudioParam.Gain, 0);
            var cutoffModParam = GetParameter(AudioParam.CutOffMod, 0);

            for (; i <= bufferSize - vectorSize; i += vectorSize)
            {
                Vector<SynthType> gainVector = new Vector<SynthType>(gainParam.Item2);
                UpdateFilterCoefficient(cutoffModParam.Item2);

                Vector<SynthType> noiseVector = currentNoiseType switch
                {
                    NoiseType.White => GetWhiteNoiseVector(),
                    NoiseType.Pink => GetPinkNoiseVector(),
                    NoiseType.Brownian => GetBrownianNoiseVector(),
                    _ => throw new ArgumentException("Invalid noise type"),
                };

                Vector<SynthType> filteredNoise = ApplyFilterVector(noiseVector);
                Vector<SynthType> result = (filteredNoise * amplitudeVector * gainVector) + dcOffsetVector +
                                           new Vector<SynthType>(gainParam.Item1);

                result.CopyTo(buffer, i);
            }

            for (; i < bufferSize; i++)
            {
                UpdateFilterCoefficient(cutoffModParam.Item2);
                var noiseValue = currentNoiseType switch
                {
                    NoiseType.White => GetWhiteNoise(),
                    NoiseType.Pink => GetPinkNoise(),
                    NoiseType.Brownian => GetBrownianNoise(),
                    _ => throw new ArgumentException("Invalid noise type"),
                };

                buffer[i] = (ApplyFilter(noiseValue) * Amplitude * gainParam.Item2) + dcOffset + gainParam.Item1;
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