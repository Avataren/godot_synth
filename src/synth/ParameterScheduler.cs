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
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly int _bufferSize;
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, List<ScheduleEvent>>> _nodeEventDictionary = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, bool>> _nodeHasRemainingEvents = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private double _currentTimeInSeconds = 0.0;
        private int _processedEventCount = 0;

        public double CurrentTimeInSeconds => _currentTimeInSeconds;
        public int ProcessedEventCount => _processedEventCount;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
        }

        public void SetCurrentTimeInSeconds(double timeInSeconds)
        {
            _currentTimeInSeconds = timeInSeconds;
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _lock.EnterWriteLock();
            try
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
                        _nodeHasRemainingEvents[node][param] = false;
                        _nodeLastScheduledValues[node][param] = 0.0;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        private double CalculateStartValue(List<ScheduleEvent> events, double startTimeInSeconds, double currentValue)
        {
            if (events.Count > 0 && events[events.Count - 1].EndTime.HasValue &&
                events[events.Count - 1].EndTime.Value > startTimeInSeconds + TIME_EPSILON)
            {
                var lastEvent = events[events.Count - 1];
                double duration = lastEvent.EndTime.Value - lastEvent.Time;
                if (duration <= TIME_EPSILON)
                {
                    return lastEvent.TargetValue;
                }

                double progress = (startTimeInSeconds - lastEvent.Time) / duration;
                progress = Math.Max(0.0, Math.Min(1.0, progress)); // Clamp progress between 0 and 1

                if (lastEvent.IsExponential)
                {
                    return SafeExponentialInterpolation(lastEvent.InitialValue, lastEvent.TargetValue, progress);
                }
                else
                {
                    return lastEvent.InitialValue + progress * (lastEvent.TargetValue - lastEvent.InitialValue);
                }
            }
            return Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
        }
        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds, double? initialValue = null)
        {
            lock (_lock)
            {
                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue));
                _nodeHasRemainingEvents[node][param] = true;
            }
        }

        // public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double startTimeInSeconds, double endTimeInSeconds, double? initialValue = null)
        // {
        //     lock (_lock)
        //     {
        //         var events = _nodeEventDictionary[node][param];
        //         int index = events.BinarySearch(new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds, targetValue, initialValue));
        //         if (index < 0) index = ~index;
        //         events.Insert(index, new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds, targetValue, initialValue));
        //         _nodeHasRemainingEvents[node][param] = true;
        //     }
        // }

        private double SafeExponentialInterpolation(double start, double end, double progress)
        {
            if (start <= MIN_EXPONENTIAL_VALUE || end <= MIN_EXPONENTIAL_VALUE)
            {
                // Fallback to linear interpolation if values are too small
                return start + progress * (end - start);
            }

            double ratio = end / start;
            if (Math.Abs(ratio - 1.0) < VALUE_EPSILON)
            {
                // If start and end are very close, use linear interpolation
                return start + progress * (end - start);
            }

            return start * Math.Pow(ratio, progress);
        }
        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue < MIN_EXPONENTIAL_VALUE)
            {
                throw new ArgumentException($"Exponential ramps require the target value to be at least {MIN_EXPONENTIAL_VALUE}.");
            }
            if (endTimeInSeconds <= _currentTimeInSeconds)
            {
                throw new ArgumentException("End time must be in the future.");
            }

            _lock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];
                double startTimeInSeconds = _currentTimeInSeconds;

                events.RemoveAll(e => e.Time >= startTimeInSeconds - TIME_EPSILON);

                double startValue = CalculateStartValue(events, startTimeInSeconds, _nodeLastScheduledValues[node][param]);

                if (startValue < MIN_EXPONENTIAL_VALUE)
                {
                    throw new InvalidOperationException($"Exponential ramps require the current value to be at least {MIN_EXPONENTIAL_VALUE}.");
                }

                if (events.Count > 0 && events[events.Count - 1].EndTime.HasValue &&
                    events[events.Count - 1].EndTime.Value > startTimeInSeconds + TIME_EPSILON)
                {
                    events.RemoveAt(events.Count - 1);
                }

                events.Add(new ScheduleEvent(startTimeInSeconds, startValue, endTimeInSeconds, targetValue, startValue, isExponential: true));
                _nodeHasRemainingEvents[node][param] = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (endTimeInSeconds <= _currentTimeInSeconds)
            {
                //throw new ArgumentException("End time must be in the future.");
                //set value at time instead
                ScheduleValueAtTime(node, param, targetValue, _currentTimeInSeconds);
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                var events = _nodeEventDictionary[node][param];
                double startTimeInSeconds = _currentTimeInSeconds;

                events.RemoveAll(e => e.Time >= startTimeInSeconds - TIME_EPSILON);

                double startValue = CalculateStartValue(events, startTimeInSeconds, _nodeLastScheduledValues[node][param]);

                if (events.Count > 0 && events[events.Count - 1].EndTime.HasValue &&
                    events[events.Count - 1].EndTime.Value > startTimeInSeconds + TIME_EPSILON)
                {
                    events.RemoveAt(events.Count - 1);
                }

                events.Add(new ScheduleEvent(startTimeInSeconds, startValue, endTimeInSeconds, targetValue, startValue, isExponential: false));
                _nodeHasRemainingEvents[node][param] = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        public void Process(double increment)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                foreach (var node in _nodeEventDictionary.Keys)
                {
                    foreach (var param in _nodeEventDictionary[node].Keys)
                    {
                        var buffer = _nodeParameterBuffers[node][param];
                        var events = _nodeEventDictionary[node][param];
                        double lastScheduledValue = Math.Max(MIN_EXPONENTIAL_VALUE, _nodeLastScheduledValues[node][param]);

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double timeAtSample = _currentTimeInSeconds + (i * increment);

                            while (events.Count > 0 && timeAtSample >= events[0].Time - TIME_EPSILON)
                            {
                                var evt = events[0];

                                if (evt.EndTime.HasValue && timeAtSample <= evt.EndTime.Value + TIME_EPSILON)
                                {
                                    double duration = evt.EndTime.Value - evt.Time;
                                    if (duration <= TIME_EPSILON)
                                    {
                                        lastScheduledValue = evt.TargetValue;
                                    }
                                    else
                                    {
                                        double progress = (timeAtSample - evt.Time) / duration;
                                        progress = Math.Max(0.0, Math.Min(1.0, progress)); // Clamp progress between 0 and 1

                                        if (evt.IsExponential)
                                        {
                                            lastScheduledValue = SafeExponentialInterpolation(evt.InitialValue, evt.TargetValue, progress);
                                        }
                                        else
                                        {
                                            lastScheduledValue = evt.InitialValue + progress * (evt.TargetValue - evt.InitialValue);
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    lastScheduledValue = evt.EndTime.HasValue ? evt.TargetValue : evt.Value;
                                    _lock.EnterWriteLock();
                                    try
                                    {
                                        events.RemoveAt(0);
                                        _processedEventCount++;
                                    }
                                    finally
                                    {
                                        _lock.ExitWriteLock();
                                    }
                                }
                            }

                            buffer[i] = Math.Max(MIN_EXPONENTIAL_VALUE, lastScheduledValue);
                        }

                        _nodeLastScheduledValues[node][param] = lastScheduledValue;
                        _nodeHasRemainingEvents[node][param] = events.Count > 0;
                    }
                }

                _currentTimeInSeconds += increment * _bufferSize;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        // public void Process(double increment)
        // {
        //     lock (_lock)
        //     {

        //         foreach (var node in _nodeEventDictionary.Keys)
        //         {
        //             foreach (var param in _nodeEventDictionary[node].Keys)
        //             {
        //                 var buffer = _nodeParameterBuffers[node][param];
        //                 // Skip processing if no events are remaining
        //                 if (!_nodeHasRemainingEvents[node][param])
        //                 {
        //                     Array.Fill(buffer, _nodeLastScheduledValues[node][param]);
        //                     continue;
        //                 }

        //                 var events = _nodeEventDictionary[node][param];
        //                 double lastScheduledValue = _nodeLastScheduledValues[node][param];

        //                 for (int i = 0; i < _bufferSize; i++)
        //                 {
        //                     double timeAtSample = _currentTimeInSeconds + (i * increment);

        //                     if (events.Count > 0)
        //                     {
        //                         var evt = events[0];

        //                         if (timeAtSample >= evt.Time)
        //                         {
        //                             if (evt.InitialValue.HasValue)
        //                             {
        //                                 lastScheduledValue = evt.InitialValue.Value;
        //                                 evt.InitialValue = null;  // Ensure the initial value is only applied once
        //                             }
        //                             else
        //                             {
        //                                 lastScheduledValue = evt.Value;
        //                                 events.RemoveAt(0);  // Remove the event once processed
        //                                 _processedEventCount++;
        //                             }
        //                         }
        //                     }

        //                     buffer[i] = lastScheduledValue;  // Always use the lastScheduledValue to fill the buffer
        //                 }
        //                 // Update the last scheduled value for future use
        //                 _nodeLastScheduledValues[node][param] = lastScheduledValue;

        //                 // Determine if more events remain to be processed
        //                 _nodeHasRemainingEvents[node][param] = events.Count > 0;
        //             }
        //         }
        //         _currentTimeInSeconds += increment * _bufferSize;
        //     }
        // }



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
            _lock.EnterWriteLock();
            try
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

                _processedEventCount = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public class ScheduleEvent : IComparable<ScheduleEvent>
    {
        public double Time { get; }
        public double Value { get; }
        public double? EndTime { get; }
        public double TargetValue { get; }
        public double InitialValue { get; set; }
        public bool IsExponential { get; } // New flag to indicate if the ramp is exponential

        public ScheduleEvent(double time, double value, double? endTime = null, double targetValue = 0.0, double? initialValue = null, bool isExponential = false)
        {
            Time = time;
            Value = value;
            EndTime = endTime;
            TargetValue = targetValue;
            InitialValue = initialValue ?? value;
            IsExponential = isExponential; // Initialize the exponential flag
        }

        public int CompareTo(ScheduleEvent other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}