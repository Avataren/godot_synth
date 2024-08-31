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
            prevOutput = 0;
            SetDelayTime(maxDelayInMilliseconds);
        }

        public void SetDelayTime(int delayInMilliseconds)
        {
            CurrentDelayInMilliseconds = Math.Min(delayInMilliseconds, MaxDelayInMilliseconds);
            SynthType delaySamples = CurrentDelayInMilliseconds * SampleRate / 1000.0f;
            int integerDelaySamples = Math.Max(1, (int)delaySamples);  // Ensure at least 1 sample delay
            fraction = delaySamples - integerDelaySamples;
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
            int index0 = readIndex;
            int index1 = (readIndex + 1) % bufferSize;

            SynthType frac = fraction;
            SynthType a = (1 - frac) / (1 + frac);

            SynthType x0 = buffer[index0];
            SynthType x1 = buffer[index1];

            SynthType delaySample = a * (x0 - prevOutput) + x1;

            SynthType feedbackSample = delaySample * Feedback;

            SynthType outputSample = ((1 - WetMix) * inputSample) + (WetMix * delaySample);

            buffer[writeIndex] = inputSample + feedbackSample;

            writeIndex = (writeIndex + 1) % bufferSize;
            readIndex = (readIndex + 1) % bufferSize;
            prevOutput = delaySample;

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