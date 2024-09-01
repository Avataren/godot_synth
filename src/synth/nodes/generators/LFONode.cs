using System;
using Godot;

namespace Synth
{
    public class LFONode : AudioNode
    {
        LFOModel lfoModel;
        public bool UseAbsoluteValue { get; set; }
        public bool UseNormalizedValue { get; set; }
        public float Frequency
        {
            get => lfoModel.Frequency;
            set
            {
                GD.Print("Setting LFO Frequency to " + value);
                lfoModel.Frequency = value;
            }
        }

        public LFONode() : base()
        {
            lfoModel = new LFOModel(1, LFOWaveform.Sine);
            Frequency = 4.0f;
            Amplitude = 1.0f;
            UseAbsoluteValue = false;
            UseNormalizedValue = false;
        }

        public LFOWaveform CurrentWaveform
        {
            get => lfoModel.Waveform;
            set => lfoModel.Waveform = value;
        }

        public override void OpenGate()
        {
            lfoModel.Reset();
        }

        public override void Process(double _increment)
        {
            for (int i = 0; i < NumSamples; i++)
            {
                var gain = GetParameter(AudioParam.Gain, i);
                var sample = lfoModel.GetSample(SampleRate) * gain.Item2 * Amplitude;
                if (UseAbsoluteValue)
                {
                    sample = Math.Abs(sample);
                }
                if (UseNormalizedValue)
                {
                    sample = (sample + 1.0f) / 2.0f;
                }
                buffer[i] = sample;
            }
        }

        // Static method to get the full waveform data for one phase
        static public SynthType[] GetWaveformData(LFOWaveform waveform, int bufferSize)
        {
            return LFOModel.GetWaveformData(waveform, bufferSize);
        }
    }
}
