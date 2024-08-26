using System;
using System.Linq;
using Godot;

namespace Synth
{
    public class FuzzNode : AudioNode
    {
        public float InputGain { get; set; } = 10.0f;
        public float OutputGain { get; set; } = 0.5f;
        public float Mix { get; set; } = 0.8f;
        public float LowPassCutoff { get; set; } = 0.3f;
        public float Bias { get; set; } = 0.1f;
        public float FeedbackAmount { get; set; } = 0.2f;
        public float StereoSpread { get; set; } = 0.02f;  // Amount of stereo spread effect

        private float _previousFilteredSampleLeft = 0.0f;
        private float _previousFilteredSampleRight = 0.0f;
        private float _feedbackLeft = 0.0f;
        private float _feedbackRight = 0.0f;

        public FuzzNode() : base()
        {
            AcceptedInputType = InputType.Stereo;
            LeftBuffer = new float[NumSamples];
            RightBuffer = new float[NumSamples];
        }

        public override void Process(double increment)
        {
            var node = GetParameterNodes(AudioParam.StereoInput).FirstOrDefault();
            if (node == null || !node.Enabled)
            {
                return;
            }

            float[] inputLeftBuffer = node.LeftBuffer;
            float[] inputRightBuffer = node.RightBuffer;

            for (int i = 0; i < NumSamples; i++)
            {
                // Process left and right channels with stereo spread
                float leftInputSample = inputLeftBuffer[i];
                float rightInputSample = inputRightBuffer[i];

                // Apply stereo spread by slightly altering the left and right channels
                float spreadedLeft = leftInputSample * (1.0f - StereoSpread);
                float spreadedRight = rightInputSample * (1.0f + StereoSpread);

                // Process the left and right channels
                LeftBuffer[i] = ProcessSample(spreadedLeft, ref _previousFilteredSampleLeft, ref _feedbackLeft);
                RightBuffer[i] = ProcessSample(spreadedRight, ref _previousFilteredSampleRight, ref _feedbackRight);
            }
        }

        private float ProcessSample(float inputSample, ref float previousFilteredSample, ref float feedback)
        {
            // Apply initial input gain and feedback
            float boostedSignal = (inputSample + feedback * FeedbackAmount) * InputGain;

            // Apply dynamic bias
            boostedSignal += Bias * Mathf.Sin(Time.GetTicksMsec() * 0.001f);

            // Multi-stage distortion
            float stage1 = SoftClipping(boostedSignal);
            float stage2 = AsymmetricSoftClipping(stage1);
            float stage3 = PolynomialShaping(stage2);

            // Advanced waveshaping for added complexity
            float shapedSignal = ComplexWaveshaping(stage3);

            // Low-pass filter to smooth out harsh frequencies
            float filteredSignal = lowPassFilter(shapedSignal, previousFilteredSample, LowPassCutoff);
            previousFilteredSample = filteredSignal;

            // Apply output gain
            float finalSignal = filteredSignal * OutputGain;

            // Mix dry and wet signals
            float mixedSignal = (1.0f - Mix) * inputSample + Mix * finalSignal;

            // Update feedback
            feedback = mixedSignal * FeedbackAmount; // Controlled feedback

            return mixedSignal;
        }

        private float SoftClipping(float input)
        {
            return input / (1.0f + Mathf.Abs(input));
        }

        private float AsymmetricSoftClipping(float input)
        {
            return input >= 0 ? input / (1.0f + 0.5f * input) : input / (1.0f + 0.8f * Math.Abs(input));
        }

        private float PolynomialShaping(float input)
        {
            return input - 0.3f * input * input * input + 0.1f * input * input * input * input * input;
        }

        private float ComplexWaveshaping(float input)
        {
            // A combination of sine shaping and further polynomial shaping
            float sineShaped = Mathf.Sin(input * Mathf.Pi * 0.5f);
            return sineShaped - 0.2f * sineShaped * sineShaped * sineShaped;
        }

        private float lowPassFilter(float input, float previousOutput, float cutoffFrequency)
        {
            float alpha = cutoffFrequency / (cutoffFrequency + 1.0f);
            return alpha * input + (1 - alpha) * previousOutput;
        }
    }
}
