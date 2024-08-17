using System;
using System.Threading;
using Godot;

namespace Synth
{
	public class WaveTableOscillatorNode : AudioNode
	{
		private ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();
		public float DetuneCents = 0.0f;
		public float DetuneSemitones = 0.0f;
		public float DetuneOctaves = 0.0f;
		public float ModulationStrength = 0.0f;
		public float SelfModulationStrength = 0.0f;
		private float DetuneFactor = 0.0f;
		public float PhaseOffset = 0.0f;
		public bool Is_PWM { get; set; } = false;
		private float _lastFrequency = -1; // Initialize to an invalid frequency to ensure initial update
		float PWM_Add = 0.0f;
		float PWM_Mul = 1.0f;

		public delegate float WaveTableFunction(WaveTable waveTable, float phase);
		public WaveTableFunction GetSampleFunc;

		private float _PWMDutyCycle = 0.5f; // 50% duty cycle by default
		public float PWMDutyCycle
		{
			get => _PWMDutyCycle;
			set => _PWMDutyCycle = Math.Clamp(value, 0.001f, 0.999f);
		}

		private WaveTableMemory _WaveMem;
		public WaveTableMemory WaveTableMem
		{
			get => _WaveMem;
			set
			{
				WaveTableMemory oldMem;
				do
				{
					oldMem = _WaveMem;
				}
				while (Interlocked.CompareExchange(ref _WaveMem, value, oldMem) != oldMem);
				UpdateWaveTableFrequency(440.0f);
			}
		}

		private void UpdateWaveTableFrequency(float freq)
		{
			float topFreq = freq / SampleFrequency;
			_currentWaveTable = 0;
			for (int i = 0; i < _WaveMem.NumWaveTables; i++)
			{
				var waveTableTopFreq = _WaveMem.GetWaveTable(i).TopFreq;
				if (topFreq <= waveTableTopFreq)
				{
					_currentWaveTable = i;
					break;
				}
			}
		}

		int _currentWaveTable = 0;
		private double _phaseDouble = 0.0; // Use double for higher precision
		private const double TwoPi = Math.PI * 2.0;
		private ulong _sampleCounter = 0;

		public WaveTableOscillatorNode(int num_samples, float sample_frequency) : base(num_samples, sample_frequency)
		{
			this.WaveTableMem = WaveTableRepository.SinOsc();
			this.Enabled = false;
			UpdateSampleFunction();
		}

		public void ResetPhase(double startPhase = 0.0)
		{
			GD.Print("Resetting phase for " + Name);
			_phaseDouble = startPhase;
			Phase = (float)startPhase;
		}

		public void UpdateSampleFunction()
		{
			GetSampleFunc = Is_PWM ? (WaveTableFunction)GetSample_PWM : GetSample;
		}

		protected float GetSample_GenericPWM(WaveTable currWaveTable)
		{
			float length = currWaveTable.WaveTableData.Length;
			float position = Phase * length;
			float fracPart = position % 1;

			float sample0 = currWaveTable.WaveTableData[(int)position % (int)length];
			float sample1 = currWaveTable.WaveTableData[(int)(position + 1) % (int)length];

			return sample0 + (sample1 - sample0) * fracPart;
		}

		protected float GetSample_PWM(WaveTable currWaveTable, float phase)
		{
			float position = phase * currWaveTable.WaveTableData.Length;
			float offsetPhase = Mathf.PosMod(phase + (PWMDutyCycle + PWM_Add) * PWM_Mul, 1.0f);
			float offsetPosition = offsetPhase * currWaveTable.WaveTableData.Length;

			return GetCubicInterpolatedSample(currWaveTable, position) - GetCubicInterpolatedSample(currWaveTable, offsetPosition);
		}

		protected float GetSample(WaveTable currWaveTable, float phase)
		{
			float position = phase * currWaveTable.WaveTableData.Length;
			return GetCubicInterpolatedSample(currWaveTable, position);
		}

