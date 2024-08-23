using System;
using Godot;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        // Constants for default values
        private const double DefaultDecayTime = 0.0;
        private const double DefaultSmoothingFactor = 0.01;
        private const double DefaultTransitionEndTime = 0.005;
        private const double MinimumReleaseTime = 0.0045;
        private const double MinimumAttackTime = 0.005;
        private const double DefaultSustainLevel = 1.0;
        private const double DefaultAttackTime = MinimumAttackTime;
        private const double DefaultReleaseTime = MinimumReleaseTime;
        // Private fields
        private double _envelopePosition = 0.0;
        private double _releaseStartPosition = 0.0;
        private bool _isGateOpen = false;
        private double _attackTime = DefaultAttackTime;
        private double _decayTime = DefaultDecayTime;
        private double _sustainLevel = DefaultSustainLevel;
        private double _releaseTime = DefaultReleaseTime;
        private double _smoothingFactor = DefaultSmoothingFactor;
        private double _transitionEndTime = DefaultTransitionEndTime;

        private double _currentAmplitude = 0.0;
        private double _releaseStartAmplitude = 0.0;

        private double _attackCtrl = 2.0;
        private double _decayCtrl = -3.0;
        private double _releaseCtrl = -3.5;
        private double _timeScale = 1.0;
        private double _attackStartAmplitude = 0.0;
        public double TimeScale
        {
            get => _timeScale;
            set => _timeScale = Math.Max(value, 0.1);
        }

        private double _expBaseAttack, _expBaseDecay, _expBaseRelease;

        // Properties with appropriate getters and setters
        public double AttackTime
        {
            get => _attackTime * TimeScale;
            set => _attackTime = Math.Max(value, MinimumAttackTime);
        }

        public double DecayTime
        {
            get => _decayTime * TimeScale;
            set => _decayTime = Math.Max(value, 0);
        }

        public double SustainLevel
        {
            get => _sustainLevel;
            set => _sustainLevel = Math.Clamp(value, 0.0, 1.0);
        }

        public double ReleaseTime
        {
            get => _releaseTime * TimeScale;
            set => _releaseTime = Math.Max(value, MinimumReleaseTime);
        }

        public double SmoothingFactor
        {
            get => _smoothingFactor;
            set => _smoothingFactor = Math.Max(value, DefaultSmoothingFactor);
        }

        public double TransitionEndTime
        {
            get => _transitionEndTime;
            set => _transitionEndTime = Math.Max(value, DefaultTransitionEndTime);
        }

        public double AttackCtrl
        {
            get => _attackCtrl;
            set
            {
                _attackCtrl = value;
                _expBaseAttack = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double DecayCtrl
        {
            get => _decayCtrl;
            set
            {
                _decayCtrl = value;
                _expBaseDecay = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double ReleaseCtrl
        {
            get => _releaseCtrl;
            set
            {
                _releaseCtrl = value;
                _expBaseRelease = Math.Pow(2.0, value) - 1.0;
            }
        }

        public EnvelopeNode() : base()
        {
            _scheduler.RegisterNode(this, [AudioParam.Gate]);
            UpdateExponentialCurves();
        }

        private void UpdateExponentialCurves()
        {
            _expBaseAttack = Math.Pow(2.0, _attackCtrl) - 1.0;
            _expBaseDecay = Math.Pow(2.0, _decayCtrl) - 1.0;
            _expBaseRelease = Math.Pow(2.0, _releaseCtrl) - 1.0;
        }

        int gateNum = 0;
        public override void OpenGate()
        {
            gateNum++;
            _isGateOpen = true;
            _envelopePosition = 0.0;
            _attackStartAmplitude = _currentAmplitude;  // Capture the current amplitude
        }
        public override void CloseGate()
        {
            _releaseStartPosition = _envelopePosition;
            _releaseStartAmplitude = _currentAmplitude;
            _isGateOpen = false;
        }

        private double ExponentialCurve(double x, double c, double precomputedBase)
        {
            return (Math.Pow(2, c * x) - 1) / precomputedBase;
        }

        private double CalculateTargetAmplitude(double position)
        {
            if (_isGateOpen)
            {
                if (position < AttackTime)
                {
                    // Attack phase: Move from _attackStartAmplitude to 1.0 over the attack duration.
                    double normalizedPosition = position / AttackTime;
                    double targetAmplitude = _attackStartAmplitude +
                        (ExponentialCurve(normalizedPosition, AttackCtrl, _expBaseAttack) * (1.0 - _attackStartAmplitude));
                    return targetAmplitude;
                }
                else if (position < AttackTime + DecayTime)
                {
                    // Decay phase: Move from 1.0 to the sustain level over the decay duration.
                    double decayPosition = (position - AttackTime) / DecayTime;
                    return 1.0 - ExponentialCurve(decayPosition, DecayCtrl, _expBaseDecay) * (1.0 - SustainLevel);
                }
                else
                {
                    // Sustain phase: Maintain the sustain level.
                    return SustainLevel;
                }
            }
            else
            {
                // Release phase: Move from the current amplitude to 0.0 over the release duration.
                double releasePosition = (position - _releaseStartPosition) / ReleaseTime;
                if (releasePosition < 1.0)
                {
                    return _releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, ReleaseCtrl, _expBaseRelease));
                }
                return 0.0;
            }
        }

        private double GetEnvelopeValue(double position)
        {
            double targetAmplitude = CalculateTargetAmplitude(position);
            return _currentAmplitude += (targetAmplitude - _currentAmplitude) * _smoothingFactor;
        }

        public override void Process(double increment)
        {
            float[] bufferRef = buffer; // Cache the buffer reference

            for (int i = 0; i < NumSamples; i++)
            {
                double gateValue = _scheduler.GetValueAtSample(this, AudioParam.Gate, i);

                if (!_isGateOpen && gateValue > 0.5)
                {
                    OpenGate();
                    GD.Print("Gate open ENV sampleNum: " + i + " gateNum: " + gateNum);
                }
                else if (_isGateOpen && gateValue <= 0.5)
                {
                    CloseGate();
                }

                _currentAmplitude = GetEnvelopeValue(_envelopePosition);

                bufferRef[i] = (float)_currentAmplitude;
                _envelopePosition += increment;
            }
        }

        public double GetEnvelopeBufferPosition(double visualizationDuration = 3.0)
        {
            double nonSustainDuration = _attackTime + _decayTime + _releaseTime;
            double sustainDuration = Math.Max(0.0, visualizationDuration - nonSustainDuration);
            double totalDuration = nonSustainDuration + sustainDuration;

            double currentPosition = _envelopePosition;

            if (!_isGateOpen)
            {
                currentPosition = _releaseStartPosition + (currentPosition - _releaseStartPosition) * (_releaseTime / (totalDuration - _releaseStartPosition));
            }

            return Math.Clamp(currentPosition / totalDuration, 0.0, 1.0);
        }

        public float[] GetVisualBuffer(int numSamples, double visualizationDuration = 3.0)
        {
            float[] visualBuffer = new float[numSamples];

            // Calculate phase durations in terms of sample indices
            double sampleRate = numSamples / visualizationDuration;
            int attackSamples = (int)(_attackTime * sampleRate);
            int decaySamples = (int)(_decayTime * sampleRate);
            int releaseSamples = (int)(_releaseTime * sampleRate);

            // Sustain phase: Fill remaining samples after attack and decay, minus release
            int sustainSamples = numSamples - (attackSamples + decaySamples + releaseSamples);
            sustainSamples = Math.Max(sustainSamples, 0); // Ensure positive or zero sample counts

            double releaseStartAmplitude = 0.0;
            bool hasReleaseStarted = false; // Flag to detect the start of the release phase

            for (int i = 0; i < numSamples; i++)
            {
                double targetAmplitude;
                double normalizedTime;

                if (i < attackSamples)
                {
                    normalizedTime = (double)i / attackSamples;
                    targetAmplitude = ExponentialCurve(normalizedTime, _attackCtrl, _expBaseAttack);
                }
                else if (i < (attackSamples + decaySamples))
                {
                    normalizedTime = (double)(i - attackSamples) / decaySamples;
                    targetAmplitude = 1.0 - ExponentialCurve(normalizedTime, _decayCtrl, _expBaseDecay) * (1.0 - _sustainLevel);
                }
                else if (i < (attackSamples + decaySamples + sustainSamples))
                {
                    targetAmplitude = _sustainLevel; // Sustain phase
                }
                else
                {
                    if (!hasReleaseStarted)
                    {
                        releaseStartAmplitude = visualBuffer[i - 1]; // Capture the amplitude at the very start of the release phase
                        hasReleaseStarted = true;
                    }
                    normalizedTime = (double)(i - (attackSamples + decaySamples + sustainSamples)) / releaseSamples;
                    targetAmplitude = releaseStartAmplitude * (1.0 - ExponentialCurve(normalizedTime, _releaseCtrl, _expBaseRelease));
                }

                visualBuffer[i] = (float)targetAmplitude;
            }

            return visualBuffer;
        }

        public void ScheduleGateOpen(double time, bool forceCloseFirst = false)
        {
            if (forceCloseFirst)
            {
                _scheduler.ScheduleValueAtTime(this, AudioParam.Gate, 1.0, time, 0.0);
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
    }
}
