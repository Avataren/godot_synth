using System;
using System.Threading;
using Godot;

namespace Synth
{
	public class WaveTableOscillatorNode : AudioNode
	{
		private readonly object _lock = new object();
		private WaveTableMemory _waveTableMemory;
		private int _currentWaveTableIndex;
		//private double _phaseAccumulator;
		private float _lastFrequency = -1f;
		private float _smoothModulationStrength;
		private float _detuneFactor;
		private float _previousSample;

		private const double TwoPi = Math.PI * 2.0;
		private const float MinPWMDutyCycle = 0.001f;
		private const float MaxPWMDutyCycle = 0.999f;

		public float DetuneCents { get; set; } = 0.0f;
		public float DetuneSemitones { get; set; } = 0.0f;
		public float DetuneOctaves { get; set; } = 0.0f;
		public float ModulationStrength { get; set; } = 0.0f;
		public float SelfModulationStrength { get; set; } = 0.0f;
		public float PhaseOffset { get; set; } = 0.0f;
		public bool IsPWM { get; set; } = false;

		private double _targetPhase;         // The phase you want to transition to
		private double _phaseTransitionSpeed; // Speed of the phase transition
		private bool _isPhaseTransitioning;  // Flag to indicate if a transition is happening


		Tuple<float, float> pitchParam;
		Tuple<float, float> gainParam;
		Tuple<float, float> pmodParam;
		Tuple<float, float> phaseParam;
		Tuple<float, float> pwmParam;
		float Gain = 1.0f;
		float FrequencyChangeThreshold = 0.000001f;
		private float _pwmDutyCycle = 0.5f;
		public float PWMDutyCycle
		{
			get => _pwmDutyCycle;
			set => _pwmDutyCycle = Math.Clamp(value, MinPWMDutyCycle, MaxPWMDutyCycle);
		}

		private float PWMAdd { get; set; } = 0.0f;
		private float PWMMultiply { get; set; } = 1.0f;

		public delegate float WaveTableFunction(WaveTable waveTable, double phase);
		public WaveTableFunction GetSampleFunction { get; private set; }

		public WaveTableMemory WaveTableMemory
		{
			get => _waveTableMemory;
			set
			{
				lock (_lock)
				{
					_waveTableMemory = value;
					UpdateWaveTableFrequency(440.0f);
				}
			}
		}

		public WaveTableOscillatorNode(int numSamples, float sampleFrequency) : base(numSamples, sampleFrequency)
		{
			WaveTableMemory = WaveTableRepository.SinOsc();
			Enabled = false;
			UpdateSampleFunction();
		}

		// public void ResetPhase(double startPhase = 0.0)
		// {
		// 	Phase = startPhase;
		// }
		public void ResetPhase(double startPhase = 0.0, int transitionSamples = 512)
		{
			_targetPhase = ModuloOne(startPhase);
			_isPhaseTransitioning = true;
			_phaseTransitionSpeed = Math.Abs(_targetPhase - Phase) / transitionSamples;
		}



		public void UpdateSampleFunction()
		{
			GetSampleFunction = IsPWM ? GetSamplePWM : GetSample;
		}

		protected float GetSamplePWM(WaveTable currentWaveTable, double phase)
		{
			double position = phase * currentWaveTable.WaveTableData.Length;
			double offsetPhase = Mathf.PosMod((float)(phase + (PWMDutyCycle + PWMAdd) * PWMMultiply), 1.0f);
			double offsetPosition = offsetPhase * currentWaveTable.WaveTableData.Length;

			return GetCubicInterpolatedSample(currentWaveTable, (float)position) - GetCubicInterpolatedSample(currentWaveTable, (float)offsetPosition);
		}

		protected float GetSample(WaveTable currentWaveTable, double phase)
		{
			double position = phase * currentWaveTable.WaveTableData.Length;
			return GetCubicInterpolatedSample(currentWaveTable, (float)position);
		}

		private float GetCubicInterpolatedSample(WaveTable table, float position)
		{
			int length = table.WaveTableData.Length;
			int baseIndex = (int)position;
			float frac = position - baseIndex;

			// Precompute indices with manual wrapping
			int p0 = (baseIndex - 1 + length) % length;
			int p1 = baseIndex % length;
			int p2 = (baseIndex + 1) % length;
			int p3 = (baseIndex + 2) % length;
			// Fetch samples
			float s0 = table.WaveTableData[p0];
			float s1 = table.WaveTableData[p1];
			float s2 = table.WaveTableData[p2];
			float s3 = table.WaveTableData[p3];

			// Cubic interpolation
			float a = (-0.5f * s0 + 1.5f * s1 - 1.5f * s2 + 0.5f * s3);
			float b = (s0 - 2.5f * s1 + 2.0f * s2 - 0.5f * s3);
			float c = (-0.5f * s0 + 0.5f * s2);
			float d = s1;

			return a * frac * frac * frac + b * frac * frac + c * frac + d;
		}

