using Godot;
using Synth;
using System;

public partial class WaveformSelect : Control
{
    PatchEditor patchEditor;

    [Export]
    ColorRect colorRect;

    [Export]
    Label waveformLabel;

    [Export]
    bool isLFO = false;

    [Signal]
    public delegate void WaveformChangedEventHandler(int idx);

    [Signal]
    public delegate void UpdateNumWaveformsEventHandler(float numWaveforms);

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

    public static LFOWaveform[] LFOWaveforms = {
        LFOWaveform.Sine,
        LFOWaveform.Saw,
        LFOWaveform.Triangle,
        LFOWaveform.Square,
    };

    public override void _Ready()
    {

        // Try initializing the PatchEditor
        CallDeferred(nameof(InitializePatchEditor));
    }

    private void InitializePatchEditor()
    {
        if (isLFO)
        {
            EmitSignal(SignalName.UpdateNumWaveforms, LFOWaveforms.Length - 1.0f);
        }
        // GD.Print("Initializing waveform select");
        patchEditor = GetTree().Root.GetNode<PatchEditor>("PatchEditor");
        if (patchEditor == null)
        {
            GD.PrintErr("Patch Editor not found in the scene tree.");
        }
        _on_waveform_knob_value_changed(0);
        initialized = true;
    }

    public override void _Process(double delta)
    {
    }

    private void _on_waveoform_value_changed_LFO(int index)
    {
        if (index < 0 || index >= LFOWaveforms.Length)
        {
            GD.PrintErr($"Waveform knob value changed, but index {index} is out of bounds, max index is {LFOWaveforms.Length}");
            return;
        }
        var data = LFONode.GetWaveformData(LFOWaveforms[index], 64);
        colorRect.Material.Set("shader_parameter/curve_data", data);
        EmitSignal(SignalName.WaveformChanged, index);
    }

    private void _on_waveform_knob_value_changed(int index)
    {
        if (isLFO)
        {
            _on_waveoform_value_changed_LFO(index);
            return;
        }
        if (patchEditor == null)
        {
            GD.PrintErr("Patch Editor not set!");
            return;
        }

        if (colorRect == null)
        {
            GD.PrintErr("Color Rect not set in " + GetParent().GetParent().GetParent().GetParent().Name);
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
