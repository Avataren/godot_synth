using System;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class DelayLine
    {
        private SynthType[] buffer;
        private int writeIndex;
        private int bufferSize;
        private int readIndex;
        private SynthType fraction;
        private SynthType prevOutput;

        public SynthType Feedback { get; set; }
        public SynthType WetMix { get; set; }
        public SynthType DryMix { get; set; }
        public SynthType SampleRate { get; set; }

        public int MaxDelayInMilliseconds { get; private set; }
        public int CurrentDelayInMilliseconds { get; private set; }

        public DelayLine(int maxDelayInMilliseconds, SynthType sampleRate, SynthType feedback = 0.25f, SynthType wetMix = 0.5f, SynthType dryMix = 1.0f)
        {
            SampleRate = sampleRate;
            MaxDelayInMilliseconds = maxDelayInMilliseconds;
            bufferSize = (int)(maxDelayInMilliseconds * sampleRate / 1000.0) + 1;
            buffer = new SynthType[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
            Feedback = feedback;
            WetMix = wetMix;
            DryMix = dryMix;
            prevOutput = 0;
            SetDelayTime(maxDelayInMilliseconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayTime(int delayInMilliseconds)
        {
            CurrentDelayInMilliseconds = Math.Min(delayInMilliseconds, MaxDelayInMilliseconds);
            SynthType delaySamples = CurrentDelayInMilliseconds * SampleRate / 1000.0f;
            SetDelayInSamples(delaySamples);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayInSamples(SynthType delaySamples)
        {
            int intDelay = (int)delaySamples;
            fraction = delaySamples - intDelay;
            readIndex = (writeIndex - intDelay + bufferSize) % bufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType Process(SynthType inputSample)
        {
            // All-pass interpolation
            int index0 = readIndex;
            int index1 = (readIndex + 1) % bufferSize;

            SynthType frac = fraction;
            SynthType a = (1 - frac) / (1 + frac);

            SynthType x0 = buffer[index0];
            SynthType x1 = buffer[index1];

            SynthType delaySample = a * (x0 - prevOutput) + x1;
            prevOutput = delaySample;

            // Apply feedback and wet/dry mix
            SynthType feedbackSample = delaySample * Feedback;
            SynthType outputSample = (DryMix * inputSample) + (WetMix * delaySample);

            // Write the new sample into the buffer
            buffer[writeIndex] = inputSample + feedbackSample;

            // Update indices
            writeIndex = (writeIndex + 1) % bufferSize;
            readIndex = (readIndex + 1) % bufferSize;

            return outputSample;
        }

        public void Mute()
        {
            buffer.AsSpan().Clear();
            prevOutput = 0;
        }

        public void SetMaxDelayTime(int maxDelayInMilliseconds)
        {
            MaxDelayInMilliseconds = maxDelayInMilliseconds;
            int newBufferSize = (int)(maxDelayInMilliseconds * SampleRate / 1000.0) + 1;
            if (newBufferSize > bufferSize)
            {
                Array.Resize(ref buffer, newBufferSize);
                bufferSize = newBufferSize;
            }
            SetDelayTime(CurrentDelayInMilliseconds); // Ensure current delay is still valid
        }
    }
}