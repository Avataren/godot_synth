using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, SortedSet<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private readonly ScheduleEventPool _eventPool = new ScheduleEventPool();
        private double _currentSample = 0;
        private readonly ReaderWriterLockSlim _globalLock = new();
        private readonly ConcurrentDictionary<AudioNode, ReaderWriterLockSlim> _nodeLocks = new();

        public double CurrentTimeInSeconds => _currentSample / _sampleRate;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
            _sampleRate = sampleRate;
        }

        public void SetCurrentTimeInSeconds(double timeInSeconds)
        {
            Interlocked.Exchange(ref _currentSample, (long)(timeInSeconds * _sampleRate));
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _globalLock.EnterWriteLock();
            try
            {
                if (!_nodeParameterBuffers.ContainsKey(node))
                {
                    var paramBuffers = new ConcurrentDictionary<AudioParam, double[]>();
                    var eventDictionary = new ConcurrentDictionary<AudioParam, SortedSet<ScheduleEvent>>();
                    var lastScheduledValues = new ConcurrentDictionary<AudioParam, double>();

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

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            _nodeLocks[node].EnterWriteLock();
            try
            {
                double sampleTime = timeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];
                var newEvent = _eventPool.Get(sampleTime, value, isSetValueAtTime: true);
                events.Add(newEvent);

                if (sampleTime <= _currentSample)
                {
                    _nodeLastScheduledValues[node][param] = value;
                }
            }
            finally
            {
                _nodeLocks[node].ExitWriteLock();
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0)
            {
                throw new InvalidParameterValueException(nameof(targetValue), targetValue);
            }

            _nodeLocks[node].EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    GD.Print($"Warning: Attempted to create zero-duration exponential ramp. Node: {node}, Param: {param}, Start: {startSampleTime}, End: {endSampleTime}, Current: {currentValue}, Target: {targetValue}");
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                TruncateOngoingEvent(events, startSampleTime);

                events.Add(_eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, true));

                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                _nodeLocks[node].ExitWriteLock();
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            _nodeLocks[node].EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    GD.Print($"Warning: Attempted to create zero-duration linear ramp. Node: {node}, Param: {param}, Start: {startSampleTime}, End: {endSampleTime}, Current: {currentValue}, Target: {targetValue}");
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                TruncateOngoingEvent(events, startSampleTime);

                events.Add(_eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, false));

                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                _nodeLocks[node].ExitWriteLock();
            }
        }

        private void TruncateOngoingEvent(SortedSet<ScheduleEvent> events, double sampleTime)
        {
            if (events.Count > 0)
            {
                var lastEvent = events.Max;

                if (lastEvent.EndSampleTime == null)
                {
                    return;
                }

                if (sampleTime <= lastEvent.SampleTime)
                {
                    if (Math.Abs(sampleTime - lastEvent.SampleTime) < TIME_EPSILON && Math.Abs(lastEvent.EndSampleTime.Value - lastEvent.SampleTime) < TIME_EPSILON)
                    {
                        return;
                    }

                    GD.PrintErr($"Attempted to truncate event to zero or invalid duration: SampleTime = {sampleTime}, StartSampleTime = {lastEvent.SampleTime}, EndSampleTime = {lastEvent.EndSampleTime}");
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
                var activeEvent = events.Min;
                if (sampleTime >= activeEvent.SampleTime && sampleTime <= activeEvent.EndSampleTime)
                {
                    double progress = (sampleTime - activeEvent.SampleTime) / (activeEvent.EndSampleTime.Value - activeEvent.SampleTime);
                    progress = Math.Clamp(progress, 0, 1);
                    currentValue = activeEvent.IsExponential
                        ? InterpolateExponential(activeEvent.Value, activeEvent.TargetValue, progress)
                        : InterpolateLinear(activeEvent.Value, activeEvent.TargetValue, progress);
                }
            }

            return Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
        }

        private double CalculateValueAtTime(ScheduleEvent scheduleEvent, double sampleTime)
        {
            if (scheduleEvent.EndSampleTime == null || scheduleEvent.EndSampleTime <= scheduleEvent.SampleTime)
            {
                return scheduleEvent.Value;
            }

            double progress = (sampleTime - scheduleEvent.SampleTime) / (scheduleEvent.EndSampleTime.Value - scheduleEvent.SampleTime);

            return scheduleEvent.IsExponential
                ? InterpolateExponential(scheduleEvent.Value, scheduleEvent.TargetValue, progress)
                : InterpolateLinear(scheduleEvent.Value, scheduleEvent.TargetValue, progress);
        }

        public void Process()
        {
            foreach (var (node, paramBuffers) in _nodeParameterBuffers)
            {
                _nodeLocks[node].EnterReadLock();
                try
                {
                    foreach (var (param, buffer) in paramBuffers)
                    {
                        var events = _nodeEventDictionary[node][param];
                        double currentValue = CalculateCurrentValue(node, param);

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double sampleTime = _currentSample + i;

                            while (events.Count > 0 && sampleTime >= events.Min.SampleTime)
                            {
                                var activeEvent = events.Min;

                                if (activeEvent.IsSetValueAtTime)
                                {
                                    currentValue = activeEvent.Value;
                                    events.Remove(activeEvent);
                                    _eventPool.Return(activeEvent);
                                }
                                else if (activeEvent.EndSampleTime == null || sampleTime >= activeEvent.EndSampleTime)
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
                    _nodeLocks[node].ExitReadLock();
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
            _nodeLocks[node].EnterReadLock();
            try
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            finally
            {
                _nodeLocks[node].ExitReadLock();
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
                _nodeParameterBuffers.TryRemove(node, out _);
                _nodeEventDictionary.TryRemove(node, out _);
                _nodeLastScheduledValues.TryRemove(node, out _);
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


        public class ScheduleEvent
        {
            private const double TIME_EPSILON = 1e-10;
            private const double VALUE_EPSILON = 1e-6;

            public double SampleTime { get; set; }
            public double Value { get; set; }
            public double? EndSampleTime { get; set; }
            public double TargetValue { get; set; }
            public bool IsExponential { get; set; }
            public bool IsSetValueAtTime { get; set; }

            public ScheduleEvent(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false, bool isSetValueAtTime = false)
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
                    if (Math.Abs(targetValue - value) > VALUE_EPSILON)
                    {
                        GD.Print($"Warning: Very short duration ramp converted to instant change. SampleTime: {sampleTime}, Value: {value}, TargetValue: {targetValue}, EndSampleTime: {endSampleTime}");
                    }
                }
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
                    EndSampleTime = null;
                    TargetValue = targetValue;
                    if (Math.Abs(targetValue - value) > VALUE_EPSILON)
                    {
                        GD.Print($"Warning: Very short duration ramp converted to instant change. SampleTime: {sampleTime}, Value: {value}, TargetValue: {targetValue}, EndSampleTime: {endSampleTime}");
                    }
                }
            }
        }

        public class ScheduleEventPool
        {
            private readonly ConcurrentBag<ScheduleEvent> _pool = new ConcurrentBag<ScheduleEvent>();

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
            : base($"Invalid value {value} for parameter {paramName}") { }
    }

    public class ScheduleEventException : ParameterSchedulerException
    {
        public ScheduleEventException(string message) : base(message) { }
    }
}