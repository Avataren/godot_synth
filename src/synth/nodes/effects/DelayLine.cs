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

        public SynthType Feedback { get; set; }
        public SynthType _wetMix;
        public SynthType WetMix
        {
            get
            {
                return _wetMix;
            }

            set
            {
                _wetMix = SynthType.Clamp(value, 0.0f, 1.0f);
            }
        }
        public SynthType SampleRate { get; set; }

        public int MaxDelayInMilliseconds { get; private set; }
        public int CurrentDelayInMilliseconds { get; private set; }

        public DelayLine(int maxDelayInMilliseconds, SynthType sampleRate, SynthType feedback = 0.25f, SynthType wetMix = 0.5f)
        {
            SampleRate = sampleRate;
            MaxDelayInMilliseconds = maxDelayInMilliseconds;
            bufferSize = (int)(maxDelayInMilliseconds * sampleRate / 1000.0) + 1;
            buffer = new SynthType[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
            Feedback = feedback;
            WetMix = wetMix;
            SetDelayTime(maxDelayInMilliseconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayTime(int delayInMilliseconds)
        {
            CurrentDelayInMilliseconds = Math.Min(delayInMilliseconds, MaxDelayInMilliseconds);
            SynthType delaySamples = CurrentDelayInMilliseconds * SampleRate / 1000.0f;
            int integerDelaySamples = (int)delaySamples;
            SetDelayInSamples(integerDelaySamples);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayInSamples(int delaySamples)
        {
            readIndex = (writeIndex - delaySamples + bufferSize) % bufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType Process(SynthType inputSample)
        {
            // Directly use the delay sample from the buffer
            SynthType delaySample = buffer[readIndex];

            // Apply feedback (if any)
            SynthType feedbackSample = delaySample * Feedback;

            // Crossfade between dry and wet signals
            SynthType outputSample = ((1 - WetMix) * inputSample) + (WetMix * delaySample);

            // Write the new sample into the buffer with feedback
            buffer[writeIndex] = inputSample + feedbackSample;

            // Update indices
            writeIndex = (writeIndex + 1) % bufferSize;
            readIndex = (readIndex + 1) % bufferSize;

            return outputSample;
        }


        public void Mute()
        {
            buffer.AsSpan().Clear();
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