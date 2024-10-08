using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Synth
{
	public class EnvelopeNode : AudioNode
	{
		// Constants
		private const SynthType DefaultDecayTime = 0.0f;
		private const SynthType MinimumReleaseTime = 0.0045f;
		private const SynthType MinimumAttackTime = 0.005f;
		private const SynthType DefaultSustainLevel = 1.0f;
		private const SynthType DefaultAttackTime = MinimumAttackTime;
		private const SynthType DefaultReleaseTime = MinimumReleaseTime;
		private const int BufferSize = 256;
		private const SynthType CrossfadeDuration = 0.005f; // 5 ms crossfade

		// Private fields
		private SynthType _envelopePosition = SynthTypeHelper.Zero;
		private SynthType _releaseStartPosition = SynthTypeHelper.Zero;
		private bool _isGateOpen = false;
		private SynthType _attackTime = DefaultAttackTime;
		private SynthType _decayTime = DefaultDecayTime;
		private SynthType _sustainLevel = DefaultSustainLevel;
		private SynthType _releaseTime = DefaultReleaseTime;
		private SynthType _timeScale = SynthTypeHelper.One;

		private SynthType _currentAmplitude = SynthTypeHelper.Zero;
		private SynthType _attackStartAmplitude = SynthTypeHelper.Zero;
		private SynthType _releaseStartAmplitude = SynthTypeHelper.Zero;

		private SynthType _attackCtrl = 2.0f;
		private SynthType _decayCtrl = -3.0f;
		private SynthType _releaseCtrl = -3.5f;

		private SynthType _expBaseAttack;
		private SynthType _expBaseDecay;
		private SynthType _expBaseRelease;

		private readonly SynthType[] _attackBuffer = new SynthType[BufferSize];
		private readonly SynthType[] _decayBuffer = new SynthType[BufferSize];
		private readonly SynthType[] _releaseBuffer = new SynthType[BufferSize];

		private SynthType _crossfadePosition = SynthTypeHelper.Zero;
		private SynthType _previousAmplitude = SynthTypeHelper.Zero;

		// Properties
		public SynthType AttackCtrl
		{
			get => _attackCtrl;
			set
			{
				_attackCtrl = value;
				UpdateExponentialCurves();
				CalculateAttackBuffer();
			}
		}

		public SynthType DecayCtrl
		{
			get => _decayCtrl;
			set
			{
				_decayCtrl = value;
				UpdateExponentialCurves();
				CalculateDecayBuffer();
			}
		}

		public SynthType ReleaseCtrl
		{
			get => _releaseCtrl;
			set
			{
				_releaseCtrl = value;
				UpdateExponentialCurves();
				CalculateReleaseBuffer();
			}
		}

		public SynthType TimeScale
		{
			get => _timeScale;
			set => _timeScale = Math.Max(value, 0.1f);
		}

		public SynthType AttackTime
		{
			get => _attackTime * TimeScale;
			set
			{
				_attackTime = Math.Max(value, MinimumAttackTime);
				CalculateAttackBuffer();
			}
		}

		public SynthType DecayTime
		{
			get => _decayTime * TimeScale;
			set
			{
				_decayTime = Math.Max(value, SynthTypeHelper.Zero);
				CalculateDecayBuffer();
			}
		}

		public SynthType SustainLevel
		{
			get => _sustainLevel;
			set
			{
				_sustainLevel = Math.Clamp(value, SynthTypeHelper.Zero, SynthTypeHelper.One);
				CalculateDecayBuffer();
			}
		}

		public SynthType ReleaseTime
		{
			get => _releaseTime * TimeScale;
			set
			{
				_releaseTime = Math.Max(value, MinimumReleaseTime);
				CalculateReleaseBuffer();
			}
		}

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
			_expBaseAttack = SynthType.Pow(2.0f, _attackCtrl) - 1.0f;
			_expBaseDecay = SynthType.Pow(2.0f, _decayCtrl) - 1.0f;
			_expBaseRelease = SynthType.Pow(2.0f, _releaseCtrl) - 1.0f;
		}

		private void CalculateAttackBuffer()
		{
			for (int i = 0; i < BufferSize; i++)
			{
				SynthType normalizedTime = i / (SynthType)(BufferSize - 1);
				_attackBuffer[i] = ExponentialCurve(normalizedTime, _attackCtrl, _expBaseAttack);
			}
		}

		private void CalculateDecayBuffer()
		{
			for (int i = 0; i < BufferSize; i++)
			{
				SynthType normalizedTime = i / (SynthType)(BufferSize - 1);
				_decayBuffer[i] = SynthTypeHelper.One - (ExponentialCurve(normalizedTime, _decayCtrl, _expBaseDecay) * (SynthTypeHelper.One - _sustainLevel));
			}
		}

		private void CalculateReleaseBuffer()
		{
			for (int i = 0; i < BufferSize; i++)
			{
				SynthType normalizedTime = i / (SynthType)(BufferSize - 1);
				_releaseBuffer[i] = SynthTypeHelper.One - ExponentialCurve(normalizedTime, _releaseCtrl, _expBaseRelease);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private SynthType ExponentialCurve(SynthType x, SynthType c, SynthType precomputedBase)
		{
			return (SynthType.Pow(2.0f, c * x) - 1.0f) / precomputedBase;
		}

		public override void Process(double increment)
		{
			for (int i = 0; i < NumSamples; i++)
			{
				SynthType gateValue = (SynthType)_scheduler.GetValueAtSample(this, AudioParam.Gate, i);

				if (!_isGateOpen && gateValue > SynthTypeHelper.Half)
				{
					OpenGate(i);
				}
				else if (_isGateOpen && gateValue <= SynthTypeHelper.Half)
				{
					CloseGate(i);
				}

				_currentAmplitude = GetEnvelopeValue(_envelopePosition);
				buffer[i] = _currentAmplitude;
				_envelopePosition += (SynthType)increment;
			}
		}

		private void OpenGate(int sampleOffset)
		{
			_isGateOpen = true;
			_attackStartAmplitude = _currentAmplitude;  // Start from current amplitude
			_envelopePosition = sampleOffset / SampleRate;
			_crossfadePosition = SynthTypeHelper.Zero;
			_previousAmplitude = _currentAmplitude;
		}


		private void CloseGate(int sampleOffset)
		{
			_releaseStartPosition = _envelopePosition;
			_releaseStartAmplitude = _currentAmplitude;
			_isGateOpen = false;
		}

		private SynthType GetEnvelopeValue(SynthType position)
		{
			SynthType newValue;
			if (_isGateOpen)
			{
				if (position < AttackTime)
				{
					SynthType normalizedTime = position / AttackTime;
					newValue = _attackStartAmplitude + (CubicInterpolation(_attackBuffer, normalizedTime * (BufferSize - 1)) * (SynthTypeHelper.One - _attackStartAmplitude));
				}
				else if (position < (AttackTime + DecayTime))
				{
					SynthType normalizedTime = (position - AttackTime) / DecayTime;
					SynthType attackEndValue = CubicInterpolation(_attackBuffer, BufferSize - 1);
					SynthType decayValue = CubicInterpolation(_decayBuffer, normalizedTime * (BufferSize - 1));
					newValue = attackEndValue + (decayValue - attackEndValue) * normalizedTime;
				}
				else
				{
					newValue = SustainLevel;
				}
			}
			else
			{
				if (ReleaseTime > SynthTypeHelper.Zero)
				{
					SynthType normalizedTime = Math.Min((position - _releaseStartPosition) / ReleaseTime, SynthTypeHelper.One);
					newValue = _releaseStartAmplitude * CubicInterpolation(_releaseBuffer, normalizedTime * (BufferSize - 1));
				}
				else
				{
					newValue = SynthTypeHelper.Zero;
				}
			}

			if (_crossfadePosition < CrossfadeDuration)
			{
				SynthType crossfadeFactor = _crossfadePosition / CrossfadeDuration;
				newValue = _previousAmplitude + (newValue - _previousAmplitude) * crossfadeFactor;
				_crossfadePosition += SynthTypeHelper.One / SampleRate;
			}

			return newValue;
		}

		private SynthType CubicInterpolation(SynthType[] buffer, SynthType index)
		{
			int i = (int)index;
			SynthType t = index - i;

			// Ensure that i is within the bounds of the buffer
			i = Math.Clamp(i, 0, buffer.Length - 1);

			// Safe access for cubic interpolation with clamping at the boundaries
			SynthType y0 = (i > 0) ? buffer[i - 1] : buffer[i]; // Use current value if at start
			SynthType y1 = buffer[i]; // Current point
			SynthType y2 = (i < buffer.Length - 1) ? buffer[i + 1] : buffer[i]; // Use current if at end
			SynthType y3 = (i < buffer.Length - 2) ? buffer[i + 2] : y2; // Use next value if near the end, otherwise current

			// Cubic interpolation formula remains the same
			SynthType a0 = y3 - y2 - y0 + y1;
			SynthType a1 = y0 - y1 - a0;
			SynthType a2 = y2 - y0;
			SynthType a3 = y1;

			return a0 * t * t * t + a1 * t * t + a2 * t + a3;
		}


		public float[] GetVisualBuffer(int numSamples, float visualizationDuration = 3.0f)
		{
			float[] visualBuffer = new float[numSamples];
			float sampleRate = numSamples / visualizationDuration;
			float attackSamples = (float)AttackTime * sampleRate;
			float decaySamples = (float)DecayTime * sampleRate;
			float releaseSamples = (float)ReleaseTime * sampleRate;
			float sustainSamples = numSamples - (attackSamples + decaySamples + releaseSamples);

			float releaseStartAmplitude = (float)SustainLevel; // Assume release starts from sustain level

			for (int i = 0; i < numSamples; i++)
			{
				float position = i / sampleRate;

				if (position < (float)AttackTime)
				{
					float normalizedTime = position / (float)AttackTime;
					visualBuffer[i] = (float)CubicInterpolation(_attackBuffer, normalizedTime * (BufferSize - 1));
				}
				else if (position < (float)(AttackTime + DecayTime))
				{
					float normalizedTime = (position - (float)AttackTime) / (float)DecayTime;
					visualBuffer[i] = (float)CubicInterpolation(_decayBuffer, normalizedTime * (BufferSize - 1));
				}
				else if (position < (float)(AttackTime + DecayTime + sustainSamples / sampleRate))
				{
					visualBuffer[i] = (float)SustainLevel;
				}
				else
				{
					float releasePosition = position - (float)(AttackTime + DecayTime + sustainSamples / sampleRate);
					float normalizedTime = Math.Min(releasePosition / (float)ReleaseTime, 1.0f);
					visualBuffer[i] = releaseStartAmplitude * (float)CubicInterpolation(_releaseBuffer, normalizedTime * (BufferSize - 1));
				}
			}
			return visualBuffer;
		}

		public float GetEnvelopeBufferPosition(float visualizationDuration = 3.0f)
		{
			float nonSustainDuration = (float)AttackTime + (float)DecayTime + (float)ReleaseTime;
			float sustainDuration = Math.Max(0.0f, visualizationDuration - nonSustainDuration);
			float totalDuration = nonSustainDuration + sustainDuration;
			float currentPosition = (float)_envelopePosition;

			if (!_isGateOpen)
			{
				currentPosition = (float)_releaseStartPosition + (currentPosition - (float)_releaseStartPosition) * ((float)ReleaseTime / (totalDuration - (float)_releaseStartPosition));
			}
			return Math.Clamp(currentPosition / totalDuration, 0.0f, 1.0f);
		}

		public void ScheduleGateOpen(double time, bool forceCloseFirst = true)
		{
			if (forceCloseFirst)
			{
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, SynthTypeHelper.Zero, time);
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, SynthTypeHelper.One, time + 4 / SampleRate);
			}
			else
			{
				_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, SynthTypeHelper.One, time);
			}
		}

		public void ScheduleGateClose(double time)
		{
			_scheduler.ScheduleValueAtTime(this, AudioParam.Gate, SynthTypeHelper.Zero, time);
		}
	}
}
