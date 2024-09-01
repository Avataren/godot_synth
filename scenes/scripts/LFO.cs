using static Godot.GD;
using Godot;
using Synth;

public partial class LFO : PanelContainer
{
	[Signal]
	public delegate void FreqValueChangedEventHandler(float value);
	[Signal]
	public delegate void GainValueChangedEventHandler(float value);
	[Signal]
	public delegate void BiasValueChangedEventHandler(float value);
	[Signal]
	public delegate void WaveformChangedEventHandler(int value);
	[Signal]
	public delegate void AbsValueChangedEventHandler(bool value);
	[Signal]
	public delegate void ADSRToggledEventHandler(bool enabled);

	[Export]
	private Control FreqKnob;
	[Export]
	private Control GainKnob;
	[Export]
	private Control BiasKnob;
	[Export]
	private CheckBox AbsCheckButton;
	[Export]
	private Control ADSREnvelope;

	public override void _Ready()
	{
		// 	EmitSignal(nameof(FreqValueChangedEventHandler), FreqKnob.Value);
		// 	EmitSignal(nameof(GainValueChangedEventHandler), GainKnob.Value);
		// 	EmitSignal(nameof(BiasValueChangedEventHandler), BiasKnob.Value);
		// 	EmitSignal(nameof(WaveformChangedEventHandler), WaveformOptions.GetItemText(WaveformOptions.Selected));
		// 	EmitSignal(nameof(AbsValueChangedEventHandler), AbsCheckButton.ButtonPressed);
		// ADSREnvelope.Visible = false;
		// EmitSignal(nameof(ADSRToggledEventHandler), false);
	}

	private void OnFreqKnobValueChanged(double value)
	{
		Print("(ui) Freq Knob Value Changed: " + value);
		EmitSignal("FreqValueChanged", (float)value);
	}

	private void OnGainKnobValueChanged(double value)
	{
		Print("(ui) Gain Knob Value Changed: " + value);
		EmitSignal("GainValueChanged", (float)value);
	}

	private void OnBiasKnobValueChanged(double value)
	{
		Print("(ui) Bias Knob Value Changed: " + value);
		EmitSignal("BiasValueChanged", (float)value);
	}

	private void OnWaveformOptionsItemSelected(int index)
	{
		Print("(ui) Waveform Option Selected: " + WaveformSelect.LFOWaveforms[index]);
		EmitSignal("WaveformChanged", (int)WaveformSelect.LFOWaveforms[index]);
	}

	private void OnAbsCheckButtonToggled(bool value)
	{
		EmitSignal(nameof(AbsValueChangedEventHandler), value);
	}

	private void OnEnableEnvelopeToggled(bool toggledOn)
	{
		ADSREnvelope.Visible = toggledOn;
		EmitSignal("ADSRToggled", toggledOn);
	}
}
