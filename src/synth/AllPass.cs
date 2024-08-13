namespace Synth
{
    public class Allpass
    {
        private float feedback;
        private float[] buffer;
        private int bufSize;
        private int bufIdx;

        public Allpass(float[] allBuffer)
        {
            Buffer = allBuffer;
            bufIdx = 0;
        }

        public float[] Buffer
        {
            get => buffer;
            set
            {
                buffer = value;
                bufSize = buffer.Length;
            }
        }

        public float Feedback
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

        public float Process(float input)
        {
            float bufOut = buffer[bufIdx];
            Undenormaliser.Undenormalise(ref bufOut);

            float output = -input + bufOut;
            buffer[bufIdx] = input + (bufOut * feedback);

            if (++bufIdx >= bufSize)
            {
                bufIdx = 0;
            }

            return output;
        }
    }
}