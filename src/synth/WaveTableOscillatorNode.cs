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

		public WaveTableOscillatorNode() : base()
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

		protected float GetSamplePWM(WaveTable currentWaveTable, double phase)
		{
			int length = currentWaveTable.WaveTableData.Length - 1;
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
			//double phaseIndex = adjustedPhase * length;
			// Retrieve the sample using linear interpolation
			return GetCubicInterpolatedSample(currentWaveTable, (float)adjustedPhase);
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
			position *= length - 1.0f;

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
			double phase = Phase;

			double phaseIncrement = _lastFrequency * increment;
			double modulatedPhase;

			for (int i = 0; i < NumSamples; i++)
			{
				UpdateParameters(i);

				if (HasFrequencyChanged(_lastFrequency))
				{
					UpdateWaveTableFrequency(_lastFrequency);
					currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
				}

				modulatedPhase = CalculateModulatedPhase(phase, PhaseOffset, _previousSample, SelfModulationStrength);
				//_previousSample = GetLinearlyInterpolatedSample(currentWaveTable, (float)(modulatedPhase * (currentWaveTable.WaveTableData.Length - 1)));
				_previousSample = GetSamplePWM(currentWaveTable, modulatedPhase);
				buffer[i] = _previousSample * Amplitude * Gain;

				phase += phaseIncrement;
			}

			Phase = ModuloOne(phase);
		}

		private void UpdateParameters(int sampleIndex)
		{
			var pitchParam = GetParameter(AudioParam.Pitch, sampleIndex);
			var gainParam = GetParameter(AudioParam.Gain, sampleIndex);
			var pmodParam = GetParameter(AudioParam.PMod, sampleIndex, 1.0f); // remember to fix add / mul type here when implemented in the UI
			var phaseParam = GetParameter(AudioParam.Phase, sampleIndex);
			var pwmParam = GetParameter(AudioParam.PWM, sampleIndex);

			PWMAdd = pwmParam.Item1;
			PWMMultiply = pwmParam.Item2;
			Gain = gainParam.Item2;
			float phase_modulation = phaseParam.Item1;
			_smoothModulationStrength = phase_modulation * ModulationStrength * pmodParam.Item1;
			_lastFrequency = pitchParam.Item1 * _detuneFactor * pitchParam.Item2;
		}

		private double CalculateModulatedPhase(double basePhase, double phaseOffset, float previousSample, float selfModulationStrength)
		{
			var offset = phaseOffset + _smoothModulationStrength + previousSample * selfModulationStrength;
			return (basePhase + offset + 100.0) % 1.0;
		}

		private bool HasFrequencyChanged(float newFrequency)
		{
			return Math.Abs(newFrequency - _lastFrequency) > FrequencyChangeThreshold;
		}

		private void UpdateWaveTableFrequency(float freq)
		{
			float topFreq = freq / SampleRate;
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
