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
        private const int TableMask = TableSize - 1;
        public float Frequency { get; set; }
        public float PhaseOffset { get; set; }

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
            int index = (int)(((phase + PhaseOffset) * TableSize) % TableSize) & TableMask;
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
            // Cubic interpolation
            int index0 = (readIndex - 1 + bufferSize) % bufferSize;
            int index1 = readIndex;
            int index2 = (readIndex + 1) % bufferSize;
            int index3 = (readIndex + 2) % bufferSize;

            SynthType y0 = buffer[index0];
            SynthType y1 = buffer[index1];
            SynthType y2 = buffer[index2];
            SynthType y3 = buffer[index3];

            SynthType mu = fraction;
            SynthType mu2 = mu * mu;
            SynthType a0 = y3 - y2 - y0 + y1;
            SynthType a1 = y0 - y1 - a0;
            SynthType a2 = y2 - y0;
            SynthType a3 = y1;

            SynthType outputSample = a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;

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
        private readonly ChorusDelayLine[] delayLines;
        private readonly ChorusLFO[] lfos;
        private const int NumVoices = 3;

        public ChorusEffectNode()
        {
            int maxDelayInSamples = (int)((MaxAverageDelayMs + MaxDepthMs) * SampleRate / 1000) + 1;
            delayLines = new ChorusDelayLine[NumVoices * 2]; // Stereo, so 2 per voice
            lfos = new ChorusLFO[NumVoices * 2];

            for (int i = 0; i < NumVoices * 2; i++)
            {
                delayLines[i] = new ChorusDelayLine(maxDelayInSamples);
                lfos[i] = new ChorusLFO(LfoFrequencyHz + i * 0.01f, i * 0.1f); // Slight detuning and phase offset
            }

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

                SynthType leftWet = 0;
                SynthType rightWet = 0;

                for (int v = 0; v < NumVoices; v++)
                {
                    // Left channel
                    SynthType oscOutLeft = (lfos[v * 2].GetSample(SampleRate) + 1) * 0.5f;
                    SynthType delaySamplesLeft = (AverageDelayMs + DepthMs * oscOutLeft) * SampleRate / 1000;
                    delayLines[v * 2].SetDelayInSamples(delaySamplesLeft);
                    leftWet += delayLines[v * 2].GetSample(leftIn + Feedback * leftWet);

                    // Right channel
                    SynthType oscOutRight = (lfos[v * 2 + 1].GetSample(SampleRate) + 1) * 0.5f;
                    SynthType delaySamplesRight = (AverageDelayMs + DepthMs * oscOutRight) * SampleRate / 1000;
                    delayLines[v * 2 + 1].SetDelayInSamples(delaySamplesRight);
                    rightWet += delayLines[v * 2 + 1].GetSample(rightIn + Feedback * rightWet);
                }

                leftWet /= NumVoices;
                rightWet /= NumVoices;

                LeftBuffer[i] = WetMix * leftWet + (1 - WetMix) * leftIn;
                RightBuffer[i] = WetMix * rightWet + (1 - WetMix) * rightIn;
            }
        }

        // Parameters with validation
        private SynthType averageDelayMs = 20.0f;
        public SynthType AverageDelayMs
        {
            get => averageDelayMs;
            set => averageDelayMs = Math.Clamp(value, 1.0f, 50.0f);
        }

        private SynthType depthMs = 3.0f;
        public SynthType DepthMs
        {
            get => depthMs;
            set => depthMs = Math.Clamp(value, 1.0f, 5.0f);
        }

        private SynthType wetMix = 0.5f;
        public SynthType WetMix
        {
            get => wetMix;
            set => wetMix = Math.Clamp(value, 0.0f, 1.0f);
        }

        private float lfoFrequencyHz = 0.5f;
        public float LfoFrequencyHz
        {
            get => lfoFrequencyHz;
            set => lfoFrequencyHz = Math.Clamp(value, 0.0f, 2.0f);
        }

        private SynthType feedback = 0.2f;
        public SynthType Feedback
        {
            get => feedback;
            set => feedback = Math.Clamp(value, 0.0f, 0.5f);
        }

        // Constants for maximum possible values (used for buffer allocation)
        private const float MaxAverageDelayMs = 15.0f;
        private const float MaxDepthMs = 5.0f;

        public void Mute()
        {
            foreach (var delayLine in delayLines)
            {
                delayLine.Mute();
            }
        }
    }
}