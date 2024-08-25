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
        private const double VALUE_EPSILON = 1e-6;
        private const double MIN_EXPONENTIAL_VALUE = 1e-6;
        private const double EPSILON = 1e-10;

        private readonly int _bufferSize;
        private readonly double _sampleRate;

        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, List<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly ConcurrentDictionary<AudioNode, ConcurrentDictionary<AudioParam, double>> _nodeLastScheduledValues = new();

        private double _currentSample = 0;
        private readonly ReaderWriterLockSlim _lock = new();

        public double CurrentTimeInSeconds
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _currentSample / _sampleRate;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
            _sampleRate = sampleRate;
        }

        public void SetCurrentTimeInSeconds(double timeInSeconds)
        {
            _lock.EnterWriteLock();
            try
            {
                _currentSample = timeInSeconds * _sampleRate;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_nodeParameterBuffers.ContainsKey(node))
                {
                    var paramBuffers = new ConcurrentDictionary<AudioParam, double[]>();
                    var eventDictionary = new ConcurrentDictionary<AudioParam, List<ScheduleEvent>>();
                    var lastScheduledValues = new ConcurrentDictionary<AudioParam, double>();

                    foreach (var param in parameters)
                    {
                        paramBuffers[param] = new double[_bufferSize];
                        eventDictionary[param] = new List<ScheduleEvent>();
                        lastScheduledValues[param] = 0.0;
                    }

                    _nodeParameterBuffers[node] = paramBuffers;
                    _nodeEventDictionary[node] = eventDictionary;
                    _nodeLastScheduledValues[node] = lastScheduledValues;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            _lock.EnterWriteLock();
            try
            {
                double sampleTime = timeInSeconds * _sampleRate;

                var events = _nodeEventDictionary[node][param];

                var newEvent = new ScheduleEvent(sampleTime, value);
                events.Add(newEvent);
                events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));

                if (sampleTime <= _currentSample)
                {
                    _nodeLastScheduledValues[node][param] = value;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0)
            {
                throw new InvalidParameterValueException(nameof(targetValue), targetValue);
            }

            _lock.EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                TruncateOngoingEvent(events, startSampleTime);

                events.Add(new ScheduleEvent(startSampleTime, currentValue, endSampleTime, targetValue, true));
                events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));

                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            _lock.EnterWriteLock();
            try
            {
                double startSampleTime = _currentSample;
                double endSampleTime = endTimeInSeconds * _sampleRate;
                var events = _nodeEventDictionary[node][param];

                double currentValue = CalculateCurrentValue(node, param);

                TruncateOngoingEvent(events, startSampleTime);

                events.Add(new ScheduleEvent(startSampleTime, currentValue, endSampleTime, targetValue, false));
                events.Sort((a, b) => a.SampleTime.CompareTo(b.SampleTime));

                _nodeLastScheduledValues[node][param] = currentValue;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void TruncateOngoingEvent(List<ScheduleEvent> events, double sampleTime)
        {
            if (events.Count > 0)
            {
                var lastEvent = events.Last();

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

                lastEvent.EndSampleTime = sampleTime;
                lastEvent.TargetValue = truncatedValue;
            }
        }

        private double CalculateCurrentValue(AudioNode node, AudioParam param)
        {
            var events = _nodeEventDictionary[node][param];
            double currentValue = _nodeLastScheduledValues[node][param];
            double sampleTime = _currentSample;

            if (events.Count > 0)
            {
                ScheduleEvent activeEvent;
                lock (events)
                {
                    if (events.Count > 0)
                    {
                        activeEvent = events[0];
                    }
                    else
                    {
                        return Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
                    }
                }

                if (sampleTime >= activeEvent.SampleTime && sampleTime <= activeEvent.EndSampleTime)
                {
                    double progress = (sampleTime - activeEvent.SampleTime) / (activeEvent.EndSampleTime.Value - activeEvent.SampleTime);
                    progress = Math.Max(0, Math.Min(1, progress));
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
            _lock.EnterWriteLock();
            try
            {
                foreach (var (node, paramBuffers) in _nodeParameterBuffers)
                {
                    foreach (var (param, buffer) in paramBuffers)
                    {
                        var events = _nodeEventDictionary[node][param];
                        double currentValue = CalculateCurrentValue(node, param);

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double sampleTime = _currentSample + i;

                            lock (events)
                            {
                                while (events.Count > 0 && sampleTime >= events[0].SampleTime)
                                {
                                    var activeEvent = events[0];

                                    if (activeEvent.EndSampleTime == null || Math.Abs(activeEvent.SampleTime - activeEvent.EndSampleTime.Value) < TIME_EPSILON)
                                    {
                                        currentValue = activeEvent.Value;
                                        events.RemoveAt(0);
                                    }
                                    else if (sampleTime >= activeEvent.EndSampleTime)
                                    {
                                        currentValue = activeEvent.TargetValue;
                                        events.RemoveAt(0);
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
                            }

                            buffer[i] = Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
                        }

                        _nodeLastScheduledValues[node][param] = currentValue;
                    }
                }

                _currentSample += _bufferSize;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private static double InterpolateLinear(double start, double end, double progress)
        {
            progress = Math.Max(0, Math.Min(1, progress));
            double result = start + progress * (end - start);
            return double.IsNaN(result) ? start : result;
        }

        private static double InterpolateExponential(double start, double end, double progress)
        {
            progress = Math.Max(0, Math.Min(1, progress));

            start = Math.Max(start, VALUE_EPSILON);
            end = Math.Max(end, VALUE_EPSILON);

            if (Math.Abs(start - end) < VALUE_EPSILON)
            {
                return start;
            }

            double result = start * Math.Pow(end / start, progress);
            return double.IsNaN(result) ? start : result;
        }

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            _lock.EnterReadLock();
            try
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
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
                _lock.ExitWriteLock();
            }
        }

        public void RemoveNode(AudioNode node)
        {
            _lock.EnterWriteLock();
            try
            {
                _nodeParameterBuffers.TryRemove(node, out _);
                _nodeEventDictionary.TryRemove(node, out _);
                _nodeLastScheduledValues.TryRemove(node, out _);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }


        public class ScheduleEvent
        {
            public double SampleTime { get; }
            public double Value { get; }
            public double? EndSampleTime { get; set; }
            public double TargetValue { get; set; }
            public bool IsExponential { get; }

            public ScheduleEvent(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false)
            {
                SampleTime = sampleTime;
                Value = value;
                EndSampleTime = endSampleTime;
                TargetValue = targetValue;
                IsExponential = isExponential;

                if (endSampleTime.HasValue && Math.Abs(SampleTime - endSampleTime.Value) < TIME_EPSILON)
                {
                    if (IsExponential || Math.Abs(TargetValue - value) > VALUE_EPSILON)
                    {
                        GD.PrintErr($"Creating zero-duration ramp event: StartSampleTime ({SampleTime}), EndSampleTime ({EndSampleTime})");
                    }
                }
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