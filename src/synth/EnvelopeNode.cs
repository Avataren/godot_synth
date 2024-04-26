using Godot;
using System;

public class EnvelopeNode : AudioNode
{
	private ulong timeOffsetUSec = 0;
	private bool gateOpen = false;

	public float AttackTime { get; set; }
	public float DecayTime { get; set; }
	public float SustainLevel { get; set; }
	public float ReleaseTime { get; set; }

	private float currentAmplitude = 0.0f;
	private float releaseStartAmplitude = 0.0f;
	private const float smoothingFactor = 0.05f;  // Smoothing factor is constant

	public EnvelopeNode(int numSamples) : base(numSamples)
	{
		AttackTime = 0.0f;
		DecayTime = 0.0f;
		SustainLevel = 1.0f;
		ReleaseTime = 0.0f;
	}

	public void OpenGate()
	{
		gateOpen = true;
		timeOffsetUSec =0;
	}

	public void CloseGate()
	{
		timeOffsetUSec = 0;
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
		ulong sampleIncrementUSec = (ulong)(1_000_000 / SampleFrequency);  // Time per sample in microseconds
		for (int i = 0; i < NumSamples; i++)
		{
			float elapsedTimeSec = timeOffsetUSec / 1_000_000.0f;  // Convert microseconds to seconds
			buffer[i] = GetEnvelopeValue(elapsedTimeSec);  // Get the envelope value for the accumulated time
			timeOffsetUSec += sampleIncrementUSec;  // Increment by the time taken for one sample
		}
		return this;
	}

}
