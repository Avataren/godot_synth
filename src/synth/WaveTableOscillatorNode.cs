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
		private double _phaseAccumulator;
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

		public void ResetPhase(double startPhase = 0.0)
		{
			_phaseAccumulator = startPhase;
			Phase = startPhase;
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

			// Ensure baseIndex is within the correct bounds
			if (baseIndex < 0)
				baseIndex += length;
			else if (baseIndex >= length)
				baseIndex -= length;

			// Precompute indices with manual wrapping
			int p0 = baseIndex - 1;
			if (p0 < 0) p0 += length;

			int p1 = baseIndex;

			int p2 = baseIndex + 1;
			if (p2 >= length) p2 -= length;

			int p3 = baseIndex + 2;
			if (p3 >= length) p3 -= length;

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
				PWMAdd = 0.0f;
				PWMMultiply = 1.0f;
				var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
				UpdateDetuneFactor();
				float sampleRate = SampleFrequency;
				double initialPhase = _phaseAccumulator;
				double lastPhase = initialPhase;
				float detunedFreq = _lastFrequency;

				for (int i = 0; i < NumSamples; i++)
				{
					var pitchParam = GetParameter(AudioParam.Pitch, i);
					var gainParam = GetParameter(AudioParam.Gain, i);
					var pmodParam = GetParameter(AudioParam.PMod, i);
					var phaseParam = GetParameter(AudioParam.Phase, i);
					var pwmParam = GetParameter(AudioParam.PWM, i);
					PWMAdd = pwmParam.Item1;
					PWMMultiply = pwmParam.Item2;

					detunedFreq = pitchParam.Item1 * _detuneFactor * pitchParam.Item2;
					float gain = gainParam.Item2;

					if (Math.Abs(detunedFreq - _lastFrequency) > 1e-6)
					{
						UpdateWaveTableFrequency(detunedFreq);
						_lastFrequency = detunedFreq;
						currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
					}

					double phaseForThisSample = lastPhase + (detunedFreq / sampleRate);
					_smoothModulationStrength = SmoothValue(_smoothModulationStrength, (ModulationStrength + pmodParam.Item1) * pmodParam.Item2, 0.01f);

					double modulatedPhase = phaseForThisSample + phaseParam.Item1 * _smoothModulationStrength * phaseParam.Item2;
					modulatedPhase += _previousSample * SelfModulationStrength;
					modulatedPhase += PhaseOffset;
					modulatedPhase = modulatedPhase % 1.0;

					var shapedPhase = (float)modulatedPhase;
					_previousSample = GetSampleFunction(currentWaveTable, modulatedPhase) * Amplitude * gain;

					buffer[i] = _previousSample;

					lastPhase = phaseForThisSample;
				}

				_phaseAccumulator = lastPhase % 1.0;
				Phase = _phaseAccumulator;
				_lastFrequency = detunedFreq;
			}
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
