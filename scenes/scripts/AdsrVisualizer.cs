using Godot;
using Synth;
public partial class AdsrVisualizer : PanelContainer
{
	[Export]
	private ButtonGroup button_group;
	[Export]
	private ColorRect ShaderRect;
	EnvelopeNode currentEnvelopeNode = new EnvelopeNode();
	const int MaxEnvelopes = 5;
	EnvelopeNode[] envelopeNodes = new EnvelopeNode[MaxEnvelopes];
	float[] visualBuffer = new float[512];
	ShaderMaterial node_shader_material;

	[Signal]
	public delegate void AttackUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void DecayUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void SustainUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void ReleaseUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void AttackCoeffUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void DecayCoeffUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void ReleaseCoeffUpdatedEventHandler(float attackTime);
	[Signal]
	public delegate void TimeScaleUpdatedEventHandler(float attackTime);


	[Signal]
	public delegate void AttackVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void DecayVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void SustainVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void ReleaseVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void AttackCoeffVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void DecayCoeffVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void ReleaseCoeffVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);
	[Signal]
	public delegate void TimeScaleVoiceUpdatedEventHandler(float attackTime, int envelopeIndex);

	int EnvelopeIndex = 0;
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
		EnvelopeIndex = int.Parse(button.Name.ToString().Substring(14)) - 1;

		if (EnvelopeIndex >= MaxEnvelopes || EnvelopeIndex < 0)
		{
			GD.PrintErr("Envelope index out of range!", EnvelopeIndex);
			return;
		}
		GD.Print("Button " + EnvelopeIndex + " pressed!");
		if (envelopeNodes[EnvelopeIndex] == null)
		{
			GD.PrintErr("Envelope node not set!");
			return;
		}
		currentEnvelopeNode = envelopeNodes[EnvelopeIndex];
		EmitSignal(SignalName.AttackUpdated, currentEnvelopeNode.AttackTime / currentEnvelopeNode.TimeScale);
		EmitSignal(SignalName.DecayUpdated, currentEnvelopeNode.DecayTime / currentEnvelopeNode.TimeScale);
		EmitSignal(SignalName.SustainUpdated, currentEnvelopeNode.SustainLevel);
		EmitSignal(SignalName.ReleaseUpdated, currentEnvelopeNode.ReleaseTime / currentEnvelopeNode.TimeScale);
		EmitSignal(SignalName.AttackCoeffUpdated, currentEnvelopeNode.AttackCtrl);
		EmitSignal(SignalName.DecayCoeffUpdated, currentEnvelopeNode.DecayCtrl);
		EmitSignal(SignalName.ReleaseCoeffUpdated, currentEnvelopeNode.ReleaseCtrl);
		EmitSignal(SignalName.TimeScaleUpdated, currentEnvelopeNode.TimeScale);


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
		EnvelopeIndex = index;
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

	private void UpdateGraph()
	{
		visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		node_shader_material.SetShaderParameter("wave_data", visualBuffer);
		node_shader_material.SetShaderParameter("total_time", TimeScale * 3.0f);
	}
	private void _on_time_knob_value_changed(float val)
	{
		TimeScale = val;
		EmitSignal(SignalName.TimeScaleVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_attack_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		EmitSignal(SignalName.AttackCoeffVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_decay_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		EmitSignal(SignalName.DecayCoeffVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_release_c_knob_value_changed(float val)
	{
		if (Mathf.Abs(val) < 0.0015)
			val = 0.0015f;
		//currentEnvelopeNode.ReleaseCtrl = val;
		EmitSignal(SignalName.ReleaseCoeffVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_attack_knob_value_changed(float val)
	{
		//currentEnvelopeNode.AttackTime = val;
		EmitSignal(SignalName.AttackVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
		//visualBuffer = currentEnvelopeNode.GetVisualBuffer(512, 3.0f);
		//node_shader_material.SetShaderParameter("wave_data", visualBuffer);
	}

	private void _on_decay_knob_value_changed(float val)
	{
		//currentEnvelopeNode.DecayTime = val;
		EmitSignal(SignalName.DecayVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_sustain_knob_value_changed(float val)
	{
		//currentEnvelopeNode.SustainLevel = val;
		EmitSignal(SignalName.SustainVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

	private void _on_release_knob_value_changed(float val)
	{
		//currentEnvelopeNode.ReleaseTime = val;
		EmitSignal(SignalName.ReleaseVoiceUpdated, val, EnvelopeIndex);
		UpdateGraph();
	}

}
