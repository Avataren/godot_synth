using static Godot.GD;
using Godot;
using System;
using System.Threading;
using Godot.Collections;
using Synth;

public partial class AudioOutputNode : AudioStreamPlayer
{
	[Export] int num_samples = 1024;
	const int AUDIOBUFFER_SIZE = 512;
	private AudioStreamGeneratorPlayback _playback;
	private float _sampleHz;
	private Vector2[] audioData;
	float sampleTime = 0.0f;
	// Define nodes for each channel
	private WaveTableOscillatorNode waveTableNode;
	private EnvelopeNode envelopeNode;
	private Thread sound_thread;
	private bool run_sound_thread = true;
	private WaveTableBank waveTableBank;
	float Amplitude = 0.0f;
	int KeyDownCount = 0;
	public int BaseOctave = 2;

	public override void _Ready()
	{
		
		base._Ready();
		if (Stream is AudioStreamGenerator generator)
		{
			try
			{
				waveTableBank = new WaveTableBank();
			}
			catch (Exception e)
			{
				PrintErr(e.Message);
				PrintErr(e.StackTrace);
			}

			_sampleHz = generator.MixRate;
			Play();
			_playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();
			audioData = new Vector2[num_samples];

			// Initialize nodes
			waveTableNode = new WaveTableOscillatorNode(num_samples, _sampleHz, waveTableBank.GetWave(WaveTableWaveType.SINE));
			envelopeNode = new EnvelopeNode(num_samples);
			
			sound_thread = new Thread(new ThreadStart(FillBuffer));
			sound_thread.Start();
		}
	}
	
	private void SetSliderValue (Godot.Slider slider, float val){
		if (slider == null) {
			GD.Print ("Slider is NULL!");
			return;
		}
		slider.Value = val;
	}

	private float CalculateFrequency(int baseOctave, int semitones)
	{
		// Assume A4 is 440 Hz and is the 4th octave
		float baseFrequencyA4 = 440.0f;
		int octaveDifference = baseOctave - 5;
		float baseFrequency = baseFrequencyA4 * (float)Math.Pow(2, octaveDifference);

		return (float)(baseFrequency * Math.Pow(2, semitones / 12.0));
	}

	protected void _on_option_octave_item_selected (int index)
	{
		BaseOctave = index;
	}

	protected void _on_option_button_item_selected (int index)
	{
		switch (index)
		{
			case 0:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.SINE);
				break;
			case 1:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.TRIANGLE);
				break;
			case 2:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.SQUARE);
				break;
			case 3:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.SAWTOOTH);
				break;
			case 4:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.ORGAN);
				break;
			case 5:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.ORGAN2);
				break;
			case 6:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.BASS);
				break;
			case 7:
				waveTableNode.WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.VOCAL_AHH);
				break;									
		}
	}

	Godot.Key CurrKey = Key.None;
	public override void _UnhandledInput(InputEvent @event)
	{
		var keyMap = new Dictionary<Godot.Key, int> {
			{ Key.Q, 0 +12},
			{ Key.Key2, 1 +12},
			{ Key.W, 2 +12},
			{ Key.Key3, 3 +12},
			{ Key.E, 4 +12},
			{ Key.R, 5 +12},
			{ Key.Key5, 6 +12},
			{ Key.T, 7 +12},
			{ Key.Key6, 8 +12},
			{ Key.Y, 9 +12},
			{ Key.Key7, 10 +12},
			{ Key.U, 11 +12},
			{ Key.I, 12 +12},
			{ Key.Key9, 13 +12},
			{ Key.O, 14 +12},
			{ Key.Key0, 15 +12},

			{ Key.Z, 0},
			{ Key.S, 1},
			{ Key.X, 2},
			{ Key.D, 3},
			{ Key.C, 4},
			{ Key.V, 5},
			{ Key.G, 6},
			{ Key.B, 7},
			{ Key.H, 8},
			{ Key.N, 9},
			{ Key.J, 10},
			{ Key.M, 11},
			{ Key.Comma, 12},
			{ Key.L, 13},
			{ Key.Period, 14},
			{ Key.Quoteleft, 15},
		};

		if (@event is InputEventKey eventKey && !eventKey.Echo)
		{
			if (!eventKey.Pressed)
			{
				if (keyMap.ContainsKey(eventKey.Keycode))
				{
					KeyDownCount--;
					if (KeyDownCount <= 0) {
						envelopeNode.CloseGate();
						KeyDownCount = 0;
						CurrKey = Key.None;
					}
				}
			}
			else if (eventKey.Pressed)
			{
				if (keyMap.ContainsKey(eventKey.Keycode))
				{
					if (CurrKey != eventKey.Keycode) {
						KeyDownCount++;
						CurrKey = eventKey.Keycode;
					}
					
					envelopeNode.OpenGate();
					var semitones = keyMap[eventKey.Keycode];
					waveTableNode.Frequency = CalculateFrequency(BaseOctave, semitones);
				}
			}

			if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
				GetTree().Quit();
		}
	}

	public void FillBuffer()
	{
		while (run_sound_thread)
		{
			float increment = 1.0f / _sampleHz;
			if (_playback.CanPushBuffer(num_samples))
			{
				envelopeNode.Process(increment);
				var samples = waveTableNode.Process(increment);
				samples.ModulateBy(envelopeNode);
				var left = samples;
				var right = samples;
				for (int i = 0; i < num_samples; i++)
				{
					audioData[i][0] = left[i];
					audioData[i][1] = right[i];
				}
				_playback.PushBuffer(audioData);
			}
			else
			{
				//int SleepTime = 10;
				int SleepTime = (int)((num_samples * 0.5 / _sampleHz) * 1000);
				Thread.Sleep(SleepTime);
			}
		}
	}


	public override void _ExitTree()
	{
		base._ExitTree();
		Print("Ending sound..");
		run_sound_thread = false;
		sound_thread.Join();
		Print("Sound ended!");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	private void _on_adsr_envelope_attack_time_changed(float attackTime)
	{
		envelopeNode.AttackTime = attackTime/1000.0f;
	}
	private void _on_adsr_envelope_decay_time_changed(float decayTime)
	{
		envelopeNode.DecayTime = decayTime/1000.0f;
	}
	private void _on_adsr_envelope_release_time_changed(float releaseTime)
	{
		envelopeNode.ReleaseTime = releaseTime/1000.0f;
	}
	private void _on_adsr_envelope_sustain_level_changed(float sustainLevel)
	{
		envelopeNode.SustainLevel = sustainLevel;
	}

}
