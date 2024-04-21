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
            // std::vector<double> organ_real = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            // std::vector<double> organ_imag = {0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 1};

            // std::vector<double> bass_real = {0, 1, 0.8144329896907216, 0.20618556701030927, 0.020618556701030927};
            // std::vector<double> bass_imag = {0, 0, 0, 0, 0};

            AddWave(WaveTableWaveType.SINE, WaveTableRepository.SinOsc());
            AddWave(WaveTableWaveType.TRIANGLE, WaveTableRepository.TriangleOsc());
            AddWave(WaveTableWaveType.SQUARE, WaveTableRepository.SquareOsc());
            AddWave(WaveTableWaveType.SAWTOOTH, WaveTableRepository.SawOsc());
            // addWave(WaveTableWaveType::FUZZY, periodicWaveOsc(fuzzy_real, fuzzy_imag));
            // addWave(WaveTableWaveType::ORGAN, periodicWaveOsc(organ_real, organ_imag));
            // addWave(WaveTableWaveType::ORGAN2, periodicWaveOsc(organ2_real, organ2_imag));
            // addWave(WaveTableWaveType::PIANO, periodicWaveOsc(piano_real, piano_imag));
            // addWave(WaveTableWaveType::BASS, periodicWaveOsc(bass_real, bass_imag));
            // addWave(WaveTableWaveType::VOCAL_AHH, periodicWaveOsc(ahh_real, ahh_imag));        
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