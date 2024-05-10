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
		PWM,
		CUSTOM,
		_WavetableWaveCount
	};

	public class WaveTableBank
	{
		Dictionary<WaveTableWaveType, WaveTableMemory> m_waves = new Dictionary<WaveTableWaveType, WaveTableMemory>();

		public WaveTableBank()
		{
			AddWave(WaveTableWaveType.SINE, WaveTableRepository.SinOsc());
			AddWave(WaveTableWaveType.TRIANGLE, WaveTableRepository.TriangleOsc());
			AddWave(WaveTableWaveType.SQUARE, WaveTableRepository.SquareOsc());
			AddWave(WaveTableWaveType.SAWTOOTH, WaveTableRepository.SawOsc());
			// addWave(WaveTableWaveType::FUZZY, periodicWaveOsc(fuzzy_real, fuzzy_imag));
			AddWave(WaveTableWaveType.ORGAN, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.OrganReal, PeriodicWaves.OrganImag));
			AddWave(WaveTableWaveType.ORGAN2, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.Organ2Real, PeriodicWaves.Organ2Imag));
			// addWave(WaveTableWaveType::PIANO, periodicWaveOsc(piano_real, piano_imag));
			AddWave(WaveTableWaveType.BASS, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.BassReal, PeriodicWaves.BassImag));
			AddWave(WaveTableWaveType.VOCAL_AHH, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.AhhReal, PeriodicWaves.AhhImag));
			AddWave(WaveTableWaveType.FUZZY, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.FuzzyReal, PeriodicWaves.FuzzyImag));
			AddWave(WaveTableWaveType.PIANO, WaveTableRepository.PeriodicWaveOsc(PeriodicWaves.PianoReal, PeriodicWaves.PianoImag));
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
