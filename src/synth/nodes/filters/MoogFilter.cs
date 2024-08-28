using System;
using Godot;

namespace Synth
{
    public class MoogFilter
    {
        private SynthType sampleRate;
        private SynthType cutoff = 1.0f;
        private SynthType resonance = 0.0f;
        private SynthType p, k, r;
        private SynthType x, y1, y2, y3, y4;
        private SynthType oldx, oldy1, oldy2, oldy3;

        public MoogFilter(SynthType sampleFrequency = 44100.0f)
        {
            sampleRate = sampleFrequency;
            Init();
        }

        private void Init()
        {
            y1 = y2 = y3 = y4 = oldx = oldy1 = oldy2 = oldy3 = 0;
            Calc(Cutoff);
        }

        private void Calc(SynthType cutoff)
        {

            p = cutoff * (1.8f - 0.8f * cutoff);
            k = 2.0f * SynthType.Sin(cutoff * SynthTypeHelper.Pi * SynthTypeHelper.Half) - SynthTypeHelper.One;

            SynthType t1 = (1.0f - p) * 1.386249f;
            SynthType t2 = 12.0f + t1 * t1;
            r = resonance * (t2 + 6.0f * t1) / (t2 - 6.0f * t1);
        }

        public SynthType Process(SynthType input, SynthType cutoff_mod)
        {
            var modulatedCutoff = cutoff * cutoff_mod; // Calculate modulated cutoff
            Calc(modulatedCutoff); // Recalculate filter coefficients with the new cutoff

            x = input - r * y4;
            y1 = x * p + oldx * p - k * y1;
            y2 = y1 * p + oldy1 * p - k * y2;
            y3 = y2 * p + oldy2 * p - k * y3;
            y4 = y3 * p + oldy3 * p - k * y4;

            // Clipper band limited sigmoid
            y4 -= (y4 * y4 * y4) / 6.0f;
            // Apply a soft limiter to prevent blow-up
            y4 = SynthType.Max(-3.0f, Math.Min(3.0f, y4));
            //y4 = (float)Math.Tanh(y4);
            oldx = x; oldy1 = y1; oldy2 = y2; oldy3 = y3;

            return y4;
        }

        public SynthType SampleRate
        {
            get => sampleRate;
            set { sampleRate = value; Calc(Cutoff); }
        }

        public SynthType Cutoff
        {
            get => cutoff;
            set
            {
                SynthType normalizedCutoff = value / (sampleRate / 2); // Normalize cutoff to 0-1
                cutoff = SynthTypeHelper.Max(SynthTypeHelper.Zero, SynthTypeHelper.Min(SynthTypeHelper.One, normalizedCutoff)); // Clamp to [0,1]
                Calc(cutoff);
            }
        }

        public SynthType Resonance
        {
            get => resonance;
            set { resonance = value; Calc(Cutoff); }
        }
    }
}
