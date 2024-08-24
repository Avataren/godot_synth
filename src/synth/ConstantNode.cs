namespace Synth
{
    public class ConstantNode : AudioNode
    {
        public ConstantNode() : base()
        {
            _scheduler.RegisterNode(this, [AudioParam.ConstValue]);
        }

        public override void Process(double increment)
        {
            float[] bufferRef = buffer; // Cache the buffer reference

            for (int i = 0; i < NumSamples; i++)
            {
                bufferRef[i] = (float)_scheduler.GetValueAtSample(this, AudioParam.ConstValue,i);
            }
        }

        public void SetValueAtTime(double value, double time)
        {
            _scheduler.ScheduleValueAtTime(this, AudioParam.ConstValue, value, time); // Gate opens at this time
        }

        //public void LinearRampToValueAtTime(double value, double time)
        //{
        //_scheduler.LinearRampToValueAtTime(this, AudioParam.Gate, value, time); // Gate closes at this time
        //}

        // public override float this[int index]
        // {
        //     get => Value;
        //     set => Value = value;
        // }

        public void LinearRampToValueAtTime(double value, double time)
        {
            base.LinearRampToValueAtTime(AudioParam.ConstValue, value, time);
        }

        public void ExponentialRampToValueAtTime(double value, double time)
        {
            base.ExponentialRampToValueAtTime(AudioParam.ConstValue, value, time);
        }
    }
}