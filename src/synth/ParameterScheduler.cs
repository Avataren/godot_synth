using System;
using System.Collections.Generic;

namespace Synth
{
    public class ParameterScheduler
    {
        private readonly int _bufferSize;
        private readonly double[] _parameterBuffer;
        private double _currentTimeInSeconds = 0.0;
        private int _currentSampleIndex = 0;
        private readonly int _sampleRate;

        private readonly List<ScheduleEvent> _events = new();
        private double _lastScheduledValue = 0.0; // Track the last scheduled value

        public ParameterScheduler(int bufferSize, int sampleRate)
        {
            _bufferSize = bufferSize;
            _parameterBuffer = new double[bufferSize];
            _sampleRate = sampleRate;
        }

        public void ScheduleValueAtTime(double value, double timeInSeconds)
        {
            _events.Add(new ScheduleEvent(timeInSeconds, value));
            _events.Sort((a, b) => a.Time.CompareTo(b.Time)); // Ensure events are sorted by time
        }

        public void LinearRampToValueAtTime(double targetValue, double startTimeInSeconds, double endTimeInSeconds)
        {
            _events.Add(new ScheduleEvent(startTimeInSeconds, targetValue, endTimeInSeconds));
            _events.Sort((a, b) => a.Time.CompareTo(b.Time)); // Ensure events are sorted by time
        }

        public void Process(double increment)
        {
            _currentTimeInSeconds += increment * _bufferSize;
            _currentSampleIndex = 0;

            // Refill the buffer based on the current time
            for (int i = 0; i < _bufferSize; i++)
            {
                double timeAtSample = _currentTimeInSeconds + (i * increment);
                _parameterBuffer[i] = GetScheduledValueAtTime(timeAtSample);
            }

            // Remove events that have already been processed
            _events.RemoveAll(evt => evt.EndTime.HasValue ? evt.EndTime.Value <= _currentTimeInSeconds : evt.Time <= _currentTimeInSeconds);
        }

        private double GetScheduledValueAtTime(double timeInSeconds)
        {
            foreach (var evt in _events)
            {
                if (timeInSeconds < evt.Time)
                    break;

                if (evt.EndTime.HasValue && timeInSeconds >= evt.Time && timeInSeconds <= evt.EndTime.Value)
                {
                    double progress = (timeInSeconds - evt.Time) / (evt.EndTime.Value - evt.Time);
                    _lastScheduledValue = evt.Value + (evt.TargetValue - evt.Value) * progress;
                }
                else if (!evt.EndTime.HasValue)
                {
                    _lastScheduledValue = evt.Value;
                }
            }

            return _lastScheduledValue;
        }

        public double GetValueAtSample(int sampleIndex)
        {
            if (sampleIndex < _bufferSize)
            {
                return _parameterBuffer[sampleIndex];
            }
            return 0.0; // Default value if out of range
        }

        public void Clear()
        {
            Array.Clear(_parameterBuffer, 0, _bufferSize);
            _events.Clear();
            _lastScheduledValue = 0.0; // Reset the last value
        }
    }

    public class ScheduleEvent
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
    }
}
