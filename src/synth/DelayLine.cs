public class DelayLine
{
    private float[] buffer;
    private int writeIndex = 0;
    private int bufferSize;
    private int delaySamples;

    // Feedback and Wet/Dry Mix
    public float Feedback;
    public float WetMix;
    public float DryMix;

    public DelayLine(int delayInMilliseconds, int sampleRate, float feedback = 0.5f, float wetMix = 0.5f, float dryMix = 0.5f)
    {
        SetDelayTime(delayInMilliseconds, sampleRate);
        this.Feedback = feedback;
        this.WetMix = wetMix;
        this.DryMix = dryMix;
    }

    // Method to set or change the delay time dynamically
    public void SetDelayTime(int delayInMilliseconds, int sampleRate)
    {
        delaySamples = (int)(delayInMilliseconds * sampleRate / 1000.0f);
        bufferSize = delaySamples + 1;
        buffer = new float[bufferSize];
        writeIndex = 0;
    }

    // Method to process a single sample with enhancements
    public float Process(float inputSample)
    {
        // Read delayed sample
        int readIndex = (writeIndex + bufferSize - delaySamples) % bufferSize;
        float delayedSample = buffer[readIndex];

        // Apply feedback directly to the delayed sample, so even the first echo is attenuated
        float processedSample = delayedSample * Feedback;

        // Mix the original input with the processed (attenuated) delayed sample
        float outputSample = (DryMix * inputSample) + (WetMix * processedSample);

        // Write the new sample into the buffer with the feedback applied
        buffer[writeIndex] = inputSample + processedSample;

        // Increment write index and wrap around if necessary
        writeIndex = (writeIndex + 1) % bufferSize;

        // Return the output sample
        return outputSample;
    }
}
