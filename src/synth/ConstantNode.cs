namespace Synth
{
    public class ConstantNode : AudioNode
    {
        public float Value { get; set; }

        public ConstantNode() : base()
        {
            Value = 0.0f;
        }

        public override void Process(double increment)
        {

        }

        public override float this[int index]
        {
            get => Value;
            set => Value = value;
        }
    }
}