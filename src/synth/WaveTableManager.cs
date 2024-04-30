
using System;
namespace Synth
{
    public class WaveTableManager
    {
        public static int FillTables(WaveTableMemory mem, double[] freqWaveRe, double[] freqWaveIm, int numSamples)
        {
            int idx;
            // Zero DC offset and Nyquist
            freqWaveRe[0] = 0.0;
            freqWaveIm[0] = 0.0;
            freqWaveRe[numSamples >> 1] = 0.0;
            freqWaveIm[numSamples >> 1] = 0.0;

            // Determine the highest non-zero harmonic in the wave
            int maxHarmonic = numSamples >> 1;
            const double minVal = 0.000001; // threshold for determining significant harmonics (-120 dB)
            while (maxHarmonic > 0 && (Math.Abs(freqWaveRe[maxHarmonic]) + Math.Abs(freqWaveIm[maxHarmonic]) < minVal))
                --maxHarmonic;

            // Calculate the top frequency for the initial wave table
            //double topFreq = 2.0 / 3.0 / maxHarmonic; // Allowing for some aliasing
                                                      // double topFreq = 1.0 / (2.0 * maxHarmonic); // Without aliasing
            double topFreq = 1.0 / (2.0 * maxHarmonic); // Without aliasing

            // Prepare for subsequent tables: double topFreq and remove upper half of harmonics
            double[] ar = new double[numSamples];
            double[] ai = new double[numSamples];
            double scale = 0.0;
            int numTables = 0;

            while (maxHarmonic > 0)
            {
                // Zero the arrays
                Array.Clear(ar, 0, numSamples);
                Array.Clear(ai, 0, numSamples);

                // Copy the needed harmonics into the new arrays
                for (idx = 1; idx <= maxHarmonic; idx++)
                {
                    ar[idx] = freqWaveRe[idx];
                    ai[idx] = freqWaveIm[idx];
                    ar[numSamples - idx] = freqWaveRe[numSamples - idx];
                    ai[numSamples - idx] = freqWaveIm[numSamples - idx];
                }

                // Make the wave table
                scale = MakeWaveTable(mem, numSamples, ar, ai, scale, topFreq);
                numTables++;

                // Prepare for the next table
                topFreq *= 2;
                maxHarmonic >>= 1;
            }
            return numTables;
        }

        public static float MakeWaveTable(WaveTableMemory mem, int len, double[] ar, double[] ai, double scale, double topFreq)
        {
            // Apply FFT on the array
            FFT.fft(len, ar, ai);

            // Calculate normalization scale if not provided
            if (scale == 0.0)
            {
                double max = 0;
                for (int idx = 0; idx < len; idx++)
                {
                    double temp = Math.Abs(ai[idx]);
                    if (max < temp)
                        max = temp;
                }
                scale = 1.0 / max * 0.999;
            }

            // Normalize and convert to float
            float[] wave = new float[len];
            for (int idx = 0; idx < len; idx++)
            {
                wave[idx] = (float)(ai[idx] * scale);
            }

            // Add wave table to memory, reset scale if successful
            if (mem.AddWaveTable(len, wave, topFreq) > 0)
            {
                scale = 0.0;
            }
            else
            {
                Godot.GD.Print("Failed to add wave table: " + topFreq);
            }

            return (float)scale;
        }
    }
}