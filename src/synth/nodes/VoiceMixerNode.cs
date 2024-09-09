using System;
using System.Threading.Tasks;

namespace Synth
{
    public class VoiceMixerNode : AudioNode
    {
        public VoiceMixerNode() : base()
        {
            AcceptedInputType = InputType.Stereo;
            RightBuffer = new SynthType[NumSamples];
            LeftBuffer = new SynthType[NumSamples];
        }

        public void Clear()
        {
            RightBuffer.AsSpan().Clear();
            LeftBuffer.AsSpan().Clear();
        }

        public void MixIn(Voice voice)
        {
            var outputNode = voice.GetOuputNode();

            for (int i = 0; i < NumSamples; i++)
            {
                LeftBuffer[i] += outputNode.LeftBuffer[i];
                RightBuffer[i] += outputNode.RightBuffer[i];
                //LeftBuffer[i] += outputNode[i];
                //RightBuffer[i] += outputNode[i];
            }
            //Godot.GD.Print($"osc data: {LeftBuffer[0]}");
            // Parallel.For(0, NumSamples, i =>
            // {
            //     LeftBuffer[i] += outputNode.LeftBuffer[i];
            //     RightBuffer[i] += outputNode.RightBuffer[i];
            // });
        }

        public override void Process(double _increment)
        {
            // Do nothing
        }
    }
}