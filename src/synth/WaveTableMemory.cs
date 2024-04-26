
using System;
using System.Collections.Generic;

public struct WaveTable
{
	public double TopFreq;
	public int WaveTableLen;
	public float[] WaveTableData;
};

public class WaveTableMemory
{
	const int MaxWaveTableSlots = 40;  // simplify allocation with reasonable maximum
	WaveTable[] WaveTables = new WaveTable[MaxWaveTableSlots];
	public int NumWaveTables = 0;  // number of wavetable slots in use

	public WaveTable GetWaveTable(int index)
	{
		return WaveTables[index];
	}
	public WaveTableMemory()
	{
		for (int idx = 0; idx < MaxWaveTableSlots; idx++)
		{
			WaveTables[idx] = new WaveTable { TopFreq = 0, WaveTableLen = 0, WaveTableData = Array.Empty<float>() };
		}
	}

	//
	// AddWaveTable
	//
	// add wavetables in order of lowest frequency to highest
	// topFreq is the highest frequency supported by a wavetable
	// wavetables within an oscillator can be different lengths
	//
	// returns 0 upon success, or the number of wavetables if no more room is available
	//
	public int AddWaveTable(int len, float[] waveTableIn, double topFreq)
	{
		if (NumWaveTables < MaxWaveTableSlots)
		{
			WaveTables[NumWaveTables].WaveTableLen = len + 1;
			WaveTables[NumWaveTables].WaveTableData = new float[len + 1];
			// fill in wave
			Array.Copy(waveTableIn, WaveTables[NumWaveTables].WaveTableData, len);
			WaveTables[NumWaveTables].TopFreq = topFreq;
			// duplicate for interpolation wraparound
			WaveTables[NumWaveTables].WaveTableData[len] = waveTableIn[0];
			NumWaveTables++;
			return NumWaveTables;
		}
		return 0;
	}
};
