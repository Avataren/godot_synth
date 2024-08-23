using Godot;
using System;
using System.Collections.Generic;

namespace Synth
{
	// Interface for audio processing nodes
	public abstract class AudioNode
	{
		public bool Enabled = true;
		public float SampleRate = 44100.0f;
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
		//private Dictionary<AudioParam, List<ParameterConnection>> originalConnections = new Dictionary<AudioParam, List<ParameterConnection>>();
		protected ParameterScheduler _scheduler = AudioContext.Scheduler;

		public static double ModuloOne(double val)
		{
			return (val + 100.0) % 1.0;
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

		public AudioNode()
		{
			NumSamples = AudioContext.BufferSize;
			SampleRate = AudioContext.SampleRate;
			buffer = new float[NumSamples];
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

		public virtual void ScheduleValueAtTime(AudioParam param, double value, double time = 0.0, double initialValue = 0.0)
		{
			_scheduler.ScheduleValueAtTime(this, param, value, time, initialValue);
		}

   		public void LinearRampToValueAtTime(AudioParam param, double targetValue,  double endTimeInSeconds)
		{
			_scheduler.LinearRampToValueAtTime(this, param, targetValue, endTimeInSeconds);
		}
	}
}