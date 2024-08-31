using System;
using System.Linq;

namespace Synth
{
    public class DelayEffectNode : AudioNode
    {
        private DelayLine leftDelayLine;
        private DelayLine rightDelayLine;

        public DelayEffectNode() : base()
        {
            leftDelayLine = new DelayLine(1000, (int)SampleRate);
            rightDelayLine = new DelayLine(1000, (int)SampleRate);
            leftDelayLine.SetDelayTime(300);
            rightDelayLine.SetDelayTime(300);
            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
        }

        public override void Process(double increment)
        {
            var inputNode = GetParameterNodes(AudioParam.StereoInput).FirstOrDefault();
            if (inputNode == null)
                return;

            for (int i = 0; i < NumSamples; i++)
            {
                var sampleL = inputNode.LeftBuffer[i];
                var sampleR = inputNode.RightBuffer[i];

                LeftBuffer[i] = leftDelayLine.Process(sampleL);
                RightBuffer[i] = rightDelayLine.Process(sampleR);
            }
        }

        public void Mute()
        {
            leftDelayLine.Mute();
            rightDelayLine.Mute();
        }

        public int DelayTimeInMs
        {
            set
            {
                leftDelayLine.SetDelayTime(value);
                rightDelayLine.SetDelayTime(value);
            }
        }

        public SynthType Feedback
        {
            get { return leftDelayLine.Feedback; }
            set
            {
                Godot.GD.Print("Setting Feedback to: ", value);
                leftDelayLine.Feedback = value;
                rightDelayLine.Feedback = value;
            }
        }


        public SynthType WetMix
        {
            get { return leftDelayLine.WetMix; }
            set
            {
                leftDelayLine.WetMix = value;
                rightDelayLine.WetMix = value;
            }
        }

    }
}
