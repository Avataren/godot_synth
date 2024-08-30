namespace Synth
{

    public class SimpleLowPassFilter
    {
        private SynthType previousOutput = SynthTypeHelper.Zero;
        private SynthType alpha;

        public SimpleLowPassFilter(SynthType cutoffFrequencyHz, SynthType sampleRate)
        {
            SetCutoffFrequency(cutoffFrequencyHz, sampleRate);
        }

        public void SetCutoffFrequency(SynthType cutoffFrequencyHz, SynthType sampleRate)
        {
            alpha = 1.0f / (1.0f + (sampleRate / (2.0f * SynthType.Pi * cutoffFrequencyHz)));
        }

        public SynthType Process(SynthType input)
        {
            previousOutput += alpha * (input - previousOutput);
            return previousOutput;
        }

        public void Mute()
        {
            previousOutput = SynthTypeHelper.Zero;
        }
    }
}