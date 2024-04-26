using Godot;
public partial class WaveformTextureRect : TextureRect
{
	[Export] AudioOutputNode AudioController;
	ShaderMaterial node_shader_material;
	public override void _Ready()
	{
		node_shader_material = (ShaderMaterial)Material;
		if (AudioController == null)
		{
			GD.PrintErr("AudioController is null. Please assign an AudioOutputNode to the AudioController property.");
			return;
		}
		AudioController.BufferFilled += OnBufferFilled;
	}

	private void OnBufferFilled(float[] buffer)
	{

		CallDeferred("UpdateShader", buffer);
	}

	private void UpdateShader(float[] buffer)
	{
		node_shader_material.SetShaderParameter("wave_data", buffer);
	}
}
