using Godot;
using System;
using Synth;
public partial class AdsrVisualizer : PanelContainer
{
	[Export]
	private Control attack_knob;

	[Export]
	private ColorRect ShaderRect;
	EnvelopeNode envelopeNode = new EnvelopeNode(512, 44100);
	float[] visualBuffer = new float[512];
	ShaderMaterial node_shader_material;
	public override void _Ready()
	{
		node_shader_material = (ShaderMaterial)ShaderRect.Material;
		visualBuffer = envelopeNode.GetVisualBuffer(512);
		// for (int i = 0; i < visualBuffer.Length; i++)
		// {
		// 	GD.Print("visualBuffer[" + i + "] = " + visualBuffer[i]);
		// }
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_attack_knob_value_changed(float val)
	{
		envelopeNode.AttackTime = val;
		visualBuffer = envelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_decay_knob_value_changed(float val)
	{
		envelopeNode.DecayTime = val;
		visualBuffer = envelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_sustain_knob_value_changed(float val)
	{
		envelopeNode.SustainLevel = val;
		visualBuffer = envelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_release_knob_value_changed(float val)
	{
		envelopeNode.ReleaseTime = val;
		visualBuffer = envelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

}
