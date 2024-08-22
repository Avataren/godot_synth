
using System;

namespace Synth
{
    public class WaveTableRepository
    {
        public static WaveTableMemory SquareOsc()
        {
            int tableLen = 2048;
            double[] timeWave = new double[tableLen];
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Generate a square wave directly in the time domain
            for (int i = 0; i < tableLen; i++)
            {
                timeWave[i] = i < tableLen / 2 ? 1.0 : -1.0; // Square waveform: first half 1.0, second half -1.0
            }

            // Perform FFT to convert time-domain waveform to frequency domain
            FFT.fft(tableLen, timeWave, freqWaveIm);

            // Copy results to freqWaveRe (cosine components)
            Array.Copy(timeWave, freqWaveRe, tableLen);

            // Zero out the DC and Nyquist components for stability
            freqWaveRe[0] = 0.0;
            freqWaveIm[0] = 0.0;
            freqWaveRe[tableLen >> 1] = 0.0;
            freqWaveIm[tableLen >> 1] = 0.0;

            // Build the wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }


        public static WaveTableMemory SawOsc()
        {
            int tableLen = 2048;
            double[] timeWave = new double[tableLen];
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Generate an inverted sawtooth wave directly in the time domain
            for (int i = 0; i < tableLen; i++)
            {
                timeWave[i] = -2.0 * (i / (double)tableLen) + 1.0; // Inverted sawtooth waveform from 1.0 to -1.0
            }

            // Perform FFT to convert time-domain waveform to frequency domain
            FFT.fft(tableLen, timeWave, freqWaveIm);

            // Copy results to freqWaveRe (cosine components)
            Array.Copy(timeWave, freqWaveRe, tableLen);

            // Zero out the DC and Nyquist components for stability
            freqWaveRe[0] = 0.0;
            freqWaveIm[0] = 0.0;
            freqWaveRe[tableLen >> 1] = 0.0;
            freqWaveIm[tableLen >> 1] = 0.0;

            // Build the wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }



        public static WaveTableMemory CustomHarmonicWave(Func<int, double> amplitudeFunc)
        {
            int tableLen = 2048;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Generate the frequency domain representation using the provided amplitude function
            for (int harmonic = 1; harmonic < tableLen / 2; harmonic++)
            {
                double amplitude = amplitudeFunc(harmonic);
                freqWaveIm[harmonic] = amplitude;
            }

            // Build a wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }

        public static WaveTableMemory SuperSaw(int numSawtooths, double detuneAmount)
        {
            int tableLen = 2048;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Generate multiple detuned sawtooth waves
            for (int n = 0; n < numSawtooths; n++)
            {
                double detune = (n - numSawtooths / 2.0) * detuneAmount;
                for (int harmonic = 1; harmonic < tableLen / 2; harmonic++)
                {
                    double amplitude = 1.0 / harmonic;
                    double phaseShift = detune * harmonic * 2 * Math.PI / tableLen;
                    freqWaveIm[harmonic] += amplitude * Math.Sin(phaseShift);
                    freqWaveRe[harmonic] += amplitude * Math.Cos(phaseShift);
                }
            }

            // Build a wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }


        public static WaveTableMemory Noise()
        {
            int tableLen = 2048;  // to give full bandwidth from 20 Hz
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];
            Random random = new Random();

            // Generate white noise in the frequency domain
            for (int idx = 0; idx < tableLen; idx++)
            {
                // Generate random values for both real and imaginary parts
                freqWaveRe[idx] = random.NextDouble() * 2.0 - 1.0; // Random value between -1 and 1
                freqWaveIm[idx] = random.NextDouble() * 2.0 - 1.0; // Random value between -1 and 1
            }


            // DC component (idx = 0) and Nyquist frequency (idx = tableLen / 2) should be zero
            freqWaveRe[0] = freqWaveIm[0] = 0.0;
            freqWaveRe[tableLen / 2] = freqWaveIm[tableLen / 2] = 0.0;

            // Build a wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }

        public static WaveTableMemory AbsSineOsc()
        {
            int tableLen = 2048;  // to give full bandwidth from 20 Hz
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Generate harmonics for abs(sine) in the frequency domain
            for (int idx = 0; idx < tableLen; idx++)
            {
                freqWaveRe[idx] = 0.0;
                freqWaveIm[idx] = 0.0;
            }

            // Harmonic components for abs(sine)
            // abs(sine) can be approximated by the sum of sine and its even harmonics:
            // sin(x) - (1/3) * sin(3x) + (1/5) * sin(5x) - ...

            int numHarmonics = tableLen / 2;
            for (int h = 1; h < numHarmonics; h += 2) // Only odd harmonics
            {
                double amplitude = 4.0 / (Math.PI * h * h); // amplitude falls off with the square of the harmonic
                freqWaveIm[h] = -amplitude;
                freqWaveIm[tableLen - h] = amplitude;  // Symmetrical
            }

            // Build the wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveIm, freqWaveRe, tableLen, 0.0, false);
            return osc;
        }




        public static WaveTableMemory SinOsc()
        {
            int tableLen = 2048;  // to give full bandwidth from 20 Hz
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Make a sine wave in frequency domain
            // Set the real part to zero and the imaginary part to non-zero at the first harmonic
            // To start at zero crossing, we set freqWaveRe[1] to a value and leave freqWaveIm[1] as zero
            for (int idx = 0; idx < tableLen; idx++)
            {
                freqWaveRe[idx] = freqWaveRe[idx] = 0.0;
            }
            freqWaveIm[1] = -0.5 * tableLen; // Setting this creates a sine wave that starts at zero crossing

            // Build a wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }

        public static WaveTableMemory TriangleOsc()
        {
            int tableLen = 2048;
            int idx;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Initialize arrays to zeros
            for (idx = 0; idx < tableLen; idx++)
            {
                freqWaveRe[idx] = 0.0;
                freqWaveIm[idx] = 0.0;
            }

            // Generate triangle wave using its Fourier expansion
            for (idx = 1; idx <= (tableLen >> 1); idx += 2)
            {
                freqWaveIm[idx] = (idx % 4 == 1) ? (1.0 / (idx * idx)) : (-1.0 / (idx * idx));
                freqWaveIm[tableLen - idx] = -freqWaveIm[idx];  // mirror for negative frequencies
            }

            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }

        public static WaveTableMemory PeriodicWaveOsc(double[] Reals, double[] Imags)
        {
            int TableLen = 2048;
            // Pad arrays with zeros up tableLen
            double[] paddedReals = new double[TableLen];
            double[] paddedImags = new double[TableLen];
            // Copy original arrays to the new larger arrays
            Array.Copy(Reals, paddedReals, Math.Min(Reals.Length, TableLen));
            Array.Copy(Imags, paddedImags, Math.Min(Imags.Length, TableLen));
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, paddedImags, paddedReals, TableLen);
            return osc;
        }
    }
}