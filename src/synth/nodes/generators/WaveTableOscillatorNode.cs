using System;
using Godot;

namespace Synth
{
	public class WaveTableOscillatorNode : AudioNode
	{
		private readonly object _lock = new object();
		private WaveTableMemory _waveTableMemory;
		private int _currentWaveTableIndex;
		private SynthType _lastFrequency = -1f;
		private SynthType _smoothModulationStrength;
		private SynthType _detuneFactor;
		private SynthType _previousSample;

		private const SynthType TwoPi = (SynthType)(Math.PI * 2.0);
		private const SynthType MinPWMDutyCycle = 0.001f;
		private const SynthType MaxPWMDutyCycle = 0.999f;
		private const SynthType FrequencyChangeThreshold = 0.000001f;

		public SynthType DetuneCents { get; set; } = 0.0f;
		public SynthType DetuneSemitones { get; set; } = 0.0f;
		public SynthType DetuneOctaves { get; set; } = 0.0f;
		public SynthType ModulationStrength { get; set; } = 0.0f;
		public SynthType SelfModulationStrength { get; set; } = 0.0f;
		public SynthType PhaseOffset { get; set; } = 0.0f;
		public bool IsPWM { get; set; } = false;
		public SynthType Gain { get; set; } = 1.0f;

		private SynthType _pwmDutyCycle = 0.5f;
		public SynthType PWMDutyCycle
		{
			get => _pwmDutyCycle;
			set => _pwmDutyCycle = Math.Clamp(value, MinPWMDutyCycle, MaxPWMDutyCycle);
		}

		private SynthType PWMAdd { get; set; } = SynthTypeHelper.Zero;
		private SynthType PWMMultiply { get; set; } = SynthTypeHelper.One;

		public delegate SynthType WaveTableFunction(WaveTable waveTable, SynthType phase);
		public WaveTableFunction GetSampleFunction { get; private set; }

		public WaveTableMemory WaveTableMemory
		{
			get => _waveTableMemory;
			set
			{
				lock (_lock)
				{
					_waveTableMemory = value;
					//UpdateWaveTableFrequency(440.0f);
					InvalidateWaveform();
				}
			}
		}

		public WaveTableOscillatorNode() : base()
		{
			_scheduler.RegisterNode(this, [AudioParam.Gate]);
			WaveTableMemory = WaveTableRepository.SinOsc();
			//Enabled = false;
			UpdateSampleFunction();
		}

		public void ResetPhase(SynthType startPhase = (SynthType)0.0)
		{
			Phase = startPhase;
		}

		private SynthType ExponentialInterpolation(SynthType current, SynthType target, SynthType alpha)
		{
			return current + (target - current) * (1 - SynthType.Exp(-alpha));
		}

		public void UpdateSampleFunction()
		{
			GetSampleFunction = GetSamplePWM;// IsPWM ? GetSamplePWM : GetSample;
		}

		protected SynthType GetSamplePWM(WaveTable currentWaveTable, SynthType phase)
		{
			int length = currentWaveTable.WaveTableData.Length - 1;
			SynthType adjustedPhase;
			if (phase < PWMDutyCycle)
			{
				// Compress the first half
				adjustedPhase = phase / PWMDutyCycle * SynthTypeHelper.Half;
			}
			else
			{
				// Expand the second half
				adjustedPhase = SynthTypeHelper.Half + (phase - PWMDutyCycle) / (SynthTypeHelper.One - PWMDutyCycle) * SynthTypeHelper.Half;
			}
			// Convert the adjusted phase to the wavetable index
			//double phaseIndex = adjustedPhase * length;
			// Retrieve the sample using linear interpolation
			return GetCubicInterpolatedSample(currentWaveTable, (float)adjustedPhase);
			//return GetSampleLinear(currentWaveTable, (float)adjustedPhase);
		}

