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
		var maxVal = (float)GetNode<Slider>("%AttackSlider").MaxValue;
		var val = (float)value / maxVal;
		val*=val;
		EmitSignal("AttackTimeChanged", (float)val*maxVal);
	}

	private void _on_decay_slider_value_changed(double value)
	{
		var maxVal = (float)GetNode<Slider>("%DecaySlider").MaxValue;
		var val = (float)value / maxVal;
		val*=val;
		EmitSignal("DecayTimeChanged", (float)val*maxVal);
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
