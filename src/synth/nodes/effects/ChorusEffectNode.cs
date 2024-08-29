using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class Oscillator
    {
        private int index;
        private readonly SynthType[] waveTable;
        private const int TableSize = 4096;

        public Oscillator()
        {
            waveTable = new SynthType[TableSize];
            InitializeWaveTable();
        }

        private void InitializeWaveTable()
        {
            for (int i = 0; i < TableSize; i++)
            {
                waveTable[i] = (SynthType)(0.7 * Math.Sin(2 * Math.PI * i / TableSize) + 0.3 * (2 * Math.Abs(2 * (i / (double)TableSize) - 1) - 1));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample()
        {
            SynthType sample = waveTable[index];
            index = (index + 1) % TableSize;
            return sample;
        }
    }

    public class ChorusDelayLine
    {
        private readonly SynthType[] buffer;
        private int writeIndex;
        private int bufferSize;
        private int readIndex;
        private SynthType fraction;

        public ChorusDelayLine(int size)
        {
            bufferSize = size;
            buffer = new SynthType[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelay(SynthType delay)
        {
            int intDelay = (int)delay;
            fraction = delay - intDelay;
            readIndex = (writeIndex - intDelay + bufferSize) % bufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(SynthType inputSample)
        {
            int nextIndex = (readIndex + 1) % bufferSize;
            SynthType outputSample = (1 - fraction) * buffer[readIndex] + fraction * buffer[nextIndex];
            buffer[writeIndex] = inputSample;
            writeIndex = (writeIndex + 1) % bufferSize;
            return outputSample;
        }

        public void Mute()
        {
            Array.Clear(buffer, 0, bufferSize);
        }
    }

    public class ChorusEffectNode : AudioNode
    {
        private const int MaxVoices = 3;
        private readonly ChorusDelayLine[] delayLines;
        private readonly Oscillator oscillator;
        private readonly SynthType[] voicePans;

        public ChorusEffectNode()
        {
            delayLines = new ChorusDelayLine[MaxVoices];
            oscillator = new Oscillator();
            voicePans = new SynthType[MaxVoices];

            int maxDelayInSamples = (int)(SampleRate * (BaseDelayTime + LfoDepth) / 1000) + 1;
            for (int i = 0; i < MaxVoices; i++)
            {
                delayLines[i] = new ChorusDelayLine(maxDelayInSamples);
                voicePans[i] = (SynthType)i / (MaxVoices - 1) - 0.5f;
            }

            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
        }

        public override void Process(double increment)
        {
            for (int i = 0; i < NumSamples; i++)
            {
                SynthType oscOut = oscillator.GetSample();
                SynthType inSample = LeftBuffer[i]; // Assuming mono input for simplicity
                SynthType wetOut = 0;

                for (int voice = 0; voice < MaxVoices; voice++)
                {
                    delayLines[voice].SetDelay(BaseDelayTime + LfoDepth * oscOut);
                    wetOut += delayLines[voice].GetSample(inSample);
                }

                LeftBuffer[i] = DryMix * inSample + WetMix * (wetOut / MaxVoices);
                RightBuffer[i] = LeftBuffer[i]; // For stereo output, you might want different processing
            }
        }

        public void Mute()
        {
            foreach (var delayLine in delayLines)
            {
                delayLine.Mute();
            }
        }

        // Parameters
        public SynthType LfoDepth { get; set; } = 1.5f;
        public int BaseDelayTime { get; set; } = 25; // milliseconds
        public SynthType DryMix { get; set; } = 0.5f;
        public SynthType WetMix { get; set; } = 0.5f;
    }
}
