using Godot;
using System;

public partial class adsr_envelope : VBoxContainer
{
	[Signal]
	public delegate void AttackTimeChangedEventHandler(float attackTime);
	[Signal]
	public delegate void DecayTimeChangedEventHandler(float decayTime);
	[Signal]
	public delegate void SustainLevelChangedEventHandler(float sustainLevel);
	[Signal]
	public delegate void ReleaseTimeChangedEventHandler(float releaseTime);

	private void _on_attack_slider_value_changed(double value)
	{
		EmitSignal("AttackTimeChanged", (float)value);
	}

	private void _on_decay_slider_value_changed(double value)
	{
		EmitSignal("DecayTimeChanged", (float)value);
	}

	private void _on_sustain_slider_value_changed(double value)
	{
		EmitSignal("SustainLevelChanged", (float)value);
	}

	private void _on_release_slider_value_changed(double value)
	{
		EmitSignal("ReleaseTimeChanged", (float)value);
	}
}
