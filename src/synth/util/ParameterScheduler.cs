using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace Synth
{
    public class ParameterScheduler : IDisposable
    {
        private const double TIME_EPSILON = 1e-9;
        private const double MIN_EXPONENTIAL_VALUE = 1e-10;
        private const double VALUE_EPSILON = 1e-8;

        private readonly int _bufferSize;
        private readonly double _sampleRate;

        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double[]>> _nodeParameterBuffers = new();
        private readonly Dictionary<AudioNode, Dictionary<AudioParam, double>> _nodeLastScheduledValues = new();
        private readonly ScheduleEventPool _eventPool = new();
        private double _currentSample = 0;

        public double CurrentSample => _currentSample;
        public int BufferSize => _bufferSize;
        public double SampleRate => _sampleRate;

        private readonly ReaderWriterLockSlim _globalLock = new();
        private readonly ConcurrentDictionary<AudioNode, ReaderWriterLockSlim> _nodeLocks = new();

        public double CurrentTimeInSeconds => _currentSample / _sampleRate;

        // Global event queue
        private readonly SortedSet<GlobalScheduleEvent> _globalEventQueue = new(new GlobalScheduleEventComparer());

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
                    var lastScheduledValues = new Dictionary<AudioParam, double>();

                    foreach (var param in parameters)
                    {
                        paramBuffers[param] = new double[_bufferSize];
                        lastScheduledValues[param] = 0.0;
                    }

                    _nodeParameterBuffers[node] = paramBuffers;
                    _nodeLastScheduledValues[node] = lastScheduledValues;

                    _nodeLocks[node] = new ReaderWriterLockSlim();
                }
            }
            finally
            {
                _globalLock.ExitWriteLock();
            }
        }

        // Internal method that assumes the lock is already held
        private void CancelScheduledValuesInternal(AudioNode node, AudioParam param, double cancelSampleTime)
        {
            // Remove events where Node, Param match and SampleTime >= cancelSampleTime
            _globalEventQueue.RemoveWhere(ev => ev.Node == node && ev.Param == param && ev.SampleTime >= cancelSampleTime);
        }

        public void CancelScheduledValues(AudioNode node, AudioParam param, double cancelTimeInSeconds)
        {
            if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                throw new ParameterSchedulerException($"Node {node} is not registered.");

            double cancelSampleTime = cancelTimeInSeconds * _sampleRate;

            nodeLock.EnterWriteLock();
            try
            {
                CancelScheduledValuesInternal(node, param, cancelSampleTime);
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
                // Cancel future events without acquiring the lock again
                double cancelSampleTime = sampleTime;
                CancelScheduledValuesInternal(node, param, cancelSampleTime);

                var newEvent = _eventPool.Get(sampleTime, value, isSetValueAtTime: true);

                if (newEvent == null)
                {
                    throw new InvalidOperationException("Failed to create ScheduleEvent.");
                }

                var globalEvent = new GlobalScheduleEvent
                {
                    SampleTime = sampleTime,
                    Node = node,
                    Param = param,
                    Event = newEvent
                };

                if (globalEvent.Event == null)
                {
                    throw new InvalidOperationException("GlobalScheduleEvent has a null Event.");
                }

                _globalEventQueue.Add(globalEvent);
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

                // Cancel future events starting from current time
                double cancelSampleTime = startSampleTime;
                CancelScheduledValuesInternal(node, param, cancelSampleTime);

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                var scheduleEvent = _eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, isExponential: false);

                var globalEvent = new GlobalScheduleEvent
                {
                    SampleTime = startSampleTime,
                    Node = node,
                    Param = param,
                    Event = scheduleEvent
                };

                _globalEventQueue.Add(globalEvent);
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0 || CalculateCurrentValue(node, param) <= 0)
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

                // Cancel future events starting from current time
                double cancelSampleTime = startSampleTime;
                CancelScheduledValuesInternal(node, param, cancelSampleTime);

                double currentValue = CalculateCurrentValue(node, param);

                if (endSampleTime <= startSampleTime + TIME_EPSILON)
                {
                    ScheduleValueAtTime(node, param, targetValue, endTimeInSeconds);
                    return;
                }

                var scheduleEvent = _eventPool.Get(startSampleTime, currentValue, endSampleTime, targetValue, isExponential: true);

                var globalEvent = new GlobalScheduleEvent
                {
                    SampleTime = startSampleTime,
                    Node = node,
                    Param = param,
                    Event = scheduleEvent
                };

                _globalEventQueue.Add(globalEvent);
            }
            finally
            {
                nodeLock.ExitWriteLock();
            }
        }

        private double CalculateCurrentValue(AudioNode node, AudioParam param)
        {
            if (_nodeLastScheduledValues.TryGetValue(node, out var paramValues) &&
                paramValues.TryGetValue(param, out var lastValue))
            {
                return lastValue;
            }
            return VALUE_EPSILON; // Use a small value to avoid issues with exponential ramps
        }

        public void Process()
        {
            double processingEndSample = _currentSample + _bufferSize;

            // Initialize parameter buffers for all nodes and parameters
            foreach (var node in _nodeParameterBuffers.Keys)
            {
                if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                    continue;

                nodeLock.EnterWriteLock();
                try
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        var buffer = _nodeParameterBuffers[node][param];
                        // Initialize buffer with the last known value
                        double lastValue = _nodeLastScheduledValues[node][param];
                        Array.Fill(buffer, lastValue);
                    }
                }
                finally
                {
                    nodeLock.ExitWriteLock();
                }
            }

            // Process all events up to the end of the current buffer
            while (_globalEventQueue.Count > 0 && _globalEventQueue.Min.SampleTime <= processingEndSample)
            {
                var globalEvent = _globalEventQueue.Min;

                if (globalEvent == null)
                {
                    GD.PrintErr("Encountered a null GlobalScheduleEvent in the queue.");
                    _globalEventQueue.Remove(globalEvent); // Remove the null event
                    continue;
                }

                _globalEventQueue.Remove(globalEvent);

                var node = globalEvent.Node;
                var param = globalEvent.Param;
                var scheduleEvent = globalEvent.Event;

                if (scheduleEvent == null)
                {
                    GD.PrintErr("GlobalScheduleEvent contains a null ScheduleEvent for Node: {Node}, Param: {Param}, SampleTime: {SampleTime}", node, param, globalEvent.SampleTime);
                    continue;
                }

                if (!_nodeLocks.TryGetValue(node, out var nodeLock))
                    continue;

                nodeLock.EnterWriteLock();
                try
                {
                    if (!_nodeParameterBuffers[node].ContainsKey(param))
                    {
                        _nodeParameterBuffers[node][param] = new double[_bufferSize];
                        // Initialize buffer with last value
                        double lastValue = _nodeLastScheduledValues[node][param];
                        Array.Fill(_nodeParameterBuffers[node][param], lastValue);
                    }

                    var buffer = _nodeParameterBuffers[node][param];

                    // Calculate the sample index within the current buffer
                    int sampleIndex = (int)(globalEvent.SampleTime - _currentSample);

                    if (sampleIndex >= 0 && sampleIndex < _bufferSize)
                    {
                        // Apply the event to the parameter buffer at the correct sample index
                        bool eventCompleted = ApplyEventToBuffer(buffer, scheduleEvent, sampleIndex);

                        if (!eventCompleted)
                        {
                            // Event is not complete, update for next buffer
                            globalEvent.SampleTime = _currentSample + _bufferSize;
                            scheduleEvent.SampleTime = globalEvent.SampleTime;
                            scheduleEvent.Value = buffer[_bufferSize - 1]; // Last value in buffer
                            _globalEventQueue.Add(globalEvent);
                        }
                        else
                        {
                            // Event is complete
                            _nodeLastScheduledValues[node][param] = scheduleEvent.TargetValue;
                            _eventPool.Return(scheduleEvent);
                        }
                    }
                    else if (sampleIndex < 0)
                    {
                        // Event time is in the past; apply immediately at the start of the buffer
                        bool eventCompleted = ApplyEventToBuffer(buffer, scheduleEvent, 0);

                        if (!eventCompleted)
                        {
                            // Event is not complete, update for next buffer
                            globalEvent.SampleTime = _currentSample + _bufferSize;
                            scheduleEvent.SampleTime = globalEvent.SampleTime;
                            scheduleEvent.Value = buffer[_bufferSize - 1]; // Last value in buffer
                            _globalEventQueue.Add(globalEvent);
                        }
                        else
                        {
                            // Event is complete
                            _nodeLastScheduledValues[node][param] = scheduleEvent.TargetValue;
                            _eventPool.Return(scheduleEvent);
                        }
                    }
                    else
                    {
                        // Event is beyond the current buffer; re-add to the queue for future processing
                        _globalEventQueue.Add(globalEvent);
                        break; // No more events to process for this buffer
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr(ex, "Error processing GlobalScheduleEvent for Node: {Node}, Param: {Param}, SampleTime: {SampleTime}", node, param, globalEvent.SampleTime);
                }
                finally
                {
                    nodeLock.ExitWriteLock();
                }
            }

            // Advance the current sample
            _currentSample += _bufferSize;
        }


        private bool ApplyEventToBuffer(double[] buffer, ScheduleEvent scheduleEvent, int startIndex)
        {
            if (scheduleEvent.EndSampleTime.HasValue)
            {
                int endIndex = _bufferSize;
                int endSampleIndex = (int)(scheduleEvent.EndSampleTime.Value - _currentSample);
                endIndex = Math.Min(endSampleIndex, _bufferSize);

                for (int i = startIndex; i < endIndex; i++)
                {
                    double t = (_currentSample + i - scheduleEvent.SampleTime) / (scheduleEvent.EndSampleTime.Value - scheduleEvent.SampleTime);

                    if (scheduleEvent.IsExponential)
                    {
                        buffer[i] = InterpolateExponential(scheduleEvent.Value, scheduleEvent.TargetValue, t);
                    }
                    else
                    {
                        buffer[i] = InterpolateLinear(scheduleEvent.Value, scheduleEvent.TargetValue, t);
                    }
                }

                if (endIndex < _bufferSize)
                {
                    // Event is complete within this buffer
                    return true; // Event is complete
                }
                else
                {
                    // Event is not complete, will continue in next buffer
                    return false; // Event is not complete
                }
            }
            else
            {
                // Instant value change, set buffer value directly from startIndex to end
                for (int i = startIndex; i < _bufferSize; i++)
                {
                    buffer[i] = scheduleEvent.Value;
                }
                return true; // Event is complete
            }
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
                if (_nodeParameterBuffers[node].TryGetValue(param, out var buffer))
                {
                    if (sampleIndex >= 0 && sampleIndex < buffer.Length)
                    {
                        return buffer[sampleIndex];
                    }
                    else
                    {
                        return _nodeLastScheduledValues[node][param];
                    }
                }
                else
                {
                    return _nodeLastScheduledValues[node][param];
                }
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
                _globalEventQueue.Clear();
                // Do not reset _nodeLastScheduledValues
                // Instead, fill parameter buffers with last known values
                foreach (var node in _nodeParameterBuffers.Keys)
                {
                    foreach (var param in _nodeParameterBuffers[node].Keys)
                    {
                        var currentValue = _nodeLastScheduledValues[node][param];
                        Array.Fill(_nodeParameterBuffers[node][param], currentValue);
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

        // Classes for the global event queue

        private class GlobalScheduleEvent
        {
            public double SampleTime { get; set; }
            public AudioNode Node { get; set; }
            public AudioParam Param { get; set; }
            public ScheduleEvent Event { get; set; }
        }
        private class GlobalScheduleEventComparer : IComparer<GlobalScheduleEvent>
        {
            public int Compare(GlobalScheduleEvent x, GlobalScheduleEvent y)
            {
                // Handle null references
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                // Compare SampleTime first
                int timeComparison = x.SampleTime.CompareTo(y.SampleTime);
                if (timeComparison != 0) return timeComparison;

                // Handle null Events
                if (x.Event == null && y.Event == null) return 0;
                if (x.Event == null) return -1;
                if (y.Event == null) return 1;

                // Compare Event IDs to ensure uniqueness
                return x.Event.Id.CompareTo(y.Event.Id);
            }
        }


        private class ScheduleEvent
        {
            private static long _nextId = 0;
            public long Id { get; private set; }

            public double SampleTime { get; set; }
            public double Value { get; set; }
            public double? EndSampleTime { get; set; }
            public double TargetValue { get; set; }
            public bool IsExponential { get; set; }
            public bool IsSetValueAtTime { get; set; }

            public ScheduleEvent(double sampleTime, double value, double? endSampleTime = null, double targetValue = 0.0, bool isExponential = false, bool isSetValueAtTime = false)
            {
                Id = Interlocked.Increment(ref _nextId);
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