		private float GetLinearInterpolatedSample(WaveTable table, float position)
		{
			int intPart = (int)position;
			float fracPart = position - intPart;
			float sample0 = table.WaveTableData[intPart % table.WaveTableData.Length];
			float sample1 = table.WaveTableData[(intPart + 1) % table.WaveTableData.Length];
			return sample0 + (sample1 - sample0) * fracPart;
		}

		private float GetCubicInterpolatedSample(WaveTable table, float position)
		{
			int length = table.WaveTableData.Length;
			int baseIndex = (int)position;
			float frac = position - baseIndex;

			int p0 = (baseIndex - 1 + length) % length;
			int p1 = baseIndex % length;
			int p2 = (baseIndex + 1) % length;
			int p3 = (baseIndex + 2) % length;

			float s0 = table.WaveTableData[p0];
			float s1 = table.WaveTableData[p1];
			float s2 = table.WaveTableData[p2];
			float s3 = table.WaveTableData[p3];

			float a = (-0.5f * s0 + 1.5f * s1 - 1.5f * s2 + 0.5f * s3);
			float b = (s0 - 2.5f * s1 + 2.0f * s2 - 0.5f * s3);
			float c = (-0.5f * s0 + 0.5f * s2);
			float d = s1;

			return a * frac * frac * frac + b * frac * frac + c * frac + d;
		}

		float prevSample = 0.0f;

		private float _smoothModulationStrength = 0.0f;
		private float _modulationSmoothingFactor = 0.01f;

		private float SmoothValue(float currentValue, float targetValue, float smoothingFactor)
		{
			return currentValue + smoothingFactor * (targetValue - currentValue);
		}

		public override void Process(float increment)
		{
			PWM_Add = 0.0f;
			PWM_Mul = 1.0f;
			var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
			UpdateDetuneFactor();
			float sampleRate = SampleFrequency;
			double initialPhase = _phaseDouble;
			double lastPhase = initialPhase;
			float lastFreq = _lastFrequency; // Store the last frequency

			for (int i = 0; i < NumSamples; i++)
			{
				var pitchParam = GetParameter(AudioParam.Pitch, i);
				var gainParam = GetParameter(AudioParam.Gain, i);
				var pmodParam = GetParameter(AudioParam.PMod, i);
				var phaseParam = GetParameter(AudioParam.Phase, i);
				var pwmParam = GetParameter(AudioParam.PWM, i);
				PWM_Add = pwmParam.Item1;
				PWM_Mul = pwmParam.Item2;

				float detunedFreq = pitchParam.Item1 * DetuneFactor * pitchParam.Item2;
				float gain = gainParam.Item2;

				if (Math.Abs(detunedFreq - lastFreq) > 1e-6)
				{
					UpdateWaveTableFrequency(detunedFreq);
					lastFreq = detunedFreq;
					currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
				}

				double phaseForThisSample = lastPhase + (detunedFreq / sampleRate);
				_smoothModulationStrength = SmoothValue(_smoothModulationStrength, (ModulationStrength + pmodParam.Item1) * pmodParam.Item2, _modulationSmoothingFactor);

				double modulatedPhase = phaseForThisSample + phaseParam.Item1 * _smoothModulationStrength * phaseParam.Item2;
				modulatedPhase += prevSample * SelfModulationStrength;
				modulatedPhase += PhaseOffset;
				modulatedPhase = Mathf.PosMod((float)modulatedPhase, 1.0f);

				var shapedPhase = (float)modulatedPhase;
				prevSample = GetSampleFunc(currWaveTable, shapedPhase) * Amplitude * gain;

				buffer[i] = prevSample;

				lastPhase = phaseForThisSample; // Update lastPhase for next iteration
			}

			// Update the global phase after processing all samples
			_phaseDouble = lastPhase % 1.0;
			Phase = (float)_phaseDouble;
			_lastFrequency = lastFreq; // Store the last frequency globally
		}

		private void UpdateDetuneFactor()
		{
			DetuneFactor = (float)Math.Pow(2, DetuneCents / 1200.0f) * (float)Math.Pow(2, DetuneSemitones / 12.0f) * (float)Math.Pow(2, DetuneOctaves);
		}
	}
}
