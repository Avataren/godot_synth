using static Godot.GD;
using Godot;
using System;
using Synth;
using System.Collections.Generic;
using System.Linq;

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
	[Export]
	private AdsrVisualizer AdsrVisualizer;

	[Signal]
	public delegate void CpuUsageChangedEventHandler(float value);

	//private Control lfoContainer;
	private Control gdLFONode;
	// Called when the node enters the scene tree for the first time.

	private Dictionary<String, float[]> waveforms = new Dictionary<String, float[]>();
	public override void _Ready()
	{
		//lfoContainer = GetNode<Control>("%LFOContainer");
		var oscs = new Oscillator[] { Oscillator1, Oscillator2, Oscillator3, Oscillator4, Oscillator5 };
		var lfos = new LFO[] { LFO1, LFO2, LFO3, LFO4 };
		try
		{
			Print("Patch Editor Ready");
			ConnectEnvelopesToGui();
			Oscillator1.Enable();
			for (int i = 0; i < oscs.Length; i++)
			{
				ConnectOscillatorSignals(oscs[i], i);
			}
			for (int i = 0; i < lfos.Length; i++)
			{
				ConnectLFOSignals(lfos[i], i);
			}
			CreateWaveforms();
			ConnectPerformanceUpdate();

		}
		catch (Exception e)
		{
			PrintErr(e.Message);
			PrintErr(e.StackTrace);
		}
	}

	[Export]
	private string scenePath = "res://scenes/patch_editor.tscn";

	double lastCpu = 0.0;
	private List<double> cpuUsageHistory = new List<double>();
	private const int historySize = 32;

	private void ConnectEnvelopesToGui()
	{
		if (AdsrVisualizer != null)
		{
			for (int i = 0; i < SynthPatch.MaxEnvelopes; i++)
			{
				AdsrVisualizer.SetADSRNodeReference(AudioOutputNode.CurrentPatch.GetEnvelope(i), i);
			}
			AdsrVisualizer.SetActiveEnvelopeIndex(0);
		}
		else
		{
			PrintErr("AdsrVisualizer not set!");
		}
	}

	private void ConnectPerformanceUpdate()
	{
		AudioOutputNode.PerformanceTimeUpdate += (process_time, buffer_push_time, frames) =>
		{
			var cpuUsage = process_time / (double)buffer_push_time;
			cpuUsageHistory.Add(cpuUsage);
			if (cpuUsageHistory.Count > historySize)
			{
				cpuUsageHistory.RemoveAt(0);
			}
			var smoothedCpuUsage = cpuUsageHistory.Average();

			EmitSignal(SignalName.CpuUsageChanged, smoothedCpuUsage);
		};
	}

	private void CreateWaveforms()
	{
		waveforms.Add("Sine", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.SINE, 64));
		waveforms.Add("Triangle", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.TRIANGLE, 64));
		waveforms.Add("Square", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.SQUARE, 64));
		waveforms.Add("Saw", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.SAWTOOTH, 64));
		waveforms.Add("Noise", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.NOISE, 64));
		waveforms.Add("Organ", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.ORGAN, 64));
		waveforms.Add("Organ2", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.ORGAN2, 64));
		waveforms.Add("Bass", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.BASS, 64));
		waveforms.Add("Ahh", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.VOCAL_AHH, 64));
		waveforms.Add("Fuzzy", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.FUZZY, 64));
		waveforms.Add("Piano", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.PIANO, 64));
		waveforms.Add("PWM", AudioOutputNode.CurrentPatch.CreateWaveform(WaveTableWaveType.SQUARE, 64));

	}

	public float[] GetWaveformData(String name)
	{
		if (waveforms.ContainsKey(name))
		{
			return waveforms[name];
		}
		else
		{
			return null;
		}
	}

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
				return WaveTableWaveType.NOISE;
			case 5:
				return WaveTableWaveType.ORGAN;
			case 6:
				return WaveTableWaveType.ORGAN2;
			case 7:
				return WaveTableWaveType.BASS;
			case 8:
				return WaveTableWaveType.VOCAL_AHH;
			case 9:
				return WaveTableWaveType.FUZZY;
			case 10:
				return WaveTableWaveType.PIANO;
			case 11:
				return WaveTableWaveType.PWM;
			default:
				return WaveTableWaveType.SINE;
		}
	}

	private void _on_moog_filter_filter_type_changed(string type)
	{
		FilterType ftype = FilterType.MoogLowPass;
		switch (type)
		{
			case "MoogLP":
				ftype = FilterType.MoogLowPass;
				break;
			case "LowPass":
				ftype = FilterType.LowPass;
				break;
			case "HighPass":
				ftype = FilterType.HighPass;
				break;
			case "BandPass":
				ftype = FilterType.BandPass;
				break;
			case "Notch":
				ftype = FilterType.Notch;
				break;
			case "Peak":
				ftype = FilterType.Peak;
				break;
			case "AllPass":
				ftype = FilterType.AllPass;
				break;
			case "Peaking":
				ftype = FilterType.Peaking;
				break;
			case "LowShelf":
				ftype = FilterType.LowShelf;
				break;
			case "HighShelf":
				ftype = FilterType.HighShelf;
				break;
		}
		AudioOutputNode.CurrentPatch.filterNode.SetFilterType(ftype);
	}

	private void _on_panel_container_enabled_changed(bool enabled)
	{
		AudioOutputNode.CurrentPatch.graph.SetNodeEnabled(AudioOutputNode.CurrentPatch.noiseNode, enabled);
	}

	private void _on_panel_container_gain_changed(float value)
	{
		AudioOutputNode.CurrentPatch.noiseNode.SetAmplitude(value);
	}

	private void _on_panel_container_slope_changed(float value)
	{
		AudioOutputNode.CurrentPatch.noiseNode.Cutoff = value;
	}

	private void _on_panel_container_noisetype_changed(string noiseType)
	{
		if (noiseType == "white")
		{
			AudioOutputNode.CurrentPatch.noiseNode.SetNoiseType(NoiseType.White);
		}
		else if (noiseType == "pink")
		{
			AudioOutputNode.CurrentPatch.noiseNode.SetNoiseType(NoiseType.Pink);
		}
		else if (noiseType == "brown")
		{
			AudioOutputNode.CurrentPatch.noiseNode.SetNoiseType(NoiseType.Brownian);
		}
	}

	private void _on_panel_container_offset_changed(float value)
	{
		AudioOutputNode.CurrentPatch.noiseNode.SetDCOffset(value);
	}

	private void _on_distortion_bias_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.Bias = value;
	}

	private void _on_distortion_cutoff_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.LowPassCutoff = value;
	}

	private void _on_distortion_enabled_changed(bool enabled)
	{
		AudioOutputNode.CurrentPatch.graph.SetNodeEnabled(AudioOutputNode.CurrentPatch.fuzzNode, enabled);
	}

	private void _on_distortion_feedback_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.FeedbackAmount = value;
	}

	private void _on_distortion_input_gain_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.InputGain = value;
	}

	private void _on_distortion_mix_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.Mix = value;
	}

	private void _on_distortion_output_gain_changed(float value)
	{
		AudioOutputNode.CurrentPatch.fuzzNode.OutputGain = value;
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
		AudioOutputNode.ClearKeyStack();
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

		// osc.AttackTimeChanged += (attackTime) =>
		// {
		// 	AudioOutputNode.CurrentPatch.SetAttack(attackTime / 1000.0f, oscNum);
		// };
		// osc.DecayTimeChanged += (decayTime) =>
		// {
		// 	AudioOutputNode.CurrentPatch.SetDecay(decayTime / 1000.0f, oscNum);
		// };
		// osc.SustainLevelChanged += (sustainLevel) =>
		// {
		// 	AudioOutputNode.CurrentPatch.SetSustain(sustainLevel, oscNum);
		// };
		// osc.ReleaseTimeChanged += (releaseTime) =>
		// {
		// 	AudioOutputNode.CurrentPatch.SetRelease(releaseTime / 1000.0f, oscNum);
		// };
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

		// osc.ADSRToggled += (enabled) =>
		// {
		// 	GD.PrintErr("ADSRToggled is CURRENTLY DISABLED");
		// 	//	AudioOutputNode.CurrentPatch.SetADSREnabled(enabled, oscNum);
		// };

		osc.PWMChanged += (pwm) =>
		{
			AudioOutputNode.CurrentPatch.SetPWM(pwm, oscNum);
		};

		osc.ModChanged += (mod) =>
		{
			AudioOutputNode.CurrentPatch.SetModulationStrength(mod, oscNum);
		};

	}

	// protected void ConnectCustomEnvelopes(ADSR_Envelope env, int envNum)
	// {
	// 	//Print("id is ", envNum);
	// 	env.AttackTimeChanged += (attackTime) =>
	// 	{
	// 		Print("Attack Time Changed: ", attackTime, " for id ", envNum);
	// 		AudioOutputNode.CurrentPatch.SetCustomAttack(attackTime / 1000.0f, envNum);
	// 	};
	// 	env.DecayTimeChanged += (decayTime) =>
	// 	{
	// 		// Print("Decay Time Changed: ", decayTime);
	// 		AudioOutputNode.CurrentPatch.SetCustomDecay(decayTime / 1000.0f, envNum);
	// 	};
	// 	env.SustainLevelChanged += (sustainLevel) =>
	// 	{
	// 		// Print("Sustain Level Changed: ", sustainLevel);
	// 		AudioOutputNode.CurrentPatch.SetCustomSustain(sustainLevel, envNum);
	// 	};
	// 	env.ReleaseTimeChanged += (releaseTime) =>
	// 	{
	// 		// Print("Release Time Changed: ", releaseTime);
	// 		AudioOutputNode.CurrentPatch.SetCustomRelease(releaseTime / 1000.0f, envNum);
	// 	};
	// }

	// protected void ConnectAmplitudeEnvelope()
	// {
	// 	ADSREnvelope.AttackTimeChanged += (attackTime) =>
	// 	{
	// 		AudioOutputNode.CurrentPatch.SetAttack(attackTime / 1000.0f);
	// 	};
	// 	ADSREnvelope.DecayTimeChanged += (decayTime) =>
	// 	{
	// 		AudioOutputNode.CurrentPatch.SetDecay(decayTime / 1000.0f);
	// 	};
	// 	ADSREnvelope.SustainLevelChanged += (sustainLevel) =>
	// 	{
	// 		AudioOutputNode.CurrentPatch.SetSustain(sustainLevel);
	// 	};
	// 	ADSREnvelope.ReleaseTimeChanged += (releaseTime) =>
	// 	{
	// 		AudioOutputNode.CurrentPatch.SetRelease(releaseTime / 1000.0f);
	// 	};
	// }

	void _on_patch_controls_gain_changed(float value)
	{
		AudioOutputNode.CurrentPatch.SetMasterGain(value);
	}

	void _on_patch_controls_portamento_changed(float value)
	{
		AudioOutputNode.CurrentPatch.PortamentoTime = value;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
