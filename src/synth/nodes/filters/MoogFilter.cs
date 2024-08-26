using System;
using Godot;

namespace Synth
{
    public class MoogFilter
    {
        private float sampleRate;
        private float cutoff = 1.0f;
        private float resonance = 0.0f;
        private float p, k, r;
        private float x, y1, y2, y3, y4;
        private float oldx, oldy1, oldy2, oldy3;

        public MoogFilter(float sampleFrequency = 44100.0f)
        {
            sampleRate = sampleFrequency;
            Init();
        }

        private void Init()
        {
            y1 = y2 = y3 = y4 = oldx = oldy1 = oldy2 = oldy3 = 0;
            Calc(Cutoff);
        }

        private void Calc(float cutoff)
        {
            const float Pi = 3.1415926535897931f;
            p = cutoff * (1.8f - 0.8f * cutoff);
            k = 2.0f * (float)Math.Sin(cutoff * Pi * 0.5f) - 1.0f;

            float t1 = (1.0f - p) * 1.386249f;
            float t2 = 12.0f + t1 * t1;
            r = resonance * (t2 + 6.0f * t1) / (t2 - 6.0f * t1);
        }

        public float Process(float input, float cutoff_mod)
        {
            float modulatedCutoff = cutoff * cutoff_mod; // Calculate modulated cutoff
            Calc(modulatedCutoff); // Recalculate filter coefficients with the new cutoff

            x = input - r * y4;
            y1 = x * p + oldx * p - k * y1;
            y2 = y1 * p + oldy1 * p - k * y2;
            y3 = y2 * p + oldy2 * p - k * y3;
            y4 = y3 * p + oldy3 * p - k * y4;

            // Clipper band limited sigmoid
            y4 -= (y4 * y4 * y4) / 6.0f;

            oldx = x; oldy1 = y1; oldy2 = y2; oldy3 = y3;

            return y4;
        }

        public float SampleRate
        {
            get => sampleRate;
            set { sampleRate = value; Calc(Cutoff); }
        }

        public float Cutoff
        {
            get => cutoff;
            set
            {
                float normalizedCutoff = value / (sampleRate / 2); // Normalize cutoff to 0-1
                cutoff = Math.Max(0, Math.Min(1, normalizedCutoff)); // Clamp to [0,1]
                Calc(cutoff);
            }
        }

        public float Resonance
        {
            get => resonance;
            set { resonance = value; Calc(Cutoff); }
        }
    }
}
