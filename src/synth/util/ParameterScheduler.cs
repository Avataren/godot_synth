using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

namespace Synth
{
    public class ParameterScheduler : IDisposable
    {
        private const double TIME_EPSILON = 1e-10;
        private const double MIN_EXPONENTIAL_VALUE = 1e-6;
        private const double VALUE_EPSILON = 1e-6;

        private readonly int _bufferSize;
        private readonly double _sampleRate;

        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, SortedSet<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private readonly ScheduleEventPool _eventPool = new();
        private double _currentSample = 0;
        private readonly ReaderWriterLockSlim _globalLock = new();
        private readonly ConcurrentDictionary<AudioNode, ReaderWriterLockSlim> _nodeLocks = new();

        public double CurrentTimeInSeconds => _currentSample / _sampleRate;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
            _sampleRate = sampleRate;
            Clear();
        }

        public void SetCurrentTimeInSeconds(double timeInSeconds)
        {
            Interlocked.Exchange(ref _currentSample, timeInSeconds * _sampleRate);
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _globalLock.EnterWriteLock();
            try
            {
                if (!_nodeParameterBuffers.ContainsKey(node))
                {
                    var paramBuffers = new Dictionary<AudioParam, double[]>();
                    var eventDictionary = new Dictionary<AudioParam, SortedSet<ScheduleEvent>>();
                    var lastScheduledValues = new Dictionary<AudioParam, double>();

                    foreach (var param in parameters)
                    {
                        paramBuffers[param] = new double[_bufferSize];
                        eventDictionary[param] = new SortedSet<ScheduleEvent>(new ScheduleEventComparer());
                        lastScheduledValues[param] = 0.0;
                    }

                    _nodeParameterBuffers[node] = paramBuffers;
                    _nodeEventDictionary[node] = eventDictionary;
                    _nodeLastScheduledValues[node] = lastScheduledValues;
                    _nodeLocks[node] = new ReaderWriterLockSlim();
                }
            }
            finally
            {
                _globalLock.ExitWriteLock();
            }
        }

        public void ScheduleValuesAtTimeBulk(AudioNode node, AudioParam param, IEnumerable<(double timeInSeconds, double value)> eventsToAdd)
        {
            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            nodeLock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];

                // Prepare new events
                var newEvents = new List<ScheduleEvent>();
                foreach (var (timeInSeconds, value) in eventsToAdd)
                {
                    double sampleTime = timeInSeconds * _sampleRate;
                    newEvents.Add(_eventPool.Get(sampleTime, value, isSetValueAtTime: true));
                }

                // Sort and insert new events in bulk
                newEvents.Sort((e1, e2) => e1.SampleTime.CompareTo(e2.SampleTime));
                foreach (var ev in newEvents)
                {
                    events.Add(ev);
                }

                // Update the last scheduled value based on the latest event
                var lastEvent = newEvents.LastOrDefault();
                if (lastEvent != null && lastEvent.SampleTime <= _currentSample + TIME_EPSILON)
                {
                    _nodeLastScheduledValues[node][param] = lastEvent.Value;
                }
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            double sampleTime = timeInSeconds * _sampleRate;

