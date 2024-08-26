namespace Synth
{

    public enum ModulationType
    {
        Add = 0,
        Multiply = 1
    }

    public partial class ParameterConnection
    {
        // Enum should still be public for GDScript access

        public AudioNode SourceNode { get; set; }

        public float Strength { get; set; } = 1.0f;

        public ModulationType ModType { get; set; } = ModulationType.Add;

        public static ModulationType GetModulationTypeAdd()
        {
            return ModulationType.Add;
        }

        public static ModulationType GetModulationTypeMultiply()
        {
            return ModulationType.Multiply;
        }

        public ParameterConnection(AudioNode sourceNode, float strength = 1.0f, ModulationType modType = ModulationType.Add)
        {
            SourceNode = sourceNode;
            Strength = strength;
            ModType = modType;
        }
    }
}