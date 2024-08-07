using System;
namespace Synth
{
    public class LFONode : AudioNode
    {
        public enum LFOWaveform
        {
            Sine,
            Triangle,
            Saw,
            Pulse
        }

        private float phase;
        public LFOWaveform CurrentWaveform { get; set; }
        public EnvelopeNode ADSR { get; set; }
        public bool UseAbsoluteValue { get; set; }

        public LFONode(int numSamples, float frequency, float amplitude, LFOWaveform waveform = LFOWaveform.Sine, bool useAbsoluteValue = false)
            : base(numSamples)
        {
            ADSR = new EnvelopeNode(numSamples, true);
            ADSR.AttackTime = 0.5f;
            Frequency = frequency;
            Amplitude = amplitude;
            CurrentWaveform = waveform;
            UseAbsoluteValue = useAbsoluteValue;
            phase = 0.0f;
        }

        private float GetNextSample(float increment)
        {
            float sample = 0.0f;
            float phaseIncrement = Frequency * 2.0f * (float)Math.PI * increment;

            switch (CurrentWaveform)
            {
                case LFOWaveform.Sine:
                    sample = (float)Math.Sin(phase);
                    break;
                case LFOWaveform.Triangle:
                    sample = 2.0f * (float)Math.Abs(phase / Math.PI % 2 - 1) - 1.0f;
                    break;
                case LFOWaveform.Saw:
                    sample = (float)(2.0f * (phase / (2.0f * Math.PI) % 1.0f) - 1.0f);
                    break;
                case LFOWaveform.Pulse:
                    sample = (phase % (2.0f * Math.PI)) < Math.PI ? 1.0f : -1.0f;
                    break;
            }

            if (UseAbsoluteValue)
            {
                sample = Math.Abs(sample);
            }

            phase += phaseIncrement;
            if (phase > 2.0f * Math.PI)
                phase -= (float)(2.0f * Math.PI);

            return sample;
        }

        public override void OpenGate()
        {
            ADSR.OpenGate();
        }

        public override void CloseGate()
        {
            ADSR.CloseGate();
        }

        public override void Process(float increment)
        {
            ADSR.Process(increment);
            for (int i = 0; i < NumSamples; i++)
            {
                buffer[i] = GetNextSample(increment) * ADSR[i] * Amplitude;
            }
        }
    }
}