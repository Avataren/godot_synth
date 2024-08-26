using System;

namespace Synth
{
    public class MoogFilter
    {
        private float fs;
        private float cutoff = 20000.0f;
        private float res = 0.0f;
        private float p, k, r;
        private float x, y1, y2, y3, y4;
        private float oldx, oldy1, oldy2, oldy3;
        private float drive = 1.0f; // Default drive

        public MoogFilter(float sampleFrequency = 44100.0f)
        {
            fs = sampleFrequency;
            init();
        }

        private void init()
        {
            y1 = y2 = y3 = y4 = oldx = oldy1 = oldy2 = oldy3 = 0;
            calc(CutOff);
        }

        private void calc(float cutOff)
        {
            float f = (cutOff + cutOff) / fs; //[0 - 1]
            p = f * (1.8f - 0.8f * f);
            k = p + p - 1f;

            float t = (1f - p) * 1.386249f;
            float t2 = 12f + t * t;
            r = res * (t2 + 6f * t) / (t2 - 6f * t);
        }

        public float Process(float input, float cutoff_mod = 1.0f)
        {
            calc(CutOff * cutoff_mod);
            // Apply drive with soft clipping using tanh for a more analog-like distortion
            //input = (float)Math.Tanh(input * drive);
            // Process input through the Moog filter
            x = input - r * y4;

            // Four cascaded one-pole filters (bilinear transform)
            y1 = x * p + oldx * p - k * y1;
            y2 = y1 * p + oldy1 * p - k * y2;
            y3 = y2 * p + oldy2 * p - k * y3;
            y4 = y3 * p + oldy3 * p - k * y4;

            // Clipper band limited sigmoid
            y4 -= (y4 * y4 * y4) / 6f;

            oldx = x;
            oldy1 = y1;
            oldy2 = y2;
            oldy3 = y3;

            return y4;
        }

        public float Drive
        {
            get { return drive; }
            set { drive = value; }
        }

        public float CutOff
        {
            get { return cutoff; }
            set
            {
                cutoff = value;
                calc(cutoff);
            }
        }

        public float Resonance
        {
            get { return res; }
            set
            {
                res = value;
                calc(CutOff);
            }
        }
    }
}
