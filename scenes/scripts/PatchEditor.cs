using static Godot.GD;
using Godot;
using System.Collections.Generic;
using System;
using Synth;

public partial class PatchEditor : Node2D
{
	[Export]
	private Oscillator Oscillator1;
	[Export]
	private Oscillator Oscillator2;
	[Export]
	private Oscillator Oscillator3;
	[Export]
	private Oscillator Oscillator4;
	[Export]
	private Oscillator Oscillator5;
	[Export]
	private ADSR_Envelope ADSREnvelope;

	[Export]
	private AudioOutputNode AudioOutputNode;

	private Control lfoContainer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		lfoContainer = GetNode<Control>("%LFOContainer");
		ConnectToLFOSignals();
		var oscs = new Oscillator[] { Oscillator1, Oscillator2, Oscillator3, Oscillator4, Oscillator5 };
		try
		{
			Print("Patch Editor Ready");
			Oscillator1.Enable();
			for (int i = 0; i < oscs.Length; i++)
			{
				ConnectOscillatorSignals(oscs[i], i);
			}
			ConnectAmplitudeEnvelope();
		}
		catch (Exception e)
		{
			PrintErr(e.Message);
			PrintErr(e.StackTrace);
		}
	}

	private void ConnectToLFOSignals()
	{
		Print("Connecting to LFO Signals:", lfoContainer);
		foreach (Node lfoNode in lfoContainer.GetChildren())
		{
			if (lfoNode.HasMeta("isLFO") && (bool)lfoNode.GetMeta("isLFO"))
			{
				Print("LFO Node Found:", lfoNode.Name);
			}
			else
			{
				Print("Node is not an LFO_UI:", lfoNode.GetClass());
			}
		}
	}

	private WaveTableWaveType GetWaveformFromIndex(int index)
	{
		switch (index)
		{
			case 0:
				return WaveTableWaveType.SINE;
			case 1:
				return WaveTableWaveType.TRIANGLE;
			case 2:
				return WaveTableWaveType.SQUARE;
			case 3:
				return WaveTableWaveType.SAWTOOTH;
			case 4:
				return WaveTableWaveType.ORGAN;
			case 5:
				return WaveTableWaveType.ORGAN2;
			case 6:
				return WaveTableWaveType.BASS;
			case 7:
				return WaveTableWaveType.VOCAL_AHH;
			case 8:
				return WaveTableWaveType.FUZZY;
			case 9:
				return WaveTableWaveType.PIANO;
			case 10:
				return WaveTableWaveType.PWM;
			default:
				return WaveTableWaveType.SINE;
		}
	}

	private void _on_octave_select_item_selected(int index)
	{
		AudioOutputNode.BaseOctave = index;
	}

	private void ConnectOscillatorSignals(Oscillator osc, int oscNum)
	{
		osc.WaveformChanged += (waveformIndex) =>
		{
			AudioOutputNode.CurrentPatch.SetWaveform(GetWaveformFromIndex(waveformIndex), oscNum);
		};
		osc.OscillatorEnabledToggled += (enabled) =>
		{
			AudioOutputNode.CurrentPatch.SetOscillatorEnabled(enabled, oscNum);
		};
		osc.AttackTimeChanged += (attackTime) =>
		{
			AudioOutputNode.CurrentPatch.SetAttack(attackTime / 1000.0f, oscNum);
		};
		osc.DecayTimeChanged += (decayTime) =>
		{
			AudioOutputNode.CurrentPatch.SetDecay(decayTime / 1000.0f, oscNum);
		};
		osc.SustainLevelChanged += (sustainLevel) =>
		{
			AudioOutputNode.CurrentPatch.SetSustain(sustainLevel, oscNum);
		};
		osc.ReleaseTimeChanged += (releaseTime) =>
		{
			AudioOutputNode.CurrentPatch.SetRelease(releaseTime / 1000.0f, oscNum);
		};
		osc.VolumeChanged += (volume) =>
		{
			AudioOutputNode.CurrentPatch.SetAmplitude(volume / 100.0f, oscNum);
		};
		osc.DetuneOctavesChanged += (detuneOctaves) =>
		{
			GD.Print("Detune Octaves Changed");
			AudioOutputNode.CurrentPatch.SetDetuneOctaves(detuneOctaves, oscNum);
		};
		osc.DetuneSemiChanged += (detuneSemi) =>
		{
			GD.Print("Detune Semi Changed");
			AudioOutputNode.CurrentPatch.SetDetuneSemi(detuneSemi, oscNum);
		};
		osc.DetuneCentsChanged += (detuneCents) =>
		{
			GD.Print("Detune Cents Changed");
			AudioOutputNode.CurrentPatch.SetDetuneCents(detuneCents, oscNum);
		};
		osc.HardSyncToggled += (enabled) =>
		{
			AudioOutputNode.CurrentPatch.SetHardSync(enabled, oscNum);
		};

		osc.ADSRToggled += (enabled) =>
		{
			AudioOutputNode.CurrentPatch.SetADSREnabled(enabled, oscNum);
		};

		osc.PWMChanged += (pwm) =>
		{
			AudioOutputNode.CurrentPatch.SetPWM(pwm, oscNum);
		};

	}

	protected void ConnectAmplitudeEnvelope()
	{
		ADSREnvelope.AttackTimeChanged += (attackTime) =>
		{
			AudioOutputNode.CurrentPatch.SetAttack(attackTime / 1000.0f);
		};
		ADSREnvelope.DecayTimeChanged += (decayTime) =>
		{
			AudioOutputNode.CurrentPatch.SetDecay(decayTime / 1000.0f);
		};
		ADSREnvelope.SustainLevelChanged += (sustainLevel) =>
		{
			AudioOutputNode.CurrentPatch.SetSustain(sustainLevel);
		};
		ADSREnvelope.ReleaseTimeChanged += (releaseTime) =>
		{
			AudioOutputNode.CurrentPatch.SetRelease(releaseTime / 1000.0f);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
