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
        private float drive = 1.0f;
        private float[] state = new float[4];
        private float dcBlock1, dcBlock2;
        private const int OVERSAMPLE = 2;

        public MoogFilter(float sampleFrequency = 44100.0f)
        {
            fs = sampleFrequency;
            init();
        }

        private void init()
        {
            y1 = y2 = y3 = y4 = oldx = oldy1 = oldy2 = oldy3 = 0;
            for (int i = 0; i < 4; i++) state[i] = 0;
            dcBlock1 = dcBlock2 = 0;
            calc(CutOff);
        }

        private void calc(float cutOff)
        {
            float f = (cutOff + cutOff) / (fs * OVERSAMPLE);
            p = f * (1.8f - 0.8f * f);
            k = p + p - 1f;

            float t = (1f - p) * 1.386249f;
            float t2 = 12f + t * t;
            r = res * (t2 + 6f * t) / (t2 - 6f * t);
        }

        private float tanhApprox(float x)
        {
            float x2 = x * x;
            return x * (27 + x2) / (27 + 9 * x2);
        }

        private float softClip(float x)
        {
            return x - 0.33333f * (x * x * x);
        }

        public float Process(float input, float cutoff_mod = 1.0f)
        {
            calc(CutOff * cutoff_mod);
            float outputSum = 0;

            for (int i = 0; i < OVERSAMPLE; i++)
            {
                float inputSample = i == 0 ? input : 0;  // Only use input for first iteration

                // Apply drive
                inputSample = tanhApprox(inputSample * drive);

                // Feedback
                x = inputSample - r * softClip(y4);

                // Four cascaded one-pole filters (bilinear transform)
                y1 = x * p + oldx * p - k * y1;
                y2 = y1 * p + oldy1 * p - k * y2;
                y3 = y2 * p + oldy2 * p - k * y3;
                y4 = y3 * p + oldy3 * p - k * y4;

                // Clipper band limited sigmoid
                y4 = softClip(y4);

                oldx = x;
                oldy1 = y1;
                oldy2 = y2;
                oldy3 = y3;

                outputSum += y4;
            }

            outputSum /= OVERSAMPLE;

            // DC blocker
            float outputSample = outputSum - dcBlock1 + 0.995f * dcBlock2;
            dcBlock1 = outputSum;
            dcBlock2 = outputSample;

            return outputSample;
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
                cutoff = Math.Max(20, Math.Min(20000, value));
                calc(cutoff);
            }
        }

        public float Resonance
        {
            get { return res; }
            set
            {
                res = Math.Max(0, Math.Min(1, value));
                calc(CutOff);
            }
        }
    }
}