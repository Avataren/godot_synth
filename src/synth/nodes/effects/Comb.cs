using System;
using Godot;

namespace Synth
{

    public class Comb
    {
        private SynthType feedback;
        private SynthType filterStore;
        private SynthType damp1;
        private SynthType damp2;
        private SynthType[] buffer;
        private int bufSize;
        private int bufIdx;

        public Comb(int combSize)
        {
            Buffer = new SynthType[combSize];
            filterStore = SynthTypeHelper.Zero;
            bufIdx = 0;
        }

        public SynthType[] Buffer
        {
            get => buffer;
            set
            {
                buffer = value;
                bufSize = buffer.Length;
            }
        }

        public SynthType Damp
        {
            get => damp1;
            set
            {
                damp1 = value;
                damp2 = SynthTypeHelper.One - value;
            }
        }

        public SynthType Feedback
        {
            get => feedback;
            set => feedback = value;
        }

        public void Mute()
        {
            for (int i = 0; i < bufSize; i++)
            {
                buffer[i] = SynthTypeHelper.Zero;
            }
        }

        public SynthType Process(SynthType input)
        {
            SynthType output = buffer[bufIdx];
            //Undenormaliser.Undenormalise(ref output);

            filterStore = (output * damp2) + (filterStore * damp1);
            //Undenormaliser.Undenormalise(ref filterStore);

            buffer[bufIdx] = input + (filterStore * feedback);

            if (++bufIdx >= bufSize)
            {
                bufIdx = 0;
            }

            return output;
        }
    }
}