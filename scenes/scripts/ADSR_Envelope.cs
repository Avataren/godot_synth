using Godot;
using System;

public partial class ADSR_Envelope : VBoxContainer
{
	[Signal]
	public delegate void AttackTimeChangedEventHandler(float attackTime);
	[Signal]
	public delegate void DecayTimeChangedEventHandler(float decayTime);
	[Signal]
	public delegate void SustainLevelChangedEventHandler(float sustainLevel);
	[Signal]
	public delegate void ReleaseTimeChangedEventHandler(float releaseTime);

	[Export]
	private float MaxAttackTimeMS = 20000.0f;
	[Export]
	private float MaxDecayTimeMS = 20000.0f;

	[Export]
	private float MaxReleaseTimeMS = 10000.0f;

	public void Enable()
	{
		Visible = true;
		QueueSort();
	}

	public void Disable()
	{
		Visible = false;
	}

	private void _on_attack_slider_value_changed(double value)
	{
		if (value < 0.0015)
			value = 0.0;
		EmitSignal("AttackTimeChanged", (float)value * MaxAttackTimeMS);
	}

	private void _on_decay_slider_value_changed(double value)
	{
		if (value < 0.0015)
			value = 0.0;
		EmitSignal("DecayTimeChanged", (float)value * MaxDecayTimeMS);
	}

	private void _on_sustain_slider_value_changed(double value)
	{
		EmitSignal("SustainLevelChanged", (float)value);
	}

	private void _on_release_slider_value_changed(double value)
	{
		if (value < 0.0015)
			value = 0.0;
		EmitSignal("ReleaseTimeChanged", (float)value * MaxReleaseTimeMS);
	}
}
