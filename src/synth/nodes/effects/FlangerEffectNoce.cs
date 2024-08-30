using System;
using System.Linq;

namespace Synth
{
    public class FlangerEffectNode : AudioNode
    {
        private readonly DelayLine[] delayLines;
        private readonly LFOModel lfo;
        private readonly SynthType[] feedbackBuffer;
        private readonly SimpleLowPassFilter[] lowPassFilters;

        public FlangerEffectNode() : base()
        {
            int maxDelayInSamples = (int)((MaxAverageDelayMs + MaxDepthMs) * SampleRate / 1000) + 1;
            delayLines = new DelayLine[2]; // Stereo: one delay line per channel
            lfo = new LFOModel(LfoFrequencyHz, LFOWaveform.Sine, 0);
            feedbackBuffer = new SynthType[NumSamples * 2]; // Stereo feedback buffer
            lowPassFilters = new SimpleLowPassFilter[2];

            for (int i = 0; i < 2; i++)
            {
                delayLines[i] = new DelayLine(maxDelayInSamples, SampleRate, 0.0f, 1.0f, 0.0f);
                lowPassFilters[i] = new SimpleLowPassFilter(10000.0f, SampleRate);
            }

            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
        }

        public override void Process(double increment)
        {
            var input = GetParameterNodes(AudioParam.StereoInput).FirstOrDefault();
            if (input == null || !input.Enabled)
                return;

            lfo.Frequency = LfoFrequencyHz;

            for (int i = 0; i < NumSamples; i++)
            {
                SynthType leftIn = input.LeftBuffer[i];
                SynthType rightIn = input.RightBuffer[i];

                SynthType lfoValue = lfo.GetSample(SampleRate);
                SynthType delaySamples = (AverageDelayMs + DepthMs * lfoValue) * SampleRate / 1000;

                // Left channel
                lowPassFilters[0].SetCutoffFrequency(FilterFrequencyHz, SampleRate);
                SynthType filteredLeftIn = lowPassFilters[0].Process(leftIn + Feedback * feedbackBuffer[i * 2]);
                delayLines[0].SetDelayInSamples(delaySamples);
                SynthType delayedSampleLeft = delayLines[0].Process(filteredLeftIn);
                LeftBuffer[i] = WetMix * (delayedSampleLeft + Feedback * feedbackBuffer[i * 2]) + (1 - WetMix) * leftIn;
                feedbackBuffer[i * 2] = delayedSampleLeft;

                // Right channel
                lowPassFilters[1].SetCutoffFrequency(FilterFrequencyHz, SampleRate);
                SynthType filteredRightIn = lowPassFilters[1].Process(rightIn + Feedback * feedbackBuffer[i * 2 + 1]);
                delayLines[1].SetDelayInSamples(delaySamples);
                SynthType delayedSampleRight = delayLines[1].Process(filteredRightIn);
                RightBuffer[i] = WetMix * (delayedSampleRight + Feedback * feedbackBuffer[i * 2 + 1]) + (1 - WetMix) * rightIn;
                feedbackBuffer[i * 2 + 1] = delayedSampleRight;
            }
        }

        // Flanger-specific parameters
        private SynthType averageDelayMs = 5.0f;
        public SynthType AverageDelayMs
        {
            get => averageDelayMs;
            set => averageDelayMs = SynthType.Clamp(value, 0.5f, 10.0f);
        }

        private SynthType depthMs = 3.0f;
        public SynthType DepthMs
        {
            get => depthMs;
            set => depthMs = SynthType.Clamp(value, 0.1f, 5.0f);
        }

        private SynthType wetMix = 0.5f;
        public SynthType WetMix
        {
            get => wetMix;
            set => wetMix = SynthType.Clamp(value, 0.0f, 1.0f);
        }

        private float lfoFrequencyHz = 0.5f;
        public float LfoFrequencyHz
        {
            get => lfoFrequencyHz;
            set => lfoFrequencyHz = float.Clamp(value, 0.0f, 20.0f);
        }

        private SynthType feedback = 0.3f;
        public SynthType Feedback
        {
            get => feedback;
            set => feedback = SynthType.Clamp(value, 0.0f, 0.99f);
        }

        private SynthType filterFrequencyHz = 20000.0f;
        public SynthType FilterFrequencyHz
        {
            get => filterFrequencyHz;
            set => filterFrequencyHz = SynthType.Clamp(value, 20.0f, 20000.0f);
        }

        // Constants for maximum possible values
        private const float MaxAverageDelayMs = 50.0f;
        private const float MaxDepthMs = 15.0f;

        public void Mute()
        {
            foreach (var delayLine in delayLines)
            {
                delayLine.Mute();
            }
            Array.Clear(feedbackBuffer, 0, feedbackBuffer.Length);
        }

        public void SetLFOWaveform(int lfoIndex, LFOWaveform waveform)
        {
            if (lfoIndex >= 0 && lfoIndex < 2)
            {
                lfo.Waveform = waveform;
            }
        }
    }
}
