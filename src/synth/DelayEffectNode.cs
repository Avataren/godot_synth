using System;

namespace Synth
{
    public class DelayEffectNode : AudioNode
    {
        private DelayLine leftDelayLine;
        private DelayLine rightDelayLine;

        public DelayEffectNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples, sampleFrequency)
        {
            leftDelayLine = new DelayLine(500, (int)sampleFrequency);
            rightDelayLine = new DelayLine(500, (int)sampleFrequency);
            LeftBuffer = new float[numSamples];
            RightBuffer = new float[numSamples];
        }

        public override void Process(float increment)
        {
            var nodes = GetParameterNodes(AudioParam.StereoInput);
            if (nodes == null || nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                if (node == null || !node.Enabled)
                    continue;

                for (int i = 0; i < NumSamples; i++)
                {
                    float sampleL = node.LeftBuffer[i];
                    float sampleR = node.RightBuffer[i];

                    LeftBuffer[i] = leftDelayLine.Process(sampleL);
                    RightBuffer[i] = rightDelayLine.Process(sampleR);
                }
            }
        }

        // public float Cutoff
        // {
        //     get { return leftFilter.Cutoff; }
        //     set
        //     {
        //         leftFilter.Cutoff = value;
        //         rightFilter.Cutoff = value; // Assuming both channels have the same cutoff
        //     }
        // }

        // public float Resonance
        // {
        //     get { return leftFilter.Resonance; }
        //     set
        //     {
        //         leftFilter.Resonance = value;
        //         rightFilter.Resonance = value; // Assuming both channels have the same resonance
        //     }
        // }

        // public float Drive
        // {
        //     get { return leftFilter.Drive; }
        //     set
        //     {
        //         leftFilter.Drive = value;
        //         rightFilter.Drive = value; // Assuming both channels have the same drive
        //     }
        // }

    }
}
