using static Godot.GD;
using Godot;
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
	private AudioOutputNode AudioOutputNode;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var oscs = new Oscillator[] { Oscillator1, Oscillator2, Oscillator3, Oscillator4 };
		try
		{
			Print("Patch Editor Ready");
			Oscillator1.Enable();
			for (int i = 0; i < oscs.Length; i++)
			{
				ConnectOscillatorSignals(oscs[i], i + 1);
			}

		}
		catch (Exception e)
		{
			PrintErr(e.Message);
			PrintErr(e.StackTrace);
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
			default:
				return WaveTableWaveType.SINE;
		}
	}

	private void ConnectOscillatorSignals(Oscillator osc, int oscNum)
	{
		osc.WaveformChanged += (waveformIndex) =>
		{
			AudioOutputNode.CurrentPatch.SetWaveform(GetWaveformFromIndex(waveformIndex), oscNum - 1);
		};
		osc.OscillatorEnabledToggled += (enabled) =>
		{
			AudioOutputNode.CurrentPatch.SetOscillatorEnabled(enabled, oscNum - 1);
		};
		osc.AttackTimeChanged += (attackTime) =>
		{
			AudioOutputNode.CurrentPatch.SetAttack(attackTime / 1000.0f, oscNum - 1);
		};
		osc.DecayTimeChanged += (decayTime) =>
		{
			AudioOutputNode.CurrentPatch.SetDecay(decayTime / 1000.0f, oscNum - 1);
		};
		osc.SustainLevelChanged += (sustainLevel) =>
		{
			AudioOutputNode.CurrentPatch.SetSustain(sustainLevel, oscNum - 1);
		};
		osc.ReleaseTimeChanged += (releaseTime) =>
		{
			AudioOutputNode.CurrentPatch.SetRelease(releaseTime / 1000.0f, oscNum - 1);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
