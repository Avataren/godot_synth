using NUnit.Framework;
using System.Collections.Generic;

namespace Synth.Tests
{
    [TestFixture]
    public class ParameterSchedulerTests
    {
        [Test]
        public void Process_WritesZeroAndOneToBuffer()
        {
            // Arrange
            var bufferSize = 5;
            var sampleRate = 44100;
            var increment = 1.0 / sampleRate;
            var scheduler = new ParameterScheduler(bufferSize, sampleRate);
            var node = new EnvelopeNode();
            var param = AudioParam.Gate;
            scheduler.RegisterNode(node, new List<AudioParam> { param });

            var time = 0.0; // let's assume current time in seconds
            scheduler.ScheduleValueAtTime(node, param, 1.0, time, 0.0); // At current time
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(0), "Should have 0 processed events");
            // Act
            scheduler.Process(increment); // Process with the increment corresponding to one sample
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(1), "Should have 1 processed events");

            // Assert
            var buffer = new double[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = scheduler.GetValueAtSample(node, param, i);
            }
            TestContext.WriteLine("Buffer values: " + string.Join(", ", buffer));
            // Expected: [0.0, 0.0, 0.0, 0.0, 1.0] or similar based on time resolution and how many samples are processed
            Assert.That(buffer[0], Is.EqualTo(0.0), "First buffer value should be 0.0");
            Assert.That(buffer[1], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[2], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[3], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[4], Is.EqualTo(1.0), "Last buffer value should be 1.0");
            // Act
            scheduler.Process(increment); // Process with the increment corresponding to one sample
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(1), "Should have 1 processed events after second process call");
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = scheduler.GetValueAtSample(node, param, i);
            }
            TestContext.WriteLine("Buffer values after process: " + string.Join(", ", buffer));
            Assert.That(buffer[0], Is.EqualTo(1.0), "First buffer value should be 1.0 after processing the next buffer");
            Assert.That(buffer[1], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[2], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[3], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[4], Is.EqualTo(1.0), "Last buffer value should be 1.0 after processing the next buffer");
        }

[Test]
        public void Process_WritesEventWithoutInitialValue()
        {
            // Arrange
            var bufferSize = 5;
            var sampleRate = 44100;
            var increment = 1.0 / sampleRate;
            var scheduler = new ParameterScheduler(bufferSize, sampleRate);
            var node = new EnvelopeNode();
            var param = AudioParam.Gate;
            scheduler.RegisterNode(node, new List<AudioParam> { param });

            var time = 0.0; // let's assume current time in seconds
            scheduler.ScheduleValueAtTime(node, param, 1.0, time); // At current time
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(0), "Should have 0 processed events");
            // Act
            scheduler.Process(increment); // Process with the increment corresponding to one sample
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(1), "Should have 1 processed events");

            // Assert
            var buffer = new double[bufferSize];
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = scheduler.GetValueAtSample(node, param, i);
            }
            TestContext.WriteLine("Buffer values: " + string.Join(", ", buffer));
            // Expected: [0.0, 0.0, 0.0, 0.0, 1.0] or similar based on time resolution and how many samples are processed
            Assert.That(buffer[0], Is.EqualTo(1.0), "First buffer value should be 0.0");
            Assert.That(buffer[1], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[2], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[3], Is.EqualTo(1.0), "Next sample value should be 1.0");
            Assert.That(buffer[4], Is.EqualTo(1.0), "Last buffer value should be 1.0");
            // Act
            scheduler.Process(increment); // Process with the increment corresponding to one sample
            Assert.That(scheduler.ProcessedEventCount, Is.EqualTo(1), "Should have 1 processed events after second process call");
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = scheduler.GetValueAtSample(node, param, i);
            }
            TestContext.WriteLine("Buffer values after process: " + string.Join(", ", buffer));
            Assert.That(buffer[0], Is.EqualTo(1.0), "First buffer value should be 1.0 after processing the next buffer");
            Assert.That(buffer[1], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[2], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[3], Is.EqualTo(1.0), "Next sample value should be 1.0 after processing the next buffer");
            Assert.That(buffer[4], Is.EqualTo(1.0), "Last buffer value should be 1.0 after processing the next buffer");
        }        
    }

}
