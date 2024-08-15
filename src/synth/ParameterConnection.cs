namespace Synth
{
    public class ParameterConnection
    {
        public enum ModulationType
        {
            Add,
            Multiply
        }
        public AudioNode SourceNode { get; set; }
        public float Strenght { get; set; } = 1.0f;
        public ModulationType ModType { get; set; } = ModulationType.Add;
    }
}