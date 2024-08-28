using System;
using System.Linq;
using Godot;

namespace Synth
{
    public class FuzzNode : AudioNode
    {
        public SynthType InputGain { get; set; } = 10.0f;
        public SynthType OutputGain { get; set; } = 0.5f;
        public SynthType Mix { get; set; } = 0.8f;
        public SynthType LowPassCutoff { get; set; } = 0.3f;
        public SynthType Bias { get; set; } = 0.1f;
        public SynthType FeedbackAmount { get; set; } = 0.2f;
        public SynthType StereoSpread { get; set; } = 0.02f;  // Amount of stereo spread effect

        private SynthType _previousFilteredSampleLeft = 0.0f;
        private SynthType _previousFilteredSampleRight = 0.0f;
        private SynthType _feedbackLeft = 0.0f;
        private SynthType _feedbackRight = 0.0f;

        public FuzzNode() : base()
        {
            AcceptedInputType = InputType.Stereo;
            LeftBuffer = new SynthType[NumSamples];
            RightBuffer = new SynthType[NumSamples];
        }

        public override void Process(double increment)
        {
            var node = GetParameterNodes(AudioParam.StereoInput).FirstOrDefault();
            if (node == null || !node.Enabled)
            {
                return;
            }

            SynthType[] inputLeftBuffer = node.LeftBuffer;
            SynthType[] inputRightBuffer = node.RightBuffer;

            for (int i = 0; i < NumSamples; i++)
            {
                // Process left and right channels with stereo spread
                var leftInputSample = inputLeftBuffer[i];
                var rightInputSample = inputRightBuffer[i];

                // Apply stereo spread by slightly altering the left and right channels
                var spreadedLeft = leftInputSample * (1.0f - StereoSpread);
                var spreadedRight = rightInputSample * (1.0f + StereoSpread);

                // Process the left and right channels
                LeftBuffer[i] = ProcessSample(spreadedLeft, ref _previousFilteredSampleLeft, ref _feedbackLeft);
                RightBuffer[i] = ProcessSample(spreadedRight, ref _previousFilteredSampleRight, ref _feedbackRight);
            }
        }

        private SynthType ProcessSample(SynthType inputSample, ref SynthType previousFilteredSample, ref SynthType feedback)
        {
            // Apply initial input gain and feedback
            var boostedSignal = (inputSample + feedback * FeedbackAmount) * InputGain;

            // Apply dynamic bias
            boostedSignal += Bias * Mathf.Sin(Time.GetTicksMsec() * 0.001f);

            // Multi-stage distortion
            var stage1 = SoftClipping(boostedSignal);
            var stage2 = AsymmetricSoftClipping(stage1);
            var stage3 = PolynomialShaping(stage2);

            // Advanced waveshaping for added complexity
            var shapedSignal = ComplexWaveshaping(stage3);

            // Low-pass filter to smooth out harsh frequencies
            var filteredSignal = lowPassFilter(shapedSignal, previousFilteredSample, LowPassCutoff);
            previousFilteredSample = filteredSignal;

            // Apply output gain
            var finalSignal = filteredSignal * OutputGain;

            // Mix dry and wet signals
            var mixedSignal = (1.0f - Mix) * inputSample + Mix * finalSignal;

            // Update feedback
            feedback = mixedSignal * FeedbackAmount; // Controlled feedback

            return mixedSignal;
        }

        private SynthType SoftClipping(SynthType input)
        {
            return input / (SynthTypeHelper.One + SynthTypeHelper.Abs(input));
        }

        private SynthType AsymmetricSoftClipping(SynthType input)
        {
            return input >= 0 ? input / (1.0f + 0.5f * input) : input / (1.0f + 0.8f * Math.Abs(input));
        }

        private SynthType PolynomialShaping(SynthType input)
        {
            return input - 0.3f * input * input * input + 0.1f * input * input * input * input * input;
        }

        private SynthType ComplexWaveshaping(SynthType input)
        {
            // A combination of sine shaping and further polynomial shaping
            SynthType sineShaped = SynthTypeHelper.Sin(input * SynthTypeHelper.Pi * SynthTypeHelper.Half);
            return sineShaped - 0.2f * sineShaped * sineShaped * sineShaped;
        }

        private SynthType lowPassFilter(SynthType input, SynthType previousOutput, SynthType cutoffFrequency)
        {
            SynthType alpha = cutoffFrequency / (cutoffFrequency + SynthTypeHelper.One);
            return alpha * input + (SynthTypeHelper.One - alpha) * previousOutput;
        }
    }
}
