using System;
using Godot;

namespace Synth
{
    public class BiquadFilter
    {
        private float a0, a1, a2, b0, b1, b2;
        private float prevInput1, prevInput2, prevOutput1, prevOutput2;
        private int sampleRate;
        private float frequency;
        private float userQ; // Q value in 0-1 range
        private float dbGain;
        private FilterType type;

        private const float MinFloat = 1.175494351e-38f; // Minimum positive normalized float
        private const float BaselineQ = 0.707f; // Butterworth response
        private const float MaxQ = 10f; // Maximum Q value

        public BiquadFilter(int sampleRate = 44100)
        {
            type = FilterType.LowPass;
            this.sampleRate = sampleRate;
            frequency = 20000;
            userQ = 0f; // Default to baseline response
            dbGain = 0;

            CalculateCoefficients(frequency);
        }

        public void SetParams(float frequency, float q, float dbGain)
        {
            this.frequency = Math.Clamp(frequency, 20, sampleRate / 2);
            this.dbGain = dbGain;
            userQ = Math.Clamp(q, 0f, 1f); // Clamp user Q to 0-1 range
            CalculateCoefficients(frequency);
        }

        private float MapQ(float userQ)
        {
            // Map 0-1 to BaselineQ-MaxQ exponentially
            return BaselineQ * (float)Math.Pow(MaxQ / BaselineQ, userQ);
        }

        private void CalculateCoefficients(float frequency)
        {
            float mappedQ = MapQ(userQ);
            float A = (float)Math.Pow(10, dbGain / 40);
            float omega = 2 * (float)Math.PI * frequency / sampleRate;
            float sinOmega = (float)Math.Sin(omega);
            float cosOmega = (float)Math.Cos(omega);

            float alpha = sinOmega / (2 * mappedQ);
            float beta = (float)Math.Sqrt(A + A);

            switch (type)
            {
                case FilterType.LowPass:
                    b0 = (1 - cosOmega) / 2;
                    b1 = 1 - cosOmega;
                    b2 = (1 - cosOmega) / 2;
                    a0 = 1 + alpha;
                    a1 = -2 * cosOmega;
                    a2 = 1 - alpha;
                    break;
                case FilterType.HighPass:
                    b0 = (1 + cosOmega) / 2;
                    b1 = -(1 + cosOmega);
                    b2 = (1 + cosOmega) / 2;
                    a0 = 1 + alpha;
                    a1 = -2 * cosOmega;
                    a2 = 1 - alpha;
                    break;
                case FilterType.BandPass:
                    b0 = alpha;
                    b1 = 0;
                    b2 = -alpha;
                    a0 = 1 + alpha;
                    a1 = -2 * cosOmega;
                    a2 = 1 - alpha;
                    break;
                case FilterType.Notch:
                    b0 = 1;
                    b1 = -2 * cosOmega;
                    b2 = 1;
                    a0 = 1 + alpha;
                    a1 = -2 * cosOmega;
                    a2 = 1 - alpha;
                    break;
                case FilterType.Peak:
                    b0 = 1 + alpha * A;
                    b1 = -2 * cosOmega;
                    b2 = 1 - alpha * A;
                    a0 = 1 + alpha / A;
                    a1 = -2 * cosOmega;
                    a2 = 1 - alpha / A;
                    break;
                case FilterType.LowShelf:
                    b0 = A * ((A + 1) - (A - 1) * cosOmega + beta * sinOmega);
                    b1 = 2 * A * ((A - 1) - (A + 1) * cosOmega);
                    b2 = A * ((A + 1) - (A - 1) * cosOmega - beta * sinOmega);
                    a0 = (A + 1) + (A - 1) * cosOmega + beta * sinOmega;
                    a1 = -2 * ((A - 1) + (A + 1) * cosOmega);
                    a2 = (A + 1) + (A - 1) * cosOmega - beta * sinOmega;
                    break;
                case FilterType.HighShelf:
                    b0 = A * ((A + 1) + (A - 1) * cosOmega + beta * sinOmega);
                    b1 = -2 * A * ((A - 1) + (A + 1) * cosOmega);
                    b2 = A * ((A + 1) + (A - 1) * cosOmega - beta * sinOmega);
                    a0 = (A + 1) - (A - 1) * cosOmega + beta * sinOmega;
                    a1 = 2 * ((A - 1) - (A + 1) * cosOmega);
                    a2 = (A + 1) - (A - 1) * cosOmega - beta * sinOmega;
                    break;
            }

            // Normalize coefficients
            if (Math.Abs(a0) > MinFloat)
            {
                float invA0 = 1.0f / a0;
                b0 *= invA0;
                b1 *= invA0;
                b2 *= invA0;
                a1 *= invA0;
                a2 *= invA0;
            }
            else
            {
                // Handle potential instability
                b0 = b1 = b2 = a1 = a2 = 0;
            }

            // Ensure coefficients are not NaN or Infinity
            b0 = float.IsNaN(b0) || float.IsInfinity(b0) ? 0 : b0;
            b1 = float.IsNaN(b1) || float.IsInfinity(b1) ? 0 : b1;
            b2 = float.IsNaN(b2) || float.IsInfinity(b2) ? 0 : b2;
            a1 = float.IsNaN(a1) || float.IsInfinity(a1) ? 0 : a1;
            a2 = float.IsNaN(a2) || float.IsInfinity(a2) ? 0 : a2;
        }



        private float previousCutoffModParam = 1f;
        private const float modulationSmoothingFactor = 0.5f; // Adjust for desired smoothness

        public float SmoothModulation(float cutoff_mod_param)
        {
            previousCutoffModParam += modulationSmoothingFactor * (cutoff_mod_param - previousCutoffModParam);
            return previousCutoffModParam;
        }

        public float Process(float input, float cutoff_mod_param)
        {
            float smoothedCutoffModParam = Mathf.Max(0.01f, SmoothModulation(cutoff_mod_param));
            CalculateCoefficients(frequency * smoothedCutoffModParam);

            float output = b0 * input + b1 * prevInput1 + b2 * prevInput2
                           - a1 * prevOutput1 - a2 * prevOutput2;

            prevInput2 = prevInput1;
            prevInput1 = input;
            prevOutput2 = prevOutput1;
            prevOutput1 = output;

            // Prevent denormals
            if (Math.Abs(output) < MinFloat)
                output = 0;

            return output;
        }

        public void Reset()
        {
            prevInput1 = prevInput2 = prevOutput1 = prevOutput2 = 0;
        }

        // Getter and Setter for Frequency
        public float GetFrequency()
        {
            return frequency;
        }

        public void SetFrequency(float value)
        {
            SetParams(value, userQ, dbGain);
        }

        public float GetQ()
        {
            return userQ;
        }

        public void SetQ(float value)
        {
            SetParams(frequency, value, dbGain);
        }

        // Getter and Setter for DbGain
        public float GetDbGain()
        {
            return dbGain;
        }

        public void SetDbGain(float value)
        {
            SetParams(frequency, userQ, value);
        }

        // Getter and Setter for FilterType
        public FilterType GetFilterType()
        {
            return type;
        }

        public void SetFilterType(FilterType value)
        {
            if (type != value)
            {
                GD.Print("Setting filter type to " + value);
                type = value;
                Reset(); // Reset the filter state when changing types
                CalculateCoefficients(frequency);
            }
        }

        // Existing SampleRate property
        public int SampleRate
        {
            get { return sampleRate; }
            set
            {
                sampleRate = value;
                CalculateCoefficients(frequency);
            }
        }
    }
}