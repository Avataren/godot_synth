using System;
using System.Linq;

namespace Synth
{
    public class ChorusEffectNode : AudioNode
    {
        private readonly DelayLine[] delayLines;
        private readonly LFOModel[] lfos;
        private readonly LFOModel globalLfo;
        private readonly SynthType[] feedbackBuffer;
        private readonly SimpleLowPassFilter[] lowPassFilters;
        private const int NumVoices = 3;

        public ChorusEffectNode() : base()
        {
            int maxDelayInSamples = (int)((MaxAverageDelayMs + MaxDepthMs) * SampleRate / 1000) + 1;
            delayLines = new DelayLine[NumVoices * 2]; // Stereo, so 2 per voice
            lfos = new LFOModel[NumVoices * 2];
            globalLfo = new LFOModel(GlobalLfoFrequencyHz, LFOWaveform.Sine, 0);
            feedbackBuffer = new SynthType[NumSamples * 2]; // Stereo feedback buffer
            lowPassFilters = new SimpleLowPassFilter[NumVoices * 2];

            for (int i = 0; i < NumVoices * 2; i++)
            {
                delayLines[i] = new DelayLine(maxDelayInSamples,SampleRate, 0.0f, 1.0f, 0.0f);
                LFOWaveform waveform = i % 2 == 0 ? LFOWaveform.Sine : LFOWaveform.Triangle;
                lfos[i] = new LFOModel(LfoFrequencyHz + i * 0.01f, waveform, i * 0.1f);
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

            // Update LFO frequencies
            globalLfo.Frequency = GlobalLfoFrequencyHz;
            for (int i = 0; i < NumVoices * 2; i++)
            {
                lfos[i].Frequency = LfoFrequencyHz + i * 0.01f; // Maintain slight detuning
            }

            for (int i = 0; i < NumSamples; i++)
            {
                SynthType leftIn = input.LeftBuffer[i];
                SynthType rightIn = input.RightBuffer[i];

                SynthType leftWet = 0;
                SynthType rightWet = 0;

                SynthType globalLfoValue = globalLfo.GetSample(SampleRate);

                for (int v = 0; v < NumVoices; v++)
                {
                    // Left channel
                    SynthType oscOutLeft = lfos[v * 2].GetSample(SampleRate);
                    SynthType combinedLfoLeft = (1 - GlobalLfoAmount) * oscOutLeft + GlobalLfoAmount * globalLfoValue;
                    SynthType delaySamplesLeft = (AverageDelayMs + DepthMs * combinedLfoLeft) * SampleRate / 1000;

                    // Apply low-pass filter before the delay line
                    lowPassFilters[v * 2].SetCutoffFrequency(FilterFrequencyHz, SampleRate);
                    SynthType filteredLeftIn = lowPassFilters[v * 2].Process(leftIn + Feedback * feedbackBuffer[i * 2]);
                    delayLines[v * 2].SetDelayInSamples(delaySamplesLeft);
                    SynthType delayedSampleLeft = delayLines[v * 2].Process(filteredLeftIn);
                    leftWet += delayedSampleLeft;

                    // Right channel
                    SynthType oscOutRight = lfos[v * 2 + 1].GetSample(SampleRate);
                    SynthType combinedLfoRight = (1 - GlobalLfoAmount) * oscOutRight + GlobalLfoAmount * globalLfoValue;
                    SynthType delaySamplesRight = (AverageDelayMs + DepthMs * combinedLfoRight) * SampleRate / 1000;

                    // Apply low-pass filter before the delay line
                    lowPassFilters[v * 2 + 1].SetCutoffFrequency(FilterFrequencyHz, SampleRate);
                    SynthType filteredRightIn = lowPassFilters[v * 2 + 1].Process(rightIn + Feedback * feedbackBuffer[i * 2 + 1]);
                    delayLines[v * 2 + 1].SetDelayInSamples(delaySamplesRight);
                    SynthType delayedSampleRight = delayLines[v * 2 + 1].Process(filteredRightIn);
                    rightWet += delayedSampleRight;
                }

                leftWet /= NumVoices;
                rightWet /= NumVoices;

                // Apply feedback directly without complex processing
                SynthType leftFeedback = leftWet;
                SynthType rightFeedback = rightWet;

                // Store processed feedback
                feedbackBuffer[i * 2] = leftFeedback;
                feedbackBuffer[i * 2 + 1] = rightFeedback;

                // Mix wet and dry signals with feedback
                SynthType leftOut = WetMix * (leftWet + Feedback * leftFeedback) + (1 - WetMix) * leftIn;
                SynthType rightOut = WetMix * (rightWet + Feedback * rightFeedback) + (1 - WetMix) * rightIn;

                // Apply soft clipping to the final output
                LeftBuffer[i] = leftOut;
                RightBuffer[i] = rightOut;
            }
        }

        // Parameters with validation
        private SynthType averageDelayMs = 15.0f;
        public SynthType AverageDelayMs
        {
            get => averageDelayMs;
            set => averageDelayMs = SynthType.Clamp(value, 5.0f, 30.0f);
        }

        private SynthType depthMs = 7.0f;
        public SynthType DepthMs
        {
            get => depthMs;
            set => depthMs = SynthType.Clamp(value, 2.0f, 15.0f);
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

        private SynthType feedback = 0.2f;
        public SynthType Feedback
        {
            get => feedback;
            set => feedback = SynthType.Clamp(value, 0.0f, 0.99f); // Directly clamp feedback to safe range
        }

        private SynthType filterFrequencyHz = 20000.0f;
        public SynthType FilterFrequencyHz
        {
            get => filterFrequencyHz;
            set => filterFrequencyHz = SynthType.Clamp(value, 20.0f, 20000.0f);
        }

        private float globalLfoFrequencyHz = 0.1f;
        public float GlobalLfoFrequencyHz
        {
            get => globalLfoFrequencyHz;
            set => globalLfoFrequencyHz = float.Clamp(value, 0.0f, 1.0f);
        }

        private SynthType globalLfoAmount = 0.2f;
        public SynthType GlobalLfoAmount
        {
            get => globalLfoAmount;
            set => globalLfoAmount = SynthType.Clamp(value, 0.0f, 1.0f);
        }

        // Constants for maximum possible values (used for buffer allocation)
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
            if (lfoIndex >= 0 && lfoIndex < lfos.Length)
            {
                lfos[lfoIndex].Waveform = waveform;
            }
        }
    }
}
