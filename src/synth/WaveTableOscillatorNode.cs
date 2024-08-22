using System;
using Godot;

namespace Synth
{
	public class WaveTableOscillatorNode : AudioNode
	{
		private readonly object _lock = new object();
		private WaveTableMemory _waveTableMemory;
		private int _currentWaveTableIndex;
		private float _lastFrequency = -1f;
		private float _smoothModulationStrength;
		private float _detuneFactor;
		private float _previousSample;

		private const double TwoPi = Math.PI * 2.0;
		private const float MinPWMDutyCycle = 0.001f;
		private const float MaxPWMDutyCycle = 0.999f;
		private const float FrequencyChangeThreshold = 0.000001f;

		public float DetuneCents { get; set; } = 0.0f;
		public float DetuneSemitones { get; set; } = 0.0f;
		public float DetuneOctaves { get; set; } = 0.0f;
		public float ModulationStrength { get; set; } = 0.0f;
		public float SelfModulationStrength { get; set; } = 0.0f;
		public float PhaseOffset { get; set; } = 0.0f;
		public bool IsPWM { get; set; } = false;
		public float Gain { get; set; } = 1.0f;

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

		public void ResetPhase(double startPhase = 0.0, int crossfadeSamples = 64)
		{
			Phase = startPhase;
		}

		private double ExponentialInterpolation(double current, double target, double alpha)
		{
			return current + (target - current) * (1 - Math.Exp(-alpha));
		}

		public void UpdateSampleFunction()
		{
			GetSampleFunction = GetSamplePWM;// IsPWM ? GetSamplePWM : GetSample;
		}


		// protected float GetSamplePWM(WaveTable currentWaveTable, double phase)
		// {
		// 	double position = phase * currentWaveTable.WaveTableData.Length;
		// 	double offsetPhase = Mathf.PosMod((float)(phase + (PWMDutyCycle + PWMAdd) * PWMMultiply), 1.0f);
		// 	double offsetPosition = offsetPhase * currentWaveTable.WaveTableData.Length;

		// 	return GetCubicInterpolatedSample(currentWaveTable, (float)position) - GetCubicInterpolatedSample(currentWaveTable, (float)offsetPosition);
		// }

		protected float GetSamplePWM(WaveTable currentWaveTable, double phase)
		{
			int length = currentWaveTable.WaveTableData.Length;

			// Ensure the PWM duty cycle is within valid bounds
			PWMDutyCycle = Math.Clamp(PWMDutyCycle, 0f, 1f);

			double adjustedPhase;

			if (phase < PWMDutyCycle)
			{
				// Compress the first half
				adjustedPhase = phase / PWMDutyCycle * 0.5;
			}
			else
			{
				// Expand the second half
				adjustedPhase = 0.5 + (phase - PWMDutyCycle) / (1.0 - PWMDutyCycle) * 0.5;
			}

			// Convert the adjusted phase to the wavetable index
			double phaseIndex = adjustedPhase * length;

			// Retrieve the sample using linear interpolation
			return GetCubicInterpolatedSample(currentWaveTable, (float)phaseIndex);
		}

		protected float GetSample(WaveTable currentWaveTable, double phase)
		{
			double position = phase * currentWaveTable.WaveTableData.Length;
			return GetCubicInterpolatedSample(currentWaveTable, (float)position);
		}

		private float GetLinearlyInterpolatedSample(WaveTable currentWaveTable, float phaseIndex)
		{
			int length = currentWaveTable.WaveTableData.Length;

			// Get the integer part and fractional part of the phase index
			int index = (int)phaseIndex;
			float frac = phaseIndex - index;

			// Get the indices of the current and next sample
			int nextIndex = (index + 1) % length;

			// Retrieve the sample values from the wavetable
			float sample1 = currentWaveTable.WaveTableData[index];
			float sample2 = currentWaveTable.WaveTableData[nextIndex];

			// Linear interpolation
			return sample1 + frac * (sample2 - sample1);
		}

		private float GetCubicInterpolatedSample(WaveTable table, float position)
		{
			int length = table.WaveTableData.Length;

			// Wrap the position around the length of the table to ensure it's within bounds
			position = Mathf.PosMod(position, length);

			int baseIndex = (int)position;
			float frac = position - baseIndex;

			// Ensure the indices wrap around correctly
			int i0 = (baseIndex - 1 + length) % length;
			int i1 = baseIndex;
			int i2 = (baseIndex + 1) % length;
			int i3 = (baseIndex + 2) % length;

			// Retrieve the sample values from the wavetable
			float sample0 = table.WaveTableData[i0];
			float sample1 = table.WaveTableData[i1];
			float sample2 = table.WaveTableData[i2];
			float sample3 = table.WaveTableData[i3];

			// Cubic interpolation formula
			float a = sample3 - sample2 - sample0 + sample1;
			float b = sample0 - sample1 - a;
			float c = sample2 - sample0;
			float d = sample1;

			return a * frac * frac * frac + b * frac * frac + c * frac + d;
		}



		public override void Process(double increment)
		{

			var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
			UpdateDetuneFactor();

			float detunedFreq = _lastFrequency;
			double phaseIncrement = detunedFreq * increment;
			double initialPhase = Phase;

			Tuple<float, float> pitchParam;
			Tuple<float, float> gainParam;
			Tuple<float, float> pmodParam;
			Tuple<float, float> phaseParam;
			Tuple<float, float> pwmParam;

			for (int i = 0; i < NumSamples; i++)
			{
				// Update parameters
				pitchParam = GetParameter(AudioParam.Pitch, i);
				gainParam = GetParameter(AudioParam.Gain, i);
				pmodParam = GetParameter(AudioParam.PMod, i);
				phaseParam = GetParameter(AudioParam.Phase, i);
				pwmParam = GetParameter(AudioParam.PWM, i);

				// Update PWM parameters
				PWMAdd = pwmParam.Item1;
				PWMMultiply = pwmParam.Item2;

				// Apply gain
				Gain = gainParam.Item2;

				// Smooth modulation strength
				_smoothModulationStrength = SmoothValue(_smoothModulationStrength, (ModulationStrength + pmodParam.Item1) * pmodParam.Item2, 0.01f);

				// Calculate the detuned frequency inline
				detunedFreq = pitchParam.Item1 * _detuneFactor * pitchParam.Item2;

				if (HasFrequencyChanged(detunedFreq))
				{
					UpdateWaveTableFrequency(detunedFreq);
					_lastFrequency = detunedFreq;
					currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
					phaseIncrement = detunedFreq * increment;
				}

				double basePhase = initialPhase + i * phaseIncrement;
				double phaseWithOffset = ModuloOne(basePhase + PhaseOffset);

				// Apply phase modulation
				double modulatedPhase = ModuloOne(phaseWithOffset + phaseParam.Item1 * _smoothModulationStrength * phaseParam.Item2 + _previousSample * SelfModulationStrength);

				_previousSample = GetSampleFunction(currentWaveTable, modulatedPhase);
				buffer[i] = _previousSample * Amplitude * Gain;
			}

			Phase = ModuloOne(initialPhase + NumSamples * phaseIncrement);
			_lastFrequency = detunedFreq;
		}

		private bool HasFrequencyChanged(float newFrequency)
		{
			return Math.Abs(newFrequency - _lastFrequency) > FrequencyChangeThreshold;
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
