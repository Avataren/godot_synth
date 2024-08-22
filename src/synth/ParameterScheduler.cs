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
        private int _processedEventCount = 0;  // Counter for processed events

        public double CurrentTimeInSeconds => _currentTimeInSeconds;

        // Property to access the processed event count
        public int ProcessedEventCount => _processedEventCount;

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

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds, double? initialValue = null)
        {
            lock (_lock)
            {
                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue));
                _nodeHasRemainingEvents[node][param] = true;
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double startTimeInSeconds, double endTimeInSeconds, double? initialValue = null)
        {
            lock (_lock)
            {
                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds, targetValue, initialValue), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds, targetValue, initialValue));
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
                        bool eventCounted = false; // Flag to count the event only once

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double timeAtSample = _currentTimeInSeconds + (i * increment);
                            double newValue = lastScheduledValue;

                            foreach (var evt in events)
                            {
                                if (timeAtSample >= evt.Time)
                                {
                                    if (i == 0 && evt.InitialValue.HasValue)
                                    {
                                        newValue = evt.InitialValue.Value;
                                    }
                                    else
                                    {
                                        newValue = evt.Value;
                                    }
                                    lastScheduledValue = newValue;
                                    eventsProcessed = true;

                                    // Count the event only once
                                    if (!eventCounted)
                                    {
                                        _processedEventCount++;
                                        eventCounted = true;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            buffer[i] = newValue;

                            if (!eventsProcessed && events.Count == 0)
                            {
                                FillRemainingBuffer(buffer, i, lastScheduledValue);
                                break;
                            }
                        }

                        _nodeLastScheduledValues[node][param] = lastScheduledValue;

                        // Remove events that have been processed
                        events.RemoveAll(evt => evt.Time <= _currentTimeInSeconds);
                    }
                }
            }
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

                _processedEventCount = 0;  // Reset the counter when clearing
            }
        }
    }

    public class ScheduleEvent : IComparable<ScheduleEvent>
    {
        public double Time { get; }
        public double Value { get; }
        public double? EndTime { get; }
        public double TargetValue { get; }
        public double? InitialValue { get; }

        public ScheduleEvent(double time, double value, double? endTime = null, double targetValue = 0.0, double? initialValue = null)
        {
            Time = time;
            Value = value;
            EndTime = endTime;
            TargetValue = targetValue;
            InitialValue = initialValue;
        }

        public int CompareTo(ScheduleEvent other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}
