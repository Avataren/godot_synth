using System;
using Godot;

namespace Synth
{
    public class BiquadFilter
    {
        private SynthType a0, a1, a2, b0, b1, b2;
        private SynthType prevInput1, prevInput2, prevOutput1, prevOutput2;
        private int sampleRate;
        private SynthType frequency;
        private SynthType userQ; // Q value in 0-1 range
        private SynthType dbGain;
        private FilterType type;

        private const SynthType MinFloat = 1.175494351e-38f; // Minimum positive normalized float
        private const SynthType BaselineQ = 0.707f; // Butterworth response
        private const SynthType MaxQ = 10f; // Maximum Q value

        public BiquadFilter(int sampleRate = 44100)
        {
            type = FilterType.LowPass;
            this.sampleRate = sampleRate;
            frequency = 20000;
            userQ = 0f; // Default to baseline response
            dbGain = 0;

            CalculateCoefficients(frequency);
        }

        public void SetParams(SynthType frequency, SynthType q, SynthType dbGain)
        {
            this.frequency = Mathf.Clamp(frequency, 20, sampleRate / 2);
            this.dbGain = dbGain;
            userQ = Mathf.Clamp(q, 0f, 1f); // Clamp user Q to 0-1 range
            CalculateCoefficients(frequency);
        }

        private SynthType MapQ(SynthType userQ)
        {
            // Map 0-1 to BaselineQ-MaxQ exponentially
            return BaselineQ * SynthTypeHelper.Pow(MaxQ / BaselineQ, userQ);
        }

        private void CalculateCoefficients(SynthType frequency)
        {
            var mappedQ = MapQ(userQ);
            var A = SynthTypeHelper.Pow(10, dbGain / 40);
            var omega = 2 * SynthTypeHelper.Pi * frequency / sampleRate;
            var sinOmega = SynthTypeHelper.Sin(omega);
            var cosOmega = SynthTypeHelper.Cos(omega);

            var alpha = sinOmega / (2 * mappedQ);
            var beta = SynthTypeHelper.Sqrt(A + A);

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
            if (SynthType.Abs(a0) > MinFloat)
            {
                var invA0 = SynthTypeHelper.One / a0;
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
            b0 = SynthType.IsNaN(b0) || SynthType.IsInfinity(b0) ? 0 : b0;
            b1 = SynthType.IsNaN(b1) || SynthType.IsInfinity(b1) ? 0 : b1;
            b2 = SynthType.IsNaN(b2) || SynthType.IsInfinity(b2) ? 0 : b2;
            a1 = SynthType.IsNaN(a1) || SynthType.IsInfinity(a1) ? 0 : a1;
            a2 = SynthType.IsNaN(a2) || SynthType.IsInfinity(a2) ? 0 : a2;
        }



        private SynthType previousCutoffModParam = 1f;
        private const SynthType modulationSmoothingFactor = 0.5f; // Adjust for desired smoothness

        public SynthType SmoothModulation(SynthType cutoff_mod_param)
        {
            previousCutoffModParam += modulationSmoothingFactor * (cutoff_mod_param - previousCutoffModParam);
            return previousCutoffModParam;
        }

        public SynthType Process(SynthType input, SynthType cutoff_mod_param)
        {
            var smoothedCutoffModParam = SynthTypeHelper.Max(0.01f, SmoothModulation(cutoff_mod_param));
            CalculateCoefficients(frequency * smoothedCutoffModParam);

            var output = b0 * input + b1 * prevInput1 + b2 * prevInput2
                           - a1 * prevOutput1 - a2 * prevOutput2;

            prevInput2 = prevInput1;
            prevInput1 = input;
            prevOutput2 = prevOutput1;
            prevOutput1 = output;

            // Prevent denormals
            if (SynthTypeHelper.Abs(output) < MinFloat)
                output = 0;

            return output;
        }

        public void Reset()
        {
            prevInput1 = prevInput2 = prevOutput1 = prevOutput2 = 0;
        }

        // Getter and Setter for Frequency
        public SynthType GetFrequency()
        {
            return frequency;
        }

        public void SetFrequency(SynthType value)
        {
            SetParams(value, userQ, dbGain);
        }

        public SynthType GetQ()
        {
            return userQ;
        }

        public void SetQ(SynthType value)
        {
            SetParams(frequency, value, dbGain);
        }

        // Getter and Setter for DbGain
        public SynthType GetDbGain()
        {
            return dbGain;
        }

        public void SetDbGain(SynthType value)
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