            nodeLock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];

                // Clear conflicting events
                ClearConflictingEvents(events, sampleTime);

                // Add new event
                var newEvent = _eventPool.Get(sampleTime, value, isSetValueAtTime: true);
                events.Add(newEvent);

                // Update the last scheduled value if necessary
                if (sampleTime <= _currentSample + TIME_EPSILON)
                {
                    _nodeLastScheduledValues[node][param] = value;
                }
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        private void ClearConflictingEvents(SortedSet<ScheduleEvent> events, double newEventTime)
        {
            var conflictingEvents = events
                .Where(e => Math.Abs(e.SampleTime - newEventTime) < TIME_EPSILON ||
                            (e.EndSampleTime.HasValue && newEventTime > e.SampleTime && newEventTime <= e.EndSampleTime.Value))
                .ToList();

            foreach (var existingEvent in conflictingEvents)
            {
                if (existingEvent.EndSampleTime.HasValue && newEventTime < existingEvent.EndSampleTime.Value)
                {
                    // Truncate the event
                    double clippedTime = newEventTime - 1 / _sampleRate;
                    double truncatedValue = CalculateValueAtTime(existingEvent, clippedTime);

                    events.Remove(existingEvent);
                    existingEvent.EndSampleTime = clippedTime;
                    existingEvent.TargetValue = truncatedValue;
                    events.Add(existingEvent);
                }
                else
                {
                    events.Remove(existingEvent);
                    _eventPool.Return(existingEvent);
                }
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0)
            {
                throw new InvalidParameterValueException(nameof(targetValue), targetValue);
            }

            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            nodeLock.EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                // Remove any events that start after or at the same time as this new ramp
                events.RemoveWhere(e => e.SampleTime >= startSampleTime);

                // Truncate the last event if it overlaps with this new ramp
                TruncateOngoingEvent(events, startSampleTime);

                // Add the new exponential ramp
                events.Add(_eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, isExponential: true));

                // Update the last scheduled value
                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            nodeLock.EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                // Remove any events that start after or at the same time as this new ramp
                events.RemoveWhere(e => e.SampleTime >= startSampleTime);

                // Truncate the last event if it overlaps with this new ramp
                TruncateOngoingEvent(events, startSampleTime);

                // Add the new linear ramp
                events.Add(_eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, isExponential: false));

                // Update the last scheduled value
                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        private void TruncateOngoingEvent(SortedSet<ScheduleEvent> events, double sampleTime)
        {
            if (events.Count > 0)
            {
                var lastEvent = events.Max;

                if (!lastEvent.EndSampleTime.HasValue)
                {
                    return;
                }

                if (sampleTime <= lastEvent.SampleTime + TIME_EPSILON)
                {
                    return;
                }

                double truncatedValue = CalculateValueAtTime(lastEvent, sampleTime);

                events.Remove(lastEvent);
                lastEvent.EndSampleTime = sampleTime;
                lastEvent.TargetValue = truncatedValue;
                events.Add(lastEvent);
            }
        }

        private double CalculateCurrentValue(AudioNode node, AudioParam param)
        {
            var events = _nodeEventDictionary[node][param];
            double currentValue = _nodeLastScheduledValues[node][param];
            double sampleTime = _currentSample;

            if (events.Count > 0)
            {
                var activeEvent = events.FirstOrDefault(e => e.SampleTime <= sampleTime && (!e.EndSampleTime.HasValue || sampleTime <= e.EndSampleTime.Value + TIME_EPSILON));
                if (activeEvent != null)
                {
                    if (!activeEvent.EndSampleTime.HasValue || sampleTime >= activeEvent.EndSampleTime.Value - TIME_EPSILON)
                    {
                        currentValue = activeEvent.TargetValue;
                    }
                    else
                    {
                        double progress = (sampleTime - activeEvent.SampleTime) / (activeEvent.EndSampleTime.Value - activeEvent.SampleTime);
                        currentValue = activeEvent.IsExponential
                            ? InterpolateExponential(activeEvent.Value, activeEvent.TargetValue, progress)
                            : InterpolateLinear(activeEvent.Value, activeEvent.TargetValue, progress);
                    }
                }
            }

            return Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
        }

        private double CalculateValueAtTime(ScheduleEvent scheduleEvent, double sampleTime)
        {
            if (!scheduleEvent.EndSampleTime.HasValue || scheduleEvent.EndSampleTime.Value <= scheduleEvent.SampleTime + TIME_EPSILON)
            {
                return scheduleEvent.Value;
            }

            double progress = (sampleTime - scheduleEvent.SampleTime) / (scheduleEvent.EndSampleTime.Value - scheduleEvent.SampleTime);
            progress = Math.Clamp(progress, 0, 1);

            return scheduleEvent.IsExponential
                ? InterpolateExponential(scheduleEvent.Value, scheduleEvent.TargetValue, progress)
                : InterpolateLinear(scheduleEvent.Value, scheduleEvent.TargetValue, progress);
        }

        public void Process()
        {
            foreach (var node in _nodeParameterBuffers.Keys)
            {
                if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                    continue;

                nodeLock.EnterReadLock();
                try
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        var buffer = _nodeParameterBuffers[node][param];
                        var events = _nodeEventDictionary[node][param];
                        double currentValue = _nodeLastScheduledValues[node][param];

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double sampleTime = _currentSample + i;

                            while (events.Count > 0 && sampleTime >= events.Min.SampleTime - TIME_EPSILON)
                            {
                                var activeEvent = events.Min;

                                if (activeEvent.IsSetValueAtTime)
                                {
                                    currentValue = activeEvent.Value;
                                    events.Remove(activeEvent);
                                    _eventPool.Return(activeEvent);
                                }
                                else if (!activeEvent.EndSampleTime.HasValue || sampleTime >= activeEvent.EndSampleTime.Value - TIME_EPSILON)
                                {
                                    currentValue = activeEvent.TargetValue;
                                    events.Remove(activeEvent);
                                    _eventPool.Return(activeEvent);
                                }
                                else
                                {
                                    double progress = (sampleTime - activeEvent.SampleTime) / (activeEvent.EndSampleTime.Value - activeEvent.SampleTime);
                                    currentValue = activeEvent.IsExponential
                                        ? InterpolateExponential(activeEvent.Value, activeEvent.TargetValue, progress)
                                        : InterpolateLinear(activeEvent.Value, activeEvent.TargetValue, progress);
                                    break;
                                }
                            }

                            buffer[i] = Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
                        }

                        _nodeLastScheduledValues[node][param] = currentValue;
                    }
                }
                finally
                {
                    nodeLock.ExitReadLock();
                }
            }

            _currentSample += _bufferSize;
        }

        private static double InterpolateLinear(double start, double end, double progress)
        {
            return start + progress * (end - start);
        }

        private static double InterpolateExponential(double start, double end, double progress)
        {
            start = Math.Max(start, VALUE_EPSILON);
            end = Math.Max(end, VALUE_EPSILON);

            if (Math.Abs(start - end) < VALUE_EPSILON)
            {
                return start;
            }

            return start * Math.Pow(end / start, progress);
        }

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            nodeLock.EnterReadLock();
            try
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            finally
            {
                nodeLock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _globalLock.EnterWriteLock();
            try
            {
                foreach (var node in _nodeParameterBuffers.Keys)
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        var currentValue = _nodeLastScheduledValues[node][param];
                        Array.Fill(_nodeParameterBuffers[node][param], currentValue);
                        _nodeEventDictionary[node][param].Clear();
                    }
                }
            }
            finally
            {
                _globalLock.ExitWriteLock();
            }
        }

        public void RemoveNode(AudioNode node)
        {
            _globalLock.EnterWriteLock();
            try
            {
                _nodeParameterBuffers.Remove(node);
                _nodeEventDictionary.Remove(node);
                _nodeLastScheduledValues.Remove(node);
                if (_nodeLocks.TryRemove(node, out var nodeLock))
                {
                    nodeLock.Dispose();
                }
            }
            finally
            {
                _globalLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _globalLock.Dispose();
            foreach (var nodeLock in _nodeLocks.Values)
            {
                nodeLock.Dispose();
            }
        }

        private class ScheduleEventComparer : IComparer<ScheduleEvent>
        {
            public int Compare(ScheduleEvent x, ScheduleEvent y)
            {
                int timeComparison = x.SampleTime.CompareTo(y.SampleTime);
                if (timeComparison != 0) return timeComparison;

                if (x.IsSetValueAtTime && !y.IsSetValueAtTime) return -1;
                if (!x.IsSetValueAtTime && y.IsSetValueAtTime) return 1;

                return 0;
            }
        }

        private class ScheduleEvent
        {
            public double SampleTime { get; set; }
            public double Value { get; set; }
            public double? EndSampleTime { get; set; }
            public double TargetValue { get; set; }
            public bool IsExponential { get; set; }
            public bool IsSetValueAtTime { get; set; }

            public ScheduleEvent(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false, bool isSetValueAtTime = false)
            {
                Reset(sampleTime, value, endSampleTime, targetValue, isExponential, isSetValueAtTime);
            }

            public void Reset(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false, bool isSetValueAtTime = false)
            {
                SampleTime = sampleTime;
                Value = value;
                IsExponential = isExponential;
                IsSetValueAtTime = isSetValueAtTime;

                if (isSetValueAtTime)
                {
                    EndSampleTime = null;
                    TargetValue = value;
                }
                else if (endSampleTime.HasValue && endSampleTime.Value > sampleTime + TIME_EPSILON)
                {
                    EndSampleTime = endSampleTime;
                    TargetValue = targetValue;
                }
                else
                {
                    // Convert to instant value change if duration is too short
                    EndSampleTime = null;
                    TargetValue = targetValue;
                }
            }
        }

        private class ScheduleEventPool
        {
            private readonly ConcurrentBag<ScheduleEvent> _pool = new();

            public ScheduleEvent Get(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false, bool isSetValueAtTime = false)
            {
                if (_pool.TryTake(out var ev))
                {
                    ev.Reset(sampleTime, value, endSampleTime, targetValue, isExponential, isSetValueAtTime);
                    return ev;
                }
                return new ScheduleEvent(sampleTime, value, endSampleTime, targetValue, isExponential, isSetValueAtTime);
            }

            public void Return(ScheduleEvent ev)
            {
                _pool.Add(ev);
            }
        }
    }

    public class ParameterSchedulerException : Exception
    {
        public ParameterSchedulerException(string message) : base(message) { }
        public ParameterSchedulerException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidParameterValueException : ParameterSchedulerException
    {
        public InvalidParameterValueException(string paramName, double value)
            : base($"Invalid value '{value}' for parameter '{paramName}'.") { }
    }
}
