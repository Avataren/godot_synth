using System;

namespace Synth
{

    public class MixerNode : AudioNode
    {
        public MixerNode(ModulationManager ModulationMgr, int numSamples) : base(ModulationMgr, numSamples)
        {
        }

        public override void Process(float increment)
        {
            Array.Clear(buffer);
            for (int i = 0; i < NumSamples; i++)
            {
                buffer[i] = GetParameter(AudioParam.Input, i); ;
            }
        }
    }
}