		public override void Process(double increment)
		{
			lock (_lock)
			{
				ResetPWMParameters();

				var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
				UpdateDetuneFactor();

				float sampleRate = SampleFrequency;
				float detunedFreq = _lastFrequency;

				for (int i = 0; i < NumSamples; i++)
				{
					UpdateParameters(i);

					detunedFreq = CalculateDetunedFrequency(detunedFreq) * pitchParam.Item2;

					if (HasFrequencyChanged(detunedFreq))
					{
						UpdateWaveTableFrequency(detunedFreq);
						_lastFrequency = detunedFreq;
						currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
					}

					// Calculate how much the phase should increment for this sample, based on frequency.
					double currentPhaseIncrement = detunedFreq * increment;

					// Handle phase transition
					if (_isPhaseTransitioning)
					{
						if (Math.Abs(Phase - _targetPhase) <= _phaseTransitionSpeed)
						{
							Phase = _targetPhase;
							_isPhaseTransitioning = false;
						}
						else
						{
							// Increment Phase towards _targetPhase
							Phase += _phaseTransitionSpeed * Math.Sign(_targetPhase - Phase);

							// Evolve _targetPhase as well, applying the same modulation effects
							double targetModulatedPhase = CalculateModulatedPhase(_targetPhase + currentPhaseIncrement);
							_targetPhase = ModuloOne(targetModulatedPhase);
						}
					}

					// Calculate the final phase to use for this sample.
					double modulatedPhase = CalculateModulatedPhase(Phase + currentPhaseIncrement);

					// Retrieve the audio sample from the wavetable using the modulated phase.
					_previousSample = GetSampleFunction(currentWaveTable, modulatedPhase) * Amplitude * Gain;

					// Store the generated sample in the output buffer.
					buffer[i] = _previousSample;

					// Evolve Phase for the next iteration/sample, using modulation.
					Phase = ModuloOne(CalculateModulatedPhase(Phase + currentPhaseIncrement));
				}

				// Store the last frequency used for the next processing cycle.
				_lastFrequency = detunedFreq;
			}
		}


		// public override void Process(double increment)
		// {
		// 	lock (_lock)
		// 	{
		// 		ResetPWMParameters();

		// 		var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
		// 		UpdateDetuneFactor();

		// 		float sampleRate = SampleFrequency;
		// 		double lastPhase = Phase;
		// 		float detunedFreq = _lastFrequency;

		// 		for (int i = 0; i < NumSamples; i++)
		// 		{
		// 			UpdateParameters(i);

		// 			detunedFreq = CalculateDetunedFrequency(detunedFreq) * pitchParam.Item2;

		// 			if (HasFrequencyChanged(detunedFreq))
		// 			{
		// 				UpdateWaveTableFrequency(detunedFreq);
		// 				_lastFrequency = detunedFreq;
		// 				currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
		// 			}

		// 			double phaseForThisSample = lastPhase + detunedFreq * increment;
		// 			double modulatedPhase = CalculateModulatedPhase(phaseForThisSample);

		// 			_previousSample = GetSampleFunction(currentWaveTable, modulatedPhase) * Amplitude * Gain;

		// 			buffer[i] = _previousSample;

		// 			lastPhase = phaseForThisSample;
		// 		}

		// 		Phase = ModuloOne(lastPhase);
		// 		_lastFrequency = detunedFreq;
		// 	}
		// }

		private void ResetPWMParameters()
		{
			PWMAdd = 0.0f;
			PWMMultiply = 1.0f;
		}

		private void UpdateParameters(int sampleIndex)
		{
			pitchParam = GetParameter(AudioParam.Pitch, sampleIndex);
			gainParam = GetParameter(AudioParam.Gain, sampleIndex);
			pmodParam = GetParameter(AudioParam.PMod, sampleIndex);
			phaseParam = GetParameter(AudioParam.Phase, sampleIndex);
			pwmParam = GetParameter(AudioParam.PWM, sampleIndex);

			PWMAdd = pwmParam.Item1;
			PWMMultiply = pwmParam.Item2;
			Gain = gainParam.Item2;
			_smoothModulationStrength = SmoothValue(_smoothModulationStrength, (ModulationStrength + pmodParam.Item1) * pmodParam.Item2, 0.01f);
		}

		private float CalculateDetunedFrequency(float currentFrequency)
		{
			return pitchParam.Item1 * _detuneFactor;
		}

		private bool HasFrequencyChanged(float newFrequency)
		{
			return Math.Abs(newFrequency - _lastFrequency) > FrequencyChangeThreshold;
		}

		private double CalculateModulatedPhase(double phaseForThisSample)
		{
			double modulatedPhase = phaseForThisSample + phaseParam.Item1 * _smoothModulationStrength * phaseParam.Item2;
			modulatedPhase += _previousSample * SelfModulationStrength;
			modulatedPhase += PhaseOffset;
			return ModuloOne(modulatedPhase);
		}

		private void UpdateWaveTableFrequency(float freq)
		{
			float topFreq = freq / SampleFrequency;
			_currentWaveTableIndex = 0;
			for (int i = 0; i < _waveTableMemory.NumWaveTables; i++)
			{
				var waveTableTopFreq = _waveTableMemory.GetWaveTable(i).TopFreq;
				if (topFreq <= waveTableTopFreq)
				{
					_currentWaveTableIndex = i;
					break;
				}
			}
		}

		private void UpdateDetuneFactor()
		{
			_detuneFactor = (float)(Math.Pow(2, DetuneCents / 1200.0f) * Math.Pow(2, DetuneSemitones / 12.0f) * Math.Pow(2, DetuneOctaves));
		}

		private float SmoothValue(float currentValue, float targetValue, float smoothingFactor)
		{
			return currentValue + smoothingFactor * (targetValue - currentValue);
		}
	}
}
