
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SynthType GetSample(double sampleRate)
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

            phase += Frequency / sampleRate;
            if (phase >= 1.0) phase -= 1.0;
            return interpolatedSample;
        }
    }
}