using System;
using Godot;

namespace Synth
{
    public class FilterNode : AudioNode
    {
        private MoogFilter leftFilter;
        private MoogFilter rightFilter;

        private BiquadFilter leftBiQuadFilter;
        private BiquadFilter rightBiQuadFilter;

        private FilterType filterType = FilterType.MoogLowPass;

        public FilterNode() : base()
        {
            leftFilter = new MoogFilter(SampleRate);
            rightFilter = new MoogFilter(SampleRate);
            leftBiQuadFilter = new BiquadFilter((int)SampleRate);
            rightBiQuadFilter = new BiquadFilter((int)SampleRate);
            LeftBuffer = new float[NumSamples];
            RightBuffer = new float[NumSamples];
        }

        public void SetFilterType(FilterType type)
        {
            filterType = type;
            if (type != FilterType.MoogLowPass)
            {
                
                leftBiQuadFilter.SetFilterType(type);
                rightBiQuadFilter.SetFilterType(type);

                leftBiQuadFilter.Reset();
                rightBiQuadFilter.Reset();
            }
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

                switch (filterType)
                {
                    case FilterType.MoogLowPass:
                        for (int i = 0; i < NumSamples; i++)
                        {
                            var cutoff_mod_param = GetParameter(AudioParam.CutOffMod, i);
                            float sampleL = node.LeftBuffer[i];
                            float sampleR = node.RightBuffer[i];

                            float co = cutoff_mod_param.Item2;
                            LeftBuffer[i] = leftFilter.Process(sampleL, co);
                            RightBuffer[i] = rightFilter.Process(sampleR, co);
                        }
                        break;
                    default:
                        for (int i = 0; i < NumSamples; i++)
                        {
                            var cutoff_mod_param = GetParameter(AudioParam.CutOffMod, i);
                            float sampleL = node.LeftBuffer[i];
                            float sampleR = node.RightBuffer[i];

                            float co = cutoff_mod_param.Item2;
                            LeftBuffer[i] = (float)leftBiQuadFilter.Process(sampleL, cutoff_mod_param.Item2);
                            RightBuffer[i] = (float)rightBiQuadFilter.Process(sampleR,  cutoff_mod_param.Item2);
                        }
                        break;
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
            get { return leftFilter.Cutoff; }
            set
            {
                leftFilter.Cutoff = (float)TransformToCutoff(value, 0.0, SampleRate / 2.0);
                GD.Print("Cutoff: " + leftFilter.Cutoff);
                rightFilter.Cutoff = (float)TransformToCutoff(value, 0.0, SampleRate / 2.0);
                leftBiQuadFilter.SetFrequency((float)TransformToCutoff(value, 0.0, SampleRate / 2.0));
                rightBiQuadFilter.SetFrequency((float)TransformToCutoff(value, 0.0, SampleRate / 2.0));
            }
        }

        public float Resonance
        {
            get { return leftFilter.Resonance; }
            set
            {
                leftFilter.Resonance = value;
                rightFilter.Resonance = value; // Assuming both channels have the same resonance
                leftBiQuadFilter.SetQ(value);
                rightBiQuadFilter.SetQ(value);

            }
        }

        // public float Drive
        // {
        //     get { return leftFilter.Drive; }
        //     set
        //     {
        //         leftFilter.Drive = value;
        //         rightFilter.Drive = value; // Assuming both channels have the same drive
        //         leftBiQuadFilter.SetDbGain(MapDriveTodBGain(value));
        //         rightBiQuadFilter.SetDbGain(MapDriveTodBGain(value));
        //     }
        // }

        private float MapDriveTodBGain(double drive)
        {
            // Map drive 1-20 to dBGain range -6 to +24 dB
            // Using a logarithmic scaling for more natural response
            double normalizedDrive = (drive - 1) / 19; // 0 to 1
            return (float)(30.0 * Math.Log10(normalizedDrive * 19 + 1) - 6.0);
        }        

    }
}
