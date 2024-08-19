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
        private bool _isInTransition = false;
        private double _transitionStartAmplitude = 0.0;
        private double _transitionTargetAmplitude = 0.0;

        private double _attackCtrl = -0.45 * 4.0;
        private double _decayCtrl = -0.48 * 4.0;
        private double _releaseCtrl = -0.5 * 4.0;

        private double _expBaseAttack, _expBaseDecay, _expBaseRelease;

        // Properties with appropriate getters and setters
        public double AttackTime
        {
            get => _attackTime;
            set => _attackTime = Math.Max(value, MinimumAttackTime);
        }

        public double DecayTime
        {
            get => _decayTime;
            set => _decayTime = Math.Max(value, 0);
        }

        public double SustainLevel
        {
            get => _sustainLevel;
            set => _sustainLevel = Math.Clamp(value, 0.0, 1.0);
        }

        public double ReleaseTime
        {
            get => _releaseTime;
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

        public EnvelopeNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples)
        {
            SampleFrequency = sampleFrequency;
            UpdateExponentialCurves();
        }

        private void UpdateExponentialCurves()
        {
            _expBaseAttack = Math.Pow(2.0, _attackCtrl) - 1.0;
            _expBaseDecay = Math.Pow(2.0, _decayCtrl) - 1.0;
            _expBaseRelease = Math.Pow(2.0, _releaseCtrl) - 1.0;
        }

        public override void OpenGate()
        {
            _isGateOpen = true;
            _envelopePosition = 0.0;
            StartTransition(0.0);
        }

        private void StartTransition(double targetAmplitude)
        {
            _isInTransition = true;
            _transitionStartAmplitude = _currentAmplitude;
            _transitionTargetAmplitude = targetAmplitude;
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
                if (position < _attackTime)
                {
                    return ExponentialCurve(position / _attackTime, _attackCtrl, _expBaseAttack);
                }
                else if (position < _attackTime + _decayTime)
                {
                    double decayPosition = (position - _attackTime) / _decayTime;
                    return 1.0 - ExponentialCurve(decayPosition, _decayCtrl, _expBaseDecay) * (1.0 - _sustainLevel);
                }
                else
                {
                    return _sustainLevel;
                }
            }
            else
            {
                double releasePosition = (position - _releaseStartPosition) / _releaseTime;
                if (releasePosition < 1.0)
                {
                    return _releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, _releaseCtrl, _expBaseRelease));
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
            double newPosition = _envelopePosition;
            float[] bufferRef = buffer; // Cache the buffer reference

            for (int i = 0; i < NumSamples; i++)
            {
                if (_isInTransition)
                {
                    double transitionProgress = (newPosition - _envelopePosition) / _transitionEndTime;
                    if (transitionProgress >= 1.0)
                    {
                        transitionProgress = 1.0;
                        _isInTransition = false;
                    }
                    _currentAmplitude = _transitionStartAmplitude + (_transitionTargetAmplitude - _transitionStartAmplitude) * transitionProgress;
                }
                else
                {
                    _currentAmplitude = GetEnvelopeValue(newPosition);
                }

                bufferRef[i] = (float)_currentAmplitude;

                newPosition += increment;
            }

            _envelopePosition = newPosition;
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
            double nonSustainDuration = _attackTime + _decayTime + _releaseTime;
            double bufferDuration = visualizationDuration;
            double sustainDuration = Math.Max(0.0, bufferDuration - nonSustainDuration);
            double totalDuration = nonSustainDuration + sustainDuration;
            double timeIncrement = totalDuration / numSamples;

            double visualizationSampleRate = numSamples / visualizationDuration;
            double adjustmentRatio = SampleFrequency / visualizationSampleRate;            
            double originalSmoothingFactor = 0.01; // This should be retrieved from your actual smoothing factor setting
            double adjustedSmoothingFactor = 1 - Math.Pow(1 - originalSmoothingFactor, adjustmentRatio);

            float[] visualBuffer = new float[numSamples];

            double simulatedPosition = 0.0;
            double simulatedCurrentAmplitude = 0.0;
            bool isGateOpen = true;
            double releaseStartPosition = 0.0;
            double releaseStartAmplitude = 0.0;

            for (int i = 0; i < numSamples; i++)
            {
                double targetAmplitude = simulatedCurrentAmplitude;

                if (isGateOpen)
                {
                    if (simulatedPosition < _attackTime)
                    {
                        targetAmplitude = ExponentialCurve(simulatedPosition / _attackTime, _attackCtrl, _expBaseAttack);
                    }
                    else if (simulatedPosition < _attackTime + _decayTime)
                    {
                        double decayPosition = (simulatedPosition - _attackTime) / _decayTime;
                        targetAmplitude = 1.0 - ExponentialCurve(decayPosition, _decayCtrl, _expBaseDecay) * (1.0 - _sustainLevel);
                    }
                    else if (simulatedPosition < _attackTime + _decayTime + sustainDuration)
                    {
                        targetAmplitude = _sustainLevel;
                    }
                    else
                    {
                        isGateOpen = false;
                        releaseStartPosition = simulatedPosition;
                        releaseStartAmplitude = _sustainLevel;
                    }
                }
                else
                {
                    double releasePosition = (simulatedPosition - releaseStartPosition) / _releaseTime;
                    if (releasePosition < 1.0)
                    {
                        targetAmplitude = releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, _releaseCtrl, _expBaseRelease));
                    }
                    else
                    {
                        targetAmplitude = 0.0;
                    }
                }

                simulatedCurrentAmplitude += (targetAmplitude - simulatedCurrentAmplitude) * adjustedSmoothingFactor;

                visualBuffer[i] = (float)simulatedCurrentAmplitude;

                simulatedPosition += timeIncrement;
            }

            return visualBuffer;
        }
    }
}
