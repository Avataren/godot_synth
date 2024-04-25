using Godot;
using System;

// Interface for audio processing nodes
public class AudioNode
{
    public float SampleFrequency = 44100.0f;
    public float Amplitude { get; set; } = 0.25f;
	public float Frequency {get; set;} = 440.0f;
	protected float Phase = 0.0f;
	protected float[] buffer;
	public int NumSamples;
	
	public AudioNode(int NumSamples){
		this.NumSamples = NumSamples;
		buffer = new float[NumSamples];
	}
	
	public float[] GetBuffer() {
		return this.buffer;
	}
	
	public virtual AudioNode Process(float increment) {
		return this;
	}
	
	public float this[int index]
	{
		get => buffer[index];
		set => buffer[index] = value;
	}

	public AudioNode ModulateBy (AudioNode modulator) {
		for (int i=0;i<NumSamples;i++) {
			buffer[i] *= modulator[i];
		}
		return this;
	}	

}

public class MonoSineWaveNode : AudioNode
{

	const float PI2 = (float)(2.0*Math.PI);
	
	public MonoSineWaveNode(int num_samples) : base(num_samples) {

	}	
	
	public override AudioNode Process(float increment)
	{
		for (int i=0;i<NumSamples;i++) {
			float sample = Mathf.Sin(Phase * PI2) * Amplitude;
			Phase += increment * Frequency;  
			Phase = Mathf.PosMod(Phase, 1.0f);
			buffer[i] = sample;
		}
		return this;
	}
}
