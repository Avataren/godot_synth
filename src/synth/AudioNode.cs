using Godot;
using System;
using System.Collections.Generic;

namespace Synth
{
	// Interface for audio processing nodes
	public abstract class AudioNode
	{
		public bool Enabled = true;
		public float SampleFrequency = 44100.0f;
		public float Amplitude { get; set; } = 1.0f;
		public float Frequency { get; set; } = 440.0f;
		public float Phase = 0.0f;
		protected float[] buffer;
		public int NumSamples;
		public bool HardSync = false;
		public string Name { get; set; }
		public Dictionary<AudioParam, List<AudioNode>> AudioParameters = new Dictionary<AudioParam, List<AudioNode>>();

		public float GetParameter(AudioParam param, int sampleIndex, float defaultVal = 0)
		{
			float value = 0.0f;
			bool hasData = false;
			if (!AudioParameters.ContainsKey(param))
			{
				return defaultVal;
			}
			foreach (AudioNode node in AudioParameters[param])
			{
				if (node.Enabled)
				{
					value += node[sampleIndex];
					hasData = true;
				}
			}
			return hasData ? value : defaultVal;
		}

		public AudioNode(int NumSamples, float SampleFrequency = 44100.0f)
		{
			this.SampleFrequency = SampleFrequency;
			this.NumSamples = NumSamples;
			buffer = new float[NumSamples];
		}

		public float[] GetBuffer()
		{
			return this.buffer;
		}

		public virtual void Process(float increment)
		{
		}

		public virtual float this[int index]
		{
			get => buffer[index];
			set => buffer[index] = value;
		}

		public AudioNode ModulateBy(AudioNode modulator)
		{
			for (int i = 0; i < NumSamples; i++)
			{
				buffer[i] *= modulator[i];
			}
			return this;
		}

		virtual public void OpenGate() { }
		virtual public void CloseGate() { }

	}

	// public class MonoSineWaveNode : AudioNode
	// {

	// 	const float PI2 = (float)(2.0 * Math.PI);

	// 	public MonoSineWaveNode(int num_samples) : base(num_samples)
	// 	{

	// 	}

	// 	public override AudioNode Process(float increment, LFOManager LFO_Manager = null)
	// 	{
	// 		for (int i = 0; i < NumSamples; i++)
	// 		{
	// 			float sample = Mathf.Sin(Phase * PI2) * Amplitude;
	// 			Phase += increment * Frequency;
	// 			Phase = Mathf.PosMod(Phase, 1.0f);
	// 			buffer[i] = sample;
	// 		}
	// 		return this;
	// 	}
	// }
}