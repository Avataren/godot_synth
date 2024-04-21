using System;
using Godot;

public class WaveTableOscillatorNode : AudioNode
{

    public float _Frequency = 440.0f;
    public new float Frequency
    {
        get
        {
            return _Frequency;
        }
        set
        {
            _Frequency = value;
            var mPhaseInc = (float)(value / SampleFrequency);
            _currentWaveTable = 0;
            while ((mPhaseInc >= WaveMem.GetWaveTable(_currentWaveTable).TopFreq) && (_currentWaveTable < (WaveMem.NumWaveTables - 1)))
            {
                ++_currentWaveTable;
            }
        }
    }
    
    public WaveTableMemory WaveMem;
    int _currentWaveTable = 0;
    public WaveTableOscillatorNode(int num_samples, float sample_frequency, WaveTableMemory WaveMem) : base(num_samples)
    {
        this.WaveMem = WaveMem;
    }

    protected float GetSample()
    {
        var waveTable = WaveMem.GetWaveTable(_currentWaveTable);
        float position = (float)(Phase * waveTable.WaveTableData.Length);
        int intPart = (int)position;
        float fracPart = position - intPart;
        float sample0 = waveTable.WaveTableData[intPart % waveTable.WaveTableData.Length];  // Ensure wrap around within array bounds
        float sample1 = waveTable.WaveTableData[(intPart + 1) % waveTable.WaveTableData.Length];  // Same as above
        return sample0 + (sample1 - sample0) * fracPart;
    }

    public override AudioNode Process(float increment)
    {
        for (int i = 0; i < NumSamples; i++)
        {
            buffer[i] = GetSample() * Amplitude;
            Phase += increment * Frequency;
            Phase = Mathf.PosMod(Phase, 1.0f);
        }
        return this;
    }
}
