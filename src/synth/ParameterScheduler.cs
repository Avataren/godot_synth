using System;
using System.Collections.Generic;
using System.Linq;
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
            Console.WriteLine($"Set current time to: {timeInSeconds}");
            _currentTimeInSeconds = timeInSeconds;
        }

        public void RegisterNode(AudioNode node, List<AudioParam> parameters)
        {
            _lock.EnterWriteLock();
            try
            {
                Console.WriteLine($"Registering node: {node}, with parameters: {parameters.Count}");
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

        public void ScheduleValueAtTime(AudioNode node, AudioParam param, double value, double timeInSeconds, double? initialValue = null)
        {
            _lock.EnterWriteLock();
            try
            {
                Console.WriteLine($"Scheduling value at time: {timeInSeconds}, value: {value}, initial: {initialValue}");
                var events = _nodeEventDictionary[node][param];
                int index = events.BinarySearch(new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue));
                if (index < 0) index = ~index;
                events.Insert(index, new ScheduleEvent(timeInSeconds, value, null, 0.0, initialValue));
                _nodeHasRemainingEvents[node][param] = true;
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
                Console.WriteLine($"Scheduling linear ramp: Node={node}, Param={param}, TargetValue={targetValue}, EndTime={endTimeInSeconds}");
                double startTimeInSeconds = _currentTimeInSeconds;
                var events = _nodeEventDictionary[node][param];
                events.RemoveAll(e => e.Time >= startTimeInSeconds - TIME_EPSILON);

                double startValue = CalculateStartValue(events, startTimeInSeconds, _nodeLastScheduledValues[node][param]);

                events.Add(new ScheduleEvent(startTimeInSeconds, startValue, endTimeInSeconds, targetValue, startValue, isExponential: false));
                _nodeHasRemainingEvents[node][param] = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            _lock.EnterWriteLock();
            try
            {
                Console.WriteLine($"Scheduling exponential ramp: Node={node}, Param={param}, TargetValue={targetValue}, EndTime={endTimeInSeconds}");
                if (targetValue < MIN_EXPONENTIAL_VALUE)
                {
                    throw new ArgumentException($"Exponential ramps require the target value to be at least {MIN_EXPONENTIAL_VALUE}.");
                }
                if (endTimeInSeconds <= _currentTimeInSeconds)
                {
                    throw new ArgumentException("End time must be in the future.");
                }

                double startTimeInSeconds = _currentTimeInSeconds;
                var events = _nodeEventDictionary[node][param];
                events.RemoveAll(e => e.Time >= startTimeInSeconds - TIME_EPSILON);

                double startValue = CalculateStartValue(events, startTimeInSeconds, _nodeLastScheduledValues[node][param]);

                events.Add(new ScheduleEvent(startTimeInSeconds, startValue, endTimeInSeconds, targetValue, startValue, isExponential: true));
                _nodeHasRemainingEvents[node][param] = true;
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
                progress = Math.Clamp(progress, 0.0, 1.0); // Using Math.Clamp for clarity

                double startValue = lastEvent.InitialValue ?? lastEvent.Value;

                if (lastEvent.IsExponential)
                {
                    return SafeExponentialInterpolation(startValue, lastEvent.TargetValue, progress);
                }
                else
                {
                    return startValue + progress * (lastEvent.TargetValue - startValue);
                }
            }
            return Math.Max(MIN_EXPONENTIAL_VALUE, currentValue);
        }

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

        public double GetValueAtSample(AudioNode node, AudioParam param, int sampleIndex)
        {
            if (sampleIndex < _bufferSize)
            {
                return _nodeParameterBuffers[node][param][sampleIndex];
            }
            Console.WriteLine($"Requested sample index {sampleIndex} out of bounds.");
            return 0.0;
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
                        double lastScheduledValue = _nodeLastScheduledValues[node][param];

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double timeAtSample = _currentTimeInSeconds + (i * increment);
                            bool isEventProcessed = false;

                            if (events.Count > 0)
                            {
                                var evt = events[0];
                                if (timeAtSample >= evt.Time)
                                {
                                    if (evt.EndTime.HasValue)
                                    {
                                        if (timeAtSample <= evt.EndTime.Value)
                                        {
                                            double duration = evt.EndTime.Value - evt.Time;
                                            double progress = (timeAtSample - evt.Time) / duration;
                                            lastScheduledValue = evt.Value + progress * (evt.TargetValue - evt.Value);
                                        }
                                        else if (timeAtSample > evt.EndTime.Value)
                                        {
                                            // Processed past the end time of the event.
                                            lastScheduledValue = evt.TargetValue;
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
                                            isEventProcessed = true;
                                        }
                                    }
                                    else
                                    {
                                        // For events without an end time, simply set the value.
                                        lastScheduledValue = evt.Value;
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
                                        isEventProcessed = true;
                                    }
                                }
                            }

                            buffer[i] = lastScheduledValue;
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


        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                Console.WriteLine("Clearing all scheduling data.");
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
                Console.WriteLine("All data cleared.");
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
        public double? InitialValue { get; set; }
        public bool IsExponential { get; }

        public ScheduleEvent(double time, double value, double? endTime = null, double targetValue = 0.0, double? initialValue = null, bool isExponential = false)
        {
            Time = time;
            Value = value;
            EndTime = endTime;
            TargetValue = targetValue;
            InitialValue = initialValue;
            IsExponential = isExponential;
        }

        public int CompareTo(ScheduleEvent other)
        {
            return Time.CompareTo(other.Time);
        }
    }
}
