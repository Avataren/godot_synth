using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace Synth
{
    public class ParameterScheduler
    {
        private const double TIME_EPSILON = 1e-10;
        private const double VALUE_EPSILON = 1e-10;
        private const double MIN_EXPONENTIAL_VALUE = 1e-6;
        private readonly int _bufferSize;
        private readonly double _sampleRate;
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, List<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private double _currentTimeInSeconds = 0.0;

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        public double CurrentTimeInSeconds
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return _currentTimeInSeconds;
                }
                finally
                {
                    _rwLock.ExitReadLock();
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
            _rwLock.EnterWriteLock();
            try
            {
                _currentTimeInSeconds = timeInSeconds;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _rwLock.EnterWriteLock();
            try
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
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds)
        {
            _rwLock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];
                events.Add(new ScheduleEvent(timeInSeconds, value));
                events.Sort((a, b) => a.Time.CompareTo(b.Time));

                // Update the last scheduled value if this event is immediate or in the past
                if (timeInSeconds <= _currentTimeInSeconds)
                {
                    _nodeLastScheduledValues[node][param] = value;
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            _rwLock.EnterWriteLock();
            try
            {
                if (targetValue <= 0)
                {
                    throw new ArgumentException("Target value for exponential ramp must be greater than zero.");
                }

                var events = _nodeEventDictionary[node][param];
                double startValue = _nodeLastScheduledValues[node][param];
                double startTime = _currentTimeInSeconds;

                // If the start value is zero or very close to zero, we need to adjust it
                if (startValue <= MIN_EXPONENTIAL_VALUE)
                {
                    startValue = MIN_EXPONENTIAL_VALUE;
                    // Add an immediate event to set the start value
                    events.Add(new ScheduleEvent(startTime, startValue));
                }

                events.Add(new ScheduleEvent(startTime, startValue, endTimeInSeconds, targetValue, true));
                events.Sort((a, b) => a.Time.CompareTo(b.Time));
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            _rwLock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];
                var startValue = _nodeLastScheduledValues[node][param];
                var startTime = _currentTimeInSeconds;

                events.Add(new ScheduleEvent(startTime, startValue, endTimeInSeconds, targetValue, false));
                events.Sort((a, b) => a.Time.CompareTo(b.Time));
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Process(double increment)
        {
            _rwLock.EnterWriteLock();
            try
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
                            double timeAtSample = _currentTimeInSeconds + (i * increment);

                            ScheduleEvent activeEvent = null;
                            while (events.Count > 0 && timeAtSample >= events[0].Time - TIME_EPSILON)
                            {
                                activeEvent = events[0];
                                if (!activeEvent.EndTime.HasValue || timeAtSample >= activeEvent.EndTime.Value)
                                {
                                    currentValue = activeEvent.EndTime.HasValue ? activeEvent.TargetValue : activeEvent.Value;
                                    events.RemoveAt(0);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (activeEvent != null && activeEvent.EndTime.HasValue && timeAtSample < activeEvent.EndTime.Value)
                            {
                                double duration = activeEvent.EndTime.Value - activeEvent.Time;
                                double progress = (timeAtSample - activeEvent.Time) / duration;
                                progress = Math.Max(0.0, Math.Min(1.0, progress));

                                if (activeEvent.IsExponential)
                                {
                                    double start = Math.Max(MIN_EXPONENTIAL_VALUE, activeEvent.Value);
                                    double end = Math.Max(MIN_EXPONENTIAL_VALUE, activeEvent.TargetValue);
                                    currentValue = start * Math.Pow(end / start, progress);
                                }
                                else
                                {
                                    currentValue = activeEvent.Value + progress * (activeEvent.TargetValue - activeEvent.Value);
                                }
                            }

                            buffer[i] = Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
                        }

                        _nodeLastScheduledValues[node][param] = currentValue;
                    }
                }

                _currentTimeInSeconds += increment * _bufferSize;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            _rwLock.EnterReadLock();
            try
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _rwLock.EnterWriteLock();
            try
            {
                foreach (var node in _nodeParameterBuffers.Keys)
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        Array.Clear(_nodeParameterBuffers[node][param], 0, _bufferSize);
                        _nodeEventDictionary[node][param].Clear();
                        _nodeLastScheduledValues[node][param] = 0.0;
                    }
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
    }

    public class ScheduleEvent
    {
        public double Time { get; }
        public double Value { get; }
        public double? EndTime { get; }
        public double TargetValue { get; }
        public bool IsExponential { get; }

        public ScheduleEvent(double time, double value, double? endTime = null, double targetValue = 0.0, bool isExponential = false)
        {
            Time = time;
            Value = value;
            EndTime = endTime;
            TargetValue = targetValue;
            IsExponential = isExponential;
        }
    }
}