using Godot;
using System;

using Synth;
public partial class AppMain : Control
{
	public enum EditorMode
	{
		Idle,
		PlayPattern,
		PlaySong,
		Edit
	}

	[Signal]
	public delegate void SetPatternPositionEventHandler(int value);

	[Export]
	public Button PlayPatternButton;
	[Export]
	public Control trackerMain;

	EditorMode editorMode = EditorMode.Idle;
	double start_time = 0.0;

	double bpm = 120.0;
	double beats_per_bar = 4.0;
	double bars_per_pattern = 4.0;

	int lastPatternPosition = 0;
	int PatternLength = 64;
	public void PlayPattern()
	{
		switch (editorMode)
		{
			case EditorMode.PlayPattern:
				editorMode = EditorMode.Idle;
				EmitSignal(SignalName.SetPatternPosition, 0);
				lastPatternPosition = 0;
				PlayPatternButton.Text = "Play Pattern";
				break;

			default:
				PlayPatternButton.Text = "Stop";
				editorMode = EditorMode.PlayPattern;
				start_time = GetCurrentTimeInSeconds();
				break;
		}

	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public override void _Process(double delta)
	{
		switch (editorMode)
		{
			case EditorMode.PlayPattern:
				// Time elapsed since the pattern started playing
				double elapsedTime = GetCurrentTimeInSeconds() - start_time;

				// Calculate beats per second (BPM -> seconds per beat)
				double secondsPerBeat = 60.0 / bpm;

				// Calculate total beats elapsed
				double totalBeatsElapsed = elapsedTime / secondsPerBeat * beats_per_bar;

				// Get the current position in the pattern
				int patternPos = (int)(totalBeatsElapsed % PatternLength);
				if (patternPos != lastPatternPosition)
				{
					lastPatternPosition = patternPos;
					GD.Print("Emitting pattern position: " + patternPos);
					EmitSignal(SignalName.SetPatternPosition, patternPos);
				}
				break;

			case EditorMode.PlaySong:
				// Handle song playback
				break;

			case EditorMode.Edit:
				// Handle edit mode
				break;

			default:
				break;
		}
	}


	private double GetCurrentTimeInSeconds()
	{
		//AudioContext.Scheduler.CurrentTimeInSeconds
		return Time.GetTicksUsec() / 1000000.0;
	}
}
