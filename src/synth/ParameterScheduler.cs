using System;
using System.Collections.Generic;

namespace Synth
{
    public class ParameterScheduler
    {
        private readonly object _lock = new object();
        private readonly int _bufferSize;
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, List<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, bool>> _nodeHasRemainingEvents = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private double _currentTimeInSeconds = 0.0;
        public double CurrentTimeInSeconds => _currentTimeInSeconds;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            lock (_lock)
            {

                if (!_nodeParameterBuffers.ContainsKey(node))
                {
                    _nodeParameterBuffers[node] = new Dictionary<AudioParam, double[]>();
                    _nodeEventDictionary[node] = new Dictionary<AudioParam, List<ScheduleEvent>>();
                    _nodeHasRemainingEvents[node] = new Dictionary<AudioParam, bool>();
                    _nodeLastScheduledValues[node] = new Dictionary<AudioParam, double>();

                    foreach (var param in parameters)
                    {
                        _nodeParameterBuffers[node][param] = new double[_bufferSize];
                        _nodeEventDictionary[node][param] = new List<ScheduleEvent>();
                        _nodeHasRemainingEvents[node][param] = true;
                        _nodeLastScheduledValues[node][param] = 0.0;
                    }
                }
            }
        }

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            lock (_lock)
            {

                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(timeInSeconds, value), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(timeInSeconds, value));
                _nodeHasRemainingEvents[node][param] = true;
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double startTimeInSeconds, double endTimeInSeconds)
        {
            lock (_lock)
            {

                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds));
                _nodeHasRemainingEvents[node][param] = true;
            }
        }

        public void Process(double increment)
        {
            lock (_lock)
            {

                _currentTimeInSeconds += increment * _bufferSize;

                foreach (var node in _nodeEventDictionary.Keys)
                {
                    foreach (var param in _nodeEventDictionary[node].Keys)
                    {
                        if (!_nodeHasRemainingEvents[node][param])
                        {
                            continue;
                        }

                        var buffer = _nodeParameterBuffers[node][param];
                        var events = _nodeEventDictionary[node][param];
                        double lastScheduledValue = _nodeLastScheduledValues[node][param];
                        bool eventsProcessed = false;

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double timeAtSample = _currentTimeInSeconds + (i * increment);
                            double newValue = GetScheduledValueAtTime(events, timeAtSample, ref lastScheduledValue);

                            if (newValue != lastScheduledValue)
                            {
                                eventsProcessed = true;
                            }

                            buffer[i] = newValue;
                            lastScheduledValue = newValue;

                            if (!eventsProcessed && events.Count == 0)
                            {
                                FillRemainingBuffer(buffer, i, lastScheduledValue);
                                break;
                            }
                        }

                        _nodeLastScheduledValues[node][param] = lastScheduledValue;

                        events.RemoveAll(evt => evt.Time <= _currentTimeInSeconds);
                    }
                }
            }
        }

        private double GetScheduledValueAtTime(List<ScheduleEvent> events, double timeInSeconds, ref double lastScheduledValue)
        {
            foreach (var evt in events)
            {
                if (timeInSeconds >= evt.Time)
                {
                    lastScheduledValue = evt.Value;
                }
                else
                {
                    break;
                }
            }

            return lastScheduledValue;
        }

        private void FillRemainingBuffer(double[] buffer, int startIndex, double value)
        {
            for (int i = startIndex; i < buffer.Length; i++)
            {
                buffer[i] = value;
            }
        }

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            if (sampleIndex < _bufferSize)
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            return 0.0;
        }

        public void Clear()
        {
            lock (_lock)
            {

                foreach (var node in _nodeParameterBuffers.Keys)
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        Array.Clear(_nodeParameterBuffers[node][param], 0, _bufferSize);
                        _nodeEventDictionary[node][param].Clear();
                        _nodeHasRemainingEvents[node][param] = false;
                        _nodeLastScheduledValues[node][param] = 0.0;
                    }
                }
            }
        }
    }

    public class ScheduleEvent : IComparable<ScheduleEvent>
    {
        public double Time { get; }
        public double Value { get; }
        public double? EndTime { get; }
        public double TargetValue { get; }

        public ScheduleEvent(double time, double value, double? endTime = null, double targetValue = 0.0)
        {
            Time = time;
            Value = value;
            EndTime = endTime;
            TargetValue = targetValue;
        }

        public int CompareTo(ScheduleEvent other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}
