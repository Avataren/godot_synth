using System;

namespace Synth
{

    public class MixerNode : AudioNode
    {
        public MixerNode(int numSamples, float sampleFrequency) : base(numSamples)
        {
            SampleFrequency = sampleFrequency;
        }

        public override void Process(float increment)
        {
            Array.Clear(buffer);
            for (int i = 0; i < NumSamples; i++)
            {
                buffer[i] = GetParameter(AudioParam.Input, i) * GetParameter(AudioParam.Gain, i);
            }
        }
    }
}