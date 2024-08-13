using System;

namespace Synth
{
    public class PassThroughNode : AudioNode
    {
        public PassThroughNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples, sampleFrequency)
        {
            LeftBuffer = new float[numSamples];
            RightBuffer = new float[numSamples];
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
        }
    }
}
