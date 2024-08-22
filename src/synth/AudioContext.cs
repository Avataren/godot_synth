using Godot;

namespace Synth
{
    public class AudioContext
    {
        static int _bufferSize = 512;
        static int _sampleRate = 44100;
        public static int Oversampling = 4;

        public static int BufferSize
        {
            get => _bufferSize; set
            {
                _bufferSize = value * Oversampling;
                Instance.ResetScheduler();
            }
        }
        public static int SampleRate 
        {
            get => _sampleRate; set
            {
                _sampleRate = value * Oversampling;
                Instance.ResetScheduler();
            }
        }

        ParameterScheduler _scheduler = null;
        static AudioContext _instance = null;
        static double _currentTimeInSeconds = Scheduler.CurrentTimeInSeconds;

        public double CurrentTimeInSeconds
        {
            get => _currentTimeInSeconds;
        }

        public static ParameterScheduler Scheduler
        {
            get => Instance._scheduler;
        }

        public static AudioContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioContext();
                }
                return _instance;
            }
        }

        public void ResetScheduler()
        {
            _scheduler = new ParameterScheduler(BufferSize, SampleRate);
        }

        public void ResetTime()
        {
            _currentTimeInSeconds = 0.0;
        }

        public AudioContext()
        {
            ResetScheduler();
        }

        public void Process (double increment)
        {
            _scheduler.Process(increment);
        }
    }
}