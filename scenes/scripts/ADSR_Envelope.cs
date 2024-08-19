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
	private float AttackDurationMS = 1000.0f;
	[Export]
	private float DecayDurationMS = 1000.0f;

	[Export]
	private float MaxReleaseTimeMS = 1000.0f;
	[Export]
	private Label EnvelopeLabel;

	[Export]
	private string EnvelopeName
	{
		get => EnvelopeLabel.Text;
		set { EnvelopeLabel.Text = value; }
	}

	public override void _Ready()
	{
		if (EnvelopeLabel?.Text == "")
		{
				EnvelopeLabel.Visible = false;
		}
	}

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
		EmitSignal("AttackTimeChanged", (float)value * AttackDurationMS);
	}

	private void _on_decay_slider_value_changed(double value)
	{
		if (value < 0.0015)
			value = 0.0;
		EmitSignal("DecayTimeChanged", (float)value * DecayDurationMS);
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
