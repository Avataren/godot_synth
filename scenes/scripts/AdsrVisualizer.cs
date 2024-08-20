using Godot;
using System;
using Synth;
using System.Collections.Generic;
public partial class AdsrVisualizer : PanelContainer
{
	[Export]
	private ButtonGroup button_group;
	[Export]
	private ColorRect ShaderRect;
	EnvelopeNode currentEnvelopeNode = new EnvelopeNode(512, 44100);
	const int MaxEnvelopes = 5;
	EnvelopeNode[] envelopeNodes = new EnvelopeNode[MaxEnvelopes];
	float[] visualBuffer = new float[512];
	ShaderMaterial node_shader_material;

	float TimeScale = 1.0f;
	public override void _Ready()
	{
		node_shader_material = (ShaderMaterial)ShaderRect.Material;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512);
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, TimeScale * 3.0f);
		// for (int i = 0; i < visualBuffer.Length; i++)
		// {
		// 	GD.Print("visualBuffer[" + i + "] = " + visualBuffer[i]);
		// }
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
		ConnectEnvelopeSelectButtons();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ConnectEnvelopeSelectButtons()
	{
		if (button_group == null)
		{
			GD.PrintErr("Button group not set!");
			return;
		}
		button_group.Pressed += OnButtonDown;
	}

	private void OnButtonDown(BaseButton button)
	{
		//remove "EnvelopeButton" from button name
		var envelopeIndex = int.Parse(button.Name.ToString().Substring(14)) - 1;
		if (envelopeIndex >= MaxEnvelopes || envelopeIndex < 0)
		{
			GD.PrintErr("Envelope index out of range!");
			return;
		}
		GD.Print("Button " + envelopeIndex + " pressed!");
		if (envelopeNodes[envelopeIndex] == null)
		{
			GD.PrintErr("Envelope node not set!");
			return;
		}
		currentEnvelopeNode = envelopeNodes[envelopeIndex];
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	public void SetActiveEnvelopeIndex(int index)
	{
		if (index >= MaxEnvelopes || index < 0)
		{
			GD.PrintErr("Envelope index out of range!");
			return;
		}
		if (envelopeNodes[index] == null)
		{
			GD.PrintErr("Envelope node not set!");
			return;
		}
		currentEnvelopeNode = envelopeNodes[index];
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	public void SetADSRNodeReference(EnvelopeNode node, int index)
	{
		//assert index < MaxEnvelopes
		if (index >= MaxEnvelopes || index < 0)
		{
			GD.PrintErr("Envelope index out of range!");
			return;
		}
		GD.Print("Setting node reference for index " + index + " to " + node.Name);
		envelopeNodes[index] = node;
	}
	private void _on_time_knob_value_changed(float val)
	{
		TimeScale = val;
		currentEnvelopeNode.TimeScale = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
		node_shader_material.SetShaderParameter("total_time", TimeScale* 3.0f);
	}

	private void _on_attack_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		currentEnvelopeNode.AttackCtrl = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_decay_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		currentEnvelopeNode.DecayCtrl = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512,  3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_release_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		currentEnvelopeNode.ReleaseCtrl = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512,  3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_attack_knob_value_changed(float val)
	{
		currentEnvelopeNode.AttackTime = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512,  3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_decay_knob_value_changed(float val)
	{
		currentEnvelopeNode.DecayTime = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_sustain_knob_value_changed(float val)
	{
		currentEnvelopeNode.SustainLevel = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512,  3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_release_knob_value_changed(float val)
	{
		currentEnvelopeNode.ReleaseTime = val;
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

}
