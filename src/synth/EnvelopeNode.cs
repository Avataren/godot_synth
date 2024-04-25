using Godot;
using System;

public class EnvelopeNode : AudioNode
{
    private ulong gateOpenTimeUSec;
    private ulong timeOffsetUSec = 0;
    private ulong gateCloseTimeUSec;
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
        AttackTime = 0.001f;  // Default values
        DecayTime = 0.25f;
        SustainLevel = 0.5f;
        ReleaseTime = 0.5f;
    }

    public void OpenGate()
    {
        gateOpenTimeUSec = Time.GetTicksUsec();
        gateOpen = true;
    }

    public void CloseGate()
    {
        gateCloseTimeUSec = Time.GetTicksUsec();
        releaseStartAmplitude = currentAmplitude;
        gateOpen = false;
    }

    public float GetEnvelopeValue(ulong currentTimeUSec)
    {
        ulong elapsedTimeUSec = currentTimeUSec - (gateOpen ? gateOpenTimeUSec : gateCloseTimeUSec);
        float elapsedTimeSec = elapsedTimeUSec / 1_000_000.0f;
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
                return elapsedTimeSec / AttackTime;
            else if (elapsedTimeSec < AttackTime + DecayTime)
                return 1 - (elapsedTimeSec - AttackTime) / DecayTime * (1 - SustainLevel);
            else
                return SustainLevel;
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
        ulong sampleIncrementUSec = (ulong)(1_000_000 / SampleFrequency);
        ulong currentTimeUSec = gateOpen ? gateOpenTimeUSec : gateCloseTimeUSec;
        for (int i = 0; i < NumSamples; i++)
        {
            buffer[i] = GetEnvelopeValue(currentTimeUSec + timeOffsetUSec);
            timeOffsetUSec += sampleIncrementUSec;
        }
        return this;
    }
}
