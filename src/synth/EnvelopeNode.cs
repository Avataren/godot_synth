using System;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        private float envelopePosition = 0.0f;
        private float releaseStartPosition = 0.0f;
        private bool isGateOpen = false;

        public float AttackTime { get; set; } = 0.0f;
        public float DecayTime { get; set; } = 0.0f;
        public float SustainLevel { get; set; } = 1.0f;
        public float ReleaseTime { get; set; } = 0.0f;
        public float SmoothingFactor { get; set; } = 0.02f;
        public float TransitionEndTime { get; set; } = 0.005f;

        private float currentAmplitude = 0.0f;
        private float releaseStartAmplitude = 0.0f;

        private bool isInTransition = false;
        private float transitionStartAmplitude = 0.0f;
        private float transitionTargetAmplitude = 0.0f;
        private float MinimumReleaseTime = 0.0045f;

        public EnvelopeNode(int numSamples, float sampleFrequency = 44100.0f) : base(numSamples)
        {
            SampleFrequency = sampleFrequency;
        }

        public override void OpenGate()
        {
            isGateOpen = true;
            envelopePosition = 0.0f;
            StartTransition(CalculateAttackTargetAmplitude(TransitionEndTime));
        }

        private void StartTransition(float targetAmplitude)
        {
            isInTransition = true;
            transitionStartAmplitude = currentAmplitude;
            transitionTargetAmplitude = targetAmplitude;
        }

        private float CalculateAttackTargetAmplitude(float position)
        {
            return Math.Min(position / AttackTime, 1.0f);
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

        public float GetEnvelopeValue(float position)
        {
            float targetAmplitude = CalculateTargetAmplitude(position);
            currentAmplitude += (targetAmplitude - currentAmplitude) * SmoothingFactor;
            return currentAmplitude;
        }

        private float CalculateTargetAmplitude(float position)
        {
            if (isGateOpen)
            {
                if (position < AttackTime)
                {
                    return position / AttackTime;
                }
                else if (position < AttackTime + DecayTime)
                {
                    return 1 - (position - AttackTime) / DecayTime * (1 - SustainLevel);
                }
                else
                {
                    return SustainLevel;
                }
            }
            else
            {
                float releasePosition = position - releaseStartPosition;
                if (releasePosition < ReleaseTime)
                {
                    return releaseStartAmplitude * (1 - (releasePosition / ReleaseTime));
                }
                return 0.0f;
            }
        }

        public override void Process(float increment)
        {
            float newPosition = envelopePosition;

            for (int i = 0; i < NumSamples; i++)
            {
                if (isInTransition)
                {
                    float transitionProgress = newPosition / TransitionEndTime;
                    if (transitionProgress >= 1.0f)
                    {
                        transitionProgress = 1.0f;
                        isInTransition = false;
                    }
                    currentAmplitude = transitionStartAmplitude + (transitionTargetAmplitude - transitionStartAmplitude) * transitionProgress;
                }
                else
                {
                    currentAmplitude = GetEnvelopeValue(newPosition);
                }

                buffer[i] = currentAmplitude;

                newPosition += increment;
            }

            envelopePosition = newPosition;
        }
    }
}
