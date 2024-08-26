using System;
using Godot;

namespace Synth
{

    public class MixerNode : AudioNode
    {
        public float Gain = 0.5f;
        public MixerNode()
        {
            RightBuffer = new float[NumSamples];
            LeftBuffer = new float[NumSamples];
        }

        public override void Process(double increment)
        {
            var nodes = GetParameterNodes(AudioParam.Input);
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }
            Array.Clear(LeftBuffer);
            Array.Clear(RightBuffer);

            foreach (var node in nodes)
            {
                if (node == null || !node.Enabled)
                {
                    continue;
                }
                // Retrieve node-specific parameters once per node
                float nodeBalance = node.Balance;
                for (int i = 0; i < NumSamples; i++)
                {
                    var gainParam = GetParameter(AudioParam.Gain, i);
                    var balanceParam = node.GetParameter(AudioParam.Balance, i);

                    //float nodeGain = GetParameter(AudioParam.Gain, i, 1.0f);
                    float sample = node[i] * gainParam.Item2;
                    float balance = (balanceParam.Item1 + nodeBalance) * balanceParam.Item2;

                    // Clip balance to -1 to 1
                    balance = Math.Max(-1, Math.Min(1, balance));

                    // Calculate left and right channel volumes based on balance
                    float leftVolume = balance < 0 ? 1.0f : 1.0f - balance;
                    float rightVolume = balance > 0 ? 1.0f : 1.0f + balance;

                    // Mix into left and right buffers
                    LeftBuffer[i] += leftVolume * sample * Gain;
                    RightBuffer[i] += rightVolume * sample * Gain;
                }
            }
        }

    }
}