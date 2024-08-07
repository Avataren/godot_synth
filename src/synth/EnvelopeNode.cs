using Godot;
using System;

namespace Synth
{
	public class EnvelopeNode : AudioNode
	{
		private float envelopePosition = 0.0f;
		private float releaseStartPosition = 0.0f;
		private bool gateOpen = false;

		public float AttackTime { get; set; }
		public float DecayTime { get; set; }
		public float SustainLevel { get; set; }
		public float ReleaseTime { get; set; }

		private float currentAmplitude = 0.0f;
		private float releaseStartAmplitude = 0.0f;
		private const float smoothingFactor = 0.1f;  // Increased smoothing factor

		public EnvelopeNode(ModulationManager ModulationMgr, int numSamples, bool enabled = true) : base(ModulationMgr, numSamples)
		{
			AttackTime = 0.0f;
			DecayTime = 0.0f;
			SustainLevel = 1.0f;
			ReleaseTime = 0.0f;
			this.Enabled = enabled;
		}

		public override void OpenGate()
		{
			gateOpen = true;
			envelopePosition = 0.0f;
			currentAmplitude = 0.0f;  // Reset to ensure starting from zero
		}

		public override void CloseGate()
		{
			releaseStartPosition = envelopePosition;
			releaseStartAmplitude = currentAmplitude;
			gateOpen = false;
		}

		// public float GetEnvelopeValue(float position)
		// {
		// 	float targetAmplitude = CalculateTargetAmplitude(position);
		// 	return targetAmplitude;
		// }

		public float GetEnvelopeValue(float position)
		{
			float targetAmplitude = CalculateTargetAmplitude(position);
			float changeRate = Math.Abs(targetAmplitude - currentAmplitude);

			// Calculate a dynamic smoothing factor based on the rate of change
			// Smaller changeRate will lead to a higher smoothing factor, and vice versa
			float dynamicSmoothing = 0.1f / (changeRate + 0.1f);  // Adding a small constant to prevent division by zero
			dynamicSmoothing = Math.Clamp(dynamicSmoothing, 0.01f, 1.0f);  // Ensuring the smoothing factor stays within reasonable bounds

			// Apply the dynamic smoothing
			currentAmplitude += (targetAmplitude - currentAmplitude) * dynamicSmoothing;

			return currentAmplitude;
		}


		private float CalculateTargetAmplitude(float position)
		{
			if (gateOpen)
			{
				if (position < AttackTime)
				{
					return position / AttackTime;  // Attack phase
				}
				else if (position < AttackTime + DecayTime)
				{
					return 1 - (position - AttackTime) / DecayTime * (1 - SustainLevel);  // Decay phase
				}
				else
				{
					return SustainLevel;  // Sustain phase
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
				buffer[i] = GetEnvelopeValue(newPosition);
				newPosition += increment;
			}
			// Smooth handling of envelope position updates
			envelopePosition = newPosition;
		}
	}
}
