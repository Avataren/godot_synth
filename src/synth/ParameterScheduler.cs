using System;
using System.Collections.Generic;

namespace Synth
{
    public class ParameterScheduler
    {
        private readonly int _bufferSize;
        private readonly Dictionary<AudioParam, double[]> _parameterBuffers = new();
        private readonly Dictionary<AudioParam, List<ScheduleEvent>> _eventDictionary = new();
        private readonly Dictionary<AudioParam, bool> _hasRemainingEvents = new();
        private readonly Dictionary<AudioParam, double> _lastScheduledValues = new();
        private double _currentTimeInSeconds = 0.0;

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;

            foreach (AudioParam param in Enum.GetValues(typeof(AudioParam)))
            {
                _parameterBuffers[param] = new double[bufferSize];
                _eventDictionary[param] = new List<ScheduleEvent>();
                _hasRemainingEvents[param] = true;
                _lastScheduledValues[param] = 0.0;
            }
        }

        public void ScheduleValueAtTime(AudioParam param, double value, double timeInSeconds)
        {
            var events = _eventDictionary[param];
            int index = events.BinarySearch(new ScheduleEvent(timeInSeconds, value), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
            if (index < 0) index = ~index;
            events.Insert(index, new ScheduleEvent(timeInSeconds, value));
            _hasRemainingEvents[param] = true;
        }

        public void LinearRampToValueAtTime(AudioParam param, double targetValue, double startTimeInSeconds, double endTimeInSeconds)
        {
            var events = _eventDictionary[param];
            int index = events.BinarySearch(new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds), Comparer<ScheduleEvent>.Create((a, b) => a.Time.CompareTo(b.Time)));
            if (index < 0) index = ~index;
            events.Insert(index, new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds));
            _hasRemainingEvents[param] = true;
        }

        public void Process(double increment)
        {
            _currentTimeInSeconds += increment * _bufferSize;

            foreach (var param in _eventDictionary.Keys)
            {
                if (!_hasRemainingEvents[param])
                {
                    continue;
                }

                var buffer = _parameterBuffers[param];
                var events = _eventDictionary[param];
                double lastScheduledValue = _lastScheduledValues[param];
                bool eventsProcessed = false;

                for (int i = 0; i < _bufferSize; i++)
                {
                    double timeAtSample = _currentTimeInSeconds + (i * increment);
                    double newValue = GetScheduledValueAtTime(param, timeAtSample, ref lastScheduledValue);

                    if (newValue != lastScheduledValue)
                    {
                        eventsProcessed = true;
                    }

                    buffer[i] = newValue;
                    lastScheduledValue = newValue; // Update the last value after setting it

                    if (!eventsProcessed && events.Count == 0)
                    {
                        // If we processed all events, fill the rest of the buffer with the last known value
                        FillRemainingBuffer(buffer, i, lastScheduledValue);
                        break; // No need to continue processing, exit early
                    }
                }

                _lastScheduledValues[param] = lastScheduledValue; // Update the last scheduled value

                // Remove processed events and skip further processing if no future events are within the current buffer
                events.RemoveAll(evt => evt.Time <= _currentTimeInSeconds);
            }
        }

        private double GetScheduledValueAtTime(AudioParam param, double timeInSeconds, ref double lastScheduledValue)
        {
            var events = _eventDictionary[param];

            foreach (var evt in events)
            {
                if (timeInSeconds >= evt.Time)
                {
                    lastScheduledValue = evt.Value;
                }
                else
                {
                    break; // Early exit if event time is beyond the current sample time
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

        public double GetValueAtSample(AudioParam param, int sampleIndex)
        {
            if (sampleIndex < _bufferSize)
            {
                return _parameterBuffers[param][sampleIndex];
            }
            return 0.0;
        }

        public void Clear()
        {
            foreach (var param in _parameterBuffers.Keys)
            {
                Array.Clear(_parameterBuffers[param], 0, _bufferSize);
                _eventDictionary[param].Clear();
                _hasRemainingEvents[param] = false;
                _lastScheduledValues[param] = 0.0;
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
