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

        // Buffers for the combs
        private float[] bufCombL1 = new float[ReverbTunings.CombTuningL1];
        private float[] bufCombR1 = new float[ReverbTunings.CombTuningR1];
        private float[] bufCombL2 = new float[ReverbTunings.CombTuningL2];
        private float[] bufCombR2 = new float[ReverbTunings.CombTuningR2];
        private float[] bufCombL3 = new float[ReverbTunings.CombTuningL3];
        private float[] bufCombR3 = new float[ReverbTunings.CombTuningR3];
        private float[] bufCombL4 = new float[ReverbTunings.CombTuningL4];
        private float[] bufCombR4 = new float[ReverbTunings.CombTuningR4];
        private float[] bufCombL5 = new float[ReverbTunings.CombTuningL5];
        private float[] bufCombR5 = new float[ReverbTunings.CombTuningR5];
        private float[] bufCombL6 = new float[ReverbTunings.CombTuningL6];
        private float[] bufCombR6 = new float[ReverbTunings.CombTuningR6];
        private float[] bufCombL7 = new float[ReverbTunings.CombTuningL7];
        private float[] bufCombR7 = new float[ReverbTunings.CombTuningR7];
        private float[] bufCombL8 = new float[ReverbTunings.CombTuningL8];
        private float[] bufCombR8 = new float[ReverbTunings.CombTuningR8];

        // Buffers for the allpasses
        private float[] bufAllpassL1 = new float[ReverbTunings.AllpassTuningL1];
        private float[] bufAllpassR1 = new float[ReverbTunings.AllpassTuningR1];
        private float[] bufAllpassL2 = new float[ReverbTunings.AllpassTuningL2];
        private float[] bufAllpassR2 = new float[ReverbTunings.AllpassTuningR2];
        private float[] bufAllpassL3 = new float[ReverbTunings.AllpassTuningL3];
        private float[] bufAllpassR3 = new float[ReverbTunings.AllpassTuningR3];
        private float[] bufAllpassL4 = new float[ReverbTunings.AllpassTuningL4];
        private float[] bufAllpassR4 = new float[ReverbTunings.AllpassTuningR4];

        public ReverbModel()
        {
            // Initialize Comb filters with their respective buffers
            combL[0] = new Comb(bufCombL1);
            combR[0] = new Comb(bufCombR1);
            combL[1] = new Comb(bufCombL2);
            combR[1] = new Comb(bufCombR2);
            combL[2] = new Comb(bufCombL3);
            combR[2] = new Comb(bufCombR3);
            combL[3] = new Comb(bufCombL4);
            combR[3] = new Comb(bufCombR4);
            combL[4] = new Comb(bufCombL5);
            combR[4] = new Comb(bufCombR5);
            combL[5] = new Comb(bufCombL6);
            combR[5] = new Comb(bufCombR6);
            combL[6] = new Comb(bufCombL7);
            combR[6] = new Comb(bufCombR7);
            combL[7] = new Comb(bufCombL8);
            combR[7] = new Comb(bufCombR8);

            // Initialize Allpass filters with their respective buffers
            allpassL[0] = new Allpass(bufAllpassL1);
            allpassR[0] = new Allpass(bufAllpassR1);
            allpassL[1] = new Allpass(bufAllpassL2);
            allpassR[1] = new Allpass(bufAllpassR2);
            allpassL[2] = new Allpass(bufAllpassL3);
            allpassR[2] = new Allpass(bufAllpassR3);
            allpassL[3] = new Allpass(bufAllpassL4);
            allpassR[3] = new Allpass(bufAllpassR4);

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
            get => mode >= ReverbTunings.FreezeMode ? 1 : 0;
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
                outL = outR = 0;
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
                outL = outR = 0;
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
            wet1 = wet * (width / 2 + 0.5f);
            wet2 = wet * ((1 - width) / 2);

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