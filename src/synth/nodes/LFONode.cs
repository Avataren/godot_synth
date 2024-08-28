using System;
using Godot;

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

        private SynthType phase;
        public LFOWaveform CurrentWaveform { get; set; }
        public bool UseAbsoluteValue { get; set; }

        public LFONode() : base()
        {
            Frequency = 4.0f;
            Amplitude = 1.0f;
            CurrentWaveform = LFOWaveform.Sine;
            UseAbsoluteValue = false;
            phase = 0.0f;
        }

        private SynthType GetNextSample(double increment)
        {
            SynthType phaseIncrement = Frequency * 2.0f * SynthTypeHelper.Pi / SampleRate;

            // Normalize phase to [0, 1] for the waveform methods
            SynthType normalizedPhase = phase / (2.0f * Mathf.Pi);
            SynthType sample = GetWaveformSample(CurrentWaveform, normalizedPhase);

            if (UseAbsoluteValue)
            {
                sample = Math.Abs(sample);
            }

            phase += phaseIncrement;
            if (phase > 2.0f * SynthTypeHelper.Pi)
                phase -= 2.0f * SynthTypeHelper.Pi;

            return sample;
        }

        public override void OpenGate()
        {
            //ADSR.OpenGate();
            phase = 0.0f;
        }

        public override void CloseGate()
        {
            //ADSR.CloseGate();
        }

        public override void Process(double increment)
        {
            for (int i = 0; i < NumSamples; i++)
            {
                buffer[i] = GetNextSample(increment) * Amplitude;
            }
        }

        // Static method to get the full waveform data for one phase
        public static SynthType[] GetWaveformData(LFOWaveform waveform, int bufferSize)
        {
            SynthType[] waveformData = new SynthType[bufferSize];
            SynthType phaseIncrement = 1.0f / bufferSize;

            for (int i = 0; i < bufferSize; i++)
            {
                SynthType normalizedPhase = i * phaseIncrement;
                waveformData[i] = GetWaveformSample(waveform, normalizedPhase);
            }

            return waveformData;
        }

        // Method to get the waveform sample for a given waveform type and normalized phase
        private static SynthType GetWaveformSample(LFOWaveform waveform, SynthType normalizedPhase)
        {
            switch (waveform)
            {
                case LFOWaveform.Sine:
                    return GetSineWave(normalizedPhase);
                case LFOWaveform.Triangle:
                    return GetTriangleWave(normalizedPhase);
                case LFOWaveform.Saw:
                    return GetSawWave(normalizedPhase);
                case LFOWaveform.Pulse:
                    return GetPulseWave(normalizedPhase);
                default:
                    throw new ArgumentOutOfRangeException(nameof(waveform), waveform, null);
            }
        }

        // Sine wave function
        private static SynthType GetSineWave(SynthType normalizedPhase)
        {
            return SynthType.Sin(normalizedPhase * 2.0f * SynthTypeHelper.Pi);
        }

        // Triangle wave function
        private static SynthType GetTriangleWave(SynthType normalizedPhase)
        {
            return 2.0f * SynthType.Abs(normalizedPhase * 2.0f - SynthTypeHelper.One) - SynthTypeHelper.One;
        }

        // Saw wave function
        private static SynthType GetSawWave(SynthType normalizedPhase)
        {
            return 2.0f * normalizedPhase - SynthTypeHelper.One;
        }

        // Pulse wave function
        private static SynthType GetPulseWave(SynthType normalizedPhase)
        {
            return normalizedPhase < SynthTypeHelper.Half ? SynthTypeHelper.One : -SynthTypeHelper.One;
        }
    }
}
