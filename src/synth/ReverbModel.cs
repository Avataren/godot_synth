using System;

namespace Synth
{

    public class ReverbModel
    {
        private float gain;
        private float roomSize, roomSize1;
        private float damp, damp1;
        private float wet, wet1, wet2;
        private float dry;
        private float width;
        private float mode;

        // Comb filters
        private Comb[] combL = new Comb[ReverbTunings.NumCombs];
        private Comb[] combR = new Comb[ReverbTunings.NumCombs];

        // Allpass filters
        private Allpass[] allpassL = new Allpass[ReverbTunings.NumAllpasses];
        private Allpass[] allpassR = new Allpass[ReverbTunings.NumAllpasses];

        public ReverbModel()
        {
            // Initialize Comb filters with their respective buffers
            combL[0] = new Comb(ReverbTunings.CombTuningL1);
            combR[0] = new Comb(ReverbTunings.CombTuningR1);
            combL[1] = new Comb(ReverbTunings.CombTuningL2);
            combR[1] = new Comb(ReverbTunings.CombTuningR1);
            combL[2] = new Comb(ReverbTunings.CombTuningL3);
            combR[2] = new Comb(ReverbTunings.CombTuningR3);
            combL[3] = new Comb(ReverbTunings.CombTuningL4);
            combR[3] = new Comb(ReverbTunings.CombTuningR4);
            combL[4] = new Comb(ReverbTunings.CombTuningL5);
            combR[4] = new Comb(ReverbTunings.CombTuningR1);
            combL[5] = new Comb(ReverbTunings.CombTuningL6);
            combR[5] = new Comb(ReverbTunings.CombTuningR6);
            combL[6] = new Comb(ReverbTunings.CombTuningL7);
            combR[6] = new Comb(ReverbTunings.CombTuningR7);
            combL[7] = new Comb(ReverbTunings.CombTuningL8);
            combR[7] = new Comb(ReverbTunings.CombTuningR8);

            combL[8] = new Comb(ReverbTunings.CombTuningL9);
            combR[8] = new Comb(ReverbTunings.CombTuningR9);
            combL[9] = new Comb(ReverbTunings.CombTuningL10);
            combR[9] = new Comb(ReverbTunings.CombTuningR10);
            combL[10] = new Comb(ReverbTunings.CombTuningL11);
            combR[10] = new Comb(ReverbTunings.CombTuningR11);
            combL[11] = new Comb(ReverbTunings.CombTuningL12);
            combR[11] = new Comb(ReverbTunings.CombTuningR12);


            // Initialize Allpass filters with their respective buffers
            allpassL[0] = new Allpass(ReverbTunings.AllpassTuningL1);
            allpassR[0] = new Allpass(ReverbTunings.AllpassTuningR1);
            allpassL[1] = new Allpass(ReverbTunings.AllpassTuningL2);
            allpassR[1] = new Allpass(ReverbTunings.AllpassTuningR2);
            allpassL[2] = new Allpass(ReverbTunings.AllpassTuningL3);
            allpassR[2] = new Allpass(ReverbTunings.AllpassTuningR3);
            allpassL[3] = new Allpass(ReverbTunings.AllpassTuningL4);
            allpassR[3] = new Allpass(ReverbTunings.AllpassTuningR4);

            allpassL[4] = new Allpass(ReverbTunings.AllpassTuningL5);
            allpassR[4] = new Allpass(ReverbTunings.AllpassTuningR5);

            allpassL[5] = new Allpass(ReverbTunings.AllpassTuningL6);
            allpassR[5] = new Allpass(ReverbTunings.AllpassTuningR6);

            // Set default values
            foreach (var allpass in allpassL)
                allpass.Feedback = 0.5f;
            foreach (var allpass in allpassR)
                allpass.Feedback = 0.5f;

            Wet = ReverbTunings.InitialWet;
            RoomSize = ReverbTunings.InitialRoom;
            Dry = ReverbTunings.InitialDry;
            Damp = ReverbTunings.InitialDamp;
            Width = ReverbTunings.InitialWidth;
            Mode = ReverbTunings.InitialMode;

            // Mute buffers
            Mute();
        }

