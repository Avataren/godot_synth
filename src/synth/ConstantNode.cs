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

        }

        public override float this[int index]
        {
            get => Value;
            set => Value = value;
        }
    }
}