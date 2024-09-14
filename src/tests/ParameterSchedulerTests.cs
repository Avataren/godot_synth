using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Synth.Tests
{
    [TestFixture]
    public class ParameterSchedulerTests
    {
        private ParameterScheduler scheduler;
        private AudioNode testNode;
        private AudioParam testParam;
        private const int BufferSize = 128;
        private const int SampleRate = 44100;
        private const double PRECISION = 1e-3;

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Initialize Godot
            //Godot.Engine.Initialize();
        }

        [SetUp]
        public void Setup()
        {
            scheduler = new ParameterScheduler(BufferSize, SampleRate);
            testNode = new PassThroughNode();
            testParam = AudioParam.Input;
            scheduler.RegisterNode(testNode, new List<AudioParam> { AudioParam.Input, AudioParam.Gate, AudioParam.Pitch });
        }

        [Test]
        public void TestImmediateValueChange()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 1.0, 0);
            scheduler.Process();

            Assert.That(scheduler.GetValueAtSample(testNode, testParam, 0), Is.EqualTo(1.0).Within(PRECISION));
            Assert.That(scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1), Is.EqualTo(1.0).Within(PRECISION));
        }

        [Test]
        public void TestLinearRamp()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
            scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);

            for (int i = 0; i < SampleRate; i += BufferSize)
            {
                scheduler.Process();
            }

            double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Linear Ramp Result: {result}");
            Assert.That(result, Is.EqualTo(1.0).Within(PRECISION));
        }

        [Test]
        public void TestExponentialRamp()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 1, 0);
            scheduler.ExponentialRampToValueAtTime(testNode, testParam, 10, 1);

            for (int i = 0; i < SampleRate; i += BufferSize)
            {
                scheduler.Process();
            }

            double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Exponential Ramp Result: {result}");
            Assert.That(result, Is.EqualTo(10.0).Within(PRECISION));
        }

        [Test]
        public void TestOverlappingEvents()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
            scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);
            scheduler.ScheduleValueAtTime(testNode, testParam, 0.5, 0.5);

            for (int i = 0; i < SampleRate / 2; i += BufferSize)
            {
                scheduler.Process();
            }

            double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Overlapping Events Result at 0.5s: {result}");
            Assert.That(result, Is.EqualTo(0.5).Within(PRECISION));

            for (int i = SampleRate / 2; i < SampleRate; i += BufferSize)
            {
                scheduler.Process();
            }

            result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Overlapping Events Result at 1s: {result}");
            Assert.That(result, Is.EqualTo(0.5).Within(PRECISION));
        }

        [Test]
        public void TestExponentialRampOverlap()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 1, 0);
            scheduler.ExponentialRampToValueAtTime(testNode, testParam, 100, 1);
            scheduler.ExponentialRampToValueAtTime(testNode, testParam, 50, 1.5);

            for (int i = 0; i < (int)(1.5 * SampleRate); i += BufferSize)
            {
                scheduler.Process();
            }

            double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Exponential Ramp Overlap Result: {result}");
            Assert.That(result, Is.EqualTo(50.0).Within(PRECISION));
        }

        [Test]
        public void TestCancelAndHold()
        {
            scheduler.SetCurrentTimeInSeconds(0);
            scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
            scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);

            for (int i = 0; i < SampleRate / 2; i += BufferSize)
            {
                scheduler.Process();
            }

            double midValue = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Mid-ramp value: {midValue}");
            scheduler.Clear(); // This acts like cancelAndHold

            for (int i = SampleRate / 2; i < SampleRate; i += BufferSize)
            {
                scheduler.Process();
            }

            double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
            Console.WriteLine($"Cancel and Hold Result: {result}");
            Assert.That(result, Is.EqualTo(midValue).Within(PRECISION));
        }

        [Test]
        public void TestFrequencyChangeAtGateOpen()
        {
            // Initialize scheduler and set current time to 0
            scheduler.SetCurrentTimeInSeconds(0);

            // Schedule a frequency change and gate open at time 0
            double newFrequency = 440.0; // A4
            scheduler.ScheduleValueAtTime(testNode, AudioParam.Pitch, newFrequency, 0);

            // Let's assume 'Gate' is another parameter we're testing
            var gateParam = AudioParam.Gate;
            scheduler.ScheduleValueAtTime(testNode, gateParam, 1.0, 0); // Open the gate at time 0

            // Process the first buffer
            scheduler.Process();

            // Retrieve the parameter values at the first sample
            double frequencyValue = scheduler.GetValueAtSample(testNode,  AudioParam.Pitch, 0);
            double gateValue = scheduler.GetValueAtSample(testNode, gateParam, 0);

            // Assert that the frequency change took effect immediately
            Assert.That(frequencyValue, Is.EqualTo(newFrequency).Within(PRECISION));
            Assert.That(gateValue, Is.EqualTo(1.0).Within(PRECISION));

            // Optionally, check the values throughout the buffer
            for (int i = 0; i < BufferSize; i++)
            {
                frequencyValue = scheduler.GetValueAtSample(testNode,  AudioParam.Pitch, i);
                gateValue = scheduler.GetValueAtSample(testNode, gateParam, i);

                Assert.That(frequencyValue, Is.EqualTo(newFrequency).Within(PRECISION), $"Frequency mismatch at sample {i}");
                Assert.That(gateValue, Is.EqualTo(1.0).Within(PRECISION), $"Gate value mismatch at sample {i}");
            }
        }

    }
}