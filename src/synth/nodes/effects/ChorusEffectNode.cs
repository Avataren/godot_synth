using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

namespace Synth
{
    public class ChorusLFO
    {
        private double phase = 0.0;
        private readonly SynthType[] waveTable;
        private const int TableSize = 4096;
        private const int TableMask = TableSize - 1; // For bitwise optimization
        public float Frequency { get; set; } // in Hz
        public float PhaseOffset { get; set; } // Phase offset in radians

        public ChorusLFO(float frequency, float phaseOffset = 0.0f)
        {
            Frequency = frequency;
            PhaseOffset = phaseOffset;
            waveTable = new SynthType[TableSize];
            InitializeWaveTable();
        }

        private void InitializeWaveTable()
        {
            for (int i = 0; i < TableSize; i++)
            {
                waveTable[i] = (SynthType)Math.Sin(2 * Math.PI * i / TableSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(double sampleRate)
        {
            int index = (int)(((phase + PhaseOffset) * TableSize) % TableSize) & TableMask; // Apply phase offset
            SynthType sample = waveTable[index];
            phase += Frequency / sampleRate;
            if (phase >= 1.0) phase -= 1.0;
            return sample;
        }
    }

    public class ChorusDelayLine
    {
        private readonly SynthType[] buffer;
        private int writeIndex;
        private readonly int bufferSize;
        private int readIndex;
        private SynthType fraction;

        public ChorusDelayLine(int sizeInSamples)
        {
            bufferSize = sizeInSamples;
            buffer = new SynthType[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayInSamples(SynthType delaySamples)
        {
            int intDelay = (int)delaySamples;
            fraction = delaySamples - intDelay;
            readIndex = (writeIndex - intDelay + bufferSize) % bufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(SynthType inputSample)
        {
            int nextIndex = (readIndex + 1) % bufferSize;
            SynthType outputSample = (1 - fraction) * buffer[readIndex] + fraction * buffer[nextIndex];
            buffer[writeIndex] = inputSample;
            writeIndex = (writeIndex + 1) % bufferSize;
            readIndex = (readIndex + 1) % bufferSize;
            return outputSample;
        }

        public void Mute()
        {
            Array.Clear(buffer, 0, bufferSize);
        }
    }

    public class ChorusEffectNode : AudioNode
    {
        private readonly ChorusDelayLine delayLineLeft;
        private readonly ChorusDelayLine delayLineRight;
        private readonly ChorusLFO lfoLeft;
        private readonly ChorusLFO lfoRight;

        public ChorusEffectNode()
        {
            int maxDelayInSamples = (int)((AverageDelayMs + DepthMs) * SampleRate / 1000) + 1;
            delayLineLeft = new ChorusDelayLine(maxDelayInSamples);
            delayLineRight = new ChorusDelayLine(maxDelayInSamples);

            // Introduce a phase offset for the right channel LFO
            lfoLeft = new ChorusLFO(LfoFrequencyHz);
            lfoRight = new ChorusLFO(LfoFrequencyHz, 0.5f); // 0.5 radians offset

            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
        }

        public override void Process(double increment)
        {
            var input = GetParameterNodes(AudioParam.StereoInput).FirstOrDefault();
            if (input == null || !input.Enabled)
                return;

            for (int i = 0; i < NumSamples; i++)
            {
                SynthType leftIn = input.LeftBuffer[i];
                SynthType rightIn = input.RightBuffer[i];

                // Calculate LFO output for each channel with a phase offset
                SynthType oscOutLeft = lfoLeft.GetSample(SampleRate);
                SynthType oscOutRight = lfoRight.GetSample(SampleRate);

                // Convert delay time from ms to samples
                SynthType delaySamplesLeft = (AverageDelayMs + DepthMs * oscOutLeft) * SampleRate / 1000;
                SynthType delaySamplesRight = (AverageDelayMs + DepthMs * oscOutRight) * SampleRate / 1000;

                // Set delay times independently
                delayLineLeft.SetDelayInSamples(delaySamplesLeft);
                delayLineRight.SetDelayInSamples(delaySamplesRight);

                // Get the delayed samples for each channel
                SynthType leftWet = delayLineLeft.GetSample(leftIn);
                SynthType rightWet = delayLineRight.GetSample(rightIn);

                // Mix wet and dry signals for each channel
                LeftBuffer[i] = WetMix * leftWet + DryMix * leftIn;
                RightBuffer[i] = WetMix * rightWet + DryMix * rightIn;
            }
        }

        // Parameters with validation
        private SynthType averageDelayMs = 15.0f;
        public SynthType AverageDelayMs
        {
            get => averageDelayMs;
            set => averageDelayMs = Math.Clamp(value, 0.0f, 50.0f);
        }

        private SynthType depthMs = 5.0f;
        public SynthType DepthMs
        {
            get => depthMs;
            set => depthMs = Math.Clamp(value, 0.0f, 20.0f);
        }

        private SynthType dryMix = 0.5f;
        public SynthType DryMix
        {
            get => dryMix;
            set => dryMix = Math.Clamp(value, 0.0f, 1.0f); // Ensure within 0-1 range
        }

        private SynthType wetMix = 0.5f;
        public SynthType WetMix
        {
            get => wetMix;
            set => wetMix = Math.Clamp(value, 0.0f, 1.0f); // Ensure within 0-1 range
        }

        private float lfoFrequencyHz = 0.5f;
        public float LfoFrequencyHz
        {
            get => lfoFrequencyHz;
            set => lfoFrequencyHz = Math.Clamp(value, 0.1f, 5.0f); // Example range
        }

        public void Mute()
        {
            delayLineLeft.Mute();
            delayLineRight.Mute();
        }
    }
}
