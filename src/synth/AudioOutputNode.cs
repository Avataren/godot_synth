using static Godot.GD;
using Godot;
using System;
using System.Threading;
using Godot.Collections;
using Synth;

public partial class AudioOutputNode : AudioStreamPlayer
{
	public delegate void BufferFilledEventHandler(float[] buffer);
	public event BufferFilledEventHandler BufferFilled;

	[Export] int num_samples = 1024;
	private Vector2[] audioData;
	private float[] buffer_copy;
	private AudioStreamGeneratorPlayback _playback;
	private float _sampleHz;
	private Thread sound_thread;
	private bool run_sound_thread = true;
	private WaveTableBank waveTableBank;
	int KeyDownCount = 0;
	public int BaseOctave = 2;
	public SynthPatch CurrentPatch;
	public override void _Ready()
	{

		base._Ready();
		buffer_copy = new float[num_samples];
		audioData = new Vector2[num_samples];
		if (Stream is AudioStreamGenerator generator)
		{
			try
			{
				waveTableBank = new WaveTableBank();
				CurrentPatch = new SynthPatch(waveTableBank);
			}
			catch (Exception e)
			{
				PrintErr(e.Message);
				PrintErr(e.StackTrace);
			}

			_sampleHz = 44100.0f;// generator.MixRate;
			Play();
			_playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();

			// Initialize nodes
			//waveTableNode = new WaveTableOscillatorNode(num_samples, _sampleHz, waveTableBank.GetWave(WaveTableWaveType.SINE));
			//envelopeNode = new EnvelopeNode(num_samples);

			sound_thread = new Thread(new ThreadStart(FillBuffer));
			sound_thread.Start();
		}
	}

	private void SetSliderValue(Godot.Slider slider, float val)
	{
		if (slider == null)
		{
			GD.Print("Slider is NULL!");
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

	protected void _on_option_octave_item_selected(int index)
	{
		BaseOctave = index;
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
					if (KeyDownCount <= 0)
					{
						//envelopeNode.CloseGate();
						CurrentPatch.NoteOff();
						KeyDownCount = 0;
						CurrKey = Key.None;
					}
				}
			}
			else if (eventKey.Pressed)
			{
				if (keyMap.ContainsKey(eventKey.Keycode))
				{
					if (CurrKey != eventKey.Keycode)
					{
						KeyDownCount++;
						CurrKey = eventKey.Keycode;
					}

					//envelopeNode.OpenGate();
					var semitones = keyMap[eventKey.Keycode];
					//waveTableNode.Frequency = CalculateFrequency(BaseOctave, semitones);
					CurrentPatch.NoteOn(semitones + 12 * BaseOctave);
				}
			}

			if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
				GetTree().Quit();
		}
	}

	public void FillBuffer()
	{
		var mix_buffer = new float[num_samples];
		float increment = 1.0f / _sampleHz;
		while (run_sound_thread)
		{
			if (_playback.CanPushBuffer(num_samples))
			{
				CurrentPatch.Process(increment, mix_buffer);

				for (int i = 0; i < num_samples; i++)
				{
					audioData[i][0] = mix_buffer[i];
					audioData[i][1] = mix_buffer[i];
				}
				_playback.PushBuffer(audioData);
				System.Array.Copy(mix_buffer, buffer_copy, num_samples);
			}
			else
			{
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

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		BufferFilled?.Invoke(buffer_copy);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

}
