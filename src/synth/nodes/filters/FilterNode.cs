using System;
using Godot;

namespace Synth
{
    public class FilterNode : AudioNode
    {
        private MoogFilter leftFilter;
        private MoogFilter rightFilter;
        public FilterNode() : base()
        {
            leftFilter = new MoogFilter(SampleRate);
            rightFilter = new MoogFilter(SampleRate);
            LeftBuffer = new float[NumSamples];
            RightBuffer = new float[NumSamples];
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

        public static double TransformToCutoff(double input, double minCutoff, double maxCutoff)
        {
            if (input < 0 || input > 1)
                throw new ArgumentOutOfRangeException(nameof(input), "Input must be between 0 and 1.");

            // Use an exponential mapping to make the transformation feel more natural
            double exponent = 3.0; // You can adjust this exponent for more or less nonlinearity
            double transformedValue = Math.Pow(input, exponent);

            // Map the transformed value to the cutoff frequency range
            double cutoffFrequency = minCutoff + transformedValue * (maxCutoff - minCutoff);

            return cutoffFrequency;
        }

        public float CutOff
        {
            get { return leftFilter.CutOff; }
            set
            {
                leftFilter.CutOff = (float)TransformToCutoff(value, 0.0, 20000.0);
                GD.Print("Cutoff: " + leftFilter.CutOff);
                rightFilter.CutOff = (float)TransformToCutoff(value,  0.0, 20000.0);
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