        public float RoomSize
        {
            get => (roomSize - ReverbTunings.OffsetRoom) / ReverbTunings.ScaleRoom;
            set
            {
                roomSize = (value * ReverbTunings.ScaleRoom) + ReverbTunings.OffsetRoom;
                Update();
            }
        }

        public float Damp
        {
            get => damp / ReverbTunings.ScaleDamp;
            set
            {
                damp = value * ReverbTunings.ScaleDamp;
                Update();
            }
        }

        public float Wet
        {
            get => wet / ReverbTunings.ScaleWet;
            set
            {
                wet = value * ReverbTunings.ScaleWet;
                Update();
            }
        }

        public float Dry
        {
            get => dry / ReverbTunings.ScaleDry;
            set => dry = value * ReverbTunings.ScaleDry;
        }

        public float Width
        {
            get => width;
            set
            {
                width = value;
                Update();
            }
        }

        public float Mode
        {
            get => mode >= ReverbTunings.FreezeMode ? 1.0f : 0.0f;
            set
            {
                mode = value;
                Update();
            }
        }

        public void Mute()
        {
            if (Mode >= ReverbTunings.FreezeMode)
                return;

            foreach (var comb in combL)
                comb.Mute();
            foreach (var comb in combR)
                comb.Mute();

            foreach (var allpass in allpassL)
                allpass.Mute();
            foreach (var allpass in allpassR)
                allpass.Mute();
        }

        public void ProcessReplace(float[] inputL, float[] inputR, float[] outputL, float[] outputR, long numSamples, int skip)
        {
            float outL, outR, input;

            for (long i = 0; i < numSamples; i++)
            {
                outL = outR = 0.0f;
                input = (inputL[i * skip] + inputR[i * skip]) * gain;

                // Accumulate comb filters in parallel
                for (int j = 0; j < ReverbTunings.NumCombs; j++)
                {
                    outL += combL[j].Process(input);
                    outR += combR[j].Process(input);
                }

                // Feed through allpasses in series
                for (int j = 0; j < ReverbTunings.NumAllpasses; j++)
                {
                    outL = allpassL[j].Process(outL);
                    outR = allpassR[j].Process(outR);
                }

                // Calculate output REPLACING anything already there
                outputL[i * skip] = outL * wet1 + outR * wet2 + inputL[i * skip] * dry;
                outputR[i * skip] = outR * wet1 + outL * wet2 + inputR[i * skip] * dry;
            }
        }

        public void ProcessMix(float[] inputL, float[] inputR, float[] outputL, float[] outputR, long numSamples, int skip)
        {
            float outL, outR, input;

            for (long i = 0; i < numSamples; i++)
            {
                outL = outR = 0.0f;
                input = (inputL[i * skip] + inputR[i * skip]) * gain;

                // Accumulate comb filters in parallel
                for (int j = 0; j < ReverbTunings.NumCombs; j++)
                {
                    outL += combL[j].Process(input);
                    outR += combR[j].Process(input);
                }

                // Feed through allpasses in series
                for (int j = 0; j < ReverbTunings.NumAllpasses; j++)
                {
                    outL = allpassL[j].Process(outL);
                    outR = allpassR[j].Process(outR);
                }

                // Calculate output MIXING with anything already there
                outputL[i * skip] += outL * wet1 + outR * wet2 + inputL[i * skip] * dry;
                outputR[i * skip] += outR * wet1 + outL * wet2 + inputR[i * skip] * dry;
            }
        }

        private void Update()
        {
            wet1 = wet * (width / 2.0f + 0.5f);
            wet2 = wet * ((1.0f - width) / 2.0f);

            if (mode >= ReverbTunings.FreezeMode)
            {
                roomSize1 = 1;
                damp1 = 0;
                gain = ReverbTunings.Muted;
            }
            else
            {
                roomSize1 = roomSize;
                damp1 = damp;
                gain = ReverbTunings.FixedGain;
            }

            foreach (var comb in combL)
            {
                comb.Feedback = roomSize1;
                comb.Damp = damp1;
            }

            foreach (var comb in combR)
            {
                comb.Feedback = roomSize1;
                comb.Damp = damp1;
            }
        }
    }
}