		private SynthType GetSampleLinear(WaveTable currentWaveTable, SynthType phase)
		{
			int length = currentWaveTable.WaveTableData.Length;
			double position = phase * length;
			int index = (int)position;
			SynthType frac = (SynthType)(position - index);
			int nextIndex = (index + 1) % length;

			return currentWaveTable.WaveTableData[index] + frac * (currentWaveTable.WaveTableData[nextIndex] - currentWaveTable.WaveTableData[index]);
		}

		protected SynthType GetSample(WaveTable currentWaveTable, SynthType phase)
		{
			SynthType position = phase * currentWaveTable.WaveTableData.Length;
			return GetCubicInterpolatedSample(currentWaveTable, (SynthType)position);
		}


		private SynthType GetCubicInterpolatedSample(WaveTable table, SynthType position)
		{
			int length = table.WaveTableData.Length;

			// Wrap the position around the length of the table to ensure it's within bounds
			position *= length - 1.0f;

			int baseIndex = (int)position;
			SynthType frac = position - baseIndex;

			// Ensure the indices wrap around correctly
			int i0 = (baseIndex - 1 + length) % length;
			int i1 = baseIndex;
			int i2 = (baseIndex + 1) % length;
			int i3 = (baseIndex + 2) % length;

			// Retrieve the sample values from the wavetable
			SynthType sample0 = table.WaveTableData[i0];
			SynthType sample1 = table.WaveTableData[i1];
			SynthType sample2 = table.WaveTableData[i2];
			SynthType sample3 = table.WaveTableData[i3];

			// Cubic interpolation formula
			SynthType a = sample3 - sample2 - sample0 + sample1;
			SynthType b = sample0 - sample1 - a;
			SynthType c = sample2 - sample0;
			SynthType d = sample1;

			return a * frac * frac * frac + b * frac * frac + c * frac + d;
		}
		bool _isGateOpen = false;
		int gateNum = 0;

		public void InvalidateWaveform()
		{
			_lastFrequency = -1f;
		}

		private int _crossfadeCounter = 0;
		private int _crossfadeFrames = 256; // Adjust this based on desired crossfade duration
		private SynthType _previousPhase = SynthTypeHelper.Zero;
		private SynthType _newPhase = SynthTypeHelper.Zero;

		public override void Process(double increment)
		{
			var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
			UpdateDetuneFactor();
			SynthType phase = Phase;
			SynthType phaseIncrement = _lastFrequency * (SynthType)increment;
			SynthType freqLastSample = _lastFrequency;

			for (int i = 0; i < NumSamples; i++)
			{
				UpdateParameters(i);
				if (freqLastSample != _lastFrequency)
				{
					phaseIncrement = _lastFrequency * (SynthType)increment;
					UpdateWaveTableFrequency(_lastFrequency);
					currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);
				}
				freqLastSample = _lastFrequency;
				SynthType gateValue = (SynthType)_scheduler.GetValueAtSample(this, AudioParam.Gate, i);

				if (!_isGateOpen && gateValue > 0.5)
				{
					gateNum++;
					_isGateOpen = true;
					_previousSample = SynthTypeHelper.Zero;

					if (HardSync)
					{
						// Start crossfade when hard sync occurs
						_crossfadeCounter = _crossfadeFrames;
						_previousPhase = phase;
						_newPhase = SynthTypeHelper.Zero; // Reset new phase
					}
				}
				else if (_isGateOpen && gateValue < SynthTypeHelper.Half)
				{
					_isGateOpen = false;
				}

				// During crossfade, blend old and new phases
				if (_crossfadeCounter > 0)
				{
					SynthType fadeAmount = SynthTypeHelper.One - (SynthType)_crossfadeCounter / _crossfadeFrames;
					_crossfadeCounter--;

					// Continue evolving both phases
					_previousPhase += phaseIncrement;  // Continue the previous phase
					_newPhase += phaseIncrement;       // Start from the new phase

					SynthType modulatedOldPhase = CalculateModulatedPhase(_previousPhase, PhaseOffset, _previousSample, SelfModulationStrength);
					SynthType modulatedNewPhase = CalculateModulatedPhase(_newPhase, PhaseOffset, _previousSample, SelfModulationStrength);

					SynthType oldSample = GetSamplePWM(currentWaveTable, modulatedOldPhase);
					SynthType newSample = GetSamplePWM(currentWaveTable, modulatedNewPhase);

					// Blend the old and new samples based on fadeAmount
					buffer[i] = ((SynthTypeHelper.One - fadeAmount) * oldSample + fadeAmount * newSample) * Amplitude * Gain;

					// After the crossfade, continue with the new phase
					if (_crossfadeCounter == 0)
					{
						phase = _newPhase;  // Set phase to the new phase after crossfade
					}
				}
				else
				{
					// After crossfade, continue with the new phase
					SynthType modulatedPhase = CalculateModulatedPhase(phase, PhaseOffset, _previousSample, SelfModulationStrength);
					SynthType currentSample = GetSamplePWM(currentWaveTable, modulatedPhase);
					buffer[i] = currentSample * Amplitude * Gain;
					_previousSample = currentSample;
					phase += phaseIncrement;
				}
			}

