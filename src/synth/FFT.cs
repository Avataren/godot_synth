using System;
namespace Synth
{
    public class FFT
    {
        // In-place complex FFT function
        public static void fft(int N, double[] ar, double[] ai)
        {
            int i, j, k, L; // indexes
            int M, LE, LE1, ip; // M = log N
            int NV2, NM1;
            double t; // temp
            double Ur, Ui, Wr, Wi, Tr, Ti;
            double Ur_old;

            if (N <= 1 || (N & (N - 1)) != 0)  // make sure we have a power of 2
                throw new ArgumentException("N must be a power of 2");

            NV2 = N >> 1;
            NM1 = N - 1;
            M = (int)Math.Log(N, 2);

            // Bit reversal of array elements
            j = 1;
            for (i = 1; i <= NM1; i++)
            {
                if (i < j)
                {
                    // Swap ar[i-1] and ar[j-1]
                    t = ar[j - 1];
                    ar[j - 1] = ar[i - 1];
                    ar[i - 1] = t;

                    // Swap ai[i-1] and ai[j-1]
                    t = ai[j - 1];
                    ai[j - 1] = ai[i - 1];
                    ai[i - 1] = t;
                }

                k = NV2;
                while (k < j)
                {
                    j -= k;
                    k /= 2;
                }
                j += k;
            }

            // FFT computation
            LE = 1;
            for (L = 1; L <= M; L++)
            {
                LE1 = LE;
                LE <<= 1;
                Ur = 1.0;
                Ui = 0.0;
                Wr = Math.Cos(Math.PI / LE1);
                Wi = -Math.Sin(Math.PI / LE1); // negative sign for inverse FFT

                for (j = 1; j <= LE1; j++)
                {
                    for (i = j; i <= N; i += LE)
                    {
                        ip = i + LE1;
                        Tr = ar[ip - 1] * Ur - ai[ip - 1] * Ui;
                        Ti = ar[ip - 1] * Ui + ai[ip - 1] * Ur;
                        ar[ip - 1] = ar[i - 1] - Tr;
                        ai[ip - 1] = ai[i - 1] - Ti;
                        ar[i - 1] += Tr;
                        ai[i - 1] += Ti;
                    }
                    Ur_old = Ur;
                    Ur = Ur_old * Wr - Ui * Wi;
                    Ui = Ur_old * Wi + Ui * Wr;
                }
            }
        }
    }
}