using System;
using Godot;

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
        private Comb[] combL;
        private Comb[] combR;

        // Allpass filters
        private Allpass[] allpassL;
        private Allpass[] allpassR;

        public ReverbModel(int numCombs = ReverbTunings.NumCombs, int numAllpasses = ReverbTunings.NumAllpasses)
        {
            if (numCombs > ReverbTunings.NumCombs)
            {
                GD.Print("Number of combs exceeds maximum. Setting to maximum.");
                numCombs = ReverbTunings.NumCombs;
            }
            if (numAllpasses > ReverbTunings.NumAllpasses)
            {
                GD.Print("Number of allpasses exceeds maximum. Setting to maximum.");
                numAllpasses = ReverbTunings.NumAllpasses;
            }

            // Initialize Comb filters
            combL = new Comb[numCombs];
            combR = new Comb[numCombs];

            // Initialize Allpass filters
            allpassL = new Allpass[numAllpasses];
            allpassR = new Allpass[numAllpasses];

            InitializeFilters(numCombs, numAllpasses);

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

        private void InitializeFilters(int numCombs, int numAllpasses)
        {
            int[] combTuningsL = {
                ReverbTunings.CombTuningL1, ReverbTunings.CombTuningL2, ReverbTunings.CombTuningL3, ReverbTunings.CombTuningL4,
                ReverbTunings.CombTuningL5, ReverbTunings.CombTuningL6, ReverbTunings.CombTuningL7, ReverbTunings.CombTuningL8,
                ReverbTunings.CombTuningL9, ReverbTunings.CombTuningL10, ReverbTunings.CombTuningL11, ReverbTunings.CombTuningL12,
                ReverbTunings.CombTuningL13, ReverbTunings.CombTuningL14, ReverbTunings.CombTuningL15, ReverbTunings.CombTuningL16
            };

            int[] allpassTuningsL = {
                ReverbTunings.AllpassTuningL1, ReverbTunings.AllpassTuningL2, ReverbTunings.AllpassTuningL3,
                ReverbTunings.AllpassTuningL4, ReverbTunings.AllpassTuningL5, ReverbTunings.AllpassTuningL6
            };

            for (int i = 0; i < numCombs; i++)
            {
                combL[i] = new Comb(combTuningsL[i % combTuningsL.Length]);
                combR[i] = new Comb(combTuningsL[i % combTuningsL.Length] + ReverbTunings.StereoSpread);
            }

            for (int i = 0; i < numAllpasses; i++)
            {
                allpassL[i] = new Allpass(allpassTuningsL[i % allpassTuningsL.Length]);
                allpassR[i] = new Allpass(allpassTuningsL[i % allpassTuningsL.Length] + ReverbTunings.StereoSpread);
            }
        }

        private float SoftClip(float input)
        {
            const float threshold = 0.6f;
            if (input > threshold)
                return threshold + (input - threshold) / (1.0f + Mathf.Pow(input - threshold, 2));
            else if (input < -threshold)
                return -threshold + (input + threshold) / (1.0f + Mathf.Pow(-input - threshold, 2));
            else
                return input;
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
                for (int j = 0; j < combL.Length; j++)
                {
                    outL += combL[j].Process(input);
                    outR += combR[j].Process(input);
                }

                // Feed through allpasses in series
                for (int j = 0; j < allpassL.Length; j++)
                {
                    outL = allpassL[j].Process(outL);
                    outR = allpassR[j].Process(outR);
                }

                // Calculate output REPLACING anything already there
                outputL[i * skip] = outL * wet1 + outR * wet2 + inputL[i * skip] * dry;
                outputR[i * skip] = outR * wet1 + outL * wet2 + inputR[i * skip] * dry;

                float phaseShift = 0.02f; // Adjust as needed
                outputR[i * skip] += outputL[i * skip] * phaseShift;
                outputL[i * skip] -= outputR[i * skip] * phaseShift;

                outputL[i * skip] = SoftClip(outputL[i * skip]);
                outputR[i * skip] = SoftClip(outputR[i * skip]);
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
                for (int j = 0; j < combL.Length; j++)
                {
                    outL += combL[j].Process(input);
                    outR += combR[j].Process(input);
                }

                // Feed through allpasses in series
                for (int j = 0; j < allpassL.Length; j++)
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