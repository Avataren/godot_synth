using static Godot.GD;
using Godot;
using System;
using System.Threading;
using Godot.Collections;
using Synth;

public partial class AudioOutputNode : AudioStreamPlayer
{
	[Signal]
	public delegate void PerformanceTimeUpdateEventHandler(float process_time, float buffer_push_time, int available_frames);
	public delegate void BufferFilledEventHandler(float[] buffer);
	public event BufferFilledEventHandler BufferFilled;

	[Export] int num_samples = 512;
	private Vector2[] audioData;
	private float[] buffer_copy;
	private AudioStreamGeneratorPlayback _playback;
	private float SampleRate;
	private Thread sound_thread;
	private bool run_sound_thread = true;
	private WaveTableBank waveTableBank;
	int KeyDownCount = 0;
	public int BaseOctave = 2;
	public SynthPatch CurrentPatch;
	private int AvailableFrames = 0;
	public int process_time = 0;
	public int total_time = 0;
	public override void _Ready()
	{

		base._Ready();
		buffer_copy = new float[num_samples];
		audioData = new Vector2[num_samples];
		if (Stream is AudioStreamGenerator generator)
		{
			try
			{
				GD.Print("Initializing AudioOutputNode at " + generator.MixRate + " Hz");
				AudioContext.SampleRate = (int)generator.MixRate;
				AudioContext.BufferSize = num_samples;
				SampleRate = generator.MixRate * AudioContext.Oversampling;
				waveTableBank = new WaveTableBank();
				CurrentPatch = new SynthPatch(waveTableBank, num_samples, SampleRate);
			}
			catch (Exception e)
			{
				PrintErr(e.Message);
				PrintErr(e.StackTrace);
			}

			Play();
			_playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();

			sound_thread = new Thread(new ThreadStart(FillBuffer))
			{
				Priority = ThreadPriority.Highest
			};
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
		if (@event is InputEventKey eKey)
		{
			if (eKey.Echo)
			{
				return;
			}
		}

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

		if (@event is InputEventKey eventKey)
		{
			int semitones;
			try
			{
				semitones = keyMap[eventKey.Keycode];
			}
			catch (Exception)
			{
				GD.PrintErr("Key not found in keyMap: " + eventKey.Keycode);
				return;
			}

			var note = semitones + 12 * BaseOctave;
			if (!eventKey.Pressed)
			{
				//if (keyMap.ContainsKey(eventKey.Keycode))
				{
					//KeyDownCount--;
					//if (KeyDownCount <= 0)
					{
						//envelopeNode.CloseGate();
						CurrentPatch.NoteOff(note);
						//KeyDownCount = 0;
						//CurrKey = Key.None;
					}
				}
			}
			else if (eventKey.Pressed)
			{
				//if (keyMap.ContainsKey(eventKey.Keycode))
				{
					//if (CurrKey != eventKey.Keycode)
					//{
					//	KeyDownCount++;
					//CurrKey = eventKey.Keycode;
					//}

					//envelopeNode.OpenGate();

					//waveTableNode.Frequency = CalculateFrequency(BaseOctave, semitones);
					CurrentPatch.NoteOn(note);
				}
			}

			// if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
			// 	GetTree().Quit();
		}
	}

	public void Debug()
	{
		//CurrentPatch.graph.DebugPrint();
	}

	public void PrepareGraph()
	{
		//CurrentPatch.graph.TopologicalSortWorkingGraph();
	}

	public void ClearKeyStack()
	{
		CurrentPatch.ClearKeyStack();
	}

	public void Connect(string srcName, string dstName, string param, ModulationType modType, float strength = 1.0f)
	{
		CurrentPatch.ConnectInVoices(srcName, dstName, param, modType, strength);
	}

	public void Disconnect(string srcName, string dstName, string param)
	{
		CurrentPatch.DisconnectInVoices(srcName, dstName, param);
	}

	public void FillBuffer()
	{
		double increment = 1.0 / SampleRate;

		var audioctx = AudioContext.Instance;
		while (run_sound_thread)
		{
			var timestamp_now = Time.GetTicksUsec();
			audioctx.Process(increment);
			var mix = CurrentPatch.Process(increment);

			// Pre-calculate length to avoid repetitive property access
			int leftRightBufferLength = mix.LeftBuffer.Length;
			SynthType repr = SynthTypeHelper.One / AudioContext.Oversampling;
			// Optimize loop by using a single loop instead of nested loops
			for (int i = 0; i < num_samples; i++)
			{
				int baseIndex = i * AudioContext.Oversampling;
				SynthType left = 0.0f;
				SynthType right = 0.0f;

				// Sum the oversampled data in one loop
				for (int j = 0; j < AudioContext.Oversampling; j++)
				{
					int sampleIndex = baseIndex + j;
					//if (sampleIndex < leftRightBufferLength) // Boundary check
					{
						left += mix.LeftBuffer[sampleIndex];
						right += mix.RightBuffer[sampleIndex];
					}
				}
				audioData[i][0] = (float)(left * repr);
				audioData[i][1] = (float)(right * repr);

				// Mix buffer average directly in the same loop
				buffer_copy[i] = (float)((left + right) * repr / 2);
			}
			var timestamp_process_done = Time.GetTicksUsec();
			// Avoid tight loop and sleep
			while (!_playback.CanPushBuffer(num_samples))
			{
				Thread.Sleep(0);
			}
			//AvailableFrames = _playback.GetFramesAvailable();
			_playback.PushBuffer(audioData);
			var timestamp_buffer_pushed = Time.GetTicksUsec();
			process_time = (int)(timestamp_process_done - timestamp_now);
			total_time = (int)(timestamp_buffer_pushed - timestamp_now);
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
		EmitSignal(SignalName.PerformanceTimeUpdate, process_time, total_time, AvailableFrames);
	}

}
