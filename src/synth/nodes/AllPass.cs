namespace Synth
{
    public class Allpass
    {
        private SynthType feedback;
        private SynthType[] buffer;
        private int bufSize;
        private int bufIdx;

        public Allpass(int bufSize)
        {
            Buffer = new SynthType[bufSize];
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

        public SynthType Feedback
        {
            get => feedback;
            set => feedback = value;
        }

        public void Mute()
        {
            for (int i = 0; i < bufSize; i++)
            {
                buffer[i] = 0;
            }
        }

        public SynthType Process(SynthType input)
        {
            SynthType bufOut = buffer[bufIdx];
            //Undenormaliser.Undenormalise(ref bufOut);

            SynthType output = -input + bufOut;
            buffer[bufIdx] = input + (bufOut * feedback);

            if (++bufIdx >= bufSize)
            {
                bufIdx = 0;
            }

            return output;
        }
    }
}