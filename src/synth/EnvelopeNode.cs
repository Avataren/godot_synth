using System;
using Godot;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        private double envelopePosition = 0.0;
        private double releaseStartPosition = 0.0;
        private bool isGateOpen = false;
        private double _attackTime = 0.005f;
        public double AttackTime
        {
            get
            {
                return _attackTime;
            }
            set
            {
                _attackTime = value;
                if (_attackTime < 0.005) { _attackTime = 0.005; }  // Set a small default attack time
                GD.Print("Attack Time: " + _attackTime + " for " + Name);
            }  // Set a small default attack time
        }
        public double DecayTime { get; set; } = 0.1;
        public double SustainLevel { get; set; } = 0.7;
        public double ReleaseTime { get; set; } = 0.1;
        public double SmoothingFactor { get; set; } = 0.01;  // Adjusted for smoother transitions
        public double TransitionEndTime { get; set; } = 0.005;

        private double currentAmplitude = 0.0;
        private double releaseStartAmplitude = 0.0;

        private bool isInTransition = false;
        private double transitionStartAmplitude = 0.0;
        private double transitionTargetAmplitude = 0.0;
        private double MinimumReleaseTime = 0.0045;

        private double _AttackCtrl;     // Control parameter for the Attack phase
        private double _DecayCtrl;     // Control parameter for the Decay phase
        private double _ReleaseCtrl;   // Control parameter for the Release phase

        public double AttackCtrl
        {
            get
            {
                return _AttackCtrl;
            }
            set
            {
                _AttackCtrl = value;
                expBaseAttack = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double DecayCtrl
        {
            get
            {
                return _DecayCtrl;
            }
            set
            {
                _DecayCtrl = value;
                expBaseDecay = Math.Pow(2.0, value) - 1.0;
            }
        }

        public double ReleaseCtrl
        {
            get
            {
                return _ReleaseCtrl;
            }
            set
            {
                _ReleaseCtrl = value;
                expBaseRelease = Math.Pow(2.0, value) - 1.0;
            }
        }

        private double expBaseAttack, expBaseDecay, expBaseRelease;

        public EnvelopeNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples)
        {
            SampleFrequency = sampleFrequency;
            AttackCtrl = -0.45;
            DecayCtrl = -0.48;
            ReleaseCtrl = -0.5;
        }

        public override void OpenGate()
        {
            isGateOpen = true;
            envelopePosition = 0.0;
            StartTransition(0.0);  // Start smoothly from 0
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
            if (ReleaseTime <= MinimumReleaseTime)
            {
                ReleaseTime = MinimumReleaseTime;
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
                if (position < AttackTime)
                {
                    return ExponentialCurve(position / AttackTime, AttackCtrl, expBaseAttack);
                }
                else if (position < AttackTime + DecayTime)
                {
                    double decayPosition = (position - AttackTime) / DecayTime;
                    return 1.0 - ExponentialCurve(decayPosition, DecayCtrl, expBaseDecay) * (1.0 - SustainLevel);
                }
                else
                {
                    return SustainLevel;
                }
            }
            else
            {
                double releasePosition = (position - releaseStartPosition) / ReleaseTime;
                if (releasePosition < 1.0)
                {
                    return releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, ReleaseCtrl, expBaseRelease));
                }
                return 0.0;
            }
        }

        // private double CalculateTargetAmplitude(double position)
        // {
        //     if (isGateOpen)
        //     {
        //         if (position < AttackTime)
        //         {
        //             return position / AttackTime;
        //         }
        //         else if (position < AttackTime + DecayTime)
        //         {
        //             return 1.0 - (position - AttackTime) / DecayTime * (1.0 - SustainLevel);
        //         }
        //         else
        //         {
        //             return SustainLevel;
        //         }
        //     }
        //     else
        //     {
        //         double releasePosition = position - releaseStartPosition;
        //         if (releasePosition < ReleaseTime)
        //         {
        //             return releaseStartAmplitude * (1.0 - (releasePosition / ReleaseTime));
        //         }
        //         return 0.0;
        //     }
        // }

        public double GetEnvelopeValue(double position)
        {
            double targetAmplitude = CalculateTargetAmplitude(position);
            currentAmplitude += (targetAmplitude - currentAmplitude) * SmoothingFactor;
            return currentAmplitude;
        }

        public override void Process(double increment)
        {
            double newPosition = envelopePosition;
            for (int i = 0; i < NumSamples; i++)
            {
                if (isInTransition)
                {
                    double transitionProgress = (newPosition - envelopePosition) / TransitionEndTime;
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

                buffer[i] = (float)currentAmplitude;

                newPosition += increment;
            }

            envelopePosition = newPosition;
        }
    }
}
