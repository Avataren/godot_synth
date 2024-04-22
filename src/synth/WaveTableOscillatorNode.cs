using System;
using System.Threading;
using Godot;

public class WaveTableOscillatorNode : AudioNode
{
	private ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

	public float _Frequency = 440.0f;
	public new float Frequency
	{
		get
		{
			lockSlim.EnterReadLock();
			try	{
				return _Frequency;
			}
			finally {
				lockSlim.ExitReadLock();
			}
		}
		set
		{
			lockSlim.EnterWriteLock();
			try	{
				_Frequency = value;
				UpdateWaveTableFrequency(value);
			}
			finally {
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
	
	private void UpdateWaveTableFrequency(float freq) {
		var mPhaseInc = (float)(freq / SampleFrequency);
		_currentWaveTable = 0;
		while ((mPhaseInc >= _WaveMem.GetWaveTable(_currentWaveTable).TopFreq) && (_currentWaveTable < (_WaveMem.NumWaveTables - 1)))
		{
			++_currentWaveTable;
		}		
	}
	
	
	int _currentWaveTable = 0;
	public WaveTableOscillatorNode(int num_samples, float sample_frequency, WaveTableMemory WaveMem) : base(num_samples)
	{
		this.WaveTableMem = WaveMem;
	}

	protected float GetSample(WaveTable currWaveTable)
	{
		
		float position = (float)(Phase * currWaveTable.WaveTableData.Length);
		int intPart = (int)position;
		float fracPart = position - intPart;
		float sample0 = currWaveTable.WaveTableData[intPart % currWaveTable.WaveTableData.Length];  // Ensure wrap around within array bounds
		float sample1 = currWaveTable.WaveTableData[(intPart + 1) % currWaveTable.WaveTableData.Length];  // Same as above
		return sample0 + (sample1 - sample0) * fracPart;
	}

	public override AudioNode Process(float increment)
	{
		var freq = Frequency;
		var currWaveTable = WaveTableMem.GetWaveTable(_currentWaveTable);
		for (int i = 0; i < NumSamples; i++)
		{
			buffer[i] = GetSample(currWaveTable) * Amplitude;
			Phase += increment * freq;
			Phase = Mathf.PosMod(Phase, 1.0f);
		}
		return this;
	}
}
