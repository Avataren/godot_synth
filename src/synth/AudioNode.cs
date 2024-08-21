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
		public double Phase = 0.0f;
		public float Balance = 0.0f;
		protected float[] buffer;
		public float[] LeftBuffer;
		public float[] RightBuffer;
		public int NumSamples;
		public bool HardSync = false;
		public string Name { get; set; }
		public Dictionary<AudioParam, List<ParameterConnection>> AudioParameters = new Dictionary<AudioParam, List<ParameterConnection>>();
		private Dictionary<AudioParam, List<ParameterConnection>> originalConnections = new Dictionary<AudioParam, List<ParameterConnection>>();

		public static double ModuloOne(double val)
		{
			return (val + 1.0) % 1.0;
			// val = Math.IEEERemainder(val, 1.0);
			// if (val < 0)
			// {
			// 	val += 1.0;
			// }
			// return val;
		}
		public Tuple<float, float> GetParameter(AudioParam param, int sampleIndex, float defaultVal = 0)
		{

			if (!AudioParameters.ContainsKey(param))
			{
				return Tuple.Create(defaultVal, 1.0f);
			}
			float adds = 0.0f;
			float muls = 1.0f;
			foreach (var ap in AudioParameters[param])
			{
				if (ap.SourceNode.Enabled)
				{
					if (ap.ModType == ModulationType.Add)
					{
						adds += ap.SourceNode[sampleIndex] * ap.Strength;
					}
					else
					{
						muls *= ap.SourceNode[sampleIndex] * ap.Strength;
					}
				}
			}
			return Tuple.Create(adds, muls);
		}

		public List<AudioNode> GetParameterNodes(AudioParam param)
		{
			if (!AudioParameters.ContainsKey(param))
			{
				return null;
			}
			return AudioParameters[param].ConvertAll(x => x.SourceNode);
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

		public virtual void Process(double increment)
		{
		}

		public virtual float this[int index]
		{
			get => buffer[index];
			set => buffer[index] = value;
		}

		virtual public void OpenGate() { }
		virtual public void CloseGate() { }

	}
}