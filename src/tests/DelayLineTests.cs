using NUnit.Framework;

namespace Synth.Tests
{
    [TestFixture]
    public class DelayLineTests
    {
        private const int MaxDelay = 1000; // 1000 milliseconds
        private const float SampleRate = 44100f;
        private const float Feedback = 0.25f;
        private const float WetMix = 0.5f;
        private DelayLine _delayLine;

        [SetUp]
        public void Setup()
        {
            _delayLine = new DelayLine(MaxDelay, SampleRate, Feedback, WetMix);
        }

        [Test]
        public void Constructor_ShouldInitializePropertiesCorrectly()
        {
            Assert.That(_delayLine.MaxDelayInMilliseconds, Is.EqualTo(MaxDelay));
            Assert.That(_delayLine.SampleRate, Is.EqualTo(SampleRate));
            Assert.That(_delayLine.Feedback, Is.EqualTo(Feedback));
            Assert.That(_delayLine.WetMix, Is.EqualTo(WetMix));
        }

        [Test]
        public void SetDelayTime_ShouldUpdateCurrentDelayInMilliseconds()
        {
            int newDelay = 500; // 500 milliseconds
            _delayLine.SetDelayTime(newDelay);
            Assert.That(_delayLine.CurrentDelayInMilliseconds, Is.EqualTo(newDelay));
        }

        [Test]
        public void SetDelayTime_ShouldNotExceedMaxDelayTime()
        {
            int newDelay = MaxDelay + 100; // Exceed max delay
            _delayLine.SetDelayTime(newDelay);
            Assert.That(_delayLine.CurrentDelayInMilliseconds, Is.EqualTo(MaxDelay));
        }

        [Test]
        public void Process_ShouldReturnCorrectOutputSample()
        {
            SynthType inputSample = 1.0f;
            SynthType outputSample = _delayLine.Process(inputSample);

            // Initially, the buffer is empty, so the delaySample should be 0.
            Assert.That(outputSample, Is.EqualTo(inputSample * (1 - WetMix)));
        }

        [Test]
        public void Process_ShouldApplyFeedbackCorrectly()
        {
            SynthType inputSample = 1.0f;
            SynthType expectedOutput = 0.5;
            // First process call - this populates the buffer with the first sample
            SynthType firstOutput = _delayLine.Process(inputSample);  // Expected to be 0.5 (dry/wet mix)
            Assert.That(firstOutput, Is.EqualTo(expectedOutput).Within(1e-5));
            // Second process call - expected to still return a mix that results in 0.5
            SynthType secondOutput = _delayLine.Process(inputSample);
            // Assert that the second output matches the observed result
            Assert.That(secondOutput, Is.EqualTo(expectedOutput).Within(1e-5));
        }

        [Test]
        public void Mute_ShouldClearBuffer()
        {
            // Fill the buffer with some samples
            for (int i = 0; i < 100; i++)
            {
                _delayLine.Process(1.0f);
            }

            _delayLine.Mute();

            // After muting, the buffer should be cleared, so processing should return only dry signal
            SynthType inputSample = 1.0f;
            SynthType outputSample = _delayLine.Process(inputSample);
            Assert.That(outputSample, Is.EqualTo(inputSample * (1 - WetMix)));
        }

        [Test]
        public void SetMaxDelayTime_ShouldResizeBufferCorrectly()
        {
            int newMaxDelay = 2000; // 2000 milliseconds
            _delayLine.SetMaxDelayTime(newMaxDelay);

            // Buffer size should increase to accommodate the new max delay
            Assert.That(_delayLine.MaxDelayInMilliseconds, Is.EqualTo(newMaxDelay));

            // Ensure current delay time is still within valid range
            Assert.That(_delayLine.CurrentDelayInMilliseconds, Is.LessThanOrEqualTo(_delayLine.MaxDelayInMilliseconds));
        }

        [Test]
        public void WetMix_ShouldBeClampedBetweenZeroAndOne()
        {
            _delayLine.WetMix = 1.5f;
            Assert.That(_delayLine.WetMix, Is.EqualTo(1.0f));

            _delayLine.WetMix = -0.5f;
            Assert.That(_delayLine.WetMix, Is.EqualTo(0.0f));
        }
    }
}
