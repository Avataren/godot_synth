
using System;
using System.Runtime.CompilerServices;

namespace Synth
{
    public enum LFOWaveform
    {
        Sine,
        Triangle,
        Square,
        Saw
    }

    public class LFOModel
    {
        private static class SharedLFOTables
        {
            public const int TableSize = 4096;
            public const int TableMask = TableSize - 1;

            public static readonly SynthType[] SineTable = new SynthType[TableSize];
            public static readonly SynthType[] TriangleTable = new SynthType[TableSize];
            public static readonly SynthType[] SquareTable = new SynthType[TableSize];
            public static readonly SynthType[] SawTable = new SynthType[TableSize];

            static SharedLFOTables()
            {
                for (int i = 0; i < TableSize; i++)
                {
                    double phase = 2 * Math.PI * i / TableSize;
                    SineTable[i] = SynthType.Sin(phase);
                    TriangleTable[i] = 2 * SynthType.Abs(2 * (i / (double)TableSize) - 1) - 1;
                    SquareTable[i] = i < TableSize / 2 ? 1 : -1;
                    SawTable[i] = 2 * (i / (double)TableSize) - 1;
                }
            }
        }

        private double phase = 0.0;
        public double Phase => phase;
        private float frequency;
        public float PhaseOffset { get; set; }
        public LFOWaveform Waveform { get; set; }

        public float Frequency
        {
            get => frequency;
            set => frequency = Math.Max(0.0f, value);
        }

        public LFOModel(float initialFrequency, LFOWaveform waveform = LFOWaveform.Saw, float phaseOffset = 0.0f)
        {
            Frequency = initialFrequency;
            Waveform = waveform;
            PhaseOffset = phaseOffset;
        }

        public void Reset()
        {
            phase = 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(double sampleRate)
        {
            // Calculate the phase increment for this sample
            double phaseIncrement = Frequency / sampleRate;

            // Apply oversampling for anti-aliasing
            const int oversampleFactor = 4;
            SynthType accumulatedSample = 0;

            for (int i = 0; i < oversampleFactor; i++)
            {
                double tableIndex = (phase + PhaseOffset) * SharedLFOTables.TableSize;
                int index1 = (int)tableIndex & SharedLFOTables.TableMask;
                int index2 = (index1 + 1) & SharedLFOTables.TableMask;
                float fraction = (float)(tableIndex - Math.Floor(tableIndex));

                var currentTable = Waveform switch
                {
                    LFOWaveform.Sine => SharedLFOTables.SineTable,
                    LFOWaveform.Triangle => SharedLFOTables.TriangleTable,
                    LFOWaveform.Square => SharedLFOTables.SquareTable,
                    LFOWaveform.Saw => SharedLFOTables.SawTable,
                    _ => SharedLFOTables.SineTable
                };

                SynthType sample1 = currentTable[index1];
                SynthType sample2 = currentTable[index2];
                SynthType interpolatedSample = sample1 + (sample2 - sample1) * fraction;

                accumulatedSample += interpolatedSample;

                // Increment phase for each oversample
                phase += phaseIncrement / oversampleFactor;
                if (phase >= 1.0) phase -= 1.0;
            }

            // Return the average of oversampled values
            return accumulatedSample / oversampleFactor;
        }

        public static SynthType[] GetWaveformData(LFOWaveform waveform, int bufferSize)
        {
            SynthType[] buffer = new SynthType[bufferSize];
            double phaseIncrement = 1.0 / bufferSize;
            double phase = 0.0;

            SynthType[] waveformTable = waveform switch
            {
                LFOWaveform.Sine => SharedLFOTables.SineTable,
                LFOWaveform.Triangle => SharedLFOTables.TriangleTable,
                LFOWaveform.Square => SharedLFOTables.SquareTable,
                LFOWaveform.Saw => SharedLFOTables.SawTable,
                _ => SharedLFOTables.SineTable,
            };

            for (int i = 0; i < bufferSize; i++)
            {
                double tableIndex = phase * SharedLFOTables.TableSize;
                int index1 = (int)tableIndex & SharedLFOTables.TableMask;
                int index2 = (index1 + 1) & SharedLFOTables.TableMask;
                float fraction = (float)(tableIndex - Math.Floor(tableIndex));

                SynthType sample1 = waveformTable[index1];
                SynthType sample2 = waveformTable[index2];
                SynthType interpolatedSample = sample1 + (sample2 - sample1) * fraction;

                buffer[i] = interpolatedSample;

                phase += phaseIncrement;
                if (phase >= 1.0) phase -= 1.0;
            }

            return buffer;
        }
    }
}