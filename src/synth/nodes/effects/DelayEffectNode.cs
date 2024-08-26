using System;

namespace Synth
{
    public class DelayEffectNode : AudioNode
    {
        private DelayLine leftDelayLine;
        private DelayLine rightDelayLine;

        public DelayEffectNode() : base()
        {
            leftDelayLine = new DelayLine(300, (int)SampleRate);
            rightDelayLine = new DelayLine(300, (int)SampleRate);
            LeftBuffer = new float[NumSamples];
            RightBuffer = new float[NumSamples];
        }

        public override void Process(double increment)
        {
            var inputs = GetParameterNodes(AudioParam.StereoInput);
            if (inputs == null || inputs.Count == 0)
                return;
                
            foreach (var node in inputs)
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

        public void Mute()
        {
            leftDelayLine.Mute();
            rightDelayLine.Mute();
        }

        public int DelayTimeInMs
        {
            set
            {
                leftDelayLine.SetDelayTime(value, (int)SampleRate);
                rightDelayLine.SetDelayTime(value, (int)SampleRate);
            }
        }

        public float Feedback
        {
            get { return leftDelayLine.Feedback; }
            set
            {
                Godot.GD.Print("Setting Feedback to: ", value);
                leftDelayLine.Feedback = value;
                rightDelayLine.Feedback = value;
            }
        }

        public float DryMix
        {
            get { return leftDelayLine.DryMix; }
            set
            {
                leftDelayLine.DryMix = value;
                rightDelayLine.DryMix = value;
            }
        }

        public float WetMix
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