			Phase = SynthTypeHelper.ModuloOne(phase);
		}
		private void UpdateParameters(int sampleIndex)
		{
			var pitchParam = GetParameter(AudioParam.Pitch, sampleIndex);
			var gainParam = GetParameter(AudioParam.Gain, sampleIndex);
			var pmodParam = GetParameter(AudioParam.PMod, sampleIndex, 1.0f); // remember to fix add / mul type here when implemented in the UI
			var phaseParam = GetParameter(AudioParam.Phase, sampleIndex);
			var pwmParam = GetParameter(AudioParam.PWM, sampleIndex);

			PWMAdd = pwmParam.Item1;
			PWMMultiply = pwmParam.Item2;
			Gain = gainParam.Item2;
			SynthType phase_modulation = phaseParam.Item1;
			_smoothModulationStrength = phase_modulation * ModulationStrength * pmodParam.Item1;
			_lastFrequency = pitchParam.Item1 * pitchParam.Item2 * _detuneFactor;
		}

		private SynthType CalculateModulatedPhase(SynthType basePhase, SynthType phaseOffset, SynthType previousSample, SynthType selfModulationStrength)
		{
			var offset = phaseOffset + _smoothModulationStrength + previousSample * selfModulationStrength;
			return SynthTypeHelper.ModuloOne(basePhase + offset);
			//(basePhase + offset + 100.0) % 1.0;
		}

		private void UpdateWaveTableFrequency(SynthType freq)
		{
			SynthType topFreq = freq / SampleRate;
			_currentWaveTableIndex = 0;

			for (int i = 0; i < _waveTableMemory.NumWaveTables; i++)
			{
				var waveTableTopFreq = _waveTableMemory.GetWaveTable(i).TopFreq;
				if (topFreq <= waveTableTopFreq)
				{
					_currentWaveTableIndex = i;
					break;
				}
			}
		}

		public void ScheduleGateOpen(double time, bool forceCloseFirst = false)
		{
			if (forceCloseFirst)
			{
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 0.0, time);
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 1.0, time + 4 / SampleRate);
			}
			else
			{
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 1.0, time);
			}
		}

		public void ScheduleGateClose(double time)
		{
			_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 0.0, time);
		}

		private void UpdateDetuneFactor()
		{
			_detuneFactor = (float)(Math.Pow(2, DetuneCents / 1200.0f) * Math.Pow(2, DetuneSemitones / 12.0f) * Math.Pow(2, DetuneOctaves));
		}

		private float SmoothValue(float currentValue, float targetValue, float smoothingFactor)
		{
			return currentValue + smoothingFactor * (targetValue - currentValue);
		}
	}
}
