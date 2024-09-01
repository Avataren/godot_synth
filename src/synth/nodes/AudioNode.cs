using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Synth
{
	public enum InputType
	{
		Mono,
		Stereo,
		Both
	}

	// Interface for audio processing nodes
	public abstract class AudioNode
	{
		public bool Enabled = true;
		public SynthType SampleRate = 44100.0f;
		public SynthType Amplitude { get; set; } = SynthTypeHelper.One;
		//public SynthType Frequency { get; set; } = SynthTypeHelper.Zero;
		public SynthType Phase = 0.0f;
		public SynthType Balance = SynthTypeHelper.Zero;
		protected SynthType[] buffer;
		public SynthType[] LeftBuffer;
		public SynthType[] RightBuffer;
		public int NumSamples;
		public bool HardSync = false;
		public string Name { get; set; }
		public Dictionary<AudioParam, List<ParameterConnection>> AudioParameters = new Dictionary<AudioParam, List<ParameterConnection>>();
		//private Dictionary<AudioParam, List<ParameterConnection>> originalConnections = new Dictionary<AudioParam, List<ParameterConnection>>();
		protected ParameterScheduler _scheduler = AudioContext.Scheduler;
		public InputType AcceptedInputType { get; set; } = InputType.Mono; // Default to stereo, can be overridden



		public ReadOnlySpan<SynthType> GetBuffer() => buffer;
		public ReadOnlySpan<SynthType> GetLeftBuffer() => LeftBuffer;
		public ReadOnlySpan<SynthType> GetRightBuffer() => RightBuffer;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Tuple<SynthType, SynthType> GetParameter(AudioParam param, int sampleIndex, SynthType defaultVal = 0)
		{

			if (!AudioParameters.ContainsKey(param))
			{
				return Tuple.Create(defaultVal, SynthTypeHelper.One);
			}
			SynthType adds = SynthTypeHelper.Zero;
			SynthType muls = SynthTypeHelper.One;
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
			buffer = new SynthType[NumSamples];
		}

		public virtual void Process(double increment)
		{
		}

		public virtual SynthType this[int index]
		{
			get => buffer[index];
			set => buffer[index] = value;
		}

		virtual public void OpenGate() { }
		virtual public void CloseGate() { }

		public virtual void ScheduleValueAtTime(AudioParam param, double value, double time = 0.0)
		{
			_scheduler.ScheduleValueAtTime(this, param, value, time);
		}

		public void LinearRampToValueAtTime(AudioParam param, double targetValue, double endTimeInSeconds)
		{
			_scheduler.LinearRampToValueAtTime(this, param, targetValue, endTimeInSeconds);
		}

		public void ExponentialRampToValueAtTime(AudioParam param, double targetValue, double endTimeInSeconds)
		{
			_scheduler.ExponentialRampToValueAtTime(this, param, targetValue, endTimeInSeconds);
		}
	}
}