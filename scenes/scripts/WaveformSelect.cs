using Godot;
using System;

public partial class WaveformSelect : Control
{
    PatchEditor patchEditor;

    [Export]
	ColorRect colorRect;

	[Export]
	Label waveformLabel;

	[Signal]
	public delegate void WaveformChangedEventHandler(int idx);

    private bool initialized = false;

    string[] waveforms = [
		"Sine",
		"Triangle",
		"Square",
		"Saw",
		"Noise",
		"Organ",
		"Organ2",
		"Bass",
		"Ahh",
		"Fuzzy",
		"Piano",
		"PWM",
	];

    public override void _Ready()
    {

        // Try initializing the PatchEditor
        CallDeferred(nameof(InitializePatchEditor));
    }

    private void InitializePatchEditor()
    {
        GD.Print("Initializing waveform select");
 		patchEditor = GetTree().Root.GetNode<PatchEditor>("PatchEditor");
        if (patchEditor == null)
        {
            GD.PrintErr("Patch Editor not found in the scene tree.");
        }
        else
        {
            _on_waveform_knob_value_changed(0);
        }
		initialized = true;
    }	

    public override void _Process(double delta)
    {
    }

    private void _on_waveform_knob_value_changed(int index)
    {
        if (patchEditor == null)
        {
            GD.PrintErr("Patch Editor not set!");
            return;
        }

        if (colorRect == null)
        {
            GD.PrintErr ("Color Rect not set in " + GetParent().GetParent().GetParent().GetParent().Name);
        }
		try
		{
        	var data = patchEditor.GetWaveformData(waveforms[index]);
			waveformLabel.Text = waveforms[index];
			GD.Print("Waveform knob value changed, setting data");
        	colorRect.Material.Set("shader_parameter/curve_data", data);
			EmitSignal(SignalName.WaveformChanged, index);
		}
		catch (IndexOutOfRangeException)
		{
			GD.PrintErr($"Waveform knob value changed, but index {index} is out of bounds, max index is {waveforms.Length}");
		}
    }

}
