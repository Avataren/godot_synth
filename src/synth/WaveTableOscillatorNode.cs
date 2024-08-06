using System;
using System.Threading;
using Godot;

namespace Synth
{
	public class WaveTableOscillatorNode : AudioNode
	{
		private ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();
		public float _Frequency = 440.0f;
		public float DetuneCents = 0.0f;
		public float DetuneSemitones = 0.0f;
		public float DetuneOctaves = 0.0f;
		public bool Is_PWM { get; set; } = false;

		public delegate float WaveTableFunction(WaveTable waveTable);
		public WaveTableFunction GetSampleFunc;

		private float _PWMDutyCycle = 0.5f; // 50% duty cycle by default
		public float PWMDutyCycle
		{
			get => _PWMDutyCycle;
			set
			{
				if (value <= 0.0f) value = 0.001f;
				if (value >= 1.0f) value = 0.999f;
				_PWMDutyCycle = value;
			}
		}


		public new float Frequency
		{
			get
			{
				lockSlim.EnterReadLock();
				try
				{
					return _Frequency;
				}
				finally
				{
					lockSlim.ExitReadLock();
				}
			}
			set
			{
				lockSlim.EnterWriteLock();
				try
				{
					float detuneFactor =
						(float)Math.Pow(2, DetuneCents / 1200.0f) *
						(float)Math.Pow(2, DetuneSemitones / 12.0f) *
						(float)Math.Pow(2, DetuneOctaves);

					_Frequency = value * detuneFactor;
					UpdateWaveTableFrequency(_Frequency);
				}
				finally
				{
					lockSlim.ExitWriteLock();
				}
			}
		}

		public void UpdateSampleFunction()
		{
			GetSampleFunc = Is_PWM ? (WaveTableFunction)GetSample_PWM : GetSample;
		}

		private volatile WaveTableMemory _WaveMem;
		public WaveTableMemory WaveTableMem
		{
			get
			{
				return _WaveMem;
			}
			set
			{
				WaveTableMemory oldMem;
				WaveTableMemory newMem = value;  // Assume new value is a completely new immutable object
				do
				{
					oldMem = _WaveMem;
				}
				while (Interlocked.CompareExchange(ref _WaveMem, newMem, oldMem) != oldMem);
				UpdateWaveTableFrequency(_Frequency);
			}
		}

		private void UpdateWaveTableFrequency(float freq)
		{
			float topFreq = freq / SampleFrequency * 3.0f;
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
			GD.Print("Current Wave Table: ", _currentWaveTable, " out of ", _WaveMem.NumWaveTables, " with note topFreq ", topFreq, " and top frequency ", _WaveMem.GetWaveTable(_currentWaveTable).TopFreq);
		}


		int _currentWaveTable = 0;
		public WaveTableOscillatorNode(ModulationManager ModulationMgr, int num_samples, float sample_frequency, WaveTableMemory WaveMem) : base(ModulationMgr, num_samples, sample_frequency)
		{
			this.WaveTableMem = WaveMem;
			this.Enabled = false;
			// this.HardSync = false;
			UpdateSampleFunction();
		}

		protected float GetSample_GenericPWM(WaveTable currWaveTable)
		{
			float position;
			float length = currWaveTable.WaveTableData.Length;
			float normalizationFactor;

			if (Phase < PWMDutyCycle)
			{
				// For the "on" phase
				position = Phase / PWMDutyCycle * length / 2;
				normalizationFactor = 1.0f / PWMDutyCycle;
			}
			else
			{
				// For the "off" phase
				position = ((Phase - PWMDutyCycle) / (1 - PWMDutyCycle) * length / 2) + length / 2;
				normalizationFactor = 1.0f / (1 - PWMDutyCycle);
			}

			int intPart = (int)position % (int)length;
			float fracPart = position - intPart;

			// Linear interpolation
			float sample0 = currWaveTable.WaveTableData[intPart];
			float sample1 = currWaveTable.WaveTableData[(intPart + 1) % (int)length];

			return (sample0 + (sample1 - sample0) * fracPart) * normalizationFactor * 0.5f;
		}

		//saw subtraction
		protected float GetSample_PWM(WaveTable currWaveTable)
		{
			float position = Phase * currWaveTable.WaveTableData.Length;
			int intPart = (int)position;
			float fracPart = position - intPart;

			// Linear interpolation for the original phase
			float sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];
			float sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];
			float sample = sample0 + (sample1 - sample0) * fracPart;

			// Offset phase calculation for PWM
			float offsetPhase = Phase + PWMDutyCycle;
			if (offsetPhase > 1.0f)
			{
				offsetPhase -= 1.0f;
			}

			position = offsetPhase * currWaveTable.WaveTableData.Length;
			intPart = (int)position;
			fracPart = position - intPart;

			// Linear interpolation for the offset phase
			sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];
			sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];
			float offsetSample = sample0 + (sample1 - sample0) * fracPart;

			// Return the difference between the original and offset samples
			return sample - offsetSample;
		}

		protected float GetSample(WaveTable currWaveTable)
		{
			float position = Phase * currWaveTable.WaveTableData.Length;
			int intPart = (int)position;
			float fracPart = position - intPart;

			// Linear interpolation for the original phase
			float sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];
			float sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];
			return sample0 + (sample1 - sample0) * fracPart;
		}

		public override void Process(float increment)
		{
			var freq = Frequency;
			var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
			//var FrequencyLFO = LFO_Manager.GetRoutedLFO(LFOName.Frequency);
			for (int i = 0; i < NumSamples; i++)
			{
				buffer[i] = GetSampleFunc(currWaveTable) * Amplitude;
				Phase += increment * (freq + GetParameter(AudioParam.Frequency, i));
				Phase = Mathf.PosMod(Phase, 1.0f);
			}
		}

	}
}