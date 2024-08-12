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
		public bool Is_PWM { get; set; } = false;
		private float _lastFrequency = -1; // Initialize to an invalid frequency to ensure initial update
		public delegate float WaveTableFunction(WaveTable waveTable, float phase);
		public WaveTableFunction GetSampleFunc;



		private float _PWMDutyCycle = 0.5f; // 50% duty cycle by default
		public float PWMDutyCycle
		{
			get => _PWMDutyCycle;
			set => _PWMDutyCycle = Math.Clamp(value, 0.001f, 0.999f);
		}

		public void UpdateSampleFunction()
		{
			GetSampleFunc = Is_PWM ? (WaveTableFunction)GetSample_PWM : GetSample;
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
			float topFreq = freq / SampleFrequency;// * 2.0f;
			_currentWaveTable = 0;
			for (int i = 0; i < _WaveMem.NumWaveTables; i++)
			{
				var waveTableTopFreq = _WaveMem.GetWaveTable(i).TopFreq;
				if (topFreq <= waveTableTopFreq)
				{
					_currentWaveTable = i;
					//GD.Print("Current wave table: " + i);
					break;
				}
			}
		}

		int _currentWaveTable = 0;
		public WaveTableOscillatorNode(int num_samples, float sample_frequency) : base(num_samples, sample_frequency)
		{
			this.WaveTableMem = WaveTableRepository.SinOsc();
			this.Enabled = false;
			UpdateSampleFunction();
		}

		protected float GetSample_GenericPWM(WaveTable currWaveTable)
		{
			float length = currWaveTable.WaveTableData.Length;
			float position = Phase * length;
			float fracPart = position % 1;

			// Linear interpolation
			float sample0 = currWaveTable.WaveTableData[(int)position % (int)length];
			float sample1 = currWaveTable.WaveTableData[(int)(position + 1) % (int)length];

			return (sample0 + (sample1 - sample0) * fracPart);
		}

		protected float GetSample_PWM(WaveTable currWaveTable, float phase)
		{
			float position = phase * currWaveTable.WaveTableData.Length;
			float offsetPhase = Mathf.PosMod(phase + PWMDutyCycle, 1.0f);
			float offsetPosition = offsetPhase * currWaveTable.WaveTableData.Length;

			return GetInterpolatedSample(currWaveTable, position) - GetInterpolatedSample(currWaveTable, offsetPosition);
		}

		protected float GetSample(WaveTable currWaveTable, float phase)
		{
			float position = phase * currWaveTable.WaveTableData.Length;
			return GetInterpolatedSample(currWaveTable, position);
		}

		// private float GetInterpolatedSample(WaveTable table, float position)
		// {
		// 	int intPart = (int)position;
		// 	float fracPart = position - intPart;
		// 	float sample0 = table.WaveTableData[intPart % table.WaveTableData.Length];
		// 	float sample1 = table.WaveTableData[(intPart + 1) % table.WaveTableData.Length];
		// 	return sample0 + (sample1 - sample0) * fracPart;
		// }

		private float GetInterpolatedSample(WaveTable table, float position)
		{
			int length = table.WaveTableData.Length;
			int baseIndex = (int)position;
			float frac = position - baseIndex;

			// Calculate indices for cubic interpolation
			int p0 = (baseIndex - 1 + length) % length;
			int p1 = baseIndex % length;
			int p2 = (baseIndex + 1) % length;
			int p3 = (baseIndex + 2) % length;

			// Retrieve samples
			float s0 = table.WaveTableData[p0];
			float s1 = table.WaveTableData[p1];
			float s2 = table.WaveTableData[p2];
			float s3 = table.WaveTableData[p3];

			// Cubic interpolation formula
			float a = (-0.5f * s0 + 1.5f * s1 - 1.5f * s2 + 0.5f * s3);
			float b = (s0 - 2.5f * s1 + 2.0f * s2 - 0.5f * s3);
			float c = (-0.5f * s0 + 0.5f * s2);
			float d = s1;

			return a * frac * frac * frac + b * frac * frac + c * frac + d;
		}

		// float prevSample = 0.0f;
		// public override void Process(float increment)
		// {
		// 	var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
		// 	UpdateDetuneFactor();
		// 	float sampleRate = SampleFrequency;  // Assuming SampleFrequency is defined elsewhere in your class

		// 	float initialPhase = Phase;  // Store the initial phase
		// 	float currentFreq = 0.0f;  // Declare this outside the loop
		// 	for (int i = 0; i < NumSamples; i++)
		// 	{
		// 		currentFreq = GetParameter(AudioParam.Frequency, i) * DetuneFactor;  // Update frequency each iteration
		// 		var gain = GetParameter(AudioParam.Gain, i, 1.0f);
		// 		if (currentFreq != _lastFrequency)
		// 		{
		// 			UpdateWaveTableFrequency(currentFreq);
		// 			_lastFrequency = currentFreq;
		// 			currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
		// 		}
		// 		float phaseForThisSample = initialPhase + (i * currentFreq / sampleRate);

		// 		// Apply modulation and calculate buffer output
		// 		float modulatedPhase = phaseForThisSample + GetParameter(AudioParam.Phase, i) * ModulationStrength;
		// 		modulatedPhase += prevSample * SelfModulationStrength;
		// 		modulatedPhase = Mathf.PosMod(modulatedPhase, 1.0f);
		// 		prevSample = GetSampleFunc(currWaveTable, modulatedPhase)  * Amplitude * gain;
		// 		buffer[i] = prevSample;
		// 	}

		// 	// Update the global phase after processing all samples to start correctly for the next call
		// 	Phase = (initialPhase + ((float)NumSamples * currentFreq / sampleRate)) % 1.0f;
		// }

		float prevSample = 0.0f;

		public override void Process(float increment)
		{
			var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
			UpdateDetuneFactor();
			float sampleRate = SampleFrequency;
			float initialPhase = Phase;
			float lastPhase = initialPhase;
			float lastFreq = _lastFrequency; // Store the last frequency
			for (int i = 0; i < NumSamples; i++)
			{
				float detunedFreq = GetParameter(AudioParam.Frequency, i) * DetuneFactor;
				float gain = GetParameter(AudioParam.Gain, i, 1.0f);

				// Only update the wave table and frequency if it has changed
				if (detunedFreq != lastFreq)
				{
					UpdateWaveTableFrequency(detunedFreq);
					lastFreq = detunedFreq;
					currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
				}

				float phaseForThisSample = lastPhase + (detunedFreq / sampleRate);

				// Apply modulation and calculate buffer output
				float modulatedPhase = phaseForThisSample + GetParameter(AudioParam.Phase, i) * ModulationStrength;
				modulatedPhase += prevSample * SelfModulationStrength;
				modulatedPhase = Mathf.PosMod(modulatedPhase, 1.0f);
				prevSample = GetSampleFunc(currWaveTable, modulatedPhase) * Amplitude * gain;
				buffer[i] = prevSample;

				lastPhase = phaseForThisSample; // Update lastPhase for next iteration
			}

			// Update the global phase after processing all samples
			Phase = lastPhase % 1.0f;
			_lastFrequency = lastFreq; // Store the last frequency globally
		}


		private void UpdateDetuneFactor()
		{
			DetuneFactor = (float)Math.Pow(2, DetuneCents / 1200.0f) * (float)Math.Pow(2, DetuneSemitones / 12.0f) * (float)Math.Pow(2, DetuneOctaves);
		}

	}
}
