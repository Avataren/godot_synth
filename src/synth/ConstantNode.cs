namespace Synth
{
    public class ConstantNode : AudioNode
    {
        public float Value { get; set; }

        public ConstantNode(int numSamples, float value) : base(numSamples)
        {
            Value = value;
        }

        public override void Process(float increment)
        {
            for (int i = 0; i < NumSamples; i++)
            {
                buffer[i] = Value + GetParameter(AudioParam.Constant, i, 1.0f);
            }
        }
    }
}