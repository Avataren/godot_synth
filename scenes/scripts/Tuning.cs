using Godot;

public partial class Tuning : Control
{

	[Signal]
	public delegate void OctaveChangedEventHandler(float attackTime);
	[Signal]
	public delegate void SemiChangedEventHandler(float decayTime);
	[Signal]
	public delegate void CentsChangedEventHandler(float sustainLevel);


	private void _on_oct_spin_box_value_changed(double value)
	{
		EmitSignal("OctaveChanged", (float)value);
	}

	private void _on_semi_spin_box_value_changed(double value)
	{
		EmitSignal("SemiChanged", (float)value);
	}

	private void _on_cents_spin_box_value_changed(double value)
	{
		EmitSignal("CentsChanged", (float)value);
	}    
    
}
