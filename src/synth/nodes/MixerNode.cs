using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;

namespace Synth
{
    public class MixerNode : AudioNode
    {
        public float Gain = 0.5f;
        private Vector<float> gainVector;
        private Vector<float> negOne = new Vector<float>(-1.0f);
        private Vector<float> posOne = new Vector<float>(1.0f);

        // Pre-allocated arrays for parameter loading
        private float[] gainArray;
        private float[] balanceArray1;
        private float[] balanceArray2;
        private float[] sampleArray;

        public MixerNode()
        {
            RightBuffer = new float[NumSamples];
            LeftBuffer = new float[NumSamples];
            gainVector = new Vector<float>(Gain);

            int vectorSize = Vector<float>.Count;
            gainArray = new float[vectorSize];
            balanceArray1 = new float[vectorSize];
            balanceArray2 = new float[vectorSize];
            sampleArray = new float[vectorSize];
        }

        public override void Process(double increment)
        {
            List<AudioNode> nodes = GetParameterNodes(AudioParam.Input);
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }
            Array.Clear(LeftBuffer);
            Array.Clear(RightBuffer);

            int vectorSize = Vector<float>.Count;

            foreach (var node in nodes)
            {
                if (node == null || !node.Enabled)
                {
                    continue;
                }

                Vector<float> nodeBalanceVector = new Vector<float>(node.Balance);
                ReadOnlySpan<float> nodeBuffer = node.GetBuffer();

                int i = 0;
                for (; i <= NumSamples - vectorSize; i += vectorSize)
                {
                    // Load parameters for the current batch
                    LoadParameters(i, vectorSize, node);

                    Vector<float> gainParam = new Vector<float>(gainArray);
                    Vector<float> balanceParam1 = new Vector<float>(balanceArray1) + nodeBalanceVector;
                    Vector<float> balanceParam2 = new Vector<float>(balanceArray2);
                    Vector<float> sample = new Vector<float>(nodeBuffer.Slice(i, vectorSize)) * gainParam;

                    // Compute balance and clamp
                    Vector<float> balance = balanceParam1 * balanceParam2;
                    balance = Vector.Max(negOne, Vector.Min(balance, posOne));

                    // Compute left and right volumes
                    Vector<float> leftVolume = Vector.ConditionalSelect(Vector.LessThan(balance, Vector<float>.Zero), posOne, posOne - balance);
                    Vector<float> rightVolume = Vector.ConditionalSelect(Vector.GreaterThan(balance, Vector<float>.Zero), posOne, posOne + balance);

                    // Mix into left and right buffers directly
                    Vector<float> leftResult = new Vector<float>(new ReadOnlySpan<float>(LeftBuffer, i, vectorSize)) + leftVolume * sample * gainVector;
                    Vector<float> rightResult = new Vector<float>(new ReadOnlySpan<float>(RightBuffer, i, vectorSize)) + rightVolume * sample * gainVector;

                    // Store results back to the buffers without copying
                    leftResult.CopyTo(LeftBuffer, i);
                    rightResult.CopyTo(RightBuffer, i);
                }

                // Handle remaining elements
                for (; i < NumSamples; i++)
                {
                    ProcessSingleSample(i, node, nodeBuffer);
                }
            }
        }

        private void LoadParameters(int startIndex, int count, AudioNode node)
        {
            for (int j = 0; j < count; j++)
            {
                gainArray[j] = GetParameter(AudioParam.Gain, startIndex + j).Item2;
                balanceArray1[j] = node.GetParameter(AudioParam.Balance, startIndex + j).Item1;
                balanceArray2[j] = node.GetParameter(AudioParam.Balance, startIndex + j).Item2;
            }
        }

        private void ProcessSingleSample(int index, AudioNode node, ReadOnlySpan<float> nodeBuffer)
        {
            var gainParam = GetParameter(AudioParam.Gain, index);
            var balanceParam = node.GetParameter(AudioParam.Balance, index);

            float sample = nodeBuffer[index] * gainParam.Item2;
            float balance = Math.Clamp((balanceParam.Item1 + node.Balance) * balanceParam.Item2, -1f, 1f);

            float leftVolume = balance < 0 ? 1.0f : 1.0f - balance;
            float rightVolume = balance > 0 ? 1.0f : 1.0f + balance;

            LeftBuffer[index] += leftVolume * sample * Gain;
            RightBuffer[index] += rightVolume * sample * Gain;
        }
    }
}