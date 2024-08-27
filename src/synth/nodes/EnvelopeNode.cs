using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Synth
{
    public class EnvelopeNode : AudioNode
    {
        // Constants
        private const double DefaultDecayTime = 0.0;
        private const double MinimumReleaseTime = 0.0045;
        private const double MinimumAttackTime = 0.005;
        private const double DefaultSustainLevel = 1.0;
        private const double DefaultAttackTime = MinimumAttackTime;
        private const double DefaultReleaseTime = MinimumReleaseTime;
        private const int BufferSize = 256;

        // Private fields
        private double _envelopePosition = 0.0;
        private double _releaseStartPosition = 0.0;
        private bool _isGateOpen = false;
        private double _attackTime = DefaultAttackTime;
        private double _decayTime = DefaultDecayTime;
        private double _sustainLevel = DefaultSustainLevel;
        private double _releaseTime = DefaultReleaseTime;
        private double _timeScale = 1.0;

        private double _currentAmplitude = 0.0;
        private double _attackStartAmplitude = 0.0;
        private double _releaseStartAmplitude = 0.0;

        private double _attackCtrl = 2.0;
        private double _decayCtrl = -3.0;
        private double _releaseCtrl = -3.5;

        private double _expBaseAttack;
        private double _expBaseDecay;
        private double _expBaseRelease;

        private readonly double[] _attackBuffer = new double[BufferSize];
        private readonly double[] _decayBuffer = new double[BufferSize];
        private readonly double[] _releaseBuffer = new double[BufferSize];

        // Properties
        public double TimeScale
        {
            get => _timeScale;
            set => _timeScale = Math.Max(value, 0.1);
        }

        public double AttackTime
        {
            get => _attackTime * TimeScale;
            set
            {
                _attackTime = Math.Max(value, MinimumAttackTime);
                CalculateAttackBuffer();
            }
        }

        public double DecayTime
        {
            get => _decayTime * TimeScale;
            set
            {
                _decayTime = Math.Max(value, 0);
                CalculateDecayBuffer();
            }
        }

        public double SustainLevel
        {
            get => _sustainLevel;
            set
            {
                _sustainLevel = Math.Clamp(value, 0.0, 1.0);
                CalculateDecayBuffer();
            }
        }

        public double ReleaseTime
        {
            get => _releaseTime * TimeScale;
            set
            {
                _releaseTime = Math.Max(value, MinimumReleaseTime);
                CalculateReleaseBuffer();
            }
        }

        public double AttackCtrl
        {
            get => _attackCtrl;
            set
            {
                _attackCtrl = value;
                UpdateExponentialCurves();
                CalculateAttackBuffer();
            }
        }

        public double DecayCtrl
        {
            get => _decayCtrl;
            set
            {
                _decayCtrl = value;
                UpdateExponentialCurves();
                CalculateDecayBuffer();
            }
        }

        public double ReleaseCtrl
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
            _expBaseAttack = Math.Pow(2.0, _attackCtrl) - 1.0;
            _expBaseDecay = Math.Pow(2.0, _decayCtrl) - 1.0;
            _expBaseRelease = Math.Pow(2.0, _releaseCtrl) - 1.0;
        }

        // Buffer calculations
        private void CalculateAttackBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                double normalizedTime = i / (double)(BufferSize - 1);
                _attackBuffer[i] = ExponentialCurve(normalizedTime, _attackCtrl, _expBaseAttack);
            }
        }

        private void CalculateDecayBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                double normalizedTime = i / (double)(BufferSize - 1);
                _decayBuffer[i] = 1.0 - ExponentialCurve(normalizedTime, _decayCtrl, _expBaseDecay) * (1.0 - _sustainLevel);
            }
        }

        private void CalculateReleaseBuffer()
        {
            for (int i = 0; i < BufferSize; i++)
            {
                double normalizedTime = i / (double)(BufferSize - 1);
                _releaseBuffer[i] = 1.0 - ExponentialCurve(normalizedTime, _releaseCtrl, _expBaseRelease);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ExponentialCurve(double x, double c, double precomputedBase)
        {
            return (Math.Pow(2, c * x) - 1) / precomputedBase;
        }

        public override void OpenGate()
        {
            _isGateOpen = true;
            _attackStartAmplitude = _currentAmplitude;
            _envelopePosition = 0.0;
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
                double gateValue = _scheduler.GetValueAtSample(this, AudioParam.Gate, i);

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
                _envelopePosition += increment;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetEnvelopeValue(double position)
        {
            if (_isGateOpen)
            {
                if (position < AttackTime)
                {
                    double normalizedTime = position / AttackTime;
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return _attackStartAmplitude + (_attackBuffer[bufferIndex] * (1.0 - _attackStartAmplitude));
                }
                else if (position < (AttackTime + DecayTime))
                {
                    double normalizedTime = (position - AttackTime) / DecayTime;
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return 1.0 + (_decayBuffer[bufferIndex] - 1.0) * (1.0 - SustainLevel);
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
                    double normalizedTime = Math.Min((position - _releaseStartPosition) / ReleaseTime, 1.0);
                    int bufferIndex = (int)(normalizedTime * (BufferSize - 1));
                    return _releaseStartAmplitude * _releaseBuffer[bufferIndex];
                }
                else
                {
                    return 0.0;
                }
            }
        }

        public float[] GetVisualBuffer(int numSamples, double visualizationDuration = 3.0)
        {
            float[] visualBuffer = new float[numSamples];
            double sampleRate = numSamples / visualizationDuration;
            int attackSamples = (int)(AttackTime * sampleRate);
            int decaySamples = (int)(DecayTime * sampleRate);
            int releaseSamples = (int)(ReleaseTime * sampleRate);
            int sustainSamples = numSamples - (attackSamples + decaySamples + releaseSamples);

            for (int i = 0; i < numSamples; i++)
            {
                double targetAmplitude = 0.0;
                if (i < attackSamples)
                {
                    double normalizedTime = (double)i / attackSamples;
                    targetAmplitude = _attackBuffer[(int)(normalizedTime * (BufferSize - 1))];
                }
                else if (i < attackSamples + decaySamples)
                {
                    double normalizedTime = (double)(i - attackSamples) / decaySamples;
                    targetAmplitude = _decayBuffer[(int)(normalizedTime * (BufferSize - 1))];
                }
                else if (i < attackSamples + decaySamples + sustainSamples)
                {
                    targetAmplitude = SustainLevel;
                }
                else
                {
                    double normalizedTime = (double)(i - (attackSamples + decaySamples + sustainSamples)) / releaseSamples;
                    targetAmplitude = _releaseBuffer[(int)(normalizedTime * (BufferSize - 1))] * SustainLevel;
                }
                visualBuffer[i] = (float)targetAmplitude;
            }
            return visualBuffer;
        }

        public double GetEnvelopeBufferPosition(double visualizationDuration = 3.0)
        {
            double nonSustainDuration = AttackTime + DecayTime + ReleaseTime;
            double sustainDuration = Math.Max(0.0, visualizationDuration - nonSustainDuration);
            double totalDuration = nonSustainDuration + sustainDuration;
            double currentPosition = _envelopePosition;

            if (!_isGateOpen)
            {
                currentPosition = _releaseStartPosition + (currentPosition - _releaseStartPosition) * (ReleaseTime / (totalDuration - _releaseStartPosition));
            }
            return Math.Clamp(currentPosition / totalDuration, 0.0, 1.0);
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
