using System;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class FilterWithTransforms
    {
        private const int MAX_STAGES = 8;
        private const int MOOG_STAGES = 4;
        private const int LOOKUP_TABLE_SIZE = 2048;
        private const int OVERSAMPLE = 2; // Oversampling factor for Moog filter
        private const double MOOG_VT = 0.000025; // Thermal voltage, lower value for softer clipping

        public enum FilterType
        {
            LowPass,
            HighPass,
            BandPass,
            Notch,
            MoogLadder,
            StateVariable
        }

        public enum FilterSlope
        {
            Slope_6dB = 6,
            Slope_12dB = 12,
            Slope_18dB = 18,
            Slope_24dB = 24,
            Slope_30dB = 30,
            Slope_36dB = 36,
            Slope_42dB = 42,
            Slope_48dB = 48
        }

        private readonly double sampleRate;
        private double cutoff;
        private double resonance;
        private FilterSlope slope;
        private FilterType currentType;
        private double saturation;
        private bool isEnabled = true;

        private readonly BiquadStage[] stages;
        private int activeStages;
        private readonly double[] moogStage;
        private readonly double[] moogDelay;
        private double moogAlpha, moogBeta, moogGamma;
        private double cutoffStart, cutoffEnd;
        private double resonanceStart, resonanceEnd;
        private double saturationStart, saturationEnd;

        private readonly double[] svf; // State Variable Filter states

        private static readonly float[] TanhTable;

        // Additional fields for Moog filter
        private double[] moogState = new double[4];
        private double[] moogStageIn = new double[4];
        private double[] moogStageOut = new double[4];
        private double moogDrive = 1.0;

        static FilterWithTransforms()
        {
            TanhTable = new float[LOOKUP_TABLE_SIZE];
            for (int i = 0; i < LOOKUP_TABLE_SIZE; i++)
            {
                float x = (i / (float)(LOOKUP_TABLE_SIZE - 1)) * 8 - 4;
                TanhTable[i] = (float)Math.Tanh(x);
            }
        }

        public FilterWithTransforms(double sampleRate)
        {
            this.sampleRate = sampleRate;
            stages = new BiquadStage[MAX_STAGES];
            for (int i = 0; i < MAX_STAGES; i++)
            {
                stages[i] = new BiquadStage();
            }
            moogStage = new double[MOOG_STAGES];
            moogDelay = new double[MOOG_STAGES];
            svf = new double[3];

            SetFilterType(FilterType.LowPass);
            SetParameters(0.5, 0.5, FilterSlope.Slope_12dB);
        }

        // Properties
        public FilterType CurrentFilterType
        {
            get => currentType;
            set => SetFilterType(value);
        }

        public double Cutoff
        {
            get => InverseCutoffTransform(cutoff);
            set => SetCutoff(value);
        }

        public double Resonance
        {
            get => InverseResonanceTransform(resonance);
            set => SetResonance(value);
        }

        public FilterSlope Slope
        {
            get => slope;
            set => SetSlope(value);
        }

        public double Saturation
        {
            get => InverseSaturationTransform(saturation);
            set => SetSaturation(value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        public double Drive
        {
            get => moogDrive;
            set => moogDrive = Math.Clamp(value, 0.1, 5.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFilterType(FilterType type)
        {
            currentType = type;
            CalculateCoefficients();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCutoff(double userValue)
        {
            cutoff = CutoffTransform(userValue);
            CalculateCoefficients();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResonance(double userValue)
        {
            resonance = ResonanceTransform(userValue);
            CalculateCoefficients();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSlope(FilterSlope newSlope)
        {
            slope = newSlope;
            activeStages = (int)slope / 6;
            CalculateCoefficients();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSaturation(double userValue)
        {
            saturation = SaturationTransform(userValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetParameters(double userCutoff, double userResonance, FilterSlope newSlope)
        {
            cutoff = CutoffTransform(userCutoff);
            resonance = ResonanceTransform(userResonance);
            SetSlope(newSlope);
        }

        // Transformation functions
        private double CutoffTransform(double userValue)
        {
            return 20 * Math.Pow(1000, Math.Clamp(userValue, 0, 1));
        }

        private double InverseCutoffTransform(double internalValue)
        {
            return Math.Clamp(Math.Log(internalValue / 20) / Math.Log(1000), 0, 1);
        }

        private double ResonanceTransform(double userValue)
        {
            return Math.Pow(10, Math.Clamp(userValue, 0, 1) * 2) / 100;
        }

        private double InverseResonanceTransform(double internalValue)
        {
            return Math.Clamp(Math.Log10(internalValue * 100) / 2, 0, 1);
        }

        private double SaturationTransform(double userValue)
        {
            return Math.Clamp(userValue, 0, 1) * Math.Clamp(userValue, 0, 1);
        }

        private double InverseSaturationTransform(double internalValue)
        {
            return Math.Clamp(Math.Sqrt(internalValue), 0, 1);
        }

        private void CalculateCoefficients()
        {
            switch (currentType)
            {
                case FilterType.MoogLadder:
                    CalculateMoogCoefficients();
                    break;
                case FilterType.StateVariable:
                    CalculateSVFCoefficients();
                    break;
                default:
                    CalculateBiquadCoefficients();
                    break;
            }
        }

        private void CalculateBiquadCoefficients()
        {
            double omega = 2 * Math.PI * cutoff / sampleRate;
            double sinOmega = Math.Sin(omega);
            double cosOmega = Math.Cos(omega);
            double alpha = sinOmega / (2 * resonance);

            double a0 = 1 + alpha;
            double a1 = -2 * cosOmega;
            double a2 = 1 - alpha;
            double b0 = 0, b1 = 0, b2 = 0;

            switch (currentType)
            {
                case FilterType.LowPass:
                    b0 = (1 - cosOmega) / 2;
                    b1 = 1 - cosOmega;
                    b2 = (1 - cosOmega) / 2;
                    break;
                case FilterType.HighPass:
                    b0 = (1 + cosOmega) / 2;
                    b1 = -(1 + cosOmega);
                    b2 = (1 + cosOmega) / 2;
                    break;
                case FilterType.BandPass:
                    b0 = alpha;
                    b1 = 0;
                    b2 = -alpha;
                    break;
                case FilterType.Notch:
                    b0 = 1;
                    b1 = -2 * cosOmega;
                    b2 = 1;
                    break;
            }

            double scale = 1 / a0;
            for (int i = 0; i < activeStages; i++)
            {
                stages[i].SetCoefficients(b0 * scale, b1 * scale, b2 * scale, a1 * scale, a2 * scale);
            }
        }

        private void CalculateMoogCoefficients()
        {
            double fc = Math.Clamp(cutoff, 0, sampleRate / 2);
            double f = fc / sampleRate;
            double k = 4.0 * (resonance) * (1 - 0.15 * f * f);

            moogAlpha = 1.0 - Math.Exp(-2.0 * Math.PI * f);
            moogBeta = 4.0 * resonance * (1.0 - 0.15 * f * f);
            moogGamma = 1.0 / (1.0 + k);
        }

        private void CalculateSVFCoefficients()
        {
            double w = 2 * Math.PI * cutoff / sampleRate;
            double q = 1 / resonance;
            double r = 2 * q;
            moogAlpha = Math.Sin(w) / r;
            moogBeta = (r - moogAlpha) / (1 + moogAlpha);
        }

        public void ProcessBuffer(ReadOnlySpan<float> inputBuffer, Span<float> outputBuffer)
        {
            if (!isEnabled)
            {
                inputBuffer.CopyTo(outputBuffer);
                return;
            }

            int bufferSize = Math.Min(inputBuffer.Length, outputBuffer.Length);

            // Prepare interpolation values
            cutoffStart = cutoffEnd;
            cutoffEnd = cutoff;
            resonanceStart = resonanceEnd;
            resonanceEnd = resonance;
            saturationStart = saturationEnd;
            saturationEnd = saturation;

            switch (currentType)
            {
                case FilterType.MoogLadder:
                    ProcessBufferMoog(inputBuffer, outputBuffer, bufferSize);
                    break;
                case FilterType.StateVariable:
                    ProcessBufferSVF(inputBuffer, outputBuffer, bufferSize);
                    break;
                default:
                    ProcessBufferBiquad(inputBuffer, outputBuffer, bufferSize);
                    break;
            }
        }

        private void ProcessBufferBiquad(ReadOnlySpan<float> inputBuffer, Span<float> outputBuffer, int bufferSize)
        {
            for (int i = 0; i < bufferSize; i++)
            {
                double input = inputBuffer[i];
                double output = input;

                // Parameter interpolation
                double t = i / (double)bufferSize;
                double currentCutoff = cutoffStart + (cutoffEnd - cutoffStart) * t;
                double currentResonance = resonanceStart + (resonanceEnd - resonanceStart) * t;

                if (Math.Abs(currentCutoff - cutoff) > 0.01 || Math.Abs(currentResonance - resonance) > 0.001)
                {
                    SetParameters(InverseCutoffTransform(currentCutoff), InverseResonanceTransform(currentResonance), slope);
                }

                for (int j = 0; j < activeStages; j++)
                {
                    output = stages[j].Process(output);
                }

                outputBuffer[i] = (float)ProcessWithSaturation(output, t);
            }
        }

        private void ProcessBufferMoog(ReadOnlySpan<float> inputBuffer, Span<float> outputBuffer, int bufferSize)
        {
            for (int i = 0; i < bufferSize; i++)
            {
                double input = inputBuffer[i];

                // Parameter interpolation
                double t = i / (double)bufferSize;
                double currentCutoff = cutoffStart + (cutoffEnd - cutoffStart) * t;
                double currentResonance = resonanceStart + (resonanceEnd - resonanceStart) * t;

                if (Math.Abs(currentCutoff - cutoff) > 0.01 || Math.Abs(currentResonance - resonance) > 0.001)
                {
                    SetParameters(InverseCutoffTransform(currentCutoff), InverseResonanceTransform(currentResonance), slope);
                    CalculateMoogCoefficients();
                }

                // Apply drive
                input *= 1.0 + moogDrive * 3;

                // Feedback
                input -= moogBeta * moogDelay[3];
                input *= moogGamma;

                // Cascade of 4 one-pole filters
                for (int j = 0; j < 4; j++)
                {
                    moogStage[j] = moogDelay[j] + moogAlpha * (MoogSaturate(input) - moogDelay[j]);
                    input = moogStage[j];
                    moogDelay[j] = moogStage[j];
                }

                double output = moogStage[3];
                output = ProcessWithSaturation(output, t);

                outputBuffer[i] = (float)output;
            }
        }

        private void ProcessBufferSVF(ReadOnlySpan<float> inputBuffer, Span<float> outputBuffer, int bufferSize)
        {
            for (int i = 0; i < bufferSize; i++)
            {
                double input = inputBuffer[i];

                // Parameter interpolation
                double t = i / (double)bufferSize;
                double currentCutoff = cutoffStart + (cutoffEnd - cutoffStart) * t;
                double currentResonance = resonanceStart + (resonanceEnd - resonanceStart) * t;

                if (Math.Abs(currentCutoff - cutoff) > 0.01 || Math.Abs(currentResonance - resonance) > 0.001)
                {
                    SetParameters(InverseCutoffTransform(currentCutoff), InverseResonanceTransform(currentResonance), slope);
                }

                double high = input - moogBeta * svf[0] - svf[1];
                double band = moogAlpha * high + svf[0];
                double low = moogAlpha * band + svf[1];

                svf[0] = band;
                svf[1] = low;

                double output = currentType switch
                {
                    FilterType.LowPass => low,
                    FilterType.HighPass => high,
                    FilterType.BandPass => band,
                    FilterType.Notch => low + high,
                    _ => low,
                };

                outputBuffer[i] = (float)ProcessWithSaturation(output, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ProcessWithSaturation(double input, double t)
        {
            double currentSaturation = saturationStart + (saturationEnd - saturationStart) * t;
            if (currentSaturation > 0)
            {
                double saturatedInput = FastTanh(input * (1 + 4 * currentSaturation)) / FastTanh(1 + 4 * currentSaturation);
                return input + currentSaturation * (saturatedInput - input);
            }
            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double MoogSaturate(double x)
        {
            return x / (1 + Math.Abs(x / (1.5 * MOOG_VT)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FastTanh(double x)
        {
            x = Math.Max(-4, Math.Min(4, x));
            int index = (int)((x + 4) * (LOOKUP_TABLE_SIZE - 1) / 8);
            return TanhTable[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FastExp(double x)
        {
            x = 1.0 + x / 256.0;
            x *= x; x *= x; x *= x; x *= x;
            x *= x; x *= x; x *= x; x *= x;
            return x;
        }

        public void Reset()
        {
            foreach (var stage in stages)
            {
                stage.Reset();
            }
            Array.Clear(moogStage, 0, MOOG_STAGES);
            Array.Clear(moogDelay, 0, MOOG_STAGES);
            Array.Clear(svf, 0, 3);
            Array.Clear(moogState, 0, moogState.Length);
            cutoffStart = cutoffEnd = cutoff;
            resonanceStart = resonanceEnd = resonance;
            saturationStart = saturationEnd = saturation;
        }

        private class BiquadStage
        {
            private readonly double[] coeffs = new double[5];
            private readonly double[] state = new double[2];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetCoefficients(double b0, double b1, double b2, double a1, double a2)
            {
                coeffs[0] = b0;
                coeffs[1] = b1;
                coeffs[2] = b2;
                coeffs[3] = a1;
                coeffs[4] = a2;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double Process(double input)
            {
                double output = coeffs[0] * input + state[0];
                state[0] = coeffs[1] * input - coeffs[3] * output + state[1];
                state[1] = coeffs[2] * input - coeffs[4] * output;
                return output;
            }

            public void Reset()
            {
                state[0] = state[1] = 0;
            }
        }
    }
}