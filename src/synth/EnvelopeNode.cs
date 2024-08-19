using System;
using Godot;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        private double envelopePosition = 0.0;
        private double releaseStartPosition = 0.0;
        private bool isGateOpen = false;
        private double _attackTime = 0.005;
        private double _decayTime = 0.1;
        private double _sustainLevel = 0.7;
        private double _releaseTime = 0.1;
        private double _smoothingFactor = 0.01;
        private double _transitionEndTime = 0.005;

        private double currentAmplitude = 0.0;
        private double releaseStartAmplitude = 0.0;
        private bool isInTransition = false;
        private double transitionStartAmplitude = 0.0;
        private double transitionTargetAmplitude = 0.0;

        private const double MinimumReleaseTime = 0.0045;

        private double _AttackCtrl = -0.45;
        private double _DecayCtrl = -0.48;
        private double _ReleaseCtrl = -0.5;

        private double expBaseAttack, expBaseDecay, expBaseRelease;

        // Properties with appropriate getters and setters
        public double AttackTime
        {
            get => _attackTime;
            set
            {
                _attackTime = value < 0.005 ? 0.005 : value;
                GD.Print("Attack Time: " + _attackTime + " for " + Name);
            }
        }

        public double DecayTime
        {
            get => _decayTime;
            set => _decayTime = value >= 0 ? value : 0.1; // Ensure DecayTime is not negative
        }

        public double SustainLevel
        {
            get => _sustainLevel;
            set => _sustainLevel = Math.Clamp(value, 0.0, 1.0); // Ensure SustainLevel is between 0 and 1
        }

        public double ReleaseTime
        {
            get => _releaseTime;
            set => _releaseTime = value >= MinimumReleaseTime ? value : MinimumReleaseTime;
        }

        public double SmoothingFactor
        {
            get => _smoothingFactor;
            set => _smoothingFactor = value > 0 ? value : 0.01; // Ensure SmoothingFactor is positive
        }

        public double TransitionEndTime
        {
            get => _transitionEndTime;
            set => _transitionEndTime = value > 0 ? value : 0.005; // Ensure TransitionEndTime is positive
        }

        public double AttackCtrl
        {
            get => _AttackCtrl;
            set
            {
                _AttackCtrl = value;
                expBaseAttack = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double DecayCtrl
        {
            get => _DecayCtrl;
            set
            {
                _DecayCtrl = value;
                expBaseDecay = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double ReleaseCtrl
        {
            get => _ReleaseCtrl;
            set
            {
                _ReleaseCtrl = value;
                expBaseRelease = Math.Pow(2.0, value) - 1.0;
            }
        }

        public EnvelopeNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples)
        {
            SampleFrequency = sampleFrequency;
            UpdateExponentialCurves();
        }

        private void UpdateExponentialCurves()
        {
            expBaseAttack = Math.Pow(2.0, _AttackCtrl) - 1.0;
            expBaseDecay = Math.Pow(2.0, _DecayCtrl) - 1.0;
            expBaseRelease = Math.Pow(2.0, _ReleaseCtrl) - 1.0;
        }

        public override void OpenGate()
        {
            isGateOpen = true;
            envelopePosition = 0.0;
            StartTransition(0.0);
        }

        private void StartTransition(double targetAmplitude)
        {
            isInTransition = true;
            transitionStartAmplitude = currentAmplitude;
            transitionTargetAmplitude = targetAmplitude;
        }

        public override void CloseGate()
        {
            releaseStartPosition = envelopePosition;
            releaseStartAmplitude = currentAmplitude;
            isGateOpen = false;
            if (_releaseTime <= MinimumReleaseTime)
            {
                _releaseTime = MinimumReleaseTime;
            }
        }

        private double ExponentialCurve(double x, double c, double precomputedBase)
        {
            return (Math.Pow(2, c * x) - 1) / precomputedBase;
        }

        private double CalculateTargetAmplitude(double position)
        {
            if (isGateOpen)
            {
                if (position < _attackTime)
                {
                    return ExponentialCurve(position / _attackTime, _AttackCtrl, expBaseAttack);
                }
                else if (position < _attackTime + _decayTime)
                {
                    double decayPosition = (position - _attackTime) / _decayTime;
                    return 1.0 - ExponentialCurve(decayPosition, _DecayCtrl, expBaseDecay) * (1.0 - _sustainLevel);
                }
                else
                {
                    return _sustainLevel;
                }
            }
            else
            {
                double releasePosition = (position - releaseStartPosition) / _releaseTime;
                if (releasePosition < 1.0)
                {
                    return releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, _ReleaseCtrl, expBaseRelease));
                }
                return 0.0;
            }
        }

        private double GetEnvelopeValue(double position)
        {
            double targetAmplitude = CalculateTargetAmplitude(position);
            return currentAmplitude += (targetAmplitude - currentAmplitude) * _smoothingFactor;
        }

        public override void Process(double increment)
        {
            double newPosition = envelopePosition;
            float[] bufferRef = buffer; // Cache the buffer reference

            for (int i = 0; i < NumSamples; i++)
            {
                if (isInTransition)
                {
                    double transitionProgress = (newPosition - envelopePosition) / _transitionEndTime;
                    if (transitionProgress >= 1.0)
                    {
                        transitionProgress = 1.0;
                        isInTransition = false;
                    }
                    currentAmplitude = transitionStartAmplitude + (transitionTargetAmplitude - transitionStartAmplitude) * transitionProgress;
                }
                else
                {
                    currentAmplitude = GetEnvelopeValue(newPosition);
                }

                bufferRef[i] = (float)currentAmplitude;

                newPosition += increment;
            }

            envelopePosition = newPosition;
        }
    }
}
