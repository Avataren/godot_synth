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

        private float phase;
        public LFOWaveform CurrentWaveform { get; set; }
        public bool UseAbsoluteValue { get; set; }

        public LFONode(int numSamples, float sampleFrequency)
            : base(numSamples, sampleFrequency)
        {
            Frequency = 4.0f;
            Amplitude = 1.0f;
            CurrentWaveform = LFOWaveform.Sine;
            UseAbsoluteValue = false;
            phase = 0.0f;
        }

        private float GetNextSample(double increment)
        {
            float phaseIncrement = Frequency * 2.0f * Mathf.Pi / SampleFrequency;

            // Normalize phase to [0, 1] for the waveform methods
            float normalizedPhase = phase / (2.0f * Mathf.Pi);
            float sample = GetWaveformSample(CurrentWaveform, normalizedPhase);

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
        public static float[] GetWaveformData(LFOWaveform waveform, int bufferSize)
        {
            float[] waveformData = new float[bufferSize];
            float phaseIncrement = 1.0f / bufferSize;

            for (int i = 0; i < bufferSize; i++)
            {
                float normalizedPhase = i * phaseIncrement;
                waveformData[i] = GetWaveformSample(waveform, normalizedPhase);
            }

            return waveformData;
        }

        // Method to get the waveform sample for a given waveform type and normalized phase
        private static float GetWaveformSample(LFOWaveform waveform, float normalizedPhase)
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
        private static float GetSineWave(float normalizedPhase)
        {
            return (float)Math.Sin(normalizedPhase * 2.0f * Mathf.Pi);
        }

        // Triangle wave function
        private static float GetTriangleWave(float normalizedPhase)
        {
            return 2.0f * (float)Math.Abs(normalizedPhase * 2.0f - 1.0f) - 1.0f;
        }

        // Saw wave function
        private static float GetSawWave(float normalizedPhase)
        {
            return 2.0f * normalizedPhase - 1.0f;
        }

        // Pulse wave function
        private static float GetPulseWave(float normalizedPhase)
        {
            return normalizedPhase < 0.5f ? 1.0f : -1.0f;
        }
    }
}
