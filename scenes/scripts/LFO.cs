using static Godot.GD;
using Godot;

public partial class LFO : Control
{
	[Signal]
	public delegate void FreqValueChangedEventHandler(float value);
	[Signal]
	public delegate void GainValueChangedEventHandler(float value);
	[Signal]
	public delegate void BiasValueChangedEventHandler(float value);
	[Signal]
	public delegate void WaveformChangedEventHandler(string value);
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
	private OptionButton WaveformOptions;
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
		EmitSignal(nameof(FreqValueChangedEventHandler), (float)value);
	}

	private void OnGainKnobValueChanged(double value)
	{
		EmitSignal(nameof(GainValueChangedEventHandler), (float)value);
	}

	private void OnBiasKnobValueChanged(double value)
	{
		EmitSignal(nameof(BiasValueChangedEventHandler), (float)value);
	}

	private void OnWaveformOptionsItemSelected(int index)
	{
		EmitSignal(nameof(WaveformChangedEventHandler), WaveformOptions.GetItemText(index));
	}

	private void OnAbsCheckButtonToggled(bool value)
	{
		EmitSignal(nameof(AbsValueChangedEventHandler), value);
	}

	private void OnEnableEnvelopeToggled(bool toggledOn)
	{
		ADSREnvelope.Visible = toggledOn;
		EmitSignal(nameof(ADSRToggledEventHandler), toggledOn);
	}
}
