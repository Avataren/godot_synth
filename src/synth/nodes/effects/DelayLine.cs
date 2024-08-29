public class DelayLine
{
    private SynthType[] buffer;
    private int writeIndex = 0;
    private int maxBufferSize;
    private int currentDelaySamples;
    private int targetDelaySamples;
    private SynthType crossfadePosition = 0;
    private const SynthType CrossfadeDuration = 0.050f; // 50ms crossfade
    public SynthType Feedback;
    public SynthType WetMix;
    public SynthType DryMix;
    public SynthType SampleRate;

    public DelayLine(int maxDelayInMilliseconds, SynthType sampleRate, SynthType feedback = 0.25f, SynthType wetMix = 0.5f, SynthType dryMix = 1.0f)
    {
        SampleRate = sampleRate;
        maxBufferSize = (int)(maxDelayInMilliseconds * sampleRate / 1000.0) + 1;
        buffer = new SynthType[maxBufferSize];
        this.Feedback = feedback;
        this.WetMix = wetMix;
        this.DryMix = dryMix;
        SetDelayTime(maxDelayInMilliseconds);
    }

    public void SetDelayTime(int delayInMilliseconds)
    {
        targetDelaySamples = (int)(delayInMilliseconds * SampleRate / 1000.0f);
        targetDelaySamples = System.Math.Min(targetDelaySamples, maxBufferSize - 1);

        if (currentDelaySamples != targetDelaySamples)
        {
            crossfadePosition = 0;
        }
    }

    public void Mute()
    {
        for (int i = 0; i < maxBufferSize; i++)
        {
            buffer[i] = 0.0f;
        }
    }

    public SynthType Process(SynthType inputSample)
    {
        int oldReadIndex = (writeIndex - currentDelaySamples + maxBufferSize) % maxBufferSize;
        int newReadIndex = (writeIndex - targetDelaySamples + maxBufferSize) % maxBufferSize;

        SynthType oldSample = buffer[oldReadIndex];
        SynthType newSample = buffer[newReadIndex];

        SynthType outputSample;

        if (crossfadePosition < 1)
        {
            // Crossfade between old and new delay times
            outputSample = (1 - crossfadePosition) * oldSample + crossfadePosition * newSample;
            crossfadePosition += 1 / (CrossfadeDuration * SampleRate);
            if (crossfadePosition >= 1)
            {
                currentDelaySamples = targetDelaySamples;
            }
        }
        else
        {
            outputSample = newSample;
        }

        // Apply feedback and wet/dry mix
        var processedSample = outputSample * Feedback;
        outputSample = (DryMix * inputSample) + (WetMix * processedSample);

        // Write the new sample into the buffer
        buffer[writeIndex] = inputSample + processedSample;

        // Increment write index
        writeIndex = (writeIndex + 1) % maxBufferSize;

        return outputSample;
    }
}