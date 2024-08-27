using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        // Constants
        private const float DefaultDecayTime = 0.0f;
        private const float MinimumReleaseTime = 0.0045f;
        private const float MinimumAttackTime = 0.005f;
        private const float DefaultSustainLevel = 1.0f;
        private const float DefaultAttackTime = MinimumAttackTime;
        private const float DefaultReleaseTime = MinimumReleaseTime;
        private const int BufferSize = 256;

        // Private fields
        private float _envelopePosition = 0.0f;
        private float _releaseStartPosition = 0.0f;
        private bool _isGateOpen = false;
        private float _attackTime = DefaultAttackTime;
        private float _decayTime = DefaultDecayTime;
        private float _sustainLevel = DefaultSustainLevel;
        private float _releaseTime = DefaultReleaseTime;
        private float _timeScale = 1.0f;

        private float _currentAmplitude = 0.0f;
        private float _attackStartAmplitude = 0.0f;
        private float _releaseStartAmplitude = 0.0f;

        private float _attackCtrl = 2.0f;
        private float _decayCtrl = -3.0f;
        private float _releaseCtrl = -3.5f;

        private float _expBaseAttack;
        private float _expBaseDecay;
        private float _expBaseRelease;

        private readonly float[] _attackBuffer = new float[BufferSize];
        private readonly float[] _decayBuffer = new float[BufferSize];
        private readonly float[] _releaseBuffer = new float[BufferSize];

        // Properties
        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = Mathf.Max(value, 0.1f);
        }

        public float AttackTime
        {
            get => _attackTime * TimeScale;
            set
            {
                _attackTime = Math.Max(value, MinimumAttackTime);
                CalculateAttackBuffer();
            }
        }

        public float DecayTime
        {
            get => _decayTime * TimeScale;
            set
            {
                _decayTime = Math.Max(value, 0);
                CalculateDecayBuffer();
            }
        }

        public float SustainLevel
        {
            get => _sustainLevel;
            set
            {
                _sustainLevel = Mathf.Clamp(value, 0.0f, 1.0f);
                CalculateDecayBuffer();
            }
        }

        public float ReleaseTime
        {
            get => _releaseTime * TimeScale;
            set
            {
                _releaseTime = Math.Max(value, MinimumReleaseTime);
                CalculateReleaseBuffer();
            }
        }

        public float AttackCtrl
        {
            get => _attackCtrl;
            set
            {
                _attackCtrl = value;
                UpdateExponentialCurves();
                CalculateAttackBuffer();
            }
        }

        public float DecayCtrl
        {
            get => _decayCtrl;
            set
            {
                _decayCtrl = value;
                UpdateExponentialCurves();
                CalculateDecayBuffer();
            }
        }

        public float ReleaseCtrl
        {
            get => _releaseCtrl;
            set
            {
                _releaseCtrl = value;
                UpdateExponentialCurves();
                CalculateReleaseBuffer();
            }
        }

        // Constructor
        public EnvelopeNode() : base()
        {
            _scheduler.RegisterNode(this, [AudioParam.Gate]);
            UpdateExponentialCurves();
            CalculateAttackBuffer();
            CalculateDecayBuffer();
            CalculateReleaseBuffer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateExponentialCurves()
        {
            _expBaseAttack = Mathf.Pow(2.0f, _attackCtrl) - 1.0f;
            _expBaseDecay = Mathf.Pow(2.0f, _decayCtrl) - 1.0f;
            _expBaseRelease = Mathf.Pow(2.0f, _releaseCtrl) - 1.0f;
        }

        // Buffer calculations
        private void CalculateAttackBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                float normalizedTime = i / (float)(BufferSize - 1);
                _attackBuffer[i] = ExponentialCurve(normalizedTime, _attackCtrl, _expBaseAttack);
            }
        }

        private void CalculateDecayBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                float normalizedTime = i / (float)(BufferSize - 1);
                _decayBuffer[i] = 1.0f - ExponentialCurve(normalizedTime, _decayCtrl, _expBaseDecay) * (1.0f - _sustainLevel);
            }
        }

        private void CalculateReleaseBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                float normalizedTime = i / (float)(BufferSize - 1);
                _releaseBuffer[i] = 1.0f - ExponentialCurve(normalizedTime, _releaseCtrl, _expBaseRelease);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ExponentialCurve(float x, float c, float precomputedBase)
        {
            return (Mathf.Pow(2, c * x) - 1.0f) / precomputedBase;
        }

        public override void OpenGate()
        {
            _isGateOpen = true;
            _attackStartAmplitude = _currentAmplitude;
            _envelopePosition = 0.0f;
        }

        public override void CloseGate()
        {
            _releaseStartPosition = _envelopePosition;
            _releaseStartAmplitude = _currentAmplitude;
            _isGateOpen = false;
        }

        public override void Process(double increment)
        {
            for (int i = 0; i < NumSamples; i++)
            {
                float gateValue = (float)_scheduler.GetValueAtSample(this, AudioParam.Gate, i);

                if (!_isGateOpen && gateValue > 0.5)
                {
                    OpenGate();
                }
                else if (_isGateOpen && gateValue <= 0.5)
                {
                    CloseGate();
                }
                _currentAmplitude = GetEnvelopeValue(_envelopePosition);
                buffer[i] = (float)_currentAmplitude;
                _envelopePosition += (float)increment;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetEnvelopeValue(float position)
        {
            if (_isGateOpen)
            {
                if (position < AttackTime)
                {
                    float normalizedTime = position / AttackTime;
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return _attackStartAmplitude + (_attackBuffer[bufferIndex] * (1.0f - _attackStartAmplitude));
                }
                else if (position < (AttackTime + DecayTime))
                {
                    float normalizedTime = (position - AttackTime) / DecayTime;
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return _decayBuffer[bufferIndex];
                }
                else
                {
                    return SustainLevel;
                }
            }
            else
            {
                if (ReleaseTime > 0)
                {
                    float normalizedTime = Mathf.Min((position - _releaseStartPosition) / ReleaseTime, 1.0f);
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return _releaseStartAmplitude * _releaseBuffer[bufferIndex];
                }
                else
                {
                    return 0.0f;
                }
            }
        }

        public float[] GetVisualBuffer(int numSamples, float visualizationDuration = 3.0f)
        {
            float[] visualBuffer = new float[numSamples];
            float sampleRate = numSamples / visualizationDuration;
            int attackSamples = (int)(AttackTime * sampleRate);
            int decaySamples = (int)(DecayTime * sampleRate);
            int releaseSamples = (int)(ReleaseTime * sampleRate);
            int sustainSamples = numSamples - (attackSamples + decaySamples + releaseSamples);

            for (int i = 0; i < numSamples; i++)
            {
                float targetAmplitude = 0.0f;
                if (i < attackSamples)
                {
                    float normalizedTime = (float)i / attackSamples;
                    targetAmplitude = _attackBuffer[(int)(normalizedTime * (BufferSize - 1))];
                }
                else if (i < attackSamples + decaySamples)
                {
                    float normalizedTime = (float)(i - attackSamples) / decaySamples;
                    targetAmplitude = _decayBuffer[(int)(normalizedTime * (BufferSize - 1))];
                }
                else if (i < attackSamples + decaySamples + sustainSamples)
                {
                    targetAmplitude = SustainLevel;
                }
                else
                {
                    float normalizedTime = (float)(i - (attackSamples + decaySamples + sustainSamples)) / releaseSamples;
                    targetAmplitude = _releaseBuffer[(int)(normalizedTime * (BufferSize - 1))] * SustainLevel;
                }
                visualBuffer[i] = (float)targetAmplitude;
            }
            return visualBuffer;
        }

        public float GetEnvelopeBufferPosition(float visualizationDuration = 3.0f)
        {
            float nonSustainDuration = AttackTime + DecayTime + ReleaseTime;
            float sustainDuration = Mathf.Max(0.0f, visualizationDuration - nonSustainDuration);
            float totalDuration = nonSustainDuration + sustainDuration;
            float currentPosition = _envelopePosition;

            if (!_isGateOpen)
            {
                currentPosition = _releaseStartPosition + (currentPosition - _releaseStartPosition) * (ReleaseTime / (totalDuration - _releaseStartPosition));
            }
            return Mathf.Clamp(currentPosition / totalDuration, 0.0f, 1.0f);
        }

        public void ScheduleGateOpen(double time, bool forceCloseFirst = true)
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
    }
}
