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
		// public void ResetPhase(double startPhase = 0.0, int transitionSamples = 64)
		// {
		// 	_targetPhase = ModuloOne(startPhase);
		// 	_isPhaseTransitioning = true;
		// 	_phaseTransitionSpeed = Math.Abs(_targetPhase - Phase) / transitionSamples;
		// }

		// public void ResetPhase(double startPhase = 0.0, int transitionSamples = 64)
		// {
		// 	lock (_lock)
		// 	{
		// 		// If a transition is already ongoing, we need to calculate the remaining transition to avoid drift
		// 		if (_isPhaseTransitioning)
		// 		{
		// 			// Calculate the remaining phase difference from the current phase to the target phase
		// 			double remainingPhaseDifference = ModuloOne(_targetPhase - Phase);

		// 			// Adjust the new target phase based on the remaining difference
		// 			_targetPhase = ModuloOne(startPhase + remainingPhaseDifference);
		// 		}
		// 		else
		// 		{
		// 			// Set the new target phase directly
		// 			_targetPhase = ModuloOne(startPhase);
		// 		}

		// 		// Calculate the phase transition speed
		// 		_phaseTransitionSpeed = Math.Abs(_targetPhase - Phase) / transitionSamples;

		// 		// Enable the phase transitioning flag
		// 		_isPhaseTransitioning = true;
		// 	}
		// }

		public void ResetPhase(double startPhase = 0.0, int crossfadeSamples = 64)
		{
			//lock (_lock)
			{
				// // Set the target phase to the start phase modulo 1 to ensure it stays within the [0, 1) range.
				// _targetPhase = ModuloOne(startPhase);

				// // Store the number of samples over which to perform the crossfade.
				// _phaseTransitionSpeed = 1.0 / crossfadeSamples;

				// // Set the flag indicating that a phase transition is happening.
				// _isPhaseTransitioning = true;
				Phase = startPhase;
			}
		}


		private double ExponentialInterpolation(double current, double target, double alpha)
		{
			return current + (target - current) * (1 - Math.Exp(-alpha));
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
			//lock (_lock)
			{
				//GD.Print(Name + " => Pase: " + Phase);

				ResetPWMParameters();

				var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
				UpdateDetuneFactor();

				float sampleRate = SampleFrequency;
				float detunedFreq = _lastFrequency;

				// Calculate the phase increment for one sample
				double phaseIncrement = detunedFreq * increment;

				// Save the initial phase
				double initialPhase = Phase;

				for (int i = 0; i < NumSamples; i++)
				{
					UpdateParameters(i);

					detunedFreq = CalculateDetunedFrequency(detunedFreq) * pitchParam.Item2;

					if (HasFrequencyChanged(detunedFreq))
					{
						UpdateWaveTableFrequency(detunedFreq);
						_lastFrequency = detunedFreq;
						currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);

						// Recalculate phase increment if the frequency changes
						phaseIncrement = detunedFreq * increment;
					}

					// Calculate the absolute base phase for this sample
					double basePhase = initialPhase + i * phaseIncrement;

					// Apply the phase offset
					double phaseWithOffset = ModuloOne(basePhase + PhaseOffset);

					// Apply modulation to the phase (assuming modulation is additive)
					double modulatedPhase = ModuloOne(CalculateModulatedPhase(phaseWithOffset));

					// Retrieve the sample from the wavetable using the modulated phase
					_previousSample = GetSampleFunction(currentWaveTable, modulatedPhase);

					// Store the generated sample in the output buffer
					buffer[i] = _previousSample * Amplitude * Gain;
				}

				// After processing all samples, update the Phase based on the accumulated phase increment
				Phase = ModuloOne(initialPhase + NumSamples * phaseIncrement);

				_lastFrequency = detunedFreq;
			}
		}


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
