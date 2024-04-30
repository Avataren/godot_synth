using Godot;
using System;

public class EnvelopeNode : AudioNode
{
	private float timeOffsetSec = 0;
	private bool gateOpen = false;

	public float AttackTime { get; set; }
	public float DecayTime { get; set; }
	public float SustainLevel { get; set; }
	public float ReleaseTime { get; set; }

	private float currentAmplitude = 0.0f;
	private float releaseStartAmplitude = 0.0f;
	private const float smoothingFactor = 0.05f;  // Smoothing factor is constant

	public EnvelopeNode(int numSamples, bool enabled = true) : base(numSamples)
	{
		AttackTime = 0.0f;
		DecayTime = 0.0f;
		SustainLevel = 1.0f;
		ReleaseTime = 0.0f;
		this.Enabled = enabled;
	}

	public void OpenGate()
	{
		gateOpen = true;
		timeOffsetSec = 0.0f;
	}

	public void CloseGate()
	{
		timeOffsetSec = 0.0f;
		releaseStartAmplitude = currentAmplitude;
		gateOpen = false;
	}

	public float GetEnvelopeValue(float elapsedTimeSec)
	{
		float targetAmplitude = CalculateTargetAmplitude(elapsedTimeSec);
		// Smooth transition to avoid clicks
		currentAmplitude += (targetAmplitude - currentAmplitude) * smoothingFactor;
		return currentAmplitude;
	}

	private float CalculateTargetAmplitude(float elapsedTimeSec)
	{
		if (gateOpen)
		{
			if (elapsedTimeSec < AttackTime)
			{
				return elapsedTimeSec / AttackTime;
			}
			else if (elapsedTimeSec < AttackTime + DecayTime)
			{
				return 1 - (elapsedTimeSec - AttackTime) / DecayTime * (1 - SustainLevel);
			}
			else
			{
				return SustainLevel;
			}
		}
		else
		{
			if (elapsedTimeSec < ReleaseTime)
				return releaseStartAmplitude * (1 - elapsedTimeSec / ReleaseTime);
			else
				return 0.0f;
		}
	}

	public override AudioNode Process(float increment)
	{
		for (int i = 0; i < NumSamples; i++)
		{
			buffer[i] = GetEnvelopeValue(timeOffsetSec);
			timeOffsetSec += increment;
		}
		return this;
	}

}
