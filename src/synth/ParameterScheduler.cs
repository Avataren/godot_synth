using System;
using System.Collections.Generic;
using Godot;

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
        public void ExponentialRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            if (targetValue <= 0)
            {
                throw new ArgumentException("Exponential ramps require the target value to be positive.");
            }

            lock (_lock)
            {
                var events = _nodeEventDictionary[node][param];
                double startTimeInSeconds = _currentTimeInSeconds;

                if (endTimeInSeconds <= startTimeInSeconds + 0.001)
                {
                    events.Add(new ScheduleEvent(startTimeInSeconds, targetValue));
                }
                else
                {
                    double currentValue = _nodeLastScheduledValues[node][param];
                    if (currentValue <= 0)
                    {
                        throw new InvalidOperationException("Exponential ramps require the current value to be positive.");
                    }
                    events.Add(new ScheduleEvent(startTimeInSeconds, currentValue, endTimeInSeconds, targetValue, currentValue, isExponential: true));
                }

                _nodeHasRemainingEvents[node][param] = true;
            }
        }


        public void LinearRampToValueAtTime(AudioNode node, AudioParam param, double targetValue, double endTimeInSeconds)
        {
            lock (_lock)
            {
                var events = _nodeEventDictionary[node][param];
                double startTimeInSeconds = _currentTimeInSeconds;

                if (endTimeInSeconds <= startTimeInSeconds + 0.001)
                {
                    events.Add(new ScheduleEvent(startTimeInSeconds, targetValue));
                }
                else
                {
                    double currentValue = _nodeLastScheduledValues[node][param];
                    events.Add(new ScheduleEvent(startTimeInSeconds, currentValue, endTimeInSeconds, targetValue, currentValue, isExponential: false));
                }

                _nodeHasRemainingEvents[node][param] = true;
            }
        }



        public void Process(double increment)
        {
            lock (_lock)
            {
                foreach (var node in _nodeEventDictionary.Keys)
                {
                    foreach (var param in _nodeEventDictionary[node].Keys)
                    {
                        var buffer = _nodeParameterBuffers[node][param];

                        if (!_nodeHasRemainingEvents[node][param])
                        {
                            Array.Fill(buffer, _nodeLastScheduledValues[node][param]);
                            continue;
                        }

                        var events = _nodeEventDictionary[node][param];
                        double lastScheduledValue = _nodeLastScheduledValues[node][param];
                        double currentTime = _currentTimeInSeconds;

                        for (int i = 0; i < _bufferSize; i++)
                        {
                            double timeAtSample = currentTime + (i * increment);

                            if (events.Count > 0)
                            {
                                var evt = events[0];

                                if (evt.EndTime.HasValue && evt.EndTime.Value > evt.Time)
                                {
                                    if (timeAtSample >= evt.Time && timeAtSample <= evt.EndTime.Value)
                                    {
                                        double progress = (timeAtSample - evt.Time) / (evt.EndTime.Value - evt.Time);

                                        if (evt.IsExponential)
                                        {
                                            if (evt.InitialValue > 0 && evt.TargetValue > 0)
                                            {
                                                lastScheduledValue = evt.InitialValue * Math.Pow(evt.TargetValue / evt.InitialValue, progress);
                                            }
                                            else
                                            {
                                                throw new InvalidOperationException("Exponential ramps require positive initial and target values.");
                                            }
                                        }
                                        else
                                        {
                                            lastScheduledValue = evt.InitialValue + progress * (evt.TargetValue - evt.InitialValue);
                                        }
                                    }
                                    else if (timeAtSample > evt.EndTime.Value)
                                    {
                                        lastScheduledValue = evt.TargetValue;
                                        events.RemoveAt(0);
                                        _processedEventCount++;
                                    }
                                }
                                else if (timeAtSample >= evt.Time)
                                {
                                    lastScheduledValue = evt.Value;
                                    events.RemoveAt(0);
                                    _processedEventCount++;
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

                _processedEventCount = 0;
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
