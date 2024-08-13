namespace Synth
{

    public class Comb
    {
        private float feedback;
        private float filterStore;
        private float damp1;
        private float damp2;
        private float[] buffer;
        private int bufSize;
        private int bufIdx;

        public Comb(float[] combBuffer)
        {
            Buffer = combBuffer;

            filterStore = 0;
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

        public float Damp
        {
            get => damp1;
            set
            {
                damp1 = value;
                damp2 = 1 - value;
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
            float output = buffer[bufIdx];
            Undenormaliser.Undenormalise(ref output);

            filterStore = (output * damp2) + (filterStore * damp1);
            Undenormaliser.Undenormalise(ref filterStore);

            buffer[bufIdx] = input + (filterStore * feedback);

            if (++bufIdx >= bufSize)
            {
                bufIdx = 0;
            }

            return output;
        }
    }
}