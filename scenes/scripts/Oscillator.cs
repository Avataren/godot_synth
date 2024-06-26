using static Godot.GD;
using Godot;

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
	[Signal]
	public delegate void HardSyncToggledEventHandler(bool enabled);
	[Signal]
	public delegate void DetuneOctavesChangedEventHandler(float detuneOctaves);
	[Signal]
	public delegate void DetuneSemiChangedEventHandler(float detuneSemi);
	[Signal]
	public delegate void PWMChangedEventHandler(float pwm);

	[Signal]
	public delegate void ADSRToggledEventHandler(bool enabled);

	[Signal]
	public delegate void DetuneCentsChangedEventHandler(float detuneCents);
	[Export]
	private ADSR_Envelope ADSREnvelope;

	public bool ADSREnvelopeEnabled { get; set; } = true;

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

	private void _on_adsr_check_box_toggled(bool value)
	{
		ADSREnvelopeEnabled = value;

		if (ADSREnvelope != null)
		{
			if (ADSREnvelopeEnabled)
			{
				ADSREnvelope.Enable();
			}
			else
			{
				ADSREnvelope.Disable();
			}
			EmitSignal("ADSRToggled", ADSREnvelopeEnabled);
		}
		else
		{
			PrintErr("ADSR_Envelope node not found.");
		}
	}
	private void _on_pwm_slider_value_changed(double value)
	{
		EmitSignal("PWMChanged", (float)value);
	}

	private void _On_Volume_Changed(double value)
	{
		EmitSignal("VolumeChanged", (float)value);
	}
	private void _on_waveform_select_item_selected(int index)
	{
		EmitSignal("WaveformChanged", index);
	}
	private void _on_hard_sync_check_box_toggled(bool value)
	{
		EmitSignal("HardSyncToggled", value);
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
	private void _on_tuning_octave_changed(float value)
	{
		Print("_on_detune_octaves_changed");
		EmitSignal("DetuneOctavesChanged", value);
	}
	private void _on_tuning_semi_changed(float value)
	{
		Print("_on_detune_semi_changed");
		EmitSignal("DetuneSemiChanged", value);
	}
	private void _on_tuning_cents_changed(float value)
	{
		Print("_on_detune_cents_changed");
		EmitSignal("DetuneCentsChanged", value);
	}
}
