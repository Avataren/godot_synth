using System;
using Godot;

namespace Synth
{
    public class ReverbEffectNode : AudioNode
    {

        ReverbModel reverbModel;
        public ReverbEffectNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples, sampleFrequency)
        {
            LeftBuffer = new float[numSamples];
            RightBuffer = new float[numSamples];
            reverbModel = new ReverbModel();
        }

        public override void Process(float increment)
        {
            var inputs = GetParameterNodes(AudioParam.StereoInput);
            if (inputs == null || inputs.Count == 0)
                return;
            Array.Clear(LeftBuffer, 0, NumSamples);
            Array.Clear(RightBuffer, 0, NumSamples);

            foreach (var node in inputs)
            {
                if (node == null || !node.Enabled)
                    continue;
                    for (int i = 0; i < NumSamples; i++)
                    {
                        LeftBuffer[i] += node.LeftBuffer[i];
                        RightBuffer[i] += node.RightBuffer[i];
                    }
            }
            reverbModel.ProcessMix(LeftBuffer, RightBuffer, LeftBuffer, RightBuffer, NumSamples, 1);
        }

        public void Mute()
        {
            reverbModel.Mute();
        }

        public float RoomSize
        {
            get => reverbModel.RoomSize;
            set
            {
                reverbModel.RoomSize = Mathf.Pow(value,0.5f);
            }
        }

        public float Damp
        {
            get => reverbModel.Damp;
            set
            {
                reverbModel.Damp = value;
            }
        }

        public float Wet
        {
            get => reverbModel.Wet;
            set
            {
                reverbModel.Wet = value;
            }
        }

        public float Dry
        {
            get => reverbModel.Dry;
            set
            {
                reverbModel.Dry = value;
            }
        }

        public float Width
        {
            get => reverbModel.Width;
            set
            {
                reverbModel.Width = value;
            }
        }


        // public int DelayTimeInMs
        // {
        //     set
        //     {
        //         leftDelayLine.SetDelayTime(value, (int)SampleFrequency);
        //         rightDelayLine.SetDelayTime(value, (int)SampleFrequency);
        //     }
        // }

        // public float Feedback
        // {
        //     get { return leftDelayLine.Feedback; }
        //     set
        //     {
        //         Godot.GD.Print("Setting Feedback to: ", value);
        //         leftDelayLine.Feedback = value;
        //         rightDelayLine.Feedback = value;
        //     }
        // }

        // public float DryMix
        // {
        //     get { return leftDelayLine.DryMix; }
        //     set
        //     {
        //         leftDelayLine.DryMix = value;
        //         rightDelayLine.DryMix = value;
        //     }
        // }

        // public float WetMix
        // {
        //     get { return leftDelayLine.WetMix; }
        //     set
        //     {
        //         leftDelayLine.WetMix = value;
        //         rightDelayLine.WetMix = value;
        //     }
        // }

    }
}
