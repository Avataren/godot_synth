using System;
using System.Threading;
using Godot;

public class WaveTableOscillatorNode : AudioNode
{
	private ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();
	public float _Frequency = 440.0f;
	public float DetuneCents = 0.0f;
	public float DetuneSemitones = 0.0f;
	public float DetuneOctaves = 0.0f;

	private float _PWMDutyCycle = 0.5f; // 50% duty cycle by default
	public float PWMDutyCycle
	{
		get => _PWMDutyCycle;
		set
		{
			if (value <= 0.0f) value = 0.0001f;
			if (value >= 1.0f) value = 0.9999f;
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
	public WaveTableOscillatorNode(int num_samples, float sample_frequency, WaveTableMemory WaveMem) : base(num_samples, sample_frequency)
	{
		this.WaveTableMem = WaveMem;
		this.Enabled = false;
		// this.HardSync = false;
	}

	// protected float GetSample(WaveTable currWaveTable)
	// {
	// 	float position;
	// 	float length = currWaveTable.WaveTableData.Length;

	// 	if (Phase < PWMDutyCycle)
	// 	{
	// 		// For the "on" phase
	// 		position = Phase / PWMDutyCycle * length / 2;
	// 	}
	// 	else
	// 	{
	// 		// For the "off" phase
	// 		position = ((Phase - PWMDutyCycle) / (1 - PWMDutyCycle) * length / 2) + length / 2;
	// 	}

	// 	int intPart = (int)position % (int)length;
	// 	float fracPart = position - intPart;

	// 	// Linear interpolation
	// 	float sample0 = currWaveTable.WaveTableData[intPart];
	// 	float sample1 = currWaveTable.WaveTableData[(intPart + 1) % (int)length];

	// 	return sample0 + (sample1 - sample0) * fracPart;
	// }

	// protected float GetSample(WaveTable currWaveTable)
	// {
	// 	float position;
	// 	float length = currWaveTable.WaveTableData.Length;

	// 	if (Phase < PWMDutyCycle)
	// 	{
	// 		// For the "on" phase
	// 		position = Phase / PWMDutyCycle * length / 2;
	// 	}
	// 	else
	// 	{
	// 		// For the "off" phase
	// 		position = ((Phase - PWMDutyCycle) / (1 - PWMDutyCycle) * length / 2) + length / 2;
	// 	}

	// 	int intPart = (int)position % (int)length;
	// 	float fracPart = position - intPart;

	// 	// Linear interpolation
	// 	float sample0 = currWaveTable.WaveTableData[intPart];
	// 	float sample1 = currWaveTable.WaveTableData[(intPart + 1) % (int)length];
	// 	float interpolatedSample = sample0 + (sample1 - sample0) * fracPart;

	// 	// Adjust the scaling factor based on the PWM duty cycle
	// 	float scalingFactor = 1.0f / (2 * MathF.Max(PWMDutyCycle, 1 - PWMDutyCycle));
	// 	return interpolatedSample * scalingFactor;
	// }

	protected float GetSample(WaveTable currWaveTable)
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

	// protected float GetSample(WaveTable currWaveTable)
	// {
	// 	float length = currWaveTable.WaveTableData.Length;

	// 	// Original phase
	// 	float originalPhasePos = Phase * length;
	// 	int originalIntPart = (int)originalPhasePos;
	// 	float originalFracPart = originalPhasePos - originalIntPart;

	// 	// Compute the sample for the original phase
	// 	float sample0 = currWaveTable.WaveTableData[originalIntPart % (int)length];
	// 	float sample1 = currWaveTable.WaveTableData[(originalIntPart + 1) % (int)length];
	// 	float originalSample = sample0 + (sample1 - sample0) * originalFracPart;

	// 	// Offset phase using PWM
	// 	float offsetPhase = Phase + (1 - 2 * PWMDutyCycle); // Adjusting around the midpoint
	// 	offsetPhase = Mathf.PosMod(offsetPhase, 1.0f);

	// 	float offsetPhasePos = offsetPhase * length;
	// 	int offsetIntPart = (int)offsetPhasePos;
	// 	float offsetFracPart = offsetPhasePos - offsetIntPart;

	// 	// Compute the sample for the offset phase
	// 	sample0 = currWaveTable.WaveTableData[offsetIntPart % (int)length];
	// 	sample1 = currWaveTable.WaveTableData[(offsetIntPart + 1) % (int)length];
	// 	float offsetSample = sample0 + (sample1 - sample0) * offsetFracPart;

	// 	// Combine original and offset samples for PWM effect
	// 	return originalSample - offsetSample;
	// }


	// protected float GetSample(WaveTable currWaveTable)
	// {
	// 	float position;

	// 	// PWM applied, adjust position based on duty cycle
	// 	position = (Phase < PWMDutyCycle)
	// 		? Phase / PWMDutyCycle * currWaveTable.WaveTableData.Length
	// 		: (Phase - PWMDutyCycle) / (1 - PWMDutyCycle) * currWaveTable.WaveTableData.Length;

	// 	int intPart = (int)position;
	// 	float fracPart = position - intPart;

	// 	// Linear interpolation
	// 	float sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];
	// 	float sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];

	// 	return sample0 + (sample1 - sample0) * fracPart;
	// }

	// protected float GetSample(WaveTable currWaveTable)
	// {
	// 	float position;

	// 	// PWM applied, adjust position based on duty cycle
	// 	if (Phase < PWMDutyCycle)
	// 	{
	// 		position = (Phase / PWMDutyCycle) * currWaveTable.WaveTableData.Length;
	// 	}
	// 	else
	// 	{
	// 		position = ((Phase - PWMDutyCycle) / (1 - PWMDutyCycle)) * currWaveTable.WaveTableData.Length;
	// 		position += currWaveTable.WaveTableData.Length / 2; // Offset to the second half
	// 	}

	// 	int intPart = (int)position;
	// 	float fracPart = position - intPart;

	// 	// Linear interpolation
	// 	float sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];
	// 	float sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];

	// 	return sample0 + (sample1 - sample0) * fracPart;
	// }


	public override AudioNode Process(float increment, LFONode FrequencyLFO)
	{
		var freq = Frequency;
		var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
		for (int i = 0; i < NumSamples; i++)
		{
			buffer[i] = GetSample(currWaveTable) * Amplitude;
			Phase += increment * (freq + FrequencyLFO[i]);
			Phase = Mathf.PosMod(Phase, 1.0f);
		}
		return this;
	}

}
