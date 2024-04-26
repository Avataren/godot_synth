using Godot;
public partial class WaveformTextureRect : TextureRect
{
	[Export] AudioOutputNode AudioController;

	public override void _Ready()
	{
		if (AudioController == null)
		{
			GD.PrintErr("AudioController is null. Please assign an AudioOutputNode to the AudioController property.");
			return;
		}
		AudioController.BufferFilled += OnBufferFilled;
	}

	private void OnBufferFilled(float[] buffer)
	{
		// Convert buffer to an Image or a similar structure to pass to shader
		// GD.Print("Got buffer!");
		// if (buffer != null && buffer.Length > 0)
		// {
		// 	UpdateShader(buffer);
		// }
		CallDeferred("UpdateShader", buffer);
	}

	private void UpdateShader(float[] buffer)
	{
		// Assuming you have created a material and assigned it a shader that can visualize audio data
		Material.Set("shader_param/wave_data", buffer);
	}
}
