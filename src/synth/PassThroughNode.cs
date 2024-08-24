using System;

namespace Synth
{
    public class PassThroughNode : AudioNode
    {
        float _gain = 1.0f;
        public float Gain
        {
            get => _gain;
            set => _gain = value;
        }

        public PassThroughNode()
        {
            AcceptedInputType = InputType.Stereo;
            LeftBuffer = new float[NumSamples];
            RightBuffer = new float[NumSamples];
        }

        public override void Process(double increment)
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
                    LeftBuffer[i] += node.LeftBuffer[i] * Gain;
                    RightBuffer[i] += node.RightBuffer[i] * Gain;
                }
            }
        }
    }
}
