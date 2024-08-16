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
	private Oscillator Oscillator5;

	[Export]
	private LFO LFO1;
	[Export]
	private LFO LFO2;
	[Export]
	private LFO LFO3;
	[Export]
	private LFO LFO4;

	[Export]
	private ADSR_Envelope ADSREnvelope;

	[Export]
	private AudioOutputNode AudioOutputNode;

	//private Control lfoContainer;
	private Control gdLFONode;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//lfoContainer = GetNode<Control>("%LFOContainer");
		var oscs = new Oscillator[] { Oscillator1, Oscillator2, Oscillator3, Oscillator4, Oscillator5 };
		var lfos = new LFO[] { LFO1, LFO2, LFO3, LFO4 };
		try
		{
			Print("Patch Editor Ready");
			Oscillator1.Enable();
			for (int i = 0; i < oscs.Length; i++)
			{
				ConnectOscillatorSignals(oscs[i], i);
			}
			for (int i = 0; i < lfos.Length; i++)
			{
				Print("Connecting LFO Signals" + i);
				ConnectLFOSignals(lfos[i], i);
			}
			ConnectAmplitudeEnvelope();
		}
		catch (Exception e)
		{
			PrintErr(e.Message);
			PrintErr(e.StackTrace);
		}
	}

	[Export]
	private string scenePath = "res://scenes/patch_editor.tscn";

	void _on_reset_button_pressed()
	{
		// Load the scene from the stored path
		GD.Print("Resetting Patch Editor");
		var newScene = (PackedScene)ResourceLoader.Load(scenePath);
		if (newScene != null)
		{
						// Free the old scene
			var currentScene = GetTree().CurrentScene;
			currentScene.QueueFree();

			Node newSceneInstance = newScene.Instantiate();
			// Add the new scene to the scene tree and set it as the current scene
			GetTree().Root.AddChild(newSceneInstance);
			GetTree().CurrentScene = newSceneInstance;


		}
		else
		{
			GD.PrintErr("Failed to load scene from path: " + scenePath);
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

	private void _on_reverb_wet_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_Wet(value);
	}

	private void _on_reverb_dry_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_Dry(value);
	}

	private void _on_reverb_damp_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_Damp(value);
	}

	private void _on_reverb_room_size_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_RoomSize(value);
	}

	private void _on_reverb_width_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_Width(value);
	}

	private void _on_reverb_enabled_toggled(bool enabled)
	{
		AudioOutputNode.CurrentPatch.SetReverbEffect_Enabled(enabled);
	}


	private void _on_drive_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetDrive(value);
	}

	private void _on_resonance_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetResonance(value);
	}

	private void _on_cutoff_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetCutoff(value);
	}

	private void _on_octave_select_item_selected(int index)
	{
		AudioOutputNode.BaseOctave = index;
	}

	private void _on_delay_delay_enabled(bool enabled)
	{
		AudioOutputNode.CurrentPatch.SetDelayEffect_Enabled(enabled);
	}

	private void _on_delay_delay_changed(int value)
	{
		AudioOutputNode.CurrentPatch.SetDelayEffect_Delay(value);
	}

	private void _on_delay_feedback_changed(float value)
	{
		GD.Print("Feedback Changed: ", value);
		AudioOutputNode.CurrentPatch.SetDelayEffect_Feedback(value);
	}

	private void _on_delay_wetmix_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetDelayEffect_WetMix(value);
	}

	private void _on_delay_drymix_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetDelayEffect_DryMix(value);
	}

	int activeLFO = 0;
	private void ConnectLFOSignals(LFO lfo, int lfoNum)
	{
		// Using a lambda to pass both the signal's argument and the extra parameter
		lfo.FreqValueChanged += (val) =>
		{
			AudioOutputNode.CurrentPatch.SetLFOFrequency(val, lfoNum);
		};
		lfo.GainValueChanged += (val) =>
		{
			AudioOutputNode.CurrentPatch.SetLFOGain(val, lfoNum);
		};
		lfo.WaveformChanged += (val) =>
		{
			AudioOutputNode.CurrentPatch.SetLFOWaveform(val, lfoNum);
		};

	}

	private void LFO_Freq_Updated(float val)
	{
		GD.Print("LFO" + activeLFO + " Freq Updated: " + val);
	}
	private void ConnectOscillatorSignals(Oscillator osc, int oscNum)
	{
		osc.PhaseOffsetChanged += (phaseOffset) =>
		{
			GD.Print("Phase Offset Changed:" + phaseOffset);
			AudioOutputNode.CurrentPatch.SetOscillatorPhaseOffset(phaseOffset, oscNum);
		};

		osc.FeedbackChanged += (feedback) =>
		{
			AudioOutputNode.CurrentPatch.SetFeedback(feedback, oscNum);
		};

		osc.BalanceChanged += (balance) =>
		{
			AudioOutputNode.CurrentPatch.SetBalance(balance, oscNum);
		};

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
			AudioOutputNode.CurrentPatch.SetAmplitude(volume, oscNum);
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

		osc.ModChanged += (mod) =>
		{
			AudioOutputNode.CurrentPatch.SetModulationStrength(mod, oscNum);
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
