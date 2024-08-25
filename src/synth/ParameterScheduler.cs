using System;
using System.Collections.Generic;
using Godot;

namespace Synth
{
    public class ParameterScheduler
    {
        private const double TIME_EPSILON = 1e-10;
        private const double VALUE_EPSILON = 1e-6;
        private const double MIN_EXPONENTIAL_VALUE = 1e-6;
        private readonly int _bufferSize;
        private readonly double _sampleRate;
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, List<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private long _currentSample = 0;

        public double CurrentTimeInSeconds => _currentSample / _sampleRate;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
            _sampleRate = sampleRate;
        }

        public void SetCurrentTimeInSeconds(double timeInSeconds)
        {
            _currentSample = (long)(timeInSeconds * _sampleRate);
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            if (!_nodeParameterBuffers.ContainsKey(node))
            {
                _nodeParameterBuffers[node] = new Dictionary<AudioParam, double[]>();
                _nodeEventDictionary[node] = new Dictionary<AudioParam, List<ScheduleEvent>>();
                _nodeLastScheduledValues[node] = new Dictionary<AudioParam, double>();

                foreach (var param in parameters)
                {
                    _nodeParameterBuffers[node][param] = new double[_bufferSize];
                    _nodeEventDictionary[node][param] = new List<ScheduleEvent>();
                    _nodeLastScheduledValues[node][param] = 0.0;
                }
            }
        }

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            long sampleTime = (long)(timeInSeconds * _sampleRate);
            var events = _nodeEventDictionary[node][param];

            // Remove all events after the new event's time
            events.RemoveAll(e => e.SampleTime > sampleTime);

            events.Add(new ScheduleEvent(sampleTime, value));
            events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));

            // Update the last scheduled value if the event is immediate or in the past
            if (sampleTime <= _currentSample)
            {
                _nodeLastScheduledValues[node][param] = value;
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0)
            {
                throw new ArgumentException("Target value for exponential ramp must be greater than zero.");
            }

            long endSampleTime = (long)(endTimeInSeconds * _sampleRate);
            var events = _nodeEventDictionary[node][param];
            double startValue = _nodeLastScheduledValues[node][param];

            // Remove all events after the current time
            events.RemoveAll(e => e.SampleTime > _currentSample);

            if (startValue <= MIN_EXPONENTIAL_VALUE)
            {
                startValue = MIN_EXPONENTIAL_VALUE;
                events.Add(new ScheduleEvent(_currentSample, startValue));
            }

            events.Add(new ScheduleEvent(_currentSample, startValue, endSampleTime, targetValue, true));
            events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            long endSampleTime = (long)(endTimeInSeconds * _sampleRate);
            var events = _nodeEventDictionary[node][param];
            var startValue = _nodeLastScheduledValues[node][param];

            // Remove all events after the current time
            events.RemoveAll(e => e.SampleTime > _currentSample);

            events.Add(new ScheduleEvent(_currentSample, startValue, endSampleTime, targetValue, false));
            events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));
        }

        public void Process()
        {
            foreach (var node in _nodeEventDictionary.Keys)
            {
                foreach (var param in _nodeEventDictionary[node].Keys)
                {
                    var buffer = _nodeParameterBuffers[node][param];
                    var events = _nodeEventDictionary[node][param];
                    double currentValue = _nodeLastScheduledValues[node][param];

                    for (int i = 0; i < _bufferSize; i++)
                    {
                        long sampleTime = _currentSample + i;

                        while (events.Count > 0 && sampleTime >= events[0].SampleTime)
                        {
                            var activeEvent = events[0];
                            if (!activeEvent.EndSampleTime.HasValue || sampleTime >= activeEvent.EndSampleTime.Value)
                            {
                                currentValue = activeEvent.EndSampleTime.HasValue ? activeEvent.TargetValue : activeEvent.Value;
                                events.RemoveAt(0);
                            }
                            else
                            {
                                double progress = (sampleTime - activeEvent.SampleTime) / (double)(activeEvent.EndSampleTime.Value - activeEvent.SampleTime);
                                progress = Math.Max(0, Math.Min(1, progress)); // Ensure progress is between 0 and 1
                                if (activeEvent.IsExponential)
                                {
                                    currentValue = InterpolateExponential(activeEvent.Value, activeEvent.TargetValue, progress);
                                }
                                else
                                {
                                    currentValue = InterpolateLinear(activeEvent.Value, activeEvent.TargetValue, progress);
                                }
                                break;
                            }
                        }

                        buffer[i] = Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
                    }

                    _nodeLastScheduledValues[node][param] = currentValue;
                }
            }

            _currentSample += _bufferSize;
        }

        private double InterpolateLinear(double start, double end, double progress)
        {
            double result = start + progress * (end - start);
            return Math.Abs(result - end) < VALUE_EPSILON ? end : result;
        }

        private double InterpolateExponential(double start, double end, double progress)
        {
            double result = start * Math.Pow(end / start, progress);
            return Math.Abs(result - end) < VALUE_EPSILON ? end : result;
        }

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            return _nodeParameterBuffers[node][param][sampleIndex];
        }

        public void Clear()
        {
            foreach (var node in _nodeParameterBuffers.Keys)
            {
                foreach (var param in _nodeParameterBuffers[node].Keys)
                {
                    var currentValue = _nodeLastScheduledValues[node][param];
                    Array.Fill(_nodeParameterBuffers[node][param], currentValue);
                    _nodeEventDictionary[node][param].Clear();
                    // Do not reset _nodeLastScheduledValues to maintain the current value
                }
            }
        }
    }

    public class ScheduleEvent
    {
        public long SampleTime { get; }
        public double Value { get; }
        public long? EndSampleTime { get; }
        public double TargetValue { get; }
        public bool IsExponential { get; }

        public ScheduleEvent(long sampleTime, double value, long? endSampleTime = null, double targetValue = 0.0, bool isExponential = false)
        {
            SampleTime = sampleTime;
            Value = value;
            EndSampleTime = endSampleTime;
            TargetValue = targetValue;
            IsExponential = isExponential;
        }
    }
}