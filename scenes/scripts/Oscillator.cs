using static Godot.GD;
using Godot;
using System;

public partial class Oscillator : Control
{
	[Export]
	private CheckBox OscillatorEnabled;

	[Signal]
	public delegate void AttackTimeChangedEventHandler(float attackTime);
	[Signal]
	public delegate void DecayTimeChangedEventHandler(float decayTime);
	[Signal]
	public delegate void SustainLevelChangedEventHandler(float sustainLevel);
	[Signal]
	public delegate void ReleaseTimeChangedEventHandler(float releaseTime);
	[Signal]
	public delegate void OscillatorEnabledToggledEventHandler(bool enabled);
	[Signal]
	public delegate void WaveformChangedEventHandler(int waveformIndex);
	[Signal]
	public delegate void VolumeChangedEventHandler(float volume);

	public void Enable()
	{
		OscillatorEnabled.ButtonPressed = true;
	}

	public void Disable()
	{
		OscillatorEnabled.ButtonPressed = false;
	}

	public bool IsEnabled()
	{
		return OscillatorEnabled.ButtonPressed;
	}

	private void _On_Volume_Changed(double value)
	{
		EmitSignal("VolumeChanged", (float)value);
	}

	private void _on_waveform_select_item_selected (int index)
	{
		EmitSignal("WaveformChanged", index);
	}

	private void _on_check_box_toggled(bool value)
	{
		EmitSignal("OscillatorEnabledToggled", value);
	}

	private void _on_attack_time_changed(double value)
	{
		EmitSignal("AttackTimeChanged", (float)value);
	}

	private void _on_decay_time_changed(double value)
	{
		EmitSignal("DecayTimeChanged", (float)value);
	}

	private void _on_sustain_level_changed(double value)
	{
		EmitSignal("SustainLevelChanged", (float)value);
	}

	private void _on_release_time_changed(double value)
	{
		EmitSignal("ReleaseTimeChanged", (float)value);
	}

}
