
using System;

namespace Synth
{
    public class WaveTableRepository
    {
        public static WaveTableMemory SquareOsc()
        {
            int tableLen = 2048;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // Zero DC and Nyquist
            freqWaveRe[0] = freqWaveRe[tableLen >> 1] = 0.0;
            // Generate only odd harmonics for a square wave
            for (int idx = 1; idx < (tableLen >> 1); idx++)
            {
                if (idx % 2 != 0) // Only odd harmonics
                {
                    freqWaveRe[idx] = 1.0 / idx;  // Amplitude for square wave
                    freqWaveRe[tableLen - idx] = -freqWaveRe[idx];  // Mirror
                }
            }

            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }

        public static WaveTableMemory SawOsc()
        {
            int tableLen = 2048;  // to give full bandwidth from 20 Hz
            int idx;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // make a sawtooth
            for (idx = 0; idx < tableLen; idx++)
            {
                freqWaveIm[idx] = 0.0;
            }
            freqWaveRe[0] = freqWaveRe[tableLen >> 1] = 0.0;
            for (idx = 1; idx < (tableLen >> 1); idx++)
            {
                freqWaveRe[idx] = 1.0 / idx;  // sawtooth spectrum
                freqWaveRe[tableLen - idx] = -freqWaveRe[idx];  // mirror
            }

            // build a wavetable oscillator
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


        public static WaveTableMemory SinOsc()
        {
            int tableLen = 2048;  // to give full bandwidth from 20 Hz
            int idx;
            double[] freqWaveRe = new double[tableLen];
            double[] freqWaveIm = new double[tableLen];

            // make a sine wave
            // DC and Nyquist are zero for sine
            for (idx = 0; idx < tableLen; idx++)
            {
                freqWaveIm[idx] = freqWaveRe[idx] = 0.0;
            }
            freqWaveIm[1] = 1;

            // build a wavetable oscillator
            var osc = new WaveTableMemory();
            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen, 0.5, true);
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
                freqWaveRe[idx] = (idx % 4 == 1) ? (1.0 / (idx * idx)) : (-1.0 / (idx * idx));
                freqWaveRe[tableLen - idx] = -freqWaveIm[idx];  // mirror for negative frequencies
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