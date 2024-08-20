using System;

namespace Synth
{
    public class MoogFilterNode : AudioNode
    {
        private MoogFilter leftFilter;
        private MoogFilter rightFilter;
        public MoogFilterNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples, sampleFrequency)
        {
            leftFilter = new MoogFilter(sampleFrequency);
            rightFilter = new MoogFilter(sampleFrequency);
            LeftBuffer = new float[numSamples];
            RightBuffer = new float[numSamples];
        }

        public override void Process(double increment)
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
                    var cutoff_mod_param = GetParameter(AudioParam.CutOffMod, i);
                    float sampleL = node.LeftBuffer[i];
                    float sampleR = node.RightBuffer[i];

                    float gain = cutoff_mod_param.Item2;
                    LeftBuffer[i] = leftFilter.Process(sampleL, gain);
                    RightBuffer[i] = rightFilter.Process(sampleR, gain);
                }
            }
        }

        public float CutOff
        {
            get { return leftFilter.CutOff; }
            set
            {
                leftFilter.CutOff = value;
                rightFilter.CutOff = value; // Assuming both channels have the same cutoff
            }
        }

        public float Resonance
        {
            get { return leftFilter.Resonance; }
            set
            {
                leftFilter.Resonance = value;
                rightFilter.Resonance = value; // Assuming both channels have the same resonance
            }
        }

        public float Drive
        {
            get { return leftFilter.Drive; }
            set
            {
                leftFilter.Drive = value;
                rightFilter.Drive = value; // Assuming both channels have the same drive
            }
        }

    }
}
