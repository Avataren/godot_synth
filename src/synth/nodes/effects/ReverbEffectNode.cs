using System;
using Godot;

namespace Synth
{
    public class ReverbEffectNode : AudioNode
    {

        ReverbModel reverbModel;
        public SynthType[] LeftBufferTmp;
        public SynthType[] RightBufferTmp;
        public ReverbEffectNode() : base()
        {
            AcceptedInputType = InputType.Stereo;
            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
            LeftBufferTmp = new SynthType[NumSamples];
            RightBufferTmp = new SynthType[NumSamples];
            reverbModel = new ReverbModel(12, 4);
        }

        public override void Process(double increment)
        {
            var inputs = GetParameterNodes(AudioParam.StereoInput);
            if (inputs == null || inputs.Count == 0)
                return;
            Array.Clear(LeftBufferTmp, 0, NumSamples);
            Array.Clear(RightBufferTmp, 0, NumSamples);

            foreach (var node in inputs)
            {
                if (node == null || !node.Enabled)
                    continue;
                for (int i = 0; i < NumSamples; i++)
                {
                    LeftBufferTmp[i] += node.LeftBuffer[i];
                    RightBufferTmp[i] += node.RightBuffer[i];
                }
            }
            reverbModel.ProcessReplace(LeftBufferTmp, RightBufferTmp, LeftBuffer, RightBuffer, NumSamples, 1);
        }

        public void Mute()
        {
            reverbModel.Mute();
        }

        public SynthType RoomSize
        {
            get => reverbModel.RoomSize;
            set
            {
                reverbModel.RoomSize = Mathf.Sqrt(value);
            }
        }

        public SynthType Damp
        {
            get => reverbModel.Damp;
            set
            {
                reverbModel.Damp = value;
            }
        }

        public SynthType Wet
        {
            get => reverbModel.Wet;
            set
            {
                reverbModel.Wet = value;
            }
        }

        public SynthType Dry
        {
            get => reverbModel.Dry;
            set
            {
                reverbModel.Dry = value;
            }
        }

        public SynthType Width
        {
            get => reverbModel.Width;
            set
            {
                reverbModel.Width = value;
            }
        }

    }
}
