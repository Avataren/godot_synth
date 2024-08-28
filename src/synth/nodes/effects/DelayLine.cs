public class DelayLine
{
    private SynthType[] buffer;
    private int writeIndex = 0;
    private int bufferSize;
    private int delaySamples;

    // Feedback and Wet/Dry Mix
    public SynthType Feedback;
    public SynthType WetMix;
    public SynthType DryMix;

    public DelayLine(int delayInMilliseconds, int sampleRate, SynthType feedback = 0.25f, SynthType wetMix = 0.5f, SynthType dryMix = 1.0f)
    {
        SetDelayTime(delayInMilliseconds, sampleRate);
        this.Feedback = feedback;
        this.WetMix = wetMix;
        this.DryMix = dryMix;
    }

    // Method to set or change the delay time dynamically
    public void SetDelayTime(int delayInMilliseconds, int sampleRate)
    {
        delaySamples = (int)(delayInMilliseconds * sampleRate / 1000.0);
        bufferSize = delaySamples + 1;
        buffer = new SynthType[bufferSize];
        writeIndex = 0;
    }

    public void Mute()
    {
        for (int i = 0; i < bufferSize; i++)
        {
            buffer[i] = 0.0f;
        }
    }

    // Method to process a single sample with enhancements
    public SynthType Process(SynthType inputSample)
    {
        // Read delayed sample
        int readIndex = (writeIndex + bufferSize - delaySamples) % bufferSize;
        var delayedSample = buffer[readIndex];

        // Apply feedback directly to the delayed sample, so even the first echo is attenuated
        var processedSample = delayedSample * Feedback;

        // Mix the original input with the processed (attenuated) delayed sample
        var outputSample = (DryMix * inputSample) + (WetMix * processedSample);

        // Write the new sample into the buffer with the feedback applied
        buffer[writeIndex] = inputSample + processedSample;

        // Increment write index and wrap around if necessary
        writeIndex = (writeIndex + 1) % bufferSize;

        // Return the output sample
        return outputSample;
    }
}
