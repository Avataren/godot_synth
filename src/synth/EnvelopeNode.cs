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
        private double _smoothingFactor = 1.0f;// 0.01;
        private double _transitionEndTime = 0.005;

        private double currentAmplitude = 0.0;
        private double releaseStartAmplitude = 0.0;
        private bool isInTransition = false;
        private double transitionStartAmplitude = 0.0;
        private double transitionTargetAmplitude = 0.0;

        private const double MinimumReleaseTime = 0.0045;

        private double _AttackCtrl = -2.5;//-0.45 * 4.0;
        private double _DecayCtrl = -2.5;//-0.48 * 4.0;
        private double _ReleaseCtrl = -3.0;//-0.5 * 4.0;

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

        // Method to map the current envelope value to the buffer position
        public double GetEnvelopeBufferPosition(double visualizationDuration = 3.0)
        {
            // Calculate the duration of the envelope without the sustain phase
            double nonSustainDuration = _attackTime + _decayTime + _releaseTime;

            // Calculate the required sustain duration to fill the buffer
            double sustainDuration = Math.Max(0.0, visualizationDuration - nonSustainDuration);

            // Recalculate the total duration now including the extended sustain phase
            double totalDuration = nonSustainDuration + sustainDuration;

            // Calculate the current position in the envelope relative to the total duration
            double currentPosition = envelopePosition;

            if (!isGateOpen)
            {
                // If the gate is closed (in release phase), calculate the position relative to the release phase
                currentPosition = releaseStartPosition + (currentPosition - releaseStartPosition) * (_releaseTime / (totalDuration - releaseStartPosition));
            }

            // Normalize the current position to a value between 0 and 1
            double normalizedPosition = Math.Clamp(currentPosition / totalDuration, 0.0, 1.0);

            return normalizedPosition;
        }

        public float[] GetVisualBuffer(int numSamples, double visualizationDuration = 3.0)
        {
            // Calculate the duration of the envelope without the sustain phase
            double nonSustainDuration = _attackTime + _decayTime + _releaseTime;

            // The total buffer duration is defined by the visualizationDuration parameter
            double bufferDuration = visualizationDuration;

            // Calculate the required sustain duration to fill the buffer
            double sustainDuration = Math.Max(0.0, bufferDuration - nonSustainDuration);

            // Recalculate the total duration now including the extended sustain phase
            double totalDuration = nonSustainDuration + sustainDuration;

            // Calculate the time increment based on the total duration and the number of samples
            double timeIncrement = totalDuration / numSamples;

            // Create a buffer to hold the amplitude values
            float[] visualBuffer = new float[numSamples];

            // Initialize variables for simulating the envelope
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
                        // Attack phase
                        targetAmplitude = ExponentialCurve(simulatedPosition / _attackTime, _AttackCtrl, expBaseAttack);
                    }
                    else if (simulatedPosition < _attackTime + _decayTime)
                    {
                        // Decay phase
                        double decayPosition = (simulatedPosition - _attackTime) / _decayTime;
                        targetAmplitude = 1.0 - ExponentialCurve(decayPosition, _DecayCtrl, expBaseDecay) * (1.0 - _sustainLevel);
                    }
                    else if (simulatedPosition < _attackTime + _decayTime + sustainDuration)
                    {
                        // Sustain phase
                        targetAmplitude = _sustainLevel;
                    }
                    else
                    {
                        // Transition to release phase after the sustain phase
                        isGateOpen = false;
                        releaseStartPosition = simulatedPosition;
                        releaseStartAmplitude = _sustainLevel; // Start release from sustain level
                    }
                }
                else
                {
                    // Release phase
                    double releasePosition = (simulatedPosition - releaseStartPosition) / _releaseTime;
                    if (releasePosition < 1.0)
                    {
                        targetAmplitude = releaseStartAmplitude * (1.0 - ExponentialCurve(releasePosition, _ReleaseCtrl, expBaseRelease));
                    }
                    else
                    {
                        targetAmplitude = 0.0; // Fully released
                    }
                }

                // Apply smoothing to the amplitude change
                simulatedCurrentAmplitude += (targetAmplitude - simulatedCurrentAmplitude) * _smoothingFactor;

                // Store the calculated amplitude in the buffer
                visualBuffer[i] = (float)simulatedCurrentAmplitude;

                // Advance the simulated position by the correct time increment
                simulatedPosition += timeIncrement;
            }

            return visualBuffer;
        }


    }
}
