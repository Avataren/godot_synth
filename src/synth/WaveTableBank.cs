using System;
using System.Collections.Generic;

namespace Synth
{
    public enum WaveTableWaveType
    {
        SINE,
        TRIANGLE,
        SQUARE,
        SAWTOOTH,
        FUZZY,
        ORGAN,
        ORGAN2,
        PIANO,
        BASS,
        VOCAL_AHH,
        CUSTOM,
        _WavetableWaveCount
    };

    public class WaveTableBank
    {
        Dictionary<WaveTableWaveType, WaveTableMemory> m_waves = new Dictionary<WaveTableWaveType, WaveTableMemory>();

        public WaveTableBank()
        {
            double[] OrganReal = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            double[] OrganImag = {0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 1};

            double[] BassReal = {0, 1, 0.8144329896907216, 0.20618556701030927, 0.020618556701030927};
            double[] BassImag = {0, 0, 0, 0, 0};

            AddWave(WaveTableWaveType.SINE, WaveTableRepository.SinOsc());
            AddWave(WaveTableWaveType.TRIANGLE, WaveTableRepository.TriangleOsc());
            AddWave(WaveTableWaveType.SQUARE, WaveTableRepository.SquareOsc());
            AddWave(WaveTableWaveType.SAWTOOTH, WaveTableRepository.SawOsc());
            // addWave(WaveTableWaveType::FUZZY, periodicWaveOsc(fuzzy_real, fuzzy_imag));
            AddWave(WaveTableWaveType.ORGAN, WaveTableRepository.PeriodicWaveOsc(OrganReal, OrganImag));
            // addWave(WaveTableWaveType::ORGAN2, periodicWaveOsc(organ2_real, organ2_imag));
            // addWave(WaveTableWaveType::PIANO, periodicWaveOsc(piano_real, piano_imag));
            AddWave(WaveTableWaveType.BASS, WaveTableRepository.PeriodicWaveOsc(BassReal, BassImag));
            AddWave(WaveTableWaveType.VOCAL_AHH, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.AhhReal, PeriodicWaves.AhhImag));        
        }


        public void AddWave(WaveTableWaveType type, WaveTableMemory waveMem)
        {
            if (m_waves.ContainsKey(type))
            {
                throw new Exception("wave memory by that name already exists");
            }

            m_waves[type] = waveMem;
        }

        public WaveTableMemory GetWave(WaveTableWaveType type)
        {
            if (m_waves.ContainsKey(type) == false)
            {
                throw new Exception("wave memory by that name does not exist");
            }
            var wave = m_waves[type];
            return wave;
        }
    };
}