using System;
using System.Runtime.CompilerServices;

namespace Synth
{
    public class CircularDelayLine
    {
        private readonly SynthType[] buffer;
        private int writeIndex;
        private readonly int bufferSize;
        private int readIndex;
        private SynthType fraction;
        private SynthType prevOutput;

        public CircularDelayLine(int sizeInSamples)
        {
            bufferSize = sizeInSamples;
            buffer = new SynthType[bufferSize];
            Array.Clear(buffer, 0, bufferSize);
            prevOutput = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDelayInSamples(SynthType delaySamples)
        {
            int intDelay = (int)delaySamples;
            fraction = delaySamples - intDelay;
            readIndex = (writeIndex - intDelay + bufferSize) % bufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(SynthType inputSample)
        {
            // All-pass interpolation
            int index0 = readIndex;
            int index1 = (readIndex + 1) % bufferSize;

            SynthType frac = fraction;
            SynthType a = (1 - frac) / (1 + frac);

            SynthType x0 = buffer[index0];
            SynthType x1 = buffer[index1];

            SynthType outputSample = a * (x0 - prevOutput) + x1;
            prevOutput = outputSample;

            buffer[writeIndex] = inputSample;
            writeIndex = (writeIndex + 1) % bufferSize;
            readIndex = (readIndex + 1) % bufferSize;
            return outputSample;
        }

        public void Mute()
        {
            Array.Clear(buffer, 0, bufferSize);
            prevOutput = 0;
        }
    }
}


// using System;
// using System.Runtime.CompilerServices;

// namespace Synth
// {
//     public class CircularDelayLine
//     {
//         private readonly SynthType[] buffer;
//         private int writeIndex;
//         private readonly int bufferSize;
//         private int readIndex;
//         private SynthType fraction;

//         public CircularDelayLine(int sizeInSamples)
//         {
//             bufferSize = sizeInSamples;
//             buffer = new SynthType[bufferSize];
//             Array.Clear(buffer, 0, bufferSize);
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public void SetDelayInSamples(SynthType delaySamples)
//         {
//             int intDelay = (int)delaySamples;
//             fraction = delaySamples - intDelay;
//             readIndex = (writeIndex - intDelay + bufferSize) % bufferSize;
//         }

//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public SynthType GetSample(SynthType inputSample)
//         {
//             // Cubic interpolation
//             int index0 = (readIndex - 1 + bufferSize) % bufferSize;
//             int index1 = readIndex;
//             int index2 = (readIndex + 1) % bufferSize;
//             int index3 = (readIndex + 2) % bufferSize;

//             SynthType y0 = buffer[index0];
//             SynthType y1 = buffer[index1];
//             SynthType y2 = buffer[index2];
//             SynthType y3 = buffer[index3];

//             SynthType mu = fraction;
//             SynthType mu2 = mu * mu;
//             SynthType a0 = y3 - y2 - y0 + y1;
//             SynthType a1 = y0 - y1 - a0;
//             SynthType a2 = y2 - y0;
//             SynthType a3 = y1;

//             SynthType outputSample = a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;

//             buffer[writeIndex] = inputSample;
//             writeIndex = (writeIndex + 1) % bufferSize;
//             readIndex = (readIndex + 1) % bufferSize;
//             return outputSample;
//         }

//         public void Mute()
//         {
//             Array.Clear(buffer, 0, bufferSize);
//         }
//     }

// }