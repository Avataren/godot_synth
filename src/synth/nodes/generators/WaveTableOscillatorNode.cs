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
                    InvalidateWaveform();
                }
            }
        }

        public WaveTableOscillatorNode() : base()
        {
            _scheduler.RegisterNode(this, new System.Collections.Generic.List<AudioParam> { AudioParam.Gate, AudioParam.Pitch, AudioParam.Gain, AudioParam.PMod, AudioParam.Phase, AudioParam.PWM });
            WaveTableMemory = WaveTableRepository.SinOsc();
            UpdateSampleFunction();
        }

        public void ResetPhase(SynthType startPhase = (SynthType)0.0)
        {
            Phase = startPhase;
        }

        public void UpdateSampleFunction()
        {
            GetSampleFunction = IsPWM ? GetSamplePWM : GetSample;
        }

        public void InvalidateWaveform()
        {
            _lastFrequency = -1f; // Force wavetable update on next process
        }

        private bool _isGateOpen = false;

        public override void Process(double increment)
        {
            SynthType phase = Phase;
            UpdateDetuneFactor();

            SynthType previousFrequency = _lastFrequency;

            for (int i = 0; i < NumSamples; i++)
            {
                // Update parameters for the current sample
                UpdateParameters(i);

                // Calculate phase increment based on updated frequency
                SynthType phaseIncrement = _lastFrequency * (SynthType)increment;

                // Check if frequency has changed significantly and update wavetable if necessary
                if (Math.Abs(_lastFrequency - previousFrequency) > FrequencyChangeThreshold)
                {
                    UpdateWaveTableFrequency(_lastFrequency);
                    previousFrequency = _lastFrequency;
                }

                // Get the current wavetable
                var currentWaveTable = WaveTableMemory.GetWaveTable(_currentWaveTableIndex);

                SynthType gateValue = (SynthType)_scheduler.GetValueAtSample(this, AudioParam.Gate, i);

                // Handle gate changes
                if (!_isGateOpen && gateValue > 0.5f)
                {
                    _isGateOpen = true;
                    if (HardSync)
                    {
                        phase = SynthTypeHelper.Zero;
                    }
                }
                else if (_isGateOpen && gateValue < 0.5f)
                {
                    _isGateOpen = false;
                }

                // Calculate modulated phase
                SynthType modulatedPhase = CalculateModulatedPhase(phase);

                // Get sample and apply gain
                SynthType currentSample = GetSampleFunction(currentWaveTable, modulatedPhase);
                buffer[i] = currentSample * Amplitude * Gain;

                // Update previous sample for self-modulation
                _previousSample = currentSample;

                // Increment phase
                phase = SynthTypeHelper.ModuloOne(phase + phaseIncrement);
            }

            Phase = phase;
        }

        private SynthType CalculateModulatedPhase(SynthType basePhase)
        {
            // Calculate modulation contributions
            SynthType phaseModulation = _smoothModulationStrength;
            SynthType selfModulation = _previousSample * SelfModulationStrength;

            // Sum all modulations and wrap the phase
            SynthType modulatedPhase = SynthTypeHelper.ModuloOne(basePhase + PhaseOffset + phaseModulation + selfModulation);

            return modulatedPhase;
        }

        private void UpdateParameters(int sampleIndex)
        {
            // Get parameter values at the current sample index
            var pitchParam = GetParameter(AudioParam.Pitch, sampleIndex);
            var gainParam = GetParameter(AudioParam.Gain, sampleIndex);
            var pmodParam = GetParameter(AudioParam.PMod, sampleIndex, 1.0f);
            var phaseParam = GetParameter(AudioParam.Phase, sampleIndex);
            var pwmParam = GetParameter(AudioParam.PWM, sampleIndex);

            // Update PWM parameters
            PWMAdd = pwmParam.Item1;
            PWMMultiply = pwmParam.Item2;

            // Update gain
            Gain = gainParam.Item2;

            // Update phase modulation strength
            _smoothModulationStrength = phaseParam.Item1 * ModulationStrength * pmodParam.Item1;

            // Update frequency with detune factor
            _lastFrequency = pitchParam.Item1 * pitchParam.Item2 * _detuneFactor;
        }

        private void UpdateDetuneFactor()
        {
            _detuneFactor = (SynthType)(Math.Pow(2, DetuneCents / 1200.0f) * Math.Pow(2, DetuneSemitones / 12.0f) * Math.Pow(2, DetuneOctaves));
        }

        private void UpdateWaveTableFrequency(SynthType freq)
        {
            SynthType topFreq = freq / SampleRate;
            _currentWaveTableIndex = 0;

            for (int i = 0; i < WaveTableMemory.NumWaveTables; i++)
            {
                var waveTableTopFreq = WaveTableMemory.GetWaveTable(i).TopFreq;
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
                _scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 1.0, time + 1.0 / SampleRate);
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

        // Sample generation methods
        private SynthType GetSample(WaveTable currentWaveTable, SynthType phase)
        {
            SynthType position = phase * currentWaveTable.WaveTableData.Length;
            return GetCubicInterpolatedSample(currentWaveTable, position);
        }

        private SynthType GetSamplePWM(WaveTable currentWaveTable, SynthType phase)
        {
            SynthType adjustedPhase;

            if (phase < PWMDutyCycle)
            {
                // Compress the first part
                adjustedPhase = phase / PWMDutyCycle * 0.5f;
            }
            else
            {
                // Expand the second part
                adjustedPhase = 0.5f + (phase - PWMDutyCycle) / (1.0f - PWMDutyCycle) * 0.5f;
            }

            // Scale adjustedPhase to the wavetable length
            SynthType position = adjustedPhase * currentWaveTable.WaveTableData.Length;

            // Retrieve the sample using cubic interpolation
            return GetCubicInterpolatedSample(currentWaveTable, position);
        }

        private SynthType GetCubicInterpolatedSample(WaveTable table, SynthType position)
        {
            int length = table.WaveTableData.Length;

            // Wrap the position within the table length
            position = position % length;
            if (position < 0) position += length;

            int baseIndex = (int)Math.Floor(position);
            SynthType frac = position - baseIndex;

            // Ensure the indices wrap around correctly
            int i0 = (baseIndex - 1 + length) % length;
            int i1 = baseIndex % length;
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
    }
}
