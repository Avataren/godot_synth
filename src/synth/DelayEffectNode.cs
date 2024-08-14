using System;

namespace Synth
{
    public class DelayEffectNode : AudioNode
    {
        private DelayLine leftDelayLine;
        private DelayLine rightDelayLine;

        public DelayEffectNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples, sampleFrequency)
        {
            leftDelayLine = new DelayLine(300, (int)sampleFrequency);
            rightDelayLine = new DelayLine(300, (int)sampleFrequency);
            LeftBuffer = new float[numSamples];
            RightBuffer = new float[numSamples];
        }

        public override void Process(float increment)
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
                leftDelayLine.SetDelayTime(value, (int)SampleFrequency);
                rightDelayLine.SetDelayTime(value, (int)SampleFrequency);
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
