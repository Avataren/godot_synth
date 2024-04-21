
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
                freqWaveRe[idx] = (idx % 4 == 1) ? (1.0 / (idx * idx)) : (-1.0 / (idx * idx));
                freqWaveRe[tableLen - idx] = -freqWaveIm[idx];  // mirror for negative frequencies
            }

            var osc = new WaveTableMemory();

            WaveTableManager.FillTables(osc, freqWaveRe, freqWaveIm, tableLen);
            return osc;
        }
    }
}