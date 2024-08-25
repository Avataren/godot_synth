// using NUnit.Framework;
// using System;
// using System.Collections.Generic;

// namespace Synth.Tests
// {
//     [TestFixture]
//     public class ParameterSchedulerTests
//     {
//         private ParameterScheduler scheduler;
//         private AudioNode testNode;
//         private AudioParam testParam;
//         private const int BufferSize = 128;
//         private const int SampleRate = 44100;
//         private const double PRECISION = 1e-3;

//         [SetUp]
//         public void Setup()
//         {
//             scheduler = new ParameterScheduler(BufferSize, SampleRate);
//             testNode = new PassThroughNode();
//             testParam = AudioParam.Input;
//             scheduler.RegisterNode(testNode, new List<AudioParam> { testParam });
//         }

//         [Test]
//         public void TestImmediateValueChange()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 1.0, 0);
//             scheduler.Process();

//             Assert.That(scheduler.GetValueAtSample(testNode, testParam, 0), Is.EqualTo(1.0).Within(PRECISION));
//             Assert.That(scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1), Is.EqualTo(1.0).Within(PRECISION));
//         }

//         [Test]
//         public void TestLinearRamp()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
//             scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);

//             for (int i = 0; i < SampleRate; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Linear Ramp Result: {result}");
//             Assert.That(result, Is.EqualTo(1.0).Within(PRECISION));
//         }

//         [Test]
//         public void TestExponentialRamp()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 1, 0);
//             scheduler.ExponentialRampToValueAtTime(testNode, testParam, 10, 1);

//             for (int i = 0; i < SampleRate; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Exponential Ramp Result: {result}");
//             Assert.That(result, Is.EqualTo(10.0).Within(PRECISION));
//         }

//         [Test]
//         public void TestOverlappingEvents()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
//             scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 0.5, 0.5);

//             for (int i = 0; i < SampleRate / 2; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Overlapping Events Result at 0.5s: {result}");
//             Assert.That(result, Is.EqualTo(0.5).Within(PRECISION));

//             for (int i = SampleRate / 2; i < SampleRate; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Overlapping Events Result at 1s: {result}");
//             Assert.That(result, Is.EqualTo(0.5).Within(PRECISION));
//         }

//         [Test]
//         public void TestExponentialRampOverlap()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 1, 0);
//             scheduler.ExponentialRampToValueAtTime(testNode, testParam, 100, 1);
//             scheduler.ExponentialRampToValueAtTime(testNode, testParam, 50, 1.5);

//             for (int i = 0; i < (int)(1.5 * SampleRate); i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Exponential Ramp Overlap Result: {result}");
//             Assert.That(result, Is.EqualTo(50.0).Within(PRECISION));
//         }

//         [Test]
//         public void TestCancelAndHold()
//         {
//             scheduler.SetCurrentTimeInSeconds(0);
//             scheduler.ScheduleValueAtTime(testNode, testParam, 0, 0);
//             scheduler.LinearRampToValueAtTime(testNode, testParam, 1, 1);

//             for (int i = 0; i < SampleRate / 2; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double midValue = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Mid-ramp value: {midValue}");
//             scheduler.Clear(); // This acts like cancelAndHold

//             for (int i = SampleRate / 2; i < SampleRate; i += BufferSize)
//             {
//                 scheduler.Process();
//             }

//             double result = scheduler.GetValueAtSample(testNode, testParam, BufferSize - 1);
//             Console.WriteLine($"Cancel and Hold Result: {result}");
//             Assert.That(result, Is.EqualTo(midValue).Within(PRECISION));
//         }
//     }
// }